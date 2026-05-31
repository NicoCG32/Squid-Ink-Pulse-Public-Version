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
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;

    [Header("Target Settings")]
    [SerializeField] private Vector3 offset = new Vector3(3f, 0f, -10f);

    [Header("Map Limits")]
    [Tooltip("Solo valores negativos restringen antes de llegar al TopBorder. La camara no excede los boundaries.")]
    [SerializeField] private float topBorderOffset = 0f;

    [Header("Dynamics")]
    [SerializeField] private float smoothTime = 0.25f;

    private Vector3 velocity = Vector3.zero;
    private float orthographicVelocity;
    private float normalOrthographicSize;
    private float wideViewRemainingSeconds;
    private float wideViewTransitionSmoothTime = 1f;

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

    private void Update()
    {
        if (wideViewRemainingSeconds > 0f)
        {
            wideViewRemainingSeconds -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
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
    }

    public void RequestFullVerticalView(float holdSeconds, float transitionSmoothTime, float extraTopSpace)
    {
        _ = extraTopSpace;
        ResolveSceneReferences();

        if (currentCamera == null || topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning("[CameraController] No se puede activar vista amplia sin CurrentCamera, TopBorder y BottomBorder.", this);
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
        float mapMaxY = topBorder.bounds.min.y + Mathf.Min(0f, topBorderOffset);
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
        if (currentCamera == null || target == null || topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning("[CameraController] Faltan referencias. Asigna CurrentCamera, Target, TopBorder y BottomBorder en el Inspector.", this);
        }
    }

    private void ResolveSceneReferences()
    {
        if (currentCamera == null)
        {
            currentCamera = GetComponent<Camera>();
        }

        if ((topBorder == null || bottomBorder == null)
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }

        if (normalOrthographicSize <= 0f && currentCamera != null)
        {
            normalOrthographicSize = currentCamera.orthographicSize;
        }
    }
}
