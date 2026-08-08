using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public enum AndroidReadinessSeverity
{
    Info,
    Warning,
    Error
}

public sealed class AndroidReadinessFinding
{
    public AndroidReadinessFinding(string code, AndroidReadinessSeverity severity, string message)
    {
        Code = code ?? string.Empty;
        Severity = severity;
        Message = message ?? string.Empty;
    }

    public string Code { get; }
    public AndroidReadinessSeverity Severity { get; }
    public string Message { get; }
}

public sealed class AndroidReadinessSnapshot
{
    public string UnityVersion { get; set; } = string.Empty;
    public bool IsAndroidModuleInstalled { get; set; }
    public string[] EnabledBuildScenes { get; set; } = Array.Empty<string>();
    public bool IsInputSystemEnabled { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ApplicationIdentifier { get; set; } = string.Empty;
    public bool UsesAutoRotation { get; set; }
    public bool AllowsPortrait { get; set; }
    public bool AllowsPortraitUpsideDown { get; set; }
    public bool AllowsLandscapeLeft { get; set; }
    public bool AllowsLandscapeRight { get; set; }
    public bool SupportsArm64 { get; set; }
    public bool UsesIl2Cpp { get; set; }
    public string[] ExistingSeedPaths { get; set; } = Array.Empty<string>();
}

public static class AndroidReadinessRules
{
    public const string ExpectedUnityVersion = "6000.3.11f1";

    public static readonly string[] ExpectedBuildScenes =
    {
        "Assets/Scenes/MainMenu/MainMenu.unity",
        "Assets/Scenes/Game/ZonaEpipelagica.unity",
        "Assets/Scenes/Game/ZonaAbisopelagica.unity",
        "Assets/Scenes/ShopMenu/ShopMenu.unity"
    };

    public static readonly string[] ExpectedSeedPaths =
    {
        "Assets/Implementation/Resources/PersistentDbSeeds/player-profile.json",
        "Assets/Implementation/Resources/PersistentDbSeeds/player-records.json",
        "Assets/Implementation/Resources/PersistentDbSeeds/unlockables-catalog.json",
        "Assets/Implementation/Resources/PersistentDbSeeds/local-leaderboard.json"
    };

    private static readonly Regex ApplicationIdentifierPattern = new(
        @"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$",
        RegexOptions.CultureInvariant);

    public static IReadOnlyList<AndroidReadinessFinding> Evaluate(AndroidReadinessSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        List<AndroidReadinessFinding> findings = new();

        AddPassOrError(
            findings,
            "UNITY_VERSION",
            string.Equals(snapshot.UnityVersion, ExpectedUnityVersion, StringComparison.Ordinal),
            $"Unity coincide con la version requerida: {ExpectedUnityVersion}.",
            $"Unity debe ser {ExpectedUnityVersion}; actual: {ValueOrMissing(snapshot.UnityVersion)}.");

        AddPassOrError(
            findings,
            "ANDROID_MODULE",
            snapshot.IsAndroidModuleInstalled,
            "Android Build Support esta disponible.",
            "Android Build Support no esta disponible para este Editor.");

        bool scenesMatch = (snapshot.EnabledBuildScenes ?? Array.Empty<string>())
            .SequenceEqual(ExpectedBuildScenes, StringComparer.Ordinal);
        AddPassOrError(
            findings,
            "BUILD_SCENES",
            scenesMatch,
            "Las escenas habilitadas coinciden con el contrato y su orden.",
            "EditorBuildSettings debe contener exactamente MainMenu, ZonaEpipelagica, ZonaAbisopelagica y ShopMenu en ese orden.");

        AddPassOrError(
            findings,
            "INPUT_SYSTEM",
            snapshot.IsInputSystemEnabled,
            "El Input System esta habilitado.",
            "El Input System debe estar habilitado antes de implementar controles moviles.");

        if (IsGenericCompanyName(snapshot.CompanyName))
        {
            findings.Add(new AndroidReadinessFinding(
                "COMPANY_NAME",
                AndroidReadinessSeverity.Warning,
                $"Company Name sigue siendo generico: {ValueOrMissing(snapshot.CompanyName)}."));
        }
        else
        {
            findings.Add(new AndroidReadinessFinding(
                "COMPANY_NAME",
                AndroidReadinessSeverity.Info,
                $"Company Name configurado: {snapshot.CompanyName}."));
        }

        if (IsGenericApplicationIdentifier(snapshot.ApplicationIdentifier))
        {
            findings.Add(new AndroidReadinessFinding(
                "APPLICATION_IDENTIFIER",
                AndroidReadinessSeverity.Error,
                $"El identificador Android no esta configurado o sigue siendo generico: {ValueOrMissing(snapshot.ApplicationIdentifier)}."));
        }
        else if (!ApplicationIdentifierPattern.IsMatch(snapshot.ApplicationIdentifier))
        {
            findings.Add(new AndroidReadinessFinding(
                "APPLICATION_IDENTIFIER",
                AndroidReadinessSeverity.Error,
                $"El identificador Android no tiene formato reverse-DNS valido: {snapshot.ApplicationIdentifier}."));
        }
        else
        {
            findings.Add(new AndroidReadinessFinding(
                "APPLICATION_IDENTIFIER",
                AndroidReadinessSeverity.Info,
                $"Identificador Android configurado: {snapshot.ApplicationIdentifier}."));
        }

        bool orientationMatches = snapshot.UsesAutoRotation
            && snapshot.AllowsLandscapeLeft
            && snapshot.AllowsLandscapeRight
            && !snapshot.AllowsPortrait
            && !snapshot.AllowsPortraitUpsideDown;
        AddPassOrError(
            findings,
            "ORIENTATION",
            orientationMatches,
            "Orientacion configurada para landscape left y right, sin portrait.",
            "La orientacion debe usar autorrotacion entre landscape left/right y deshabilitar ambas variantes portrait.");

        AddPassOrError(
            findings,
            "ARM64",
            snapshot.SupportsArm64,
            "ARM64 esta habilitado para Android.",
            "ARM64 debe estar habilitado para Android.");

        AddPassOrError(
            findings,
            "IL2CPP",
            snapshot.UsesIl2Cpp,
            "IL2CPP esta configurado para Android.",
            "Android debe utilizar IL2CPP antes de generar la candidata.");

        string[] missingSeeds = FindMissingSeeds(snapshot.ExistingSeedPaths);
        AddPassOrError(
            findings,
            "SEEDS",
            missingSeeds.Length == 0,
            "Las cuatro semillas de persistencia estan presentes.",
            $"Faltan semillas de persistencia: {string.Join(", ", missingSeeds)}.");

        return findings;
    }

    public static string[] FindMissingSeeds(IEnumerable<string> existingSeedPaths)
    {
        HashSet<string> existing = new(
            existingSeedPaths ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        return ExpectedSeedPaths
            .Where(path => !existing.Contains(path))
            .ToArray();
    }

    private static bool IsGenericCompanyName(string companyName)
    {
        return string.IsNullOrWhiteSpace(companyName)
            || string.Equals(companyName.Trim(), "DefaultCompany", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericApplicationIdentifier(string applicationIdentifier)
    {
        if (string.IsNullOrWhiteSpace(applicationIdentifier))
        {
            return true;
        }

        return applicationIdentifier.Contains("DefaultCompany", StringComparison.OrdinalIgnoreCase)
            || applicationIdentifier.Contains("2D-URP", StringComparison.OrdinalIgnoreCase)
            || applicationIdentifier.StartsWith("com.unity.", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddPassOrError(
        ICollection<AndroidReadinessFinding> findings,
        string code,
        bool passed,
        string passMessage,
        string errorMessage)
    {
        findings.Add(new AndroidReadinessFinding(
            code,
            passed ? AndroidReadinessSeverity.Info : AndroidReadinessSeverity.Error,
            passed ? passMessage : errorMessage));
    }

    private static string ValueOrMissing(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<sin configurar>" : value;
    }
}
