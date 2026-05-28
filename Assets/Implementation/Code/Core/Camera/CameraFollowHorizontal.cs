using UnityEngine;

public class CameraFollowHorizontal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;

    [Header("Target Settings")]
    [SerializeField] private Vector3 offset = new Vector3(3f, 0f, -10f);

    [Header("Dynamics")]
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float topBorderOffset = 2f;

    private Vector3 velocity = Vector3.zero;
    private float fixedY;

    private void Start()
    {
        WarnIfMissingReferences();

        float minCameraY = bottomBorder != null ? bottomBorder.bounds.max.y : transform.position.y;
        float maxCameraY = topBorder != null ? topBorder.bounds.min.y + topBorderOffset : transform.position.y;
        fixedY = Mathf.Clamp(transform.position.y, minCameraY, maxCameraY);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float minCameraY = bottomBorder != null ? bottomBorder.bounds.max.y : float.MinValue;
        float maxCameraY = topBorder != null ? topBorder.bounds.min.y + topBorderOffset : float.MaxValue;

        Vector3 targetPosition = new Vector3(
            target.position.x + offset.x,
            Mathf.Clamp(fixedY + offset.y, minCameraY, maxCameraY),
            offset.z);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime);
    }

    private void WarnIfMissingReferences()
    {
        if (target == null || topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning("[CameraFollowHorizontal] Faltan referencias. Asigna Target, TopBorder y BottomBorder en el Inspector.", this);
        }
    }
}
