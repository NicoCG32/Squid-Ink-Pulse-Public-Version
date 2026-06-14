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
