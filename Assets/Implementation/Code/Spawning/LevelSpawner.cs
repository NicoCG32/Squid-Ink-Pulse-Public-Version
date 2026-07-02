using UnityEngine;

[DisallowMultipleComponent]
public class LevelSpawner : MonoBehaviour
{
    [Header("Zone Profile")]
    [SerializeField] private ZoneSpawnProfile zoneSpawnProfile = null;

    [Header("References")]
    [SerializeField] private GameSessionController session = null;
    [SerializeField] private RunProgressionDirector progression = null;
    [SerializeField] private Camera spawnCamera = null;
    [SerializeField] private Transform player = null;
    [SerializeField] private Transform spawnedParent = null;
    [SerializeField] private Transform portalSpawnedParent = null;

    private float regularSpawnTimer;
    private float activeIntervalMultiplier = 1f;
    private int spawnedEnemyCount;

    private float dealerFishTimer;
    private float dealerFishTargetInterval;
    private bool hasSpawnedDealerFish;

    private float portalTimer;
    private bool hasSpawnedPortalInCurrentWindow;
    private bool hasRolledPostBossPortalInCurrentWindow;
    private GameObject activePortal;

    private void Awake()
    {
        ResolveSceneReferences();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

        ResolveSceneReferences();
        UpdateDealerFishSpawnTimer();
        UpdatePortalSpawnTimer();

        if (progression != null && progression.IsEventBlockingRegularSpawns)
        {
            regularSpawnTimer = 0f;
            return;
        }

        regularSpawnTimer += Time.deltaTime;
        if (regularSpawnTimer >= GetCurrentSpawnInterval())
        {
            SpawnRegularObject();
            regularSpawnTimer = 0f;
        }
    }

    private void UpdateDealerFishSpawnTimer()
    {
        if (!CanTickDealerFishSpawns())
        {
            return;
        }

        EnsureDealerFishTargetInterval();
        dealerFishTimer += Time.deltaTime;

        if (dealerFishTimer < dealerFishTargetInterval)
        {
            return;
        }

        if (TrySpawnDealerFish())
        {
            dealerFishTimer = 0f;
            hasSpawnedDealerFish = true;
            ScheduleNextDealerFishTargetInterval();
        }
    }

    private bool CanTickDealerFishSpawns()
    {
        return zoneSpawnProfile != null
            && zoneSpawnProfile.EnableDealerFishSpawns
            && zoneSpawnProfile.DealerFishPrefab != null
            && spawnCamera != null;
    }

    private void UpdatePortalSpawnTimer()
    {
        if (!CanTickPortalSpawns())
        {
            return;
        }

        if (!CanSpawnPortalForCurrentState())
        {
            ResetPortalSpawnWindow();
            return;
        }

        UpdatePostBossPortalSpawnTimer();
    }

    private bool CanTickPortalSpawns()
    {
        return zoneSpawnProfile != null
            && zoneSpawnProfile.PortalPrefab != null
            && spawnCamera != null
            && zoneSpawnProfile.PortalSpawnPolicy != PortalSpawnPolicy.Disabled;
    }

    private void UpdatePostBossPortalSpawnTimer()
    {
        if (hasRolledPostBossPortalInCurrentWindow || hasSpawnedPortalInCurrentWindow)
        {
            return;
        }

        portalTimer += Time.deltaTime;
        if (portalTimer < zoneSpawnProfile.FirstPortalSpawnDelay)
        {
            return;
        }

        hasRolledPostBossPortalInCurrentWindow = true;
        if (UnityEngine.Random.value <= zoneSpawnProfile.PostBossPortalSpawnChance && TrySpawnPortal())
        {
            hasSpawnedPortalInCurrentWindow = true;
        }
    }

    private void SpawnRegularObject()
    {
        if (zoneSpawnProfile == null
            || spawnCamera == null
            || !EnemySpawnSelector.HasAnyProfilePrefab(zoneSpawnProfile.EnemyProfiles))
        {
            return;
        }

        if (ShouldSpawnCoin())
        {
            TrySpawnCoin();
            return;
        }

        TrySpawnEnemy();
    }

    private bool ShouldSpawnCoin()
    {
        return zoneSpawnProfile.CoinPrefab != null
            && UnityEngine.Random.value < zoneSpawnProfile.CoinSpawnChance;
    }

    private bool TrySpawnCoin()
    {
        if (!SpawnPositionResolver.TryCalculateCoinSpawnPosition(
                spawnCamera,
                zoneSpawnProfile.VerticalPadding,
                zoneSpawnProfile.SpawnDistanceFromCameraRight,
                out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject spawnedCoin = Instantiate(SelectCoinPrefab(), spawnPosition, Quaternion.identity, spawnedParent);
        SpawnedObjectConfigurator.ConfigureCollectible(spawnedCoin, GameplayTagCatalog.Shrimp);
        activeIntervalMultiplier = 1f;
        return true;
    }

    private GameObject SelectCoinPrefab()
    {
        if (zoneSpawnProfile.RareCoinPrefab != null
            && UnityEngine.Random.value < zoneSpawnProfile.RareCoinSpawnChanceWithinCoins)
        {
            return zoneSpawnProfile.RareCoinPrefab;
        }

        return zoneSpawnProfile.CoinPrefab;
    }

    private bool TrySpawnEnemy()
    {
        EnemySpawnProfile selectedProfile = EnemySpawnSelector.SelectForNextEnemy(
            zoneSpawnProfile.EnemyProfiles,
            GetCurrentIntensity(),
            zoneSpawnProfile.FishingRodEnemyInterval,
            spawnedEnemyCount,
            IsBossActive());

        if (selectedProfile == null || selectedProfile.prefab == null)
        {
            return false;
        }

        if (!SpawnPositionResolver.TryCalculateEnemySpawnPosition(
                spawnCamera,
                player,
                progression,
                zoneSpawnProfile,
                selectedProfile.enemyTag,
                out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject spawnedEnemy = Instantiate(selectedProfile.prefab, spawnPosition, Quaternion.identity, spawnedParent);
        SpawnedObjectConfigurator.ConfigureEnemy(spawnedEnemy, selectedProfile.enemyTag, BuildEnemySpawnContext());

        activeIntervalMultiplier = Mathf.Max(0.1f, selectedProfile.spawnIntervalMultiplier);
        spawnedEnemyCount++;
        return true;
    }

    private EnemySpawnContext BuildEnemySpawnContext()
    {
        PufferfishEnemyTuning pufferfishTuning = zoneSpawnProfile != null
            ? zoneSpawnProfile.PufferfishTuning
            : new PufferfishEnemyTuning();
        FishingRodEnemyTuning fishingRodTuning = zoneSpawnProfile != null
            ? zoneSpawnProfile.FishingRodTuning
            : new FishingRodEnemyTuning();
        RayEnemyTuning rayTuning = zoneSpawnProfile != null
            ? zoneSpawnProfile.RayTuning
            : new RayEnemyTuning();
        JellyfishEnemyTuning jellyfishTuning = zoneSpawnProfile != null
            ? zoneSpawnProfile.JellyfishTuning
            : new JellyfishEnemyTuning();

        return new EnemySpawnContext(spawnCamera, player, pufferfishTuning, fishingRodTuning, rayTuning, jellyfishTuning);
    }

    private bool CanSpawnPortalForCurrentState()
    {
        return zoneSpawnProfile.PortalSpawnPolicy switch
        {
            PortalSpawnPolicy.PostBossWindow => progression != null && progression.EventState == RunEventState.PostBossWindow,
            _ => false
        };
    }

    private void ResetPortalSpawnWindow()
    {
        portalTimer = 0f;
        hasSpawnedPortalInCurrentWindow = false;
        hasRolledPostBossPortalInCurrentWindow = false;
    }

    private bool TrySpawnPortal()
    {
        if (zoneSpawnProfile.RequireNoActivePortal && activePortal != null)
        {
            return false;
        }

        if (!SpawnPositionResolver.TryCalculatePortalSpawnPosition(
                spawnCamera,
                zoneSpawnProfile.VerticalPadding,
                zoneSpawnProfile.SpawnDistanceFromCameraRight,
                out Vector3 spawnPosition))
        {
            return false;
        }

        Transform parent = portalSpawnedParent != null ? portalSpawnedParent : spawnedParent;
        activePortal = Instantiate(zoneSpawnProfile.PortalPrefab, spawnPosition, Quaternion.identity, parent);
        SpawnedObjectConfigurator.ConfigureCollectible(activePortal, GameplayTagCatalog.Portal);
        return true;
    }

    private bool TrySpawnDealerFish()
    {
        if (!SpawnPositionResolver.TryCalculateDealerFishSpawnPosition(
                spawnCamera,
                zoneSpawnProfile.VerticalPadding,
                zoneSpawnProfile.DealerFishSpawnDistanceFromCameraRight,
                zoneSpawnProfile.DealerFishSpawnZoneMin,
                zoneSpawnProfile.DealerFishSpawnZoneMax,
                out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject dealerFish = Instantiate(zoneSpawnProfile.DealerFishPrefab, spawnPosition, Quaternion.identity, spawnedParent);
        SpawnedObjectConfigurator.ConfigureCollectible(dealerFish, GameplayTagCatalog.Collectible);
        return true;
    }

    private void EnsureDealerFishTargetInterval()
    {
        if (dealerFishTargetInterval <= 0f)
        {
            ScheduleNextDealerFishTargetInterval();
        }
    }

    private void ScheduleNextDealerFishTargetInterval()
    {
        float baseInterval = hasSpawnedDealerFish
            ? zoneSpawnProfile.DealerFishSpawnInterval
            : zoneSpawnProfile.FirstDealerFishSpawnDelay;
        float randomMultiplier = UnityEngine.Random.Range(
            zoneSpawnProfile.DealerFishIntervalRandomMultiplierMin,
            zoneSpawnProfile.DealerFishIntervalRandomMultiplierMax);

        dealerFishTargetInterval = Mathf.Max(0f, baseInterval) * randomMultiplier;
    }

    private float GetCurrentSpawnInterval()
    {
        float baseInterval = progression != null
            ? progression.Current.SpawnInterval
            : zoneSpawnProfile != null
                ? zoneSpawnProfile.TimeBetweenSpawns
                : 1.5f;

        return Mathf.Max(0.01f, baseInterval * activeIntervalMultiplier);
    }

    private float GetCurrentIntensity()
    {
        return progression != null ? progression.Current.Intensity : 0f;
    }

    private bool IsBossActive()
    {
        return progression != null && progression.EventState == RunEventState.BossActive;
    }

    private void ResolveSceneReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (progression == null && RunProgressionDirector.HasInstance)
        {
            progression = RunProgressionDirector.Instance;
        }

        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }

        ResolvePlayerReference();
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(GameplayTagCatalog.Player);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null
            || zoneSpawnProfile == null
            || spawnCamera == null
            || zoneSpawnProfile.CoinPrefab == null
            || !EnemySpawnSelector.HasAnyProfilePrefab(zoneSpawnProfile.EnemyProfiles)
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out _, out _)
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out _, out _))
        {
            Debug.LogWarning(
                $"[LevelSpawner] Faltan referencias, ZoneSpawnProfile o boundaries. Configura Session, SpawnCamera, ZoneSpawnProfile y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)} / {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Player)}.",
                this);
        }

        if (zoneSpawnProfile != null && zoneSpawnProfile.EnableDealerFishSpawns && zoneSpawnProfile.DealerFishPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] DealerFish spawns esta activo, pero falta asignar DealerFishPrefab.", this);
        }

        if (zoneSpawnProfile != null
            && zoneSpawnProfile.PortalSpawnPolicy != PortalSpawnPolicy.Disabled
            && zoneSpawnProfile.PortalPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] Portal spawns esta activo, pero falta asignar PortalPrefab.", this);
        }
    }
}
