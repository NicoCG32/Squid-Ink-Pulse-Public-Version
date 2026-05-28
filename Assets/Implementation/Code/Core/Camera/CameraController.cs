using UnityEngine;

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
    [Tooltip("Positivo: permite subir un poco mas arriba del TopBorder. Negativo: restringe antes de llegar.")]
    [SerializeField] private float topBorderOffset = 0f;

    [Header("Dynamics")]
    [SerializeField] private float smoothTime = 0.25f;

    private Vector3 velocity = Vector3.zero;
    private float orthographicVelocity;
    private float normalOrthographicSize;
    private float wideViewRemainingSeconds;
    private float wideViewTransitionSmoothTime = 1f;
    private float wideViewExtraTopSpace;

    private void Awake()
    {
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
        if (target == null)
        {
            return;
        }

        bool useWideView = wideViewRemainingSeconds > 0f;
        Vector3 targetPosition = useWideView
            ? CalculateWideViewPosition()
            : CalculateNormalViewPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            GetActiveSmoothTime(useWideView));

        UpdateOrthographicSize(useWideView);
    }

    public void RequestFullVerticalView(float holdSeconds, float transitionSmoothTime, float extraTopSpace)
    {
        if (currentCamera == null || topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning("[CameraController] No se puede activar vista amplia sin CurrentCamera, TopBorder y BottomBorder.", this);
            return;
        }

        wideViewRemainingSeconds = Mathf.Max(wideViewRemainingSeconds, holdSeconds);
        wideViewTransitionSmoothTime = Mathf.Max(0.01f, transitionSmoothTime);
        wideViewExtraTopSpace = Mathf.Max(0f, extraTopSpace);
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

    private float GetActiveSmoothTime(bool useWideView)
    {
        bool returningFromWideView = currentCamera != null
            && Mathf.Abs(currentCamera.orthographicSize - normalOrthographicSize) > 0.01f;

        return useWideView || returningFromWideView
            ? wideViewTransitionSmoothTime
            : smoothTime;
    }

    private void UpdateOrthographicSize(bool useWideView)
    {
        if (currentCamera == null || !currentCamera.orthographic)
        {
            return;
        }

        float targetSize = useWideView ? CalculateWideViewOrthographicSize() : normalOrthographicSize;
        currentCamera.orthographicSize = Mathf.SmoothDamp(
            currentCamera.orthographicSize,
            targetSize,
            ref orthographicVelocity,
            GetActiveSmoothTime(useWideView));
    }

    private float CalculateWideViewCenterY()
    {
        if (topBorder == null || bottomBorder == null)
        {
            return target.position.y + offset.y;
        }

        float bottomY = bottomBorder.bounds.max.y;
        float topY = topBorder.bounds.min.y + wideViewExtraTopSpace;
        return (bottomY + topY) * 0.5f;
    }

    private float CalculateWideViewOrthographicSize()
    {
        if (topBorder == null || bottomBorder == null)
        {
            return normalOrthographicSize;
        }

        float bottomY = bottomBorder.bounds.max.y;
        float topY = topBorder.bounds.min.y + wideViewExtraTopSpace;
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
        float mapMaxY = topBorder.bounds.min.y + topBorderOffset;
        float halfViewportHeight = currentCamera.orthographicSize;

        float minCameraY = mapMinY + halfViewportHeight;
        float maxCameraY = mapMaxY - halfViewportHeight;

        if (minCameraY > maxCameraY)
        {
            return (mapMinY + mapMaxY) * 0.5f;
        }

        return Mathf.Clamp(targetY, minCameraY, maxCameraY);
    }

    private void WarnIfMissingReferences()
    {
        if (currentCamera == null || target == null || topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning("[CameraController] Faltan referencias. Asigna CurrentCamera, Target, TopBorder y BottomBorder en el Inspector.", this);
        }
    }
}
