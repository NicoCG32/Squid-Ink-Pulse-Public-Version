using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SSCarnageNetWall : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Collider2D wallCollider;

    [Header("Collision Rules")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyWhenBroken = true;

    [Header("Camera Fit")]
    [SerializeField] private bool fitHeightToBoundaries = true;
    [SerializeField] private float wallWidth = 0.75f;

    private bool isBroken;

    private void Awake()
    {
        if (wallCollider == null)
        {
            wallCollider = GetComponent<Collider2D>();
        }

        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }
    }

    private void Start()
    {
        FitToVerticalBoundaries();
        WarnIfMissingReferences();
    }

    public void Initialize(
        GameSessionController sessionReference,
        Camera cameraReference,
        Collider2D topBorderReference,
        Collider2D bottomBorderReference)
    {
        session = sessionReference;
        gameplayCamera = cameraReference;
        topBorder = topBorderReference;
        bottomBorder = bottomBorderReference;
        FitToVerticalBoundaries();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken || session == null || !session.IsPlaying)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        InkPulseController pulse = other.GetComponentInParent<InkPulseController>();
        if (pulse != null && pulse.IsPulseActive)
        {
            BreakWall();
            return;
        }

        session.RequestGameOver();
    }

    private void BreakWall()
    {
        isBroken = true;

        if (wallCollider != null)
        {
            wallCollider.enabled = false;
        }

        if (destroyWhenBroken)
        {
            Destroy(gameObject);
        }
    }

    private void FitToVerticalBoundaries()
    {
        if (!fitHeightToBoundaries)
        {
            return;
        }

        Vector2 verticalBounds = CalculateVerticalBounds();
        float bottomY = verticalBounds.x;
        float topY = verticalBounds.y;
        float height = Mathf.Max(1f, topY - bottomY);

        transform.localScale = new Vector3(wallWidth, height, 1f);
        transform.position = new Vector3(transform.position.x, (bottomY + topY) * 0.5f, transform.position.z);
    }

    private Vector2 CalculateVerticalBounds()
    {
        if (topBorder != null && bottomBorder != null)
        {
            return new Vector2(bottomBorder.bounds.max.y, topBorder.bounds.min.y);
        }

        if (gameplayCamera == null)
        {
            return new Vector2(transform.position.y - 0.5f, transform.position.y + 0.5f);
        }

        float depthToWall = Mathf.Abs(gameplayCamera.transform.position.z - transform.position.z);
        float bottomY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depthToWall)).y;
        float topY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, depthToWall)).y;
        return new Vector2(bottomY, topY);
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || gameplayCamera == null || topBorder == null || bottomBorder == null || wallCollider == null)
        {
            Debug.LogWarning(
                "[SSCarnageNetWall] Faltan referencias. El SS Carnage debe entregar Session, Camera, TopBorder y BottomBorder, y el prefab debe tener Collider2D.",
                this);
        }
    }
}
