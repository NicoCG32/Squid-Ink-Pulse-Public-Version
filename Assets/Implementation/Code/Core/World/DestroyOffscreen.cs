using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DestroyOffscreen : MonoBehaviour
{
    private const float HorizontalOffsetBehindCamera = 3f;
    private const float ColliderWidthWorldUnits = 1f;
    private const float VerticalPaddingWorldUnits = 8f;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    private BoxCollider2D cleanupCollider;
    private Rigidbody2D cleanupBody;

    private void Reset()
    {
        ResolveComponents();
        ConfigurePhysics();
    }

    private void OnValidate()
    {
        ResolveComponents();
        ConfigurePhysics();
    }

    private void Awake()
    {
        ResolveComponents();
        ResolveCamera();
        ConfigurePhysics();
        AlignToCamera();
    }

    private void LateUpdate()
    {
        ResolveCamera();
        AlignToCamera();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DestroyIfOwnedByCleanableObject(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DestroyIfOwnedByCleanableObject(other);
    }

    private void ResolveComponents()
    {
        if (cleanupCollider == null)
        {
            cleanupCollider = GetComponent<BoxCollider2D>();
        }

        if (cleanupBody == null)
        {
            cleanupBody = GetComponent<Rigidbody2D>();
        }
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void ConfigurePhysics()
    {
        if (cleanupCollider != null)
        {
            cleanupCollider.isTrigger = true;
        }

        if (cleanupBody != null)
        {
            cleanupBody.bodyType = RigidbodyType2D.Kinematic;
            cleanupBody.gravityScale = 0f;
            cleanupBody.simulated = true;
        }
    }

    private void AlignToCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        FitColliderToCameraHeight();
        PositionBehindCameraLeftEdge();
    }

    private void FitColliderToCameraHeight()
    {
        if (cleanupCollider == null || !targetCamera.orthographic)
        {
            return;
        }

        float scaleX = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
        float worldHeight = targetCamera.orthographicSize * 2f + VerticalPaddingWorldUnits;

        cleanupCollider.offset = Vector2.zero;
        cleanupCollider.size = new Vector2(
            ColliderWidthWorldUnits / scaleX,
            worldHeight / scaleY);
    }

    private void PositionBehindCameraLeftEdge()
    {
        float cameraDepthToCollector = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
        Vector3 cameraLeftEdge = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, cameraDepthToCollector));
        Vector3 nextPosition = transform.position;

        nextPosition.x = cameraLeftEdge.x - HorizontalOffsetBehindCamera;
        nextPosition.y = targetCamera.transform.position.y;
        transform.position = nextPosition;
    }

    private void DestroyIfOwnedByCleanableObject(Collider2D other)
    {
        GameObject cleanableObject = ResolveCleanableObject(other);
        if (cleanableObject != null)
        {
            Destroy(cleanableObject);
        }
    }

    private GameObject ResolveCleanableObject(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        Transform current = other.transform;
        while (current != null)
        {
            GameObject candidate = current.gameObject;
            if (IsCleanableTag(candidate.tag))
            {
                return candidate;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsCleanableTag(string tag)
    {
        return EnemyTagCatalog.IsEnemyTag(tag)
            || tag == GameplayTagCatalog.Shrimp
            || tag == GameplayTagCatalog.Collectible
            || tag == GameplayTagCatalog.Portal;
    }
}
