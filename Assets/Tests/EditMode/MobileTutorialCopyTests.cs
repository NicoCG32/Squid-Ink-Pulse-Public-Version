using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileTutorialCopyTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void PartialMobileCopyPreservesOtherDescriptionsAndRestoresDesktop(bool isMobile)
        {
            var root = new GameObject("Descriptions");
            try
            {
                TMP_Text first = NewText(root.transform,"First","Original movement");
                TMP_Text second = NewText(root.transform,"Second","Original pulse");
                Type type = typeof(MainMenu).GetNestedType("TutorialComicPage",BindingFlags.NonPublic);
                object page = Activator.CreateInstance(type,true);
                type.GetField("descriptionTexts",BindingFlags.Instance|BindingFlags.NonPublic)
                    .SetValue(page,new TMP_Text[] { first,second,null });
                type.GetField("mobileDescriptionOverrides",BindingFlags.Instance|BindingFlags.NonPublic)
                    .SetValue(page,new[] { "", "Toca INK-PULSO" });
                MethodInfo apply = type.GetMethod("ApplyPlatformCopy");
                apply.Invoke(page,new object[] { isMobile });
                Assert.That(first.text,Is.EqualTo("Original movement"));
                Assert.That(second.text,Is.EqualTo(isMobile ? "Toca INK-PULSO" : "Original pulse"));
                apply.Invoke(page,new object[] { true });
                apply.Invoke(page,new object[] { true });
                Assert.That(second.text,Is.EqualTo("Toca INK-PULSO"));
                apply.Invoke(page,new object[] { false });
                Assert.That(first.text,Is.EqualTo("Original movement"));
                Assert.That(second.text,Is.EqualTo("Original pulse"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static TMP_Text NewText(Transform parent,string name,string copy)
        {
            var root = new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
            root.transform.SetParent(parent,false);
            TMP_Text text = root.GetComponent<TMP_Text>();
            text.text=copy;
            return text;
        }
    }
}
