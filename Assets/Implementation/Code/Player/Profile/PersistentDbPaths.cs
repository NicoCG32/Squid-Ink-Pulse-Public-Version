using System.IO;
using UnityEngine;

public static class PersistentDbPaths
{
    public const string DbDirectoryName = "db";
    public const string SeedResourcesDirectoryName = "PersistentDbSeeds";
    public const string UnlockablesCatalogFileName = "unlockables-catalog.json";
    public const string PlayerProfileFileName = "player-profile.json";
    public const string PlayerRecordsFileName = "player-records.json";
    public const string LocalLeaderboardFileName = "local-leaderboard.json";

    public static string RuntimeDbDirectory => Path.Combine(Application.persistentDataPath, DbDirectoryName);
    public static string EditorSeedDirectory => Path.Combine(
        Application.dataPath,
        "Implementation",
        "Resources",
        SeedResourcesDirectoryName);
    public static string WindowsBuildSeedDirectory => Path.Combine(Application.streamingAssetsPath, DbDirectoryName);

    public static string UnlockablesCatalogPath => Path.Combine(RuntimeDbDirectory, UnlockablesCatalogFileName);
    public static string PlayerProfilePath => Path.Combine(RuntimeDbDirectory, PlayerProfileFileName);
    public static string PlayerRecordsPath => Path.Combine(RuntimeDbDirectory, PlayerRecordsFileName);
    public static string LocalLeaderboardPath => Path.Combine(RuntimeDbDirectory, LocalLeaderboardFileName);

    public static string LegacyPlayerProfilePath => Path.Combine(Application.persistentDataPath, PlayerProfileFileName);
}
