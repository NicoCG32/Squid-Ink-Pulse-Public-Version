using NUnit.Framework;

public sealed class LoreComicEntrySelectorTests
{
    [Test]
    public void FindEntry_PrefersExactZoneOverUnknownFallback()
    {
        LoreComicEntry fallback = Entry(LoreComicEvent.Defeat, LoreComicZone.Unknown);
        LoreComicEntry exact = Entry(LoreComicEvent.Defeat, LoreComicZone.Abyssopelagic);

        LoreComicEntry selected = LoreComicEntrySelector.FindEntry(
            new[] { fallback, exact },
            new LoreComicRequest(LoreComicEvent.Defeat, LoreComicZone.Abyssopelagic));

        Assert.AreSame(exact, selected);
    }

    [Test]
    public void FindEntry_UsesUnknownFallback_WhenExactZoneIsMissing()
    {
        LoreComicEntry fallback = Entry(LoreComicEvent.ShopInGameFirst, LoreComicZone.Unknown);

        LoreComicEntry selected = LoreComicEntrySelector.FindEntry(
            new[] { fallback },
            new LoreComicRequest(LoreComicEvent.ShopInGameFirst, LoreComicZone.Epipelagic));

        Assert.AreSame(fallback, selected);
    }

    [Test]
    public void FindEntry_ReturnsNull_WhenEventDoesNotExist()
    {
        LoreComicEntry selected = LoreComicEntrySelector.FindEntry(
            new[] { Entry(LoreComicEvent.Defeat, LoreComicZone.Unknown) },
            new LoreComicRequest(LoreComicEvent.GameStart, LoreComicZone.Unknown));

        Assert.IsNull(selected);
    }

    [Test]
    public void ResolveDisplaySeconds_ClampsNegativeEntryDuration()
    {
        LoreComicEntry entry = Entry(LoreComicEvent.GameStart, LoreComicZone.Unknown);
        entry.displaySeconds = -3f;

        float seconds = LoreComicEntrySelector.ResolveDisplaySeconds(
            entry,
            new LoreComicRequest(LoreComicEvent.GameStart, LoreComicZone.Unknown),
            defaultDisplaySeconds: 2f,
            defaultStartDisplaySeconds: 4f);

        Assert.AreEqual(0f, seconds);
    }

    [Test]
    public void ResolveDisplaySeconds_UsesStartDefaultOnlyForGameStartWithoutEntry()
    {
        float startSeconds = LoreComicEntrySelector.ResolveDisplaySeconds(
            entry: null,
            new LoreComicRequest(LoreComicEvent.GameStart, LoreComicZone.Unknown),
            defaultDisplaySeconds: 2f,
            defaultStartDisplaySeconds: 4f);
        float defeatSeconds = LoreComicEntrySelector.ResolveDisplaySeconds(
            entry: null,
            new LoreComicRequest(LoreComicEvent.Defeat, LoreComicZone.Epipelagic),
            defaultDisplaySeconds: 2f,
            defaultStartDisplaySeconds: 4f);

        Assert.AreEqual(4f, startSeconds);
        Assert.AreEqual(2f, defeatSeconds);
    }

    [Test]
    public void ResolveContinueFlags_UseEntryValuesWhenEntryExists()
    {
        LoreComicEntry entry = Entry(LoreComicEvent.Defeat, LoreComicZone.Unknown);
        entry.waitForContinue = true;
        entry.showContinueButton = true;
        LoreComicRequest request = new(LoreComicEvent.Defeat, LoreComicZone.Epipelagic);

        Assert.IsTrue(LoreComicEntrySelector.ResolveWaitForContinue(entry, request, defaultStartWaitsForContinue: false));
        Assert.IsTrue(LoreComicEntrySelector.ResolveShowContinueButton(entry, request, defaultStartShowsContinueButton: false));
    }

    [Test]
    public void ResolveContinueFlags_UseStartDefaultsOnlyForGameStartWithoutEntry()
    {
        LoreComicRequest startRequest = new(LoreComicEvent.GameStart, LoreComicZone.Unknown);
        LoreComicRequest defeatRequest = new(LoreComicEvent.Defeat, LoreComicZone.Epipelagic);

        Assert.IsTrue(LoreComicEntrySelector.ResolveWaitForContinue(null, startRequest, defaultStartWaitsForContinue: true));
        Assert.IsTrue(LoreComicEntrySelector.ResolveShowContinueButton(null, startRequest, defaultStartShowsContinueButton: true));
        Assert.IsFalse(LoreComicEntrySelector.ResolveWaitForContinue(null, defeatRequest, defaultStartWaitsForContinue: true));
        Assert.IsFalse(LoreComicEntrySelector.ResolveShowContinueButton(null, defeatRequest, defaultStartShowsContinueButton: true));
    }

    private static LoreComicEntry Entry(LoreComicEvent comicEvent, LoreComicZone zone)
    {
        return new LoreComicEntry
        {
            comicEvent = comicEvent,
            zone = zone
        };
    }
}
