using UnityEngine;

public static class EnemySpawnSelector
{
    public static EnemySpawnProfile SelectForNextEnemy(
        EnemySpawnProfile[] profiles,
        float intensity,
        int fishingRodEnemyInterval,
        int spawnedEnemyCount,
        bool isBossActive)
    {
        if (ShouldForceFishingRodSpawn(fishingRodEnemyInterval, spawnedEnemyCount, isBossActive))
        {
            EnemySpawnProfile fishingRodProfile = FindProfileByTag(profiles, EnemyTagCatalog.FishingRod);
            if (fishingRodProfile != null)
            {
                return fishingRodProfile;
            }
        }

        return SelectWeightedProfile(profiles, intensity, includeFishingRod: false);
    }

    public static bool HasAnyProfilePrefab(EnemySpawnProfile[] profiles)
    {
        if (profiles == null)
        {
            return false;
        }

        foreach (EnemySpawnProfile profile in profiles)
        {
            if (profile != null && profile.prefab != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldForceFishingRodSpawn(int interval, int spawnedEnemyCount, bool isBossActive)
    {
        return interval > 0
            && !isBossActive
            && (spawnedEnemyCount + 1) % interval == 0;
    }

    private static EnemySpawnProfile SelectWeightedProfile(
        EnemySpawnProfile[] profiles,
        float intensity,
        bool includeFishingRod)
    {
        if (profiles == null || profiles.Length == 0)
        {
            return null;
        }

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

    private static EnemySpawnProfile FindProfileByTag(EnemySpawnProfile[] profiles, string enemyTag)
    {
        if (profiles == null)
        {
            return null;
        }

        foreach (EnemySpawnProfile profile in profiles)
        {
            if (profile != null && profile.enemyTag == enemyTag)
            {
                return profile;
            }
        }

        return null;
    }

    private static float GetProfileWeight(EnemySpawnProfile profile, float intensity, bool includeFishingRod)
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
}
