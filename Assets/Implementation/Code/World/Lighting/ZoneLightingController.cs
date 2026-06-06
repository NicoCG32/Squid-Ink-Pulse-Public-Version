using UnityEngine;

[DisallowMultipleComponent]
public class ZoneLightingController : MonoBehaviour
{
    private static ZoneLightingController instance;

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer darknessOverlay;

    [Header("Darkness")]
    [SerializeField, Range(0f, 1f)] private float darkAlpha = 0.68f;
    [SerializeField, Range(0f, 1f)] private float litAlpha = 0f;
    [SerializeField, Min(0f)] private float litHoldSeconds = 0.55f;
    [SerializeField, Min(0.01f)] private float fadeToLitSpeed = 7f;
    [SerializeField, Min(0.01f)] private float fadeToDarkSpeed = 1.75f;
    [SerializeField, Min(0f)] private float overlayPadding = 2f;

    [Header("Light Graze")]
    [SerializeField, Min(0.01f)] private float lightGrazeRadius = 2.5f;

    private float litTimer;

    public static ZoneLightingController Instance => instance;
    public static bool HasInstance => instance != null;
    public float LightGrazeRadius => Mathf.Max(0.01f, lightGrazeRadius);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveReferences();
        ApplyOverlayAlpha(darkAlpha);
        FitOverlayToCamera();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (litTimer > 0f)
        {
            litTimer = Mathf.Max(0f, litTimer - Time.deltaTime);
        }

        float targetAlpha = litTimer > 0f ? litAlpha : darkAlpha;
        float speed = targetAlpha < GetCurrentOverlayAlpha() ? fadeToLitSpeed : fadeToDarkSpeed;
        float nextAlpha = Mathf.MoveTowards(
            GetCurrentOverlayAlpha(),
            targetAlpha,
            speed * Time.deltaTime);

        ApplyOverlayAlpha(nextAlpha);
    }

    private void LateUpdate()
    {
        FitOverlayToCamera();
    }

    public void NotifyLightGraze()
    {
        litTimer = Mathf.Max(litTimer, litHoldSeconds);
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
        if (darknessOverlay == null || targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        Transform overlayTransform = darknessOverlay.transform;
        Vector3 cameraPosition = targetCamera.transform.position;
        overlayTransform.position = new Vector3(cameraPosition.x, cameraPosition.y, overlayTransform.position.z);

        float worldHeight = targetCamera.orthographicSize * 2f + overlayPadding;
        float worldWidth = worldHeight * targetCamera.aspect + overlayPadding;
        Vector2 spriteSize = darknessOverlay.sprite != null
            ? darknessOverlay.sprite.bounds.size
            : Vector2.one;

        overlayTransform.localScale = new Vector3(
            worldWidth / Mathf.Max(0.01f, spriteSize.x),
            worldHeight / Mathf.Max(0.01f, spriteSize.y),
            1f);
    }

    private float GetCurrentOverlayAlpha()
    {
        return darknessOverlay != null ? darknessOverlay.color.a : 0f;
    }

    private void ApplyOverlayAlpha(float alpha)
    {
        if (darknessOverlay == null)
        {
            return;
        }

        Color color = darknessOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        darknessOverlay.color = color;
    }

    private void WarnIfMissingReferences()
    {
        if (targetCamera == null || darknessOverlay == null)
        {
            Debug.LogWarning("[ZoneLightingController] Faltan referencias. Asigna TargetCamera y DarknessOverlay.", this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
