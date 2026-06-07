using UnityEngine;

[DisallowMultipleComponent]
public class FishingRodEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private FishingRodEnemyTuning tuning = new();
    private Transform player;
    private Collider2D topBorder;
    private bool hasCapturedTarget;
    private bool hasReachedTarget;
    private float targetY;

    private void Start()
    {
        ResolveSceneReferences();

        if (!hasCapturedTarget)
        {
            CaptureTargetY();
            PlaceAtDropStartY();
        }
    }

    private void Update()
    {
        if (!GameSessionController.IsGameplayActive || !hasCapturedTarget || hasReachedTarget)
        {
            return;
        }

        MoveTowardCapturedTargetY();
    }

    public void InitializeEnemySpawnContext(EnemySpawnContext context)
    {
        player = context.PlayerReference;
        tuning = context.FishingRodTuning ?? new FishingRodEnemyTuning();

        ResolveSceneReferences();
        CaptureTargetY();
        PlaceAtDropStartY();
    }

    private void CaptureTargetY()
    {
        float resolvedTargetY = player != null ? player.position.y : transform.position.y;
        if (BoundaryReferenceResolver.TryResolveInnerVerticalRange(BoundaryReferenceDomain.Player, 0f, out Vector2 playerRange))
        {
            resolvedTargetY = Mathf.Clamp(resolvedTargetY, playerRange.x, playerRange.y);
        }

        targetY = resolvedTargetY;
        hasCapturedTarget = true;
    }

    private void PlaceAtDropStartY()
    {
        float startY = transform.position.y;
        if (topBorder != null)
        {
            startY = topBorder.bounds.min.y - tuning.StartYOffsetBelowTopBoundary;
        }

        if (startY <= targetY)
        {
            startY = targetY;
        }

        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        hasReachedTarget = Mathf.Abs(transform.position.y - targetY) <= tuning.ArriveDistance;
    }

    private void MoveTowardCapturedTargetY()
    {
        float nextY = Mathf.MoveTowards(transform.position.y, targetY, tuning.DropSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, nextY, transform.position.z);

        if (Mathf.Abs(nextY - targetY) > tuning.ArriveDistance)
        {
            return;
        }

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        hasReachedTarget = true;
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
