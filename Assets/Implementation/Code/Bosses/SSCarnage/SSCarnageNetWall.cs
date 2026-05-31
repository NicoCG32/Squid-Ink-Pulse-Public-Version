using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SSCarnageNetWall : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Collider2D wallCollider;

    [Header("Visual Layers")]
    [SerializeField] private Transform[] intactVisualLayers;
    [SerializeField] private Transform brokenVisualLayer;
    [SerializeField] private bool fitVisualsToBoundaryHeight = true;
    [Tooltip("Altura base en unidades de mundo. Usa 0 para calcularla desde las capas visuales del prefab. Con PPU 100, 1 unidad = 100 px.")]
    [SerializeField, Min(0f)] private float authoredBoundaryHeight;

    [Header("Collision Rules")]
    [SerializeField] private bool destroyWhenBroken;

    [Header("Camera Fit")]
    [SerializeField] private bool fitHeightToBoundaries = true;
    [SerializeField] private float wallWidth = 0.75f;

    private bool isBroken;
    private bool hasFailed;
    private bool hasCapturedAuthoringMetrics;
    private Vector3 authoredRootLocalScale = Vector3.one;
    private Vector2 authoredColliderOffset;
    private Vector2 authoredColliderSize = Vector2.one;

    public event Action Resolved;
    public event Action Failed;

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
        Camera cameraReference,
        Collider2D topBorderReference,
        Collider2D bottomBorderReference)
    {
        session = sessionReference;
        progression = progressionReference;
        gameplayCamera = cameraReference;
        topBorder = topBorderReference;
        bottomBorder = bottomBorderReference;
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

        if (!other.CompareTag(PlayerTag))
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

        if (wallCollider != null)
        {
            wallCollider.enabled = false;
        }

        ApplyVisualState();
        progression?.NotifyBossResolved();
        Resolved?.Invoke();

        if (destroyWhenBroken)
        {
            Destroy(gameObject);
        }
    }

    private void FitToVerticalBoundaries()
    {
        if (!fitHeightToBoundaries)
        {
            return;
        }

        Vector2 verticalBounds = CalculateVerticalBounds();
        float bottomY = verticalBounds.x;
        float topY = verticalBounds.y;
        float height = Mathf.Max(1f, topY - bottomY);

        transform.position = new Vector3(transform.position.x, bottomY, transform.position.z);
        FitVisualRootToHeight(height);
        FitCollisionVolume(height);
    }

    private Vector2 CalculateVerticalBounds()
    {
        if (topBorder != null && bottomBorder != null)
        {
            return new Vector2(bottomBorder.bounds.max.y, topBorder.bounds.min.y);
        }

        if (gameplayCamera == null)
        {
            return new Vector2(transform.position.y - 0.5f, transform.position.y + 0.5f);
        }

        float depthToWall = Mathf.Abs(gameplayCamera.transform.position.z - transform.position.z);
        float bottomY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depthToWall)).y;
        float topY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, depthToWall)).y;
        return new Vector2(bottomY, topY);
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

        if (authoredBoundaryHeight <= 0f && TryCalculateAuthoredVisualHeight(out float visualHeight))
        {
            authoredBoundaryHeight = visualHeight;
        }

        if (authoredBoundaryHeight <= 0f)
        {
            authoredBoundaryHeight = Mathf.Abs(authoredRootLocalScale.y) * Mathf.Max(0.01f, authoredColliderSize.y);
        }

        authoredBoundaryHeight = Mathf.Max(0.01f, authoredBoundaryHeight);
        hasCapturedAuthoringMetrics = true;
    }

    private void FitVisualRootToHeight(float targetHeight)
    {
        if (!fitVisualsToBoundaryHeight)
        {
            return;
        }

        CaptureAuthoringMetrics();
        float scaleFactor = targetHeight / authoredBoundaryHeight;
        transform.localScale = new Vector3(
            authoredRootLocalScale.x * scaleFactor,
            authoredRootLocalScale.y * scaleFactor,
            authoredRootLocalScale.z);
    }

    private void FitCollisionVolume(float height)
    {
        if (wallCollider is not BoxCollider2D boxCollider)
        {
            if (!fitVisualsToBoundaryHeight)
            {
                transform.localScale = new Vector3(wallWidth, height, 1f);
            }

            return;
        }

        float scaleX = Mathf.Max(0.01f, Mathf.Abs(transform.localScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(transform.localScale.y));
        boxCollider.offset = new Vector2(authoredColliderOffset.x, height * 0.5f / scaleY);
        boxCollider.size = new Vector2(Mathf.Max(0.01f, wallWidth) / scaleX, height / scaleY);
    }

    private bool TryCalculateAuthoredVisualHeight(out float visualHeight)
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
        visualHeight = hasBounds ? maxY - minY : 0f;
        return visualHeight > Mathf.Epsilon;
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
            return;
        }

        if ((topBorder == null || bottomBorder == null)
            && BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out Collider2D resolvedTop, out Collider2D resolvedBottom))
        {
            topBorder = resolvedTop;
            bottomBorder = resolvedBottom;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || gameplayCamera == null || topBorder == null || bottomBorder == null || wallCollider == null)
        {
            Debug.LogWarning(
                "[SSCarnageNetWall] Faltan referencias. El SS Carnage debe entregar Session, Camera, TopBorder y BottomBorder, y el prefab debe tener Collider2D.",
                this);
        }
    }
}
