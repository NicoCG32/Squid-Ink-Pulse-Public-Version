using System;
using System.Linq;

[Serializable]
public class LocalLeaderboardSaveData
{
    public int version = PlayerProfileRepository.LeaderboardVersion;
    public int maxEntries = 20;
    public LocalLeaderboardEntrySaveData[] entries = Array.Empty<LocalLeaderboardEntrySaveData>();

    public static LocalLeaderboardSaveData CreateDefault()
    {
        LocalLeaderboardSaveData data = new();
        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        version = Math.Max(1, version);
        maxEntries = Math.Max(1, maxEntries);
        entries = entries?
            .Where(entry => entry != null)
            .Select(entry =>
            {
                entry.Normalize();
                return entry;
            })
            .Where(entry => entry.score > 0)
            .OrderByDescending(entry => entry.score)
            .ThenBy(entry => entry.timestampUtc)
            .Take(maxEntries)
            .ToArray() ?? Array.Empty<LocalLeaderboardEntrySaveData>();
    }
}

[Serializable]
public class LocalLeaderboardEntrySaveData
{
    public string playerName = "Player";
    public long score;
    public string zoneId = string.Empty;
    public string timestampUtc = string.Empty;

    public void Normalize()
    {
        playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
        score = Math.Max(0, score);
        zoneId = zoneId?.Trim() ?? string.Empty;
        timestampUtc = string.IsNullOrWhiteSpace(timestampUtc)
            ? DateTime.UtcNow.ToString("O")
            : timestampUtc.Trim();
    }
}
