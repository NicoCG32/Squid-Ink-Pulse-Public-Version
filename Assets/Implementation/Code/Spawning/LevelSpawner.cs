using System;
using UnityEngine;

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

public class LevelSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Transform player;
    [SerializeField] private Collider2D playerTopBorder;
    [SerializeField] private Collider2D playerBottomBorder;
    [SerializeField] private Transform spawnedParent;

    [Header("What to Spawn")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject rareCoinPrefab;
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
    [SerializeField, Range(0f, 1f)] private float coinSpawnChance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float rareCoinSpawnChanceWithinCoins = 0.1f;
    [SerializeField, Min(1)] private int fishingRodEnemyInterval = 5;
    [SerializeField, Range(0f, 1f)] private float fishingRodBoundaryPressure = 0.85f;
    [SerializeField, Range(0.01f, 1f)] private float upperZoneSpawnCoverage = 0.75f;
    [SerializeField, Range(0.01f, 1f)] private float lowerZoneSpawnCoverage = 0.75f;

    [Header("Boundaries")]
    [SerializeField] private float fallbackMinY = -9.5f;
    [SerializeField] private float fallbackMaxY = 9.5f;

    private float timer = 0f;
    private float activeIntervalMultiplier = 1f;
    private int spawnedEnemyCount;

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
            Vector3 coinSpawnPosition = CalculateCoinSpawnPosition();
            Instantiate(SelectCoinPrefab(), coinSpawnPosition, Quaternion.identity, spawnedParent);
            activeIntervalMultiplier = 1f;
            return;
        }

        EnemySpawnProfile selectedProfile = SelectEnemyProfileForNextEnemy();
        if (selectedProfile == null || selectedProfile.prefab == null)
        {
            return;
        }

        GameObject objectToSpawn = selectedProfile.prefab;
        string enemyTag = selectedProfile.enemyTag;
        Vector3 spawnPosition = CalculateEnemySpawnPosition(enemyTag);
        GameObject spawnedEnemy = Instantiate(objectToSpawn, spawnPosition, Quaternion.identity, spawnedParent);
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

    private float GetCameraRightEdgeX()
    {
        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        Vector3 rightEdge = spawnCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, cameraDepthToWorldZero));
        return rightEdge.x;
    }

    private Vector2 CalculateVisibleSpawnRange()
    {
        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        float cameraMinY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, cameraDepthToWorldZero)).y + verticalPadding;
        float cameraMaxY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, cameraDepthToWorldZero)).y - verticalPadding;

        float minY = cameraMinY;
        float maxY = cameraMaxY;

        if (bottomBorder != null)
        {
            minY = Mathf.Max(minY, bottomBorder.bounds.max.y + verticalPadding);
        }

        if (topBorder != null)
        {
            maxY = Mathf.Min(maxY, topBorder.bounds.min.y - verticalPadding);
        }

        if (minY <= maxY)
        {
            return new Vector2(minY, maxY);
        }

        return new Vector2(fallbackMinY, fallbackMaxY);
    }

    private Vector3 CalculateCoinSpawnPosition()
    {
        Vector2 spawnRange = CalculateVisibleSpawnRange();
        float randomY = UnityEngine.Random.Range(spawnRange.x, spawnRange.y);
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;
        return new Vector3(spawnX, randomY, 0f);
    }

    private Vector3 CalculateEnemySpawnPosition(string enemyTag)
    {
        Vector2 playerRange = CalculatePlayerSpawnRange();
        float centerY = (playerRange.x + playerRange.y) * 0.5f;
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;

        if (enemyTag == EnemyTagCatalog.Pufferfish)
        {
            float upperY = RandomInUpperCoverage(playerRange, centerY);
            return new Vector3(spawnX, upperY, 0f);
        }

        if (enemyTag == EnemyTagCatalog.FishingRod)
        {
            float laneY = CalculateFishingRodPressureY(playerRange, centerY);
            return new Vector3(spawnX, laneY, 0f);
        }

        float lowerY = RandomInLowerCoverage(playerRange, centerY);
        return new Vector3(spawnX, lowerY, 0f);
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

    private float CalculateFishingRodPressureY(Vector2 playerRange, float centerY)
    {
        float playerY = player != null ? Mathf.Clamp(player.position.y, playerRange.x, playerRange.y) : centerY;
        float targetBoundaryY = playerY >= centerY ? playerRange.y : playerRange.x;
        return Mathf.Lerp(centerY, targetBoundaryY, fishingRodBoundaryPressure);
    }

    private Vector2 CalculatePlayerSpawnRange()
    {
        ResolvePlayerBoundaryReferences();

        if (playerTopBorder != null && playerBottomBorder != null)
        {
            float minY = playerBottomBorder.bounds.max.y + verticalPadding;
            float maxY = playerTopBorder.bounds.min.y - verticalPadding;
            if (minY <= maxY)
            {
                return new Vector2(minY, maxY);
            }
        }

        return CalculateVisibleSpawnRange();
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
            && (spawnedEnemyCount + 1) % fishingRodEnemyInterval == 0;
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
                receiver.InitializeEnemySpawnContext(spawnCamera, playerTopBorder, playerBottomBorder, player);
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

        if ((topBorder == null || bottomBorder == null)
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }

        ResolvePlayerBoundaryReferences();
        ResolvePlayerReference();
    }

    private void ResolvePlayerBoundaryReferences()
    {
        if ((playerTopBorder == null || playerBottomBorder == null)
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            playerTopBorder = resolvedTop;
            playerBottomBorder = resolvedBottom;
        }
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || spawnCamera == null || !HasAnyProfilePrefab() || coinPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] Faltan referencias. Asigna Session, SpawnCamera, EnemyProfiles con prefabs y CoinPrefab en el Inspector.", this);
        }
    }
}
