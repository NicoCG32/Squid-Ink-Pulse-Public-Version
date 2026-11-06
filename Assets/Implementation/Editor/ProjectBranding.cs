using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ProjectBranding
{
    private const string Logo = "Assets/Content/Art/UI/Brand/Logo.png";
    private const string Icon = "Assets/Content/Art/UI/Brand/AppIcon.png";
    private const string AndroidForeground = "Assets/Content/Art/UI/Brand/AndroidIconForeground.png";
    private const string AndroidBackground = "Assets/Content/Art/UI/Brand/AndroidIconBackground.png";

    [MenuItem("Squid Ink-Pulse/Identity/Apply visual identity")]
    public static void Apply()
    {
        Import(Logo, true);
        ApplyPlayerIcons();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu/MainMenu.unity");
        var image = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Image>(true)).Single(x => x.name == "Logo");
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Logo);
        image.preserveAspect = true;
        EditorUtility.SetDirty(image);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Identity] Squid Ink-Pulse: menu logo and player icons saved; gameplay sprites preserved.");
    }

    [MenuItem("Squid Ink-Pulse/Identity/Apply Android and Windows icons")]
    public static void ApplyPlayerIcons()
    {
        var icon = LoadIcon(Icon);
        var foreground = LoadIcon(AndroidForeground);
        var background = LoadIcon(AndroidBackground);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        int[] windowsSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
        if (windowsSizes.Length == 0)
            throw new InvalidOperationException("Windows icon slots are unavailable; install Windows Build Support.");
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone,
            Enumerable.Repeat(icon, windowsSizes.Length).ToArray(), IconKind.Any);

        // Keep explicit compatibility icons for the minimum Android API 25 target.
#pragma warning disable CS0618
        ApplyAndroidIcons(AndroidPlatformIconKind.Legacy, icon);
        ApplyAndroidIcons(AndroidPlatformIconKind.Round, icon);
#pragma warning restore CS0618
        // Unity orders adaptive layers as background, then foreground.
        ApplyAndroidIcons(AndroidPlatformIconKind.Adaptive, background, foreground);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Identity] Android legacy/round/adaptive and Windows ({string.Join(",", windowsSizes)}) icons saved.");
    }

    private static Texture2D LoadIcon(string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new InvalidOperationException($"Missing player icon: {path}");
        return texture;
    }

    private static void ApplyAndroidIcons(PlatformIconKind kind, params Texture2D[] layers)
    {
        var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        if (icons.Length == 0)
            throw new InvalidOperationException($"Android icon slots are unavailable: {kind}");
        foreach (var slot in icons)
            slot.SetTextures(layers);
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
    }

    private static void Import(string path, bool sprite)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
