using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobilePersistenceSeedTests
    {
        private IJsonSeedProvider seedProvider;
        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            seedProvider = new ResourcesJsonSeedProvider(PersistentDbPaths.SeedResourcesDirectoryName);
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"SquidInkPulse-PackagedSeeds-{Guid.NewGuid():N}");
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

        [TestCase(PersistentDbPaths.PlayerProfileFileName)]
        [TestCase(PersistentDbPaths.PlayerRecordsFileName)]
        [TestCase(PersistentDbPaths.UnlockablesCatalogFileName)]
        [TestCase(PersistentDbPaths.LocalLeaderboardFileName)]
        public void ResourcesProvider_ExposesEveryPackagedSeed(string seedFileName)
        {
            bool found = seedProvider.TryGetSeedText(seedFileName, out string seedText);

            Assert.That(found, Is.True, $"No se encontro la semilla Resources {seedFileName}.");
            Assert.That(seedText, Is.Not.Empty);
        }

        [Test]
        public void CleanRuntime_CreatesExactProfileSeed()
        {
            PlayerProfileSaveData profile = LoadIntoCleanRuntime(
                PersistentDbPaths.PlayerProfileFileName,
                PlayerProfileSaveData.CreateDefault,
                data => data.Normalize());

            Assert.That(profile.version, Is.EqualTo(PlayerProfileRepository.CurrentVersion));
            Assert.That(profile.permanentUpgrades.inkPulseDurationLevel, Is.Zero);
            Assert.That(profile.permanentUpgrades.inkPulseRechargeRateLevel, Is.Zero);
            Assert.That(profile.permanentUpgrades.shrimpMultiplierLevel, Is.Zero);
            Assert.That(profile.permanentUpgrades.scoreMultiplierLevel, Is.Zero);
            Assert.That(profile.skins.unlockedSkinIds, Is.EqualTo(new[] { PlayerSkinIds.Default }));
            Assert.That(profile.skins.equippedSkinId, Is.EqualTo(PlayerSkinIds.Default));
            Assert.That(profile.runGadgetUnlocks.unlockedRunGadgetIds, Is.EqualTo(new[]
            {
                PlayerUnlockableIds.ShellShieldGadget,
                PlayerUnlockableIds.InkBottleGadget
            }));
            Assert.That(profile.lore.viewedComicEventIds, Is.Empty);
        }

        [Test]
        public void CleanRuntime_CreatesExactRecordsSeed()
        {
            PlayerRecordsSaveData records = LoadIntoCleanRuntime(
                PersistentDbPaths.PlayerRecordsFileName,
                PlayerRecordsSaveData.CreateDefault,
                data => data.Normalize());

            Assert.That(records.version, Is.EqualTo(PlayerProfileRepository.RecordsVersion));
            Assert.That(records.totalShrimps, Is.Zero);
            Assert.That(records.bestScore, Is.Zero);
            Assert.That(records.totalRuns, Is.Zero);
            Assert.That(records.totalPortalsCrossed, Is.Zero);
            Assert.That(records.totalShrimpsCollected, Is.Zero);
        }

        [Test]
        public void CleanRuntime_CreatesExactCatalogSeed()
        {
            UnlockablesCatalogSaveData catalog = LoadIntoCleanRuntime(
                PersistentDbPaths.UnlockablesCatalogFileName,
                UnlockablesCatalogSaveData.CreateDefault,
                data => data.Normalize());

            Assert.That(catalog.version, Is.EqualTo(PlayerProfileRepository.UnlockablesCatalogVersion));
            Assert.That(catalog.skins.Select(item => item.id), Is.EqualTo(new[]
            {
                "skin.default",
                "skin.bob_marley",
                "skin.rockstar",
                "skin.formal",
                "skin.sonic",
                "skin.huaso",
                "skin.chile",
                "skin.nemo",
                "skin.travis"
            }));
            Assert.That(catalog.runGadgets.Select(item => item.id), Is.EqualTo(new[]
            {
                PlayerUnlockableIds.ShellShieldGadget,
                PlayerUnlockableIds.InkBottleGadget
            }));
            Assert.That(catalog.permanentUpgrades.Select(item => item.id), Is.EqualTo(new[]
            {
                PlayerUnlockableIds.InkPulseDurationUpgrade,
                PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
                PlayerUnlockableIds.ShrimpMultiplierUpgrade,
                PlayerUnlockableIds.ScoreMultiplierUpgrade
            }));

            UnlockableSkinDefinition rastaSkin = catalog.skins.Single(item => item.id == "skin.bob_marley");
            UnlockableSkinDefinition travisSkin = catalog.skins.Single(item => item.id == "skin.travis");
            PermanentUpgradeDefinition durationUpgrade = catalog.permanentUpgrades
                .Single(item => item.id == PlayerUnlockableIds.InkPulseDurationUpgrade);
            PermanentUpgradeDefinition rechargeUpgrade = catalog.permanentUpgrades
                .Single(item => item.id == PlayerUnlockableIds.InkPulseRechargeRateUpgrade);

            Assert.That(rastaSkin.basePrice, Is.EqualTo(420000));
            Assert.That(travisSkin.basePrice, Is.EqualTo(10000000));
            Assert.That(durationUpgrade.basePrice, Is.EqualTo(350));
            Assert.That(durationUpgrade.priceGrowthMultiplier, Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(durationUpgrade.effectPerLevel, Is.EqualTo(0.075f).Within(0.0001f));
            Assert.That(rechargeUpgrade.basePrice, Is.EqualTo(500));
            Assert.That(rechargeUpgrade.priceGrowthMultiplier, Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(rechargeUpgrade.effectPerLevel, Is.EqualTo(4.5f).Within(0.0001f));
        }

        [Test]
        public void CleanRuntime_CreatesExactLeaderboardSeed()
        {
            LocalLeaderboardSaveData leaderboard = LoadIntoCleanRuntime(
                PersistentDbPaths.LocalLeaderboardFileName,
                LocalLeaderboardSaveData.CreateDefault,
                data => data.Normalize());

            Assert.That(leaderboard.version, Is.EqualTo(PlayerProfileRepository.LeaderboardVersion));
            Assert.That(leaderboard.maxEntries, Is.EqualTo(20));
            Assert.That(leaderboard.entries, Is.Empty);
        }

        private T LoadIntoCleanRuntime<T>(string seedFileName, Func<T> createDefault, Action<T> normalize)
            where T : class
        {
            string runtimePath = Path.Combine(temporaryDirectory, seedFileName);
            T data = JsonSaveFile.LoadOrCreate(
                runtimePath,
                () => seedProvider.TryGetSeedText(seedFileName, out string seedText)
                    ? seedText
                    : null,
                createDefault,
                normalize,
                $"packaged {seedFileName}");

            Assert.That(File.Exists(runtimePath), Is.True);
            return data;
        }
    }
}
