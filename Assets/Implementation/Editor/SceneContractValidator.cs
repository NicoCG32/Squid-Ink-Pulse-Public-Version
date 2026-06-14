using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneContractValidator
{
    private const string PlayerPrefabPath = "Assets/Content/Prefabs/Player/BabySquid.prefab";
    private const string BoundariesPrefabPath = "Assets/Content/Prefabs/World/Boundaries.prefab";
    private const string CleanupPrefabPath = "Assets/Content/Prefabs/World/CleanUp.prefab";
    private const string EpipelagicaSpawnProfilePath = "Assets/Implementation/Config/Spawning/ZonaEpipelagicaSpawnProfile.asset";
    private const string AbisopelagicaSpawnProfilePath = "Assets/Implementation/Config/Spawning/ZonaAbisopelagicaSpawnProfile.asset";
    private const string TutorialSpawnProfilePath = "Assets/Implementation/Config/Spawning/ZonaTutorialSpawnProfile.asset";
    private const string InkBarHorizontalPrefabPath = "Assets/Content/Prefabs/UI/HUD/InkBarHorizontal.prefab";
    private const string InkBarVerticalPrefabPath = "Assets/Content/Prefabs/UI/HUD/InkBarVertical.prefab";
    private const string InkPulseBarLegacyPrefabPath = "Assets/Content/Prefabs/UI/HUD/InkPulseBarLegacy.prefab";
    private const string GadgetSlotsPrefabPath = "Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab";
    private const string ShrimpCounterPrefabPath = "Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab";
    private const string ScoreCounterPrefabPath = "Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab";
    private const string PauseMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab";
    private const string GameOverMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab";
    private const string InGameShopMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab";

    private static readonly SceneContract[] SceneContracts =
    {
        new(
            "Assets/Scenes/Game/ZonaEpipelagica.unity",
            EpipelagicaSpawnProfilePath,
            PortalSpawnPolicy.PostBossWindow,
            BossContract.Required,
            LightingContract.Forbidden),
        new(
            "Assets/Scenes/Game/ZonaAbisopelagica.unity",
            AbisopelagicaSpawnProfilePath,
            PortalSpawnPolicy.AlwaysInterval,
            BossContract.Forbidden,
            LightingContract.Required),
        new(
            "Assets/Scenes/Game/ZonaTutorial.unity",
            TutorialSpawnProfilePath,
            PortalSpawnPolicy.Disabled,
            BossContract.Allowed,
            LightingContract.Forbidden)
    };

    private static readonly PrefabTagLayerContract[] PrefabContracts =
    {
        new(PlayerPrefabPath, "BabySquid", GameplayTagCatalog.Player, "Player"),
        new("Assets/Content/Prefabs/Enemies/PezGlobo.prefab", "PezGlobo", EnemyTagCatalog.Pufferfish, "Enemy"),
        new("Assets/Content/Prefabs/Enemies/Mina.prefab", "Mina", EnemyTagCatalog.Mine, "Enemy"),
        new("Assets/Content/Prefabs/Enemies/CanaPescar.prefab", "CanaPescar", EnemyTagCatalog.FishingRod, "Enemy"),
        new("Assets/Content/Prefabs/Collectibles/ShrimpCoin.prefab", "ShrimpCoin", GameplayTagCatalog.Shrimp, "Collectible"),
        new("Assets/Content/Prefabs/Collectibles/ShrimpCoinX10.prefab", "ShrimpCoinX10", GameplayTagCatalog.Shrimp, "Collectible"),
        new("Assets/Content/Prefabs/Shop/DealerFish.prefab", "DealerFish", GameplayTagCatalog.Collectible, "Collectible"),
        new("Assets/Content/Prefabs/Portals/ScenePortal.prefab", "ScenePortal", GameplayTagCatalog.Portal, "Collectible"),
        new("Assets/Content/Prefabs/Bosses/SSCarnage/SSCarnage.prefab", "SSCarnage", GameplayTagCatalog.SSCarnage, "Boss"),
        new("Assets/Content/Prefabs/Bosses/SSCarnage/BossNetWall.prefab", "BossNetWall", GameplayTagCatalog.SSCarnage, "Boss"),
        new(BoundariesPrefabPath, "Boundaries", "Untagged", "Boundary"),
        new(CleanupPrefabPath, "DestroyZone", "DestroyZone", "Cleanup")
    };

    [MenuItem("Tools/Squid/Validate Scene Contracts")]
    public static void ValidateSceneContracts()
    {
        List<string> failures = new();

        ValidateRequiredAssets(failures);
        ValidatePersistentDbSeeds(failures);
        ValidatePrefabContracts(failures);
        ValidateSpawnProfiles(failures);

        foreach (SceneContract contract in SceneContracts)
        {
            ValidateScene(contract, failures);
        }

        if (failures.Count > 0)
        {
            string report = "[SceneContractValidator] Scene contract validation failed:\n- "
                + string.Join("\n- ", failures);
            Debug.LogError(report);
            throw new InvalidOperationException(report);
        }

        Debug.Log("[SceneContractValidator] Scene contracts validated successfully.");
    }

    private static void ValidateRequiredAssets(List<string> failures)
    {
        foreach (PrefabTagLayerContract contract in PrefabContracts)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(contract.PrefabPath) == null)
            {
                failures.Add($"Missing prefab asset: {contract.PrefabPath}");
            }
        }

        RequireAsset<GameObject>(InkBarHorizontalPrefabPath, failures);
        RequireAsset<GameObject>(InkBarVerticalPrefabPath, failures);
        RequireAsset<GameObject>(InkPulseBarLegacyPrefabPath, failures);
        RequireAsset<GameObject>(GadgetSlotsPrefabPath, failures);
        RequireAsset<GameObject>(ShrimpCounterPrefabPath, failures);
        RequireAsset<GameObject>(ScoreCounterPrefabPath, failures);
        RequireAsset<GameObject>(PauseMenuPrefabPath, failures);
        RequireAsset<GameObject>(GameOverMenuPrefabPath, failures);
        RequireAsset<GameObject>(InGameShopMenuPrefabPath, failures);

        foreach (SceneContract contract in SceneContracts)
        {
            if (AssetDatabase.LoadAssetAtPath<ZoneSpawnProfile>(contract.SpawnProfilePath) == null)
            {
                failures.Add($"Missing ZoneSpawnProfile asset: {contract.SpawnProfilePath}");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(contract.ScenePath) == null)
            {
                failures.Add($"Missing scene asset: {contract.ScenePath}");
            }
        }
    }

    private static void ValidatePersistentDbSeeds(List<string> failures)
    {
        ValidateJsonSeed(
            PersistentDbPaths.StreamingUnlockablesCatalogPath,
            "unlockables catalog seed",
            (UnlockablesCatalogSaveData catalog) =>
            {
                catalog.Normalize();
                if (!catalog.skins.Any(skin => skin.id == PlayerSkinIds.Default))
                {
                    failures.Add("unlockables-catalog.json must contain skin.default.");
                }

                if (!catalog.runGadgets.Any(gadget => gadget.id == PlayerUnlockableIds.ShellShieldGadget))
                {
                    failures.Add("unlockables-catalog.json must contain gadget.shell_shield.");
                }

                if (!catalog.runGadgets.Any(gadget => gadget.id == PlayerUnlockableIds.InkBottleGadget))
                {
                    failures.Add("unlockables-catalog.json must contain gadget.ink_bottle.");
                }

                if (!catalog.permanentUpgrades.Any(upgrade => upgrade.id == PlayerUnlockableIds.InkPulseDurationUpgrade))
                {
                    failures.Add("unlockables-catalog.json must contain upgrade.ink_pulse_duration.");
                }

                if (!catalog.permanentUpgrades.Any(upgrade => upgrade.id == PlayerUnlockableIds.InkPulseRechargeRateUpgrade))
                {
                    failures.Add("unlockables-catalog.json must contain upgrade.ink_pulse_recharge_rate.");
                }

                if (!catalog.permanentUpgrades.Any(upgrade => upgrade.id == PlayerUnlockableIds.ShrimpMultiplierUpgrade))
                {
                    failures.Add("unlockables-catalog.json must contain upgrade.shrimp_multiplier.");
                }

                if (!catalog.permanentUpgrades.Any(upgrade => upgrade.id == PlayerUnlockableIds.ScoreMultiplierUpgrade))
                {
                    failures.Add("unlockables-catalog.json must contain upgrade.score_multiplier.");
                }
            },
            failures);

        ValidateJsonSeed(
            PersistentDbPaths.StreamingPlayerProfilePath,
            "player profile seed",
            (PlayerProfileSaveData profile) => profile.Normalize(),
            failures);

        ValidateJsonSeed(
            PersistentDbPaths.StreamingPlayerRecordsPath,
            "player records seed",
            (PlayerRecordsSaveData records) => records.Normalize(),
            failures);

        ValidateJsonSeed(
            PersistentDbPaths.StreamingLocalLeaderboardPath,
            "local leaderboard seed",
            (LocalLeaderboardSaveData leaderboard) => leaderboard.Normalize(),
            failures);
    }

    private static void ValidateJsonSeed<T>(
        string path,
        string context,
        Action<T> validate,
        List<string> failures)
        where T : class
    {
        if (!File.Exists(path))
        {
            failures.Add($"Missing {context}: {path}");
            return;
        }

        try
        {
            T data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            if (data == null)
            {
                failures.Add($"{context} deserialized to null: {path}");
                return;
            }

            validate(data);
        }
        catch (Exception exception)
        {
            failures.Add($"Invalid {context}: {path}. {exception.Message}");
        }
    }

    private static void ValidatePrefabContracts(List<string> failures)
    {
        foreach (PrefabTagLayerContract contract in PrefabContracts)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(contract.PrefabPath);
            if (prefab == null)
            {
                continue;
            }

            Transform target = FindChildByName(prefab.transform, contract.ObjectName);
            if (target == null)
            {
                failures.Add($"{contract.PrefabPath} is missing object '{contract.ObjectName}'.");
                continue;
            }

            ValidateTag(target.gameObject, contract.ExpectedTag, $"{contract.PrefabPath}/{contract.ObjectName}", failures);
            ValidateLayer(target.gameObject, contract.ExpectedLayerName, $"{contract.PrefabPath}/{contract.ObjectName}", failures);
        }
    }

    private static void ValidateSpawnProfiles(List<string> failures)
    {
        foreach (SceneContract contract in SceneContracts)
        {
            ZoneSpawnProfile profile = AssetDatabase.LoadAssetAtPath<ZoneSpawnProfile>(contract.SpawnProfilePath);
            if (profile == null)
            {
                continue;
            }

            string context = $"{contract.SpawnProfilePath}";
            if (profile.CoinPrefab == null)
            {
                failures.Add($"{context} has no CoinPrefab.");
            }

            if (profile.DealerFishPrefab == null)
            {
                failures.Add($"{context} has no DealerFishPrefab.");
            }

            if (profile.PortalSpawnPolicy != PortalSpawnPolicy.Disabled && profile.PortalPrefab == null)
            {
                failures.Add($"{context} has portal spawning enabled but no PortalPrefab.");
            }

            if (profile.PortalSpawnPolicy != contract.ExpectedPortalPolicy)
            {
                failures.Add($"{context} expected portal policy {contract.ExpectedPortalPolicy}, got {profile.PortalSpawnPolicy}.");
            }

            ValidateEnemyProfiles(profile, context, failures);
        }
    }

    private static void ValidateEnemyProfiles(ZoneSpawnProfile profile, string context, List<string> failures)
    {
        EnemySpawnProfile[] profiles = profile.EnemyProfiles;
        if (profiles == null || profiles.Length == 0)
        {
            failures.Add($"{context} has no enemy profiles.");
            return;
        }

        HashSet<string> seenTags = new();
        foreach (EnemySpawnProfile enemyProfile in profiles)
        {
            if (enemyProfile == null)
            {
                failures.Add($"{context} contains a null enemy profile.");
                continue;
            }

            if (enemyProfile.prefab == null)
            {
                failures.Add($"{context} enemy profile '{enemyProfile.enemyTag}' has no prefab.");
            }

            if (!EnemyTagCatalog.IsEnemyTag(enemyProfile.enemyTag) || enemyProfile.enemyTag == EnemyTagCatalog.Generic)
            {
                failures.Add($"{context} enemy profile has invalid tag '{enemyProfile.enemyTag}'.");
            }

            seenTags.Add(enemyProfile.enemyTag);
        }

        RequireEnemyTag(seenTags, EnemyTagCatalog.Pufferfish, context, failures);
        RequireEnemyTag(seenTags, EnemyTagCatalog.Mine, context, failures);
        RequireEnemyTag(seenTags, EnemyTagCatalog.FishingRod, context, failures);
    }

    private static void ValidateScene(SceneContract contract, List<string> failures)
    {
        Scene scene = EditorSceneManager.OpenScene(contract.ScenePath, OpenSceneMode.Single);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(contract.ScenePath);

        ValidateNoMissingScripts(scene, failures);
        ValidateSceneCompositionPrefabs(scene, sceneName, failures);
        ValidateRequiredSceneRoots(scene, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/Player/Squid", PlayerPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/Gameplay/Boundaries", BoundariesPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/Gameplay/CleanUp", CleanupPrefabPath, sceneName, failures);
        ValidateBoundaries(scene, sceneName, failures);
        ValidateCleanup(scene, sceneName, failures);
        ValidateLevelSpawner(scene, contract, sceneName, failures);
        ValidateGameUIRoot(scene, sceneName, failures);
        ValidateGameUiPrefabInstances(scene, sceneName, failures);
        ValidateBossContract(scene, contract, sceneName, failures);
        ValidateLightingContract(scene, contract, sceneName, failures);
        ValidateCriticalSceneTagsAndLayers(scene, sceneName, failures);
    }

    private static void ValidateSceneCompositionPrefabs(Scene scene, string sceneName, List<string> failures)
    {
        string expectedCameraRigPrefabPath = $"Assets/Content/Prefabs/Core/Camera/CameraRig_{sceneName}.prefab";
        string expectedGameRootPrefabPath = $"Assets/Content/Prefabs/Core/Scenes/GameRoot_{sceneName}.prefab";
        string expectedAudioRootPrefabPath = $"Assets/Content/Prefabs/Core/Audio/AudioRoot_{sceneName}.prefab";
        string expectedEnvironmentRootPrefabPath = $"Assets/Content/Prefabs/Core/Environment/EnviromentRoot_{sceneName}.prefab";

        RequireAsset<GameObject>(expectedCameraRigPrefabPath, failures);
        RequireAsset<GameObject>(expectedGameRootPrefabPath, failures);
        RequireAsset<GameObject>(expectedAudioRootPrefabPath, failures);
        RequireAsset<GameObject>(expectedEnvironmentRootPrefabPath, failures);

        ValidateScenePrefabInstance(scene, "CameraRig", expectedCameraRigPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot", expectedGameRootPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "Audio", expectedAudioRootPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "Enviroment", expectedEnvironmentRootPrefabPath, sceneName, failures);
    }

    private static void ValidateNoMissingScripts(Scene scene, List<string> failures)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (missingCount > 0)
                {
                    failures.Add($"{scene.path}/{GetHierarchyPath(transform)} has {missingCount} missing script(s).");
                }
            }
        }
    }

    private static void ValidateRequiredSceneRoots(Scene scene, string sceneName, List<string> failures)
    {
        RequireSceneTransform(scene, "GameRoot", sceneName, failures);
        RequireSceneTransform(scene, "GameRoot/Systems", sceneName, failures);
        RequireSceneTransform(scene, "GameRoot/Gameplay", sceneName, failures);
        RequireSceneTransform(scene, "GameRoot/Player", sceneName, failures);
        RequireSceneTransform(scene, "GameRoot/GameUIRoot", sceneName, failures);
        RequireSceneTransform(scene, "CameraRig/Main Camera", sceneName, failures);
        RequireSceneTransform(scene, "Enviroment", sceneName, failures);
        RequireSceneTransform(scene, "Audio", sceneName, failures);
    }

    private static void ValidateScenePrefabInstance(
        Scene scene,
        string scenePath,
        string expectedPrefabPath,
        string sceneName,
        List<string> failures)
    {
        Transform transform = FindSceneTransform(scene, scenePath);
        if (transform == null)
        {
            failures.Add($"{sceneName} is missing {scenePath}.");
            return;
        }

        string actualPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject);
        if (actualPrefabPath != expectedPrefabPath)
        {
            failures.Add($"{sceneName}/{scenePath} must be an instance of {expectedPrefabPath}. Actual: {actualPrefabPath}");
        }
    }

    private static void ValidateBoundaries(Scene scene, string sceneName, List<string> failures)
    {
        Transform boundaries = FindSceneTransform(scene, "GameRoot/Gameplay/Boundaries");
        ValidateLayer(boundaries?.gameObject, "Boundary", $"{sceneName}/Boundaries", failures);

        ValidateBoundaryDomain(boundaries, BoundaryReferenceResolver.CameraBoundaryRootName, sceneName, failures);
        ValidateBoundaryDomain(boundaries, BoundaryReferenceResolver.PlayerBoundaryRootName, sceneName, failures);

        HorizontalTracker tracker = boundaries != null ? boundaries.GetComponent<HorizontalTracker>() : null;
        if (tracker == null)
        {
            failures.Add($"{sceneName}/Boundaries must have HorizontalTracker.");
        }
    }

    private static void ValidateBoundaryDomain(
        Transform boundaries,
        string domainRootName,
        string sceneName,
        List<string> failures)
    {
        Transform domainRoot = boundaries != null ? boundaries.Find(domainRootName) : null;
        if (domainRoot == null)
        {
            failures.Add($"{sceneName}/Boundaries is missing {domainRootName}.");
            return;
        }

        ValidateLayer(domainRoot.gameObject, "Boundary", $"{sceneName}/Boundaries/{domainRootName}", failures);
        ValidateBoundaryCollider(domainRoot, BoundaryReferenceResolver.TopBoundaryName, sceneName, failures);
        ValidateBoundaryCollider(domainRoot, BoundaryReferenceResolver.BottomBoundaryName, sceneName, failures);
    }

    private static void ValidateBoundaryCollider(
        Transform domainRoot,
        string boundaryName,
        string sceneName,
        List<string> failures)
    {
        Transform boundary = domainRoot.Find(boundaryName);
        if (boundary == null)
        {
            failures.Add($"{sceneName}/{GetHierarchyPath(domainRoot)} is missing {boundaryName}.");
            return;
        }

        if (boundary.GetComponent<Collider2D>() == null)
        {
            failures.Add($"{sceneName}/{GetHierarchyPath(boundary)} must have Collider2D.");
        }

        ValidateLayer(boundary.gameObject, "Boundary", $"{sceneName}/{GetHierarchyPath(boundary)}", failures);
    }

    private static void ValidateCleanup(Scene scene, string sceneName, List<string> failures)
    {
        Transform destroyZone = FindSceneTransform(scene, "GameRoot/Gameplay/CleanUp/DestroyZone");
        if (destroyZone == null)
        {
            failures.Add($"{sceneName} is missing GameRoot/Gameplay/CleanUp/DestroyZone.");
            return;
        }

        ValidateTag(destroyZone.gameObject, "DestroyZone", $"{sceneName}/CleanUp/DestroyZone", failures);
        ValidateLayer(destroyZone.gameObject, "Cleanup", $"{sceneName}/CleanUp/DestroyZone", failures);

        Transform garbageCollector = destroyZone.Find("GarbageCollector");
        if (garbageCollector == null)
        {
            failures.Add($"{sceneName}/CleanUp/DestroyZone is missing GarbageCollector.");
            return;
        }

        ValidateLayer(garbageCollector.gameObject, "Cleanup", $"{sceneName}/CleanUp/DestroyZone/GarbageCollector", failures);
        if (garbageCollector.GetComponent<DestroyOffscreen>() == null)
        {
            failures.Add($"{sceneName}/CleanUp/DestroyZone/GarbageCollector must have DestroyOffscreen.");
        }

        BoxCollider2D trigger = garbageCollector.GetComponent<BoxCollider2D>();
        if (trigger == null || !trigger.isTrigger)
        {
            failures.Add($"{sceneName}/CleanUp/DestroyZone/GarbageCollector must have a trigger BoxCollider2D.");
        }
    }

    private static void ValidateLevelSpawner(
        Scene scene,
        SceneContract contract,
        string sceneName,
        List<string> failures)
    {
        LevelSpawner[] spawners = FindSceneComponents<LevelSpawner>(scene).ToArray();
        if (spawners.Length != 1)
        {
            failures.Add($"{sceneName} must have exactly one LevelSpawner. Found {spawners.Length}.");
            return;
        }

        LevelSpawner spawner = spawners[0];
        SerializedObject serializedObject = new(spawner);
        ZoneSpawnProfile profile = GetObjectReference<ZoneSpawnProfile>(serializedObject, "zoneSpawnProfile");
        ZoneSpawnProfile expectedProfile = AssetDatabase.LoadAssetAtPath<ZoneSpawnProfile>(contract.SpawnProfilePath);
        if (profile != expectedProfile)
        {
            failures.Add($"{sceneName}/LevelSpawner must reference {contract.SpawnProfilePath}.");
        }

        if (GetObjectReference<Camera>(serializedObject, "spawnCamera") == null)
        {
            failures.Add($"{sceneName}/LevelSpawner must reference SpawnCamera.");
        }

        if (GetObjectReference<Transform>(serializedObject, "player") == null)
        {
            failures.Add($"{sceneName}/LevelSpawner must reference Player.");
        }

        if (GetObjectReference<Transform>(serializedObject, "spawnedParent") == null)
        {
            failures.Add($"{sceneName}/LevelSpawner must reference SpawnedParent.");
        }
    }

    private static void ValidateGameUIRoot(Scene scene, string sceneName, List<string> failures)
    {
        GameUIRoot[] roots = FindSceneComponents<GameUIRoot>(scene).ToArray();
        if (roots.Length != 1)
        {
            failures.Add($"{sceneName} must have exactly one GameUIRoot. Found {roots.Length}.");
            return;
        }

        GameUIRoot root = roots[0];
        RequireNotNull(root.EventSystemRoot, $"{sceneName}/GameUIRoot.EventSystemRoot", failures);
        RequireNotNull(root.HudRoot, $"{sceneName}/GameUIRoot.HudRoot", failures);
        RequireNotNull(root.PauseMenuRoot, $"{sceneName}/GameUIRoot.PauseMenuRoot", failures);
        RequireNotNull(root.GameOverMenuRoot, $"{sceneName}/GameUIRoot.GameOverMenuRoot", failures);
        RequireNotNull(root.InGameShopMenuRoot, $"{sceneName}/GameUIRoot.InGameShopMenuRoot", failures);
        RequireNotNull(root.InkBar, $"{sceneName}/GameUIRoot.InkBar", failures);
        RequireNotNull(root.GadgetSlots, $"{sceneName}/GameUIRoot.GadgetSlots", failures);
        RequireNotNull(root.ShrimpCounter, $"{sceneName}/GameUIRoot.ShrimpCounter", failures);
        RequireNotNull(root.ScoreCounter, $"{sceneName}/GameUIRoot.ScoreCounter", failures);
        RequireNotNull(root.PauseMenuManager, $"{sceneName}/GameUIRoot.PauseMenuManager", failures);
        RequireNotNull(root.GameOverMenuManager, $"{sceneName}/GameUIRoot.GameOverMenuManager", failures);
        RequireNotNull(root.InGameShopManager, $"{sceneName}/GameUIRoot.InGameShopManager", failures);
    }

    private static void ValidateGameUiPrefabInstances(Scene scene, string sceneName, List<string> failures)
    {
        if (sceneName == "ZonaTutorial")
        {
            ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/HUD/InkPulseBar", InkPulseBarLegacyPrefabPath, sceneName, failures);
        }
        else if (sceneName == "ZonaAbisopelagica")
        {
            ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/HUD/InkBar", InkBarVerticalPrefabPath, sceneName, failures);
        }
        else
        {
            ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/HUD/InkBar", InkBarHorizontalPrefabPath, sceneName, failures);
        }

        ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/HUD/GadgetSlots", GadgetSlotsPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/HUD/ShrimpCounter", ShrimpCounterPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/HUD/Score", ScoreCounterPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/PauseMenuManager/PauseCanvas", PauseMenuPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/GameOverMenuManager/GameOverCanvas", GameOverMenuPrefabPath, sceneName, failures);
        ValidateScenePrefabInstance(scene, "GameRoot/GameUIRoot/InGameShopManager/InGameCanvas", InGameShopMenuPrefabPath, sceneName, failures);
    }

    private static void ValidateBossContract(
        Scene scene,
        SceneContract contract,
        string sceneName,
        List<string> failures)
    {
        BossEventDirector[] directors = FindSceneComponents<BossEventDirector>(scene).ToArray();
        if (contract.BossContract == BossContract.Required && directors.Length != 1)
        {
            failures.Add($"{sceneName} must have exactly one BossEventDirector. Found {directors.Length}.");
        }

        if (contract.BossContract == BossContract.Forbidden && directors.Length > 0)
        {
            failures.Add($"{sceneName} must not have BossEventDirector.");
        }
    }

    private static void ValidateLightingContract(
        Scene scene,
        SceneContract contract,
        string sceneName,
        List<string> failures)
    {
        ZoneLightingController[] controllers = FindSceneComponents<ZoneLightingController>(scene).ToArray();
        if (contract.LightingContract == LightingContract.Required && controllers.Length != 1)
        {
            failures.Add($"{sceneName} must have exactly one ZoneLightingController. Found {controllers.Length}.");
        }

        if (contract.LightingContract == LightingContract.Forbidden && controllers.Length > 0)
        {
            failures.Add($"{sceneName} must not have ZoneLightingController.");
        }
    }

    private static void ValidateCriticalSceneTagsAndLayers(Scene scene, string sceneName, List<string> failures)
    {
        Transform player = FindSceneTransform(scene, "GameRoot/Player/Squid");
        ValidateTag(player?.gameObject, GameplayTagCatalog.Player, $"{sceneName}/Squid", failures);
        ValidateLayer(player?.gameObject, "Player", $"{sceneName}/Squid", failures);

        Camera mainCamera = FindSceneComponents<Camera>(scene).FirstOrDefault(camera => camera.CompareTag("MainCamera"));
        if (mainCamera == null)
        {
            failures.Add($"{sceneName} must have a Camera tagged MainCamera.");
        }
    }

    private static void RequireSceneTransform(Scene scene, string path, string sceneName, List<string> failures)
    {
        if (FindSceneTransform(scene, path) == null)
        {
            failures.Add($"{sceneName} is missing {path}.");
        }
    }

    private static void RequireEnemyTag(HashSet<string> tags, string expectedTag, string context, List<string> failures)
    {
        if (!tags.Contains(expectedTag))
        {
            failures.Add($"{context} is missing enemy profile for {expectedTag}.");
        }
    }

    private static void RequireNotNull(UnityEngine.Object value, string context, List<string> failures)
    {
        if (value == null)
        {
            failures.Add($"{context} is not assigned.");
        }
    }

    private static void RequireAsset<T>(string assetPath, List<string> failures) where T : UnityEngine.Object
    {
        if (AssetDatabase.LoadAssetAtPath<T>(assetPath) == null)
        {
            failures.Add($"Missing asset: {assetPath}");
        }
    }

    private static void ValidateTag(GameObject target, string expectedTag, string context, List<string> failures)
    {
        if (target == null)
        {
            failures.Add($"{context} is missing.");
            return;
        }

        if (target.tag != expectedTag)
        {
            failures.Add($"{context} expected tag '{expectedTag}', got '{target.tag}'.");
        }
    }

    private static void ValidateLayer(GameObject target, string expectedLayerName, string context, List<string> failures)
    {
        if (target == null)
        {
            failures.Add($"{context} is missing.");
            return;
        }

        int expectedLayer = LayerMask.NameToLayer(expectedLayerName);
        if (expectedLayer < 0)
        {
            failures.Add($"Project is missing layer '{expectedLayerName}'.");
            return;
        }

        if (target.layer != expectedLayer)
        {
            failures.Add($"{context} expected layer '{expectedLayerName}', got '{LayerMask.LayerToName(target.layer)}'.");
        }
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
                current = current.Find(segments[i]);
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

    private static IEnumerable<T> FindSceneComponents<T>(Scene scene) where T : Component
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

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private static T GetObjectReference<T>(SerializedObject serializedObject, string propertyName) where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.ObjectReference
            ? property.objectReferenceValue as T
            : null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        Stack<string> names = new();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private readonly struct SceneContract
    {
        public readonly string ScenePath;
        public readonly string SpawnProfilePath;
        public readonly PortalSpawnPolicy ExpectedPortalPolicy;
        public readonly BossContract BossContract;
        public readonly LightingContract LightingContract;

        public SceneContract(
            string scenePath,
            string spawnProfilePath,
            PortalSpawnPolicy expectedPortalPolicy,
            BossContract bossContract,
            LightingContract lightingContract)
        {
            ScenePath = scenePath;
            SpawnProfilePath = spawnProfilePath;
            ExpectedPortalPolicy = expectedPortalPolicy;
            BossContract = bossContract;
            LightingContract = lightingContract;
        }
    }

    private readonly struct PrefabTagLayerContract
    {
        public readonly string PrefabPath;
        public readonly string ObjectName;
        public readonly string ExpectedTag;
        public readonly string ExpectedLayerName;

        public PrefabTagLayerContract(
            string prefabPath,
            string objectName,
            string expectedTag,
            string expectedLayerName)
        {
            PrefabPath = prefabPath;
            ObjectName = objectName;
            ExpectedTag = expectedTag;
            ExpectedLayerName = expectedLayerName;
        }
    }

    private enum BossContract
    {
        Forbidden,
        Allowed,
        Required
    }

    private enum LightingContract
    {
        Forbidden,
        Required
    }
}
