using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class ZoneLightingController : MonoBehaviour
{
    private static ZoneLightingController instance;
    private static Sprite sharedCircleMaskSprite;
    private static Texture2D sharedCircleMaskTexture;
    private static Sprite sharedFeatherSprite;
    private static Texture2D sharedFeatherTexture;
    private static float sharedFeatherSoftness = -1f;

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Camera targetCamera;
    [FormerlySerializedAs("darknessOverlay")]
    [SerializeField] private SpriteRenderer layerBlack;

    [Header("Layer Black")]
    [FormerlySerializedAs("darkAlpha")]
    [SerializeField, Range(0f, 1f)] private float blackAlpha = 0.68f;
    [SerializeField, Min(0f)] private float overlayPadding = 2f;
    [SerializeField, Min(0)] private int maskSortingOrderPadding = 1;

    [Header("Light Holes")]
    [FormerlySerializedAs("lightGrazeRadius")]
    [SerializeField, Min(0.01f)] private float lightHoleRadius = 1.15f;
    [SerializeField, Range(0f, 0.95f)] private float lightEdgeSoftness = 0.35f;
    [SerializeField, Range(0.01f, 1f)] private float maskAlphaCutoff = 0.5f;

    [Header("Composite Overlay")]
    [SerializeField] private bool useCompositeLightOverlay = true;
    [SerializeField, Min(32)] private int compositeTextureWidth = 256;
    [SerializeField, Min(18)] private int compositeTextureHeight = 144;

    private readonly List<Vector3> activeLightPositions = new();
    private Sprite originalLayerBlackSprite;
    private Texture2D compositeTexture;
    private Sprite compositeSprite;
    private Color32[] compositePixels;
    private int currentCompositeTextureWidth;
    private int currentCompositeTextureHeight;

    public static ZoneLightingController Instance => instance;
    public static bool HasInstance => instance != null;
    public float LightHoleRadius => Mathf.Max(0.01f, lightHoleRadius);
    public bool UsesCompositeLightOverlay => useCompositeLightOverlay && layerBlack != null && targetCamera != null && targetCamera.orthographic;
    public bool UsesLightFeather => !UsesCompositeLightOverlay && lightEdgeSoftness > 0f && layerBlack != null;
    public Sprite CircleMaskSprite => GetOrCreateCircleMaskSprite();
    public Sprite LightFeatherSprite => GetOrCreateFeatherSprite(lightEdgeSoftness);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveReferences();
        ConfigureLayerBlack();
        FitOverlayToCamera();
        LightGrazeSource.RefreshAllActiveSources();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        ResolveReferences();
        ConfigureLayerBlack();
    }

    private void LateUpdate()
    {
        FitOverlayToCamera();
        UpdateCompositeOverlay();
    }

    public void ConfigureLightMask(SpriteMask lightMask)
    {
        if (lightMask == null || layerBlack == null)
        {
            return;
        }

        lightMask.sprite = CircleMaskSprite;
        lightMask.alphaCutoff = maskAlphaCutoff;
        lightMask.isCustomRangeActive = true;
        lightMask.frontSortingLayerID = layerBlack.sortingLayerID;
        lightMask.backSortingLayerID = layerBlack.sortingLayerID;
        lightMask.frontSortingOrder = layerBlack.sortingOrder + maskSortingOrderPadding;
        lightMask.backSortingOrder = layerBlack.sortingOrder - maskSortingOrderPadding;
    }

    public void ConfigureLightFeather(SpriteRenderer lightFeather)
    {
        if (lightFeather == null || layerBlack == null)
        {
            return;
        }

        lightFeather.sprite = LightFeatherSprite;
        lightFeather.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        lightFeather.sortingLayerID = layerBlack.sortingLayerID;
        lightFeather.sortingOrder = layerBlack.sortingOrder;

        Color color = Color.black;
        color.a = Mathf.Clamp01(blackAlpha);
        lightFeather.color = color;
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void FitOverlayToCamera()
    {
        if (layerBlack == null || targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        Transform overlayTransform = layerBlack.transform;
        Vector3 cameraPosition = targetCamera.transform.position;
        overlayTransform.position = new Vector3(cameraPosition.x, cameraPosition.y, overlayTransform.position.z);

        float worldHeight = targetCamera.orthographicSize * 2f + overlayPadding;
        float worldWidth = worldHeight * targetCamera.aspect + overlayPadding;
        Vector2 spriteSize = layerBlack.sprite != null
            ? layerBlack.sprite.bounds.size
            : Vector2.one;

        overlayTransform.localScale = new Vector3(
            worldWidth / Mathf.Max(0.01f, spriteSize.x),
            worldHeight / Mathf.Max(0.01f, spriteSize.y),
            1f);
    }

    private void ConfigureLayerBlack()
    {
        if (layerBlack == null)
        {
            return;
        }

        if (UsesCompositeLightOverlay)
        {
            EnsureCompositeResources();
            layerBlack.sprite = compositeSprite;
            layerBlack.maskInteraction = SpriteMaskInteraction.None;
            layerBlack.color = Color.white;
            return;
        }

        if (originalLayerBlackSprite != null && layerBlack.sprite == compositeSprite)
        {
            layerBlack.sprite = originalLayerBlackSprite;
        }

        layerBlack.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        Color color = Color.black;
        color.a = Mathf.Clamp01(blackAlpha);
        layerBlack.color = color;
    }

    private void UpdateCompositeOverlay()
    {
        if (!UsesCompositeLightOverlay)
        {
            return;
        }

        EnsureCompositeResources();
        LightGrazeSource.CollectActiveWorldPositions(activeLightPositions);

        int width = currentCompositeTextureWidth;
        int height = currentCompositeTextureHeight;
        float alphaOutsideLight = Mathf.Clamp01(blackAlpha);

        float worldHeight = targetCamera.orthographicSize * 2f + overlayPadding;
        float worldWidth = worldHeight * targetCamera.aspect + overlayPadding;
        Vector3 cameraPosition = targetCamera.transform.position;
        float worldMinX = cameraPosition.x - worldWidth * 0.5f;
        float worldMinY = cameraPosition.y - worldHeight * 0.5f;
        float radius = LightHoleRadius;
        float innerRadius = radius * (1f - Mathf.Clamp01(lightEdgeSoftness));
        float featherWidth = Mathf.Max(0.0001f, radius - innerRadius);

        for (int y = 0; y < height; y++)
        {
            float normalizedY = (y + 0.5f) / height;
            float worldY = worldMinY + normalizedY * worldHeight;

            for (int x = 0; x < width; x++)
            {
                float normalizedX = (x + 0.5f) / width;
                float worldX = worldMinX + normalizedX * worldWidth;
                float targetAlpha = alphaOutsideLight;

                for (int i = 0; i < activeLightPositions.Count; i++)
                {
                    Vector3 lightPosition = activeLightPositions[i];
                    float distance = Vector2.Distance(
                        new Vector2(worldX, worldY),
                        new Vector2(lightPosition.x, lightPosition.y));

                    if (distance > radius)
                    {
                        continue;
                    }

                    float lightAlpha = CalculateCompositeLightAlpha(distance, innerRadius, featherWidth, alphaOutsideLight);
                    targetAlpha = Mathf.Min(targetAlpha, lightAlpha);
                }

                byte alphaByte = (byte)Mathf.RoundToInt(targetAlpha * byte.MaxValue);
                compositePixels[y * width + x] = new Color32(0, 0, 0, alphaByte);
            }
        }

        compositeTexture.SetPixels32(compositePixels);
        compositeTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    }

    private float CalculateCompositeLightAlpha(
        float distance,
        float innerRadius,
        float featherWidth,
        float alphaOutsideLight)
    {
        if (lightEdgeSoftness <= 0f || distance <= innerRadius)
        {
            return 0f;
        }

        float edgeProgress = Mathf.Clamp01((distance - innerRadius) / featherWidth);
        return alphaOutsideLight * Mathf.SmoothStep(0f, 1f, edgeProgress);
    }

    private void EnsureCompositeResources()
    {
        if (layerBlack == null)
        {
            return;
        }

        if (originalLayerBlackSprite == null && layerBlack.sprite != null && layerBlack.sprite != compositeSprite)
        {
            originalLayerBlackSprite = layerBlack.sprite;
        }

        int width = Mathf.Max(32, compositeTextureWidth);
        int height = Mathf.Max(18, compositeTextureHeight);
        if (compositeTexture != null
            && compositeSprite != null
            && currentCompositeTextureWidth == width
            && currentCompositeTextureHeight == height)
        {
            return;
        }

        DestroyCompositeResources();

        currentCompositeTextureWidth = width;
        currentCompositeTextureHeight = height;
        compositePixels = new Color32[width * height];

        compositeTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "GeneratedCompositeLayerBlack",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        compositeSprite = Sprite.Create(
            compositeTexture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            height);
        compositeSprite.name = "GeneratedCompositeLayerBlack";
        compositeSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    private void DestroyCompositeResources()
    {
        if (compositeSprite != null)
        {
            Destroy(compositeSprite);
            compositeSprite = null;
        }

        if (compositeTexture != null)
        {
            Destroy(compositeTexture);
            compositeTexture = null;
        }

        compositePixels = null;
        currentCompositeTextureWidth = 0;
        currentCompositeTextureHeight = 0;
    }

    private void WarnIfMissingReferences()
    {
        if (targetCamera == null || layerBlack == null)
        {
            Debug.LogWarning("[ZoneLightingController] Faltan referencias. Asigna TargetCamera y LayerBlack.", this);
        }
    }

    private static Sprite GetOrCreateCircleMaskSprite()
    {
        if (sharedCircleMaskSprite != null)
        {
            return sharedCircleMaskSprite;
        }

        const int textureSize = 64;
        const float radius = (textureSize - 2) * 0.5f;
        Vector2 center = new((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);

        sharedCircleMaskTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "GeneratedLightGrazeCircleMask",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = distance <= radius ? byte.MaxValue : (byte)0;
                pixels[y * textureSize + x] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
            }
        }

        sharedCircleMaskTexture.SetPixels32(pixels);
        sharedCircleMaskTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        sharedCircleMaskSprite = Sprite.Create(
            sharedCircleMaskTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        sharedCircleMaskSprite.name = "GeneratedLightGrazeCircleMask";
        sharedCircleMaskSprite.hideFlags = HideFlags.HideAndDontSave;
        return sharedCircleMaskSprite;
    }

    private static Sprite GetOrCreateFeatherSprite(float softness)
    {
        float normalizedSoftness = Mathf.Clamp(softness, 0f, 0.95f);
        if (sharedFeatherSprite != null && Mathf.Approximately(sharedFeatherSoftness, normalizedSoftness))
        {
            return sharedFeatherSprite;
        }

        if (sharedFeatherSprite != null)
        {
            Destroy(sharedFeatherSprite);
            sharedFeatherSprite = null;
        }

        if (sharedFeatherTexture != null)
        {
            Destroy(sharedFeatherTexture);
            sharedFeatherTexture = null;
        }

        const int textureSize = 128;
        const float radius = (textureSize - 2) * 0.5f;
        Vector2 center = new((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float innerRadius = radius * (1f - normalizedSoftness);
        float featherWidth = Mathf.Max(0.0001f, radius - innerRadius);

        sharedFeatherTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "GeneratedLightGrazeFeather",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;

                if (distance <= radius && normalizedSoftness > 0f)
                {
                    float edgeProgress = Mathf.Clamp01((distance - innerRadius) / featherWidth);
                    alpha = Mathf.SmoothStep(0f, 1f, edgeProgress);
                }

                pixels[y * textureSize + x] = new Color32(
                    byte.MaxValue,
                    byte.MaxValue,
                    byte.MaxValue,
                    (byte)Mathf.RoundToInt(alpha * byte.MaxValue));
            }
        }

        sharedFeatherTexture.SetPixels32(pixels);
        sharedFeatherTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        sharedFeatherSprite = Sprite.Create(
            sharedFeatherTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        sharedFeatherSprite.name = "GeneratedLightGrazeFeather";
        sharedFeatherSprite.hideFlags = HideFlags.HideAndDontSave;
        sharedFeatherSoftness = normalizedSoftness;
        return sharedFeatherSprite;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            LightGrazeSource.RefreshAllActiveSources();
        }

        DestroyCompositeResources();
    }
}
