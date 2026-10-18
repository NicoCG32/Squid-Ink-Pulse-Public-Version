using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileBackgroundCoverTests
    {
        [TestCase(1920f,1080f)]
        [TestCase(2400f,1080f)]
        [TestCase(1920f,1200f)]
        public void Mobile_CoversViewportWithoutDistortingSprite(float width,float height)
        {
            WithCover((rect,cover)=>
            {
                cover.Apply(new Vector2(width,height),true);
                Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(width));
                Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(height));
                Assert.That(rect.rect.width/rect.rect.height,Is.EqualTo(16f/9f).Within(0.0001f));
                Vector2 size=rect.sizeDelta;
                cover.Apply(new Vector2(width,height),true);
                Assert.That(rect.sizeDelta,Is.EqualTo(size));
                cover.Apply(new Vector2(1920f,1080f),true);
                Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(1920f,1080f)));
            });
        }

        [Test]
        public void Desktop_PreservesAuthoredGeometryAndImageSetting()
        {
            WithCover((rect,cover)=>
            {
                rect.anchorMin=rect.anchorMax=new Vector2(0.2f,0.3f);
                rect.anchoredPosition=new Vector2(17f,-8f);
                rect.sizeDelta=new Vector2(1920f,1080.3f);
                cover.Apply(new Vector2(2560f,1600f),false);
                Assert.That(rect.anchorMin,Is.EqualTo(new Vector2(0.2f,0.3f)));
                Assert.That(rect.anchoredPosition,Is.EqualTo(new Vector2(17f,-8f)));
                Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(1920f,1080.3f)));
                Assert.That(rect.GetComponent<Image>().preserveAspect,Is.False);
            });
        }

        private static void WithCover(System.Action<RectTransform,MobileBackgroundCover> check)
        {
            var texture=new Texture2D(16,9);
            Sprite sprite=Sprite.Create(texture,new Rect(0,0,16,9),Vector2.one*0.5f);
            var root=new GameObject("Cover",typeof(RectTransform),typeof(Image),typeof(MobileBackgroundCover));
            try { root.GetComponent<Image>().sprite=sprite; check((RectTransform)root.transform,root.GetComponent<MobileBackgroundCover>()); }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
    }
}
