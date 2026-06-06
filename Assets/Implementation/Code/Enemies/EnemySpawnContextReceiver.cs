using UnityEngine;

public readonly struct EnemySpawnContext
{
    public EnemySpawnContext(
        Camera cameraReference,
        Transform playerReference,
        PufferfishEnemyTuning pufferfishTuning)
    {
        CameraReference = cameraReference;
        PlayerReference = playerReference;
        PufferfishTuning = pufferfishTuning;
    }

    public Camera CameraReference { get; }
    public Transform PlayerReference { get; }
    public PufferfishEnemyTuning PufferfishTuning { get; }
}

public interface IEnemySpawnContextReceiver
{
    void InitializeEnemySpawnContext(EnemySpawnContext context);
}
