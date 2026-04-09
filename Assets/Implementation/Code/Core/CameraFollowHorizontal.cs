using UnityEngine;

public class CameraFollowHorizontal : MonoBehaviour
{
    [Header("Target Settings")]
    public GameObject target;
    public Vector3 offset = new Vector3(3f, 0f, -10f);

    [Header("Dynamics")]
    public float smoothTime = 0.25f;
    public float topBorderOffset = 2f;

    private Vector3 velocity = Vector3.zero;
    private float fixedY;
    private Collider2D topBorder;
    private Collider2D bottomBorder;

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                target = playerObject;
        }

        ResolveBorders();
        
        // Calcular fixedY dentro de los límites permitidos
        float minCameraY = bottomBorder != null ? bottomBorder.bounds.max.y : transform.position.y;
        float maxCameraY = topBorder != null ? topBorder.bounds.min.y + topBorderOffset : transform.position.y;
        fixedY = Mathf.Clamp(transform.position.y, minCameraY, maxCameraY);
    }

    private void ResolveBorders()
    {
        // Buscar por nombre primero (más confiable)
        GameObject topObj = GameObject.Find("TopBoundary");
        GameObject bottomObj = GameObject.Find("BottomBoundary");
        
        if (topObj != null)
            topBorder = topObj.GetComponent<Collider2D>();
        if (bottomObj != null)
            bottomBorder = bottomObj.GetComponent<Collider2D>();
        
        // Si no encuentra por nombre, buscar por tag
        if (topBorder == null || bottomBorder == null)
        {
            Collider2D[] borderColliders = FindObjectsOfType<Collider2D>();
            foreach (Collider2D col in borderColliders)
            {
                if (col.CompareTag("Border"))
                {
                    float centerY = col.bounds.center.y;
                    if (topBorder == null || centerY > topBorder.bounds.center.y)
                        topBorder = col;
                    if (bottomBorder == null || centerY < bottomBorder.bounds.center.y)
                        bottomBorder = col;
                }
            }
        }
        
        if (topBorder == null || bottomBorder == null)
            Debug.LogWarning("[CameraFollowHorizontal] No se encontraron TopBoundary y/o BottomBoundary");
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Transform targetTransform = target.transform;

        // Calcular límites de cámara
        float minCameraY = bottomBorder != null ? bottomBorder.bounds.max.y : float.MinValue;
        float maxCameraY = topBorder != null ? topBorder.bounds.min.y + topBorderOffset : float.MaxValue;

        Vector3 targetPosition = new Vector3(
            targetTransform.position.x + offset.x,
            Mathf.Clamp(fixedY + offset.y, minCameraY, maxCameraY),
            offset.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
}
