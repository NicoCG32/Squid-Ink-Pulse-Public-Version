using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class LoreComicMobileLayoutTests
    {
        [TestCase(1920f,1080f,true)]
        [TestCase(1920f,1080f,false)]
        [TestCase(1440f,900f,true)]
        [TestCase(2400f,1080f,false)]
        public void Mobile_ReservesSafeMarginsAndButtonSpaceAndRestoresDesktop(float width,float height,bool showButton)
        {
            var root=new GameObject("Content",typeof(RectTransform));
            try
            {
                var content=(RectTransform)root.transform; content.sizeDelta=new Vector2(width,height);
                var image=new GameObject("Image",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(content,false); image.preserveAspect=true;
                image.rectTransform.sizeDelta=new Vector2(1280f,720f);
                image.rectTransform.anchoredPosition=new Vector2(12f,18f);
                var button=new GameObject("Continue",typeof(RectTransform)).GetComponent<RectTransform>();
                button.SetParent(content,false); button.anchorMin=button.anchorMax=new Vector2(0.5f,0f);
                button.sizeDelta=new Vector2(260f,92f); button.anchoredPosition=new Vector2(0f,118f);
                button.gameObject.SetActive(showButton);
                var layout=root.AddComponent<LoreComicMobileLayout>();
                Set(layout,"comicImage",image); Set(layout,"continueRoot",button);
                layout.Apply(true);
                var corners=new Vector3[4]; image.rectTransform.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point=content.InverseTransformPoint(corner);
                    Assert.That(content.rect.Contains(point),Is.True);
                }
                if (showButton)
                {
                    var buttonCorners=new Vector3[4]; button.GetWorldCorners(buttonCorners);
                    Assert.That(corners[0].y,Is.GreaterThanOrEqualTo(buttonCorners[1].y+23.9f));
                }
                Assert.That(image.preserveAspect,Is.True);
                Vector2 fitted=image.rectTransform.sizeDelta;
                layout.Apply(true); Assert.That(image.rectTransform.sizeDelta,Is.EqualTo(fitted));
                layout.Apply(false);
                Assert.That(image.rectTransform.sizeDelta,Is.EqualTo(new Vector2(1280f,720f)));
                Assert.That(image.rectTransform.anchoredPosition,Is.EqualTo(new Vector2(12f,18f)));
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Set(object target,string name,object value) => target.GetType()
            .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    }
}
