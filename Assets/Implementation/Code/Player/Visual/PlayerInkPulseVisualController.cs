using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInkPulseVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private Animator inkPulseAnimator;
    [SerializeField] private SpriteRenderer inkPulseRenderer;
    [SerializeField] private GameObject standardVisualRoot;

    [Header("Animation")]
    [SerializeField] private string inkPulseStateName = "InkPulse";
    [SerializeField] private string inkPulseClipName = "InkPulse";
    [SerializeField, Min(0.01f)] private float fallbackInkPulseClipLength = 1f;

    private Renderer[] standardVisualRenderers;
    private bool[] standardVisualRendererEnabledStates;
    private float resolvedInkPulseClipLength;
    private bool standardVisualHidden;
    private bool warnedMissingReferences;

    private void Awake()
    {
        ResolveReferences();
        resolvedInkPulseClipLength = ResolveAnimationClipLength();
        HideVisual();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToInkPulse();
        ApplyCurrentState();
    }

    private void Start()
    {
        ResolveReferences();
        resolvedInkPulseClipLength = ResolveAnimationClipLength();
        SubscribeToInkPulse();
        ApplyCurrentState();
        WarnIfMissingReferences();
    }

    private void OnDisable()
    {
        if (inkPulse != null)
        {
            inkPulse.PulseStarted -= HandlePulseStarted;
            inkPulse.PulseEnded -= HandlePulseEnded;
            inkPulse.StateChanged -= HandleInkPulseStateChanged;
        }

        HideVisual();
    }

    private void HandlePulseStarted()
    {
        PlayInkPulseVisual();
    }

    private void HandlePulseEnded()
    {
        HideVisual();
    }

    private void HandleInkPulseStateChanged(InkPulseState previousState, InkPulseState nextState)
    {
        if (nextState == InkPulseState.Active)
        {
            PlayInkPulseVisual();
            return;
        }

        HideVisual();
    }

    private void ApplyCurrentState()
    {
        if (inkPulse != null && inkPulse.IsPulseActive)
        {
            PlayInkPulseVisual();
            return;
        }

        HideVisual();
    }

    private void PlayInkPulseVisual()
    {
        if (inkPulseAnimator == null || inkPulseRenderer == null || inkPulse == null)
        {
            return;
        }

        float pulseDuration = Mathf.Max(0.01f, inkPulse.PulseDuration);
        float remainingSeconds = Mathf.Clamp(inkPulse.PulseRemainingSeconds, 0f, pulseDuration);
        float elapsedRatio = 1f - (remainingSeconds / pulseDuration);

        inkPulseRenderer.enabled = true;
        HideStandardVisual();
        inkPulseAnimator.speed = Mathf.Max(0.01f, resolvedInkPulseClipLength) / pulseDuration;
        inkPulseAnimator.Play(inkPulseStateName, 0, Mathf.Clamp01(elapsedRatio));
        inkPulseAnimator.Update(0f);
    }

    private void HideVisual()
    {
        if (inkPulseRenderer != null)
        {
            inkPulseRenderer.enabled = false;
        }

        if (inkPulseAnimator != null)
        {
            inkPulseAnimator.speed = 1f;
            inkPulseAnimator.Play(inkPulseStateName, 0, 0f);
            inkPulseAnimator.Update(0f);
        }

        ShowStandardVisual();
    }

    private void ResolveReferences()
    {
        if (inkPulse == null)
        {
            inkPulse = GetComponentInParent<InkPulseController>();
        }

        if (inkPulseAnimator == null)
        {
            inkPulseAnimator = GetComponent<Animator>();
        }

        if (inkPulseRenderer == null)
        {
            inkPulseRenderer = GetComponent<SpriteRenderer>();
        }

        if (standardVisualRoot == null)
        {
            standardVisualRoot = ResolveStandardVisualRoot();
        }

        if ((standardVisualRenderers == null || standardVisualRenderers.Length == 0) && standardVisualRoot != null)
        {
            standardVisualRenderers = standardVisualRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        }
    }

    private GameObject ResolveStandardVisualRoot()
    {
        Transform playerRoot = transform.parent;
        if (playerRoot == null)
        {
            return null;
        }

        Transform standardVisual = playerRoot.Find("SquidVisual");
        return standardVisual != null ? standardVisual.gameObject : null;
    }

    private void HideStandardVisual()
    {
        if (standardVisualHidden || standardVisualRenderers == null || standardVisualRenderers.Length == 0)
        {
            return;
        }

        if (standardVisualRendererEnabledStates == null || standardVisualRendererEnabledStates.Length != standardVisualRenderers.Length)
        {
            standardVisualRendererEnabledStates = new bool[standardVisualRenderers.Length];
        }

        for (int i = 0; i < standardVisualRenderers.Length; i++)
        {
            Renderer rendererToHide = standardVisualRenderers[i];
            if (rendererToHide == null)
            {
                continue;
            }

            standardVisualRendererEnabledStates[i] = rendererToHide.enabled;
            rendererToHide.enabled = false;
        }

        standardVisualHidden = true;
    }

    private void ShowStandardVisual()
    {
        if (!standardVisualHidden || standardVisualRenderers == null || standardVisualRendererEnabledStates == null)
        {
            return;
        }

        int rendererCount = Mathf.Min(standardVisualRenderers.Length, standardVisualRendererEnabledStates.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            Renderer rendererToShow = standardVisualRenderers[i];
            if (rendererToShow != null)
            {
                rendererToShow.enabled = standardVisualRendererEnabledStates[i];
            }
        }

        standardVisualHidden = false;
    }

    private void SubscribeToInkPulse()
    {
        if (inkPulse == null)
        {
            return;
        }

        inkPulse.PulseStarted -= HandlePulseStarted;
        inkPulse.PulseEnded -= HandlePulseEnded;
        inkPulse.StateChanged -= HandleInkPulseStateChanged;

        inkPulse.PulseStarted += HandlePulseStarted;
        inkPulse.PulseEnded += HandlePulseEnded;
        inkPulse.StateChanged += HandleInkPulseStateChanged;
    }

    private float ResolveAnimationClipLength()
    {
        if (inkPulseAnimator == null
            || inkPulseAnimator.runtimeAnimatorController == null
            || string.IsNullOrWhiteSpace(inkPulseClipName))
        {
            return fallbackInkPulseClipLength;
        }

        AnimationClip[] clips = inkPulseAnimator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == inkPulseClipName)
            {
                return Mathf.Max(0.01f, clip.length);
            }
        }

        return fallbackInkPulseClipLength;
    }

    private void WarnIfMissingReferences()
    {
        if (warnedMissingReferences)
        {
            return;
        }

        if (inkPulse == null
            || inkPulseAnimator == null
            || inkPulseRenderer == null
            || standardVisualRoot == null
            || standardVisualRenderers == null
            || standardVisualRenderers.Length == 0)
        {
            Debug.LogWarning("[PlayerInkPulseVisualController] Faltan referencias. Asigna InkPulse, Animator, SpriteRenderer y SquidVisual en InkPulseVisual.", this);
            warnedMissingReferences = true;
        }
    }
}
