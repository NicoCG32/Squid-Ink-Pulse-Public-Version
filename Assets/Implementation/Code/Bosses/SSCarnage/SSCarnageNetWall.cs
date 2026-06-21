using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SSCarnageNetWall : MonoBehaviour, IOffscreenCleanupEligibility
{
    private const string AuthoringBoundaryRootName = "AuthoringPlayerBoundaries";

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D wallCollider;

    [Header("Visual Layers")]
    [SerializeField] private Transform[] intactVisualLayers;
    [SerializeField] private Transform brokenVisualLayer;

    private bool isBroken;
    private bool hasFailed;
    private bool hasCapturedAuthoringMetrics;
    private Vector3 authoredRootLocalScale = Vector3.one;
    private Vector2 authoredColliderOffset;
    private Vector2 authoredColliderSize = Vector2.one;
    private Collider2D topBorder;
    private Collider2D bottomBorder;
    private float authoredReferenceBottomY;
    private float authoredReferenceHeight = 1f;

    public event Action Resolved;
    public event Action Failed;
    public bool CanBeCleanedUpOffscreen => isBroken || hasFailed;

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

        ResolveProgressionReference();
        ResolveVisualReferences();
        CaptureAuthoringMetrics();
        ApplyVisualState();
    }

    private void Start()
    {
        ResolveBoundaryReferences();
        ResolveVisualReferences();
        ApplyVisualState();
        FitToVerticalBoundaries();
        WarnIfMissingReferences();
    }

    public void Initialize(
        GameSessionController sessionReference,
        RunProgressionDirector progressionReference,
        Camera cameraReference)
    {
        session = sessionReference;
        progression = progressionReference;
        gameplayCamera = cameraReference;
        ResolveBoundaryReferences();
        ResolveVisualReferences();
        CaptureAuthoringMetrics();
        ApplyVisualState();
        FitToVerticalBoundaries();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken || hasFailed || session == null || !session.IsPlaying)
        {
            return;
        }

        if (!other.CompareTag(GameplayTagCatalog.Player))
        {
            return;
        }

        InkPulseController pulse = other.GetComponentInParent<InkPulseController>();
        if (pulse != null && pulse.IsPulseActive)
        {
            BreakWall();
            return;
        }

        PlayerGadgetInventory gadgetInventory = other.GetComponentInParent<PlayerGadgetInventory>();
        if (gadgetInventory != null && gadgetInventory.TryConsumeShellShield())
        {
            BreakWall();
            return;
        }

        progression?.NotifyBossFailed();
        hasFailed = true;
        Failed?.Invoke();
        session.RequestGameOver();
    }

    private void BreakWall()
    {
        isBroken = true;

        ApplyVisualState();
        progression?.NotifyBossResolved();
        Resolved?.Invoke();
    }

    private void FitToVerticalBoundaries()
    {
        if (!TryCalculateVerticalBounds(out Vector2 verticalBounds))
        {
            return;
        }

        float bottomY = verticalBounds.x;
        float topY = verticalBounds.y;
        float height = Mathf.Max(1f, topY - bottomY);

        Vector3 targetScale = CalculateTargetScale(height);
        transform.localScale = targetScale;

        float authoredBottomOffset = transform.TransformVector(Vector3.up * authoredReferenceBottomY).y;
        transform.position = new Vector3(transform.position.x, bottomY - authoredBottomOffset, transform.position.z);

        FitCollisionVolume(height);
    }

    private bool TryCalculateVerticalBounds(out Vector2 verticalBounds)
    {
        verticalBounds = default;
        if (topBorder != null && bottomBorder != null)
        {
            verticalBounds = new Vector2(bottomBorder.bounds.max.y, topBorder.bounds.min.y);
            return verticalBounds.x <= verticalBounds.y;
        }

        return BoundaryReferenceResolver.TryResolveInnerVerticalRange(
            BoundaryReferenceDomain.Player,
            0f,
            out verticalBounds);
    }

    private void CaptureAuthoringMetrics()
    {
        if (hasCapturedAuthoringMetrics)
        {
            return;
        }

        authoredRootLocalScale = transform.localScale;

        if (wallCollider is BoxCollider2D boxCollider)
        {
            authoredColliderOffset = boxCollider.offset;
            authoredColliderSize = boxCollider.size;
        }

        if (!TryCalculateAuthoredBoundaryRange(out Vector2 authoredRange)
            && !TryCalculateAuthoredVisualRange(out authoredRange))
        {
            float fallbackHeight = Mathf.Abs(authoredRootLocalScale.y) * Mathf.Max(0.01f, authoredColliderSize.y);
            authoredRange = new Vector2(0f, fallbackHeight);
        }

        authoredReferenceBottomY = authoredRange.x;
        authoredReferenceHeight = Mathf.Max(0.01f, authoredRange.y - authoredRange.x);
        hasCapturedAuthoringMetrics = true;
    }

    private Vector3 CalculateTargetScale(float targetHeight)
    {
        CaptureAuthoringMetrics();
        float authoredWorldHeight = authoredReferenceHeight * Mathf.Max(0.01f, Mathf.Abs(authoredRootLocalScale.y));
        float scaleFactor = targetHeight / authoredWorldHeight;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float parentScaleX = Mathf.Max(0.01f, Mathf.Abs(parentScale.x));
        float parentScaleY = Mathf.Max(0.01f, Mathf.Abs(parentScale.y));

        return new Vector3(
            authoredRootLocalScale.x * scaleFactor / parentScaleX,
            authoredRootLocalScale.y * scaleFactor / parentScaleY,
            authoredRootLocalScale.z);
    }

    private void FitCollisionVolume(float height)
    {
        if (wallCollider is not BoxCollider2D boxCollider)
        {
            return;
        }

        float scaleX = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
        float authoredWorldWidth = Mathf.Max(0.01f, Mathf.Abs(authoredColliderSize.x * authoredRootLocalScale.x));
        boxCollider.offset = new Vector2(authoredColliderOffset.x, authoredReferenceBottomY + (height * 0.5f / scaleY));
        boxCollider.size = new Vector2(authoredWorldWidth / scaleX, height / scaleY);
    }

    private bool TryCalculateAuthoredBoundaryRange(out Vector2 authoredRange)
    {
        authoredRange = default;
        Transform referenceRoot = transform.Find(AuthoringBoundaryRootName);
        if (referenceRoot == null)
        {
            return false;
        }

        Transform top = referenceRoot.Find(BoundaryReferenceResolver.TopBoundaryName);
        Transform bottom = referenceRoot.Find(BoundaryReferenceResolver.BottomBoundaryName);
        if (top == null || bottom == null)
        {
            return false;
        }

        Collider2D topCollider = top.GetComponent<Collider2D>();
        Collider2D bottomCollider = bottom.GetComponent<Collider2D>();

        float bottomY = TryCalculateColliderLocalEdge(bottomCollider, upperEdge: true, out float calculatedBottomY)
            ? calculatedBottomY
            : transform.InverseTransformPoint(bottom.position).y;
        float topY = TryCalculateColliderLocalEdge(topCollider, upperEdge: false, out float calculatedTopY)
            ? calculatedTopY
            : transform.InverseTransformPoint(top.position).y;

        if (bottomY > topY)
        {
            return false;
        }

        authoredRange = new Vector2(bottomY, topY);
        return true;
    }

    private bool TryCalculateColliderLocalEdge(Collider2D collider, bool upperEdge, out float localY)
    {
        localY = default;
        if (collider == null)
        {
            return false;
        }

        if (collider is BoxCollider2D boxCollider)
        {
            Vector2 localPoint = boxCollider.offset
                + Vector2.up * ((upperEdge ? 1f : -1f) * boxCollider.size.y * 0.5f);
            localY = transform.InverseTransformPoint(boxCollider.transform.TransformPoint(localPoint)).y;
            return true;
        }

        if (collider is CircleCollider2D circleCollider)
        {
            Vector2 localPoint = circleCollider.offset
                + Vector2.up * ((upperEdge ? 1f : -1f) * circleCollider.radius);
            localY = transform.InverseTransformPoint(circleCollider.transform.TransformPoint(localPoint)).y;
            return true;
        }

        if (collider is PolygonCollider2D polygonCollider)
        {
            bool hasPoint = false;
            float edgeY = 0f;
            for (int pathIndex = 0; pathIndex < polygonCollider.pathCount; pathIndex++)
            {
                Vector2[] points = polygonCollider.GetPath(pathIndex);
                foreach (Vector2 point in points)
                {
                    float candidateY = transform.InverseTransformPoint(polygonCollider.transform.TransformPoint(point)).y;
                    edgeY = !hasPoint
                        ? candidateY
                        : upperEdge ? Mathf.Max(edgeY, candidateY) : Mathf.Min(edgeY, candidateY);
                    hasPoint = true;
                }
            }

            if (hasPoint)
            {
                localY = edgeY;
                return true;
            }
        }

        return false;
    }

    private bool TryCalculateAuthoredVisualRange(out Vector2 visualRange)
    {
        bool hasBounds = false;
        float minY = 0f;
        float maxY = 0f;

        if (intactVisualLayers != null)
        {
            foreach (Transform layer in intactVisualLayers)
            {
                IncludeVisualLayerHeight(layer, ref hasBounds, ref minY, ref maxY);
            }
        }

        IncludeVisualLayerHeight(brokenVisualLayer, ref hasBounds, ref minY, ref maxY);
        visualRange = hasBounds ? new Vector2(minY, maxY) : default;
        return hasBounds && visualRange.y - visualRange.x > Mathf.Epsilon;
    }

    private void IncludeVisualLayerHeight(Transform visualLayer, ref bool hasBounds, ref float minY, ref float maxY)
    {
        if (visualLayer == null || !visualLayer.TryGetComponent(out SpriteRenderer renderer) || renderer.sprite == null)
        {
            return;
        }

        Bounds spriteBounds = renderer.sprite.bounds;
        float scaledMinY = visualLayer.localPosition.y + Mathf.Min(
            spriteBounds.min.y * visualLayer.localScale.y,
            spriteBounds.max.y * visualLayer.localScale.y);
        float scaledMaxY = visualLayer.localPosition.y + Mathf.Max(
            spriteBounds.min.y * visualLayer.localScale.y,
            spriteBounds.max.y * visualLayer.localScale.y);

        if (!hasBounds)
        {
            minY = scaledMinY;
            maxY = scaledMaxY;
            hasBounds = true;
            return;
        }

        minY = Mathf.Min(minY, scaledMinY);
        maxY = Mathf.Max(maxY, scaledMaxY);
    }

    private void ApplyVisualState()
    {
        bool showIntactLayers = !isBroken;

        if (intactVisualLayers != null)
        {
            foreach (Transform layer in intactVisualLayers)
            {
                SetVisualLayerActive(layer, showIntactLayers);
            }
        }

        SetVisualLayerActive(brokenVisualLayer, isBroken);
    }

    private void SetVisualLayerActive(Transform visualLayer, bool active)
    {
        if (visualLayer != null)
        {
            visualLayer.gameObject.SetActive(active);
        }
    }

    private void ResolveVisualReferences()
    {
        if (intactVisualLayers == null || intactVisualLayers.Length == 0)
        {
            List<Transform> resolvedLayers = new();
            AddLayerIfFound(resolvedLayers, "BackLayer");
            AddLayerIfFound(resolvedLayers, "BackLayer_BehindSquid");
            AddLayerIfFound(resolvedLayers, "FrontLayer");
            AddLayerIfFound(resolvedLayers, "FrontLayer_OverSquid");
            intactVisualLayers = resolvedLayers.ToArray();
        }

        if (brokenVisualLayer == null)
        {
            brokenVisualLayer = transform.Find("BrokenNet");
        }
    }

    private void AddLayerIfFound(List<Transform> layers, string childName)
    {
        Transform layer = transform.Find(childName);
        if (layer != null && !layers.Contains(layer))
        {
            layers.Add(layer);
        }
    }

    private void ResolveProgressionReference()
    {
        if (progression == null && RunProgressionDirector.HasInstance)
        {
            progression = RunProgressionDirector.Instance;
        }
    }

    private void ResolveBoundaryReferences()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D resolvedPlayerTop, out Collider2D resolvedPlayerBottom))
        {
            topBorder = resolvedPlayerTop;
            bottomBorder = resolvedPlayerBottom;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || gameplayCamera == null || topBorder == null || bottomBorder == null || wallCollider == null)
        {
            Debug.LogWarning(
                $"[SSCarnageNetWall] Faltan referencias. El SS Carnage debe entregar Session y Camera; la escena debe tener {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Player)}; el prefab debe tener Collider2D.",
                this);
        }
    }
}
