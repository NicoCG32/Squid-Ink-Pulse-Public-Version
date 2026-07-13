using UnityEngine;

[DisallowMultipleComponent]
public class RayEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private RayEnemyTuning tuning = new();
    private float verticalDirection = 1f;
    private Collider2D bodyCollider;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        RandomizeDiagonalDirection();
    }

    private void Update()
    {
        if (!GameSessionController.IsGameplayActive)
        {
            return;
        }

        float verticalInset = bodyCollider != null ? bodyCollider.bounds.extents.y : 0f;
        bool hasVerticalRange = BoundaryReferenceResolver.TryResolveInnerVerticalRange(
            BoundaryReferenceDomain.Player,
            verticalInset,
            out Vector2 verticalRange);

        transform.position = CalculateNextPosition(
            transform.position,
            tuning.HorizontalSpeed,
            tuning.VerticalSpeed,
            verticalDirection,
            Time.deltaTime,
            hasVerticalRange,
            verticalRange,
            out verticalDirection);
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

    public static Vector3 CalculateNextPosition(
        Vector3 currentPosition,
        float horizontalSpeed,
        float verticalSpeed,
        float currentVerticalDirection,
        float deltaTime,
        bool hasVerticalRange,
        Vector2 verticalRange,
        out float nextVerticalDirection)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        float safeHorizontalSpeed = Mathf.Max(0f, horizontalSpeed);
        float safeVerticalSpeed = Mathf.Max(0f, verticalSpeed);

        nextVerticalDirection = currentVerticalDirection;

        Vector3 nextPosition = currentPosition;
        nextPosition.x -= safeHorizontalSpeed * safeDeltaTime;
        nextPosition.y += safeVerticalSpeed * currentVerticalDirection * safeDeltaTime;

        if (!hasVerticalRange)
        {
            return nextPosition;
        }

        float minY = Mathf.Min(verticalRange.x, verticalRange.y);
        float maxY = Mathf.Max(verticalRange.x, verticalRange.y);

        if (currentVerticalDirection > 0f && nextPosition.y >= maxY)
        {
            nextPosition.y = maxY;
            nextVerticalDirection = 0f;
        }
        else if (currentVerticalDirection < 0f && nextPosition.y <= minY)
        {
            nextPosition.y = minY;
            nextVerticalDirection = 0f;
        }
        else if (currentVerticalDirection == 0f)
        {
            nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);
        }

        return nextPosition;
    }
}
