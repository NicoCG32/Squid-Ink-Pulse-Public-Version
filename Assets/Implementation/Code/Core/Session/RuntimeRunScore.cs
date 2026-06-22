using System;

public static class RuntimeRunScore
{
    private static long totalScore;
    private static long lastCompletedScore;

    public static long TotalScore => totalScore;
    public static long LastCompletedScore => lastCompletedScore;
    public static event Action<long> ScoreChanged;

    public static void Add(long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        totalScore = Math.Max(0, totalScore + amount);
        ScoreChanged?.Invoke(totalScore);
    }

    public static long CaptureCompletedScore()
    {
        lastCompletedScore = Math.Max(0, totalScore);
        return lastCompletedScore;
    }

    public static void ResetForRuntime()
    {
        totalScore = 0;
        ScoreChanged?.Invoke(totalScore);
    }
}
