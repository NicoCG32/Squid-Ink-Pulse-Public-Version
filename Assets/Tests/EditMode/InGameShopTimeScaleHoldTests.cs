using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InGameShopTimeScaleHoldTests
    {
        [Test]
        public void Begin_WhenEnabled_CapturesAndPausesTimeScale()
        {
            float timeScale = 1.25f;
            InGameShopTimeScaleHold hold = new(() => timeScale, value => timeScale = value);

            hold.Begin(shouldHold: true);

            Assert.That(hold.IsHolding, Is.True);
            Assert.That(timeScale, Is.Zero);
        }

        [Test]
        public void Begin_WhenDisabled_DoesNotChangeTimeScale()
        {
            float timeScale = 1.25f;
            InGameShopTimeScaleHold hold = new(() => timeScale, value => timeScale = value);

            hold.Begin(shouldHold: false);

            Assert.That(hold.IsHolding, Is.False);
            Assert.That(timeScale, Is.EqualTo(1.25f));
        }

        [Test]
        public void End_WhenRestoreAllowed_RestoresCapturedTimeScale()
        {
            float timeScale = 0.75f;
            InGameShopTimeScaleHold hold = new(() => timeScale, value => timeScale = value);
            hold.Begin(shouldHold: true);

            bool restored = hold.End(canRestore: true);

            Assert.That(restored, Is.True);
            Assert.That(hold.IsHolding, Is.False);
            Assert.That(timeScale, Is.EqualTo(0.75f));
        }

        [Test]
        public void End_WhenRestoreIsNotAllowed_ReleasesHoldWithoutChangingCurrentTimeScale()
        {
            float timeScale = 1f;
            InGameShopTimeScaleHold hold = new(() => timeScale, value => timeScale = value);
            hold.Begin(shouldHold: true);
            timeScale = 0.25f;

            bool restored = hold.End(canRestore: false);

            Assert.That(restored, Is.False);
            Assert.That(hold.IsHolding, Is.False);
            Assert.That(timeScale, Is.EqualTo(0.25f));
        }
    }
}
