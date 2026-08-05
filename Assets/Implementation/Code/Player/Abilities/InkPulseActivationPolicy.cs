public static class InkPulseActivationPolicy
{
    public static bool CanActivate(
        bool isGameplayActive,
        bool isActivationSuppressed,
        bool isShopBlockingActivation,
        bool isPulseActive,
        bool isCharged)
    {
        return isGameplayActive
            && !isActivationSuppressed
            && !isShopBlockingActivation
            && !isPulseActive
            && isCharged;
    }
}
