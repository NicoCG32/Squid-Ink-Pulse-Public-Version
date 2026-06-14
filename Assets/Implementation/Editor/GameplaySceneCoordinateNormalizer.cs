using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplaySceneCoordinateNormalizer
{
    private static readonly string[] GameplayScenePaths =
    {
        "Assets/Scenes/Game/ZonaEpipelagica.unity",
        "Assets/Scenes/Game/ZonaAbisopelagica.unity",
        "Assets/Scenes/Game/ZonaTutorial.unity"
    };

    [MenuItem("Tools/Squid/Normalize Gameplay Scene Coordinates")]
    public static void NormalizeGameplaySceneCoordinates()
    {
        foreach (string scenePath in GameplayScenePaths)
        {
            NormalizeScene(scenePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameplaySceneCoordinateNormalizer] Gameplay scene coordinates normalized around the origin.");
    }

    private static void NormalizeScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Transform mainCamera = RequireSceneTransform(scene, "CameraRig/Main Camera");
        Vector3 cameraAnchor = mainCamera.position;

        NormalizeWorldTransform(mainCamera, cameraAnchor);
        NormalizeWorldTransform(RequireSceneTransform(scene, "GameRoot/Player/Squid"), cameraAnchor);
        NormalizeWorldTransform(RequireSceneTransform(scene, "GameRoot/Gameplay/Boundaries"), cameraAnchor);
        NormalizeOptionalWorldTransform(scene, "Enviroment/Global Light 2D", cameraAnchor);
        NormalizeOptionalWorldTransform(scene, "Audio/Soundtrack", cameraAnchor);
        NormalizeBackground(scene, cameraAnchor);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Could not save normalized scene {scenePath}.");
        }
    }

    private static void NormalizeWorldTransform(Transform target, Vector3 cameraAnchor)
    {
        Vector3 position = target.position - new Vector3(cameraAnchor.x, cameraAnchor.y, 0f);
        target.position = position;
        EditorUtility.SetDirty(target);
    }

    private static void NormalizeOptionalWorldTransform(Scene scene, string path, Vector3 cameraAnchor)
    {
        Transform target = FindSceneTransform(scene, path);
        if (target != null)
        {
            NormalizeWorldTransform(target, cameraAnchor);
        }
    }

    private static void NormalizeBackground(Scene scene, Vector3 cameraAnchor)
    {
        Transform background = FindSceneTransform(scene, "Enviroment/Background");
        if (background == null)
        {
            return;
        }

        List<TransformSnapshot> childSnapshots = new();
        for (int i = 0; i < background.childCount; i++)
        {
            Transform child = background.GetChild(i);
            childSnapshots.Add(new TransformSnapshot(
                child,
                child.position - new Vector3(cameraAnchor.x, cameraAnchor.y, 0f)));
        }

        background.localPosition = Vector3.zero;
        EditorUtility.SetDirty(background);

        foreach (TransformSnapshot snapshot in childSnapshots)
        {
            snapshot.Apply();
        }
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
        private readonly Transform transform;
        private readonly Vector3 normalizedPosition;

        public TransformSnapshot(Transform transform, Vector3 normalizedPosition)
        {
            this.transform = transform;
            this.normalizedPosition = normalizedPosition;
        }

        public void Apply()
        {
            transform.position = normalizedPosition;
            EditorUtility.SetDirty(transform);
        }
    }
}
