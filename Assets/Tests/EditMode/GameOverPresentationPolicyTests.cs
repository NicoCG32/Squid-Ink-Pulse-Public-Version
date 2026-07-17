using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class GameOverPresentationPolicyTests
    {
        [Test]
        public void ResolveSessionState_WhenEnteringGameOver_StartsDefeatComicFlow()
        {
            GameOverPresentationAction action = GameOverPresentationPolicy.ResolveSessionState(
                GameSessionState.GameOver,
                menuAlreadyShownForState: false,
                presentationRoutineRunning: false);

            Assert.That(action, Is.EqualTo(GameOverPresentationAction.PlayDefeatComicThenShow));
        }

        [Test]
        public void ResolveSessionState_WhenGameOverPresentationAlreadyShown_DoesNothing()
        {
            GameOverPresentationAction action = GameOverPresentationPolicy.ResolveSessionState(
                GameSessionState.GameOver,
                menuAlreadyShownForState: true,
                presentationRoutineRunning: false);

            Assert.That(action, Is.EqualTo(GameOverPresentationAction.None));
        }

        [Test]
        public void ResolveSessionState_WhenDefeatComicRoutineIsRunning_DoesNothing()
        {
            GameOverPresentationAction action = GameOverPresentationPolicy.ResolveSessionState(
                GameSessionState.GameOver,
                menuAlreadyShownForState: false,
                presentationRoutineRunning: true);

            Assert.That(action, Is.EqualTo(GameOverPresentationAction.None));
        }

        [TestCase(GameSessionState.Playing)]
        [TestCase(GameSessionState.Paused)]
        public void ResolveSessionState_WhenLeavingGameOver_HidesImmediately(GameSessionState state)
        {
            GameOverPresentationAction action = GameOverPresentationPolicy.ResolveSessionState(
                state,
                menuAlreadyShownForState: true,
                presentationRoutineRunning: true);

            Assert.That(action, Is.EqualTo(GameOverPresentationAction.HideImmediate));
        }

        [Test]
        public void FormatScoreText_CombinesPrefixAndScore()
        {
            Assert.That(GameOverScoreText.Format("Puntaje: ", 1250), Is.EqualTo("Puntaje: 1250"));
        }
    }
}
