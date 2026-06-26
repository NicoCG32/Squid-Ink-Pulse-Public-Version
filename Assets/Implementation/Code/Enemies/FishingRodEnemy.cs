using UnityEngine;

[DisallowMultipleComponent]
public class FishingRodEnemy : MonoBehaviour, IEnemySpawnContextReceiver
{
    private enum DropState
    {
        WaitingForReadWindow,
        Windup,
        Dropping,
        Arrived
    }

    private FishingRodEnemyTuning tuning = new();
    private Transform player;
    private Camera gameplayCamera;
    private Collider2D topBorder;
    private bool hasCapturedTarget;
    private float targetY;
    private float windupTimer;
    private DropState dropState = DropState.WaitingForReadWindow;

    private void Start()
    {
        ResolveSceneReferences();

        if (!hasCapturedTarget)
        {
            PrepareDrop();
        }
    }

    private void Update()
    {
        if (!GameSessionController.IsGameplayActive || !hasCapturedTarget || dropState == DropState.Arrived)
        {
            return;
        }

        UpdateDropState();
    }

    public void InitializeEnemySpawnContext(EnemySpawnContext context)
    {
        gameplayCamera = context.CameraReference;
        player = context.PlayerReference;
        tuning = context.FishingRodTuning ?? new FishingRodEnemyTuning();

        ResolveSceneReferences();
        PrepareDrop();
    }

    private void PrepareDrop()
    {
        CaptureTargetY();
        PlaceAtDropStartY();
        ResetDropState();
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
    }

    private void ResetDropState()
    {
        windupTimer = 0f;
        dropState = Mathf.Abs(transform.position.y - targetY) <= tuning.ArriveDistance
            ? DropState.Arrived
            : DropState.WaitingForReadWindow;
    }

    private void UpdateDropState()
    {
        ApplyFastPaceHorizontalHold();

        switch (dropState)
        {
            case DropState.WaitingForReadWindow:
                if (IsInsideDescentReadWindow())
                {
                    BeginWindupOrDrop();
                }

                break;
            case DropState.Windup:
                windupTimer += Time.deltaTime;
                if (windupTimer >= tuning.DescentWindupSeconds)
                {
                    dropState = DropState.Dropping;
                }

                break;
            case DropState.Dropping:
                MoveTowardCapturedTargetY();
                break;
        }
    }

    private void ApplyFastPaceHorizontalHold()
    {
        if (!ShouldApplyFastPaceHorizontalHold())
        {
            return;
        }

        float depth = Mathf.Abs(gameplayCamera.transform.position.z - transform.position.z);
        Vector3 holdPosition = gameplayCamera.ViewportToWorldPoint(new Vector3(tuning.HorizontalHoldViewportX, 0.5f, depth));
        transform.position = new Vector3(holdPosition.x, transform.position.y, transform.position.z);
    }

    private bool ShouldApplyFastPaceHorizontalHold()
    {
        if (!tuning.EnableFastPaceHorizontalHold || dropState == DropState.Arrived)
        {
            return false;
        }

        ResolveCameraReference();
        if (gameplayCamera == null)
        {
            return false;
        }

        return GetCurrentHorizontalSpeed() >= tuning.HorizontalHoldMinScrollSpeed;
    }

    private float GetCurrentHorizontalSpeed()
    {
        float speed = 0f;
        if (player != null && player.TryGetComponent(out PlayerMovement movement))
        {
            speed = Mathf.Max(speed, movement.CurrentHorizontalSpeed);
        }

        if (RunProgressionDirector.HasInstance)
        {
            speed = Mathf.Max(speed, RunProgressionDirector.Instance.Current.TargetScrollSpeed);
        }

        return speed;
    }

    private bool IsInsideDescentReadWindow()
    {
        ResolveCameraReference();
        if (gameplayCamera == null)
        {
            return true;
        }

        Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(transform.position);
        float startViewportX = ShouldApplyFastPaceHorizontalHold()
            ? Mathf.Max(tuning.DescentStartViewportX, tuning.HorizontalHoldViewportX)
            : tuning.DescentStartViewportX;

        return viewportPoint.z >= 0f && viewportPoint.x <= startViewportX;
    }

    private void BeginWindupOrDrop()
    {
        windupTimer = 0f;
        dropState = tuning.DescentWindupSeconds > 0f
            ? DropState.Windup
            : DropState.Dropping;
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
        dropState = DropState.Arrived;
    }

    private void ResolveSceneReferences()
    {
        ResolveCameraReference();

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

    private void ResolveCameraReference()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }
}
