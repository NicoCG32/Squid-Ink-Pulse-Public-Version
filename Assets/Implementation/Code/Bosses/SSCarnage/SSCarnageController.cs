using UnityEngine;

[DisallowMultipleComponent]
public class SSCarnageController : MonoBehaviour, IBossSpawnContextReceiver
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Transform ownedObjectsParent;

    [Header("Warning")]
    [SerializeField] private float warningDuration = 10f;
    [SerializeField, Range(0f, 1f)] private float warningViewportX = 0.65f;
    [SerializeField] private float verticalOffsetAboveTopBoundary = 2f;
    [SerializeField] private float followSmoothTime = 0.2f;
    [SerializeField] private bool destroyAfterNetDeploy = true;
    [SerializeField] private float destroyDelayAfterNetDeploy = 0.5f;

    [Header("Carnage Net")]
    [SerializeField] private GameObject bossNetWallPrefab;
    [SerializeField] private float netSpawnDistanceFromCameraRight = 2f;
    [SerializeField, Range(0f, 1f)] private float netViewportY = 0.5f;
    [SerializeField] private bool deployNetOnStart = true;

    private Vector3 followVelocity;
    private float warningTimer;
    private float destroyTimer;
    private bool netDeployed;

    private void Awake()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }
    }

    private void Start()
    {
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

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
        Camera cameraReference,
        Collider2D topBorderReference,
        Collider2D bottomBorderReference,
        Transform parentReference)
    {
        session = sessionReference;
        gameplayCamera = cameraReference;
        topBorder = topBorderReference;
        bottomBorder = bottomBorderReference;
        ownedObjectsParent = parentReference;
        transform.position = CalculateWarningPosition();
    }

    public void DeployNetWall()
    {
        if (netDeployed || bossNetWallPrefab == null || gameplayCamera == null)
        {
            return;
        }

        netDeployed = true;

        Vector3 spawnPosition = CalculateNetSpawnPosition();
        Transform parent = ownedObjectsParent != null ? ownedObjectsParent : transform.parent;
        GameObject wallInstance = Instantiate(bossNetWallPrefab, spawnPosition, Quaternion.identity, parent);

        if (wallInstance.TryGetComponent(out SSCarnageNetWall netWall))
        {
            netWall.Initialize(session, gameplayCamera, topBorder, bottomBorder);
        }
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
        float y = topBorder != null
            ? topBorder.bounds.max.y + verticalOffsetAboveTopBoundary
            : viewportPosition.y + verticalOffsetAboveTopBoundary;

        return new Vector3(viewportPosition.x, y, 0f);
    }

    private void UpdateDestroyAfterDeploy()
    {
        if (!netDeployed || !destroyAfterNetDeploy)
        {
            return;
        }

        destroyTimer += Time.deltaTime;
        if (destroyTimer >= destroyDelayAfterNetDeploy)
        {
            Destroy(gameObject);
        }
    }

    private Vector3 CalculateNetSpawnPosition()
    {
        float depthToWorldZero = Mathf.Abs(gameplayCamera.transform.position.z);
        Vector3 rightEdge = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, netViewportY, depthToWorldZero));
        return new Vector3(rightEdge.x + netSpawnDistanceFromCameraRight, rightEdge.y, 0f);
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
}
