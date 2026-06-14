using System;
using System.Linq;

public static class LocalLeaderboardRepository
{
    public static event Action<LocalLeaderboardSaveData> LeaderboardChanged;

    public static LocalLeaderboardSaveData Load()
    {
        return PlayerProfileRepository.LoadLocalLeaderboard();
    }

    public static void Save(LocalLeaderboardSaveData data)
    {
        PlayerProfileRepository.SaveLocalLeaderboard(data);
        LeaderboardChanged?.Invoke(data);
    }

    public static void RecordScore(string playerName, long score, string zoneId = "")
    {
        if (score <= 0)
        {
            return;
        }

        LocalLeaderboardSaveData data = Load();
        LocalLeaderboardEntrySaveData entry = new()
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim(),
            score = score,
            zoneId = zoneId?.Trim() ?? string.Empty,
            timestampUtc = DateTime.UtcNow.ToString("O")
        };

        data.entries = data.entries
            .Concat(new[] { entry })
            .ToArray();

        data.Normalize();
        Save(data);
    }

    public static void Clear()
    {
        Save(LocalLeaderboardSaveData.CreateDefault());
    }
}
