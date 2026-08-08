using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PlayerProfileMigrationTests
    {
        private string temporaryDirectory;
        private string legacyPath;
        private string profilePath;
        private string recordsPath;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"SquidInkPulse-Migration-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);
            legacyPath = Path.Combine(temporaryDirectory, "player-profile-legacy.json");
            profilePath = Path.Combine(temporaryDirectory, PersistentDbPaths.PlayerProfileFileName);
            recordsPath = Path.Combine(temporaryDirectory, PersistentDbPaths.PlayerRecordsFileName);
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
        public void LegacyMigration_CreatesSplitProfileAndRecords_InTemporaryDirectory()
        {
            File.WriteAllText(legacyPath, @"
{
  ""version"": 1,
  ""wallet"": { ""totalShrimps"": 321 },
  ""upgrades"": {
    ""inkPulseDurationLevel"": 2,
    ""inkPulseRechargeRateLevel"": 3
  },
  ""skins"": {
    ""unlockedSkinIds"": [""skin.default"", ""skin.sonic""],
    ""equippedSkinId"": ""skin.sonic""
  },
  ""stats"": {
    ""bestScore"": 9876,
    ""totalRuns"": 7,
    ""totalPortalsCrossed"": 5,
    ""totalShrimpsCollected"": 44
  }
}");

            RunLegacyMigration();

            PlayerProfileSaveData profile = LoadProfile();
            PlayerRecordsSaveData records = LoadRecords();
            Assert.That(profile.version, Is.EqualTo(PlayerProfileRepository.CurrentVersion));
            Assert.That(profile.permanentUpgrades.inkPulseDurationLevel, Is.EqualTo(2));
            Assert.That(profile.permanentUpgrades.inkPulseRechargeRateLevel, Is.EqualTo(3));
            Assert.That(profile.skins.equippedSkinId, Is.EqualTo("skin.sonic"));
            Assert.That(records.version, Is.EqualTo(PlayerProfileRepository.RecordsVersion));
            Assert.That(records.totalShrimps, Is.EqualTo(321));
            Assert.That(records.bestScore, Is.EqualTo(9876));
            Assert.That(records.totalRuns, Is.EqualTo(7));
            Assert.That(records.totalPortalsCrossed, Is.EqualTo(5));
            Assert.That(records.totalShrimpsCollected, Is.EqualTo(44));
        }

        [Test]
        public void LegacyMigration_PreservesExistingProfile_AndCreatesOnlyMissingRecords()
        {
            File.WriteAllText(legacyPath, @"
{
  ""version"": 1,
  ""wallet"": { ""totalShrimps"": 55 },
  ""stats"": { ""bestScore"": 800 }
}");
            PlayerProfileSaveData existingProfile = PlayerProfileSaveData.CreateDefault();
            existingProfile.permanentUpgrades.scoreMultiplierLevel = 4;
            SaveProfile(existingProfile);
            string profileHashBefore = ComputeHashText(profilePath);

            RunLegacyMigration();

            Assert.That(ComputeHashText(profilePath), Is.EqualTo(profileHashBefore));
            Assert.That(LoadProfile().permanentUpgrades.scoreMultiplierLevel, Is.EqualTo(4));
            Assert.That(LoadRecords().totalShrimps, Is.EqualTo(55));
            Assert.That(LoadRecords().bestScore, Is.EqualTo(800));
        }

        [Test]
        public void Version2Migration_PreservesUpgradesSkinsAndGadgets()
        {
            File.WriteAllText(profilePath, @"
{
  ""version"": 2,
  ""upgrades"": {
    ""inkPulseDurationLevel"": 6,
    ""inkPulseRechargeRateLevel"": 4
  },
  ""skins"": {
    ""unlockedSkinIds"": [""skin.default"", ""skin.huaso""],
    ""equippedSkinId"": ""skin.huaso""
  },
  ""gadgets"": {
    ""unlockedGadgetIds"": [""gadget.shell_shield""]
  },
  ""activeSkillIds"": [""legacy.skill""]
}");

            PlayerProfileMigration.EnsureVersion2Migration(profilePath, SaveProfile);

            PlayerProfileSaveData profile = LoadProfile();
            Assert.That(profile.version, Is.EqualTo(PlayerProfileRepository.CurrentVersion));
            Assert.That(profile.permanentUpgrades.inkPulseDurationLevel, Is.EqualTo(6));
            Assert.That(profile.permanentUpgrades.inkPulseRechargeRateLevel, Is.EqualTo(4));
            Assert.That(profile.skins.equippedSkinId, Is.EqualTo("skin.huaso"));
            Assert.That(profile.runGadgetUnlocks.unlockedRunGadgetIds, Is.EqualTo(new[]
            {
                PlayerUnlockableIds.ShellShieldGadget
            }));
        }

        [Test]
        public void Version2Migration_DoesNotRewriteCurrentProfile()
        {
            PlayerProfileSaveData currentProfile = PlayerProfileSaveData.CreateDefault();
            currentProfile.permanentUpgrades.shrimpMultiplierLevel = 5;
            SaveProfile(currentProfile);
            string profileHashBefore = ComputeHashText(profilePath);
            bool saveRequested = false;

            PlayerProfileMigration.EnsureVersion2Migration(
                profilePath,
                profile => saveRequested = true);

            Assert.That(saveRequested, Is.False);
            Assert.That(ComputeHashText(profilePath), Is.EqualTo(profileHashBefore));
        }

        private void RunLegacyMigration()
        {
            PlayerProfileMigration.EnsureLegacyMigration(
                legacyPath,
                profilePath,
                recordsPath,
                SaveProfile,
                SaveRecords);
        }

        private void SaveProfile(PlayerProfileSaveData profile)
        {
            profile.version = PlayerProfileRepository.CurrentVersion;
            JsonSaveFile.Save(profilePath, profile, data => data.Normalize(), "migration profile");
        }

        private void SaveRecords(PlayerRecordsSaveData records)
        {
            records.version = PlayerProfileRepository.RecordsVersion;
            JsonSaveFile.Save(recordsPath, records, data => data.Normalize(), "migration records");
        }

        private PlayerProfileSaveData LoadProfile()
        {
            Assert.That(JsonSaveFile.TryLoad(
                profilePath,
                (PlayerProfileSaveData data) => data.Normalize(),
                "migration profile assertion",
                out PlayerProfileSaveData profile), Is.True);
            return profile;
        }

        private PlayerRecordsSaveData LoadRecords()
        {
            Assert.That(JsonSaveFile.TryLoad(
                recordsPath,
                (PlayerRecordsSaveData data) => data.Normalize(),
                "migration records assertion",
                out PlayerRecordsSaveData records), Is.True);
            return records;
        }

        private static string ComputeHashText(string path)
        {
            return File.ReadAllText(path);
        }
    }

    public sealed class JsonSaveFileRecoveryTests
    {
        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"SquidInkPulse-Recovery-{Guid.NewGuid():N}");
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
        public void CorruptRuntime_RecoversFromValidSeed()
        {
            string runtimePath = Path.Combine(temporaryDirectory, "records.json");
            File.WriteAllText(runtimePath, "not-json");
            PlayerRecordsSaveData seed = new() { totalShrimps = 12 };
            LogAssert.Expect(LogType.Warning, new Regex("Could not load records at"));

            PlayerRecordsSaveData loaded = JsonSaveFile.LoadOrCreate(
                runtimePath,
                () => JsonUtility.ToJson(seed),
                PlayerRecordsSaveData.CreateDefault,
                data => data.Normalize(),
                "records");

            Assert.That(loaded.totalShrimps, Is.EqualTo(12));
            Assert.That(LoadRecords(runtimePath).totalShrimps, Is.EqualTo(12));
        }

        [Test]
        public void CorruptRuntimeAndMissingSeed_RecoversWithNormalizedDefault()
        {
            string runtimePath = Path.Combine(temporaryDirectory, "records.json");
            File.WriteAllText(runtimePath, "not-json");
            LogAssert.Expect(LogType.Warning, new Regex("Could not load records at"));

            PlayerRecordsSaveData loaded = JsonSaveFile.LoadOrCreate(
                runtimePath,
                null,
                PlayerRecordsSaveData.CreateDefault,
                data => data.Normalize(),
                "records");

            Assert.That(loaded.version, Is.EqualTo(PlayerProfileRepository.RecordsVersion));
            Assert.That(loaded.totalShrimps, Is.Zero);
            Assert.That(LoadRecords(runtimePath).totalShrimps, Is.Zero);
        }

        [Test]
        public void CorruptSeedAndMissingRuntime_RecoversWithNormalizedDefault()
        {
            string runtimePath = Path.Combine(temporaryDirectory, "records.json");
            LogAssert.Expect(LogType.Warning, new Regex("Could not deserialize records seed"));

            PlayerRecordsSaveData loaded = JsonSaveFile.LoadOrCreate(
                runtimePath,
                () => "not-json",
                PlayerRecordsSaveData.CreateDefault,
                data => data.Normalize(),
                "records");

            Assert.That(loaded.version, Is.EqualTo(PlayerProfileRepository.RecordsVersion));
            Assert.That(loaded.totalShrimps, Is.Zero);
            Assert.That(LoadRecords(runtimePath).totalShrimps, Is.Zero);
        }

        private static PlayerRecordsSaveData LoadRecords(string path)
        {
            Assert.That(JsonSaveFile.TryLoad(
                path,
                (PlayerRecordsSaveData data) => data.Normalize(),
                "recovery assertion",
                out PlayerRecordsSaveData records), Is.True);
            return records;
        }
    }
}
