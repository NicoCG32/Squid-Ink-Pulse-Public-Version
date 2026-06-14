using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DestroyOffscreen : MonoBehaviour
{
    private const float HorizontalOffsetBehindCamera = 3f;
    private const float ColliderWidthWorldUnits = 1f;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    private BoxCollider2D cleanupCollider;
    private Rigidbody2D cleanupBody;
    private Collider2D cameraTopBorder;
    private Collider2D cameraBottomBorder;
    private bool missingBoundaryWarningLogged;
    private bool invalidBoundaryWarningLogged;

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

    private void OnTriggerExit2D(Collider2D other)
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

        if (!TryGetCameraBoundaryRange(out float centerY, out float height))
        {
            return;
        }

        FitColliderToCameraBoundaries(height);
        PositionBehindCameraLeftEdge();
        AlignVerticallyToCameraBoundaries(centerY);
    }

    private bool TryGetCameraBoundaryRange(out float centerY, out float height)
    {
        centerY = 0f;
        height = 0f;

        ResolveCameraBoundaries();
        if (cameraTopBorder == null || cameraBottomBorder == null)
        {
            WarnMissingCameraBoundaries();
            return false;
        }

        float topY = cameraTopBorder.bounds.min.y;
        float bottomY = cameraBottomBorder.bounds.max.y;
        height = topY - bottomY;

        if (height <= 0f)
        {
            WarnInvalidCameraBoundaries(topY, bottomY);
            return false;
        }

        centerY = (topY + bottomY) * 0.5f;
        return true;
    }

    private void FitColliderToCameraBoundaries(float worldHeight)
    {
        if (cleanupCollider == null)
        {
            return;
        }

        float scaleX = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));

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
        transform.position = nextPosition;
    }

    private void AlignVerticallyToCameraBoundaries(float centerY)
    {
        Vector3 nextPosition = transform.position;
        nextPosition.y = centerY;
        transform.position = nextPosition;
    }

    private void ResolveCameraBoundaries()
    {
        if (cameraTopBorder != null && cameraBottomBorder != null)
        {
            return;
        }

        BoundaryReferenceResolver.TryResolve(
            BoundaryReferenceDomain.Camera,
            out cameraTopBorder,
            out cameraBottomBorder);
    }

    private void WarnMissingCameraBoundaries()
    {
        if (missingBoundaryWarningLogged)
        {
            return;
        }

        missingBoundaryWarningLogged = true;
        Debug.LogWarning(
            $"[DestroyOffscreen] Faltan boundaries de camara. Configura la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)}.",
            this);
    }

    private void WarnInvalidCameraBoundaries(float topY, float bottomY)
    {
        if (invalidBoundaryWarningLogged)
        {
            return;
        }

        invalidBoundaryWarningLogged = true;
        Debug.LogWarning(
            $"[DestroyOffscreen] CameraBoundaries invalidos: TopBoundary ({topY}) debe estar sobre BottomBoundary ({bottomY}).",
            this);
    }

    private void DestroyIfOwnedByCleanableObject(Collider2D other)
    {
        GameObject cleanableObject = ResolveCleanableObject(other);
        if (cleanableObject != null && IsFullyBehindCleanupPlane(cleanableObject))
        {
            Destroy(cleanableObject);
        }
    }

    private bool IsFullyBehindCleanupPlane(GameObject cleanableObject)
    {
        float cleanupPlaneX = cleanupCollider != null
            ? cleanupCollider.bounds.center.x
            : transform.position.x;

        if (TryCalculateCleanableBounds(cleanableObject, out Bounds bounds))
        {
            return bounds.max.x <= cleanupPlaneX;
        }

        return cleanableObject.transform.position.x <= cleanupPlaneX;
    }

    private bool TryCalculateCleanableBounds(GameObject cleanableObject, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        Collider2D[] colliders = cleanableObject.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D candidate = colliders[i];
            if (candidate == null || !candidate.enabled)
            {
                continue;
            }

            EncapsulateBounds(candidate.bounds, ref bounds, ref hasBounds);
        }

        Renderer[] renderers = cleanableObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null || !candidate.enabled)
            {
                continue;
            }

            EncapsulateBounds(candidate.bounds, ref bounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void EncapsulateBounds(Bounds candidate, ref Bounds aggregate, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            aggregate = candidate;
            hasBounds = true;
            return;
        }

        aggregate.Encapsulate(candidate);
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
            || tag == GameplayTagCatalog.Portal
            || tag == GameplayTagCatalog.SSCarnage;
    }
}
