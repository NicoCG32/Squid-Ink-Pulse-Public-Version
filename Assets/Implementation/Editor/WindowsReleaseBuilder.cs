using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class WindowsReleaseBuilder
{
    [MenuItem("Tools/Squid Ink Pulse/Build Windows Release")]
    public static void BuildWindowsRelease()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new BuildFailedException("Windows Build Support no esta disponible.");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (!scenes.SequenceEqual(AndroidReadinessRules.ExpectedBuildScenes))
            throw new BuildFailedException("La distribucion requiere las cuatro escenas de producto en el orden canonico.");

        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(root, "Build", "WindowsRelease");
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new BuildFailedException("Build/WindowsRelease ya contiene archivos; preserve el artefacto antes de repetir.");

        Directory.CreateDirectory(output);
        BuildPlayerOptions options = new()
        {
            scenes = scenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = Path.Combine(output, "SquidInkPulse.exe"),
            options = BuildOptions.None
        };
        Stopwatch clock = Stopwatch.StartNew();
        BuildReport report = BuildPipeline.BuildPlayer(options);
        clock.Stop();
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Windows release: {report.summary.result}, errores={report.summary.totalErrors}.");
        if ((report.summary.options & (BuildOptions.Development | BuildOptions.AllowDebugging)) != 0)
            throw new BuildFailedException("El build de distribucion no debe habilitar Development ni AllowDebugging.");

        File.WriteAllText(Path.Combine(output, "build-info.json"), JsonUtility.ToJson(new BuildInfo
        {
            unity = Application.unityVersion,
            version = PlayerSettings.bundleVersion,
            target = report.summary.platform.ToString(),
            options = report.summary.options.ToString(),
            result = report.summary.result.ToString(),
            errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings,
            durationSeconds = clock.Elapsed.TotalSeconds
        }, true));
        Debug.Log($"[WindowsReleaseBuilder] Result=Succeeded; Options={report.summary.options}; Errors={report.summary.totalErrors}; Warnings={report.summary.totalWarnings}; DurationSeconds={clock.Elapsed.TotalSeconds:F2}");
    }

    [Serializable]
    private sealed class BuildInfo
    {
        public string unity;
        public string version;
        public string target;
        public string options;
        public string result;
        public long errors;
        public long warnings;
        public double durationSeconds;
    }
}
