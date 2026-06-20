using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParallaxLayer : MonoBehaviour
{
    private const int MinimumTileCount = 3;

    [Header("References")]
    [SerializeField] private SpriteRenderer sourceRenderer = null;
    [SerializeField] private Transform cameraTransform = null;

    [Header("Parallax")]
    [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.2f;
    [SerializeField] private bool followVertical = false;
    [SerializeField, Min(0)] private int extraTilesPerSide = 2;
    [SerializeField, Min(0f)] private float recycleSafetyTiles = 1f;
    [SerializeField, Min(MinimumTileCount)] private int maximumGeneratedTiles = 31;

    private readonly List<Transform> tiles = new();

    private Camera targetCamera;
    private float tileWidthWorld;
    private float tileWidthLocal;
    private Vector3 lastCameraPosition;
    private bool initialized;
    private bool tileLimitWarningLogged;

    private void Start()
    {
        ResolveCameraReference();

        if (cameraTransform == null || targetCamera == null)
        {
            Debug.LogError($"[{name}] No encontro Camera.main para inicializar parallax.", this);
            enabled = false;
            return;
        }

        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            Debug.LogError($"[{name}] Falta asignar SourceRenderer con un sprite valido.", this);
            enabled = false;
            return;
        }

        tileWidthWorld = sourceRenderer.bounds.size.x;

        if (tileWidthWorld <= 0f)
        {
            Debug.LogError($"[{name}] El ancho del sprite es invalido.", this);
            enabled = false;
            return;
        }

        float lossyX = Mathf.Abs(transform.lossyScale.x);
        tileWidthLocal = lossyX > 0f ? tileWidthWorld / lossyX : tileWidthWorld;

        BuildTiles();

        sourceRenderer.enabled = false;
        lastCameraPosition = cameraTransform.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        ResolveCameraReference();
        if (cameraTransform == null || targetCamera == null)
        {
            return;
        }

        EnsureTileCoverage();

        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        float moveX = cameraDelta.x * parallaxFactor;
        float moveY = followVertical ? cameraDelta.y * parallaxFactor : 0f;

        transform.position += new Vector3(moveX, moveY, 0f);

        RecycleTiles();

        lastCameraPosition = cameraTransform.position;
    }

    private void ResolveCameraReference()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (targetCamera == null && cameraTransform != null)
        {
            targetCamera = cameraTransform.GetComponent<Camera>();
        }

        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main;
            cameraTransform = targetCamera.transform;
        }
    }

    private void BuildTiles()
    {
        tiles.Clear();

        int tileCount = CalculateRequiredTileCount();
        int halfTileCount = tileCount / 2;
        for (int i = -halfTileCount; i <= halfTileCount; i++)
        {
            CreateTile(i);
        }
    }

    private void EnsureTileCoverage()
    {
        int requiredTileCount = CalculateRequiredTileCount();
        while (tiles.Count < requiredTileCount)
        {
            CreateTileBeforeLeftmost();
            if (tiles.Count >= requiredTileCount)
            {
                break;
            }

            CreateTileAfterRightmost();
        }
    }

    private int CalculateRequiredTileCount()
    {
        int configuredTileCount = NormalizeOddTileCount(Mathf.Max(0, extraTilesPerSide) * 2 + 1);
        if (!TryGetCameraHorizontalRange(out float visibleLeft, out float visibleRight))
        {
            return configuredTileCount;
        }

        float visibleWidth = Mathf.Max(0f, visibleRight - visibleLeft);
        int visibleTileCount = Mathf.CeilToInt(visibleWidth / Mathf.Max(0.01f, tileWidthWorld));
        int requiredTileCount = NormalizeOddTileCount(Mathf.Max(
            configuredTileCount,
            visibleTileCount + 2 + Mathf.Max(0, extraTilesPerSide) * 2));
        int tileLimit = Mathf.Max(configuredTileCount, NormalizeOddTileCount(maximumGeneratedTiles));

        if (requiredTileCount > tileLimit)
        {
            WarnTileLimit(requiredTileCount, tileLimit, visibleWidth);
            return tileLimit;
        }

        return requiredTileCount;
    }

    private int NormalizeOddTileCount(int tileCount)
    {
        int normalizedTileCount = Mathf.Max(MinimumTileCount, tileCount);
        return normalizedTileCount % 2 == 0
            ? normalizedTileCount + 1
            : normalizedTileCount;
    }

    private void WarnTileLimit(int requiredTileCount, int tileLimit, float visibleWidth)
    {
        if (tileLimitWarningLogged)
        {
            return;
        }

        tileLimitWarningLogged = true;
        Debug.LogWarning(
            $"[{name}] Parallax requiere {requiredTileCount} tiles para cubrir {visibleWidth:0.##} unidades, pero MaximumGeneratedTiles limita a {tileLimit}. Revisa ancho del sprite, zoom de camara o limite de tiles si aparecen huecos.",
            this);
    }

    private Transform CreateTile(int tileIndex)
    {
        GameObject clone = new GameObject($"Tile_{tileIndex}");
        clone.layer = sourceRenderer.gameObject.layer;
        clone.tag = sourceRenderer.gameObject.tag;
        clone.isStatic = sourceRenderer.gameObject.isStatic;
        clone.transform.SetParent(transform, false);
        clone.transform.localPosition = GetTileLocalPosition(tileIndex);
        clone.transform.localRotation = GetSourceLocalRotation();
        clone.transform.localScale = GetSourceLocalScale();

        SpriteRenderer cloneRenderer = clone.AddComponent<SpriteRenderer>();
        CopyRendererSettings(sourceRenderer, cloneRenderer);

        tiles.Add(clone.transform);
        return clone.transform;
    }

    private Vector3 GetTileLocalPosition(int tileIndex)
    {
        Vector3 sourceLocalPosition = sourceRenderer.transform == transform
            ? Vector3.zero
            : sourceRenderer.transform.localPosition;

        return new Vector3(
            sourceLocalPosition.x + tileIndex * tileWidthLocal,
            sourceLocalPosition.y,
            sourceLocalPosition.z);
    }

    private Quaternion GetSourceLocalRotation()
    {
        return sourceRenderer.transform == transform
            ? Quaternion.identity
            : sourceRenderer.transform.localRotation;
    }

    private Vector3 GetSourceLocalScale()
    {
        return sourceRenderer.transform == transform
            ? Vector3.one
            : sourceRenderer.transform.localScale;
    }

    private void CreateTileBeforeLeftmost()
    {
        Transform leftmost = GetLeftmostTile();
        Transform tile = CreateTile(-tiles.Count);
        tile.position = leftmost != null
            ? new Vector3(leftmost.position.x - tileWidthWorld, leftmost.position.y, leftmost.position.z)
            : transform.TransformPoint(GetTileLocalPosition(0));
    }

    private void CreateTileAfterRightmost()
    {
        Transform rightmost = GetRightmostTile();
        Transform tile = CreateTile(tiles.Count);
        tile.position = rightmost != null
            ? new Vector3(rightmost.position.x + tileWidthWorld, rightmost.position.y, rightmost.position.z)
            : transform.TransformPoint(GetTileLocalPosition(0));
    }

    private void CopyRendererSettings(SpriteRenderer from, SpriteRenderer to)
    {
        to.enabled = true;
        to.sprite = from.sprite;
        to.color = from.color;
        to.flipX = from.flipX;
        to.flipY = from.flipY;
        to.drawMode = from.drawMode;
        to.size = from.size;
        to.sortingLayerID = from.sortingLayerID;
        to.sortingOrder = from.sortingOrder;
        to.maskInteraction = from.maskInteraction;
        to.spriteSortPoint = from.spriteSortPoint;
        to.sharedMaterial = from.sharedMaterial;
    }

    private void RecycleTiles()
    {
        if (tiles.Count < MinimumTileCount || !TryGetCameraHorizontalRange(out float visibleLeft, out float visibleRight))
        {
            return;
        }

        Transform leftmost = GetLeftmostTile();
        Transform rightmost = GetRightmostTile();
        float recycleMargin = tileWidthWorld * Mathf.Max(0f, recycleSafetyTiles);

        while (leftmost != null && rightmost != null && GetTileRightEdge(leftmost) < visibleLeft - recycleMargin)
        {
            leftmost.position = new Vector3(
                rightmost.position.x + tileWidthWorld,
                leftmost.position.y,
                leftmost.position.z);

            leftmost = GetLeftmostTile();
            rightmost = GetRightmostTile();
        }

        while (leftmost != null && rightmost != null && GetTileLeftEdge(rightmost) > visibleRight + recycleMargin)
        {
            rightmost.position = new Vector3(
                leftmost.position.x - tileWidthWorld,
                rightmost.position.y,
                rightmost.position.z);

            leftmost = GetLeftmostTile();
            rightmost = GetRightmostTile();
        }
    }

    private bool TryGetCameraHorizontalRange(out float visibleLeft, out float visibleRight)
    {
        visibleLeft = 0f;
        visibleRight = 0f;

        ResolveCameraReference();
        if (targetCamera == null)
        {
            return false;
        }

        float depthToLayer = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
        Vector3 left = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, depthToLayer));
        Vector3 right = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depthToLayer));
        visibleLeft = Mathf.Min(left.x, right.x);
        visibleRight = Mathf.Max(left.x, right.x);
        return visibleRight > visibleLeft;
    }

    private float GetTileLeftEdge(Transform tile)
    {
        if (tile != null && tile.TryGetComponent(out Renderer renderer))
        {
            return renderer.bounds.min.x;
        }

        return tile != null ? tile.position.x - tileWidthWorld * 0.5f : 0f;
    }

    private float GetTileRightEdge(Transform tile)
    {
        if (tile != null && tile.TryGetComponent(out Renderer renderer))
        {
            return renderer.bounds.max.x;
        }

        return tile != null ? tile.position.x + tileWidthWorld * 0.5f : 0f;
    }

    private Transform GetLeftmostTile()
    {
        Transform result = tiles[0];

        for (int i = 1; i < tiles.Count; i++)
        {
            if (tiles[i].position.x < result.position.x)
            {
                result = tiles[i];
            }
        }

        return result;
    }

    private Transform GetRightmostTile()
    {
        Transform result = tiles[0];

        for (int i = 1; i < tiles.Count; i++)
        {
            if (tiles[i].position.x > result.position.x)
            {
                result = tiles[i];
            }
        }

        return result;
    }
}
