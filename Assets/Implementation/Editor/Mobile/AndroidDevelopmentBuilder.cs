using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class AndroidDevelopmentBuilder
{
    private const string MenuPath = "Tools/Squid Ink Pulse/Build Android Development APK";
    private const string LogPrefix = "[AndroidDevelopmentBuilder]";

    [MenuItem(MenuPath)]
    public static void BuildAndroidDevelopmentApk()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputPath = Path.GetFullPath(Path.Combine(
            projectRoot,
            AndroidDevelopmentBuildRules.DefaultOutputRelativePath));

        if (!AndroidDevelopmentBuildRules.IsOutputInsideBuildDirectory(projectRoot, outputPath))
        {
            throw new BuildFailedException(
                $"La salida Android debe permanecer bajo Build/: {AndroidDevelopmentBuildRules.DefaultOutputRelativePath}");
        }

        string androidSupportError = AndroidDevelopmentBuildRules.GetAndroidSupportError(
            BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android));
        if (androidSupportError != null)
        {
            throw new BuildFailedException(androidSupportError);
        }

        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        string[] missingScenes = AndroidDevelopmentBuildRules.FindMissingRequiredScenes(enabledScenes);
        if (missingScenes.Length > 0)
        {
            throw new BuildFailedException(
                $"Faltan escenas obligatorias habilitadas: {string.Join(", ", missingScenes)}.");
        }

        BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
        bool switchedTarget = false;
        if (originalTarget != BuildTarget.Android)
        {
            if (Application.isBatchMode)
            {
                throw new BuildFailedException(
                    "El build batch debe iniciarse con -buildTarget Android.");
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                throw new BuildFailedException("Unity no pudo activar el target Android.");
            }

            switchedTarget = true;
        }

        bool originalBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool originalExportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new BuildFailedException("No se pudo resolver la carpeta de salida Android.");
            }

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);

            string commit = ResolveGitCommit(projectRoot);
            Debug.Log($"{LogPrefix} Unity={Application.unityVersion}");
            Debug.Log($"{LogPrefix} Commit={commit}");
            Debug.Log($"{LogPrefix} Target=Android; Configuration=Development; Artifact=APK");
            Debug.Log($"{LogPrefix} Output={AndroidDevelopmentBuildRules.DefaultOutputRelativePath}");
            Debug.Log($"{LogPrefix} Scenes={string.Join(", ", enabledScenes)}");

            BuildPlayerOptions options = new()
            {
                scenes = enabledScenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            stopwatch.Stop();
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Build Android finalizado con resultado {report.summary.result}; " +
                    $"errores={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; " +
                    $"duracion={stopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)}s.");
            }

            long artifactSize = new FileInfo(outputPath).Length;
            Debug.Log(
                $"{LogPrefix} Result=Succeeded; " +
                $"DurationSeconds={stopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)}; " +
                $"SizeBytes={artifactSize}; SizeMiB={(artifactSize / 1048576d).ToString("F2", CultureInfo.InvariantCulture)}; " +
                $"Warnings={report.summary.totalWarnings}; Errors={report.summary.totalErrors}");
        }
        finally
        {
            stopwatch.Stop();
            EditorUserBuildSettings.buildAppBundle = originalBuildAppBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = originalExportProject;

            if (switchedTarget &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildPipeline.GetBuildTargetGroup(originalTarget),
                    originalTarget))
            {
                Debug.LogWarning($"{LogPrefix} No se pudo restaurar el target {originalTarget}.");
            }
        }
    }

    private static string ResolveGitCommit(string projectRoot)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process process = Process.Start(startInfo);
            if (process == null)
            {
                return "unavailable";
            }

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output)
                ? output
                : "unavailable";
        }
        catch (Exception)
        {
            return "unavailable";
        }
    }
}
