using System;
using UnityEngine;

public enum CameraEventMode
{
    Follow,
    WideEvent,
    ReturningToFollow
}

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera currentCamera;
    [SerializeField] private Transform target;
    [SerializeField] private InkPulseController inkPulse;

    [Header("Target Settings")]
    [SerializeField] private Vector3 offset = new Vector3(3f, 0f, -10f);

    [Header("Dynamics")]
    [SerializeField] private float smoothTime = 0.25f;

    [Header("Ink-Pulse Feedback")]
    [SerializeField] private bool enableInkPulseScreenPulse = true;
    [SerializeField, Min(0f)] private float inkPulseFeedbackDuration = 0.18f;
    [SerializeField, Min(0f)] private float inkPulseShakeAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float inkPulseZoomAmplitude = 0.18f;
    [SerializeField, Min(1f)] private float inkPulseShakeFrequency = 24f;

    private Vector3 velocity = Vector3.zero;
    private float orthographicVelocity;
    private float normalOrthographicSize;
    private float wideViewRemainingSeconds;
    private float wideViewTransitionSmoothTime = 1f;
    private InkPulseController subscribedInkPulse;
    private float inkPulseFeedbackRemainingSeconds;
    private float inkPulseFeedbackElapsedSeconds;
    private Vector3 activeFeedbackOffset;
    private float activeFeedbackOrthographicOffset;
    private Collider2D topBorder;
    private Collider2D bottomBorder;

    public CameraEventMode CurrentMode { get; private set; } = CameraEventMode.Follow;
    public event Action<CameraEventMode, CameraEventMode> ModeChanged;

    private void Awake()
    {
        ResolveSceneReferences();

        if (currentCamera != null)
        {
            normalOrthographicSize = currentCamera.orthographicSize;
        }

        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        ResolveSceneReferences();
        UpdateInkPulseSubscription();
    }

    private void OnDisable()
    {
        ClearInkPulseSubscription();
        RemoveActiveFeedback();
    }

    private void Update()
    {
        if (wideViewRemainingSeconds > 0f)
        {
            wideViewRemainingSeconds -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        RemoveActiveFeedback();
        ResolveSceneReferences();

        if (target == null)
        {
            return;
        }

        CameraEventMode nextMode = ResolveMode();
        ApplyMode(nextMode);

        bool useWideView = CurrentMode == CameraEventMode.WideEvent;
        Vector3 targetPosition = useWideView
            ? CalculateWideViewPosition()
            : CalculateNormalViewPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            GetActiveSmoothTime(CurrentMode));

        UpdateOrthographicSize(CurrentMode);
        ApplyInkPulseScreenFeedback();
    }

    public void RequestFullVerticalView(float holdSeconds, float transitionSmoothTime, float extraTopSpace)
    {
        _ = extraTopSpace;
        ResolveSceneReferences();

        if (currentCamera == null || topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning(
                $"[CameraController] No se puede activar vista amplia sin CurrentCamera y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)}.",
                this);
            return;
        }

        wideViewRemainingSeconds = Mathf.Max(wideViewRemainingSeconds, holdSeconds);
        wideViewTransitionSmoothTime = Mathf.Max(0.01f, transitionSmoothTime);
        ApplyMode(CameraEventMode.WideEvent);
    }

    private Vector3 CalculateNormalViewPosition()
    {
        Vector3 targetPosition = target.position + offset;
        targetPosition.y = ClampCameraY(targetPosition.y);
        return targetPosition;
    }

    private Vector3 CalculateWideViewPosition()
    {
        Vector3 targetPosition = target.position + offset;
        targetPosition.y = CalculateWideViewCenterY();
        return targetPosition;
    }

    private float GetActiveSmoothTime(CameraEventMode mode)
    {
        return mode == CameraEventMode.WideEvent || mode == CameraEventMode.ReturningToFollow
            ? wideViewTransitionSmoothTime
            : smoothTime;
    }

    private void UpdateOrthographicSize(CameraEventMode mode)
    {
        if (currentCamera == null || !currentCamera.orthographic)
        {
            return;
        }

        bool useWideView = mode == CameraEventMode.WideEvent;
        float targetSize = useWideView ? CalculateWideViewOrthographicSize() : normalOrthographicSize;
        currentCamera.orthographicSize = Mathf.SmoothDamp(
            currentCamera.orthographicSize,
            targetSize,
            ref orthographicVelocity,
            GetActiveSmoothTime(mode));
    }

    private void HandleInkPulseStarted()
    {
        if (!enableInkPulseScreenPulse || inkPulseFeedbackDuration <= 0f)
        {
            return;
        }

        inkPulseFeedbackRemainingSeconds = inkPulseFeedbackDuration;
        inkPulseFeedbackElapsedSeconds = 0f;
    }

    private void ApplyInkPulseScreenFeedback()
    {
        if (!enableInkPulseScreenPulse || inkPulseFeedbackRemainingSeconds <= 0f)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        inkPulseFeedbackElapsedSeconds += deltaTime;
        inkPulseFeedbackRemainingSeconds = Mathf.Max(0f, inkPulseFeedbackRemainingSeconds - deltaTime);

        float progress = Mathf.Clamp01(inkPulseFeedbackElapsedSeconds / Mathf.Max(0.01f, inkPulseFeedbackDuration));
        float decay = 1f - progress;
        float angularTime = inkPulseFeedbackElapsedSeconds * inkPulseShakeFrequency * Mathf.PI * 2f;

        activeFeedbackOffset = new Vector3(
            Mathf.Sin(angularTime) * inkPulseShakeAmplitude * decay,
            Mathf.Cos(angularTime * 0.73f) * inkPulseShakeAmplitude * 0.6f * decay,
            0f);

        transform.position += activeFeedbackOffset;

        if (currentCamera != null && currentCamera.orthographic)
        {
            activeFeedbackOrthographicOffset = Mathf.Sin(progress * Mathf.PI) * inkPulseZoomAmplitude * decay;
            currentCamera.orthographicSize += activeFeedbackOrthographicOffset;
        }
    }

    private void RemoveActiveFeedback()
    {
        if (activeFeedbackOffset != Vector3.zero)
        {
            transform.position -= activeFeedbackOffset;
            activeFeedbackOffset = Vector3.zero;
        }

        if (activeFeedbackOrthographicOffset != 0f && currentCamera != null && currentCamera.orthographic)
        {
            currentCamera.orthographicSize -= activeFeedbackOrthographicOffset;
            activeFeedbackOrthographicOffset = 0f;
        }
    }

    private float CalculateWideViewCenterY()
    {
        if (topBorder == null || bottomBorder == null)
        {
            return target.position.y + offset.y;
        }

        float bottomY = bottomBorder.bounds.max.y;
        float topY = topBorder.bounds.min.y;
        return (bottomY + topY) * 0.5f;
    }

    private float CalculateWideViewOrthographicSize()
    {
        if (topBorder == null || bottomBorder == null)
        {
            return normalOrthographicSize;
        }

        float bottomY = bottomBorder.bounds.max.y;
        float topY = topBorder.bounds.min.y;
        float fullHeight = Mathf.Max(0.01f, topY - bottomY);
        return fullHeight * 0.5f;
    }

    private float ClampCameraY(float targetY)
    {
        if (topBorder == null || bottomBorder == null || currentCamera == null || !currentCamera.orthographic)
        {
            return targetY;
        }

        float mapMinY = bottomBorder.bounds.max.y;
        float mapMaxY = topBorder.bounds.min.y;
        float halfViewportHeight = currentCamera.orthographicSize;

        float minCameraY = mapMinY + halfViewportHeight;
        float maxCameraY = mapMaxY - halfViewportHeight;

        if (minCameraY > maxCameraY)
        {
            return (mapMinY + mapMaxY) * 0.5f;
        }

        return Mathf.Clamp(targetY, minCameraY, maxCameraY);
    }

    private CameraEventMode ResolveMode()
    {
        if (wideViewRemainingSeconds > 0f)
        {
            return CameraEventMode.WideEvent;
        }

        if (IsReturningFromWideView())
        {
            return CameraEventMode.ReturningToFollow;
        }

        return CameraEventMode.Follow;
    }

    private bool IsReturningFromWideView()
    {
        return currentCamera != null
            && Mathf.Abs(currentCamera.orthographicSize - normalOrthographicSize) > 0.01f;
    }

    private void ApplyMode(CameraEventMode nextMode)
    {
        CameraEventMode previousMode = CurrentMode;
        if (previousMode == nextMode)
        {
            return;
        }

        CurrentMode = nextMode;
        ModeChanged?.Invoke(previousMode, nextMode);
    }

    private void WarnIfMissingReferences()
    {
        if (currentCamera == null
            || target == null
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out _, out _))
        {
            Debug.LogWarning(
                $"[CameraController] Faltan referencias. Configura CurrentCamera, Target y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)}.",
                this);
        }
    }

    private void ResolveSceneReferences()
    {
        if (currentCamera == null)
        {
            currentCamera = GetComponent<Camera>();
        }

        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }

        if (normalOrthographicSize <= 0f && currentCamera != null)
        {
            normalOrthographicSize = currentCamera.orthographicSize;
        }

        if (inkPulse == null && target != null)
        {
            inkPulse = target.GetComponent<InkPulseController>();
        }

        UpdateInkPulseSubscription();
    }

    private void UpdateInkPulseSubscription()
    {
        if (subscribedInkPulse == inkPulse)
        {
            return;
        }

        ClearInkPulseSubscription();

        if (inkPulse != null)
        {
            subscribedInkPulse = inkPulse;
            subscribedInkPulse.PulseStarted += HandleInkPulseStarted;
        }
    }

    private void ClearInkPulseSubscription()
    {
        if (subscribedInkPulse == null)
        {
            return;
        }

        subscribedInkPulse.PulseStarted -= HandleInkPulseStarted;
        subscribedInkPulse = null;
    }
}
