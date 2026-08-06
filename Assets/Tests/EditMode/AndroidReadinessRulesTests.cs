using System.Linq;
using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class AndroidReadinessRulesTests
    {
        [Test]
        public void Evaluate_CompliantSnapshot_HasNoErrorsOrWarnings()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();

            var findings = AndroidReadinessRules.Evaluate(snapshot);

            Assert.That(findings, Has.None.Matches<AndroidReadinessFinding>(
                finding => finding.Severity != AndroidReadinessSeverity.Info));
        }

        [Test]
        public void Evaluate_RejectsUnexpectedUnityVersion()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.UnityVersion = "6000.5.6f1";

            AssertError(AndroidReadinessRules.Evaluate(snapshot), "UNITY_VERSION");
        }

        [Test]
        public void Evaluate_RejectsMissingAndroidModuleAndInputSystem()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.IsAndroidModuleInstalled = false;
            snapshot.IsInputSystemEnabled = false;

            var findings = AndroidReadinessRules.Evaluate(snapshot);

            AssertError(findings, "ANDROID_MODULE");
            AssertError(findings, "INPUT_SYSTEM");
        }

        [Test]
        public void Evaluate_RejectsChangedSceneOrder()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.EnabledBuildScenes = AndroidReadinessRules.ExpectedBuildScenes.Reverse().ToArray();

            AssertError(AndroidReadinessRules.Evaluate(snapshot), "BUILD_SCENES");
        }

        [Test]
        public void Evaluate_ReportsGenericIdentity()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.CompanyName = "DefaultCompany";
            snapshot.ApplicationIdentifier = "com.DefaultCompany.2D-URP";

            var findings = AndroidReadinessRules.Evaluate(snapshot);

            AssertWarning(findings, "COMPANY_NAME");
            AssertError(findings, "APPLICATION_IDENTIFIER");
        }

        [Test]
        public void Evaluate_RejectsInvalidApplicationIdentifierFormat()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.ApplicationIdentifier = "squid ink pulse";

            AssertError(AndroidReadinessRules.Evaluate(snapshot), "APPLICATION_IDENTIFIER");
        }

        [Test]
        public void Evaluate_RejectsPortraitOrIncompleteLandscapeRotation()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.AllowsPortrait = true;
            snapshot.AllowsLandscapeRight = false;

            AssertError(AndroidReadinessRules.Evaluate(snapshot), "ORIENTATION");
        }

        [Test]
        public void Evaluate_RejectsMissingArm64AndIl2Cpp()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.SupportsArm64 = false;
            snapshot.UsesIl2Cpp = false;

            var findings = AndroidReadinessRules.Evaluate(snapshot);

            AssertError(findings, "ARM64");
            AssertError(findings, "IL2CPP");
        }

        [Test]
        public void Evaluate_ReportsEveryMissingSeed()
        {
            AndroidReadinessSnapshot snapshot = CreateCompliantSnapshot();
            snapshot.ExistingSeedPaths = new[]
            {
                AndroidReadinessRules.ExpectedSeedPaths[0],
                AndroidReadinessRules.ExpectedSeedPaths[2]
            };

            AndroidReadinessFinding finding = AndroidReadinessRules.Evaluate(snapshot)
                .Single(item => item.Code == "SEEDS");

            Assert.That(finding.Severity, Is.EqualTo(AndroidReadinessSeverity.Error));
            Assert.That(finding.Message, Does.Contain(AndroidReadinessRules.ExpectedSeedPaths[1]));
            Assert.That(finding.Message, Does.Contain(AndroidReadinessRules.ExpectedSeedPaths[3]));
        }

        private static AndroidReadinessSnapshot CreateCompliantSnapshot()
        {
            return new AndroidReadinessSnapshot
            {
                UnityVersion = AndroidReadinessRules.ExpectedUnityVersion,
                IsAndroidModuleInstalled = true,
                EnabledBuildScenes = (string[])AndroidReadinessRules.ExpectedBuildScenes.Clone(),
                IsInputSystemEnabled = true,
                CompanyName = "Yeco Works",
                ApplicationIdentifier = "com.yecoworks.squidinkpulse",
                UsesAutoRotation = true,
                AllowsPortrait = false,
                AllowsPortraitUpsideDown = false,
                AllowsLandscapeLeft = true,
                AllowsLandscapeRight = true,
                SupportsArm64 = true,
                UsesIl2Cpp = true,
                ExistingSeedPaths = (string[])AndroidReadinessRules.ExpectedSeedPaths.Clone()
            };
        }

        private static void AssertError(
            System.Collections.Generic.IEnumerable<AndroidReadinessFinding> findings,
            string code)
        {
            Assert.That(findings, Has.Some.Matches<AndroidReadinessFinding>(
                finding => finding.Code == code && finding.Severity == AndroidReadinessSeverity.Error));
        }

        private static void AssertWarning(
            System.Collections.Generic.IEnumerable<AndroidReadinessFinding> findings,
            string code)
        {
            Assert.That(findings, Has.Some.Matches<AndroidReadinessFinding>(
                finding => finding.Code == code && finding.Severity == AndroidReadinessSeverity.Warning));
        }
    }
}
