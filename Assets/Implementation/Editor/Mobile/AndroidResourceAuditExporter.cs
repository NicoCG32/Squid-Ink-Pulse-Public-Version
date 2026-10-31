using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Reads an existing build report and current importer settings; never reimports or edits assets.
public static class AndroidResourceAuditExporter
{
    [MenuItem("Tools/Squid Ink Pulse/Export Last Android Resource Audit")]
    public static void ExportLatest()
    {
        BuildReport report = BuildReport.GetLatestReport();
        if (report == null || report.summary.platform != BuildTarget.Android || report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("The last successful build report must be Android.");

        string guid = report.summary.guid.ToString();
        string expectedGuid = Argument("--sip-audit-build-guid");
        if (!string.IsNullOrEmpty(expectedGuid) && !string.Equals(guid, expectedGuid, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Last build GUID differs from the expected artifact.");

        string label = Argument("--sip-audit-label") ?? guid;
        if (!Regex.IsMatch(label, "^[a-z0-9-]+$")) throw new ArgumentException("Invalid audit label.");
        string sourceCommit = Argument("--sip-audit-source-commit") ?? "unrecorded";
        if (sourceCommit != "unrecorded" && !Regex.IsMatch(sourceCommit, "^[a-f0-9]{7,40}$"))
            throw new ArgumentException("Invalid build source commit.");
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(root, "TestResults", "AndroidResourceAudit", label + ".json");
        if (File.Exists(output)) throw new IOException("Keep the previous audit; use another label.");
        string apk = Path.GetFullPath(report.summary.outputPath);
        if (!File.Exists(apk) || !apk.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("The reported APK must still exist.");
        string apkHash = Hash(apk);
        string expectedHash = Argument("--sip-audit-apk-sha256");
        if (!string.IsNullOrEmpty(expectedHash) && !string.Equals(apkHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("APK checksum differs from the expected artifact.");

        var assets = new Dictionary<string, AssetRow>(StringComparer.Ordinal);
        long total = 0;
        long overhead = 0;
        foreach (PackedAssets container in report.packedAssets)
        {
            overhead = checked(overhead + (long)container.overhead);
            foreach (PackedAssetInfo item in container.contents)
            {
                string path = string.IsNullOrEmpty(item.sourceAssetPath) ? "<unattributed>" : item.sourceAssetPath;
                if (!assets.TryGetValue(path, out AssetRow row))
                {
                    row = new AssetRow { path = path };
                    assets.Add(path, row);
                }
                row.packedBytes = checked(row.packedBytes + (long)item.packedSize);
                row.objectCount++;
                total = checked(total + (long)item.packedSize);
            }
        }
        if (assets.Count == 0) throw new InvalidOperationException("The report has no packed-asset data; do not substitute source-file sizes.");

        foreach (AssetRow row in assets.Values)
        {
            if (!row.path.StartsWith("Assets/", StringComparison.Ordinal) && !row.path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                row.kind = "BuiltInOrUnattributed";
                continue;
            }
            if (File.Exists(Path.Combine(root, row.path))) row.sourceFileBytes = new FileInfo(Path.Combine(root, row.path)).Length;
            AssetImporter importer = AssetImporter.GetAtPath(row.path);
            if (importer is TextureImporter texture)
            {
                row.kind = "Texture";
                TextureImporterPlatformSettings defaults = texture.GetDefaultPlatformTextureSettings();
                TextureImporterPlatformSettings android = texture.GetPlatformTextureSettings("Android");
                TextureImporterPlatformSettings effective = android.overridden ? android : defaults;
                texture.GetSourceTextureWidthAndHeight(out int width, out int height);
                row.sourceWidth = width;
                row.sourceHeight = height;
                row.androidOverride = android.overridden;
                row.maxTextureSize = effective.maxTextureSize;
                row.compression = effective.textureCompression.ToString();
                row.resolvedAndroidFormat = (effective.format == TextureImporterFormat.Automatic
                    ? texture.GetAutomaticFormat("Android") : effective.format).ToString();
                row.readable = texture.isReadable;
                row.mipmaps = texture.mipmapEnabled;
            }
            else if (importer is AudioImporter audio)
            {
                row.kind = "Audio";
                row.androidOverride = audio.ContainsSampleSettingsOverride("Android");
                AudioImporterSampleSettings settings = row.androidOverride ? audio.GetOverrideSampleSettings("Android") : audio.defaultSampleSettings;
                row.compression = settings.compressionFormat.ToString();
                row.audioLoadType = settings.loadType.ToString();
                row.audioBackgroundLoad = audio.loadInBackground;
            }
            else row.kind = "Other";
        }

        // Only inspect a small sample. These are Editor observations, never device memory measurements.
        foreach (AssetRow row in assets.Values.Where(row => row.kind == "Texture").OrderByDescending(row => row.packedBytes).Take(3))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(row.path);
            if (texture == null) continue;
            row.editorObservedFormat = texture.format.ToString();
            row.editorObservedWidth = texture.width;
            row.editorObservedHeight = texture.height;
        }

        var result = new Audit
        {
            capturedUtc = DateTime.UtcNow.ToString("o"),
            unity = Application.unityVersion,
            buildGuid = guid,
            reportedSourceCommit = sourceCommit,
            buildOptions = report.summary.options.ToString(),
            currentAndroidSubtarget = EditorUserBuildSettings.androidBuildSubtarget.ToString(),
            currentTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString(),
            currentMaxTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize,
            buildEndedUtc = report.summary.buildEndedAt.ToUniversalTime().ToString("o"),
            apkName = Path.GetFileName(apk),
            apkSha256 = apkHash,
            apkBytes = new FileInfo(apk).Length,
            packedAssetBytes = total,
            serializedHeaderBytes = overhead,
            assets = assets.Values.OrderByDescending(row => row.packedBytes).ThenBy(row => row.path, StringComparer.Ordinal).ToArray(),
            categories = assets.Values.GroupBy(row => row.kind).Select(group => new GroupRow
                { name = group.Key, packedBytes = group.Sum(row => row.packedBytes), assetCount = group.Count() })
                .OrderByDescending(row => row.packedBytes).ToArray(),
            folders = assets.Values.GroupBy(row => row.kind == "BuiltInOrUnattributed" ? "<built-in-or-unattributed>" : Path.GetDirectoryName(row.path)?.Replace('\\', '/'))
                .Select(group => new GroupRow { name = group.Key, packedBytes = group.Sum(row => row.packedBytes), assetCount = group.Count() })
                .OrderByDescending(row => row.packedBytes).ThenBy(row => row.name, StringComparer.Ordinal).ToArray()
        };
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
        Debug.Log($"[AndroidResourceAudit] Build={guid}; APK={apkHash}; Assets={assets.Count}; PackedBytes={total}; Output={output}");
    }

    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        if (index < 0) return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal)) throw new ArgumentException("Missing value: " + name);
        return args[index + 1];
    }

    private static string Hash(string path)
    {
        using SHA256 algorithm = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    [Serializable]
    private sealed class Audit
    {
        public string capturedUtc, unity, buildGuid, reportedSourceCommit, buildOptions, buildEndedUtc, apkName, apkSha256;
        public string currentAndroidSubtarget, currentTextureCompressionOverride;
        public int currentMaxTextureSizeOverride;
        public string limitations = "Packed sizes describe serialized build content, not compressed APK size or resident RAM/VRAM. Scene levels, native libraries and other APK entries are not fully attributed by PackedAssets. Importer settings and global overrides are current Editor settings, not a historical snapshot. Observed texture formats/dimensions are Editor observations, not device readings. No reimport, asset modification, CPU/GPU/GC or FPS measurement.";
        public long apkBytes, packedAssetBytes, serializedHeaderBytes;
        public AssetRow[] assets;
        public GroupRow[] categories, folders;
    }

    [Serializable]
    private sealed class AssetRow
    {
        public string path, kind, compression, resolvedAndroidFormat, audioLoadType, editorObservedFormat;
        public long packedBytes, sourceFileBytes;
        public int objectCount, sourceWidth, sourceHeight, maxTextureSize, editorObservedWidth, editorObservedHeight;
        public bool androidOverride, readable, mipmaps, audioBackgroundLoad;
    }

    [Serializable]
    private sealed class GroupRow
    {
        public string name;
        public long packedBytes;
        public int assetCount;
    }
}
