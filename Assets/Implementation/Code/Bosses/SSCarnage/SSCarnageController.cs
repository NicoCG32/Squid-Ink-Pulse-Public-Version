using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum SSCarnageAttackState
{
    Inactive,
    Warning,
    DeployingNet,
    NetActive,
    Resolved,
    Failed,
    Exiting,
    Finished
}

[DisallowMultipleComponent]
public class SSCarnageController : MonoBehaviour, IBossSpawnContextReceiver
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Collider2D playerTopBorder;
    [SerializeField] private Transform ownedObjectsParent;

    [Header("Warning")]
    [SerializeField] private float warningDuration = 10f;
    [SerializeField, Range(0f, 1f)] private float warningViewportX = 0.85f;
    [FormerlySerializedAs("verticalOffsetBelowTopBoundary")]
    [SerializeField] private float verticalOffsetAbovePlayerTopBoundary = 0.75f;
    [SerializeField] private float followSmoothTime = 0.2f;
    [SerializeField] private bool destroyAfterNetDeploy = true;
    [SerializeField] private float destroyDelayAfterNetDeploy = 0.5f;
    [SerializeField] private float exitDistanceFromCameraRight = 4f;
    [SerializeField] private float exitSpeed = 8f;

    [Header("Carnage Net")]
    [SerializeField] private GameObject bossNetWallPrefab;
    [SerializeField] private float netSpawnDistanceFromCameraRight = 2f;
    [SerializeField, Range(0f, 1f)] private float netViewportY = 0.5f;
    [SerializeField] private bool deployNetOnStart = true;

    private Vector3 followVelocity;
    private float warningTimer;
    private float destroyTimer;
    private bool netDeployed;
    private bool exitStarted;
    private SSCarnageNetWall activeNetWall;

    public SSCarnageAttackState CurrentAttackState { get; private set; } = SSCarnageAttackState.Inactive;
    public event Action<SSCarnageAttackState, SSCarnageAttackState> AttackStateChanged;

    private void Awake()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        ResolveProgressionReference();
        ResolveBoundaryReferences();
    }

    private void Start()
    {
        ApplyAttackState(deployNetOnStart ? SSCarnageAttackState.Warning : SSCarnageAttackState.Inactive, force: true);
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

        ResolveBoundaryReferences();

        if (deployNetOnStart && !netDeployed)
        {
            FollowWarningPosition();

            warningTimer += Time.deltaTime;
            if (warningTimer >= warningDuration)
            {
                DeployNetWall();
            }
        }

        UpdateDestroyAfterDeploy();
    }

    public void InitializeBossSpawnContext(
        GameSessionController sessionReference,
        RunProgressionDirector progressionReference,
        Camera cameraReference,
        Collider2D topBorderReference,
        Collider2D bottomBorderReference,
        Transform parentReference)
    {
        session = sessionReference;
        progression = progressionReference;
        gameplayCamera = cameraReference;
        topBorder = topBorderReference;
        bottomBorder = bottomBorderReference;
        ownedObjectsParent = parentReference;
        ResolveBoundaryReferences();
        transform.position = CalculateWarningPosition();
        ApplyAttackState(deployNetOnStart ? SSCarnageAttackState.Warning : SSCarnageAttackState.Inactive);
    }

    public void DeployNetWall()
    {
        if (netDeployed || bossNetWallPrefab == null || gameplayCamera == null)
        {
            return;
        }

        netDeployed = true;
        ApplyAttackState(SSCarnageAttackState.DeployingNet);

        Vector3 spawnPosition = CalculateNetSpawnPosition();
        Transform parent = ownedObjectsParent != null ? ownedObjectsParent : transform.parent;
        GameObject wallInstance = Instantiate(bossNetWallPrefab, spawnPosition, Quaternion.identity, parent);

        if (wallInstance.TryGetComponent(out SSCarnageNetWall netWall))
        {
            activeNetWall = netWall;
            activeNetWall.Resolved += HandleNetResolved;
            activeNetWall.Failed += HandleNetFailed;
            netWall.Initialize(session, progression, gameplayCamera, topBorder, bottomBorder);
            ApplyAttackState(SSCarnageAttackState.NetActive);
            return;
        }

        ApplyAttackState(SSCarnageAttackState.Finished);
    }

    private void FollowWarningPosition()
    {
        if (gameplayCamera == null)
        {
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            CalculateWarningPosition(),
            ref followVelocity,
            followSmoothTime);
    }

    private Vector3 CalculateWarningPosition()
    {
        if (gameplayCamera == null)
        {
            return transform.position;
        }

        float depthToWorldZero = Mathf.Abs(gameplayCamera.transform.position.z);
        Vector3 viewportPosition = gameplayCamera.ViewportToWorldPoint(new Vector3(warningViewportX, 0.5f, depthToWorldZero));
        float fallbackTopY = gameplayCamera.ViewportToWorldPoint(new Vector3(warningViewportX, 1f, depthToWorldZero)).y;
        float y = playerTopBorder != null
            ? playerTopBorder.bounds.max.y + verticalOffsetAbovePlayerTopBoundary
            : fallbackTopY - verticalOffsetAbovePlayerTopBoundary;

        return new Vector3(viewportPosition.x, y, 0f);
    }

    private void UpdateDestroyAfterDeploy()
    {
        if (!destroyAfterNetDeploy
            || (CurrentAttackState != SSCarnageAttackState.Resolved
                && CurrentAttackState != SSCarnageAttackState.Failed
                && CurrentAttackState != SSCarnageAttackState.Exiting))
        {
            return;
        }

        if (!exitStarted)
        {
            destroyTimer += Time.deltaTime;
            if (destroyTimer < destroyDelayAfterNetDeploy)
            {
                return;
            }

            exitStarted = true;
            ApplyAttackState(SSCarnageAttackState.Exiting);
        }

        Vector3 exitPosition = CalculateExitPosition();
        transform.position = Vector3.MoveTowards(transform.position, exitPosition, exitSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - exitPosition.x) <= 0.01f)
        {
            ApplyAttackState(SSCarnageAttackState.Finished);
            Destroy(gameObject);
        }
    }

    private Vector3 CalculateNetSpawnPosition()
    {
        float depthToWorldZero = Mathf.Abs(gameplayCamera.transform.position.z);
        Vector3 rightEdge = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, netViewportY, depthToWorldZero));
        return new Vector3(rightEdge.x + netSpawnDistanceFromCameraRight, rightEdge.y, 0f);
    }

    private Vector3 CalculateExitPosition()
    {
        if (gameplayCamera == null)
        {
            return transform.position + Vector3.right * exitDistanceFromCameraRight;
        }

        float depthToWorldZero = Mathf.Abs(gameplayCamera.transform.position.z);
        Vector3 rightEdge = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depthToWorldZero));
        return new Vector3(rightEdge.x + exitDistanceFromCameraRight, transform.position.y, transform.position.z);
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || gameplayCamera == null || topBorder == null || bottomBorder == null || bossNetWallPrefab == null)
        {
            Debug.LogWarning(
                "[SSCarnageController] Faltan referencias. El director debe entregar Session, Camera, TopBorder y BottomBorder; el prefab debe tener BossNetWallPrefab asignado.",
                this);
        }
    }

    private void ResolveProgressionReference()
    {
        if (progression == null && RunProgressionDirector.HasInstance)
        {
            progression = RunProgressionDirector.Instance;
        }
    }

    private void ResolveBoundaryReferences()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if ((topBorder == null || bottomBorder == null)
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }

        if (playerTopBorder == null
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D resolvedPlayerTop, out _))
        {
            playerTopBorder = resolvedPlayerTop;
        }
    }

    private void HandleNetResolved()
    {
        UnsubscribeFromActiveNetWall();
        ApplyAttackState(SSCarnageAttackState.Resolved);
    }

    private void HandleNetFailed()
    {
        UnsubscribeFromActiveNetWall();
        ApplyAttackState(SSCarnageAttackState.Failed);
    }

    private void ApplyAttackState(SSCarnageAttackState nextState, bool force = false)
    {
        SSCarnageAttackState previousState = CurrentAttackState;
        if (!force && previousState == nextState)
        {
            return;
        }

        CurrentAttackState = nextState;
        AttackStateChanged?.Invoke(previousState, nextState);
    }

    private void UnsubscribeFromActiveNetWall()
    {
        if (activeNetWall == null)
        {
            return;
        }

        activeNetWall.Resolved -= HandleNetResolved;
        activeNetWall.Failed -= HandleNetFailed;
        activeNetWall = null;
    }

    private void OnDestroy()
    {
        UnsubscribeFromActiveNetWall();
    }
}
