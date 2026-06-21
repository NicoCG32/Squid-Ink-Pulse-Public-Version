using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OptionsMenuLayoutMigration
{
    private const string OptionsMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab";
    private const string OptionsMenuRootName = "OptionsMenu";
    private const string CanvasChildName = "Canvas";

    private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

    [MenuItem("Tools/Squid/UI/Normalize Options Menu Layout")]
    public static void NormalizeOptionsMenuLayout()
    {
        int changedAssets = 0;

        try
        {
            changedAssets += NormalizeOptionsMenuPrefab() ? 1 : 0;
            changedAssets += NormalizeSceneInstances();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[OptionsMenuLayoutMigration] Options menu layout normalized. Changed assets: {changedAssets}.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[OptionsMenuLayoutMigration] Normalization failed: {exception}");
            throw;
        }
    }

    public static void NormalizeOptionsMenuLayoutBatch()
    {
        NormalizeOptionsMenuLayout();
    }

    public static void ReportOptionsMenuLayoutsBatch()
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform optionsRoot in root.GetComponentsInChildren<Transform>(includeInactive: true)
                             .Where(transform => transform.name == OptionsMenuRootName))
                {
                    Transform canvasTransform = optionsRoot.Find(CanvasChildName);
                    RectTransform canvasRect = canvasTransform != null
                        ? canvasTransform.GetComponent<RectTransform>()
                        : null;
                    CanvasScaler canvasScaler = canvasTransform != null
                        ? canvasTransform.GetComponent<CanvasScaler>()
                        : null;

                    Debug.Log(
                        "[OptionsMenuLayoutMigration] "
                        + $"{scenePath}/{GetPath(optionsRoot)} "
                        + $"rootPosition={optionsRoot.localPosition} "
                        + $"canvasScale={(canvasRect != null ? canvasRect.localScale.ToString() : "<missing>")} "
                        + $"canvasAnchors={(canvasRect != null ? $"{canvasRect.anchorMin}->{canvasRect.anchorMax}" : "<missing>")} "
                        + $"canvasPivot={(canvasRect != null ? canvasRect.pivot.ToString() : "<missing>")} "
                        + $"match={(canvasScaler != null ? canvasScaler.matchWidthOrHeight.ToString() : "<missing>")} "
                        + $"ppu={(canvasScaler != null ? canvasScaler.referencePixelsPerUnit.ToString() : "<missing>")}");
                }
            }
        }
    }

    private static bool NormalizeOptionsMenuPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(OptionsMenuPrefabPath);
        try
        {
            bool changed = NormalizeOptionsMenuRoot(prefabRoot.transform);
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, OptionsMenuPrefabPath);
            }

            return changed;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static int NormalizeSceneInstances()
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        int changedScenes = 0;
        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                changed |= NormalizeOptionsMenusInHierarchy(root.transform);
            }

            if (!changed)
            {
                continue;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            changedScenes++;
        }

        return changedScenes;
    }

    private static bool NormalizeOptionsMenusInHierarchy(Transform hierarchyRoot)
    {
        HashSet<Transform> optionsRoots = new();
        if (hierarchyRoot.name == OptionsMenuRootName)
        {
            optionsRoots.Add(hierarchyRoot);
        }

        foreach (OptionsMenuManager manager in hierarchyRoot.GetComponentsInChildren<OptionsMenuManager>(includeInactive: true))
        {
            Transform optionsRoot = FindOptionsMenuRoot(manager.transform);
            if (optionsRoot != null)
            {
                optionsRoots.Add(optionsRoot);
            }
        }

        foreach (Transform namedRoot in hierarchyRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                     .Where(transform => transform.name == OptionsMenuRootName))
        {
            optionsRoots.Add(namedRoot);
        }

        bool changed = false;
        foreach (Transform optionsRoot in optionsRoots)
        {
            changed |= NormalizeOptionsMenuRoot(optionsRoot);
        }

        return changed;
    }

    private static Transform FindOptionsMenuRoot(Transform source)
    {
        Transform current = source;
        while (current != null)
        {
            if (current.name == OptionsMenuRootName && current.Find(CanvasChildName) != null)
            {
                return current;
            }

            current = current.parent;
        }

        Canvas parentCanvas = source.GetComponentInParent<Canvas>(includeInactive: true);
        Transform canvasParent = parentCanvas != null ? parentCanvas.transform.parent : null;
        return canvasParent != null && canvasParent.name == OptionsMenuRootName
            ? canvasParent
            : null;
    }

    private static bool NormalizeOptionsMenuRoot(Transform optionsRoot)
    {
        bool changed = false;
        changed |= RevertPrefabInstanceObjectOverride(optionsRoot);
        changed |= SetTransformIdentity(optionsRoot);

        Transform canvasTransform = optionsRoot.Find(CanvasChildName);
        if (canvasTransform == null)
        {
            Debug.LogWarning($"[OptionsMenuLayoutMigration] '{GetPath(optionsRoot)}' has no '{CanvasChildName}' child.");
            return changed;
        }

        RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning($"[OptionsMenuLayoutMigration] '{GetPath(canvasTransform)}' has no RectTransform.");
            return changed;
        }

        changed |= RevertPrefabInstanceObjectOverride(canvasRect);
        changed |= NormalizeCanvasRect(canvasRect);

        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas != null)
        {
            changed |= NormalizeCanvas(canvas);
        }

        CanvasScaler canvasScaler = canvasTransform.GetComponent<CanvasScaler>();
        if (canvasScaler != null)
        {
            changed |= RevertPrefabInstanceObjectOverride(canvasScaler);
            changed |= NormalizeCanvasScaler(canvasScaler);
        }

        return changed;
    }

    private static bool RevertPrefabInstanceObjectOverride(UnityEngine.Object target)
    {
        if (target == null || !PrefabUtility.IsPartOfPrefabInstance(target))
        {
            return false;
        }

        PrefabUtility.RevertObjectOverride(target, InteractionMode.AutomatedAction);
        return true;
    }

    private static bool SetTransformIdentity(Transform transform)
    {
        bool changed = false;
        changed |= SetVector3(() => transform.localPosition, value => transform.localPosition = value, Vector3.zero);
        changed |= SetQuaternion(() => transform.localRotation, value => transform.localRotation = value, Quaternion.identity);
        changed |= SetVector3(() => transform.localScale, value => transform.localScale = value, Vector3.one);
        return changed;
    }

    private static bool NormalizeCanvasRect(RectTransform rectTransform)
    {
        bool changed = false;
        changed |= SetVector2(() => rectTransform.anchorMin, value => rectTransform.anchorMin = value, Vector2.zero);
        changed |= SetVector2(() => rectTransform.anchorMax, value => rectTransform.anchorMax = value, Vector2.one);
        changed |= SetVector2(() => rectTransform.offsetMin, value => rectTransform.offsetMin = value, Vector2.zero);
        changed |= SetVector2(() => rectTransform.offsetMax, value => rectTransform.offsetMax = value, Vector2.zero);
        changed |= SetVector2(() => rectTransform.pivot, value => rectTransform.pivot = value, new Vector2(0.5f, 0.5f));
        changed |= SetVector3(() => rectTransform.localPosition, value => rectTransform.localPosition = value, Vector3.zero);
        changed |= SetQuaternion(() => rectTransform.localRotation, value => rectTransform.localRotation = value, Quaternion.identity);
        changed |= SetVector3(() => rectTransform.localScale, value => rectTransform.localScale = value, Vector3.one);
        return changed;
    }

    private static bool NormalizeCanvas(Canvas canvas)
    {
        bool changed = false;
        changed |= SetValue(() => canvas.renderMode, value => canvas.renderMode = value, RenderMode.ScreenSpaceOverlay);
        changed |= SetValue(() => canvas.overrideSorting, value => canvas.overrideSorting = value, true);
        changed |= SetValue(() => canvas.sortingOrder, value => canvas.sortingOrder = value, 100);
        return changed;
    }

    private static bool NormalizeCanvasScaler(CanvasScaler canvasScaler)
    {
        bool changed = false;
        changed |= SetValue(() => canvasScaler.uiScaleMode, value => canvasScaler.uiScaleMode = value, CanvasScaler.ScaleMode.ScaleWithScreenSize);
        changed |= SetVector2(() => canvasScaler.referenceResolution, value => canvasScaler.referenceResolution = value, ReferenceResolution);
        changed |= SetValue(() => canvasScaler.screenMatchMode, value => canvasScaler.screenMatchMode = value, CanvasScaler.ScreenMatchMode.MatchWidthOrHeight);
        changed |= SetFloat(() => canvasScaler.matchWidthOrHeight, value => canvasScaler.matchWidthOrHeight = value, 0.5f);
        changed |= SetFloat(() => canvasScaler.referencePixelsPerUnit, value => canvasScaler.referencePixelsPerUnit = value, 100f);
        return changed;
    }

    private static bool SetValue<T>(Func<T> getter, Action<T> setter, T expected)
    {
        if (EqualityComparer<T>.Default.Equals(getter(), expected))
        {
            return false;
        }

        setter(expected);
        return true;
    }

    private static bool SetFloat(Func<float> getter, Action<float> setter, float expected)
    {
        if (Mathf.Approximately(getter(), expected))
        {
            return false;
        }

        setter(expected);
        return true;
    }

    private static bool SetVector2(Func<Vector2> getter, Action<Vector2> setter, Vector2 expected)
    {
        if (Approximately(getter(), expected))
        {
            return false;
        }

        setter(expected);
        return true;
    }

    private static bool SetVector3(Func<Vector3> getter, Action<Vector3> setter, Vector3 expected)
    {
        if (Approximately(getter(), expected))
        {
            return false;
        }

        setter(expected);
        return true;
    }

    private static bool SetQuaternion(Func<Quaternion> getter, Action<Quaternion> setter, Quaternion expected)
    {
        if (Approximately(getter(), expected))
        {
            return false;
        }

        setter(expected);
        return true;
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x)
            && Mathf.Approximately(left.y, right.y);
    }

    private static bool Approximately(Vector3 left, Vector3 right)
    {
        return Mathf.Approximately(left.x, right.x)
            && Mathf.Approximately(left.y, right.y)
            && Mathf.Approximately(left.z, right.z);
    }

    private static bool Approximately(Quaternion left, Quaternion right)
    {
        return Mathf.Approximately(left.x, right.x)
            && Mathf.Approximately(left.y, right.y)
            && Mathf.Approximately(left.z, right.z)
            && Mathf.Approximately(left.w, right.w);
    }

    private static string GetPath(Transform transform)
    {
        Stack<string> path = new();
        Transform current = transform;
        while (current != null)
        {
            path.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", path);
    }
}
