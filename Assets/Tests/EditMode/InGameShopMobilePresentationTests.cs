using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InGameShopMobilePresentationTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void PlatformPresentation_IsRepeatableAndRestoresAuthoredDesktopState(bool firstHintActive)
        {
            var root = new GameObject("shop");
            root.SetActive(false);
            try
            {
                var first = new GameObject("B"); first.transform.SetParent(root.transform); first.SetActive(firstHintActive);
                var second = new GameObject("B"); second.transform.SetParent(root.transform);
                var label = new GameObject("mobile label"); label.transform.SetParent(root.transform); label.SetActive(false);
                var timer = new GameObject("mobile timer"); timer.transform.SetParent(root.transform); timer.SetActive(false);
                var price = new GameObject("price", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                price.transform.SetParent(root.transform);
                price.fontSize = 90f; price.fontSizeMin = 18f; price.fontSizeMax = 72f; price.enableAutoSizing = false;
                price.textWrappingMode = TextWrappingModes.Normal;
                var view = root.AddComponent<InGameShopMobilePresentation>();
                Set(view, "keyboardHints", new[] { first, second }); Set(view, "mobileBuyLabel", label);
                Set(view, "mobileTimerRoot", timer); Set(view, "priceText", price);
                view.Apply(false);
                Assert.That(first.activeSelf, Is.EqualTo(firstHintActive));
                Assert.That(price.enableAutoSizing, Is.False);
                for (int i = 0; i < 2; i++)
                {
                    view.Apply(true);
                    Assert.That(first.activeSelf || second.activeSelf, Is.False);
                    Assert.That(label.activeSelf && timer.activeSelf, Is.True);
                    Assert.That(price.enableAutoSizing, Is.True);
                    Assert.That(price.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
                    Assert.That(price.fontSizeMax, Is.EqualTo(90f));
                }
                view.Apply(false);
                Assert.That(first.activeSelf, Is.EqualTo(firstHintActive));
                Assert.That(second.activeSelf, Is.True);
                Assert.That(label.activeSelf || timer.activeSelf, Is.False);
                Assert.That(price.enableAutoSizing, Is.False);
                Assert.That(price.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal));
                Assert.That(price.fontSize, Is.EqualTo(90f));
                Assert.That(price.fontSizeMin, Is.EqualTo(18f));
                Assert.That(price.fontSizeMax, Is.EqualTo(72f));
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
