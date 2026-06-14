using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PlayerProfileRepository
{
    public const int CurrentVersion = 3;
    public const int RecordsVersion = 1;
    public const int UnlockablesCatalogVersion = 1;
    public const int LeaderboardVersion = 1;

    public static string ProfilePath => PersistentDbPaths.PlayerProfilePath;
    public static string RecordsPath => PersistentDbPaths.PlayerRecordsPath;
    public static string UnlockablesCatalogPath => PersistentDbPaths.UnlockablesCatalogPath;
    public static string LocalLeaderboardPath => PersistentDbPaths.LocalLeaderboardPath;

    public static PlayerProfileSaveData Load()
    {
        return LoadProfile();
    }

    public static PlayerProfileSaveData LoadProfile()
    {
        EnsureMigratedFromLegacyProfile();
        EnsureMigratedFromVersion2Profile();
        PlayerProfileSaveData data = JsonSaveFile.LoadOrCreate(
            PersistentDbPaths.PlayerProfilePath,
            PersistentDbPaths.StreamingPlayerProfilePath,
            PlayerProfileSaveData.CreateDefault,
            NormalizeProfile,
            "player profile");

        if (data.version < CurrentVersion)
        {
            data.version = CurrentVersion;
            Save(data);
        }

        return data;
    }

    public static PlayerRecordsSaveData LoadRecords()
    {
        EnsureMigratedFromLegacyProfile();
        PlayerRecordsSaveData data = JsonSaveFile.LoadOrCreate(
            PersistentDbPaths.PlayerRecordsPath,
            PersistentDbPaths.StreamingPlayerRecordsPath,
            PlayerRecordsSaveData.CreateDefault,
            NormalizeRecords,
            "player records");

        if (data.version < RecordsVersion)
        {
            data.version = RecordsVersion;
            SaveRecords(data);
        }

        return data;
    }

    public static UnlockablesCatalogSaveData LoadUnlockablesCatalog()
    {
        UnlockablesCatalogSaveData runtimeCatalog = null;
        JsonSaveFile.TryLoad(
            PersistentDbPaths.UnlockablesCatalogPath,
            NormalizeUnlockablesCatalog,
            "unlockables catalog",
            out runtimeCatalog);

        UnlockablesCatalogSaveData seedCatalog = null;
        JsonSaveFile.TryLoad(
            PersistentDbPaths.StreamingUnlockablesCatalogPath,
            NormalizeUnlockablesCatalog,
            "unlockables catalog seed",
            out seedCatalog);

        UnlockablesCatalogSaveData selectedCatalog = SelectCatalog(runtimeCatalog, seedCatalog);
        if (selectedCatalog == null)
        {
            selectedCatalog = UnlockablesCatalogSaveData.CreateDefault();
        }

        SaveUnlockablesCatalog(selectedCatalog);
        return selectedCatalog;
    }

    public static LocalLeaderboardSaveData LoadLocalLeaderboard()
    {
        LocalLeaderboardSaveData data = JsonSaveFile.LoadOrCreate(
            PersistentDbPaths.LocalLeaderboardPath,
            PersistentDbPaths.StreamingLocalLeaderboardPath,
            LocalLeaderboardSaveData.CreateDefault,
            NormalizeLeaderboard,
            "local leaderboard");

        if (data.version < LeaderboardVersion)
        {
            data.version = LeaderboardVersion;
            SaveLocalLeaderboard(data);
        }

        return data;
    }

    public static void Save(PlayerProfileSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.version = CurrentVersion;
        JsonSaveFile.Save(PersistentDbPaths.PlayerProfilePath, data, NormalizeProfile, "player profile");
    }

    public static void SaveRecords(PlayerRecordsSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.version = RecordsVersion;
        JsonSaveFile.Save(PersistentDbPaths.PlayerRecordsPath, data, NormalizeRecords, "player records");
    }

    public static void SaveUnlockablesCatalog(UnlockablesCatalogSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.version = Math.Max(UnlockablesCatalogVersion, data.version);
        JsonSaveFile.Save(PersistentDbPaths.UnlockablesCatalogPath, data, NormalizeUnlockablesCatalog, "unlockables catalog");
    }

    public static void SaveLocalLeaderboard(LocalLeaderboardSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.version = LeaderboardVersion;
        JsonSaveFile.Save(PersistentDbPaths.LocalLeaderboardPath, data, NormalizeLeaderboard, "local leaderboard");
    }

    private static UnlockablesCatalogSaveData SelectCatalog(
        UnlockablesCatalogSaveData runtimeCatalog,
        UnlockablesCatalogSaveData seedCatalog)
    {
        if (seedCatalog != null && (runtimeCatalog == null || seedCatalog.version > runtimeCatalog.version))
        {
            return seedCatalog;
        }

        return runtimeCatalog ?? seedCatalog;
    }

    private static void EnsureMigratedFromLegacyProfile()
    {
        bool needsProfile = !File.Exists(PersistentDbPaths.PlayerProfilePath);
        bool needsRecords = !File.Exists(PersistentDbPaths.PlayerRecordsPath);
        if (!needsProfile && !needsRecords)
        {
            return;
        }

        if (!JsonSaveFile.TryLoad(
            PersistentDbPaths.LegacyPlayerProfilePath,
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
            Save(profile);
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
            SaveRecords(records);
        }
    }

    private static void EnsureMigratedFromVersion2Profile()
    {
        if (!File.Exists(PersistentDbPaths.PlayerProfilePath))
        {
            return;
        }

        if (!JsonSaveFile.TryLoad(
            PersistentDbPaths.PlayerProfilePath,
            NormalizeVersion2Profile,
            "version 2 player profile",
            out Version2PlayerProfileSaveData version2Data))
        {
            return;
        }

        if (version2Data.version >= CurrentVersion)
        {
            return;
        }

        PlayerProfileSaveData migratedProfile = PlayerProfileSaveData.CreateDefault();
        migratedProfile.permanentUpgrades = ConvertLegacyUpgrades(version2Data.upgrades);
        migratedProfile.skins = version2Data.skins ?? PlayerProfileSkinsSaveData.CreateDefault();
        migratedProfile.runGadgetUnlocks = ConvertLegacyGadgets(version2Data.gadgets);
        migratedProfile.Normalize();
        Save(migratedProfile);
    }

    private static PlayerProfilePermanentUpgradesSaveData ConvertLegacyUpgrades(PlayerProfileUpgradesSaveData legacyUpgrades)
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

    private static PlayerProfileRunGadgetUnlocksSaveData ConvertLegacyGadgets(Version2PlayerProfileGadgetsSaveData legacyGadgets)
    {
        PlayerProfileRunGadgetUnlocksSaveData runGadgetUnlocks = PlayerProfileRunGadgetUnlocksSaveData.CreateDefault();
        if (legacyGadgets != null && legacyGadgets.unlockedGadgetIds != null && legacyGadgets.unlockedGadgetIds.Length > 0)
        {
            runGadgetUnlocks.unlockedRunGadgetIds = legacyGadgets.unlockedGadgetIds;
        }

        runGadgetUnlocks.Normalize();
        return runGadgetUnlocks;
    }

    private static void NormalizeProfile(PlayerProfileSaveData data)
    {
        data?.Normalize();
    }

    private static void NormalizeRecords(PlayerRecordsSaveData data)
    {
        data?.Normalize();
    }

    private static void NormalizeUnlockablesCatalog(UnlockablesCatalogSaveData data)
    {
        data?.Normalize();
    }

    private static void NormalizeLeaderboard(LocalLeaderboardSaveData data)
    {
        data?.Normalize();
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
    private class LegacyPlayerProfileSaveData
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
    private class LegacyPlayerProfileWalletSaveData
    {
        public int totalShrimps;

        public void Normalize()
        {
            totalShrimps = Math.Max(0, totalShrimps);
        }
    }

    [Serializable]
    private class Version2PlayerProfileSaveData
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
    private class PlayerProfileUpgradesSaveData
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
    private class Version2PlayerProfileGadgetsSaveData
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
    private class LegacyPlayerProfileStatsSaveData
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
