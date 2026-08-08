public static class TouchSteeringAvailabilityPolicy
{
    public static bool IsAllowed(
        bool isGameplayPlaying,
        bool isShopBlocking,
        bool isOverlayInteractionAllowed,
        bool isTimeAdvancing,
        bool isReaderEnabled)
    {
        return isGameplayPlaying
            && !isShopBlocking
            && isOverlayInteractionAllowed
            && isTimeAdvancing
            && isReaderEnabled;
    }
}
