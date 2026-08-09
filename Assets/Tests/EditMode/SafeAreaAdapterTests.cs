using NUnit.Framework;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class SafeAreaAdapterTests
    {
        [Test]
        public void Resolve_LeftLandscapeCutout_ProducesNormalizedHorizontalInset()
        {
            SafeAreaAnchors anchors = SafeAreaAnchorPolicy.Resolve(
                new Rect(122f, 0f, 2590f, 1220f),
                new Vector2(2712f, 1220f));

            Assert.That(anchors.Minimum.x, Is.EqualTo(122f / 2712f).Within(0.00001f));
            Assert.That(anchors.Minimum.y, Is.Zero);
            Assert.That(anchors.Maximum, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void Resolve_RightLandscapeCutout_ProducesMirroredInset()
        {
            SafeAreaAnchors anchors = SafeAreaAnchorPolicy.Resolve(
                new Rect(0f, 0f, 2590f, 1220f),
                new Vector2(2712f, 1220f));

            Assert.That(anchors.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.Maximum.x, Is.EqualTo(2590f / 2712f).Within(0.00001f));
            Assert.That(anchors.Maximum.y, Is.EqualTo(1f));
        }

        [Test]
        public void Resolve_InvalidScreenOrInvertedArea_FallsBackToFullFrame()
        {
            SafeAreaAnchors zeroScreen = SafeAreaAnchorPolicy.Resolve(
                new Rect(0f, 0f, 100f, 100f),
                Vector2.zero);
            SafeAreaAnchors inverted = SafeAreaAnchorPolicy.Resolve(
                Rect.MinMaxRect(900f, 0f, 100f, 1080f),
                new Vector2(1920f, 1080f));

            Assert.That(zeroScreen.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(zeroScreen.Maximum, Is.EqualTo(Vector2.one));
            Assert.That(inverted.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(inverted.Maximum, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void Adapter_AppliesAnchorsAndClearsOffsetsWithoutAccumulation()
        {
            var parent = new GameObject("SafeAreaParent", typeof(RectTransform));
            var child = new GameObject("SafeAreaTarget", typeof(RectTransform), typeof(SafeAreaAdapter));
            child.transform.SetParent(parent.transform, false);
            try
            {
                RectTransform target = child.GetComponent<RectTransform>();
                target.offsetMin = new Vector2(20f, 30f);
                target.offsetMax = new Vector2(-40f, -50f);
                SafeAreaAdapter adapter = child.GetComponent<SafeAreaAdapter>();

                adapter.Apply(
                    new Rect(100f, 20f, 1800f, 1040f),
                    new Vector2(2000f, 1080f));
                adapter.Apply(
                    new Rect(100f, 20f, 1800f, 1040f),
                    new Vector2(2000f, 1080f));

                Assert.That(target.anchorMin, Is.EqualTo(new Vector2(0.05f, 20f / 1080f)));
                Assert.That(target.anchorMax, Is.EqualTo(new Vector2(0.95f, 1060f / 1080f)));
                Assert.That(target.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(target.offsetMax, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
