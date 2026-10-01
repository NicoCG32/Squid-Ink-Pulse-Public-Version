using System;
using System.Linq;
using UnityEditor;

public static class MobileGameRootCatalog
{
    public const string CanonicalGameRootPath =
        "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab";
    public const string AbyssopelagicGameRootPath =
        "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab";
    public const string TutorialGameRootPath =
        "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaTutorial.prefab";

    public static string[] ProductGameRootPaths => AssetDatabase
        .FindAssets("GameRoot_Zona t:Prefab", new[] { "Assets/Content/Prefabs/Core/Scenes" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(IsProductGameRootPath)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    private static bool IsProductGameRootPath(string path)
    {
        string fileName = path.Split('/').LastOrDefault() ?? string.Empty;
        return !string.Equals(path, TutorialGameRootPath, StringComparison.Ordinal)
            && fileName.StartsWith("GameRoot_Zona", StringComparison.Ordinal)
            && fileName.EndsWith(".prefab", StringComparison.Ordinal);
    }
}
