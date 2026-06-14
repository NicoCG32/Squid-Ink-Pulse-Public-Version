using System;
using System.Linq;

public static class UnlockablesCatalogQuery
{
    public static RunGadgetUnlockDefinition FindRunGadget(string gadgetUnlockId)
    {
        UnlockablesCatalogSaveData catalog = PersistentPlayerProfile.UnlockablesCatalog;
        return catalog.runGadgets.FirstOrDefault(gadget => gadget.id == gadgetUnlockId);
    }

    public static RunGadgetUnlockDefinition FindRunGadget(GadgetId gadget)
    {
        return FindRunGadget(GadgetCatalog.GetUnlockId(gadget));
    }

    public static UnlockableSkinDefinition FindSkin(string skinId)
    {
        UnlockablesCatalogSaveData catalog = PersistentPlayerProfile.UnlockablesCatalog;
        return catalog.skins.FirstOrDefault(skin => skin.id == skinId);
    }

    public static PermanentUpgradeDefinition FindPermanentUpgrade(string upgradeId)
    {
        UnlockablesCatalogSaveData catalog = PersistentPlayerProfile.UnlockablesCatalog;
        return catalog.permanentUpgrades.FirstOrDefault(upgrade => upgrade.id == upgradeId);
    }

    public static int CalculatePermanentUpgradePrice(PermanentUpgradeDefinition upgrade, int currentLevel)
    {
        if (upgrade == null || upgrade.basePrice <= 0)
        {
            return 0;
        }

        int normalizedLevel = Math.Max(0, currentLevel);
        float scaledPrice = upgrade.basePrice * (float)Math.Pow(upgrade.priceGrowthMultiplier, normalizedLevel);
        return Math.Max(0, (int)Math.Ceiling(scaledPrice));
    }

    public static bool IsGoalMet(UnlockGoalDefinition goal)
    {
        if (goal == null)
        {
            return true;
        }

        goal.Normalize();
        PlayerRecordsSaveData records = PersistentPlayerProfile.Records;
        return goal.goalType switch
        {
            UnlockGoalTypes.None => true,
            UnlockGoalTypes.BestScore => records.bestScore >= goal.targetValue,
            UnlockGoalTypes.TotalShrimpsCollected => records.totalShrimpsCollected >= goal.targetValue,
            UnlockGoalTypes.TotalRuns => records.totalRuns >= goal.targetValue,
            UnlockGoalTypes.TotalPortalsCrossed => records.totalPortalsCrossed >= goal.targetValue,
            _ => false
        };
    }

    public static float GetPermanentUpgradeMultiplier(string upgradeId)
    {
        PermanentUpgradeDefinition upgrade = FindPermanentUpgrade(upgradeId);
        int level = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgradeId);
        return 1f + Math.Max(0, level) * Math.Max(0f, upgrade?.effectPerLevel ?? 0f);
    }
}
