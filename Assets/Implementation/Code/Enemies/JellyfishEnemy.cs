using UnityEngine;

[DisallowMultipleComponent]
public class JellyfishEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private JellyfishEnemyTuning tuning = new();

    private void Update()
    {
        if (!GameSessionController.IsGameplayActive)
        {
            return;
        }

        transform.position += Vector3.up * (tuning.UpwardSpeed * Time.deltaTime);
    }

    public void InitializeEnemySpawnContext(EnemySpawnContext context)
    {
        tuning = context.JellyfishTuning ?? new JellyfishEnemyTuning();
    }
}
