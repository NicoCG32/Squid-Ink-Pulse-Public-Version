using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidReadinessAuditor
{
    private const string MenuPath = "Tools/Squid Ink Pulse/Audit Android Readiness";

    [MenuItem(MenuPath)]
    public static void RunAndroidReadinessAudit()
    {
        AndroidReadinessSnapshot snapshot = CaptureSnapshot();
        var findings = AndroidReadinessRules.Evaluate(snapshot);

        foreach (AndroidReadinessFinding finding in findings)
        {
            string message = $"[AndroidReadinessAuditor][{finding.Severity.ToString().ToUpperInvariant()}][{finding.Code}] {finding.Message}";
            switch (finding.Severity)
            {
                case AndroidReadinessSeverity.Error:
                    Debug.LogError(message);
                    break;
                case AndroidReadinessSeverity.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }

        int errorCount = findings.Count(finding => finding.Severity == AndroidReadinessSeverity.Error);
        int warningCount = findings.Count(finding => finding.Severity == AndroidReadinessSeverity.Warning);
        string summary = $"[AndroidReadinessAuditor] Auditoria finalizada: {errorCount} error(es), {warningCount} advertencia(s), {findings.Count - errorCount - warningCount} dato(s) informativo(s).";

        if (errorCount == 0)
        {
            Debug.Log(summary);
            return;
        }

        if (Application.isBatchMode)
        {
            throw new BuildFailedException(summary);
        }

        Debug.LogWarning(summary);
    }

    private static AndroidReadinessSnapshot CaptureSnapshot()
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        string[] existingSeeds = AndroidReadinessRules.ExpectedSeedPaths
            .Where(File.Exists)
            .ToArray();

        bool inputSystemEnabled;
#if ENABLE_INPUT_SYSTEM
        inputSystemEnabled = true;
#else
        inputSystemEnabled = false;
#endif

        AndroidArchitecture architectures = PlayerSettings.Android.targetArchitectures;

        return new AndroidReadinessSnapshot
        {
            UnityVersion = Application.unityVersion,
            IsAndroidModuleInstalled = BuildPipeline.IsBuildTargetSupported(
                BuildTargetGroup.Android,
                BuildTarget.Android),
            EnabledBuildScenes = enabledScenes,
            IsInputSystemEnabled = inputSystemEnabled,
            CompanyName = PlayerSettings.companyName,
            ApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
            UsesAutoRotation = PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation,
            AllowsPortrait = PlayerSettings.allowedAutorotateToPortrait,
            AllowsPortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown,
            AllowsLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft,
            AllowsLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight,
            SupportsArm64 = (architectures & AndroidArchitecture.ARM64) != 0,
            UsesIl2Cpp = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
            ExistingSeedPaths = existingSeeds
        };
    }
}
