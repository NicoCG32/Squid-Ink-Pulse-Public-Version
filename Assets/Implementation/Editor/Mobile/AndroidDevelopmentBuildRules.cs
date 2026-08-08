using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class AndroidDevelopmentBuildRules
{
    public const string DefaultOutputRelativePath = "Build/AndroidDevelopment/SquidInkPulse-development.apk";
    public const string AndroidSupportUnavailableMessage =
        "Android Build Support no esta disponible para este Editor de Unity.";

    public static string GetAndroidSupportError(bool isAndroidSupportAvailable)
    {
        return isAndroidSupportAvailable ? null : AndroidSupportUnavailableMessage;
    }

    public static string[] FindMissingRequiredScenes(IEnumerable<string> enabledScenes)
    {
        HashSet<string> enabled = new(
            enabledScenes ?? Array.Empty<string>(),
            StringComparer.Ordinal);

        return AndroidReadinessRules.ExpectedBuildScenes
            .Where(scene => !enabled.Contains(scene))
            .ToArray();
    }

    public static bool IsOutputInsideBuildDirectory(string projectRoot, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || string.IsNullOrWhiteSpace(outputPath))
        {
            return false;
        }

        try
        {
            string normalizedProjectRoot = Path.GetFullPath(projectRoot);
            string buildRoot = Path.GetFullPath(Path.Combine(normalizedProjectRoot, "Build"));
            string normalizedOutput = Path.GetFullPath(outputPath);
            string buildRootWithSeparator = buildRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return normalizedOutput.StartsWith(buildRootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
