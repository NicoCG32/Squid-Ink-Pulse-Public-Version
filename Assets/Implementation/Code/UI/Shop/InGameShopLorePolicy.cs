public static class InGameShopLorePolicy
{
    public static bool ShouldAttemptFirstDealerExitComic(InGameShopOpenSource openSource, bool isGameOver)
    {
        return openSource == InGameShopOpenSource.DealerFish && !isGameOver;
    }
}
