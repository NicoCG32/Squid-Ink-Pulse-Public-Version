public static class PermanentUpgradeEffectResolver
{
    public static float InkPulseDurationMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.InkPulseDurationUpgrade);

    public static float InkPulseRechargeRateBonus =>
        UnlockablesCatalogQuery.GetPermanentUpgradeAdditiveBonus(PlayerUnlockableIds.InkPulseRechargeRateUpgrade);

    public static float ShrimpRewardMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.ShrimpMultiplierUpgrade);

    public static float PointsMultiplier =>
        UnlockablesCatalogQuery.GetPermanentUpgradeMultiplier(PlayerUnlockableIds.PointsMultiplierUpgrade);

    public static float ScoreMultiplier =>
        PointsMultiplier;
}
