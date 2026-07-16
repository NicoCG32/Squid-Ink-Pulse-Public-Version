public static class LoreComicPersistencePolicy
{
    public static string BuildPersistentComicEventId(LoreComicRequest request)
    {
        return request.ComicEvent switch
        {
            LoreComicEvent.PortalEpipelagicToAbyssopelagic => request.ComicEvent.ToString(),
            LoreComicEvent.PortalAbyssopelagicToEpipelagic => request.ComicEvent.ToString(),
            LoreComicEvent.ShopInGameFirst => request.ComicEvent.ToString(),
            LoreComicEvent.ShopInGameLastPurchased => request.ComicEvent.ToString(),
            LoreComicEvent.ShopInGameLastNoPurchase => request.ComicEvent.ToString(),
            _ => null
        };
    }
}
