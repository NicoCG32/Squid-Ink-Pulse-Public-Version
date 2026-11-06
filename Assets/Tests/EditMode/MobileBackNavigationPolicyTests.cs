using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileBackNavigationPolicyTests
    {
        [TestCase(true, true, true, true, GameSessionState.Playing, true, true, MobileBackAction.None)]
        [TestCase(false, true, true, true, GameSessionState.Paused, true, true, MobileBackAction.CloseOptions)]
        [TestCase(false, false, true, false, null, false, false, MobileBackAction.CloseTutorial)]
        [TestCase(false, false, false, true, GameSessionState.Playing, false, false, MobileBackAction.CloseRunShop)]
        [TestCase(false, false, false, false, GameSessionState.Playing, true, false, MobileBackAction.Pause)]
        [TestCase(false, false, false, false, GameSessionState.Paused, true, false, MobileBackAction.Resume)]
        [TestCase(false, false, false, false, GameSessionState.Paused, false, false, MobileBackAction.None)]
        [TestCase(false, false, false, false, GameSessionState.GameOver, true, false, MobileBackAction.None)]
        [TestCase(false, false, false, false, null, false, true, MobileBackAction.ReturnToMainMenu)]
        [TestCase(false, false, false, false, null, false, false, MobileBackAction.None)]
        public void Back_RespectsTheTopContext(bool transition, bool options, bool tutorial, bool runShop,
            GameSessionState? state, bool canPause, bool shop, MobileBackAction expected)
        {
            Assert.That(MobileBackNavigationPolicy.Resolve(transition, options, tutorial, runShop, state, canPause, shop), Is.EqualTo(expected));
        }
    }
}
