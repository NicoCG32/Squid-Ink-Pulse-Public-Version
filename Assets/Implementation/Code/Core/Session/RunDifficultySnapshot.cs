public readonly struct RunDifficultySnapshot
{
    public RunDifficultySnapshot(
        float elapsedSeconds,
        float cycleElapsedSeconds,
        float distance,
        float intensity,
        float targetScrollSpeed,
        float spawnInterval,
        float bossInterval,
        int progressionCycle,
        RunEventState eventState)
    {
        ElapsedSeconds = elapsedSeconds;
        CycleElapsedSeconds = cycleElapsedSeconds;
        Distance = distance;
        Intensity = intensity;
        TargetScrollSpeed = targetScrollSpeed;
        SpawnInterval = spawnInterval;
        BossInterval = bossInterval;
        ProgressionCycle = progressionCycle;
        EventState = eventState;
    }

    public float ElapsedSeconds { get; }
    public float CycleElapsedSeconds { get; }
    public float Distance { get; }
    public float Intensity { get; }
    public float TargetScrollSpeed { get; }
    public float SpawnInterval { get; }
    public float BossInterval { get; }
    public int ProgressionCycle { get; }
    public RunEventState EventState { get; }
}
