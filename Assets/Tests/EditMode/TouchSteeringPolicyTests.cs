using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class TouchSteeringPolicyTests
    {
        [Test]
        public void Availability_RequiresEveryGameplayGateToRemainOpen()
        {
            Assert.That(
                TouchSteeringAvailabilityPolicy.IsAllowed(true, false, true, true, true),
                Is.True);
            Assert.That(
                TouchSteeringAvailabilityPolicy.IsAllowed(false, false, true, true, true),
                Is.False);
            Assert.That(
                TouchSteeringAvailabilityPolicy.IsAllowed(true, true, true, true, true),
                Is.False);
            Assert.That(
                TouchSteeringAvailabilityPolicy.IsAllowed(true, false, false, true, true),
                Is.False);
            Assert.That(
                TouchSteeringAvailabilityPolicy.IsAllowed(true, false, true, false, true),
                Is.False);
            Assert.That(
                TouchSteeringAvailabilityPolicy.IsAllowed(true, false, true, true, false),
                Is.False);
        }

        [Test]
        public void UiPolicy_AcceptsSurfaceDecoration_AndRejectsSelectableOrOutsideTargets()
        {
            var surface = new GameObject("TouchSurface", typeof(RectTransform));
            var decoration = new GameObject("Decoration", typeof(RectTransform), typeof(Image));
            var button = new GameObject("TouchButton", typeof(RectTransform), typeof(Button));
            var outside = new GameObject("Outside", typeof(RectTransform), typeof(Image));
            decoration.transform.SetParent(surface.transform, false);
            button.transform.SetParent(surface.transform, false);

            try
            {
                Assert.That(
                    TouchSteeringUiPolicy.StartedOverInteractiveUi(
                        surface.transform,
                        surface),
                    Is.False);
                Assert.That(
                    TouchSteeringUiPolicy.StartedOverInteractiveUi(
                        surface.transform,
                        decoration),
                    Is.False);
                Assert.That(
                    TouchSteeringUiPolicy.StartedOverInteractiveUi(
                        surface.transform,
                        button),
                    Is.True);
                Assert.That(
                    TouchSteeringUiPolicy.StartedOverInteractiveUi(
                        surface.transform,
                        outside),
                    Is.True);
                Assert.That(
                    TouchSteeringUiPolicy.StartedOverInteractiveUi(
                        surface.transform,
                        null),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(surface);
                Object.DestroyImmediate(outside);
            }
        }
    }
}
