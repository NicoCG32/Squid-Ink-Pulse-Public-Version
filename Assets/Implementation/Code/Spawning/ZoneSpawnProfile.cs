using UnityEngine;

[CreateAssetMenu(
    fileName = "ZoneSpawnProfile",
    menuName = "Squid Ink Pulse/Spawning/Zone Spawn Profile")]
public class ZoneSpawnProfile : ScriptableObject
{
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
    [SerializeField, Min(0.01f)] private float timeBetweenSpawns = 1.5f;
    [SerializeField, Min(0f)] private float spawnDistanceFromCameraRight = 2f;
    [SerializeField, Min(0f)] private float verticalPadding = 0.75f;
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
    [SerializeField, Min(0f)] private float firstPortalSpawnDelay = 0f;
    [SerializeField, Range(0f, 1f)] private float postBossPortalSpawnChance = 1f;
    [SerializeField, Min(1f)] private float portalSpawnInterval = 20f;
    [SerializeField] private bool requireNoActivePortal = true;

    public GameObject CoinPrefab => coinPrefab;
    public GameObject RareCoinPrefab => rareCoinPrefab;
    public GameObject DealerFishPrefab => dealerFishPrefab;
    public GameObject PortalPrefab => portalPrefab;
    public EnemySpawnProfile[] EnemyProfiles => enemyProfiles;

    public float TimeBetweenSpawns => Mathf.Max(0.01f, timeBetweenSpawns);
    public float SpawnDistanceFromCameraRight => Mathf.Max(0f, spawnDistanceFromCameraRight);
    public float VerticalPadding => Mathf.Max(0f, verticalPadding);
    public float CoinSpawnChance => Mathf.Clamp01(coinSpawnChance);
    public float RareCoinSpawnChanceWithinCoins => Mathf.Clamp01(rareCoinSpawnChanceWithinCoins);
    public int FishingRodEnemyInterval => Mathf.Max(1, fishingRodEnemyInterval);
    public float UpperZoneSpawnCoverage => Mathf.Clamp(upperZoneSpawnCoverage, 0.01f, 1f);
    public float LowerZoneSpawnCoverage => Mathf.Clamp(lowerZoneSpawnCoverage, 0.01f, 1f);

    public PufferfishEnemyTuning PufferfishTuning => pufferfishTuning ?? new PufferfishEnemyTuning();
    public FishingRodEnemyTuning FishingRodTuning => fishingRodTuning ?? new FishingRodEnemyTuning();

    public bool EnableDealerFishSpawns => enableDealerFishSpawns;
    public float FirstDealerFishSpawnDelay => Mathf.Max(0f, firstDealerFishSpawnDelay);
    public float DealerFishSpawnInterval => Mathf.Max(1f, dealerFishSpawnInterval);
    public float DealerFishIntervalRandomMultiplierMin => Mathf.Max(1f, dealerFishIntervalRandomMultiplierMin);
    public float DealerFishIntervalRandomMultiplierMax => Mathf.Max(DealerFishIntervalRandomMultiplierMin, dealerFishIntervalRandomMultiplierMax);
    public float DealerFishSpawnDistanceFromCameraRight => Mathf.Max(0f, dealerFishSpawnDistanceFromCameraRight);
    public float DealerFishSpawnZoneMin => Mathf.Clamp(dealerFishSpawnZoneMin, 0f, 0.5f);
    public float DealerFishSpawnZoneMax => Mathf.Clamp(dealerFishSpawnZoneMax, DealerFishSpawnZoneMin, 0.5f);

    public PortalSpawnPolicy PortalSpawnPolicy => portalSpawnPolicy;
    public float FirstPortalSpawnDelay => Mathf.Max(0f, firstPortalSpawnDelay);
    public float PostBossPortalSpawnChance => Mathf.Clamp01(postBossPortalSpawnChance);
    public float PortalSpawnInterval => Mathf.Max(1f, portalSpawnInterval);
    public bool RequireNoActivePortal => requireNoActivePortal;
}
