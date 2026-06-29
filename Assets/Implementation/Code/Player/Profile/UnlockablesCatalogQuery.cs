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

    public static UnlockableSkinDefinition GetEquippedSkin()
    {
        return FindSkin(PersistentPlayerProfile.EquippedSkinId);
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
        if (upgrade == null)
        {
            return 1f;
        }

        upgrade.Normalize();
        if (upgrade.IsAdditiveEffect)
        {
            return 1f;
        }

        int level = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgradeId);
        return Math.Max(0f, CalculatePermanentUpgradeEffectValue(upgrade, level));
    }

    public static float GetPermanentUpgradeAdditiveBonus(string upgradeId)
    {
        PermanentUpgradeDefinition upgrade = FindPermanentUpgrade(upgradeId);
        if (upgrade == null)
        {
            return 0f;
        }

        upgrade.Normalize();
        int level = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgradeId);
        float effectValue = CalculatePermanentUpgradeEffectValue(upgrade, level);
        return upgrade.IsAdditiveEffect
            ? Math.Max(0f, effectValue)
            : Math.Max(0f, effectValue - 1f);
    }

    public static float GetPermanentUpgradeEffectValue(string upgradeId)
    {
        PermanentUpgradeDefinition upgrade = FindPermanentUpgrade(upgradeId);
        if (upgrade == null)
        {
            return 0f;
        }

        upgrade.Normalize();
        int level = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgradeId);
        return CalculatePermanentUpgradeEffectValue(upgrade, level);
    }

    private static float CalculatePermanentUpgradeEffectValue(PermanentUpgradeDefinition upgrade, int currentLevel)
    {
        int normalizedLevel = Math.Max(0, currentLevel);
        return upgrade.baseEffectValue + normalizedLevel * Math.Max(0f, upgrade.effectPerLevel);
    }
}
