using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.2f;
    [SerializeField] private bool followVertical = false;
    [SerializeField] private int extraTilesPerSide = 2;

    private SpriteRenderer sourceRenderer;
    private readonly List<Transform> tiles = new();

    private float tileWidthWorld;
    private float tileWidthLocal;
    private Vector3 lastCameraPosition;
    private bool initialized;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (cameraTransform == null)
        {
            Debug.LogError($"[{name}] No se encontró la Main Camera.");
            enabled = false;
            return;
        }

        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            Debug.LogError($"[{name}] Este objeto necesita un SpriteRenderer con sprite asignado.");
            enabled = false;
            return;
        }

        tileWidthWorld = sourceRenderer.bounds.size.x;

        if (tileWidthWorld <= 0f)
        {
            Debug.LogError($"[{name}] El ancho del sprite es inválido.");
            enabled = false;
            return;
        }

        float lossyX = Mathf.Abs(transform.lossyScale.x);
        tileWidthLocal = lossyX > 0f ? tileWidthWorld / lossyX : tileWidthWorld;

        BuildTiles();

        // El padre queda solo como contenedor, el sprite original se oculta
        sourceRenderer.enabled = false; 
        lastCameraPosition = cameraTransform.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized) return;

        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        float moveX = cameraDelta.x * parallaxFactor;
        float moveY = followVertical ? cameraDelta.y * parallaxFactor : 0f;

        transform.position += new Vector3(moveX, moveY, 0f);

        RecycleTiles();

        lastCameraPosition = cameraTransform.position;
    }

    private void BuildTiles()
    {
        tiles.Clear();

        for (int i = -extraTilesPerSide; i <= extraTilesPerSide; i++)
        {
            GameObject clone = new GameObject($"Tile_{i}");
            clone.transform.SetParent(transform, false);
            clone.transform.localPosition = new Vector3(i * tileWidthLocal, 0f, 0f);
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            SpriteRenderer cloneRenderer = clone.AddComponent<SpriteRenderer>();
            CopyRendererSettings(sourceRenderer, cloneRenderer);

            tiles.Add(clone.transform);
        }
    }

    private void CopyRendererSettings(SpriteRenderer from, SpriteRenderer to)
    {
        to.sprite = from.sprite;
        to.color = from.color;
        to.flipX = from.flipX;
        to.flipY = from.flipY;
        to.drawMode = from.drawMode;
        to.size = from.size;
        to.sortingLayerID = from.sortingLayerID;
        to.sortingOrder = from.sortingOrder;
        to.maskInteraction = from.maskInteraction;
        to.sharedMaterial = from.sharedMaterial;
    }

    private void RecycleTiles()
    {
        if (tiles.Count < 3) return;

        Transform leftmost = GetLeftmostTile();
        Transform rightmost = GetRightmostTile();
        float cameraX = cameraTransform.position.x;

        while (cameraX > leftmost.position.x + tileWidthWorld)
        {
            leftmost.position = new Vector3(
                rightmost.position.x + tileWidthWorld,
                leftmost.position.y,
                leftmost.position.z
            );

            leftmost = GetLeftmostTile();
            rightmost = GetRightmostTile();
        }

        while (cameraX < rightmost.position.x - tileWidthWorld)
        {
            rightmost.position = new Vector3(
                leftmost.position.x - tileWidthWorld,
                rightmost.position.y,
                rightmost.position.z
            );

            leftmost = GetLeftmostTile();
            rightmost = GetRightmostTile();
        }
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