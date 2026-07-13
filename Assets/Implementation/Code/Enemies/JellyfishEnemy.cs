using UnityEngine;

[DisallowMultipleComponent]
public class JellyfishEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private JellyfishEnemyTuning tuning = new();
    private CircleCollider2D bounceCollider;
    private BoxCollider2D deathCollider;
    private float nextBounceAllowedTime;

    private void Awake()
    {
        ResolveColliderReferences();
    }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyBounce(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyBounce(other);
    }

    private void TryApplyBounce(Collider2D other)
    {
        if (!GameSessionController.IsGameplayActive
            || other == null
            || Time.time < nextBounceAllowedTime
            || !other.CompareTag(GameplayTagCatalog.Player))
        {
            return;
        }

        ResolveColliderReferences();
        if (bounceCollider == null || !bounceCollider.IsTouching(other))
        {
            return;
        }

        if (deathCollider != null && deathCollider.IsTouching(other))
        {
            return;
        }

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            return;
        }

        playerMovement.ApplyExternalVerticalImpulse(
            tuning.BounceVerticalVelocity,
            tuning.BounceDuration);
        nextBounceAllowedTime = Time.time + tuning.BounceCooldown;
    }

    private void ResolveColliderReferences()
    {
        if (bounceCollider == null)
        {
            bounceCollider = GetComponent<CircleCollider2D>();
        }

        if (deathCollider == null)
        {
            deathCollider = GetComponent<BoxCollider2D>();
        }
    }
}
