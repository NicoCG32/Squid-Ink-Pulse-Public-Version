using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulseController;
    [SerializeField] private ShrimpCollector shrimpCollector;
    [SerializeField] private PlayerGadgetInventory gadgetInventory;
    [SerializeField] private Collider2D damageCollider;

    public event Action<Collider2D> HazardIgnoredByInkPulse;
    public event Action<Collider2D> HazardBlockedByShellShield;
    public event Action<Collider2D> HazardCausedGameOver;

    private void Awake()
    {
        ResolveReferences();
        WarnIfMissingReferences();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ResolveReferences();

        if (session == null || !session.IsPlaying)
        {
            return;
        }

        if (!IsDamageColliderTouching(other))
        {
            return;
        }

        if (other.CompareTag(GameplayTagCatalog.Shrimp))
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
            HazardIgnoredByInkPulse?.Invoke(hazard);
            return;
        }

        if (gadgetInventory != null && gadgetInventory.TryConsumeShellShield())
        {
            HazardBlockedByShellShield?.Invoke(hazard);
            if (hazard != null)
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

        HazardCausedGameOver?.Invoke(hazard);
        session.RequestGameOver();
    }

    private bool IsDamageColliderTouching(Collider2D other)
    {
        return damageCollider == null || damageCollider.IsTouching(other);
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (inkPulseController == null)
        {
            inkPulseController = GetComponent<InkPulseController>();
        }

        if (shrimpCollector == null)
        {
            shrimpCollector = GetComponent<ShrimpCollector>();
        }

        if (gadgetInventory == null)
        {
            gadgetInventory = GetComponent<PlayerGadgetInventory>();
        }

        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || inkPulseController == null || shrimpCollector == null || gadgetInventory == null || damageCollider == null)
        {
            Debug.LogWarning(
                "[PlayerCollision] Faltan referencias. Asigna Session, InkPulseController, ShrimpCollector, GadgetInventory y DamageCollider en el Inspector.",
                this);
        }
    }
}
