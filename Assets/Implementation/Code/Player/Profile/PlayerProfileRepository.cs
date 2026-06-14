using System;
using System.IO;
using UnityEngine;

public static class PlayerProfileRepository
{
    public const int CurrentVersion = 1;
    private const string FileName = "player-profile.json";

    public static string ProfilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static PlayerProfileSaveData Load()
    {
        string path = ProfilePath;
        if (!File.Exists(path))
        {
            PlayerProfileSaveData defaultProfile = PlayerProfileSaveData.CreateDefault();
            Save(defaultProfile);
            return defaultProfile;
        }

        try
        {
            string json = File.ReadAllText(path);
            PlayerProfileSaveData data = JsonUtility.FromJson<PlayerProfileSaveData>(json);
            if (data == null)
            {
                throw new InvalidOperationException("Profile JSON deserialized to null.");
            }

            data.Normalize();
            if (data.version < CurrentVersion)
            {
                data.version = CurrentVersion;
                Save(data);
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[PlayerProfileRepository] Could not load profile. A default profile will be used. {exception.Message}");
            PlayerProfileSaveData fallbackProfile = PlayerProfileSaveData.CreateDefault();
            Save(fallbackProfile);
            return fallbackProfile;
        }
    }

    public static void Save(PlayerProfileSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.version = CurrentVersion;
        data.Normalize();

        string path = ProfilePath;
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = $"{path}.tmp";
        string backupPath = $"{path}.bak";
        string json = JsonUtility.ToJson(data, prettyPrint: true);

        File.WriteAllText(temporaryPath, json);
        ReplaceProfileFile(temporaryPath, path, backupPath);
    }

    private static void ReplaceProfileFile(string temporaryPath, string path, string backupPath)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, backupPath, ignoreMetadataErrors: true);
                CleanupBackup(backupPath);
                return;
            }

            File.Move(temporaryPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[PlayerProfileRepository] Atomic replace failed; using fallback write. {exception.Message}");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
            CleanupBackup(backupPath);
        }
    }

    private static void CleanupBackup(string backupPath)
    {
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
    }
}
