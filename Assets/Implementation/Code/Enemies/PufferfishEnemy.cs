using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class PufferfishEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private PufferfishEnemyTuning tuning = new();
    private Vector3 baseScale;
    private bool isExpanded;
    private Transform player;
    private CircleCollider2D bodyCollider;
    private Collider2D topBorder;

    private void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CircleCollider2D>();
        }

        baseScale = transform.localScale;
    }

    private void Start()
    {
        ResolveSceneReferences();
        ClampBelowTopBorder();
    }

    private void Update()
    {
        if (!GameSessionController.IsGameplayActive)
        {
            return;
        }

        ResolveSceneReferences();
        UpdateExpansionState();
        MoveVertically();
        UpdateExpansion();
        ClampBelowTopBorder();
    }

    public void InitializeEnemySpawnContext(EnemySpawnContext context)
    {
        player = context.PlayerReference;
        tuning = context.PufferfishTuning ?? new PufferfishEnemyTuning();
        ResolveSceneReferences();
        ClampBelowTopBorder();
    }

    private void MoveVertically()
    {
        float direction = isExpanded ? 1f : -1f;
        float speedMultiplier = isExpanded ? tuning.ExpandedRiseSpeedMultiplier : 1f;
        transform.position += Vector3.up * (direction * tuning.FallSpeed * speedMultiplier * Time.deltaTime);
    }

    private void UpdateExpansion()
    {
        float targetMultiplier = isExpanded ? tuning.ExpandedScaleMultiplier : 1f;
        Vector3 targetScale = baseScale * targetMultiplier;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            tuning.ExpansionSmoothSpeed * Time.deltaTime);
    }

    private void UpdateExpansionState()
    {
        isExpanded = ShouldExpand();
    }

    private bool ShouldExpand()
    {
        if (player == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, player.position) <= tuning.ProximityRadius;
    }

    private void ClampBelowTopBorder()
    {
        if (topBorder == null || bodyCollider == null)
        {
            return;
        }

        float overshoot = bodyCollider.bounds.max.y - topBorder.bounds.min.y;
        if (overshoot > 0f)
        {
            transform.position -= Vector3.up * overshoot;
        }
    }

    private void ResolveSceneReferences()
    {
        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D resolvedTop, out _))
        {
            topBorder = resolvedTop;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(GameplayTagCatalog.Player);
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }
}
