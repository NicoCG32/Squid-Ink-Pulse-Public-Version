using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class PufferfishEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private PufferfishEnemyTuning tuning = new();
    private Vector3 baseScale;
    private bool isExpanded;
    private bool hasStartedInflateAnimation;
    private bool hasStoppedInflateAnimation;
    private Transform player;
    private CircleCollider2D bodyCollider;
    private Collider2D topBorder;
    private Animator inflateAnimator;
    private int inflateLayerIndex;
    private int inflateStateHash;

    private void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CircleCollider2D>();
        }

        inflateAnimator = GetComponentInChildren<Animator>(includeInactive: true);
        ConfigureInflateAnimationInitialState();
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
        StopInflateAnimationAfterOnePass();
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
        if (isExpanded || !ShouldExpand())
        {
            return;
        }

        isExpanded = true;
        PlayInflateAnimationOnce();
    }

    private bool ShouldExpand()
    {
        if (player == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, player.position) <= tuning.ProximityRadius;
    }

    private void ConfigureInflateAnimationInitialState()
    {
        if (inflateAnimator == null)
        {
            return;
        }

        inflateLayerIndex = 0;
        AnimatorStateInfo stateInfo = inflateAnimator.GetCurrentAnimatorStateInfo(inflateLayerIndex);
        inflateStateHash = stateInfo.shortNameHash;
        inflateAnimator.speed = 0f;
    }

    private void PlayInflateAnimationOnce()
    {
        if (inflateAnimator == null || hasStartedInflateAnimation)
        {
            return;
        }

        hasStartedInflateAnimation = true;
        hasStoppedInflateAnimation = false;
        inflateAnimator.enabled = true;
        inflateAnimator.speed = 1f;

        int stateHash = inflateStateHash != 0
            ? inflateStateHash
            : inflateAnimator.GetCurrentAnimatorStateInfo(inflateLayerIndex).shortNameHash;

        if (stateHash != 0)
        {
            inflateAnimator.Play(stateHash, inflateLayerIndex, 0f);
        }
    }

    private void StopInflateAnimationAfterOnePass()
    {
        if (inflateAnimator == null || !hasStartedInflateAnimation || hasStoppedInflateAnimation)
        {
            return;
        }

        AnimatorStateInfo stateInfo = inflateAnimator.GetCurrentAnimatorStateInfo(inflateLayerIndex);
        if (stateInfo.normalizedTime < 1f)
        {
            return;
        }

        inflateAnimator.speed = 0f;
        hasStoppedInflateAnimation = true;
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
