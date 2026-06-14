using System;

[Serializable]
public class PlayerRecordsSaveData
{
    public int version = PlayerProfileRepository.RecordsVersion;
    public int totalShrimps;
    public long bestScore;
    public int totalRuns;
    public int totalPortalsCrossed;
    public int totalShrimpsCollected;

    public static PlayerRecordsSaveData CreateDefault()
    {
        PlayerRecordsSaveData data = new();
        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        version = Math.Max(1, version);
        totalShrimps = Math.Max(0, totalShrimps);
        bestScore = Math.Max(0, bestScore);
        totalRuns = Math.Max(0, totalRuns);
        totalPortalsCrossed = Math.Max(0, totalPortalsCrossed);
        totalShrimpsCollected = Math.Max(0, totalShrimpsCollected);
    }
}
