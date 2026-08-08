using System;
using System.IO;
using UnityEngine;

public static class JsonSaveFile
{
    public static T LoadOrCreate<T>(
        string path,
        Func<string> getSeedJson,
        Func<T> createDefault,
        Action<T> normalize,
        string context)
        where T : class
    {
        if (TryLoad(path, normalize, context, out T data))
        {
            return data;
        }

        string seedJson = getSeedJson?.Invoke();
        if (TryDeserialize(seedJson, normalize, $"{context} seed", out data))
        {
            Save(path, data, normalize, context);
            return data;
        }

        data = createDefault();
        normalize?.Invoke(data);
        Save(path, data, normalize, context);
        return data;
    }

    public static bool TryLoad<T>(string path, Action<T> normalize, string context, out T data)
        where T : class
    {
        data = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<T>(json);
            if (data == null)
            {
                throw new InvalidOperationException("JSON deserialized to null.");
            }

            normalize?.Invoke(data);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[JsonSaveFile] Could not load {context} at {path}. {exception.Message}");
            data = null;
            return false;
        }
    }

    public static bool TryDeserialize<T>(string json, Action<T> normalize, string context, out T data)
        where T : class
    {
        data = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<T>(json);
            if (data == null)
            {
                throw new InvalidOperationException("JSON deserialized to null.");
            }

            normalize?.Invoke(data);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[JsonSaveFile] Could not deserialize {context}. {exception.Message}");
            data = null;
            return false;
        }
    }

    public static void Save<T>(string path, T data, Action<T> normalize, string context)
        where T : class
    {
        if (data == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        normalize?.Invoke(data);

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = $"{path}.tmp";
        string backupPath = $"{path}.bak";
        string json = JsonUtility.ToJson(data, prettyPrint: true);

        File.WriteAllText(temporaryPath, json);
        ReplaceFile(temporaryPath, path, backupPath, context);
    }

    private static void ReplaceFile(string temporaryPath, string path, string backupPath, string context)
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
            Debug.LogWarning($"[JsonSaveFile] Atomic replace failed for {context}; using fallback write. {exception.Message}");
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
