using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InGameShopOfferTimerTests
    {
        [Test]
        public void Start_ClampsToMinimumDuration()
        {
            InGameShopOfferTimer timer = new();

            timer.Start(0.1f);

            Assert.That(timer.IsRunning, Is.True);
            Assert.That(timer.RemainingSeconds, Is.EqualTo(InGameShopOfferTimer.MinimumDurationSeconds));
        }

        [Test]
        public void Tick_DecreasesTimeAndExpiresAtZero()
        {
            InGameShopOfferTimer timer = new();
            timer.Start(1.5f);

            bool expiredAfterFirstTick = timer.Tick(1f);
            bool expiredAfterSecondTick = timer.Tick(1f);

            Assert.That(expiredAfterFirstTick, Is.False);
            Assert.That(expiredAfterSecondTick, Is.True);
            Assert.That(timer.RemainingSeconds, Is.Zero);
        }

        [Test]
        public void Tick_IgnoresNegativeDelta()
        {
            InGameShopOfferTimer timer = new();
            timer.Start(2f);

            bool expired = timer.Tick(-5f);

            Assert.That(expired, Is.False);
            Assert.That(timer.RemainingSeconds, Is.EqualTo(2f));
        }

        [Test]
        public void Stop_ClearsRunningStateAndRemainingTime()
        {
            InGameShopOfferTimer timer = new();
            timer.Start(2f);

            timer.Stop();

            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.RemainingSeconds, Is.Zero);
        }
    }
}
