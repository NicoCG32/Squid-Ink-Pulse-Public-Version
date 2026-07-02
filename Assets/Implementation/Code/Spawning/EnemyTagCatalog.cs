using UnityEngine;

public static class EnemyTagCatalog
{
    public const string Generic = "Enemy";
    public const string Mine = "EnemyMina";
    public const string Pufferfish = "EnemyPezGlobo";
    public const string FishingRod = "EnemyCanaPescar";
    public const string Ray = "EnemyRay";
    public const string Jellyfish = "EnemyJellyfish";

    public static bool IsEnemyTag(string tag)
    {
        return tag == Generic
            || tag == Mine
            || tag == Pufferfish
            || tag == FishingRod
            || tag == Ray
            || tag == Jellyfish;
    }

    public static bool IsEnemy(Collider2D collider)
    {
        return collider != null && IsEnemyTag(collider.gameObject.tag);
    }

    public static void ApplyEnemyTag(GameObject target, string tag)
    {
        if (target == null)
        {
            return;
        }

        string resolvedTag = IsEnemyTag(tag) ? tag : Generic;

        try
        {
            target.tag = resolvedTag;
        }
        catch (UnityException)
        {
            target.tag = Generic;
        }
    }
}
