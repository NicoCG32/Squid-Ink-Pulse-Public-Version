using System;
using System.Linq;

[Serializable]
public class UnlockablesCatalogSaveData
{
    public int version = PlayerProfileRepository.UnlockablesCatalogVersion;
    public UnlockableSkinDefinition[] skins = Array.Empty<UnlockableSkinDefinition>();
    public RunGadgetUnlockDefinition[] runGadgets = Array.Empty<RunGadgetUnlockDefinition>();
    public PermanentUpgradeDefinition[] permanentUpgrades = Array.Empty<PermanentUpgradeDefinition>();

    public static UnlockablesCatalogSaveData CreateDefault()
    {
        UnlockablesCatalogSaveData data = new()
        {
            skins = new[]
            {
                new UnlockableSkinDefinition
                {
                    id = PlayerSkinIds.Default,
                    displayName = "Default",
                    defaultUnlocked = true,
                    basePrice = 0,
                    unlockGoal = UnlockGoalDefinition.None()
                }
            },
            runGadgets = new[]
            {
                new RunGadgetUnlockDefinition
                {
                    id = PlayerUnlockableIds.ShellShieldGadget,
                    gameplayId = "ShellShield",
                    displayName = "Shell Shield",
                    defaultUnlocked = true,
                    basePrice = 0,
                    unlockGoal = UnlockGoalDefinition.None()
                },
                new RunGadgetUnlockDefinition
                {
                    id = PlayerUnlockableIds.InkBottleGadget,
                    gameplayId = "InkBottle",
                    displayName = "Ink-Bottle",
                    defaultUnlocked = true,
                    basePrice = 0,
                    unlockGoal = UnlockGoalDefinition.None()
                }
            },
            permanentUpgrades = new[]
            {
                new PermanentUpgradeDefinition
                {
                    id = PlayerUnlockableIds.InkPulseDurationUpgrade,
                    displayName = "Ink Pulse Duration",
                    maxLevel = 5,
                    basePrice = 100,
                    priceGrowthMultiplier = 1.5f,
                    effectPerLevel = 0.15f,
                    unlockGoal = UnlockGoalDefinition.None()
                },
                new PermanentUpgradeDefinition
                {
                    id = PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
                    displayName = "Ink Pulse Recharge Rate",
                    maxLevel = 5,
                    basePrice = 100,
                    priceGrowthMultiplier = 1.5f,
                    effectPerLevel = 0.15f,
                    unlockGoal = UnlockGoalDefinition.None()
                },
                new PermanentUpgradeDefinition
                {
                    id = PlayerUnlockableIds.ShrimpMultiplierUpgrade,
                    displayName = "Shrimp Multiplier",
                    maxLevel = 5,
                    basePrice = 150,
                    priceGrowthMultiplier = 1.6f,
                    effectPerLevel = 0.10f,
                    unlockGoal = UnlockGoalDefinition.None()
                },
                new PermanentUpgradeDefinition
                {
                    id = PlayerUnlockableIds.ScoreMultiplierUpgrade,
                    displayName = "Score Multiplier",
                    maxLevel = 5,
                    basePrice = 150,
                    priceGrowthMultiplier = 1.6f,
                    effectPerLevel = 0.10f,
                    unlockGoal = UnlockGoalDefinition.None()
                }
            }
        };

        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        version = Math.Max(1, version);
        skins = NormalizeDefinitions(skins);
        runGadgets = NormalizeDefinitions(runGadgets);
        permanentUpgrades = NormalizeDefinitions(permanentUpgrades);
    }

    private static T[] NormalizeDefinitions<T>(T[] definitions) where T : UnlockableDefinitionBase
    {
        return definitions?
            .Where(definition => definition != null && !string.IsNullOrWhiteSpace(definition.id))
            .Select(definition =>
            {
                definition.Normalize();
                return definition;
            })
            .GroupBy(definition => definition.id)
            .Select(group => group.First())
            .ToArray() ?? Array.Empty<T>();
    }
}

[Serializable]
public abstract class UnlockableDefinitionBase
{
    public string id;
    public string displayName;
    public bool defaultUnlocked;
    public int basePrice;
    public UnlockGoalDefinition unlockGoal = UnlockGoalDefinition.None();

    public virtual void Normalize()
    {
        id = id?.Trim() ?? string.Empty;
        displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
        basePrice = Math.Max(0, basePrice);
        unlockGoal ??= UnlockGoalDefinition.None();
        unlockGoal.Normalize();
    }
}

[Serializable]
public class UnlockableSkinDefinition : UnlockableDefinitionBase
{
}

[Serializable]
public class RunGadgetUnlockDefinition : UnlockableDefinitionBase
{
    public string gameplayId;

    public override void Normalize()
    {
        base.Normalize();
        gameplayId = gameplayId?.Trim() ?? string.Empty;
    }
}

[Serializable]
public class PermanentUpgradeDefinition : UnlockableDefinitionBase
{
    public int maxLevel = 1;
    public float priceGrowthMultiplier = 1f;
    public float effectPerLevel = 0.1f;

    public override void Normalize()
    {
        base.Normalize();
        maxLevel = Math.Max(1, maxLevel);
        priceGrowthMultiplier = Math.Max(1f, priceGrowthMultiplier);
        effectPerLevel = Math.Max(0f, effectPerLevel);
    }
}

[Serializable]
public class UnlockGoalDefinition
{
    public string goalType = "None";
    public long targetValue;

    public static UnlockGoalDefinition None()
    {
        return new UnlockGoalDefinition
        {
            goalType = UnlockGoalTypes.None,
            targetValue = 0
        };
    }

    public void Normalize()
    {
        goalType = string.IsNullOrWhiteSpace(goalType) ? UnlockGoalTypes.None : goalType.Trim();
        targetValue = Math.Max(0, targetValue);
    }
}

public static class UnlockGoalTypes
{
    public const string None = "None";
    public const string BestScore = "BestScore";
    public const string TotalShrimpsCollected = "TotalShrimpsCollected";
    public const string TotalRuns = "TotalRuns";
    public const string TotalPortalsCrossed = "TotalPortalsCrossed";
}
