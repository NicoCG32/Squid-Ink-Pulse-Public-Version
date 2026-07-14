using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PermanentShopLevelMeterTests
    {
        [Test]
        public void CalculateDropStates_EmptyAtZeroLevel()
        {
            PermanentShopLevelDropState[] states = PermanentShopLevelMeter.CalculateDropStates(
                level: 0,
                maxLevel: 10,
                dropCount: 5,
                segmentsPerDrop: 2);

            Assert.That(states, Is.EqualTo(new[]
            {
                PermanentShopLevelDropState.Empty,
                PermanentShopLevelDropState.Empty,
                PermanentShopLevelDropState.Empty,
                PermanentShopLevelDropState.Empty,
                PermanentShopLevelDropState.Empty
            }));
        }

        [Test]
        public void CalculateDropStates_UsesHalfAndFullDrops()
        {
            PermanentShopLevelDropState[] states = PermanentShopLevelMeter.CalculateDropStates(
                level: 3,
                maxLevel: 10,
                dropCount: 5,
                segmentsPerDrop: 2);

            Assert.That(states, Is.EqualTo(new[]
            {
                PermanentShopLevelDropState.Full,
                PermanentShopLevelDropState.Half,
                PermanentShopLevelDropState.Empty,
                PermanentShopLevelDropState.Empty,
                PermanentShopLevelDropState.Empty
            }));
        }

        [Test]
        public void CalculateDropStates_ClampsAtMaxLevel()
        {
            PermanentShopLevelDropState[] states = PermanentShopLevelMeter.CalculateDropStates(
                level: 20,
                maxLevel: 10,
                dropCount: 5,
                segmentsPerDrop: 2);

            Assert.That(states, Is.All.EqualTo(PermanentShopLevelDropState.Full));
        }
    }
}
