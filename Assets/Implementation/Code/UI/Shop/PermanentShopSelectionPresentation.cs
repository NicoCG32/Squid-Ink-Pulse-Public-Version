using System;

public readonly struct PermanentShopSelectionPresentation
{
    public PermanentShopSelectionPresentation(
        string displayName,
        string description,
        string price,
        string state,
        bool canPurchase,
        bool showUpgradeLevel = false,
        int upgradeLevel = 0,
        int upgradeMaxLevel = 10)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        Price = price ?? string.Empty;
        State = state ?? string.Empty;
        CanPurchase = canPurchase;
        ShowUpgradeLevel = showUpgradeLevel;
        UpgradeLevel = Math.Max(0, upgradeLevel);
        UpgradeMaxLevel = Math.Max(1, upgradeMaxLevel);
    }

    public static PermanentShopSelectionPresentation Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        canPurchase: false);

    public string DisplayName { get; }
    public string Description { get; }
    public string Price { get; }
    public string State { get; }
    public bool CanPurchase { get; }
    public bool ShowUpgradeLevel { get; }
    public int UpgradeLevel { get; }
    public int UpgradeMaxLevel { get; }
}

public static class PermanentShopSelectionPresenter
{
    public static PermanentShopSelectionPresentation ForUpgrade(
        PermanentUpgradeDefinition upgrade,
        int currentLevel,
        bool isGoalMet,
        int nextPrice,
        Func<int, string> formatPrice)
    {
        if (upgrade == null)
        {
            return PermanentShopSelectionPresentation.Empty;
        }

        bool isMaxed = currentLevel >= upgrade.maxLevel;
        string price = nextPrice > 0 ? FormatPrice(formatPrice, nextPrice) : "MAX";
        string state = !isGoalMet ? "BLOQUEADO" : isMaxed ? "MAX" : string.Empty;

        return new PermanentShopSelectionPresentation(
            upgrade.displayName,
            upgrade.description,
            price,
            state,
            canPurchase: isGoalMet && !isMaxed,
            showUpgradeLevel: true,
            upgradeLevel: currentLevel,
            upgradeMaxLevel: upgrade.maxLevel);
    }

    public static PermanentShopSelectionPresentation ForSkin(
        UnlockableSkinDefinition skin,
        bool isOwned,
        bool isEquipped,
        bool isGoalMet,
        Func<int, string> formatPrice)
    {
        if (skin == null)
        {
            return PermanentShopSelectionPresentation.Empty;
        }

        bool canUnequipToDefault = isOwned
            && isEquipped
            && !string.Equals(skin.id, PlayerSkinIds.Default, StringComparison.Ordinal);

        string price = isOwned
            ? isEquipped
                ? canUnequipToDefault ? "QUITAR" : "EQUIPADA"
                : "USAR"
            : FormatPrice(formatPrice, skin.basePrice);
        string state = !isGoalMet ? "BLOQUEADO" : isEquipped ? "EQUIPADA" : string.Empty;

        return new PermanentShopSelectionPresentation(
            skin.displayName,
            skin.description,
            price,
            state,
            canPurchase: isGoalMet && (!isOwned || !isEquipped || canUnequipToDefault));
    }

    private static string FormatPrice(Func<int, string> formatPrice, int amount)
    {
        return formatPrice != null ? formatPrice(amount) : amount.ToString();
    }
}
