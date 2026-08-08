using System;
using UnityEngine;

public static class PlayerProfileRepository
{
    public const int CurrentVersion = 3;
    public const int RecordsVersion = 1;
    public const int UnlockablesCatalogVersion = 8;
    public const int LeaderboardVersion = 1;

    private static readonly IJsonSeedProvider SeedProvider = ResolveSeedProvider();

    public static string ProfilePath => PersistentDbPaths.PlayerProfilePath;
    public static string RecordsPath => PersistentDbPaths.PlayerRecordsPath;
    public static string UnlockablesCatalogPath => PersistentDbPaths.UnlockablesCatalogPath;
    public static string LocalLeaderboardPath => PersistentDbPaths.LocalLeaderboardPath;
    public static bool UsesTransientEditorPlayModeProfileSaves => ShouldUseTransientEditorPlayModeProfileSaves();

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
            () => GetSeedText(PersistentDbPaths.PlayerProfileFileName),
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
            () => GetSeedText(PersistentDbPaths.PlayerRecordsFileName),
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
        JsonSaveFile.TryDeserialize(
            GetSeedText(PersistentDbPaths.UnlockablesCatalogFileName),
            NormalizeUnlockablesCatalog,
            "unlockables catalog seed",
            out seedCatalog);

        UnlockablesCatalogSaveData selectedCatalog = UnlockablesCatalogSelectionPolicy.Select(runtimeCatalog, seedCatalog);
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
            () => GetSeedText(PersistentDbPaths.LocalLeaderboardFileName),
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
        if (ShouldUseTransientEditorPlayModeProfileSaves())
        {
            NormalizeProfile(data);
            return;
        }

        JsonSaveFile.Save(PersistentDbPaths.PlayerProfilePath, data, NormalizeProfile, "player profile");
    }

    public static void SaveRecords(PlayerRecordsSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.version = RecordsVersion;
        if (ShouldUseTransientEditorPlayModeProfileSaves())
        {
            NormalizeRecords(data);
            return;
        }

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

    private static bool ShouldUseTransientEditorPlayModeProfileSaves()
    {
#if UNITY_EDITOR
        return Application.isPlaying;
#else
        return false;
#endif
    }

    private static IJsonSeedProvider ResolveSeedProvider()
    {
#if UNITY_EDITOR
        return new FileSystemJsonSeedProvider(PersistentDbPaths.EditorSeedDirectory);
#elif UNITY_STANDALONE_WIN
        return new FileSystemJsonSeedProvider(PersistentDbPaths.WindowsBuildSeedDirectory);
#else
        return new ResourcesJsonSeedProvider(PersistentDbPaths.SeedResourcesDirectoryName);
#endif
    }

    private static string GetSeedText(string seedFileName)
    {
        return SeedProvider.TryGetSeedText(seedFileName, out string seedText)
            ? seedText
            : null;
    }

    private static void EnsureMigratedFromLegacyProfile()
    {
        PlayerProfileMigration.EnsureLegacyMigration(
            PersistentDbPaths.LegacyPlayerProfilePath,
            PersistentDbPaths.PlayerProfilePath,
            PersistentDbPaths.PlayerRecordsPath,
            Save,
            SaveRecords);
    }

    private static void EnsureMigratedFromVersion2Profile()
    {
        PlayerProfileMigration.EnsureVersion2Migration(
            PersistentDbPaths.PlayerProfilePath,
            Save);
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
}
