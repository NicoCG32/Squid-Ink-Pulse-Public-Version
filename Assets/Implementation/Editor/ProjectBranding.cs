using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ProjectBranding
{
    private const string Logo = "Assets/Content/Art/UI/Brand/Logo.png";
    private const string Icon = "Assets/Content/Art/UI/Brand/AppIcon.png";

    [MenuItem("Squid Ink-Pulse/Identity/Apply visual identity")]
    public static void Apply()
    {
        Import(Logo, true);
        Import(Icon, false);
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Icon);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu/MainMenu.unity");
        var image = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Image>(true)).Single(x => x.name == "Logo");
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Logo);
        image.preserveAspect = true;
        EditorUtility.SetDirty(image);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Identity] Squid Ink-Pulse: menu logo and default player icon saved; gameplay sprites preserved.");
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
