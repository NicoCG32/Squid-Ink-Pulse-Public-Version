using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PufferfishEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Movement")]
    [SerializeField] private float fallSpeed = 0.2f;
    [SerializeField] private float expandedRiseSpeedMultiplier = 2f;

    [Header("Expansion")]
    [SerializeField] private float proximityRadius = 2.5f;
    [SerializeField] private float expandedScaleMultiplier = 2f;
    [SerializeField] private float expansionSmoothSpeed = 8f;

    private Vector3 baseScale;
    private bool isExpanded;

    private void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
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

    public void InitializeEnemySpawnContext(
        Camera cameraReference,
        Collider2D playerTopBorderReference,
        Collider2D playerBottomBorderReference,
        Transform playerReference)
    {
        topBorder = playerTopBorderReference;
        bottomBorder = playerBottomBorderReference;
        player = playerReference;
        ClampBelowTopBorder();
    }

    private void MoveVertically()
    {
        float direction = isExpanded ? 1f : -1f;
        float speedMultiplier = isExpanded ? expandedRiseSpeedMultiplier : 1f;
        transform.position += Vector3.up * (direction * fallSpeed * speedMultiplier * Time.deltaTime);
    }

    private void UpdateExpansion()
    {
        float targetMultiplier = isExpanded ? expandedScaleMultiplier : 1f;
        Vector3 targetScale = baseScale * targetMultiplier;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            expansionSmoothSpeed * Time.deltaTime);
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

        return Vector2.Distance(transform.position, player.position) <= proximityRadius;
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
        if ((topBorder == null || bottomBorder == null)
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }
}
