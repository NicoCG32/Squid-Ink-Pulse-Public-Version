using NUnit.Framework;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class FairModePlatformPolicyTests
    {
        [TestCase(RuntimePlatform.Android, false, false)]
        [TestCase(RuntimePlatform.Android, true, false)]
        [TestCase(RuntimePlatform.WindowsPlayer, false, true)]
        [TestCase(RuntimePlatform.WindowsPlayer, true, false)]
        [TestCase(RuntimePlatform.WindowsEditor, false, true)]
        [TestCase(RuntimePlatform.WindowsEditor, true, false)]
        [TestCase(RuntimePlatform.OSXEditor, false, true)]
        [TestCase(RuntimePlatform.LinuxEditor, false, true)]
        public void FairMode_AndroidIsExcludedAndDesktopKeepsItsDisableFlag(
            RuntimePlatform platform, bool disabledByArgument, bool expected)
        {
            Assert.That(FairModePlatformPolicy.IsEnabled(platform, disabledByArgument), Is.EqualTo(expected));
        }
    }
}
