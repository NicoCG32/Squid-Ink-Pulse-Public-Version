using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class TouchGameplayControlsPolicyTests
    {
        [Test]
        public void InkPulse_RepresentsChargingReadyActiveAndBlockedWithoutColorOnlyState()
        {
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveInkPulse(
                    GameSessionState.Playing, true, true, false, false, false, false),
                true,
                TouchGameplayControlState.Charging);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveInkPulse(
                    GameSessionState.Playing, true, true, false, false, false, true),
                true,
                TouchGameplayControlState.Ready);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveInkPulse(
                    GameSessionState.Playing, true, true, false, false, true, true),
                false,
                TouchGameplayControlState.Active);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveInkPulse(
                    GameSessionState.Playing, true, true, false, true, false, false),
                false,
                TouchGameplayControlState.Blocked);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveInkPulse(
                    GameSessionState.Paused, false, true, false, false, false, true),
                false,
                TouchGameplayControlState.Blocked);
        }

        [Test]
        public void Pause_IsAvailableOnlyWhenAuthorityAndCommandChannelCanToggle()
        {
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolvePause(GameSessionState.Playing, true, true),
                true,
                TouchGameplayControlState.Pause);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolvePause(GameSessionState.Paused, true, true),
                true,
                TouchGameplayControlState.Resume);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolvePause(GameSessionState.Paused, true, false),
                false,
                TouchGameplayControlState.Blocked);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolvePause(GameSessionState.GameOver, true, true),
                false,
                TouchGameplayControlState.Blocked);
        }

        [Test]
        public void Gadget_DistinguishesEmptyPassiveReadyAndBlocked()
        {
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveGadget(
                    GameSessionState.Playing, true, true, false, GadgetId.None, false, false),
                false,
                TouchGameplayControlState.Empty);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveGadget(
                    GameSessionState.Playing, true, true, false, GadgetId.ShellShield, true, false),
                false,
                TouchGameplayControlState.Passive);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveGadget(
                    GameSessionState.Playing, true, true, false, GadgetId.InkBottle, true, true),
                true,
                TouchGameplayControlState.Ready);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveGadget(
                    GameSessionState.Playing, true, true, false, GadgetId.InkBottle, true, false),
                false,
                TouchGameplayControlState.Blocked);
            AssertPresentation(
                TouchGameplayControlsPolicy.ResolveGadget(
                    GameSessionState.Playing, true, true, true, GadgetId.InkBottle, true, true),
                false,
                TouchGameplayControlState.Blocked);
        }

        private static void AssertPresentation(
            TouchGameplayControlPresentation presentation,
            bool interactable,
            TouchGameplayControlState state)
        {
            Assert.That(presentation.Interactable, Is.EqualTo(interactable));
            Assert.That(presentation.State, Is.EqualTo(state));
        }
    }
}
