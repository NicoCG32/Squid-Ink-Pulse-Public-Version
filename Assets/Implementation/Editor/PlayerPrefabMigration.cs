using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerPrefabContractUtility
{
    private const string PrefabPath = "Assets/Content/Prefabs/Player/BabySquid.prefab";
    private const string SourceScenePath = "Assets/Scenes/Game/ZonaEpipelagica.unity";
    private const string FishingRodPrefabPath = "Assets/Content/Prefabs/Enemies/CanaPescar.prefab";

    private static readonly string[] TargetScenePaths =
    {
        "Assets/Scenes/Game/ZonaEpipelagica.unity",
        "Assets/Scenes/Game/ZonaExe.unity",
        "Assets/Scenes/Game/ZonaTutorial.unity"
    };

    [MenuItem("Tools/Squid/Rebuild And Wire Player Prefab Contract")]
    public static void RebuildAndWirePlayerPrefabContract()
    {
        GameObject prefabAsset = RebuildPrefabFromSourceScene();
        foreach (string scenePath in TargetScenePaths)
        {
            RebuildScenePlayerInstance(scenePath, prefabAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] Player prefab contract rebuilt and wired.");
    }

    [MenuItem("Tools/Squid/Wire Player Scene References")]
    public static void WirePlayerSceneReferencesOnly()
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null)
        {
            throw new InvalidOperationException($"Missing player prefab at {PrefabPath}.");
        }

        foreach (string scenePath in TargetScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject player = FindSinglePlayerRoot(scene);
            EnsurePlayerIsPrefabInstance(player, prefabAsset, scenePath);
            ConfigureScenePlayerReferences(scene, player);
            ConfigureSceneManagerReferences(scene, player);
            ConfigureZoneSpecificPlayerOverrides(scene, player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] Player scene references wired.");
    }

    [MenuItem("Tools/Squid/Ensure Enemy Prefab Contracts")]
    public static void EnsureEnemyPrefabContracts()
    {
        EnsureFishingRodEnemyContract();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] Enemy prefab contracts ensured.");
    }

    [MenuItem("Tools/Squid/Ensure Player Visual State Contract")]
    public static void EnsurePlayerVisualStateContract()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            throw new InvalidOperationException($"Missing player prefab at {PrefabPath}.");
        }

        try
        {
            ConfigurePrefabInternalReferences(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        foreach (string scenePath in TargetScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject player = FindSinglePlayerRoot(scene);
            EnsurePlayerIsPrefabInstance(player, prefabAsset, scenePath);
            ConfigureScenePlayerReferences(scene, player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] Player visual state contract ensured.");
    }

    private static void EnsureFishingRodEnemyContract()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(FishingRodPrefabPath);
        if (prefabRoot == null)
        {
            throw new InvalidOperationException($"Missing fishing rod prefab at {FishingRodPrefabPath}.");
        }

        try
        {
            if (prefabRoot.GetComponent<FishingRodEnemy>() == null)
            {
                prefabRoot.AddComponent<FishingRodEnemy>();
            }

            prefabRoot.tag = EnemyTagCatalog.FishingRod;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                ApplyLayerRecursively(prefabRoot, enemyLayer);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, FishingRodPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static GameObject RebuildPrefabFromSourceScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        GameObject sourcePlayer = FindSinglePlayerRoot(scene);
        if (sourcePlayer == null)
        {
            throw new InvalidOperationException($"No player named Squid with tag Player was found in {SourceScenePath}.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject prefabSource = UnityEngine.Object.Instantiate(sourcePlayer);
        prefabSource.name = "BabySquid";
        prefabSource.transform.SetParent(null, worldPositionStays: false);
        prefabSource.transform.localPosition = Vector3.zero;
        prefabSource.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        prefabSource.transform.localScale = Vector3.one;

        ConfigurePrefabIdentity(prefabSource);
        RemoveZoneSpecificComponents(prefabSource);
        ConfigurePrefabInternalReferences(prefabSource);

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource, PrefabPath);
        UnityEngine.Object.DestroyImmediate(prefabSource);

        if (prefabAsset == null)
        {
            throw new InvalidOperationException($"Could not create player prefab at {PrefabPath}.");
        }

        Debug.Log($"[PlayerPrefabContractUtility] Rebuilt prefab: {PrefabPath}");
        return prefabAsset;
    }

    private static void RebuildScenePlayerInstance(string scenePath, GameObject prefabAsset)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject oldPlayer = FindSinglePlayerRoot(scene);
        if (oldPlayer == null)
        {
            throw new InvalidOperationException($"No player named Squid with tag Player was found in {scenePath}.");
        }

        Transform oldTransform = oldPlayer.transform;
        Transform parent = oldTransform.parent;
        int siblingIndex = oldTransform.GetSiblingIndex();
        Vector3 localPosition = oldTransform.localPosition;
        Quaternion localRotation = oldTransform.localRotation;
        Vector3 localScale = oldTransform.localScale;
        bool wasActive = oldPlayer.activeSelf;
        string playerName = oldPlayer.name;

        GameObject newPlayer = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
        newPlayer.name = playerName;
        newPlayer.SetActive(wasActive);
        newPlayer.transform.SetParent(parent, worldPositionStays: false);
        newPlayer.transform.SetSiblingIndex(siblingIndex);
        newPlayer.transform.localPosition = localPosition;
        newPlayer.transform.localRotation = localRotation;
        newPlayer.transform.localScale = localScale;

        CopySceneSpecificComponents(oldPlayer, newPlayer);
        Dictionary<UnityEngine.Object, UnityEngine.Object> objectMap = BuildObjectMap(oldPlayer, newPlayer);
        ReplaceSceneObjectReferences(scene, objectMap);

        UnityEngine.Object.DestroyImmediate(oldPlayer);

        ConfigureScenePlayerReferences(scene, newPlayer);
        ConfigureSceneManagerReferences(scene, newPlayer);
        ConfigureZoneSpecificPlayerOverrides(scene, newPlayer);
        ValidateSceneContract(scene, newPlayer, prefabAsset);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PlayerPrefabContractUtility] Rebuilt scene player instance: {scenePath}");
    }

    private static void ConfigurePrefabIdentity(GameObject playerRoot)
    {
        playerRoot.tag = GameplayTagCatalog.Player;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            ApplyLayerRecursively(playerRoot, playerLayer);
        }
    }

    private static void ConfigurePrefabInternalReferences(GameObject playerRoot)
    {
        InkPulseController inkPulse = playerRoot.GetComponent<InkPulseController>();
        PlayerMovement movement = playerRoot.GetComponent<PlayerMovement>();
        PlayerCollision collision = playerRoot.GetComponent<PlayerCollision>();
        ShrimpCollector shrimpCollector = playerRoot.GetComponent<ShrimpCollector>();
        PlayerStateController state = playerRoot.GetComponent<PlayerStateController>();
        PlayerGadgetInventory inventory = playerRoot.GetComponent<PlayerGadgetInventory>();
        Collider2D playerCollider = playerRoot.GetComponent<Collider2D>();
        GrazeDetector grazeDetector = playerRoot.GetComponentInChildren<GrazeDetector>(includeInactive: true);
        PlayerVisualStateController visualState = EnsurePlayerVisualStateController(playerRoot);

        SetObjectReference(inkPulse, "session", null);
        SetObjectReference(inkPulse, "chargeBar", null);

        SetObjectReference(movement, "session", null);
        SetObjectReference(movement, "progression", null);
        SetObjectReference(movement, "gameplayCamera", null);
        SetObjectReference(movement, "playerCollider", playerCollider);

        SetObjectReference(collision, "session", null);
        SetObjectReference(collision, "inkPulseController", inkPulse);
        SetObjectReference(collision, "shrimpCollector", shrimpCollector);
        SetObjectReference(collision, "gadgetInventory", inventory);
        SetObjectReference(collision, "damageCollider", playerCollider);

        SetObjectReference(shrimpCollector, "session", null);

        SetObjectReference(state, "session", null);
        SetObjectReference(state, "inkPulse", inkPulse);
        SetObjectReference(state, "movement", movement);

        SetObjectReference(inventory, "session", null);
        SetObjectReference(inventory, "inkPulseController", inkPulse);

        SetObjectReference(grazeDetector, "session", null);
        SetObjectReference(grazeDetector, "inkPulseController", inkPulse);

        ConfigurePlayerVisualStateReferences(playerRoot, visualState, state, inkPulse);
    }

    private static void ConfigureScenePlayerReferences(Scene scene, GameObject playerRoot)
    {
        GameSessionController session = FindFirstInScene<GameSessionController>(scene);
        RunProgressionDirector progression = FindFirstInScene<RunProgressionDirector>(scene);
        Camera mainCamera = FindMainCamera(scene);
        ChargeBar chargeBar = FindFirstInScene<ChargeBar>(scene);

        InkPulseController inkPulse = playerRoot.GetComponent<InkPulseController>();
        PlayerMovement movement = playerRoot.GetComponent<PlayerMovement>();
        PlayerCollision collision = playerRoot.GetComponent<PlayerCollision>();
        ShrimpCollector shrimpCollector = playerRoot.GetComponent<ShrimpCollector>();
        PlayerStateController state = playerRoot.GetComponent<PlayerStateController>();
        PlayerGadgetInventory inventory = playerRoot.GetComponent<PlayerGadgetInventory>();
        Collider2D playerCollider = playerRoot.GetComponent<Collider2D>();
        GrazeDetector grazeDetector = playerRoot.GetComponentInChildren<GrazeDetector>(includeInactive: true);
        PlayerVisualStateController visualState = EnsurePlayerVisualStateController(playerRoot);

        SetObjectReference(movement, "session", session);
        SetObjectReference(movement, "progression", progression);
        SetObjectReference(movement, "gameplayCamera", mainCamera);
        SetObjectReference(movement, "playerCollider", playerCollider);

        SetObjectReference(inkPulse, "session", session);
        SetObjectReference(inkPulse, "chargeBar", chargeBar);

        SetObjectReference(shrimpCollector, "session", session);

        SetObjectReference(collision, "session", session);
        SetObjectReference(collision, "inkPulseController", inkPulse);
        SetObjectReference(collision, "shrimpCollector", shrimpCollector);
        SetObjectReference(collision, "gadgetInventory", inventory);
        SetObjectReference(collision, "damageCollider", playerCollider);

        SetObjectReference(inventory, "session", session);
        SetObjectReference(inventory, "inkPulseController", inkPulse);

        SetObjectReference(state, "session", session);
        SetObjectReference(state, "inkPulse", inkPulse);
        SetObjectReference(state, "movement", movement);

        SetObjectReference(grazeDetector, "session", session);
        SetObjectReference(grazeDetector, "inkPulseController", inkPulse);

        ConfigurePlayerVisualStateReferences(playerRoot, visualState, state, inkPulse);
    }

    private static void ConfigureSceneManagerReferences(Scene scene, GameObject playerRoot)
    {
        GameSessionController session = FindFirstInScene<GameSessionController>(scene);
        RunProgressionDirector progression = FindFirstInScene<RunProgressionDirector>(scene);
        SceneFlowController sceneFlow = FindFirstInScene<SceneFlowController>(scene);
        Camera mainCamera = FindMainCamera(scene);
        CameraController cameraController = mainCamera != null ? mainCamera.GetComponent<CameraController>() : null;
        InkPulseController inkPulse = playerRoot.GetComponent<InkPulseController>();

        RunProgressionDirector progressionDirector = FindFirstInScene<RunProgressionDirector>(scene);
        SetObjectReference(progressionDirector, "session", session);
        SetObjectReference(progressionDirector, "distanceReference", playerRoot.transform);

        LevelSpawner levelSpawner = FindFirstInScene<LevelSpawner>(scene);
        SetObjectReference(levelSpawner, "session", session);
        SetObjectReference(levelSpawner, "progression", progression);
        SetObjectReference(levelSpawner, "spawnCamera", mainCamera);
        SetObjectReference(levelSpawner, "player", playerRoot.transform);

        BossEventDirector bossDirector = FindFirstInScene<BossEventDirector>(scene);
        SetObjectReference(bossDirector, "session", session);
        SetObjectReference(bossDirector, "progression", progression);
        SetObjectReference(bossDirector, "spawnCamera", mainCamera);
        SetObjectReference(bossDirector, "eventCameraController", cameraController);

        SetObjectReference(cameraController, "currentCamera", mainCamera);
        SetObjectReference(cameraController, "target", playerRoot.transform);
        SetObjectReference(cameraController, "inkPulse", inkPulse);

        InGameShopManager shop = FindFirstInScene<InGameShopManager>(scene);
        SetObjectReference(shop, "session", session);
        SetObjectReference(shop, "progression", progression);

        PauseMenuManager pause = FindFirstInScene<PauseMenuManager>(scene);
        SetObjectReference(pause, "session", session);
        SetObjectReference(pause, "sceneFlow", sceneFlow);

        GameOverMenuManager gameOver = FindFirstInScene<GameOverMenuManager>(scene);
        SetObjectReference(gameOver, "session", session);
        SetObjectReference(gameOver, "sceneFlow", sceneFlow);

        ZoneLightingController lighting = FindFirstInScene<ZoneLightingController>(scene);
        SetObjectReference(lighting, "session", session);
        SetObjectReference(lighting, "targetCamera", mainCamera);

        InkPulseMusicCrossfader music = FindFirstInScene<InkPulseMusicCrossfader>(scene);
        SetObjectReference(music, "inkPulse", inkPulse);
        ConfigureMusicSources(music);

        foreach (HorizontalTracker tracker in FindAllInScene<HorizontalTracker>(scene))
        {
            SetObjectReference(tracker, "cameraTransform", mainCamera != null ? mainCamera.transform : null);
        }

        foreach (DestroyOffscreen cleanup in FindAllInScene<DestroyOffscreen>(scene))
        {
            SetObjectReference(cleanup, "targetCamera", mainCamera);
        }

        foreach (ChargeBar chargeBar in FindAllInScene<ChargeBar>(scene))
        {
            if (GetObjectReference(chargeBar, "slider") == null)
            {
                SetObjectReference(chargeBar, "slider", chargeBar.GetComponentInChildren<UnityEngine.UI.Slider>(includeInactive: true));
            }
        }
    }

    private static void ConfigureZoneSpecificPlayerOverrides(Scene scene, GameObject playerRoot)
    {
        bool isZonaExe = scene.path.EndsWith("ZonaExe.unity", StringComparison.OrdinalIgnoreCase);
        LightGrazeSource lightGraze = playerRoot.GetComponent<LightGrazeSource>();

        if (isZonaExe)
        {
            if (lightGraze == null)
            {
                playerRoot.AddComponent<LightGrazeSource>();
            }

            return;
        }

        if (lightGraze != null)
        {
            UnityEngine.Object.DestroyImmediate(lightGraze);
        }
    }

    private static PlayerVisualStateController EnsurePlayerVisualStateController(GameObject playerRoot)
    {
        PlayerVisualStateController visualState = playerRoot.GetComponent<PlayerVisualStateController>();
        if (visualState == null)
        {
            visualState = playerRoot.AddComponent<PlayerVisualStateController>();
        }

        return visualState;
    }

    private static void ConfigurePlayerVisualStateReferences(
        GameObject playerRoot,
        PlayerVisualStateController visualState,
        PlayerStateController playerState,
        InkPulseController inkPulse)
    {
        if (visualState == null)
        {
            return;
        }

        Transform movementVisual = playerRoot.transform.Find("SquidVisual");
        Transform inkPulseVisual = playerRoot.transform.Find("InkPulseVisual");
        Transform portalVisual = playerRoot.transform.Find("PortalVisual");

        SetObjectReference(visualState, "playerState", playerState);
        SetObjectReference(visualState, "inkPulse", inkPulse);
        SetObjectReference(visualState, "movementVisualRoot", movementVisual != null ? movementVisual.gameObject : null);
        SetObjectReference(visualState, "inkPulseVisualRoot", inkPulseVisual != null ? inkPulseVisual.gameObject : null);
        SetObjectReference(visualState, "portalVisualRoot", portalVisual != null ? portalVisual.gameObject : null);
        SetObjectReference(visualState, "movementAnimator", movementVisual != null ? movementVisual.GetComponent<Animator>() : null);
        SetObjectReference(visualState, "inkPulseAnimator", inkPulseVisual != null ? inkPulseVisual.GetComponent<Animator>() : null);
        SetObjectReference(visualState, "portalAnimator", portalVisual != null ? portalVisual.GetComponent<Animator>() : null);
    }

    private static void ConfigureMusicSources(InkPulseMusicCrossfader music)
    {
        if (music == null)
        {
            return;
        }

        AudioSource[] sources = music.GetComponents<AudioSource>();
        if (sources.Length == 0)
        {
            return;
        }

        AudioSource normal = sources.FirstOrDefault(source => source != null && !source.name.Contains("INK", StringComparison.OrdinalIgnoreCase));
        AudioSource ink = sources.FirstOrDefault(source => source != null && source.name.Contains("INK", StringComparison.OrdinalIgnoreCase));

        normal ??= sources[0];
        ink ??= sources.FirstOrDefault(source => source != null && source != normal);

        SetObjectReference(music, "normalTrack", normal);
        SetObjectReference(music, "inkTrack", ink);
    }

    private static void ValidateSceneContract(Scene scene, GameObject playerRoot, GameObject prefabAsset)
    {
        EnsurePlayerIsPrefabInstance(playerRoot, prefabAsset, scene.path);

        if (FindAllPlayerRoots(scene).Count != 1)
        {
            Debug.LogWarning($"[PlayerPrefabContractUtility] Scene {scene.path} should contain exactly one Squid player.", playerRoot);
        }

        if (!BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out _, out _))
        {
            Debug.LogWarning($"[PlayerPrefabContractUtility] Scene {scene.path} is missing PlayerBoundaries/TopBoundary or BottomBoundary.", playerRoot);
        }

        if (!BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out _, out _))
        {
            Debug.LogWarning($"[PlayerPrefabContractUtility] Scene {scene.path} is missing CameraBoundaries/TopBoundary or BottomBoundary.", playerRoot);
        }
    }

    private static void EnsurePlayerIsPrefabInstance(GameObject playerRoot, GameObject prefabAsset, string scenePath)
    {
        if (playerRoot == null)
        {
            throw new InvalidOperationException($"Scene {scenePath} has no player.");
        }

        string instancePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(playerRoot);
        string expectedPath = AssetDatabase.GetAssetPath(prefabAsset);
        if (instancePath != expectedPath)
        {
            throw new InvalidOperationException($"Scene {scenePath} player is not an instance of {expectedPath}. Actual: {instancePath}");
        }
    }

    private static void RemoveZoneSpecificComponents(GameObject playerRoot)
    {
        foreach (LightGrazeSource lightGrazeSource in playerRoot.GetComponents<LightGrazeSource>())
        {
            UnityEngine.Object.DestroyImmediate(lightGrazeSource);
        }
    }

    private static void CopySceneSpecificComponents(GameObject oldRoot, GameObject newRoot)
    {
        foreach (Transform oldTransform in oldRoot.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            string relativePath = GetRelativePath(oldRoot.transform, oldTransform);
            Transform newTransform = string.IsNullOrEmpty(relativePath)
                ? newRoot.transform
                : newRoot.transform.Find(relativePath);

            if (newTransform == null)
            {
                continue;
            }

            foreach (Component oldComponent in oldTransform.GetComponents<Component>())
            {
                if (oldComponent == null || oldComponent is Transform)
                {
                    continue;
                }

                Type componentType = oldComponent.GetType();
                if (newTransform.GetComponent(componentType) != null)
                {
                    continue;
                }

                ComponentUtility.CopyComponent(oldComponent);
                ComponentUtility.PasteComponentAsNew(newTransform.gameObject);
            }
        }
    }

    private static Dictionary<UnityEngine.Object, UnityEngine.Object> BuildObjectMap(GameObject oldRoot, GameObject newRoot)
    {
        Dictionary<UnityEngine.Object, UnityEngine.Object> objectMap = new();

        foreach (Transform oldTransform in oldRoot.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            string relativePath = GetRelativePath(oldRoot.transform, oldTransform);
            Transform newTransform = string.IsNullOrEmpty(relativePath)
                ? newRoot.transform
                : newRoot.transform.Find(relativePath);

            if (newTransform == null)
            {
                continue;
            }

            objectMap[oldTransform.gameObject] = newTransform.gameObject;
            objectMap[oldTransform] = newTransform;
            MapComponents(oldTransform.gameObject, newTransform.gameObject, objectMap);
        }

        return objectMap;
    }

    private static void MapComponents(
        GameObject oldObject,
        GameObject newObject,
        Dictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
    {
        Component[] oldComponents = oldObject.GetComponents<Component>();
        foreach (Component oldComponent in oldComponents)
        {
            if (oldComponent == null || oldComponent is Transform)
            {
                continue;
            }

            Component newComponent = FindMatchingComponent(oldComponent, oldComponents, newObject);
            if (newComponent != null)
            {
                objectMap[oldComponent] = newComponent;
            }
        }
    }

    private static Component FindMatchingComponent(Component oldComponent, Component[] oldComponents, GameObject newObject)
    {
        Type componentType = oldComponent.GetType();
        int typeIndex = oldComponents
            .TakeWhile(component => component != oldComponent)
            .Count(component => component != null && component.GetType() == componentType);

        Component[] newComponents = newObject.GetComponents(componentType);
        return typeIndex >= 0 && typeIndex < newComponents.Length
            ? newComponents[typeIndex]
            : null;
    }

    private static void ReplaceSceneObjectReferences(
        Scene scene,
        IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (component == null)
                {
                    continue;
                }

                SerializedObject serializedObject = new(component);
                SerializedProperty property = serializedObject.GetIterator();
                bool changed = false;

                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    UnityEngine.Object currentReference = property.objectReferenceValue;
                    if (currentReference == null || !objectMap.TryGetValue(currentReference, out UnityEngine.Object replacement))
                    {
                        continue;
                    }

                    property.objectReferenceValue = replacement;
                    changed = true;
                }

                if (changed)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }

    private static GameObject FindSinglePlayerRoot(Scene scene)
    {
        List<GameObject> players = FindAllPlayerRoots(scene);
        if (players.Count == 0)
        {
            return null;
        }

        GameObject prefabPlayer = players.FirstOrDefault(player => !string.IsNullOrEmpty(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player)));
        return prefabPlayer != null ? prefabPlayer : players[0];
    }

    private static List<GameObject> FindAllPlayerRoots(Scene scene)
    {
        List<GameObject> players = new();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (candidate != null
                    && candidate.name == "Squid"
                    && candidate.CompareTag(GameplayTagCatalog.Player))
                {
                    players.Add(candidate.gameObject);
                }
            }
        }

        return players;
    }

    private static Camera FindMainCamera(Scene scene)
    {
        Camera[] cameras = FindAllInScene<Camera>(scene).ToArray();
        Camera mainByTag = cameras.FirstOrDefault(camera => camera != null && camera.CompareTag("MainCamera"));
        if (mainByTag != null)
        {
            return mainByTag;
        }

        return cameras.FirstOrDefault(camera => camera != null && camera.name == "Main Camera")
            ?? cameras.FirstOrDefault();
    }

    private static T FindFirstInScene<T>(Scene scene) where T : Component
    {
        return FindAllInScene<T>(scene).FirstOrDefault();
    }

    private static IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Component
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

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == target)
        {
            return string.Empty;
        }

        Stack<string> names = new();
        Transform current = target;
        while (current != null && current != root)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static UnityEngine.Object GetObjectReference(Component component, string propertyName)
    {
        if (component == null)
        {
            return null;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.ObjectReference
            ? property.objectReferenceValue
            : null;
    }

    private static void SetObjectReference(Component component, string propertyName, UnityEngine.Object value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
        {
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
    }
}
