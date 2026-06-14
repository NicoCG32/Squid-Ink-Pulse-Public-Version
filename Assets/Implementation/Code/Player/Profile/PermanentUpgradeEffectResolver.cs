public static class PermanentUpgradeEffectResolver
{
    public static float InkPulseDurationMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.InkPulseDurationUpgrade);

    public static float InkPulseRechargeRateMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.InkPulseRechargeRateUpgrade);

    public static float ShrimpRewardMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.ShrimpMultiplierUpgrade);

    public static float ScoreMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.ScoreMultiplierUpgrade);
}
