using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Transform spawnedParent;

    [Header("What to Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject coinPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float timeBetweenSpawns = 1.5f;
    [SerializeField] private float spawnDistanceFromCameraRight = 2f;
    [SerializeField] private float verticalPadding = 0.75f;

    [Header("Boundaries")]
    [SerializeField] private float fallbackMinY = -9.5f;
    [SerializeField] private float fallbackMaxY = 9.5f;

    private float timer = 0f;

    private void Awake()
    {
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= timeBetweenSpawns)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    private void SpawnObject()
    {
        if (enemyPrefab == null || coinPrefab == null)
        {
            return;
        }

        if (spawnCamera == null)
        {
            return;
        }

        Vector2 spawnRange = CalculateVisibleSpawnRange();
        float randomY = Random.Range(spawnRange.x, spawnRange.y);
        float spawnX = GetCameraRightEdgeX() + spawnDistanceFromCameraRight;
        Vector3 spawnPosition = new Vector3(spawnX, randomY, 0f);

        GameObject objectToSpawn;
        if (Random.value > 0.7f) 
        {
            objectToSpawn = coinPrefab;
        }
        else 
        {
            objectToSpawn = enemyPrefab;
        }

        Instantiate(objectToSpawn, spawnPosition, Quaternion.identity, spawnedParent);
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

    private void WarnIfMissingReferences()
    {
        if (session == null || spawnCamera == null || enemyPrefab == null || coinPrefab == null)
        {
            Debug.LogWarning("[LevelSpawner] Faltan referencias. Asigna Session, SpawnCamera, EnemyPrefab y CoinPrefab en el Inspector.", this);
        }
    }
}
