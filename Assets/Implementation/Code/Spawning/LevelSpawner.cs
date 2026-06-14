using UnityEngine;

[DisallowMultipleComponent]
public class LevelSpawner : MonoBehaviour
{
    [Header("Zone Profile")]
    [SerializeField] private ZoneSpawnProfile zoneSpawnProfile;

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnedParent;

    [Header("Legacy Fallback - Used When Zone Profile Is Empty")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject rareCoinPrefab;
    [SerializeField] private GameObject dealerFishPrefab;
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private EnemySpawnProfile[] enemyProfiles =
    {
        new EnemySpawnProfile(EnemyTagCatalog.Pufferfish, 1f, 0f, 1f),
        new EnemySpawnProfile(EnemyTagCatalog.Mine, 0.8f, 0.15f, 1.2f),
        new EnemySpawnProfile(EnemyTagCatalog.FishingRod, 0.55f, 0.35f, 1.35f)
    };

    [Header("Spawn Settings")]
    [SerializeField] private float timeBetweenSpawns = 1.5f;
    [SerializeField] private float spawnDistanceFromCameraRight = 2f;
    [SerializeField] private float verticalPadding = 0.75f;
    [SerializeField, Range(0f, 1f)] private float coinSpawnChance = 0.225f;
    [SerializeField, Range(0f, 1f)] private float rareCoinSpawnChanceWithinCoins = 0.1f;
    [SerializeField, Min(1)] private int fishingRodEnemyInterval = 5;
    [SerializeField, Range(0.01f, 1f)] private float upperZoneSpawnCoverage = 0.75f;
    [SerializeField, Range(0.01f, 1f)] private float lowerZoneSpawnCoverage = 0.75f;

    [Header("Enemy Behaviour Tuning")]
    [SerializeField] private PufferfishEnemyTuning pufferfishTuning = new();
    [SerializeField] private FishingRodEnemyTuning fishingRodTuning = new();

    [Header("Dealer Fish Spawning")]
    [SerializeField] private bool enableDealerFishSpawns = true;
    [SerializeField, Min(0f)] private float firstDealerFishSpawnDelay = 18f;
    [SerializeField, Min(1f)] private float dealerFishSpawnInterval = 30f;
    [SerializeField, Min(1f)] private float dealerFishIntervalRandomMultiplierMin = 1f;
    [SerializeField, Min(1f)] private float dealerFishIntervalRandomMultiplierMax = 3f;
    [SerializeField, Min(0f)] private float dealerFishSpawnDistanceFromCameraRight = 5f;
    [SerializeField, Range(0f, 0.5f)] private float dealerFishSpawnZoneMin = 0f;
    [SerializeField, Range(0.01f, 0.5f)] private float dealerFishSpawnZoneMax = 0.25f;

    [Header("Portal Spawning")]
    [SerializeField] private PortalSpawnPolicy portalSpawnPolicy = PortalSpawnPolicy.PostBossWindow;
    [SerializeField] private Transform portalSpawnedParent;
    [SerializeField, Min(0f)] private float firstPortalSpawnDelay = 0f;
    [SerializeField, Range(0f, 1f)] private float postBossPortalSpawnChance = 1f;
    [SerializeField, Min(1f)] private float portalSpawnInterval = 20f;
    [SerializeField] private bool requireNoActivePortal = true;

    private GameObject ActiveCoinPrefab => zoneSpawnProfile != null ? zoneSpawnProfile.CoinPrefab : coinPrefab;
    private GameObject ActiveRareCoinPrefab => zoneSpawnProfile != null ? zoneSpawnProfile.RareCoinPrefab : rareCoinPrefab;
    private GameObject ActiveDealerFishPrefab => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishPrefab : dealerFishPrefab;
    private GameObject ActivePortalPrefab => zoneSpawnProfile != null ? zoneSpawnProfile.PortalPrefab : portalPrefab;
    private EnemySpawnProfile[] ActiveEnemyProfiles => zoneSpawnProfile != null ? zoneSpawnProfile.EnemyProfiles : enemyProfiles;

    private float ActiveTimeBetweenSpawns => zoneSpawnProfile != null ? zoneSpawnProfile.TimeBetweenSpawns : timeBetweenSpawns;
    private float ActiveSpawnDistanceFromCameraRight => zoneSpawnProfile != null ? zoneSpawnProfile.SpawnDistanceFromCameraRight : spawnDistanceFromCameraRight;
    private float ActiveVerticalPadding => zoneSpawnProfile != null ? zoneSpawnProfile.VerticalPadding : verticalPadding;
    private float ActiveCoinSpawnChance => zoneSpawnProfile != null ? zoneSpawnProfile.CoinSpawnChance : coinSpawnChance;
    private float ActiveRareCoinSpawnChanceWithinCoins => zoneSpawnProfile != null ? zoneSpawnProfile.RareCoinSpawnChanceWithinCoins : rareCoinSpawnChanceWithinCoins;
    private int ActiveFishingRodEnemyInterval => zoneSpawnProfile != null ? zoneSpawnProfile.FishingRodEnemyInterval : fishingRodEnemyInterval;
    private float ActiveUpperZoneSpawnCoverage => zoneSpawnProfile != null ? zoneSpawnProfile.UpperZoneSpawnCoverage : upperZoneSpawnCoverage;
    private float ActiveLowerZoneSpawnCoverage => zoneSpawnProfile != null ? zoneSpawnProfile.LowerZoneSpawnCoverage : lowerZoneSpawnCoverage;

    private PufferfishEnemyTuning ActivePufferfishTuning => zoneSpawnProfile != null ? zoneSpawnProfile.PufferfishTuning : pufferfishTuning ?? new PufferfishEnemyTuning();
    private FishingRodEnemyTuning ActiveFishingRodTuning => zoneSpawnProfile != null ? zoneSpawnProfile.FishingRodTuning : fishingRodTuning ?? new FishingRodEnemyTuning();

    private bool ActiveEnableDealerFishSpawns => zoneSpawnProfile != null ? zoneSpawnProfile.EnableDealerFishSpawns : enableDealerFishSpawns;
    private float ActiveFirstDealerFishSpawnDelay => zoneSpawnProfile != null ? zoneSpawnProfile.FirstDealerFishSpawnDelay : firstDealerFishSpawnDelay;
    private float ActiveDealerFishSpawnInterval => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishSpawnInterval : dealerFishSpawnInterval;
    private float ActiveDealerFishIntervalRandomMultiplierMin => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishIntervalRandomMultiplierMin : dealerFishIntervalRandomMultiplierMin;
    private float ActiveDealerFishIntervalRandomMultiplierMax => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishIntervalRandomMultiplierMax : dealerFishIntervalRandomMultiplierMax;
    private float ActiveDealerFishSpawnDistanceFromCameraRight => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishSpawnDistanceFromCameraRight : dealerFishSpawnDistanceFromCameraRight;
    private float ActiveDealerFishSpawnZoneMin => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishSpawnZoneMin : dealerFishSpawnZoneMin;
    private float ActiveDealerFishSpawnZoneMax => zoneSpawnProfile != null ? zoneSpawnProfile.DealerFishSpawnZoneMax : dealerFishSpawnZoneMax;

    private PortalSpawnPolicy ActivePortalSpawnPolicy => zoneSpawnProfile != null ? zoneSpawnProfile.PortalSpawnPolicy : portalSpawnPolicy;
    private float ActiveFirstPortalSpawnDelay => zoneSpawnProfile != null ? zoneSpawnProfile.FirstPortalSpawnDelay : firstPortalSpawnDelay;
    private float ActivePostBossPortalSpawnChance => zoneSpawnProfile != null ? zoneSpawnProfile.PostBossPortalSpawnChance : postBossPortalSpawnChance;
    private float ActivePortalSpawnInterval => zoneSpawnProfile != null ? zoneSpawnProfile.PortalSpawnInterval : portalSpawnInterval;
    private bool ActiveRequireNoActivePortal => zoneSpawnProfile != null ? zoneSpawnProfile.RequireNoActivePortal : requireNoActivePortal;

    private float timer = 0f;
    private float dealerFishTimer;
    private float dealerFishTargetInterval;
    private float portalTimer;
    private float activeIntervalMultiplier = 1f;
    private int spawnedEnemyCount;
    private bool hasSpawnedDealerFish;
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
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= GetCurrentSpawnInterval())
        {
            SpawnObject();
            timer = 0f;
        }
    }

    private void UpdateDealerFishSpawnTimer()
    {
        if (!ActiveEnableDealerFishSpawns || ActiveDealerFishPrefab == null || spawnCamera == null)
        {
            return;
        }

        EnsureDealerFishTargetInterval();
        dealerFishTimer += Time.deltaTime;

        if (dealerFishTimer < dealerFishTargetInterval)
        {
            return;
        }

        if (SpawnDealerFish())
        {
            dealerFishTimer = 0f;
            hasSpawnedDealerFish = true;
            ScheduleNextDealerFishTargetInterval();
        }
    }

    private void UpdatePortalSpawnTimer()
    {
        if (ActivePortalPrefab == null || spawnCamera == null || ActivePortalSpawnPolicy == PortalSpawnPolicy.Disabled)
        {
            return;
        }

        if (!CanSpawnPortalForCurrentState())
        {
            ResetPortalSpawnWindow();
            return;
        }

        if (ActivePortalSpawnPolicy == PortalSpawnPolicy.PostBossWindow)
        {
            UpdatePostBossPortalSpawnTimer();
            return;
        }

        UpdateIntervalPortalSpawnTimer();
    }

    private void UpdatePostBossPortalSpawnTimer()
    {
        if (hasRolledPostBossPortalInCurrentWindow || hasSpawnedPortalInCurrentWindow)
        {
            return;
        }

        portalTimer += Time.deltaTime;
        if (portalTimer < ActiveFirstPortalSpawnDelay)
        {
            return;
        }

        hasRolledPostBossPortalInCurrentWindow = true;
        if (UnityEngine.Random.value <= ActivePostBossPortalSpawnChance && TrySpawnPortal())
        {
            hasSpawnedPortalInCurrentWindow = true;
        }
    }

    private void UpdateIntervalPortalSpawnTimer()
    {
        float targetInterval = hasSpawnedPortalInCurrentWindow
            ? ActivePortalSpawnInterval
            : ActiveFirstPortalSpawnDelay;

        portalTimer += Time.deltaTime;
        if (portalTimer < targetInterval)
        {
            return;
        }

        if (TrySpawnPortal())
        {
            hasSpawnedPortalInCurrentWindow = true;
        }

        portalTimer = 0f;
    }

    private void SpawnObject()
    {
        if (!HasAnyProfilePrefab())
        {
            return;
        }

        if (spawnCamera == null)
        {
            return;
        }

        if (ActiveCoinPrefab != null && UnityEngine.Random.value < ActiveCoinSpawnChance)
        {
            if (TryCalculateCoinSpawnPosition(out Vector3 coinSpawnPosition))
            {
                GameObject spawnedCoin = Instantiate(SelectCoinPrefab(), coinSpawnPosition, Quaternion.identity, spawnedParent);
                LightGrazeSource.EnsureOn(spawnedCoin);
                activeIntervalMultiplier = 1f;
            }

            return;
        }

        EnemySpawnProfile selectedProfile = SelectEnemyProfileForNextEnemy();
        if (selectedProfile == null || selectedProfile.prefab == null)
        {
            return;
        }

        GameObject objectToSpawn = selectedProfile.prefab;
        string enemyTag = selectedProfile.enemyTag;
        if (!TryCalculateEnemySpawnPosition(enemyTag, out Vector3 spawnPosition))
        {
            return;
        }

        GameObject spawnedEnemy = Instantiate(objectToSpawn, spawnPosition, Quaternion.identity, spawnedParent);
        LightGrazeSource.EnsureOn(spawnedEnemy);
        EnemyTagCatalog.ApplyEnemyTag(spawnedEnemy, enemyTag);
        InjectSpawnContext(spawnedEnemy);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            ApplyLayerRecursively(spawnedEnemy, enemyLayer);
        }

        activeIntervalMultiplier = Mathf.Max(0.1f, selectedProfile.spawnIntervalMultiplier);
        spawnedEnemyCount++;
    }

    private bool CanSpawnPortalForCurrentState()
    {
        return ActivePortalSpawnPolicy switch
        {
            PortalSpawnPolicy.PostBossWindow => progression != null && progression.EventState == RunEventState.PostBossWindow,
            PortalSpawnPolicy.AlwaysInterval => progression == null || !progression.IsEventBlockingRegularSpawns,
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
        if (ActiveRequireNoActivePortal && activePortal != null)
        {
            return false;
        }

        if (!TryCalculatePortalSpawnPosition(out Vector3 spawnPosition))
        {
            return false;
        }

        Transform parent = portalSpawnedParent != null ? portalSpawnedParent : spawnedParent;
        activePortal = Instantiate(ActivePortalPrefab, spawnPosition, Quaternion.identity, parent);
        LightGrazeSource.EnsureOn(activePortal);
        activePortal.tag = GameplayTagCatalog.Portal;

        int collectibleLayer = LayerMask.NameToLayer("Collectible");
        if (collectibleLayer >= 0)
        {
            ApplyLayerRecursively(activePortal, collectibleLayer);
        }

        return true;
    }

    private float GetCameraRightEdgeX()
    {
        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        Vector3 rightEdge = spawnCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, cameraDepthToWorldZero));
        return rightEdge.x;
    }

    private bool TryCalculateVisibleSpawnRange(out Vector2 spawnRange)
    {
        spawnRange = default;
        if (!BoundaryReferenceResolver.TryResolveInnerVerticalRange(BoundaryReferenceDomain.Camera, ActiveVerticalPadding, out Vector2 boundaryRange))
        {
            return false;
        }

        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        float cameraMinY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, cameraDepthToWorldZero)).y + ActiveVerticalPadding;
        float cameraMaxY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, cameraDepthToWorldZero)).y - ActiveVerticalPadding;

        float minY = Mathf.Max(cameraMinY, boundaryRange.x);
        float maxY = Mathf.Min(cameraMaxY, boundaryRange.y);

        if (minY <= maxY)
        {
            spawnRange = new Vector2(minY, maxY);
            return true;
        }

        return false;
    }

    private bool TryCalculateCoinSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculateVisibleSpawnRange(out Vector2 visibleRange)
            || !TryCalculatePlayerSpawnRange(out Vector2 playerRange))
        {
            return false;
        }

        float minY = Mathf.Max(visibleRange.x, playerRange.x);
        float maxY = Mathf.Min(visibleRange.y, playerRange.y);
        if (minY > maxY)
        {
            return false;
        }

        float randomY = UnityEngine.Random.Range(minY, maxY);
        float spawnX = GetCameraRightEdgeX() + ActiveSpawnDistanceFromCameraRight;
        spawnPosition = new Vector3(spawnX, randomY, 0f);
        return true;
    }

    private bool TryCalculatePortalSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculatePlayerSpawnRange(out Vector2 playerRange))
        {
            return false;
        }

        float randomY = UnityEngine.Random.Range(playerRange.x, playerRange.y);
        float spawnX = GetCameraRightEdgeX() + ActiveSpawnDistanceFromCameraRight;
        spawnPosition = new Vector3(spawnX, randomY, 0f);
        return true;
    }

    private bool TryCalculateDealerFishSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculatePlayerSpawnRange(out Vector2 playerRange))
        {
            return false;
        }

        float minNormalized = Mathf.Clamp(ActiveDealerFishSpawnZoneMin, 0f, 0.5f);
        float maxNormalized = Mathf.Clamp(ActiveDealerFishSpawnZoneMax, minNormalized, 0.5f);
        float minY = Mathf.Lerp(playerRange.x, playerRange.y, minNormalized);
        float maxY = Mathf.Lerp(playerRange.x, playerRange.y, maxNormalized);
        float randomY = UnityEngine.Random.Range(minY, maxY);
        float spawnX = GetCameraRightEdgeX() + ActiveDealerFishSpawnDistanceFromCameraRight;
        spawnPosition = new Vector3(spawnX, randomY, 0f);
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
            ? ActiveDealerFishSpawnInterval
            : ActiveFirstDealerFishSpawnDelay;
        float multiplierMin = Mathf.Max(1f, ActiveDealerFishIntervalRandomMultiplierMin);
        float multiplierMax = Mathf.Max(multiplierMin, ActiveDealerFishIntervalRandomMultiplierMax);
        float randomMultiplier = UnityEngine.Random.Range(multiplierMin, multiplierMax);

        dealerFishTargetInterval = Mathf.Max(0f, baseInterval) * randomMultiplier;
    }

    private bool SpawnDealerFish()
    {
        if (!TryCalculateDealerFishSpawnPosition(out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject dealerFish = Instantiate(ActiveDealerFishPrefab, spawnPosition, Quaternion.identity, spawnedParent);
        LightGrazeSource.EnsureOn(dealerFish);
        dealerFish.tag = GameplayTagCatalog.Collectible;

        int collectibleLayer = LayerMask.NameToLayer("Collectible");
        if (collectibleLayer >= 0)
        {
            ApplyLayerRecursively(dealerFish, collectibleLayer);
        }

        return true;
    }

    private bool TryCalculateEnemySpawnPosition(string enemyTag, out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculatePlayerSpawnRange(out Vector2 playerRange))
        {
            return false;
        }

        float centerY = (playerRange.x + playerRange.y) * 0.5f;
        float spawnX = GetCameraRightEdgeX() + ActiveSpawnDistanceFromCameraRight;

        if (enemyTag == EnemyTagCatalog.Pufferfish)
        {
            float upperY = RandomInUpperCoverage(playerRange, centerY);
            spawnPosition = new Vector3(spawnX, upperY, 0f);
            return true;
        }

        if (enemyTag == EnemyTagCatalog.FishingRod)
        {
            float laneY = CalculateFishingRodPlayerY(playerRange, centerY);
            spawnPosition = new Vector3(CalculateFishingRodSpawnX(laneY), laneY, 0f);
            return true;
        }

        float lowerY = RandomInLowerCoverage(playerRange, centerY);
        spawnPosition = new Vector3(spawnX, lowerY, 0f);
        return true;
    }

    private GameObject SelectCoinPrefab()
    {
        if (ActiveRareCoinPrefab != null && UnityEngine.Random.value < ActiveRareCoinSpawnChanceWithinCoins)
        {
            return ActiveRareCoinPrefab;
        }

        return ActiveCoinPrefab;
    }

    private float RandomInUpperCoverage(Vector2 playerRange, float centerY)
    {
        float upperHeight = Mathf.Max(0.01f, playerRange.y - centerY);
        float minY = playerRange.y - (upperHeight * ActiveUpperZoneSpawnCoverage);
        return UnityEngine.Random.Range(minY, playerRange.y);
    }

    private float RandomInLowerCoverage(Vector2 playerRange, float centerY)
    {
        float lowerHeight = Mathf.Max(0.01f, centerY - playerRange.x);
        float maxY = playerRange.x + (lowerHeight * ActiveLowerZoneSpawnCoverage);
        return UnityEngine.Random.Range(playerRange.x, maxY);
    }

    private float CalculateFishingRodPlayerY(Vector2 playerRange, float centerY)
    {
        return player != null
            ? Mathf.Clamp(player.position.y, playerRange.x, playerRange.y)
            : centerY;
    }

    private float CalculateFishingRodSpawnX(float targetY)
    {
        float dropStartY = CalculateFishingRodStartY(targetY);
        float dropDistance = Mathf.Max(0f, dropStartY - targetY);
        float dropDuration = dropDistance / ActiveFishingRodTuning.DropSpeed;
        float playerSpeed = GetCurrentPlayerHorizontalSpeed();
        float dynamicLeadDistance = playerSpeed * (dropDuration + ActiveFishingRodTuning.HorizontalLeadTimePaddingSeconds);
        float minimumDistance = Mathf.Max(ActiveSpawnDistanceFromCameraRight, ActiveFishingRodTuning.MinimumHorizontalLeadDistance);
        float spawnDistance = Mathf.Max(minimumDistance, dynamicLeadDistance);

        return GetCameraRightEdgeX() + spawnDistance;
    }

    private float CalculateFishingRodStartY(float targetY)
    {
        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D topBorder, out _))
        {
            float startY = topBorder.bounds.min.y - ActiveFishingRodTuning.StartYOffsetBelowTopBoundary;
            return Mathf.Max(startY, targetY);
        }

        return targetY;
    }

    private float GetCurrentPlayerHorizontalSpeed()
    {
        if (player != null && player.TryGetComponent(out PlayerMovement movement))
        {
            return Mathf.Max(0f, movement.CurrentHorizontalSpeed);
        }

        if (progression != null)
        {
            return Mathf.Max(0f, progression.Current.TargetScrollSpeed);
        }

        return 0f;
    }

    private bool TryCalculatePlayerSpawnRange(out Vector2 playerRange)
    {
        playerRange = default;

        return BoundaryReferenceResolver.TryResolveInnerVerticalRange(
            BoundaryReferenceDomain.Player,
            ActiveVerticalPadding,
            out playerRange);
    }

    private float GetCurrentSpawnInterval()
    {
        float baseInterval = progression != null
            ? progression.Current.SpawnInterval
            : ActiveTimeBetweenSpawns;

        return Mathf.Max(0.01f, baseInterval * activeIntervalMultiplier);
    }

    private EnemySpawnProfile SelectEnemyProfileForNextEnemy()
    {
        if (ShouldForceFishingRodSpawn())
        {
            EnemySpawnProfile fishingRodProfile = FindProfileByTag(EnemyTagCatalog.FishingRod);
            if (fishingRodProfile != null)
            {
                return fishingRodProfile;
            }
        }

        return SelectEnemyProfile(includeFishingRod: false);
    }

    private bool ShouldForceFishingRodSpawn()
    {
        return ActiveFishingRodEnemyInterval > 0
            && !IsBossActive()
            && (spawnedEnemyCount + 1) % ActiveFishingRodEnemyInterval == 0;
    }

    private bool IsBossActive()
    {
        return progression != null && progression.EventState == RunEventState.BossActive;
    }

    private EnemySpawnProfile SelectEnemyProfile(bool includeFishingRod)
    {
        EnemySpawnProfile[] profiles = ActiveEnemyProfiles;
        if (profiles == null || profiles.Length == 0)
        {
            return null;
        }

        float intensity = progression != null ? progression.Current.Intensity : 0f;
        float totalWeight = 0f;

        foreach (EnemySpawnProfile profile in profiles)
        {
            totalWeight += GetProfileWeight(profile, intensity, includeFishingRod);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (EnemySpawnProfile profile in profiles)
        {
            float weight = GetProfileWeight(profile, intensity, includeFishingRod);
            if (weight <= 0f)
            {
                continue;
            }

            if (roll <= weight)
            {
                return profile;
            }

            roll -= weight;
        }

        return null;
    }

    private EnemySpawnProfile FindProfileByTag(string enemyTag)
    {
        if (ActiveEnemyProfiles == null)
        {
            return null;
        }

        foreach (EnemySpawnProfile profile in ActiveEnemyProfiles)
        {
            if (profile != null && profile.enemyTag == enemyTag)
            {
                return profile;
            }
        }

        return null;
    }

    private float GetProfileWeight(EnemySpawnProfile profile, float intensity, bool includeFishingRod)
    {
        if (profile == null || intensity < profile.minIntensity)
        {
            return 0f;
        }

        if (!includeFishingRod && profile.enemyTag == EnemyTagCatalog.FishingRod)
        {
            return 0f;
        }

        return Mathf.Max(0f, profile.baseWeight);
    }

    private bool HasAnyProfilePrefab()
    {
        if (ActiveEnemyProfiles == null)
        {
            return false;
        }

        foreach (EnemySpawnProfile profile in ActiveEnemyProfiles)
        {
            if (profile != null && profile.prefab != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        foreach (Transform child in root.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
    }

    private void InjectSpawnContext(GameObject spawnedEnemy)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        MonoBehaviour[] behaviours = spawnedEnemy.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IEnemySpawnContextReceiver receiver)
            {
                receiver.InitializeEnemySpawnContext(new EnemySpawnContext(
                    spawnCamera,
                    player,
                    ActivePufferfishTuning,
                    ActiveFishingRodTuning));
            }
        }
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
            || spawnCamera == null
            || !HasAnyProfilePrefab()
            || ActiveCoinPrefab == null
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out _, out _)
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out _, out _))
        {
            Debug.LogWarning(
                $"[LevelSpawner] Faltan referencias o boundaries. Configura Session, SpawnCamera, EnemyProfiles, CoinPrefab y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)} / {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Player)}.",
                this);
        }

        if (ActiveEnableDealerFishSpawns && ActiveDealerFishPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] DealerFish spawns esta activo, pero falta asignar DealerFishPrefab.", this);
        }

        if (ActivePortalSpawnPolicy != PortalSpawnPolicy.Disabled && ActivePortalPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] Portal spawns esta activo, pero falta asignar PortalPrefab.", this);
        }
    }
}
