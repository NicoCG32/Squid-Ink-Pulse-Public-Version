using NUnit.Framework;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PlayerMovementFramePolicyTests
    {
        [Test]
        public void MissingVerticalTarget_StillAdvancesHorizontally()
        {
            PlayerVerticalMovementStep verticalStep = PlayerVerticalMovementPolicy.Resolve(
                currentY: 2f,
                hasPlayerTarget: false,
                playerTargetY: 0f,
                playerVerticalSpeed: 5f,
                externalImpulse: default,
                deltaTime: 0.1f);

            Vector2 next = PlayerMovementFramePolicy.ResolveNextPosition(
                currentPosition: new Vector2(10f, 2f),
                horizontalSpeed: 5f,
                verticalStep,
                minY: -4f,
                maxY: 4f,
                deltaTime: 0.1f);

            Assert.That(verticalStep.HasMovement, Is.False);
            Assert.That(next.x, Is.EqualTo(10.5f).Within(0.0001f));
            Assert.That(next.y, Is.EqualTo(2f));
        }

        [Test]
        public void VerticalTarget_ComposesWithHorizontalAutoscrollAndClamp()
        {
            PlayerVerticalMovementStep verticalStep = PlayerVerticalMovementPolicy.Resolve(
                currentY: 3.8f,
                hasPlayerTarget: true,
                playerTargetY: 10f,
                playerVerticalSpeed: 5f,
                externalImpulse: default,
                deltaTime: 0.1f);

            Vector2 next = PlayerMovementFramePolicy.ResolveNextPosition(
                currentPosition: new Vector2(10f, 3.8f),
                horizontalSpeed: 5f,
                verticalStep,
                minY: -4f,
                maxY: 4f,
                deltaTime: 0.1f);

            Assert.That(next.x, Is.EqualTo(10.5f).Within(0.0001f));
            Assert.That(next.y, Is.EqualTo(4f));
        }
    }
}
