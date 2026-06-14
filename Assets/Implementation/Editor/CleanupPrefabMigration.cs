using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CleanupPrefabMigration
{
    private const string PrefabFolder = "Assets/Content/Prefabs/World";
    private const string CleanupPrefabPath = PrefabFolder + "/CleanUp.prefab";
    private const string DestroyZoneTag = "DestroyZone";

    private static readonly string[] GameplayScenePaths =
    {
        "Assets/Scenes/Game/ZonaEpipelagica.unity",
        "Assets/Scenes/Game/ZonaAbisopelagica.unity",
        "Assets/Scenes/Game/ZonaTutorial.unity"
    };

    [MenuItem("Tools/Squid/Migrate Cleanup To Prefab")]
    public static void MigrateCleanupToPrefab()
    {
        EnsureCleanupPrefab();

        foreach (string scenePath in GameplayScenePaths)
        {
            MigrateScene(scenePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CleanupPrefabMigration] Cleanup prefab created and connected in gameplay scenes.");
    }

    private static void EnsureCleanupPrefab()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CleanupPrefabPath) != null)
        {
            return;
        }

        GameObject cleanupRoot = new("CleanUp");
        GameObject destroyZone = new("DestroyZone");
        GameObject garbageCollector = new("GarbageCollector");

        destroyZone.transform.SetParent(cleanupRoot.transform, worldPositionStays: false);
        garbageCollector.transform.SetParent(destroyZone.transform, worldPositionStays: false);

        cleanupRoot.layer = 0;
        int destroyZoneLayer = LayerMask.NameToLayer(DestroyZoneTag);
        if (destroyZoneLayer >= 0)
        {
            destroyZone.layer = destroyZoneLayer;
            garbageCollector.layer = destroyZoneLayer;
        }

        destroyZone.tag = DestroyZoneTag;

        BoxCollider2D collider = garbageCollector.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        Rigidbody2D body = garbageCollector.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;

        garbageCollector.AddComponent<DestroyOffscreen>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cleanupRoot, CleanupPrefabPath);
        UnityEngine.Object.DestroyImmediate(cleanupRoot);

        if (prefab == null)
        {
            throw new InvalidOperationException($"Could not create cleanup prefab at {CleanupPrefabPath}.");
        }
    }

    private static void MigrateScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CleanupPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Missing cleanup prefab at {CleanupPrefabPath}.");
        }

        Transform gameplay = RequireSceneTransform(scene, "GameRoot/Gameplay");
        Transform existingCleanup = FindDirectChild(gameplay, "CleanUp");
        if (existingCleanup == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} is missing GameRoot/Gameplay/CleanUp.");
        }

        if (IsInstanceOfPrefab(existingCleanup.gameObject, CleanupPrefabPath))
        {
            NormalizeCleanupTransform(existingCleanup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        TransformSnapshot snapshot = TransformSnapshot.Capture(existingCleanup);
        UnityEngine.Object.DestroyImmediate(existingCleanup.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (instance == null)
        {
            throw new InvalidOperationException($"Could not instantiate {CleanupPrefabPath} in {scene.path}.");
        }

        snapshot.Apply(instance.transform);
        NormalizeCleanupTransform(instance.transform);
        EditorUtility.SetDirty(instance);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Could not save scene {scene.path}.");
        }
    }

    private static void NormalizeCleanupTransform(Transform cleanup)
    {
        cleanup.localPosition = Vector3.zero;
        cleanup.localRotation = Quaternion.identity;
        cleanup.localScale = Vector3.one;
        EditorUtility.SetDirty(cleanup);
    }

    private static bool IsInstanceOfPrefab(GameObject sceneObject, string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject);
        return prefabRoot != null && AssetDatabase.GetAssetPath(prefabRoot) == prefabPath;
    }

    private static Transform RequireSceneTransform(Scene scene, string path)
    {
        Transform target = FindSceneTransform(scene, path);
        if (target == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} is missing required transform path {path}.");
        }

        return target;
    }

    private static Transform FindSceneTransform(Scene scene, string path)
    {
        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != segments[0])
            {
                continue;
            }

            Transform current = root.transform;
            for (int i = 1; i < segments.Length; i++)
            {
                current = FindDirectChild(current, segments[i]);
                if (current == null)
                {
                    break;
                }
            }

            if (current != null)
            {
                return current;
            }
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private readonly struct TransformSnapshot
    {
        private readonly Transform parent;
        private readonly int siblingIndex;
        private readonly bool activeSelf;

        private TransformSnapshot(Transform transform)
        {
            parent = transform.parent;
            siblingIndex = transform.GetSiblingIndex();
            activeSelf = transform.gameObject.activeSelf;
        }

        public static TransformSnapshot Capture(Transform transform)
        {
            return new TransformSnapshot(transform);
        }

        public void Apply(Transform transform)
        {
            transform.SetParent(parent, worldPositionStays: false);
            transform.SetSiblingIndex(siblingIndex);
            transform.gameObject.SetActive(activeSelf);
        }
    }
}
