using UnityEngine;

public static class EnemyTagCatalog
{
    public const string Generic = "Enemy";
    public const string Mine = "EnemyMina";
    public const string Pufferfish = "EnemyPezGlobo";
    public const string FishingRod = "EnemyCanaPescar";

    public static bool IsEnemyTag(string tag)
    {
        return tag == Generic
            || tag == Mine
            || tag == Pufferfish
            || tag == FishingRod;
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
