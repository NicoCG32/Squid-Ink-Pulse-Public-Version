using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PlayerVerticalMovementPolicyTests
    {
        [Test]
        public void ExternalImpulse_TakesPriorityOverOpposingPlayerTarget()
        {
            PlayerVerticalMovementStep step = PlayerVerticalMovementPolicy.Resolve(
                currentY: 0f,
                hasPlayerTarget: true,
                playerTargetY: -10f,
                playerVerticalSpeed: 5f,
                externalImpulse: new PlayerVerticalImpulseState(3f, 0.5f),
                deltaTime: 0.1f);

            Assert.That(step.Source, Is.EqualTo(PlayerVerticalMovementSource.ExternalImpulse));
            Assert.That(step.NextY, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(step.ExternalImpulse.Velocity, Is.EqualTo(3f));
            Assert.That(step.ExternalImpulse.RemainingSeconds, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void ExternalImpulse_FinalStepUsesOnlyItsRemainingDuration()
        {
            PlayerVerticalMovementStep step = PlayerVerticalMovementPolicy.Resolve(
                currentY: 1f,
                hasPlayerTarget: true,
                playerTargetY: -10f,
                playerVerticalSpeed: 5f,
                externalImpulse: new PlayerVerticalImpulseState(4f, 0.05f),
                deltaTime: 0.1f);

            Assert.That(step.Source, Is.EqualTo(PlayerVerticalMovementSource.ExternalImpulse));
            Assert.That(step.NextY, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(step.ExternalImpulse.Velocity, Is.Zero);
            Assert.That(step.ExternalImpulse.RemainingSeconds, Is.Zero);

            PlayerVerticalMovementStep resumed = PlayerVerticalMovementPolicy.Resolve(
                currentY: step.NextY,
                hasPlayerTarget: true,
                playerTargetY: -10f,
                playerVerticalSpeed: 5f,
                externalImpulse: step.ExternalImpulse,
                deltaTime: 0.1f);

            Assert.That(resumed.Source, Is.EqualTo(PlayerVerticalMovementSource.PlayerTarget));
            Assert.That(resumed.NextY, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void PlayerTarget_ResumesAfterExternalImpulseExpires()
        {
            PlayerVerticalMovementStep step = PlayerVerticalMovementPolicy.Resolve(
                currentY: 2f,
                hasPlayerTarget: true,
                playerTargetY: -3f,
                playerVerticalSpeed: 5f,
                externalImpulse: default,
                deltaTime: 0.2f);

            Assert.That(step.Source, Is.EqualTo(PlayerVerticalMovementSource.PlayerTarget));
            Assert.That(step.NextY, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void PlayerTarget_DoesNotOvershootAtCurrentVerticalSpeed()
        {
            PlayerVerticalMovementStep step = PlayerVerticalMovementPolicy.Resolve(
                currentY: 1f,
                hasPlayerTarget: true,
                playerTargetY: 1.25f,
                playerVerticalSpeed: 20f,
                externalImpulse: default,
                deltaTime: 0.1f);

            Assert.That(step.Source, Is.EqualTo(PlayerVerticalMovementSource.PlayerTarget));
            Assert.That(step.NextY, Is.EqualTo(1.25f).Within(0.0001f));
        }

        [Test]
        public void ExternalImpulse_AtUpperClampKeepsPriorityUntilItsWindowExpires()
        {
            PlayerVerticalMovementStep firstStep = PlayerVerticalMovementPolicy.Resolve(
                currentY: 4.9f,
                hasPlayerTarget: true,
                playerTargetY: -4f,
                playerVerticalSpeed: 5f,
                externalImpulse: new PlayerVerticalImpulseState(10f, 0.2f),
                deltaTime: 0.1f);
            float clampedY = UnityEngine.Mathf.Clamp(firstStep.NextY, -5f, 5f);

            PlayerVerticalMovementStep secondStep = PlayerVerticalMovementPolicy.Resolve(
                currentY: clampedY,
                hasPlayerTarget: true,
                playerTargetY: -4f,
                playerVerticalSpeed: 5f,
                externalImpulse: firstStep.ExternalImpulse,
                deltaTime: 0.1f);

            Assert.That(clampedY, Is.EqualTo(5f));
            Assert.That(firstStep.Source, Is.EqualTo(PlayerVerticalMovementSource.ExternalImpulse));
            Assert.That(firstStep.ExternalImpulse.RemainingSeconds, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(secondStep.Source, Is.EqualTo(PlayerVerticalMovementSource.ExternalImpulse));
            Assert.That(secondStep.ExternalImpulse.RemainingSeconds, Is.Zero);
        }

        [Test]
        public void MissingPlayerTarget_ProducesNoMovement()
        {
            PlayerVerticalMovementStep step = PlayerVerticalMovementPolicy.Resolve(
                currentY: 2f,
                hasPlayerTarget: false,
                playerTargetY: -10f,
                playerVerticalSpeed: 5f,
                externalImpulse: default,
                deltaTime: 0.1f);

            Assert.That(step.Source, Is.EqualTo(PlayerVerticalMovementSource.None));
            Assert.That(step.HasMovement, Is.False);
            Assert.That(step.NextY, Is.EqualTo(2f));
        }

        [Test]
        public void ApplyImpulse_IgnoresInvalidRequests()
        {
            var current = new PlayerVerticalImpulseState(6f, 0.25f);

            PlayerVerticalImpulseState negativeVelocity =
                PlayerVerticalMovementPolicy.ApplyImpulse(current, -1f, 0.8f);
            PlayerVerticalImpulseState zeroDuration =
                PlayerVerticalMovementPolicy.ApplyImpulse(current, 9f, 0f);

            Assert.That(negativeVelocity.Velocity, Is.EqualTo(6f));
            Assert.That(negativeVelocity.RemainingSeconds, Is.EqualTo(0.25f));
            Assert.That(zeroDuration.Velocity, Is.EqualTo(6f));
            Assert.That(zeroDuration.RemainingSeconds, Is.EqualTo(0.25f));
        }

        [Test]
        public void ApplyImpulse_PreservesStrongestVelocityAndLongestWindow()
        {
            PlayerVerticalImpulseState impulse = PlayerVerticalMovementPolicy.ApplyImpulse(
                new PlayerVerticalImpulseState(6f, 0.25f),
                requestedVelocity: 4f,
                requestedDurationSeconds: 0.8f);

            Assert.That(impulse.Velocity, Is.EqualTo(6f));
            Assert.That(impulse.RemainingSeconds, Is.EqualTo(0.8f));

            impulse = PlayerVerticalMovementPolicy.ApplyImpulse(
                impulse,
                requestedVelocity: 9f,
                requestedDurationSeconds: 0.1f);

            Assert.That(impulse.Velocity, Is.EqualTo(9f));
            Assert.That(impulse.RemainingSeconds, Is.EqualTo(0.8f));
        }
    }
}
