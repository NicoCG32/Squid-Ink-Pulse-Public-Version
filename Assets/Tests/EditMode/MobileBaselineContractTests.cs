using System;
using System.IO;
using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class UnlockablesCatalogSelectionPolicyTests
    {
        [Test]
        public void Select_PrefersNewerSeedCatalog()
        {
            UnlockablesCatalogSaveData runtimeCatalog = Catalog(version: 7);
            UnlockablesCatalogSaveData seedCatalog = Catalog(version: 8);

            UnlockablesCatalogSaveData selected = UnlockablesCatalogSelectionPolicy.Select(
                runtimeCatalog,
                seedCatalog);

            Assert.That(selected, Is.SameAs(seedCatalog));
        }

        [TestCase(8, 8)]
        [TestCase(9, 8)]
        public void Select_PreservesRuntimeCatalog_WhenItsVersionIsCurrentOrNewer(
            int runtimeVersion,
            int seedVersion)
        {
            UnlockablesCatalogSaveData runtimeCatalog = Catalog(runtimeVersion);
            UnlockablesCatalogSaveData seedCatalog = Catalog(seedVersion);

            UnlockablesCatalogSaveData selected = UnlockablesCatalogSelectionPolicy.Select(
                runtimeCatalog,
                seedCatalog);

            Assert.That(selected, Is.SameAs(runtimeCatalog));
        }

        [Test]
        public void Select_UsesAvailableCatalog_WhenOnlyOneExists()
        {
            UnlockablesCatalogSaveData runtimeCatalog = Catalog(version: 8);
            UnlockablesCatalogSaveData seedCatalog = Catalog(version: 9);

            Assert.That(
                UnlockablesCatalogSelectionPolicy.Select(runtimeCatalog, null),
                Is.SameAs(runtimeCatalog));
            Assert.That(
                UnlockablesCatalogSelectionPolicy.Select(null, seedCatalog),
                Is.SameAs(seedCatalog));
        }

        [Test]
        public void Select_ReturnsNull_WhenNoCatalogExists()
        {
            Assert.That(UnlockablesCatalogSelectionPolicy.Select(null, null), Is.Null);
        }

        private static UnlockablesCatalogSaveData Catalog(int version)
        {
            return new UnlockablesCatalogSaveData { version = version };
        }
    }

    public sealed class InkPulseActivationPolicyTests
    {
        [Test]
        public void CanActivate_AllowsChargedPulseDuringActiveGameplay()
        {
            bool canActivate = InkPulseActivationPolicy.CanActivate(
                isGameplayActive: true,
                isActivationSuppressed: false,
                isShopBlockingActivation: false,
                isPulseActive: false,
                isCharged: true);

            Assert.That(canActivate, Is.True);
        }

        [TestCase(false, false, false, false, true)]
        [TestCase(true, true, false, false, true)]
        [TestCase(true, false, true, false, true)]
        [TestCase(true, false, false, true, true)]
        [TestCase(true, false, false, false, false)]
        public void CanActivate_RejectsBlockedState(
            bool isGameplayActive,
            bool isActivationSuppressed,
            bool isShopBlockingActivation,
            bool isPulseActive,
            bool isCharged)
        {
            bool canActivate = InkPulseActivationPolicy.CanActivate(
                isGameplayActive,
                isActivationSuppressed,
                isShopBlockingActivation,
                isPulseActive,
                isCharged);

            Assert.That(canActivate, Is.False);
        }
    }

    public sealed class PauseMenuCommandPolicyTests
    {
        [TestCase(GameSessionState.Playing, PauseMenuCommandAction.RequestPause)]
        [TestCase(GameSessionState.Paused, PauseMenuCommandAction.RequestResume)]
        [TestCase(GameSessionState.GameOver, PauseMenuCommandAction.None)]
        public void ResolveToggle_UsesSessionState(
            GameSessionState sessionState,
            PauseMenuCommandAction expected)
        {
            PauseMenuCommandAction action = PauseMenuCommandPolicy.ResolveToggle(
                sessionState,
                hasMenuRoot: true,
                isAnimating: false);

            Assert.That(action, Is.EqualTo(expected));
        }

        [Test]
        public void ResolveToggle_RejectsMissingSession()
        {
            PauseMenuCommandAction action = PauseMenuCommandPolicy.ResolveToggle(
                sessionState: null,
                hasMenuRoot: true,
                isAnimating: false);

            Assert.That(action, Is.EqualTo(PauseMenuCommandAction.None));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void ResolveToggle_RejectsUnavailableOrAnimatingMenu(
            bool hasMenuRoot,
            bool isAnimating)
        {
            PauseMenuCommandAction action = PauseMenuCommandPolicy.ResolveToggle(
                GameSessionState.Playing,
                hasMenuRoot,
                isAnimating);

            Assert.That(action, Is.EqualTo(PauseMenuCommandAction.None));
        }
    }

    public sealed class JsonSaveFileBaselineTests
    {
        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"SquidInkPulse-MobileBaseline-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        [Test]
        public void LoadOrCreate_UsesNormalizedDefault_WhenRuntimeAndSeedAreMissing()
        {
            string runtimePath = Path.Combine(temporaryDirectory, "runtime-profile.json");
            string seedPath = Path.Combine(temporaryDirectory, "missing-seed.json");

            PlayerProfileSaveData loaded = JsonSaveFile.LoadOrCreate(
                runtimePath,
                seedPath,
                PlayerProfileSaveData.CreateDefault,
                data => data.Normalize(),
                "mobile baseline profile");

            Assert.That(loaded.version, Is.EqualTo(PlayerProfileRepository.CurrentVersion));
            Assert.That(loaded.skins.equippedSkinId, Is.EqualTo(PlayerSkinIds.Default));
            Assert.That(loaded.runGadgetUnlocks.unlockedRunGadgetIds, Has.Length.EqualTo(2));
            Assert.That(File.Exists(runtimePath), Is.True);
        }

        [Test]
        public void LoadOrCreate_PrefersExistingRuntimeData_OverSeed()
        {
            string runtimePath = Path.Combine(temporaryDirectory, "runtime-records.json");
            string seedPath = Path.Combine(temporaryDirectory, "seed-records.json");
            PlayerRecordsSaveData runtimeRecords = new() { totalShrimps = 25 };
            PlayerRecordsSaveData seedRecords = new() { totalShrimps = 5 };

            JsonSaveFile.Save(runtimePath, runtimeRecords, data => data.Normalize(), "runtime records");
            JsonSaveFile.Save(seedPath, seedRecords, data => data.Normalize(), "seed records");

            PlayerRecordsSaveData loaded = JsonSaveFile.LoadOrCreate(
                runtimePath,
                seedPath,
                PlayerRecordsSaveData.CreateDefault,
                data => data.Normalize(),
                "mobile baseline records");

            Assert.That(loaded.totalShrimps, Is.EqualTo(25));
        }
    }
}
