using UnityEngine;

public interface IEnemySpawnContextReceiver
{
    void InitializeEnemySpawnContext(
        Camera cameraReference,
        Collider2D playerTopBorderReference,
        Collider2D playerBottomBorderReference,
        Transform playerReference);
}
