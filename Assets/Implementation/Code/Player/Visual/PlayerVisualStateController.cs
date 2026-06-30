using UnityEngine;

[DisallowMultipleComponent]
public class PlayerVisualStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStateController playerState;
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private GameObject movementVisualRoot;
    [SerializeField] private GameObject inkPulseVisualRoot;
    [SerializeField] private GameObject portalVisualRoot;
    [SerializeField] private Animator movementAnimator;
    [SerializeField] private Animator inkPulseAnimator;
    [SerializeField] private Animator portalAnimator;

    [Header("Animation States")]
    [SerializeField] private string inkPulseStateName = "InkPulse";
    [SerializeField] private string inkPulseClipName = "InkPulse";
    [SerializeField] private string portalStateName = "Portal";
    [SerializeField] private string portalClipName = "PortalEffect";
    [SerializeField, Min(0.01f)] private float fallbackInkPulseClipLength = 1f;
    [SerializeField, Min(0.01f)] private float fallbackPortalClipLength = 1f;

    private GameObject defaultMovementVisualRoot;
    private GameObject defaultInkPulseVisualRoot;
    private GameObject defaultPortalVisualRoot;
    private Animator defaultMovementAnimator;
    private Animator defaultInkPulseAnimator;
    private Animator defaultPortalAnimator;
    private string defaultInkPulseStateName;
    private string defaultInkPulseClipName;
    private string defaultPortalStateName;
    private string defaultPortalClipName;
    private float defaultFallbackInkPulseClipLength;
    private float defaultFallbackPortalClipLength;
    private bool defaultVisualSetCaptured;
    private Renderer[] movementRenderers;
    private Renderer[] inkPulseRenderers;
    private Renderer[] portalRenderers;
    private float resolvedInkPulseClipLength;
    private float resolvedPortalClipLength;
    private float lastPortalTransitionDuration;
    private bool warnedMissingReferences;

    public float PortalTransitionDuration => Mathf.Max(
        0.01f,
        lastPortalTransitionDuration > 0f ? lastPortalTransitionDuration : resolvedPortalClipLength);

    private void Awake()
    {
        ResolveReferences();
        CaptureDefaultVisualSet();
        ResolveRendererCaches();
        ResolveClipLengths();
        ApplyCurrentState();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureDefaultVisualSet();
        SubscribeToState();
        ApplyCurrentState();
    }

    private void Start()
    {
        ResolveReferences();
        CaptureDefaultVisualSet();
        ResolveRendererCaches();
        ResolveClipLengths();
        SubscribeToState();
        ApplyCurrentState();
        WarnIfMissingReferences();
    }

    private void OnDisable()
    {
        if (playerState != null)
        {
            playerState.StateChanged -= HandlePlayerStateChanged;
        }
    }

    public bool ApplySkinVisualSet(PlayerSkinVisualSet visualSet)
    {
        if (visualSet == null)
        {
            UseDefaultVisualSet();
            return false;
        }

        ResolveReferences();
        CaptureDefaultVisualSet();
        visualSet.ResolveReferences();
        if (!visualSet.IsConfigured)
        {
            UseDefaultVisualSet();
            return false;
        }

        HideCurrentVisualSet();
        movementVisualRoot = visualSet.MovementVisualRoot;
        inkPulseVisualRoot = visualSet.InkPulseVisualRoot;
        portalVisualRoot = visualSet.PortalVisualRoot;
        movementAnimator = visualSet.MovementAnimator;
        inkPulseAnimator = visualSet.InkPulseAnimator;
        portalAnimator = visualSet.PortalAnimator;
        inkPulseStateName = string.IsNullOrWhiteSpace(visualSet.InkPulseStateName)
            ? defaultInkPulseStateName
            : visualSet.InkPulseStateName;
        inkPulseClipName = string.IsNullOrWhiteSpace(visualSet.InkPulseClipName)
            ? defaultInkPulseClipName
            : visualSet.InkPulseClipName;
        portalStateName = string.IsNullOrWhiteSpace(visualSet.PortalStateName)
            ? defaultPortalStateName
            : visualSet.PortalStateName;
        portalClipName = string.IsNullOrWhiteSpace(visualSet.PortalClipName)
            ? defaultPortalClipName
            : visualSet.PortalClipName;
        fallbackInkPulseClipLength = visualSet.FallbackInkPulseClipLength;
        fallbackPortalClipLength = visualSet.FallbackPortalClipLength;

        RefreshVisualSet();
        return true;
    }

    public void UseDefaultVisualSet()
    {
        ResolveReferences();
        CaptureDefaultVisualSet();
        if (!defaultVisualSetCaptured)
        {
            return;
        }

        HideCurrentVisualSet();
        movementVisualRoot = defaultMovementVisualRoot;
        inkPulseVisualRoot = defaultInkPulseVisualRoot;
        portalVisualRoot = defaultPortalVisualRoot;
        movementAnimator = defaultMovementAnimator;
        inkPulseAnimator = defaultInkPulseAnimator;
        portalAnimator = defaultPortalAnimator;
        inkPulseStateName = defaultInkPulseStateName;
        inkPulseClipName = defaultInkPulseClipName;
        portalStateName = defaultPortalStateName;
        portalClipName = defaultPortalClipName;
        fallbackInkPulseClipLength = defaultFallbackInkPulseClipLength;
        fallbackPortalClipLength = defaultFallbackPortalClipLength;

        RefreshVisualSet();
    }

    public void RefreshVisualSet()
    {
        ResolveRendererCaches();
        ResolveClipLengths();
        ApplyCurrentState();
    }

    private void HandlePlayerStateChanged(PlayerRuntimeState previousState, PlayerRuntimeState nextState)
    {
        ApplyVisualForState(nextState);
    }

    private void ApplyCurrentState()
    {
        if (playerState != null)
        {
            ApplyVisualForState(playerState.CurrentState);
            return;
        }

        ApplyVisualForState(inkPulse != null && inkPulse.IsPulseActive
            ? PlayerRuntimeState.InkPulse
            : PlayerRuntimeState.Moving);
    }

    private void ApplyVisualForState(PlayerRuntimeState state)
    {
        if (state == PlayerRuntimeState.PortalTransition)
        {
            ShowPortalVisual();
            return;
        }

        if (state == PlayerRuntimeState.InkPulse)
        {
            ShowInkPulseVisual();
            return;
        }

        ShowMovementVisual();
    }

    private void ShowMovementVisual()
    {
        SetRenderersVisible(movementRenderers, true);
        SetRenderersVisible(inkPulseRenderers, false);
        SetRenderersVisible(portalRenderers, false);

        SetAnimatorSpeed(movementAnimator, 1f);
        SetAnimatorSpeed(inkPulseAnimator, 0f);
        SetAnimatorSpeed(portalAnimator, 0f);
    }

    private void ShowInkPulseVisual()
    {
        SetRenderersVisible(movementRenderers, false);
        SetRenderersVisible(inkPulseRenderers, true);
        SetRenderersVisible(portalRenderers, false);

        SetAnimatorSpeed(movementAnimator, 0f);
        SetAnimatorSpeed(portalAnimator, 0f);

        if (inkPulseAnimator == null || inkPulse == null)
        {
            return;
        }

        float pulseDuration = Mathf.Max(0.01f, inkPulse.PulseDuration);
        float remainingSeconds = Mathf.Clamp(inkPulse.PulseRemainingSeconds, 0f, pulseDuration);
        float elapsedRatio = 1f - (remainingSeconds / pulseDuration);

        inkPulseAnimator.speed = Mathf.Max(0.01f, resolvedInkPulseClipLength) / pulseDuration;
        inkPulseAnimator.Play(inkPulseStateName, 0, Mathf.Clamp01(elapsedRatio));
        inkPulseAnimator.Update(0f);
    }

    private void ShowPortalVisual()
    {
        SetRenderersVisible(movementRenderers, false);
        SetRenderersVisible(inkPulseRenderers, false);
        SetRenderersVisible(portalRenderers, true);

        SetAnimatorSpeed(movementAnimator, 0f);
        SetAnimatorSpeed(inkPulseAnimator, 0f);

        if (portalAnimator == null)
        {
            lastPortalTransitionDuration = resolvedPortalClipLength;
            return;
        }

        portalAnimator.speed = 1f;
        portalAnimator.Play(portalStateName, 0, 0f);
        portalAnimator.Update(0f);
        lastPortalTransitionDuration = ResolveCurrentStateLength(portalAnimator, resolvedPortalClipLength);
    }

    private void ResolveReferences()
    {
        if (playerState == null)
        {
            playerState = GetComponent<PlayerStateController>();
        }

        if (inkPulse == null)
        {
            inkPulse = GetComponent<InkPulseController>();
        }

        movementVisualRoot ??= ResolveChild("SquidVisual");
        inkPulseVisualRoot ??= ResolveChild("InkPulseVisual");
        portalVisualRoot ??= ResolveChild("PortalVisual");

        if (movementAnimator == null && movementVisualRoot != null)
        {
            movementAnimator = movementVisualRoot.GetComponent<Animator>();
        }

        if (inkPulseAnimator == null && inkPulseVisualRoot != null)
        {
            inkPulseAnimator = inkPulseVisualRoot.GetComponent<Animator>();
        }

        if (portalAnimator == null && portalVisualRoot != null)
        {
            portalAnimator = portalVisualRoot.GetComponent<Animator>();
        }
    }

    private GameObject ResolveChild(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void ResolveRendererCaches()
    {
        movementRenderers = ResolveRenderers(movementVisualRoot);
        inkPulseRenderers = ResolveRenderers(inkPulseVisualRoot);
        portalRenderers = ResolveRenderers(portalVisualRoot);
    }

    private static Renderer[] ResolveRenderers(GameObject root)
    {
        return root != null
            ? root.GetComponentsInChildren<Renderer>(includeInactive: true)
            : System.Array.Empty<Renderer>();
    }

    private void ResolveClipLengths()
    {
        resolvedInkPulseClipLength = ResolveClipLength(
            inkPulseAnimator,
            inkPulseClipName,
            fallbackInkPulseClipLength);

        resolvedPortalClipLength = ResolveClipLength(
            portalAnimator,
            portalClipName,
            fallbackPortalClipLength);

        lastPortalTransitionDuration = resolvedPortalClipLength;
    }

    private void CaptureDefaultVisualSet()
    {
        if (defaultVisualSetCaptured)
        {
            return;
        }

        defaultMovementVisualRoot = movementVisualRoot;
        defaultInkPulseVisualRoot = inkPulseVisualRoot;
        defaultPortalVisualRoot = portalVisualRoot;
        defaultMovementAnimator = movementAnimator;
        defaultInkPulseAnimator = inkPulseAnimator;
        defaultPortalAnimator = portalAnimator;
        defaultInkPulseStateName = inkPulseStateName;
        defaultInkPulseClipName = inkPulseClipName;
        defaultPortalStateName = portalStateName;
        defaultPortalClipName = portalClipName;
        defaultFallbackInkPulseClipLength = fallbackInkPulseClipLength;
        defaultFallbackPortalClipLength = fallbackPortalClipLength;
        defaultVisualSetCaptured = true;
    }

    private void HideCurrentVisualSet()
    {
        if (movementRenderers == null || inkPulseRenderers == null || portalRenderers == null)
        {
            ResolveRendererCaches();
        }

        SetRenderersVisible(movementRenderers, false);
        SetRenderersVisible(inkPulseRenderers, false);
        SetRenderersVisible(portalRenderers, false);
        SetAnimatorSpeed(movementAnimator, 0f);
        SetAnimatorSpeed(inkPulseAnimator, 0f);
        SetAnimatorSpeed(portalAnimator, 0f);
    }

    private static float ResolveClipLength(Animator animator, string clipName, float fallbackLength)
    {
        if (animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrWhiteSpace(clipName))
        {
            return Mathf.Max(0.01f, fallbackLength);
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
            {
                return Mathf.Max(0.01f, clip.length);
            }
        }

        return Mathf.Max(0.01f, fallbackLength);
    }

    private static float ResolveCurrentStateLength(Animator animator, float fallbackLength)
    {
        if (animator == null)
        {
            return Mathf.Max(0.01f, fallbackLength);
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length > 0f && !float.IsInfinity(stateInfo.length)
            ? stateInfo.length
            : Mathf.Max(0.01f, fallbackLength);
    }

    private static void SetRenderersVisible(Renderer[] renderers, bool visible)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer != null)
            {
                targetRenderer.enabled = visible;
            }
        }
    }

    private static void SetAnimatorSpeed(Animator animator, float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    private void SubscribeToState()
    {
        if (playerState == null)
        {
            return;
        }

        playerState.StateChanged -= HandlePlayerStateChanged;
        playerState.StateChanged += HandlePlayerStateChanged;
    }

    private void WarnIfMissingReferences()
    {
        if (warnedMissingReferences)
        {
            return;
        }

        if (playerState == null
            || inkPulse == null
            || movementVisualRoot == null
            || inkPulseVisualRoot == null
            || portalVisualRoot == null
            || movementRenderers.Length == 0
            || inkPulseRenderers.Length == 0
            || portalRenderers.Length == 0)
        {
            Debug.LogWarning(
                "[PlayerVisualStateController] Faltan referencias visuales. BabySquid debe tener SquidVisual, InkPulseVisual y PortalVisual cableados desde el prefab.",
                this);
            warnedMissingReferences = true;
        }
    }
}
