using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialPresentationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialDirector director;
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Header("Presentation")]
    [SerializeField] private bool freezeGameplay = true;
    [SerializeField] private bool suppressInkPulse = true;
    [SerializeField] private bool darkenDuringPresentation = true;
    [SerializeField, Range(0f, 1f)] private float presentationAlpha = 0.35f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.12f;

    private bool presentationActive;
    private bool freezeActive;
    private float timeScaleBeforePresentation = 1f;
    private float currentAlpha;
    private float targetAlpha;

    public bool IsPresenting => presentationActive;

    private void Awake()
    {
        ResolveReferences();
        ApplyOverlayState(immediate: true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        SyncWithDirector();
    }

    private void Update()
    {
        ApplyOverlayState(immediate: false);
    }

    private void OnDisable()
    {
        Unsubscribe();
        EndPresentation(immediate: true);
    }

    public void BeginPresentation(TutorialStep step)
    {
        presentationActive = true;
        targetAlpha = darkenDuringPresentation ? presentationAlpha : 0f;

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
        }

        if (freezeGameplay && !freezeActive)
        {
            timeScaleBeforePresentation = Time.timeScale;
            Time.timeScale = 0f;
            freezeActive = true;
        }

        if (suppressInkPulse && inkPulse != null)
        {
            inkPulse.SetActivationSuppressed(true);
        }

        ApplyOverlayState(immediate: fadeSeconds <= 0f);
    }

    public void EndPresentation()
    {
        EndPresentation(immediate: false);
    }

    private void EndPresentation(bool immediate)
    {
        presentationActive = false;
        targetAlpha = 0f;

        if (freezeActive)
        {
            freezeActive = false;
            if (session == null || session.IsPlaying)
            {
                Time.timeScale = timeScaleBeforePresentation;
            }
        }

        if (suppressInkPulse && inkPulse != null)
        {
            inkPulse.SetActivationSuppressed(false);
        }

        ApplyOverlayState(immediate: immediate || fadeSeconds <= 0f);
    }

    private void Subscribe()
    {
        if (director != null)
        {
            director.PhaseStarted += HandlePhaseStarted;
        }
    }

    private void Unsubscribe()
    {
        if (director != null)
        {
            director.PhaseStarted -= HandlePhaseStarted;
        }
    }

    private void HandlePhaseStarted(TutorialStep step, TutorialPhase phase)
    {
        if (phase == TutorialPhase.Presentation)
        {
            BeginPresentation(step);
            return;
        }

        EndPresentation();
    }

    private void SyncWithDirector()
    {
        if (director != null && director.CurrentPhase == TutorialPhase.Presentation)
        {
            BeginPresentation(director.CurrentStep);
            return;
        }

        EndPresentation();
    }

    private void ApplyOverlayState(bool immediate)
    {
        if (overlayCanvasGroup == null)
        {
            return;
        }

        if (immediate || fadeSeconds <= 0f)
        {
            currentAlpha = targetAlpha;
        }
        else
        {
            currentAlpha = Mathf.MoveTowards(
                currentAlpha,
                targetAlpha,
                Time.unscaledDeltaTime / fadeSeconds);
        }

        overlayCanvasGroup.alpha = currentAlpha;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        if (overlayRoot != null && !presentationActive && Mathf.Approximately(currentAlpha, 0f))
        {
            overlayRoot.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (director == null)
        {
            director = GetComponent<TutorialDirector>();
        }

        if (session == null)
        {
            session = GetComponent<GameSessionController>();
        }

        if (inkPulse == null && director != null)
        {
            inkPulse = director.InkPulse;
        }

        if (overlayCanvasGroup == null && overlayRoot != null)
        {
            overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        }
    }
}
