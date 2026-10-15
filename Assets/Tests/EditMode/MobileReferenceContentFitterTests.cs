using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileReferenceContentFitterTests
    {
        [TestCase(1812f, 1080f)] // A 16:9 safe area narrowed by a side cutout.
        [TestCase(2292f, 1080f)] // Extra-wide landscape with the same cutout.
        [TestCase(1920f, 1200f)] // Tablet after root Canvas width matching.
        [TestCase(1600f, 900f)]
        public void Mobile_FitsWholeReferenceCompositionWithSymmetricMargins(float width, float height)
        {
            WithContent((parent, content, fitter) =>
            {
                parent.sizeDelta = new Vector2(width, height);
                content.anchorMin = Vector2.zero;
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(0.1f, 0.9f);
                content.anchoredPosition = new Vector2(73f, -29f);
                content.sizeDelta = new Vector2(300f, 200f);
                content.localScale = new Vector3(0.6f, 1.4f, 2f);

                fitter.Apply(parent.rect.size, true);
                content.ForceUpdateRectTransforms();
                var corners = new Vector3[4];
                content.GetWorldCorners(corners);
                Vector3 minimum = parent.InverseTransformPoint(corners[0]);
                Vector3 maximum = parent.InverseTransformPoint(corners[2]);
                Rect available = parent.rect;
                Assert.That(minimum.x, Is.GreaterThanOrEqualTo(available.xMin - 0.01f));
                Assert.That(minimum.y, Is.GreaterThanOrEqualTo(available.yMin - 0.01f));
                Assert.That(maximum.x, Is.LessThanOrEqualTo(available.xMax + 0.01f));
                Assert.That(maximum.y, Is.LessThanOrEqualTo(available.yMax + 0.01f));
                Assert.That(minimum.x - available.xMin, Is.EqualTo(available.xMax - maximum.x).Within(0.01f));
                Assert.That(minimum.y - available.yMin, Is.EqualTo(available.yMax - maximum.y).Within(0.01f));
                Assert.That(content.rect.size, Is.EqualTo(new Vector2(1920f, 1080f)));
                Assert.That(content.localScale.x, Is.EqualTo(content.localScale.y));
                Assert.That(content.localScale.z, Is.EqualTo(content.localScale.x));
                Assert.That((maximum.x - minimum.x) / (maximum.y - minimum.y),
                    Is.EqualTo(1920f / 1080f).Within(0.0001f));
                Assert.That(Mathf.Min(minimum.x - available.xMin, minimum.y - available.yMin),
                    Is.EqualTo(0f).Within(0.01f), "The composition must use the available width or height.");
                Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(content.anchorMax, Is.EqualTo(content.anchorMin));
                Assert.That(content.pivot, Is.EqualTo(content.anchorMin));
                Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
            });
        }

        [Test]
        public void Mobile_ResizeAndRepeatDoesNotAccumulateScaleOrOffset()
        {
            WithContent((parent, content, fitter) =>
            {
                fitter.Apply(new Vector2(1812f, 1080f), true);
                Vector3 narrowedScale = content.localScale;
                fitter.Apply(new Vector2(1812f, 1080f), true);
                Assert.That(content.localScale, Is.EqualTo(narrowedScale));
                Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));

                fitter.Apply(new Vector2(2400f, 1080f), true);
                Assert.That(content.localScale, Is.EqualTo(Vector3.one));
                fitter.Apply(new Vector2(1812f, 1080f), true);
                Assert.That(content.localScale, Is.EqualTo(narrowedScale));
                Assert.That(content.sizeDelta, Is.EqualTo(new Vector2(1920f, 1080f)));
            });
        }

        [Test]
        public void Desktop_DoesNotChangeAuthoredGeometry()
        {
            WithContent((parent, content, fitter) =>
            {
                SetDistinctGeometry(content);
                Geometry before = new(content);
                fitter.Apply(new Vector2(1812f, 1080f), false);
                before.AssertUnchanged(content);
            });
        }

        private static IEnumerable<Vector2> InvalidSizes => new[]
        {
            Vector2.zero, new Vector2(-1f, 1080f), new Vector2(1920f, 0f),
            new Vector2(float.NaN, 1080f), new Vector2(1920f, float.NaN),
            new Vector2(float.PositiveInfinity, 1080f), new Vector2(1920f, float.NegativeInfinity)
        };

        [TestCaseSource(nameof(InvalidSizes))]
        public void InvalidAvailableSize_DoesNotCollapseOrMoveContent(Vector2 invalid)
        {
            WithContent((parent, content, fitter) =>
            {
                SetDistinctGeometry(content);
                Geometry before = new(content);
                fitter.Apply(invalid, true);
                before.AssertUnchanged(content);
            });
        }

        [TestCaseSource(nameof(InvalidSizes))]
        public void InvalidReferenceSize_DoesNotCollapseOrMoveContent(Vector2 invalid)
        {
            WithContent((parent, content, fitter) =>
            {
                SetDistinctGeometry(content);
                var serialized = new SerializedObject(fitter);
                serialized.FindProperty("referenceResolution").vector2Value = invalid;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Geometry before = new(content);
                fitter.Apply(new Vector2(1812f, 1080f), true);
                before.AssertUnchanged(content);
            });
        }

        private static void SetDistinctGeometry(RectTransform content)
        {
            content.anchorMin = new Vector2(0.1f, 0.2f);
            content.anchorMax = new Vector2(0.8f, 0.9f);
            content.pivot = new Vector2(0.3f, 0.6f);
            content.anchoredPosition3D = new Vector3(34f, -56f, 7f);
            content.sizeDelta = new Vector2(720f, 430f);
            content.localScale = new Vector3(0.6f, 1.4f, 2f);
            content.localRotation = Quaternion.Euler(0f, 0f, 13f);
        }

        private readonly struct Geometry
        {
            private readonly Vector2 anchorMin, anchorMax, pivot, size;
            private readonly Vector3 position, scale;
            private readonly Quaternion rotation;

            public Geometry(RectTransform rect)
            {
                anchorMin = rect.anchorMin; anchorMax = rect.anchorMax; pivot = rect.pivot;
                position = rect.anchoredPosition3D; size = rect.sizeDelta;
                scale = rect.localScale; rotation = rect.localRotation;
            }

            public void AssertUnchanged(RectTransform rect)
            {
                Assert.That(rect.anchorMin, Is.EqualTo(anchorMin));
                Assert.That(rect.anchorMax, Is.EqualTo(anchorMax));
                Assert.That(rect.pivot, Is.EqualTo(pivot));
                Assert.That(rect.anchoredPosition3D, Is.EqualTo(position));
                Assert.That(rect.sizeDelta, Is.EqualTo(size));
                Assert.That(rect.localScale, Is.EqualTo(scale));
                Assert.That(rect.localRotation, Is.EqualTo(rotation));
            }
        }

        private static void WithContent(System.Action<RectTransform, RectTransform, MobileReferenceContentFitter> assertion)
        {
            var parent = new GameObject("SafeArea", typeof(RectTransform));
            var child = new GameObject("TutorialContent", typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            try
            {
                RectTransform available = parent.GetComponent<RectTransform>();
                available.sizeDelta = new Vector2(1920f, 1080f);
                MobileReferenceContentFitter fitter = child.AddComponent<MobileReferenceContentFitter>();
                assertion(available, child.GetComponent<RectTransform>(), fitter);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
