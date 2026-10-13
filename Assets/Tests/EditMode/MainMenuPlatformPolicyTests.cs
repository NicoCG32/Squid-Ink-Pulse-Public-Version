using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MainMenuPlatformPolicyTests
    {
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void Exit_IsAvailableOnlyOnDesktop(bool isMobile, bool expected)
        {
            Assert.That(MainMenuPlatformPolicy.CanExitFromMenu(isMobile), Is.EqualTo(expected));
        }
    }
}
