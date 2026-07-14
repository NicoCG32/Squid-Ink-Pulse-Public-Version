using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InGameShopOverlayPresenterTests
    {
        [Test]
        public void PresentOffer_AppliesIconTextsAndBuyState()
        {
            OverlayFixture fixture = CreateFixture();
            try
            {
                Sprite icon = fixture.CreateSprite();

                fixture.Presenter.PresentOffer(icon, Color.red, price: 12, remainingSeconds: 4.2f, canBuy: true);

                Assert.That(fixture.GadgetImage.enabled, Is.True);
                Assert.That(fixture.GadgetImage.sprite, Is.SameAs(icon));
                Assert.That(fixture.GadgetImage.color, Is.EqualTo(Color.red));
                Assert.That(fixture.PriceText.text, Is.EqualTo("12"));
                Assert.That(fixture.TimerText.text, Is.EqualTo("5"));
                Assert.That(fixture.BuyButton.interactable, Is.True);
                Assert.That(fixture.InsufficientFundsText.gameObject.activeSelf, Is.False);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void HideImmediate_HidesOverlayClearsIconAndDisablesButton()
        {
            OverlayFixture fixture = CreateFixture();
            try
            {
                fixture.Presenter.Show();
                fixture.Presenter.PresentOffer(fixture.CreateSprite(), Color.white, price: 10, remainingSeconds: 3f, canBuy: true);
                fixture.Presenter.SetInsufficientFundsVisible(true);

                fixture.Presenter.HideImmediate();

                Assert.That(fixture.Root.activeSelf, Is.False);
                Assert.That(fixture.CanvasGroup.alpha, Is.Zero);
                Assert.That(fixture.CanvasGroup.interactable, Is.False);
                Assert.That(fixture.CanvasGroup.blocksRaycasts, Is.False);
                Assert.That(fixture.GadgetImage.enabled, Is.False);
                Assert.That(fixture.BuyButton.interactable, Is.False);
                Assert.That(fixture.InsufficientFundsText.gameObject.activeSelf, Is.False);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void WireBuyButton_ReplacesSameListenerWithoutDuplicatingIt()
        {
            OverlayFixture fixture = CreateFixture();
            try
            {
                int invocations = 0;
                UnityAction action = () => invocations++;

                fixture.Presenter.WireBuyButton(action);
                fixture.Presenter.WireBuyButton(action);
                fixture.BuyButton.onClick.Invoke();

                Assert.That(invocations, Is.EqualTo(1));
                Assert.That(fixture.BuyButton.targetGraphic.raycastTarget, Is.True);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void AnimateAttentionTexts_UsesCachedBaseScale()
        {
            OverlayFixture fixture = CreateFixture();
            try
            {
                fixture.PriceText.rectTransform.localScale = new Vector3(2f, 2f, 2f);
                fixture.BuyKeyText.rectTransform.localScale = new Vector3(3f, 3f, 3f);
                fixture.Presenter.CacheAnimatedTextScales();

                fixture.Presenter.AnimateAttentionTexts(unscaledTime: 0.25f, amplitude: 0.5f, frequency: 1f);

                Assert.That(fixture.PriceText.rectTransform.localScale.x, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(fixture.BuyKeyText.rectTransform.localScale.x, Is.EqualTo(4.5f).Within(0.0001f));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private static OverlayFixture CreateFixture()
        {
            GameObject root = new("InGameShopMenu");
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            Image gadgetImage = CreateChild<Image>(root.transform, "Gadget");
            TMP_Text priceText = CreateChild<TextMeshProUGUI>(root.transform, "Precio");
            TMP_Text buyKeyText = CreateChild<TextMeshProUGUI>(root.transform, "B");
            TMP_Text insufficientFundsText = CreateChild<TextMeshProUGUI>(root.transform, "SinSaldo");
            TMP_Text timerText = CreateChild<TextMeshProUGUI>(root.transform, "Tiempo");

            GameObject buttonObject = new("Comprar");
            buttonObject.transform.SetParent(root.transform);
            Image buttonImage = buttonObject.AddComponent<Image>();
            Button buyButton = buttonObject.AddComponent<Button>();
            buyButton.targetGraphic = buttonImage;

            InGameShopOverlayPresenter presenter = new(
                root,
                canvasGroup,
                gadgetImage,
                priceText,
                buyKeyText,
                buyButton,
                insufficientFundsText,
                timerText);

            return new OverlayFixture(
                root,
                canvasGroup,
                gadgetImage,
                priceText,
                buyKeyText,
                buyButton,
                insufficientFundsText,
                timerText,
                presenter);
        }

        private static T CreateChild<T>(Transform parent, string name)
            where T : Component
        {
            GameObject child = new(name);
            child.transform.SetParent(parent);
            return child.AddComponent<T>();
        }

        private sealed class OverlayFixture
        {
            private readonly Texture2D texture;
            private Sprite sprite;

            public OverlayFixture(
                GameObject root,
                CanvasGroup canvasGroup,
                Image gadgetImage,
                TMP_Text priceText,
                TMP_Text buyKeyText,
                Button buyButton,
                TMP_Text insufficientFundsText,
                TMP_Text timerText,
                InGameShopOverlayPresenter presenter)
            {
                Root = root;
                CanvasGroup = canvasGroup;
                GadgetImage = gadgetImage;
                PriceText = priceText;
                BuyKeyText = buyKeyText;
                BuyButton = buyButton;
                InsufficientFundsText = insufficientFundsText;
                TimerText = timerText;
                Presenter = presenter;
                texture = new Texture2D(1, 1);
            }

            public GameObject Root { get; }
            public CanvasGroup CanvasGroup { get; }
            public Image GadgetImage { get; }
            public TMP_Text PriceText { get; }
            public TMP_Text BuyKeyText { get; }
            public Button BuyButton { get; }
            public TMP_Text InsufficientFundsText { get; }
            public TMP_Text TimerText { get; }
            public InGameShopOverlayPresenter Presenter { get; }

            public Sprite CreateSprite()
            {
                sprite ??= Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
                return sprite;
            }

            public void Destroy()
            {
                if (sprite != null)
                {
                    Object.DestroyImmediate(sprite);
                }

                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(Root);
            }
        }
    }
}
