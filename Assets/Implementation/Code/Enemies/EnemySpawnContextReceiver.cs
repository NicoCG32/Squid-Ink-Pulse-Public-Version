using UnityEngine;

public readonly struct EnemySpawnContext
{
    public EnemySpawnContext(
        Camera cameraReference,
        Transform playerReference,
        PufferfishEnemyTuning pufferfishTuning,
        FishingRodEnemyTuning fishingRodTuning)
    {
        CameraReference = cameraReference;
        PlayerReference = playerReference;
        PufferfishTuning = pufferfishTuning;
        FishingRodTuning = fishingRodTuning;
    }

    public Camera CameraReference { get; }
    public Transform PlayerReference { get; }
    public PufferfishEnemyTuning PufferfishTuning { get; }
    public FishingRodEnemyTuning FishingRodTuning { get; }
}

public interface IEnemySpawnContextReceiver
{
    void InitializeEnemySpawnContext(EnemySpawnContext context);
}
