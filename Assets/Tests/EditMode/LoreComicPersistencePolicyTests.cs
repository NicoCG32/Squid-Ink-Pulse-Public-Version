using NUnit.Framework;

public sealed class LoreComicPersistencePolicyTests
{
    [TestCase(LoreComicEvent.PortalEpipelagicToAbyssopelagic)]
    [TestCase(LoreComicEvent.PortalAbyssopelagicToEpipelagic)]
    [TestCase(LoreComicEvent.ShopInGameFirst)]
    [TestCase(LoreComicEvent.ShopInGameLastPurchased)]
    [TestCase(LoreComicEvent.ShopInGameLastNoPurchase)]
    public void BuildPersistentComicEventId_ReturnsEventName_ForPersistentEvents(LoreComicEvent comicEvent)
    {
        string eventId = LoreComicPersistencePolicy.BuildPersistentComicEventId(
            new LoreComicRequest(comicEvent, LoreComicZone.Epipelagic));

        Assert.AreEqual(comicEvent.ToString(), eventId);
    }

    [TestCase(LoreComicEvent.GameStart)]
    [TestCase(LoreComicEvent.Defeat)]
    [TestCase(LoreComicEvent.ScoreMilestone)]
    [TestCase(LoreComicEvent.None)]
    public void BuildPersistentComicEventId_ReturnsNull_ForNonPersistentEvents(LoreComicEvent comicEvent)
    {
        string eventId = LoreComicPersistencePolicy.BuildPersistentComicEventId(
            new LoreComicRequest(comicEvent, LoreComicZone.Epipelagic));

        Assert.IsNull(eventId);
    }
}
