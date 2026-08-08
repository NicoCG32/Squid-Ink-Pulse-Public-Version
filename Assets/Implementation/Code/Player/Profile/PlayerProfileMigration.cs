using System;
using System.IO;
using System.Linq;

public static class PlayerProfileMigration
{
    public static void EnsureLegacyMigration(
        string legacyProfilePath,
        string profilePath,
        string recordsPath,
        Action<PlayerProfileSaveData> saveProfile,
        Action<PlayerRecordsSaveData> saveRecords)
    {
        if (saveProfile == null)
        {
            throw new ArgumentNullException(nameof(saveProfile));
        }

        if (saveRecords == null)
        {
            throw new ArgumentNullException(nameof(saveRecords));
        }

        bool needsProfile = !File.Exists(profilePath);
        bool needsRecords = !File.Exists(recordsPath);
        if (!needsProfile && !needsRecords)
        {
            return;
        }

        if (!JsonSaveFile.TryLoad(
            legacyProfilePath,
            NormalizeLegacyProfile,
            "legacy player profile",
            out LegacyPlayerProfileSaveData legacyData))
        {
            return;
        }

        if (needsProfile)
        {
            PlayerProfileSaveData profile = PlayerProfileSaveData.CreateDefault();
            profile.permanentUpgrades = ConvertLegacyUpgrades(legacyData.upgrades);
            profile.skins = legacyData.skins ?? PlayerProfileSkinsSaveData.CreateDefault();
            profile.Normalize();
            saveProfile(profile);
        }

        if (needsRecords)
        {
            PlayerRecordsSaveData records = PlayerRecordsSaveData.CreateDefault();
            if (legacyData.wallet != null)
            {
                records.totalShrimps = legacyData.wallet.totalShrimps;
            }

            if (legacyData.stats != null)
            {
                records.bestScore = legacyData.stats.bestScore;
                records.totalRuns = legacyData.stats.totalRuns;
                records.totalPortalsCrossed = legacyData.stats.totalPortalsCrossed;
                records.totalShrimpsCollected = legacyData.stats.totalShrimpsCollected;
            }

            records.Normalize();
            saveRecords(records);
        }
    }

    public static void EnsureVersion2Migration(
        string profilePath,
        Action<PlayerProfileSaveData> saveProfile)
    {
        if (saveProfile == null)
        {
            throw new ArgumentNullException(nameof(saveProfile));
        }

        if (!File.Exists(profilePath))
        {
            return;
        }

        if (!JsonSaveFile.TryLoad(
            profilePath,
            NormalizeVersion2Profile,
            "version 2 player profile",
            out Version2PlayerProfileSaveData version2Data))
        {
            return;
        }

        if (version2Data.version >= PlayerProfileRepository.CurrentVersion)
        {
            return;
        }

        PlayerProfileSaveData migratedProfile = PlayerProfileSaveData.CreateDefault();
        migratedProfile.permanentUpgrades = ConvertLegacyUpgrades(version2Data.upgrades);
        migratedProfile.skins = version2Data.skins ?? PlayerProfileSkinsSaveData.CreateDefault();
        migratedProfile.runGadgetUnlocks = ConvertLegacyGadgets(version2Data.gadgets);
        migratedProfile.Normalize();
        saveProfile(migratedProfile);
    }

    private static PlayerProfilePermanentUpgradesSaveData ConvertLegacyUpgrades(
        PlayerProfileUpgradesSaveData legacyUpgrades)
    {
        PlayerProfilePermanentUpgradesSaveData upgrades = new();
        if (legacyUpgrades != null)
        {
            upgrades.inkPulseDurationLevel = legacyUpgrades.inkPulseDurationLevel;
            upgrades.inkPulseRechargeRateLevel = legacyUpgrades.inkPulseRechargeRateLevel;
        }

        upgrades.Normalize();
        return upgrades;
    }

    private static PlayerProfileRunGadgetUnlocksSaveData ConvertLegacyGadgets(
        Version2PlayerProfileGadgetsSaveData legacyGadgets)
    {
        PlayerProfileRunGadgetUnlocksSaveData runGadgetUnlocks =
            PlayerProfileRunGadgetUnlocksSaveData.CreateDefault();
        if (legacyGadgets != null &&
            legacyGadgets.unlockedGadgetIds != null &&
            legacyGadgets.unlockedGadgetIds.Length > 0)
        {
            runGadgetUnlocks.unlockedRunGadgetIds = legacyGadgets.unlockedGadgetIds;
        }

        runGadgetUnlocks.Normalize();
        return runGadgetUnlocks;
    }

    private static void NormalizeLegacyProfile(LegacyPlayerProfileSaveData data)
    {
        data?.Normalize();
    }

    private static void NormalizeVersion2Profile(Version2PlayerProfileSaveData data)
    {
        data?.Normalize();
    }

    [Serializable]
    private sealed class LegacyPlayerProfileSaveData
    {
        public int version = 1;
        public LegacyPlayerProfileWalletSaveData wallet = new();
        public PlayerProfileUpgradesSaveData upgrades = new();
        public PlayerProfileSkinsSaveData skins = PlayerProfileSkinsSaveData.CreateDefault();
        public LegacyPlayerProfileStatsSaveData stats = new();

        public void Normalize()
        {
            version = Math.Max(1, version);
            wallet ??= new LegacyPlayerProfileWalletSaveData();
            upgrades ??= new PlayerProfileUpgradesSaveData();
            skins ??= PlayerProfileSkinsSaveData.CreateDefault();
            stats ??= new LegacyPlayerProfileStatsSaveData();

            wallet.Normalize();
            upgrades.Normalize();
            skins.Normalize();
            stats.Normalize();
        }
    }

    [Serializable]
    private sealed class LegacyPlayerProfileWalletSaveData
    {
        public int totalShrimps;

        public void Normalize()
        {
            totalShrimps = Math.Max(0, totalShrimps);
        }
    }

    [Serializable]
    private sealed class Version2PlayerProfileSaveData
    {
        public int version = 2;
        public PlayerProfileUpgradesSaveData upgrades = new();
        public PlayerProfileSkinsSaveData skins = PlayerProfileSkinsSaveData.CreateDefault();
        public Version2PlayerProfileGadgetsSaveData gadgets = new();
        public string[] activeSkillIds = Array.Empty<string>();

        public void Normalize()
        {
            version = Math.Max(1, version);
            upgrades ??= new PlayerProfileUpgradesSaveData();
            skins ??= PlayerProfileSkinsSaveData.CreateDefault();
            gadgets ??= new Version2PlayerProfileGadgetsSaveData();

            upgrades.Normalize();
            skins.Normalize();
            gadgets.Normalize();
        }
    }

    [Serializable]
    private sealed class PlayerProfileUpgradesSaveData
    {
        public int inkPulseDurationLevel;
        public int inkPulseRechargeRateLevel;

        public void Normalize()
        {
            inkPulseDurationLevel = Math.Max(0, inkPulseDurationLevel);
            inkPulseRechargeRateLevel = Math.Max(0, inkPulseRechargeRateLevel);
        }
    }

    [Serializable]
    private sealed class Version2PlayerProfileGadgetsSaveData
    {
        public string[] unlockedGadgetIds = Array.Empty<string>();

        public void Normalize()
        {
            unlockedGadgetIds = unlockedGadgetIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .ToArray() ?? Array.Empty<string>();
        }
    }

    [Serializable]
    private sealed class LegacyPlayerProfileStatsSaveData
    {
        public long bestScore;
        public int totalRuns;
        public int totalPortalsCrossed;
        public int totalShrimpsCollected;

        public void Normalize()
        {
            bestScore = Math.Max(0, bestScore);
            totalRuns = Math.Max(0, totalRuns);
            totalPortalsCrossed = Math.Max(0, totalPortalsCrossed);
            totalShrimpsCollected = Math.Max(0, totalShrimpsCollected);
        }
    }
}
