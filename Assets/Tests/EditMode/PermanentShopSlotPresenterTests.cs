using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PermanentShopSlotPresenterTests
    {
        [Test]
        public void PresentSkinSlot_OwnedButNotEquipped_ActivatesPurchasedStatesOnly()
        {
            SlotFixture fixture = CreateSlotFixture();
            try
            {
                PermanentShopSlotPresenter presenter = new(logContext: null);

                presenter.PresentSkinSlot(fixture.Visual, Skin(), isOwned: true, isEquipped: false);

                Assert.That(fixture.Purchased.activeSelf, Is.True);
                Assert.That(fixture.LegacyBuyed.activeSelf, Is.True);
                Assert.That(fixture.Equipped.activeSelf, Is.False);
                Assert.That(fixture.LegacySelected.activeSelf, Is.False);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void PresentSkinSlot_Equipped_ActivatesEquippedStatesOnly()
        {
            SlotFixture fixture = CreateSlotFixture();
            try
            {
                PermanentShopSlotPresenter presenter = new(logContext: null);

                presenter.PresentSkinSlot(fixture.Visual, Skin(), isOwned: true, isEquipped: true);

                Assert.That(fixture.Purchased.activeSelf, Is.False);
                Assert.That(fixture.LegacyBuyed.activeSelf, Is.False);
                Assert.That(fixture.Equipped.activeSelf, Is.True);
                Assert.That(fixture.LegacySelected.activeSelf, Is.True);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private static SlotFixture CreateSlotFixture()
        {
            GameObject root = new("SkinBoton");
            GameObject buttonObject = new(UiButtonContract.ButtonChildName);
            buttonObject.transform.SetParent(root.transform);
            Image fallbackImage = buttonObject.AddComponent<Image>();
            Button button = buttonObject.AddComponent<Button>();
            ButtonVisualState buttonVisualState = buttonObject.AddComponent<ButtonVisualState>();

            GameObject visual = new(UiButtonContract.VisualChildName);
            visual.transform.SetParent(root.transform);
            Image normalImage = AddState(visual.transform, UiButtonContract.NormalStateName).GetComponent<Image>();
            Image highlightedImage = AddState(visual.transform, UiButtonContract.HighlightedStateName).GetComponent<Image>();
            Image pressedImage = AddState(visual.transform, UiButtonContract.PressedStateName).GetComponent<Image>();
            GameObject purchased = AddState(visual.transform, UiButtonContract.PurchasedStateName);
            GameObject equipped = AddState(visual.transform, UiButtonContract.EquippedStateName);
            GameObject legacyBuyed = AddState(visual.transform, UiButtonContract.LegacyBuyedStateName);
            GameObject legacySelected = AddState(visual.transform, UiButtonContract.LegacySelectedStateName);

            purchased.SetActive(false);
            equipped.SetActive(false);
            legacyBuyed.SetActive(false);
            legacySelected.SetActive(false);

            PermanentShopSlotVisual slotVisual = new(
                button,
                buttonVisualState,
                normalImage,
                highlightedImage,
                pressedImage,
                fallbackImage,
                purchased,
                equipped,
                legacyBuyed,
                legacySelected);

            return new SlotFixture(root, slotVisual, purchased, equipped, legacyBuyed, legacySelected);
        }

        private static GameObject AddState(Transform parent, string name)
        {
            GameObject state = new(name);
            state.transform.SetParent(parent);
            state.AddComponent<Image>();
            return state;
        }

        private static UnlockableSkinDefinition Skin()
        {
            return new UnlockableSkinDefinition
            {
                id = "skin.test",
                shopSpriteResourcePath = string.Empty
            };
        }

        private readonly struct SlotFixture
        {
            private readonly GameObject root;

            public SlotFixture(
                GameObject root,
                PermanentShopSlotVisual visual,
                GameObject purchased,
                GameObject equipped,
                GameObject legacyBuyed,
                GameObject legacySelected)
            {
                this.root = root;
                Visual = visual;
                Purchased = purchased;
                Equipped = equipped;
                LegacyBuyed = legacyBuyed;
                LegacySelected = legacySelected;
            }

            public PermanentShopSlotVisual Visual { get; }
            public GameObject Purchased { get; }
            public GameObject Equipped { get; }
            public GameObject LegacyBuyed { get; }
            public GameObject LegacySelected { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
