using System.IO;
using UnityEngine;

public sealed class ResourcesJsonSeedProvider : IJsonSeedProvider
{
    private readonly string resourceDirectory;

    public ResourcesJsonSeedProvider(string resourceDirectory)
    {
        this.resourceDirectory = resourceDirectory?.Trim().Trim('/');
    }

    public bool TryGetSeedText(string seedFileName, out string seedText)
    {
        seedText = null;
        if (string.IsNullOrWhiteSpace(seedFileName))
        {
            return false;
        }

        string resourceName = Path.GetFileNameWithoutExtension(seedFileName);
        string resourcePath = string.IsNullOrWhiteSpace(resourceDirectory)
            ? resourceName
            : $"{resourceDirectory}/{resourceName}";
        TextAsset seedAsset = Resources.Load<TextAsset>(resourcePath);
        if (seedAsset == null)
        {
            return false;
        }

        seedText = seedAsset.text;
        return !string.IsNullOrWhiteSpace(seedText);
    }
}
