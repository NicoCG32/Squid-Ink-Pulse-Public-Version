using System;
using System.Reflection;
using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class CoreDataAndPricingTests
    {
        [Test]
        public void ShopPriceCalculator_NullOffer_ReturnsZero()
        {
            int price = ShopPriceCalculator.CalculatePrice(
                offer: null,
                score: 5000,
                scorePriceStep: 1000f,
                globalPriceMultiplier: 2f,
                randomPriceMultiplier: 1.5f);

            Assert.That(price, Is.Zero);
        }

        [Test]
        public void ShopPriceCalculator_AppliesScoreAndPriceMultipliers()
        {
            ShopGadgetOffer offer = CreateOfferWithBasePrice(100);

            int price = ShopPriceCalculator.CalculatePrice(
                offer,
                score: 1500,
                scorePriceStep: 1000f,
                globalPriceMultiplier: 1.25f,
                randomPriceMultiplier: 0.5f);

            Assert.That(price, Is.EqualTo(157));
        }

        [Test]
        public void PlayerProfileNormalize_RepairsInvalidNestedData()
        {
            PlayerProfileSaveData profile = new()
            {
                version = 0,
                permanentUpgrades = new PlayerProfilePermanentUpgradesSaveData
                {
                    inkPulseDurationLevel = -2,
                    scoreMultiplierLevel = -1
                },
                skins = new PlayerProfileSkinsSaveData
                {
                    unlockedSkinIds = new[] { " skin.sonic ", "skin.sonic", "" },
                    equippedSkinId = "skin.missing"
                },
                runGadgetUnlocks = null,
                lore = new PlayerProfileLoreSaveData
                {
                    viewedComicEventIds = new[] { " portal ", "portal", null }
                }
            };

            profile.Normalize();

            Assert.That(profile.version, Is.EqualTo(1));
            Assert.That(profile.permanentUpgrades.inkPulseDurationLevel, Is.Zero);
            Assert.That(profile.permanentUpgrades.scoreMultiplierLevel, Is.Zero);
            Assert.That(profile.skins.unlockedSkinIds, Is.EquivalentTo(new[] { "skin.sonic", PlayerSkinIds.Default }));
            Assert.That(profile.skins.equippedSkinId, Is.EqualTo(PlayerSkinIds.Default));
            Assert.That(profile.runGadgetUnlocks.unlockedRunGadgetIds, Has.Length.EqualTo(2));
            Assert.That(profile.lore.viewedComicEventIds, Is.EqualTo(new[] { "portal" }));
        }

        [Test]
        public void LocalLeaderboardNormalize_FiltersSortsAndLimitsEntries()
        {
            LocalLeaderboardSaveData leaderboard = new()
            {
                version = 0,
                maxEntries = 2,
                entries = new[]
                {
                    Entry("B", 50, "2026-01-02T00:00:00.0000000Z"),
                    null,
                    Entry("C", 100, "2026-01-03T00:00:00.0000000Z"),
                    Entry("A", 100, "2026-01-01T00:00:00.0000000Z"),
                    Entry("Invalid", -10, "2026-01-04T00:00:00.0000000Z")
                }
            };

            leaderboard.Normalize();

            Assert.That(leaderboard.version, Is.EqualTo(1));
            Assert.That(leaderboard.entries, Has.Length.EqualTo(2));
            Assert.That(leaderboard.entries[0].playerName, Is.EqualTo("A"));
            Assert.That(leaderboard.entries[1].playerName, Is.EqualTo("C"));
            Assert.That(leaderboard.entries[0].score, Is.EqualTo(100));
        }

        [Test]
        public void PermanentUpgradePrice_UsesCeilingAndNormalizesNegativeLevel()
        {
            PermanentUpgradeDefinition upgrade = new()
            {
                basePrice = 101,
                priceGrowthMultiplier = 1.5f
            };

            int basePrice = UnlockablesCatalogQuery.CalculatePermanentUpgradePrice(upgrade, -3);
            int levelTwoPrice = UnlockablesCatalogQuery.CalculatePermanentUpgradePrice(upgrade, 2);

            Assert.That(basePrice, Is.EqualTo(101));
            Assert.That(levelTwoPrice, Is.EqualTo(228));
        }

        private static ShopGadgetOffer CreateOfferWithBasePrice(int basePrice)
        {
            ShopGadgetOffer offer = new();
            FieldInfo priceField = typeof(ShopGadgetOffer).GetField(
                "basePriceOverride",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(priceField, Is.Not.Null, "ShopGadgetOffer debe conservar su campo serializado de precio base.");
            priceField.SetValue(offer, basePrice);
            return offer;
        }

        private static LocalLeaderboardEntrySaveData Entry(string name, long score, string timestamp)
        {
            return new LocalLeaderboardEntrySaveData
            {
                playerName = name,
                score = score,
                timestampUtc = timestamp
            };
        }
    }
}
