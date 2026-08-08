using System.IO;
using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class AndroidDevelopmentBuildRulesTests
    {
        [Test]
        public void FindMissingRequiredScenes_AllExpectedScenesEnabled_ReturnsEmpty()
        {
            string[] missing = AndroidDevelopmentBuildRules.FindMissingRequiredScenes(
                AndroidReadinessRules.ExpectedBuildScenes);

            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void FindMissingRequiredScenes_ReportsEveryMissingScene()
        {
            string[] enabled =
            {
                AndroidReadinessRules.ExpectedBuildScenes[0],
                AndroidReadinessRules.ExpectedBuildScenes[2]
            };

            string[] missing = AndroidDevelopmentBuildRules.FindMissingRequiredScenes(enabled);

            Assert.That(missing, Is.EqualTo(new[]
            {
                AndroidReadinessRules.ExpectedBuildScenes[1],
                AndroidReadinessRules.ExpectedBuildScenes[3]
            }));
        }

        [Test]
        public void IsOutputInsideBuildDirectory_AcceptsDedicatedBuildPath()
        {
            string projectRoot = CreateProjectRoot();
            string output = Path.Combine(projectRoot, "Build", "AndroidDevelopment", "game.apk");

            Assert.That(
                AndroidDevelopmentBuildRules.IsOutputInsideBuildDirectory(projectRoot, output),
                Is.True);
        }

        [Test]
        public void IsOutputInsideBuildDirectory_RejectsAssetsAndSiblingPrefix()
        {
            string projectRoot = CreateProjectRoot();
            string assetsOutput = Path.Combine(projectRoot, "Assets", "game.apk");
            string siblingOutput = Path.Combine(projectRoot, "BuildBackup", "game.apk");

            Assert.That(
                AndroidDevelopmentBuildRules.IsOutputInsideBuildDirectory(projectRoot, assetsOutput),
                Is.False);
            Assert.That(
                AndroidDevelopmentBuildRules.IsOutputInsideBuildDirectory(projectRoot, siblingOutput),
                Is.False);
        }

        [Test]
        public void GetAndroidSupportError_AvailableSupport_ReturnsNoError()
        {
            Assert.That(AndroidDevelopmentBuildRules.GetAndroidSupportError(true), Is.Null);
        }

        [Test]
        public void GetAndroidSupportError_MissingSupport_ReturnsActionableMessage()
        {
            string error = AndroidDevelopmentBuildRules.GetAndroidSupportError(false);

            Assert.That(error, Is.EqualTo(AndroidDevelopmentBuildRules.AndroidSupportUnavailableMessage));
            Assert.That(error, Does.Contain("Android Build Support"));
            Assert.That(error, Does.Contain("Editor de Unity"));
        }

        private static string CreateProjectRoot()
        {
            return Path.Combine(Path.GetTempPath(), "SquidInkPulseBuildRulesTests", "Project");
        }
    }
}
