using NUnit.Framework;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileMenuCanvasScalePolicyTests
    {
        [TestCase(2560, 1600, 0f)]
        [TestCase(1600, 2560, 0f)]
        [TestCase(1920, 1080, 1f)]
        [TestCase(2340, 1080, 1f)]
        [TestCase(2400, 1080, 1f)]
        [TestCase(1220, 2712, 1f)]
        public void Mobile_KeepsReferenceCompositionInsideLandscapeViewport(int width, int height, float expected)
        {
            Assert.That(MobileMenuCanvasScalePolicy.ResolveMatch(true, width, height,
                new Vector2(1920f, 1080f), 0.5f), Is.EqualTo(expected));

            // Verify the resulting logical viewport contains the authored
            // 1920x1080 composition on both axes, including narrower tablets.
            float longSide = Mathf.Max(width, height);
            float shortSide = Mathf.Min(width, height);
            float scale = expected == 1f ? shortSide / 1080f : longSide / 1920f;
            Assert.That(longSide / scale, Is.GreaterThanOrEqualTo(1919.99f));
            Assert.That(shortSide / scale, Is.GreaterThanOrEqualTo(1079.99f));
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void Desktop_PreservesAuthoredMatch(float authored)
        {
            Assert.That(MobileMenuCanvasScalePolicy.ResolveMatch(false, 2560, 1600,
                new Vector2(1920f, 1080f), authored), Is.EqualTo(authored));
        }

        [TestCase(0, 1080, 1920f, 1080f)]
        [TestCase(1920, -1, 1920f, 1080f)]
        [TestCase(1920, 1080, 0f, 1080f)]
        public void InvalidDimensions_PreserveAuthoredMatch(int width, int height, float referenceWidth, float referenceHeight)
        {
            Assert.That(MobileMenuCanvasScalePolicy.ResolveMatch(true, width, height,
                new Vector2(referenceWidth, referenceHeight), 0.5f), Is.EqualTo(0.5f));
        }
    }
}
