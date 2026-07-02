using System;
using UnityEngine;

public static class FairModeSettings
{
    private const string ServerArgument = "--fair-server";
    private const string MachineArgument = "--fair-machine";
    private const string ServerArgumentPrefix = ServerArgument + "=";
    private const string MachineArgumentPrefix = MachineArgument + "=";
    private const string DisabledArgument = "--fair-disabled";
    private const string ServerBaseUrlPrefsKey = "FairMode.ServerBaseUrl";
    private const string DefaultServerBaseUrl = "http://localhost:8080";

    public static bool IsEnabled => !HasArgument(DisabledArgument);
    public static bool HasExplicitServerArgument => !string.IsNullOrWhiteSpace(GetArgumentValue(ServerArgument, ServerArgumentPrefix));

    public static string ServerBaseUrl
    {
        get
        {
            string commandLineValue = GetArgumentValue(ServerArgument, ServerArgumentPrefix);
            if (!string.IsNullOrWhiteSpace(commandLineValue))
            {
                return NormalizeBaseUrl(commandLineValue);
            }

            string savedValue = PlayerPrefs.GetString(ServerBaseUrlPrefsKey, DefaultServerBaseUrl);
            return NormalizeBaseUrl(savedValue);
        }
    }

    public static string MachineId
    {
        get
        {
            string commandLineValue = GetArgumentValue(MachineArgument, MachineArgumentPrefix);
            if (!string.IsNullOrWhiteSpace(commandLineValue))
            {
                return Clamp(commandLineValue.Trim(), 64);
            }

            string deviceName = string.IsNullOrWhiteSpace(SystemInfo.deviceName)
                ? Environment.MachineName
                : SystemInfo.deviceName;
            string uniqueId = string.IsNullOrWhiteSpace(SystemInfo.deviceUniqueIdentifier)
                ? "device"
                : SystemInfo.deviceUniqueIdentifier;
            return Clamp($"{deviceName}-{uniqueId}", 64);
        }
    }

    public static string BuildVersion => Clamp(Application.version, 64);

    public static float RequestTimeoutSeconds => 10f;
    public static float StartupProbeTimeoutSeconds => 4f;
    public static float HeartbeatIntervalSeconds => 20f;

    public static void SaveServerBaseUrl(string serverBaseUrl)
    {
        PlayerPrefs.SetString(ServerBaseUrlPrefsKey, NormalizeBaseUrl(serverBaseUrl));
        PlayerPrefs.Save();
    }

    private static string NormalizeBaseUrl(string value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? DefaultServerBaseUrl : value.Trim();
        if (normalized.EndsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - "/health".Length);
        }

        return normalized.TrimEnd('/');
    }

    private static string GetArgumentValue(string argument, string prefix)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg != null && arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring(prefix.Length);
            }

            if (string.Equals(arg, argument, StringComparison.OrdinalIgnoreCase)
                && index + 1 < args.Length
                && !string.IsNullOrWhiteSpace(args[index + 1])
                && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static bool HasArgument(string argument)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], argument, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Clamp(string value, int maxLength)
    {
        string normalized = value ?? string.Empty;
        return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
    }
}
