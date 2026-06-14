using UnityEngine;

public static class SpawnedObjectConfigurator
{
    public static void ConfigureCollectible(GameObject spawnedObject, string tag)
    {
        if (spawnedObject == null)
        {
            return;
        }

        LightGrazeSource.EnsureOn(spawnedObject);
        spawnedObject.tag = tag;
        ApplyLayerIfExists(spawnedObject, "Collectible");
    }

    public static void ConfigureEnemy(GameObject spawnedEnemy, string enemyTag, EnemySpawnContext context)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        LightGrazeSource.EnsureOn(spawnedEnemy);
        EnemyTagCatalog.ApplyEnemyTag(spawnedEnemy, enemyTag);
        InjectSpawnContext(spawnedEnemy, context);
        ApplyLayerIfExists(spawnedEnemy, "Enemy");
    }

    private static void InjectSpawnContext(GameObject spawnedEnemy, EnemySpawnContext context)
    {
        MonoBehaviour[] behaviours = spawnedEnemy.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IEnemySpawnContextReceiver receiver)
            {
                receiver.InitializeEnemySpawnContext(context);
            }
        }
    }

    private static void ApplyLayerIfExists(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            ApplyLayerRecursively(root, layer);
        }
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
