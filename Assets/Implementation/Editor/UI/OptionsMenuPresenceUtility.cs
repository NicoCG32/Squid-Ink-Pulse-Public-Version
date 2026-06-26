using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OptionsMenuPresenceUtility
{
    private const string OptionsMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab";

    private static readonly string[] RequiredScenePaths =
    {
        "Assets/Scenes/MainMenu/MainMenu.unity",
        "Assets/Scenes/ShopMenu/ShopMenu.unity",
        "Assets/Scenes/Game/ZonaTutorial.unity",
        "Assets/Scenes/Game/ZonaEpipelagica.unity",
        "Assets/Scenes/Game/ZonaAbisopelagica.unity"
    };

    [MenuItem("Tools/Squid/UI/Install Missing Global Options Menus")]
    public static void InstallMissingGlobalOptionsMenus()
    {
        int installedCount = 0;
        foreach (string scenePath in RequiredScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!EnsureSingleOptionsMenu(scene))
            {
                continue;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            installedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[OptionsMenuPresenceUtility] OptionsMenu instalado en {installedCount} escena(s). Las instancias existentes no fueron modificadas.");
    }

    /// <summary>
    /// Ensures only the presence of the authored prefab. It deliberately does not change any visual
    /// hierarchy, layout, scale, sprites, colors, or prefab overrides.
    /// </summary>
    public static bool EnsureSingleOptionsMenu(Scene scene)
    {
        OptionsMenuManager[] existing = FindSceneComponents<OptionsMenuManager>(scene).ToArray();
        if (existing.Length > 1)
        {
            throw new InvalidOperationException($"[OptionsMenuPresenceUtility] {scene.path} tiene {existing.Length} OptionsMenuManager. Debe conservar exactamente uno.");
        }

        if (existing.Length == 1)
        {
            return false;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptionsMenuPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"[OptionsMenuPresenceUtility] No existe {OptionsMenuPrefabPath}.");
        }

        PrefabUtility.InstantiatePrefab(prefab, scene);
        return true;
    }

    private static System.Collections.Generic.IEnumerable<T> FindSceneComponents<T>(Scene scene)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(includeInactive: true))
            {
                if (component != null)
                {
                    yield return component;
                }
            }
        }
    }
}
