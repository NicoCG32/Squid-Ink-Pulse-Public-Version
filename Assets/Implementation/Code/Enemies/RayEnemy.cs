using UnityEngine;

[DisallowMultipleComponent]
public class RayEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private RayEnemyTuning tuning = new();
    private float verticalDirection = 1f;

    private void Awake()
    {
        RandomizeDiagonalDirection();
    }

    private void Update()
    {
        if (!GameSessionController.IsGameplayActive)
        {
            return;
        }

        Vector3 movement = new(
            -tuning.HorizontalSpeed,
            tuning.VerticalSpeed * verticalDirection,
            0f);
        transform.position += movement * Time.deltaTime;
    }

    public void InitializeEnemySpawnContext(EnemySpawnContext context)
    {
        tuning = context.RayTuning ?? new RayEnemyTuning();
        RandomizeDiagonalDirection();
    }

    private void RandomizeDiagonalDirection()
    {
        verticalDirection = Random.value < 0.5f ? -1f : 1f;
    }
}
