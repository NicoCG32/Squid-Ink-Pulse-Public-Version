public interface IJsonSeedProvider
{
    bool TryGetSeedText(string seedFileName, out string seedText);
}
