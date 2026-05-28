using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuBubbles : MonoBehaviour
{
    [Header("Bubble Generation")]
    [SerializeField, Min(0)] protected int cantidadBurbujas = 15;
    [SerializeField, Min(0f)] protected float velocidad = 100f;
    [SerializeField, Min(0f)] protected float tamanoMin = 20f;
    [SerializeField, Min(0f)] protected float tamanoMax = 55f;
    [SerializeField] protected Color colorBurbuja = new Color(0.5f, 0.8f, 1f, 0.18f);

    [Header("Layering")]
    [SerializeField] private bool keepOwnerBehindSiblingUi = true;
    [SerializeField] private bool keepBubblesBehindOwnerChildren = true;
    [SerializeField] private string bubbleLayerName = "BubbleLayer";

    private readonly List<MenuBubbleData> bubbles = new List<MenuBubbleData>();
    private RectTransform parentRect;
    private RectTransform bubbleLayer;
    private Sprite circleSprite;

    private void OnEnable()
    {
        parentRect = GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning("[MenuBubbles] Este componente requiere un RectTransform.", this);
            return;
        }

        ApplyLayering();
        EnsureBubbleLayer();
        circleSprite = CreateCircleSprite();
        RebuildBubbles();
    }

    private void Update()
    {
        if (parentRect == null || bubbleLayer == null)
        {
            return;
        }

        ApplyLayering();
        MoveBubbles();
    }

    private void ApplyLayering()
    {
        if (keepOwnerBehindSiblingUi)
        {
            transform.SetAsFirstSibling();
        }

        if (keepBubblesBehindOwnerChildren && bubbleLayer != null)
        {
            bubbleLayer.SetAsFirstSibling();
        }
    }

    private void EnsureBubbleLayer()
    {
        Transform existingLayer = transform.Find(bubbleLayerName);
        if (existingLayer != null)
        {
            bubbleLayer = existingLayer as RectTransform;
        }

        if (bubbleLayer == null)
        {
            GameObject layerObject = new GameObject(bubbleLayerName, typeof(RectTransform));
            layerObject.transform.SetParent(transform, false);
            bubbleLayer = layerObject.GetComponent<RectTransform>();
        }

        bubbleLayer.anchorMin = Vector2.zero;
        bubbleLayer.anchorMax = Vector2.one;
        bubbleLayer.anchoredPosition = Vector2.zero;
        bubbleLayer.sizeDelta = Vector2.zero;
        bubbleLayer.pivot = new Vector2(0.5f, 0.5f);
        bubbleLayer.localScale = Vector3.one;

        if (keepBubblesBehindOwnerChildren)
        {
            bubbleLayer.SetAsFirstSibling();
        }
    }

    private void RebuildBubbles()
    {
        bubbles.Clear();
        ClearLegacyBubbles();
        ClearBubbleLayer();

        int count = Mathf.Max(0, cantidadBurbujas);
        for (int i = 0; i < count; i++)
        {
            SpawnBubble(initialRandomPosition: true);
        }
    }

    private void MoveBubbles()
    {
        float halfHeight = parentRect.rect.height * 0.5f;
        for (int i = 0; i < bubbles.Count; i++)
        {
            MenuBubbleData data = bubbles[i];
            if (data == null || data.RectTransform == null)
            {
                continue;
            }

            data.Timer += Time.unscaledDeltaTime;

            float y = data.RectTransform.anchoredPosition.y + data.Speed * Time.unscaledDeltaTime;
            float x = data.StartX + Mathf.Sin(data.Timer * data.WobbleSpeed) * data.WobbleAmount;
            data.RectTransform.anchoredPosition = new Vector2(x, y);

            if (y > halfHeight + data.Size)
            {
                ResetBubbleAtBottom(data);
            }
        }
    }

    private void SpawnBubble(bool initialRandomPosition)
    {
        GameObject bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(MenuBubbleData));
        bubble.transform.SetParent(bubbleLayer, false);

        float minSize = Mathf.Min(tamanoMin, tamanoMax);
        float maxSize = Mathf.Max(tamanoMin, tamanoMax);
        float size = Random.Range(minSize, maxSize);

        RectTransform rect = bubble.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        float halfWidth = parentRect.rect.width * 0.45f;
        float halfHeight = parentRect.rect.height * 0.5f;
        float x = Random.Range(-halfWidth, halfWidth);
        float y = initialRandomPosition ? Random.Range(-halfHeight, halfHeight) : -halfHeight - size;
        rect.anchoredPosition = new Vector2(x, y);

        Image image = bubble.GetComponent<Image>();
        image.sprite = circleSprite;
        image.color = CreateBubbleColor();
        image.raycastTarget = false;

        MenuBubbleData data = bubble.GetComponent<MenuBubbleData>();
        data.Initialize(
            rect,
            velocidad + Random.Range(-30f, 30f),
            Random.Range(15f, 45f),
            Random.Range(1.5f, 3f),
            x,
            size,
            Random.Range(0f, 6f));

        bubbles.Add(data);
    }

    private void ResetBubbleAtBottom(MenuBubbleData data)
    {
        float halfWidth = parentRect.rect.width * 0.45f;
        float halfHeight = parentRect.rect.height * 0.5f;
        float newX = Random.Range(-halfWidth, halfWidth);

        data.StartX = newX;
        data.Timer = 0f;
        data.RectTransform.anchoredPosition = new Vector2(newX, -halfHeight - data.Size);
    }

    private Color CreateBubbleColor()
    {
        float maxAlpha = Mathf.Clamp01(colorBurbuja.a);
        float minAlpha = Mathf.Min(0.06f, maxAlpha);

        return new Color(
            Mathf.Clamp01(colorBurbuja.r + Random.Range(-0.05f, 0.05f)),
            Mathf.Clamp01(colorBurbuja.g + Random.Range(-0.05f, 0.05f)),
            Mathf.Clamp01(colorBurbuja.b + Random.Range(-0.02f, 0.02f)),
            Random.Range(minAlpha, maxAlpha));
    }

    private void ClearBubbleLayer()
    {
        if (bubbleLayer == null)
        {
            return;
        }

        for (int i = bubbleLayer.childCount - 1; i >= 0; i--)
        {
            DestroyBubbleObject(bubbleLayer.GetChild(i).gameObject);
        }
    }

    private void ClearLegacyBubbles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == bubbleLayer || child.name == bubbleLayerName || !child.name.StartsWith("Bubble"))
            {
                continue;
            }

            DestroyBubbleObject(child.gameObject);
        }
    }

    private void DestroyBubbleObject(GameObject bubble)
    {
        if (Application.isPlaying)
        {
            Destroy(bubble);
        }
        else
        {
            DestroyImmediate(bubble);
        }
    }

    private Sprite CreateCircleSprite()
    {
        const int resolution = 32;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float center = resolution * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - distance) / 1.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
}

public sealed class MenuBubbleData : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }
    public float Speed { get; private set; }
    public float WobbleAmount { get; private set; }
    public float WobbleSpeed { get; private set; }
    public float StartX { get; set; }
    public float Size { get; private set; }
    public float Timer { get; set; }

    public void Initialize(
        RectTransform rectTransform,
        float speed,
        float wobbleAmount,
        float wobbleSpeed,
        float startX,
        float size,
        float timer)
    {
        RectTransform = rectTransform;
        Speed = speed;
        WobbleAmount = wobbleAmount;
        WobbleSpeed = wobbleSpeed;
        StartX = startX;
        Size = size;
        Timer = timer;
    }
}
