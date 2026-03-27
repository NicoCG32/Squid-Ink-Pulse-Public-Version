using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    [Header("What to Spawn")]
    public GameObject enemyPrefab;
    public GameObject coinPrefab;

    [Header("Spawn Settings")]
    public float timeBetweenSpawns = 1.5f;
    public float spawnDistanceX = 25f;
    
    [Header("Boundaries")]
    public float minY = -9.5f;
    public float maxY = 9.5f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeBetweenSpawns)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    private void SpawnObject()
    {
        float randomY = Random.Range(minY, maxY);

        Vector3 spawnPosition = new Vector3(transform.position.x + spawnDistanceX, randomY, 0f);

        GameObject objectToSpawn;
        if (Random.value > 0.7f) 
        {
            objectToSpawn = coinPrefab;
        }
        else 
        {
            objectToSpawn = enemyPrefab;
        }
        Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
    }
}