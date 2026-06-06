using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D playerCollider;

    [Header("Spawn")]
    [SerializeField] private bool randomizeInitialYWithinPlayerBoundaries = true;

    [Header("Horizontal Movement")]
    [SerializeField, FormerlySerializedAs("baseAutoScrollSpeed")] private float normalHorizontalSpeed = 5f;
    [SerializeField, FormerlySerializedAs("boostedAutoScrollSpeed")] private float inkPulseHorizontalSpeed = 15f;

    [Header("Vertical Movement")]
    [SerializeField, FormerlySerializedAs("baseSpeed")] private float normalVerticalSpeed = 5f;
    [SerializeField, FormerlySerializedAs("boostedSpeed")] private float inkPulseVerticalSpeed = 20f;
    [SerializeField] private float smoothSpeedTransition = 2f;

    [Header("Tilt")]
    [SerializeField] private float baseRotationZ = -90f;
    [SerializeField] private float maxTiltAngle = 12f;
    [SerializeField] private float tiltSmoothSpeed = 8f;

    private float currentHorizontalSpeed;
    private float currentVerticalSpeed;
    private bool inkPulseActive;
    private float previousY;
    private Collider2D topBorder;
    private Collider2D bottomBorder;
    private float minY;
    private float maxY;
    private bool hasVerticalLimits;

    public float CurrentHorizontalSpeed => currentHorizontalSpeed;
    public float CurrentVerticalSpeed => currentVerticalSpeed;

    private void Awake()
    {
        ResolveSceneReferences();
        ResetRuntimeState();
        WarnIfMissingReferences();
    }

    private void Start()
    {
        ResolveSceneReferences();
        if (UpdateVerticalLimitsFromBorders())
        {
            RandomizeInitialYInsideLimits();
            ClampPlayerInsideLimits();
        }

        previousY = transform.position.y;

        Vector3 startEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(startEuler.x, startEuler.y, baseRotationZ);
    }

    private void Update()
    {
        if (!IsGameplayActive())
        {
            return;
        }

        ResolveSceneReferences();
        if (!UpdateVerticalLimitsFromBorders())
        {
            return;
        }

        HandleMovement();
        UpdateSpeedTransition();
        ClampPlayerInsideLimits();
        UpdateTiltFromPositionDelta();
    }

    public void SetInkPulseActive(bool active)
    {
        inkPulseActive = active;

        if (inkPulseActive)
        {
            currentHorizontalSpeed = inkPulseHorizontalSpeed;
            currentVerticalSpeed = inkPulseVerticalSpeed;
        }
    }

    private void ResetRuntimeState()
    {
        currentHorizontalSpeed = inkPulseActive ? inkPulseHorizontalSpeed : normalHorizontalSpeed;
        currentVerticalSpeed = inkPulseActive ? inkPulseVerticalSpeed : normalVerticalSpeed;
    }

    private void HandleMovement()
    {
        if (gameplayCamera == null || Mouse.current == null || !hasVerticalLimits)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWithDepth = new Vector3(mousePos.x, mousePos.y, gameplayCamera.nearClipPlane + 10f);
        Vector3 worldMousePos = gameplayCamera.ScreenToWorldPoint(mouseWithDepth);
        worldMousePos.z = 0f;

        float nextX = transform.position.x + currentHorizontalSpeed * Time.deltaTime;
        float nextY = Mathf.MoveTowards(transform.position.y, worldMousePos.y, currentVerticalSpeed * Time.deltaTime);
        nextY = Mathf.Clamp(nextY, minY, maxY);

        transform.position = new Vector3(nextX, nextY, 0f);
    }

    private void UpdateSpeedTransition()
    {
        if (inkPulseActive)
        {
            return;
        }

        float targetHorizontalSpeed = progression != null
            ? progression.Current.TargetScrollSpeed
            : normalHorizontalSpeed;

        currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, targetHorizontalSpeed, smoothSpeedTransition * Time.deltaTime);
        currentVerticalSpeed = Mathf.Lerp(currentVerticalSpeed, normalVerticalSpeed, smoothSpeedTransition * Time.deltaTime);
    }

    private void UpdateTiltFromPositionDelta()
    {
        float deltaY = transform.position.y - previousY;
        float verticalSpeed = Time.deltaTime > 0f ? deltaY / Time.deltaTime : 0f;
        previousY = transform.position.y;
        UpdateTilt(verticalSpeed);
    }

    private void UpdateTilt(float verticalSpeed)
    {
        float speedReference = Mathf.Max(normalVerticalSpeed, 0.01f);
        float normalizedVertical = Mathf.Clamp(verticalSpeed / speedReference, -1f, 1f);
        float targetZ = baseRotationZ + (normalizedVertical * maxTiltAngle);

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetZ);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, tiltSmoothSpeed * Time.deltaTime);
    }

    private bool UpdateVerticalLimitsFromBorders()
    {
        float halfHeight = playerCollider != null ? playerCollider.bounds.extents.y : 0f;
        if (!BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                BoundaryReferenceDomain.Player,
                halfHeight,
                out Vector2 verticalRange))
        {
            hasVerticalLimits = false;
            return false;
        }

        minY = verticalRange.x;
        maxY = verticalRange.y;
        hasVerticalLimits = true;
        return true;
    }

    private void ClampPlayerInsideLimits()
    {
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
        transform.position = new Vector3(transform.position.x, clampedY, 0f);
    }

    private void RandomizeInitialYInsideLimits()
    {
        if (!randomizeInitialYWithinPlayerBoundaries || !hasVerticalLimits)
        {
            return;
        }

        float randomY = UnityEngine.Random.Range(minY, maxY);
        transform.position = new Vector3(transform.position.x, randomY, 0f);
    }

    private bool IsGameplayActive()
    {
        return session != null && session.IsPlaying;
    }

    private void ResolveSceneReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (progression == null && RunProgressionDirector.HasInstance)
        {
            progression = RunProgressionDirector.Instance;
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null
            || gameplayCamera == null
            || playerCollider == null
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out _, out _))
        {
            Debug.LogWarning(
                $"[PlayerMovement] Faltan referencias. Configura Session, GameplayCamera, PlayerCollider y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Player)}.",
                this);
        }
    }
}
