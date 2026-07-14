using NUnit.Framework;

public sealed class InGameShopLorePolicyTests
{
    [Test]
    public void ShouldAttemptFirstDealerExitComic_ReturnsTrue_ForDealerFishWhileGameIsActive()
    {
        Assert.IsTrue(InGameShopLorePolicy.ShouldAttemptFirstDealerExitComic(
            InGameShopOpenSource.DealerFish,
            isGameOver: false));
    }

    [TestCase(InGameShopOpenSource.Timed)]
    [TestCase(InGameShopOpenSource.Tutorial)]
    public void ShouldAttemptFirstDealerExitComic_ReturnsFalse_ForNonDealerSources(InGameShopOpenSource openSource)
    {
        Assert.IsFalse(InGameShopLorePolicy.ShouldAttemptFirstDealerExitComic(
            openSource,
            isGameOver: false));
    }

    [Test]
    public void ShouldAttemptFirstDealerExitComic_ReturnsFalse_ForDealerFishAfterGameOver()
    {
        Assert.IsFalse(InGameShopLorePolicy.ShouldAttemptFirstDealerExitComic(
            InGameShopOpenSource.DealerFish,
            isGameOver: true));
    }
}
