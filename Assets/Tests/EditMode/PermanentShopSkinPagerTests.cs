using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PermanentShopSkinPagerTests
    {
        [Test]
        public void SetCatalogSkins_FiltersSkinsWithoutShopSprite()
        {
            PermanentShopSkinPager pager = new(4);

            pager.SetCatalogSkins(new[]
            {
                Skin("skin.visible", "ShopMenu/Skins/Visible"),
                Skin("skin.hidden", string.Empty),
                null
            });

            Assert.That(pager.VisibleSkinCount, Is.EqualTo(1));
            Assert.That(pager.GetSkinAtIndex(0).id, Is.EqualTo("skin.visible"));
        }

        [Test]
        public void NextAndPreviousPage_ClampToAvailablePages()
        {
            PermanentShopSkinPager pager = new(4);
            pager.SetCatalogSkins(new[]
            {
                Skin("skin.1"),
                Skin("skin.2"),
                Skin("skin.3"),
                Skin("skin.4"),
                Skin("skin.5")
            });

            Assert.That(pager.MaxPage, Is.EqualTo(1));

            pager.NextPage();
            pager.NextPage();
            Assert.That(pager.Page, Is.EqualTo(1));

            pager.PreviousPage();
            pager.PreviousPage();
            Assert.That(pager.Page, Is.Zero);
        }

        [Test]
        public void TryGetSkinIndexForSlot_UsesCurrentPageOffset()
        {
            PermanentShopSkinPager pager = new(4);
            pager.SetCatalogSkins(new[]
            {
                Skin("skin.1"),
                Skin("skin.2"),
                Skin("skin.3"),
                Skin("skin.4"),
                Skin("skin.5")
            });

            pager.NextPage();

            bool hasSkin = pager.TryGetSkinIndexForSlot(0, out int skinIndex);
            bool emptySlot = pager.TryGetSkinIndexForSlot(1, out _);

            Assert.That(hasSkin, Is.True);
            Assert.That(skinIndex, Is.EqualTo(4));
            Assert.That(emptySlot, Is.False);
            Assert.That(pager.GetSkinForSlot(0).id, Is.EqualTo("skin.5"));
        }

        private static UnlockableSkinDefinition Skin(string id, string shopSpritePath = "ShopMenu/Skins/Test")
        {
            return new UnlockableSkinDefinition
            {
                id = id,
                shopSpriteResourcePath = shopSpritePath
            };
        }
    }
}
