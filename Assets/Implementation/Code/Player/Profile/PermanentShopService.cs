using System;

public static class PermanentShopService
{
    public static PermanentShopPurchaseResult TryPurchaseSkin(string skinId)
    {
        UnlockableSkinDefinition skin = UnlockablesCatalogQuery.FindSkin(skinId);
        if (skin == null)
        {
            return PermanentShopPurchaseResult.UnknownItem;
        }

        if (!skin.defaultUnlocked && !UnlockablesCatalogQuery.IsGoalMet(skin.unlockGoal))
        {
            return PermanentShopPurchaseResult.LockedByGoal;
        }

        if (PersistentPlayerProfile.HasUnlockedSkin(skin.id))
        {
            return PermanentShopPurchaseResult.AlreadyOwned;
        }

        if (skin.basePrice <= 0)
        {
            return PermanentShopPurchaseResult.InvalidPrice;
        }

        if (!ShrimpRuntimeWallet.TrySpend(skin.basePrice))
        {
            return PermanentShopPurchaseResult.InsufficientShrimps;
        }

        PersistentPlayerProfile.UnlockSkin(skin.id);
        return PermanentShopPurchaseResult.Success;
    }

    public static PermanentShopPurchaseResult TryEquipSkin(string skinId)
    {
        return PersistentPlayerProfile.TryEquipSkin(skinId)
            ? PermanentShopPurchaseResult.Success
            : PermanentShopPurchaseResult.LockedByGoal;
    }

    public static PermanentShopPurchaseResult TryPurchasePermanentUpgradeLevel(string upgradeId)
    {
        PermanentUpgradeDefinition upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId);
        if (upgrade == null)
        {
            return PermanentShopPurchaseResult.UnknownItem;
        }

        if (!upgrade.defaultUnlocked && !UnlockablesCatalogQuery.IsGoalMet(upgrade.unlockGoal))
        {
            return PermanentShopPurchaseResult.LockedByGoal;
        }

        int currentLevel = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgrade.id);
        if (currentLevel >= upgrade.maxLevel)
        {
            return PermanentShopPurchaseResult.MaxLevelReached;
        }

        int price = UnlockablesCatalogQuery.CalculatePermanentUpgradePrice(upgrade, currentLevel);
        if (price <= 0)
        {
            return PermanentShopPurchaseResult.InvalidPrice;
        }

        if (!ShrimpRuntimeWallet.TrySpend(price))
        {
            return PermanentShopPurchaseResult.InsufficientShrimps;
        }

        PersistentPlayerProfile.SetPermanentUpgradeLevel(upgrade.id, currentLevel + 1);
        return PermanentShopPurchaseResult.Success;
    }

    public static int GetPermanentUpgradePrice(string upgradeId)
    {
        PermanentUpgradeDefinition upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId);
        if (upgrade == null)
        {
            return 0;
        }

        int currentLevel = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgrade.id);
        if (currentLevel >= upgrade.maxLevel)
        {
            return 0;
        }

        return UnlockablesCatalogQuery.CalculatePermanentUpgradePrice(upgrade, currentLevel);
    }

    public static bool IsPermanentUpgradeMaxed(string upgradeId)
    {
        PermanentUpgradeDefinition upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId);
        return upgrade != null && PersistentPlayerProfile.GetPermanentUpgradeLevel(upgrade.id) >= Math.Max(1, upgrade.maxLevel);
    }
}
