using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerPrefabContractUtility
{
    private const string PrefabPath = "Assets/Content/Prefabs/Player/BabySquid.prefab";
    private const string SourceScenePath = "Assets/Scenes/Game/ZonaEpipelagica.unity";
    private const string SecondaryScenePath = "Assets/Scenes/Game/ZonaAbisopelagica.unity";
    private const string TutorialScenePath = "Assets/Scenes/Game/ZonaTutorial.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string OptionsMenuScenePath = "Assets/Scenes/OptionsMenu/OptionsMenu.unity";
    private const string ShopMenuScenePath = "Assets/Scenes/ShopMenu/ShopMenu.unity";
    private const string FishingRodPrefabPath = "Assets/Content/Prefabs/Enemies/CanaPescar.prefab";
    private const string PufferfishPrefabPath = "Assets/Content/Prefabs/Enemies/PezGlobo.prefab";
    private const string MinePrefabPath = "Assets/Content/Prefabs/Enemies/Mina.prefab";
    private const string ShrimpPrefabPath = "Assets/Content/Prefabs/Collectibles/ShrimpCoin.prefab";
    private const string RareShrimpPrefabPath = "Assets/Content/Prefabs/Collectibles/ShrimpCoinX10.prefab";
    private const string DealerFishPrefabPath = "Assets/Content/Prefabs/Shop/DealerFish.prefab";
    private const string ScenePortalPrefabPath = "Assets/Content/Prefabs/Portals/ScenePortal.prefab";

    private static readonly string[] TargetScenePaths =
    {
        SourceScenePath,
        SecondaryScenePath,
        TutorialScenePath
    };

    private static readonly string[] MenuScenePaths =
    {
        MainMenuScenePath,
        OptionsMenuScenePath,
        ShopMenuScenePath
    };

    private static readonly string[] BuildScenePaths =
    {
        MainMenuScenePath,
        SourceScenePath,
        SecondaryScenePath,
        TutorialScenePath,
        ShopMenuScenePath,
        OptionsMenuScenePath
    };

    [MenuItem("Tools/Squid/Wire All Scene Contracts And Clean Legacy")]
    public static void WireAllSceneContractsAndCleanLegacy()
    {
        EnsureBuildSettingsScenes();
        EnsureCorePrefabContracts();

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null)
        {
            throw new InvalidOperationException($"Missing player prefab at {PrefabPath}.");
        }

        foreach (string scenePath in TargetScenePaths)
        {
            WirePlayableSceneContract(scenePath, prefabAsset);
        }

        foreach (string scenePath in MenuScenePaths)
        {
            WireMenuSceneContract(scenePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] All scene contracts wired and legacy missing scripts cleaned.");
    }

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
            WirePlayableSceneContract(scenePath, prefabAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] Player scene references wired.");
    }

    [MenuItem("Tools/Squid/Ensure Enemy Prefab Contracts")]
    public static void EnsureEnemyPrefabContracts()
    {
        EnsureEnemyPrefabContractsInternal();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabContractUtility] Enemy prefab contracts ensured.");
    }

    private static void WirePlayableSceneContract(string scenePath, GameObject prefabAsset)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int removedMissingScripts = RemoveMissingScriptsInScene(scene);

        GameObject player = FindSinglePlayerRoot(scene);
        EnsurePlayerIsPrefabInstance(player, prefabAsset, scenePath);
        ConfigureScenePlayerReferences(scene, player);
        ConfigureSceneManagerReferences(scene, player);
        ConfigureZoneSpecificPlayerOverrides(scene, player);
        ValidateSceneContract(scene, player, prefabAsset);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PlayerPrefabContractUtility] Wired playable scene contract: {scenePath}. Removed missing scripts: {removedMissingScripts}.");
    }

    private static void WireMenuSceneContract(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int removedMissingScripts = RemoveMissingScriptsInScene(scene);

        ConfigureMainMenuController(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PlayerPrefabContractUtility] Wired menu scene contract: {scenePath}. Removed missing scripts: {removedMissingScripts}.");
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

    private static void EnsureCorePrefabContracts()
    {
        EnsurePlayerPrefabContract();
        EnsureEnemyPrefabContractsInternal();
        EnsureCollectiblePrefabContracts();
        EnsureWorldPrefabContracts();
    }

    private static void EnsurePlayerPrefabContract()
    {
        EditPrefab(PrefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            ConfigurePrefabIdentity(prefabRoot);
            RemoveZoneSpecificComponents(prefabRoot);
            ConfigurePrefabInternalReferences(prefabRoot);
        });
    }

    private static void EnsureEnemyPrefabContractsInternal()
    {
        EnsurePufferfishEnemyContract();
        EnsureFishingRodEnemyContract();
        EnsureMineEnemyContract();
    }

    private static void EnsurePufferfishEnemyContract()
    {
        EditPrefab(PufferfishPrefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            EnsureComponent<PufferfishEnemy>(prefabRoot);
            foreach (BoxCollider2D boxCollider in prefabRoot.GetComponents<BoxCollider2D>())
            {
                UnityEngine.Object.DestroyImmediate(boxCollider);
            }

            CircleCollider2D bodyCollider = EnsureComponent<CircleCollider2D>(prefabRoot);
            bodyCollider.isTrigger = true;
            prefabRoot.tag = EnemyTagCatalog.Pufferfish;
            ApplyLayerIfDefinedRecursively(prefabRoot, "Enemy");
        });
    }

    private static void EnsureFishingRodEnemyContract()
    {
        EditPrefab(FishingRodPrefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            EnsureComponent<FishingRodEnemy>(prefabRoot);
            EnsureAnyTriggerCollider(prefabRoot);
            prefabRoot.tag = EnemyTagCatalog.FishingRod;
            ApplyLayerIfDefinedRecursively(prefabRoot, "Enemy");
        });
    }

    private static void EnsureMineEnemyContract()
    {
        EditPrefab(MinePrefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            EnsureAnyTriggerCollider(prefabRoot);
            prefabRoot.tag = EnemyTagCatalog.Mine;
            ApplyLayerIfDefinedRecursively(prefabRoot, "Enemy");
        });
    }

    private static void EnsureCollectiblePrefabContracts()
    {
        EnsureShrimpContract(ShrimpPrefabPath, 1);
        EnsureShrimpContract(RareShrimpPrefabPath, 10);
    }

    private static void EnsureShrimpContract(string prefabPath, int amount)
    {
        EditPrefab(prefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            EnsureAnyTriggerCollider(prefabRoot);
            ShrimpValue shrimpValue = EnsureComponent<ShrimpValue>(prefabRoot);
            SetInt(shrimpValue, "amount", amount);
            prefabRoot.tag = GameplayTagCatalog.Shrimp;
            ApplyLayerIfDefinedRecursively(prefabRoot, "Collectible");
        });
    }

    private static void EnsureWorldPrefabContracts()
    {
        EnsureDealerFishContract();
        EnsureScenePortalContract();
    }

    private static void EnsureDealerFishContract()
    {
        EditPrefab(DealerFishPrefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            EnsureComponent<DealerFish>(prefabRoot);
            EnsureAnyTriggerCollider(prefabRoot);
            prefabRoot.tag = GameplayTagCatalog.Collectible;
            ApplyLayerIfDefinedRecursively(prefabRoot, "Collectible");
        });
    }

    private static void EnsureScenePortalContract()
    {
        EditPrefab(ScenePortalPrefabPath, prefabRoot =>
        {
            RemoveMissingScriptsInHierarchy(prefabRoot);
            ScenePortal portal = EnsureComponent<ScenePortal>(prefabRoot);
            SetObjectReference(portal, "sceneFlow", null);
            EnsureAnyTriggerCollider(prefabRoot);
            prefabRoot.tag = GameplayTagCatalog.Portal;
            ApplyLayerIfDefinedRecursively(prefabRoot, "Collectible");
        });
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
        ConfigureSceneFlowController(sceneFlow);
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
        ConfigureLevelSpawnerTuning(scene, levelSpawner);

        ConfigureBossEventDirector(scene, session, progression, mainCamera, cameraController);

        SetObjectReference(cameraController, "currentCamera", mainCamera);
        SetObjectReference(cameraController, "target", playerRoot.transform);
        SetObjectReference(cameraController, "inkPulse", inkPulse);

        InGameShopManager shop = FindFirstInScene<InGameShopManager>(scene);
        SetObjectReference(shop, "session", session);
        SetObjectReference(shop, "progression", progression);
        ConfigureInGameShopUiReferences(scene, shop);

        PauseMenuManager pause = FindFirstInScene<PauseMenuManager>(scene);
        SetObjectReference(pause, "session", session);
        SetObjectReference(pause, "sceneFlow", sceneFlow);
        ConfigurePauseMenuUiReferences(scene, pause);

        GameOverMenuManager gameOver = FindFirstInScene<GameOverMenuManager>(scene);
        SetObjectReference(gameOver, "session", session);
        SetObjectReference(gameOver, "sceneFlow", sceneFlow);
        ConfigureGameOverMenuUiReferences(scene, gameOver);

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

        foreach (ParallaxLayer parallaxLayer in FindAllInScene<ParallaxLayer>(scene))
        {
            SetObjectReference(parallaxLayer, "cameraTransform", mainCamera != null ? mainCamera.transform : null);
        }

        foreach (DestroyOffscreen cleanup in FindAllInScene<DestroyOffscreen>(scene))
        {
            SetObjectReference(cleanup, "targetCamera", mainCamera);
        }

        foreach (ScenePortal portal in FindAllInScene<ScenePortal>(scene))
        {
            SetObjectReference(portal, "sceneFlow", sceneFlow);
        }

        foreach (ChargeBar chargeBar in FindAllInScene<ChargeBar>(scene))
        {
            if (GetObjectReference(chargeBar, "slider") == null)
            {
                SetObjectReference(chargeBar, "slider", chargeBar.GetComponentInChildren<UnityEngine.UI.Slider>(includeInactive: true));
            }
        }

        ConfigureHudReferences(scene);
    }

    private static void ConfigureLevelSpawnerTuning(Scene scene, LevelSpawner levelSpawner)
    {
        if (levelSpawner == null)
        {
            return;
        }

        SetFloat(levelSpawner, "coinSpawnChance", 0.225f);
        SetFloat(levelSpawner, "rareCoinSpawnChanceWithinCoins", 0.1f);
        SetFloat(levelSpawner, "upperZoneSpawnCoverage", 0.75f);
        SetFloat(levelSpawner, "lowerZoneSpawnCoverage", 0.75f);
        SetFloat(levelSpawner, "fishingRodTuning.dropSpeed", 14f);

        SetFloat(levelSpawner, "firstDealerFishSpawnDelay", 18f);
        SetFloat(levelSpawner, "dealerFishSpawnInterval", 30f);
        SetFloat(levelSpawner, "dealerFishIntervalRandomMultiplierMin", 1f);
        SetFloat(levelSpawner, "dealerFishIntervalRandomMultiplierMax", 3f);
        SetFloat(levelSpawner, "dealerFishSpawnDistanceFromCameraRight", 5f);
        SetFloat(levelSpawner, "dealerFishSpawnZoneMin", 0f);
        SetFloat(levelSpawner, "dealerFishSpawnZoneMax", 0.25f);

        if (scene.path.Equals(SecondaryScenePath, StringComparison.OrdinalIgnoreCase))
        {
            SetInt(levelSpawner, "portalSpawnPolicy", (int)PortalSpawnPolicy.AlwaysInterval);
            SetFloat(levelSpawner, "firstPortalSpawnDelay", 20f);
            SetFloat(levelSpawner, "postBossPortalSpawnChance", 1f);
            SetFloat(levelSpawner, "portalSpawnInterval", 20f);
            return;
        }

        if (scene.path.Equals(SourceScenePath, StringComparison.OrdinalIgnoreCase))
        {
            SetInt(levelSpawner, "portalSpawnPolicy", (int)PortalSpawnPolicy.PostBossWindow);
            SetFloat(levelSpawner, "firstPortalSpawnDelay", 3f);
            SetFloat(levelSpawner, "postBossPortalSpawnChance", 1f);
            SetFloat(levelSpawner, "portalSpawnInterval", 20f);
            return;
        }

        if (scene.path.Equals(TutorialScenePath, StringComparison.OrdinalIgnoreCase))
        {
            SetInt(levelSpawner, "portalSpawnPolicy", (int)PortalSpawnPolicy.Disabled);
            SetFloat(levelSpawner, "firstPortalSpawnDelay", 3f);
            SetFloat(levelSpawner, "postBossPortalSpawnChance", 1f);
            SetFloat(levelSpawner, "portalSpawnInterval", 20f);
        }
    }

    private static void ConfigureBossEventDirector(
        Scene scene,
        GameSessionController session,
        RunProgressionDirector progression,
        Camera mainCamera,
        CameraController cameraController)
    {
        BossEventDirector[] bossDirectors = FindAllInScene<BossEventDirector>(scene).ToArray();
        if (bossDirectors.Length == 0)
        {
            return;
        }

        if (SceneDisablesBossEvent(scene))
        {
            foreach (BossEventDirector bossDirector in bossDirectors)
            {
                RemoveBossDirectorOwnerIfEmpty(bossDirector);
            }

            return;
        }

        BossEventDirector primaryBossDirector = bossDirectors[0];
        SetObjectReference(primaryBossDirector, "session", session);
        SetObjectReference(primaryBossDirector, "progression", progression);
        SetObjectReference(primaryBossDirector, "spawnCamera", mainCamera);
        SetObjectReference(primaryBossDirector, "eventCameraController", cameraController);
    }

    private static bool SceneDisablesBossEvent(Scene scene)
    {
        return scene.path.Equals(SecondaryScenePath, StringComparison.OrdinalIgnoreCase);
    }

    private static void RemoveBossDirectorOwnerIfEmpty(BossEventDirector bossDirector)
    {
        if (bossDirector == null)
        {
            return;
        }

        GameObject owner = bossDirector.gameObject;
        UnityEngine.Object.DestroyImmediate(bossDirector);

        if (owner == null || owner.transform.childCount > 0 || !HasOnlyTransformComponent(owner))
        {
            return;
        }

        UnityEngine.Object.DestroyImmediate(owner);
    }

    private static bool HasOnlyTransformComponent(GameObject gameObject)
    {
        return gameObject.GetComponents<Component>()
            .All(component => component == null || component is Transform);
    }

    private static void ConfigureZoneSpecificPlayerOverrides(Scene scene, GameObject playerRoot)
    {
        bool isZonaAbisopelagica = scene.path.EndsWith("ZonaAbisopelagica.unity", StringComparison.OrdinalIgnoreCase);
        LightGrazeSource lightGraze = playerRoot.GetComponent<LightGrazeSource>();

        if (isZonaAbisopelagica)
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

    private static void ConfigureSceneFlowController(SceneFlowController sceneFlow)
    {
        if (sceneFlow == null)
        {
            return;
        }

        SetString(sceneFlow, "mainMenuSceneName", "MainMenu");
        SetInt(sceneFlow, "mainMenuBuildIndex", 0);
        SetString(sceneFlow, "tutorialSceneName", TutorialScenePath);
        SetString(sceneFlow, "shopMenuSceneName", ShopMenuScenePath);
        SetString(sceneFlow, "optionsMenuSceneName", OptionsMenuScenePath);
        SetString(sceneFlow, "primaryGameplaySceneName", SourceScenePath);
        SetString(sceneFlow, "secondaryGameplaySceneName", SecondaryScenePath);
    }

    private static void ConfigureInGameShopUiReferences(Scene scene, InGameShopManager shop)
    {
        if (shop == null)
        {
            return;
        }

        GameObject menuRoot = FindChildGameObject(shop.transform, "InGameCanvas")
            ?? FindGameObjectByName(scene, "InGameCanvas");

        SetObjectReference(shop, "menuRoot", menuRoot);
        SetObjectReference(shop, "canvasGroup", menuRoot != null ? menuRoot.GetComponent<CanvasGroup>() : null);
        SetObjectReference(shop, "gadgetImage", FindComponentInChildrenByName<Image>(menuRoot, "Gadget"));
        SetObjectReference(shop, "priceText", FindComponentInChildrenByName<TMP_Text>(menuRoot, "Precio"));
        SetObjectReference(shop, "buyKeyText", FindComponentInChildrenByName<TMP_Text>(menuRoot, "B"));
        SetObjectReference(shop, "buyButton", EnsureBuyOfferButton(menuRoot));
        SetObjectReference(shop, "insufficientFundsText", FindComponentInChildrenByName<TMP_Text>(menuRoot, "SinSaldo"));
        SetObjectReference(shop, "timerText", FindComponentInChildrenByName<TMP_Text>(menuRoot, "Tiempo", "Timer"));
    }

    private static Button EnsureBuyOfferButton(GameObject menuRoot)
    {
        GameObject buyButtonObject = FindChildGameObject(menuRoot != null ? menuRoot.transform : null, "Comprar");
        if (buyButtonObject == null)
        {
            return null;
        }

        Button button = buyButtonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buyButtonObject.AddComponent<Button>();
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = buyButtonObject.GetComponent<Graphic>();
        }

        if (button.targetGraphic != null)
        {
            button.targetGraphic.raycastTarget = true;
        }

        return button;
    }

    private static void ConfigurePauseMenuUiReferences(Scene scene, PauseMenuManager pause)
    {
        if (pause == null)
        {
            return;
        }

        GameObject menuRoot = FindChildGameObject(pause.transform, "PauseCanvas")
            ?? FindGameObjectByName(scene, "PauseCanvas");

        SetObjectReference(pause, "menuRoot", menuRoot);
        SetObjectReference(pause, "canvasGroup", menuRoot != null ? menuRoot.GetComponent<CanvasGroup>() : null);
        SetObjectReference(pause, "resumeButton", FindComponentInChildrenByName<Button>(menuRoot, "BotonReanudar", "Reanudar"));
        SetObjectReference(pause, "optionsButton", FindComponentInChildrenByName<Button>(menuRoot, "BotonOpciones", "Opciones"));
        SetObjectReference(pause, "menuButton", FindComponentInChildrenByName<Button>(menuRoot, "BotonMenu", "Menu"));
        SetObjectReference(pause, "exitButton", FindComponentInChildrenByName<Button>(menuRoot, "BotonSalir", "Salir"));
        SetObjectArray(pause, "animatedDecorations", FindRectTransformsByName(menuRoot, "PauseDecoration"));
        SetObjectArray(
            pause,
            "animatedButtons",
            FindRectTransformsByName(menuRoot, "BotonReanudar", "BotonOpciones", "BotonMenu", "BotonSalir"));
    }

    private static void ConfigureGameOverMenuUiReferences(Scene scene, GameOverMenuManager gameOver)
    {
        if (gameOver == null)
        {
            return;
        }

        GameObject menuRoot = FindChildGameObject(gameOver.transform, "GameOverCanvas")
            ?? FindGameObjectByName(scene, "GameOverCanvas");

        SetObjectReference(gameOver, "menuRoot", menuRoot);
        SetObjectReference(gameOver, "canvasGroup", menuRoot != null ? menuRoot.GetComponent<CanvasGroup>() : null);
        SetObjectReference(gameOver, "retryButton", FindComponentInChildrenByName<Button>(menuRoot, "BotonReintentar"));
        SetObjectReference(gameOver, "menuButton", FindComponentInChildrenByName<Button>(menuRoot, "BotonMenu", "Menu"));
        SetObjectArray(gameOver, "animatedDecorations", FindRectTransformsByName(menuRoot, "GameOverDecoration"));
        SetObjectArray(gameOver, "animatedButtons", FindRectTransformsByName(menuRoot, "BotonReintentar", "BotonMenu"));
    }

    private static void ConfigureHudReferences(Scene scene)
    {
        ConfigureScoreDisplay(scene);

        foreach (ShrimpCounterDisplay display in FindAllInScene<ShrimpCounterDisplay>(scene))
        {
            TMP_Text amountText = FindComponentInChildrenByName<TMP_Text>(display.gameObject, "ShrimpAmountText")
                ?? display.GetComponentInChildren<TMP_Text>(includeInactive: true);

            SetObjectReference(display, "amountText", amountText);
        }

        foreach (GadgetInventoryHud hud in FindAllInScene<GadgetInventoryHud>(scene))
        {
            RectTransform firstSlotRoot = FindComponentInChildrenByName<RectTransform>(hud.gameObject, "Gadget1");
            RectTransform secondSlotRoot = FindComponentInChildrenByName<RectTransform>(hud.gameObject, "Gadget2");

            SetObjectReference(hud, "firstSlotRoot", firstSlotRoot);
            SetObjectReference(hud, "secondSlotRoot", secondSlotRoot);
            SetObjectReference(hud, "firstSlotIcon", firstSlotRoot != null ? firstSlotRoot.GetComponent<Image>() : null);
            SetObjectReference(hud, "secondSlotIcon", secondSlotRoot != null ? secondSlotRoot.GetComponent<Image>() : null);
            SetObjectReference(hud, "firstSlotText", firstSlotRoot != null ? firstSlotRoot.GetComponentInChildren<TMP_Text>(includeInactive: true) : null);
            SetObjectReference(hud, "secondSlotText", secondSlotRoot != null ? secondSlotRoot.GetComponentInChildren<TMP_Text>(includeInactive: true) : null);
        }
    }

    private static void ConfigureScoreDisplay(Scene scene)
    {
        GameObject scoreRoot = FindGameObjectByName(scene, "Score");
        if (scoreRoot == null)
        {
            return;
        }

        TMP_Text scoreText = scoreRoot.GetComponent<TMP_Text>()
            ?? scoreRoot.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (scoreText == null)
        {
            return;
        }

        ScoreCounterDisplay display = scoreRoot.GetComponent<ScoreCounterDisplay>();
        if (display == null)
        {
            display = scoreRoot.AddComponent<ScoreCounterDisplay>();
        }

        scoreText.alignment = TextAlignmentOptions.TopRight;
        scoreText.textWrappingMode = TextWrappingModes.NoWrap;
        scoreText.overflowMode = TextOverflowModes.Overflow;

        SetObjectReference(display, "scoreText", scoreText);
        SetString(display, "prefix", string.Empty);
        SetString(display, "suffix", string.Empty);
    }

    private static void ConfigureMainMenuController(Scene scene)
    {
        MainMenu controller = FindFirstInScene<MainMenu>(scene);
        if (controller == null)
        {
            GameObject controllerObject = FindGameObjectByName(scene, "Canvas") ?? new GameObject("MainMenuController");
            if (controllerObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(controllerObject, scene);
            }

            controller = controllerObject.AddComponent<MainMenu>();
        }

        SetString(controller, "playSceneName", SourceScenePath);
        SetString(controller, "optionsSceneName", OptionsMenuScenePath);

        if (GetFloat(controller, "timeDelay") <= 0f)
        {
            SetFloat(controller, "timeDelay", 0.6f);
        }
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

    private static void SetObjectArray(Component component, string propertyName, IReadOnlyList<UnityEngine.Object> values)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return;
        }

        int count = values?.Count ?? 0;
        property.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element.propertyType == SerializedPropertyType.ObjectReference)
            {
                element.objectReferenceValue = values[i];
            }
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Component component, string propertyName, string value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.String)
        {
            return;
        }

        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Component component, string propertyName, float value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Float)
        {
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static float GetFloat(Component component, string propertyName)
    {
        if (component == null)
        {
            return 0f;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.Float
            ? property.floatValue
            : 0f;
    }

    private static void SetInt(Component component, string propertyName, int value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
        else if (property.propertyType == SerializedPropertyType.Enum)
        {
            property.enumValueIndex = Mathf.Clamp(value, 0, property.enumDisplayNames.Length - 1);
        }
        else
        {
            return;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EditPrefab(string prefabPath, Action<GameObject> configure)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            throw new InvalidOperationException($"Missing prefab at {prefabPath}.");
        }

        try
        {
            configure(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static int RemoveMissingScriptsInScene(Scene scene)
    {
        int removedCount = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            removedCount += RemoveMissingScriptsInHierarchy(root);
        }

        return removedCount;
    }

    private static int RemoveMissingScriptsInHierarchy(GameObject root)
    {
        if (root == null)
        {
            return 0;
        }

        int removedCount = 0;
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            GameObject gameObject = transform.gameObject;
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingCount <= 0)
            {
                continue;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            removedCount += missingCount;
        }

        return removedCount;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target == null)
        {
            return null;
        }

        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Collider2D EnsureAnyTriggerCollider(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        Collider2D collider = target.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = target.AddComponent<CircleCollider2D>();
        }

        collider.isTrigger = true;
        return collider;
    }

    private static void ApplyLayerIfDefinedRecursively(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            ApplyLayerRecursively(root, layer);
        }
    }

    private static GameObject FindGameObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform transform in transforms)
            {
                if (transform.name == objectName)
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindChildGameObject(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static T FindComponentInChildrenByName<T>(GameObject root, params string[] objectNames) where T : Component
    {
        if (root == null || objectNames == null || objectNames.Length == 0)
        {
            return null;
        }

        HashSet<string> names = new(objectNames);
        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform transform in transforms)
        {
            if (names.Contains(transform.name) && transform.TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    private static RectTransform[] FindRectTransformsByName(GameObject root, params string[] objectNames)
    {
        if (root == null || objectNames == null || objectNames.Length == 0)
        {
            return Array.Empty<RectTransform>();
        }

        HashSet<string> names = new(objectNames);
        List<RectTransform> matches = new();
        RectTransform[] rectTransforms = root.GetComponentsInChildren<RectTransform>(includeInactive: true);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (names.Contains(rectTransform.name))
            {
                matches.Add(rectTransform);
            }
        }

        return matches.ToArray();
    }

    private static void EnsureBuildSettingsScenes()
    {
        Dictionary<string, EditorBuildSettingsScene> existingScenes = EditorBuildSettings.scenes
            .Where(scene => !string.IsNullOrWhiteSpace(scene.path))
            .ToDictionary(scene => scene.path, scene => scene);

        List<EditorBuildSettingsScene> orderedScenes = new();
        foreach (string scenePath in BuildScenePaths)
        {
            bool enabled = !existingScenes.TryGetValue(scenePath, out EditorBuildSettingsScene existing) || existing.enabled;
            orderedScenes.Add(new EditorBuildSettingsScene(scenePath, enabled));
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!orderedScenes.Any(existing => existing.path == scene.path))
            {
                orderedScenes.Add(scene);
            }
        }

        EditorBuildSettings.scenes = orderedScenes.ToArray();
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
