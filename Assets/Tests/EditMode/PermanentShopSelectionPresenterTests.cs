using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PermanentShopSelectionPresenterTests
    {
        [Test]
        public void ForUpgrade_MaxLevel_DisablesPurchaseAndShowsMax()
        {
            PermanentUpgradeDefinition upgrade = Upgrade(maxLevel: 3);

            PermanentShopSelectionPresentation presentation = PermanentShopSelectionPresenter.ForUpgrade(
                upgrade,
                currentLevel: 3,
                isGoalMet: true,
                nextPrice: 0,
                FormatPrice);

            Assert.That(presentation.Price, Is.EqualTo("MAX"));
            Assert.That(presentation.State, Is.EqualTo("MAX"));
            Assert.That(presentation.CanPurchase, Is.False);
            Assert.That(presentation.ShowUpgradeLevel, Is.True);
            Assert.That(presentation.UpgradeLevel, Is.EqualTo(3));
            Assert.That(presentation.UpgradeMaxLevel, Is.EqualTo(3));
        }

        [Test]
        public void ForUpgrade_LockedGoal_DisablesPurchaseAndShowsLocked()
        {
            PermanentShopSelectionPresentation presentation = PermanentShopSelectionPresenter.ForUpgrade(
                Upgrade(maxLevel: 3),
                currentLevel: 1,
                isGoalMet: false,
                nextPrice: 250,
                FormatPrice);

            Assert.That(presentation.Price, Is.EqualTo("$250"));
            Assert.That(presentation.State, Is.EqualTo("BLOQUEADO"));
            Assert.That(presentation.CanPurchase, Is.False);
        }

        [Test]
        public void ForSkin_UnownedAndUnlocked_AllowsPurchaseWithPrice()
        {
            PermanentShopSelectionPresentation presentation = PermanentShopSelectionPresenter.ForSkin(
                Skin("skin.test", basePrice: 120),
                isOwned: false,
                isEquipped: false,
                isGoalMet: true,
                FormatPrice);

            Assert.That(presentation.Price, Is.EqualTo("$120"));
            Assert.That(presentation.State, Is.Empty);
            Assert.That(presentation.CanPurchase, Is.True);
        }

        [Test]
        public void ForSkin_EquippedNonDefault_AllowsUnequip()
        {
            PermanentShopSelectionPresentation presentation = PermanentShopSelectionPresenter.ForSkin(
                Skin("skin.test", basePrice: 120),
                isOwned: true,
                isEquipped: true,
                isGoalMet: true,
                FormatPrice);

            Assert.That(presentation.Price, Is.EqualTo("QUITAR"));
            Assert.That(presentation.State, Is.EqualTo("EQUIPADA"));
            Assert.That(presentation.CanPurchase, Is.True);
        }

        [Test]
        public void ForSkin_EquippedDefault_DisablesAction()
        {
            PermanentShopSelectionPresentation presentation = PermanentShopSelectionPresenter.ForSkin(
                Skin(PlayerSkinIds.Default, basePrice: 0),
                isOwned: true,
                isEquipped: true,
                isGoalMet: true,
                FormatPrice);

            Assert.That(presentation.Price, Is.EqualTo("EQUIPADA"));
            Assert.That(presentation.State, Is.EqualTo("EQUIPADA"));
            Assert.That(presentation.CanPurchase, Is.False);
        }

        private static PermanentUpgradeDefinition Upgrade(int maxLevel)
        {
            return new PermanentUpgradeDefinition
            {
                id = PlayerUnlockableIds.InkPulseDurationUpgrade,
                displayName = "Upgrade",
                description = "Description",
                maxLevel = maxLevel
            };
        }

        private static UnlockableSkinDefinition Skin(string id, int basePrice)
        {
            return new UnlockableSkinDefinition
            {
                id = id,
                displayName = "Skin",
                description = "Description",
                basePrice = basePrice
            };
        }

        private static string FormatPrice(int amount)
        {
            return $"${amount}";
        }
    }
}
