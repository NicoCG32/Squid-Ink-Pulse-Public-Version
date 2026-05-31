using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCollision : MonoBehaviour
{
    private const string ShrimpTag = "Shrimp";

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulseController;
    [SerializeField] private ShrimpCollector shrimpCollector;
    [SerializeField] private Collider2D damageCollider;

    [Header("Rules")]
    [SerializeField] private bool destroyHazardDuringPulse;

    private void Awake()
    {
        WarnIfMissingReferences();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

        if (!IsDamageColliderTouching(other))
        {
            return;
        }

        if (other.CompareTag(ShrimpTag))
        {
            shrimpCollector?.Collect(other.gameObject);
            return;
        }

        if (EnemyTagCatalog.IsEnemy(other))
        {
            HandleHazardCollision(other);
        }
    }

    private void HandleHazardCollision(Collider2D hazard)
    {
        if (inkPulseController != null && inkPulseController.IsPulseActive)
        {
            if (destroyHazardDuringPulse && hazard != null)
            {
                Destroy(hazard.gameObject);
            }

            return;
        }

        if (session == null)
        {
            Debug.LogError("[PlayerCollision] No se puede declarar Game Over sin GameSessionController asignado.", this);
            return;
        }

        session.RequestGameOver();
    }

    private bool IsDamageColliderTouching(Collider2D other)
    {
        return damageCollider == null || damageCollider.IsTouching(other);
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || inkPulseController == null || shrimpCollector == null || damageCollider == null)
        {
            Debug.LogWarning(
                "[PlayerCollision] Faltan referencias. Asigna Session, InkPulseController, ShrimpCollector y DamageCollider en el Inspector.",
                this);
        }
    }
}
