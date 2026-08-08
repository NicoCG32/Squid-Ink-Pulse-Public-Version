using System;
using System.IO;
using UnityEngine;

public sealed class FileSystemJsonSeedProvider : IJsonSeedProvider
{
    private readonly string seedDirectory;

    public FileSystemJsonSeedProvider(string seedDirectory)
    {
        this.seedDirectory = seedDirectory;
    }

    public bool TryGetSeedText(string seedFileName, out string seedText)
    {
        seedText = null;
        if (string.IsNullOrWhiteSpace(seedDirectory) || string.IsNullOrWhiteSpace(seedFileName))
        {
            return false;
        }

        string seedPath = Path.Combine(seedDirectory, seedFileName);
        try
        {
            seedText = File.ReadAllText(seedPath);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[FileSystemJsonSeedProvider] Could not read seed {seedFileName}. {exception.Message}");
            return false;
        }
    }
}
