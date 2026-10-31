using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PufferfishTextureTrial
{
    private const string Folder = "Assets/Content/Animations/Enemies/PezGlobo";

    [MenuItem("Tools/Squid Ink Pulse/Probe Pufferfish Android ASTC (Restores Importer)")]
    public static void Probe()
    {
        RequireAndroid();
        string path = Folder + "/47.png";
        byte[] original = File.ReadAllBytes(path + ".meta");
        string contract = SpriteContract(path);
        var result = new Trial { label = "probe", before = new[] { Observe(path) } };
        try
        {
            SetAstc(path);
            result.after = new[] { Observe(path) };
            result.spriteContractsUnchanged = SpriteContract(path) == contract;
            result.allAstc4x4 = result.after.All(row => row.format == "ASTC_4x4");
        }
        finally
        {
            File.WriteAllBytes(path + ".meta", original);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            result.originalMetaRestored = File.ReadAllBytes(path + ".meta").SequenceEqual(original);
            Write(result);
        }
        if (!result.spriteContractsUnchanged || !result.allAstc4x4 || !result.originalMetaRestored)
            throw new InvalidOperationException("ASTC probe failed; inspect the report before applying a family trial.");
    }

    [MenuItem("Tools/Squid Ink Pulse/Apply Pufferfish Android ASTC4x4 Trial")]
    public static void Apply()
    {
        RequireAndroid();
        string[] paths = Directory.GetFiles(Folder, "*.png").Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (paths.Length != 47) throw new InvalidOperationException("Expected 47 pufferfish source textures; review changed content first.");
        var originals = paths.ToDictionary(path => path, path => File.ReadAllBytes(path + ".meta"));
        var contracts = paths.ToDictionary(path => path, SpriteContract);
        var sources = paths.ToDictionary(path => path, Hash);
        var result = new Trial { label = "family", before = paths.Select(Observe).ToArray() };
        try
        {
            foreach (string path in paths) SetAstc(path);
            result.after = paths.Select(Observe).ToArray();
            result.spriteContractsUnchanged = paths.All(path => SpriteContract(path) == contracts[path] && Hash(path) == sources[path]);
            result.allAstc4x4 = result.after.All(row => row.format == "ASTC_4x4");
            if (!result.spriteContractsUnchanged || !result.allAstc4x4)
                throw new InvalidOperationException("Texture format or sprite contract changed unexpectedly.");
            Write(result);
        }
        catch
        {
            foreach (string path in paths)
            {
                File.WriteAllBytes(path + ".meta", originals[path]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            throw;
        }
    }

    private static void SetAstc(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = importer.GetPlatformTextureSettings("Android");
        settings.maxTextureSize = (settings.overridden ? settings : importer.GetDefaultPlatformTextureSettings()).maxTextureSize;
        settings.name = "Android";
        settings.overridden = true;
        settings.format = TextureImporterFormat.ASTC_4x4;
        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void RequireAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Use the Android target; Windows import settings must remain unchanged.");
    }

    private static string SpriteContract(string path)
    {
        // Compare sprite layout/identity and default/Standalone settings across the Android-only format change.
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var parts = new List<string>
        {
            AssetDatabase.AssetPathToGUID(path), importer.spritePixelsPerUnit.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            PlatformContract(importer.GetDefaultPlatformTextureSettings()),
            PlatformContract(importer.GetPlatformTextureSettings("Standalone"))
        };
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal))
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id);
            parts.Add(JsonUtility.ToJson(new SpriteRow
            {
                name = sprite.name, guid = guid, localId = id, rect = sprite.rect,
                pivot = sprite.pivot, bounds = sprite.bounds, pixelsPerUnit = sprite.pixelsPerUnit,
                vertices = sprite.vertices, triangles = sprite.triangles
            }));
        }
        return string.Join("\n", parts);
    }

    private static TextureRow Observe(string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return new TextureRow { path = path, width = texture.width, height = texture.height, format = texture.format.ToString() };
    }

    private static string PlatformContract(TextureImporterPlatformSettings value) => string.Join("|",
        value.name, value.overridden, value.maxTextureSize, value.resizeAlgorithm, value.format,
        value.textureCompression, value.compressionQuality, value.crunchedCompression,
        value.allowsAlphaSplitting, value.androidETC2FallbackOverride, value.ignorePlatformSupport);

    private static string Hash(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    private static void Write(Trial result)
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults", "AndroidTextureTrial"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, result.label + "-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
        Debug.Log($"[PufferfishTextureTrial] ASTC={result.allAstc4x4}; Contracts={result.spriteContractsUnchanged}; Report={path}");
    }

    [Serializable] private sealed class Trial
    {
        public string label;
        public bool allAstc4x4, spriteContractsUnchanged, originalMetaRestored;
        public TextureRow[] before, after;
    }
    [Serializable] private sealed class TextureRow { public string path, format; public int width, height; }
    [Serializable] private sealed class SpriteRow
    {
        public string name, guid;
        public long localId;
        public Rect rect;
        public Vector2 pivot;
        public Bounds bounds;
        public float pixelsPerUnit;
        public Vector2[] vertices;
        public ushort[] triangles;
    }
}
