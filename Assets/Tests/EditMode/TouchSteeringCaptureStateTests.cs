using NUnit.Framework;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class TouchSteeringCaptureStateTests
    {
        [TestCase(-1)]
        [TestCase(1048577)]
        public void Begin_CapturesOpaquePointerAndExactScreenPosition(int pointerId)
        {
            var state = new TouchSteeringCaptureState();
            Vector2 position = pointerId < 0 ? Vector2.zero : new Vector2(1830f, 760f);

            bool captured = state.TryBegin(
                pointerId,
                position,
                isAvailable: true,
                startedOverInteractiveUi: false);

            Assert.That(captured, Is.True);
            Assert.That(state.HasActivePointer, Is.True);
            Assert.That(state.ActivePointerId, Is.EqualTo(pointerId));
            Assert.That(state.ScreenPosition, Is.EqualTo(position));
        }

        [Test]
        public void SecondPointer_CannotReplaceMoveOrReleaseTheFirst()
        {
            var state = new TouchSteeringCaptureState();
            Vector2 firstPosition = new(300f, 400f);
            Assert.That(state.TryBegin(11, firstPosition, true, false), Is.True);

            Assert.That(
                state.TryBegin(22, new Vector2(1200f, 700f), true, false),
                Is.False);
            Assert.That(state.TryMove(22, new Vector2(1500f, 900f)), Is.False);
            Assert.That(state.TryEnd(22), Is.False);
            Assert.That(state.ActivePointerId, Is.EqualTo(11));
            Assert.That(state.ScreenPosition, Is.EqualTo(firstPosition));

            Vector2 ownedDragPosition = new(500f, 600f);
            Assert.That(state.TryMove(11, ownedDragPosition), Is.True);
            Assert.That(state.ScreenPosition, Is.EqualTo(ownedDragPosition));
        }

        [Test]
        public void EndAndCancel_AreIdempotent_AndRequireAFreshDown()
        {
            var state = new TouchSteeringCaptureState();
            Assert.That(state.TryBegin(7, new Vector2(10f, 20f), true, false), Is.True);
            Assert.That(state.TryEnd(7), Is.True);
            Assert.That(state.TryEnd(7), Is.False);
            Assert.That(state.Cancel(), Is.False);
            Assert.That(state.TryMove(7, new Vector2(30f, 40f)), Is.False);

            Assert.That(state.TryBegin(8, Vector2.zero, true, false), Is.True);
            Assert.That(state.Cancel(), Is.True);
            Assert.That(state.TryMove(8, new Vector2(50f, 60f)), Is.False);
            Assert.That(state.TryBegin(9, new Vector2(70f, 80f), true, false), Is.True);
        }

        [Test]
        public void BlockedDown_NeverAdoptsALaterDrag()
        {
            var state = new TouchSteeringCaptureState();

            Assert.That(state.TryBegin(1, new Vector2(20f, 30f), true, true), Is.False);
            Assert.That(state.TryMove(1, new Vector2(200f, 300f)), Is.False);
            Assert.That(state.TryBegin(2, new Vector2(40f, 50f), false, false), Is.False);
            Assert.That(state.TryMove(2, new Vector2(400f, 500f)), Is.False);

            Assert.That(state.TryBegin(3, new Vector2(60f, 70f), true, false), Is.True);
        }
    }
}
