using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PauseMenuPresentationPolicyTests
    {
        [Test]
        public void ResolveSessionTransition_WhenEnteringPauseAndMenuIsHidden_ShowsAnimated()
        {
            PauseMenuPresentationAction action = PauseMenuPresentationPolicy.ResolveSessionTransition(
                GameSessionState.Playing,
                GameSessionState.Paused,
                isMenuPaused: false,
                isAnimating: false);

            Assert.That(action, Is.EqualTo(PauseMenuPresentationAction.ShowAnimated));
        }

        [Test]
        public void ResolveSessionTransition_WhenLeavingPauseAndMenuIsVisible_HidesAnimated()
        {
            PauseMenuPresentationAction action = PauseMenuPresentationPolicy.ResolveSessionTransition(
                GameSessionState.Paused,
                GameSessionState.Playing,
                isMenuPaused: true,
                isAnimating: false);

            Assert.That(action, Is.EqualTo(PauseMenuPresentationAction.HideAnimated));
        }

        [Test]
        public void ResolveSessionTransition_WhenGameOver_HidesImmediatelyEvenIfAnimating()
        {
            PauseMenuPresentationAction action = PauseMenuPresentationPolicy.ResolveSessionTransition(
                GameSessionState.Paused,
                GameSessionState.GameOver,
                isMenuPaused: true,
                isAnimating: true);

            Assert.That(action, Is.EqualTo(PauseMenuPresentationAction.HideImmediate));
        }

        [Test]
        public void ResolveSessionTransition_WhenAnimatingNonTerminalTransition_DoesNothing()
        {
            PauseMenuPresentationAction action = PauseMenuPresentationPolicy.ResolveSessionTransition(
                GameSessionState.Playing,
                GameSessionState.Paused,
                isMenuPaused: false,
                isAnimating: true);

            Assert.That(action, Is.EqualTo(PauseMenuPresentationAction.None));
        }
    }
}
