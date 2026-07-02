using UnityEngine;

public readonly struct EnemySpawnContext
{
    public EnemySpawnContext(
        Camera cameraReference,
        Transform playerReference,
        PufferfishEnemyTuning pufferfishTuning,
        FishingRodEnemyTuning fishingRodTuning,
        RayEnemyTuning rayTuning,
        JellyfishEnemyTuning jellyfishTuning)
    {
        CameraReference = cameraReference;
        PlayerReference = playerReference;
        PufferfishTuning = pufferfishTuning;
        FishingRodTuning = fishingRodTuning;
        RayTuning = rayTuning;
        JellyfishTuning = jellyfishTuning;
    }

    public Camera CameraReference { get; }
    public Transform PlayerReference { get; }
    public PufferfishEnemyTuning PufferfishTuning { get; }
    public FishingRodEnemyTuning FishingRodTuning { get; }
    public RayEnemyTuning RayTuning { get; }
    public JellyfishEnemyTuning JellyfishTuning { get; }
}
