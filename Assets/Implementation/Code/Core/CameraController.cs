using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 

    public Vector3 offset = new Vector3(3f, 0f, -10f); 

    [Header("Map Limits")]
    [SerializeField] private string topBorderTag = "TopBorder";
    [SerializeField] private string bottomBorderTag = "BottomBorder";
    [Tooltip("Positivo: permite subir un poco mas arriba del TopBorder. Negativo: restringe antes de llegar.")]
    public float topBorderOffset = 0f;

    [Header("Dynamics")]
    public float smoothTime = 0.25f;

  
    private Vector3 velocity = Vector3.zero;
    private Collider2D topBorder;
    private Collider2D bottomBorder;
    private Camera currentCamera;
    private bool loggedInvalidBounds;

    private void Awake()
    {
        currentCamera = GetComponent<Camera>();
        if (currentCamera == null)
        {
            currentCamera = Camera.main;
        }

        TryResolveTarget();

        ResolveBorders();
    }

    private void TryResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    private void ResolveBorders()
    {
        GameObject topObj = GameObject.FindGameObjectWithTag(topBorderTag);
        GameObject bottomObj = GameObject.FindGameObjectWithTag(bottomBorderTag);

        if (topObj != null)
        {
            topBorder = topObj.GetComponent<Collider2D>();
        }

        if (bottomObj != null)
        {
            bottomBorder = bottomObj.GetComponent<Collider2D>();
        }

        if (topBorder == null || bottomBorder == null)
        {
            Debug.LogWarning("[CameraController] No se encontraron TopBorder y/o BottomBorder.");
        }
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
            if (!loggedInvalidBounds)
            {
                Debug.LogWarning("[CameraController] Los limites verticales son menores que el alto visible de la camara. Se desactiva clamp vertical.");
                loggedInvalidBounds = true;
            }
            return targetY;
        }

        loggedInvalidBounds = false;

        return Mathf.Clamp(targetY, minCameraY, maxCameraY);
    }

    void LateUpdate()
    {
        if (currentCamera == null)
        {
            currentCamera = GetComponent<Camera>();
            if (currentCamera == null)
            {
                currentCamera = Camera.main;
            }
        }

        if (topBorder == null || bottomBorder == null)
        {
            ResolveBorders();
        }

        if (target == null)
        {
            TryResolveTarget();
        }

        if (target == null) return;


        Vector3 targetPosition = target.position + offset;
        targetPosition.y = ClampCameraY(targetPosition.y);

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
}