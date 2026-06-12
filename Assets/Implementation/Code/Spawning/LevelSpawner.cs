using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EnemySpawnProfile
{
    public GameObject prefab;
    public string enemyTag = EnemyTagCatalog.Mine;
    [Min(0f)] public float baseWeight = 1f;
    [Range(0f, 1f)] public float minIntensity;
    [Min(0.1f)] public float spawnIntervalMultiplier = 1f;

    public EnemySpawnProfile()
    {
    }

    public EnemySpawnProfile(string enemyTag, float baseWeight, float minIntensity, float spawnIntervalMultiplier)
    {
        this.enemyTag = enemyTag;
        this.baseWeight = baseWeight;
        this.minIntensity = minIntensity;
        this.spawnIntervalMultiplier = spawnIntervalMultiplier;
    }
}

[Serializable]
public class PufferfishEnemyTuning
{
    [SerializeField, Min(0f)] private float fallSpeed = 0.55f;
    [FormerlySerializedAs("expandedRiseSpeedMultiplier")]
    [SerializeField, Min(0f)] private float expandedSpeedMultiplier = 2.2f;
    [SerializeField, Min(0f)] private float proximityRadius = 2.5f;
    [SerializeField, Min(1f)] private float expandedScaleMultiplier = 2f;
    [SerializeField, Min(0f)] private float expansionSmoothSpeed = 8f;
    [SerializeField, Min(0f)] private float erraticDirectionChangeIntervalMin = 1.2f;
    [SerializeField, Min(0f)] private float erraticDirectionChangeIntervalMax = 2.6f;
    [SerializeField, Range(0f, 1f)] private float erraticDirectionChangeChance = 0.45f;

    public float FallSpeed => Mathf.Max(0f, fallSpeed);
    public float ExpandedSpeedMultiplier => Mathf.Max(0f, expandedSpeedMultiplier);
    public float ProximityRadius => Mathf.Max(0f, proximityRadius);
    public float ExpandedScaleMultiplier => Mathf.Max(1f, expandedScaleMultiplier);
    public float ExpansionSmoothSpeed => Mathf.Max(0f, expansionSmoothSpeed);
    public float ErraticDirectionChangeIntervalMin => Mathf.Max(0f, erraticDirectionChangeIntervalMin);
    public float ErraticDirectionChangeIntervalMax => Mathf.Max(ErraticDirectionChangeIntervalMin, erraticDirectionChangeIntervalMax);
    public float ErraticDirectionChangeChance => Mathf.Clamp01(erraticDirectionChangeChance);
}

[Serializable]
public class FishingRodEnemyTuning
{
    [SerializeField, Min(0.01f)] private float dropSpeed = 14f;
    [SerializeField, Min(0f)] private float startYOffsetBelowTopBoundary = 0.15f;
    [SerializeField, Min(0.001f)] private float arriveDistance = 0.03f;
    [SerializeField, Min(0f)] private float horizontalLeadTimePaddingSeconds = 0.25f;
    [SerializeField, Min(0f)] private float minimumHorizontalLeadDistance = 2f;

    public float DropSpeed => Mathf.Max(0.01f, dropSpeed);
    public float StartYOffsetBelowTopBoundary => Mathf.Max(0f, startYOffsetBelowTopBoundary);
    public float ArriveDistance => Mathf.Max(0.001f, arriveDistance);
    public float HorizontalLeadTimePaddingSeconds => Mathf.Max(0f, horizontalLeadTimePaddingSeconds);
    public float MinimumHorizontalLeadDistance => Mathf.Max(0f, minimumHorizontalLeadDistance);
}

public enum PortalSpawnPolicy
{
    Disabled,
    PostBossWindow,
    AlwaysInterval
}

public class LevelSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnedParent;

    [Header("What to Spawn")]
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

    [Header("Portal Spawning")]
    [SerializeField] private PortalSpawnPolicy portalSpawnPolicy = PortalSpawnPolicy.PostBossWindow;
    [SerializeField] private Transform portalSpawnedParent;
    [SerializeField, Min(0f)] private float firstPortalSpawnDelay = 0f;
    [SerializeField, Range(0f, 1f)] private float postBossPortalSpawnChance = 1f;
    [SerializeField, Min(1f)] private float portalSpawnInterval = 20f;
    [SerializeField] private bool requireNoActivePortal = true;

    private float timer = 0f;
    private float dealerFishTimer;
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
        if (!enableDealerFishSpawns || dealerFishPrefab == null || spawnCamera == null)
        {
            return;
        }

        dealerFishTimer += Time.deltaTime;

        float targetInterval = hasSpawnedDealerFish ? dealerFishSpawnInterval : firstDealerFishSpawnDelay;
        if (dealerFishTimer < targetInterval)
        {
            return;
        }

        if (SpawnDealerFish())
        {
            dealerFishTimer = 0f;
            hasSpawnedDealerFish = true;
        }
    }

    private void UpdatePortalSpawnTimer()
    {
        if (portalPrefab == null || spawnCamera == null || portalSpawnPolicy == PortalSpawnPolicy.Disabled)
        {
            return;
        }

        if (!CanSpawnPortalForCurrentState())
        {
            ResetPortalSpawnWindow();
            return;
        }

        if (portalSpawnPolicy == PortalSpawnPolicy.PostBossWindow)
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
        if (portalTimer < firstPortalSpawnDelay)
        {
            return;
        }

        hasRolledPostBossPortalInCurrentWindow = true;
        if (UnityEngine.Random.value <= postBossPortalSpawnChance && TrySpawnPortal())
        {
            hasSpawnedPortalInCurrentWindow = true;
        }
    }

    private void UpdateIntervalPortalSpawnTimer()
    {
        float targetInterval = hasSpawnedPortalInCurrentWindow
            ? portalSpawnInterval
            : firstPortalSpawnDelay;

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

        if (coinPrefab != null && UnityEngine.Random.value < coinSpawnChance)
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
        return portalSpawnPolicy switch
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
        if (requireNoActivePortal && activePortal != null)
        {
            return false;
        }

        if (!TryCalculatePortalSpawnPosition(out Vector3 spawnPosition))
        {
            return false;
        }

        Transform parent = portalSpawnedParent != null ? portalSpawnedParent : spawnedParent;
        activePortal = Instantiate(portalPrefab, spawnPosition, Quaternion.identity, parent);
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
        if (!BoundaryReferenceResolver.TryResolveInnerVerticalRange(BoundaryReferenceDomain.Camera, verticalPadding, out Vector2 boundaryRange))
        {
            return false;
        }

        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        float cameraMinY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, cameraDepthToWorldZero)).y + verticalPadding;
        float cameraMaxY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, cameraDepthToWorldZero)).y - verticalPadding;

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
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;
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
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;
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

        float lowerQuarterTopY = Mathf.Lerp(playerRange.x, playerRange.y, 0.25f);
        float randomY = UnityEngine.Random.Range(playerRange.x, lowerQuarterTopY);
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;
        spawnPosition = new Vector3(spawnX, randomY, 0f);
        return true;
    }

    private bool SpawnDealerFish()
    {
        if (!TryCalculateDealerFishSpawnPosition(out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject dealerFish = Instantiate(dealerFishPrefab, spawnPosition, Quaternion.identity, spawnedParent);
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
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;

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
        if (rareCoinPrefab != null && UnityEngine.Random.value < rareCoinSpawnChanceWithinCoins)
        {
            return rareCoinPrefab;
        }

        return coinPrefab;
    }

    private float RandomInUpperCoverage(Vector2 playerRange, float centerY)
    {
        float upperHeight = Mathf.Max(0.01f, playerRange.y - centerY);
        float minY = playerRange.y - (upperHeight * upperZoneSpawnCoverage);
        return UnityEngine.Random.Range(minY, playerRange.y);
    }

    private float RandomInLowerCoverage(Vector2 playerRange, float centerY)
    {
        float lowerHeight = Mathf.Max(0.01f, centerY - playerRange.x);
        float maxY = playerRange.x + (lowerHeight * lowerZoneSpawnCoverage);
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
        float dropDuration = dropDistance / fishingRodTuning.DropSpeed;
        float playerSpeed = GetCurrentPlayerHorizontalSpeed();
        float dynamicLeadDistance = playerSpeed * (dropDuration + fishingRodTuning.HorizontalLeadTimePaddingSeconds);
        float minimumDistance = Mathf.Max(spawnDistanceFromCameraRight, fishingRodTuning.MinimumHorizontalLeadDistance);
        float spawnDistance = Mathf.Max(minimumDistance, dynamicLeadDistance);

        return GetCameraRightEdgeX() + spawnDistance;
    }

    private float CalculateFishingRodStartY(float targetY)
    {
        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D topBorder, out _))
        {
            float startY = topBorder.bounds.min.y - fishingRodTuning.StartYOffsetBelowTopBoundary;
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
            verticalPadding,
            out playerRange);
    }

    private float GetCurrentSpawnInterval()
    {
        float baseInterval = progression != null
            ? progression.Current.SpawnInterval
            : timeBetweenSpawns;

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
        return fishingRodEnemyInterval > 0
            && !IsBossActive()
            && (spawnedEnemyCount + 1) % fishingRodEnemyInterval == 0;
    }

    private bool IsBossActive()
    {
        return progression != null && progression.EventState == RunEventState.BossActive;
    }

    private EnemySpawnProfile SelectEnemyProfile(bool includeFishingRod)
    {
        EnemySpawnProfile[] profiles = enemyProfiles;
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
        if (enemyProfiles == null)
        {
            return null;
        }

        foreach (EnemySpawnProfile profile in enemyProfiles)
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
        if (enemyProfiles == null)
        {
            return false;
        }

        foreach (EnemySpawnProfile profile in enemyProfiles)
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
                    pufferfishTuning,
                    fishingRodTuning));
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
            || coinPrefab == null
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out _, out _)
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out _, out _))
        {
            Debug.LogWarning(
                $"[LevelSpawner] Faltan referencias o boundaries. Configura Session, SpawnCamera, EnemyProfiles, CoinPrefab y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)} / {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Player)}.",
                this);
        }

        if (enableDealerFishSpawns && dealerFishPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] DealerFish spawns esta activo, pero falta asignar DealerFishPrefab.", this);
        }

        if (portalSpawnPolicy != PortalSpawnPolicy.Disabled && portalPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] Portal spawns esta activo, pero falta asignar PortalPrefab.", this);
        }
    }
}
