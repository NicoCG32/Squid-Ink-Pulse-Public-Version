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

    public static ZoneLightingController Instance => instance;
    public static bool HasInstance => instance != null;
    public float LightHoleRadius => Mathf.Max(0.01f, lightHoleRadius);
    public bool UsesLightFeather => lightEdgeSoftness > 0f && layerBlack != null;
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

        layerBlack.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        Color color = Color.black;
        color.a = Mathf.Clamp01(blackAlpha);
        layerBlack.color = color;
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
    }
}
