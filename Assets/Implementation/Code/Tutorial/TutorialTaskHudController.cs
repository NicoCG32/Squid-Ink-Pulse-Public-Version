using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialTaskHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialDirector director;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Step Views")]
    [SerializeField] private bool hideWhenStepHasNoView = true;
    [SerializeField] private TutorialTaskHudStepView[] stepViews = Array.Empty<TutorialTaskHudStepView>();

    private TutorialDirector subscribedDirector;

    private void Awake()
    {
        ResolveReferences();
        HideAllViews();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        SyncWithDirector();
    }

    private void OnDisable()
    {
        Unsubscribe();
        HideAllViews();
    }

    private void Subscribe()
    {
        if (subscribedDirector == director)
        {
            return;
        }

        Unsubscribe();
        subscribedDirector = director;

        if (subscribedDirector != null)
        {
            subscribedDirector.PhaseStarted += HandlePhaseStarted;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedDirector != null)
        {
            subscribedDirector.PhaseStarted -= HandlePhaseStarted;
            subscribedDirector = null;
        }
    }

    private void HandlePhaseStarted(TutorialStep step, TutorialPhase phase)
    {
        if (phase == TutorialPhase.Presentation)
        {
            ShowStep(step);
            return;
        }

        HideAllViews();
    }

    private void SyncWithDirector()
    {
        if (director != null && director.CurrentPhase == TutorialPhase.Presentation)
        {
            ShowStep(director.CurrentStep);
            return;
        }

        HideAllViews();
    }

    private void ShowStep(TutorialStep step)
    {
        TutorialTaskHudStepView view = FindView(step);
        if (view == null || view.Root == null)
        {
            if (hideWhenStepHasNoView)
            {
                HideAllViews();
            }

            return;
        }

        SetHudVisible(true);

        for (int i = 0; i < stepViews.Length; i++)
        {
            TutorialTaskHudStepView candidate = stepViews[i];
            if (candidate?.Root != null)
            {
                candidate.Root.SetActive(candidate == view);
            }
        }

        view.RestartAnimationIfNeeded();
    }

    private TutorialTaskHudStepView FindView(TutorialStep step)
    {
        if (stepViews == null)
        {
            return null;
        }

        for (int i = 0; i < stepViews.Length; i++)
        {
            TutorialTaskHudStepView view = stepViews[i];
            if (view != null && view.Step == step)
            {
                return view;
            }
        }

        return null;
    }

    private void HideAllViews()
    {
        SetHudVisible(false);

        if (stepViews == null)
        {
            return;
        }

        for (int i = 0; i < stepViews.Length; i++)
        {
            if (stepViews[i]?.Root != null)
            {
                stepViews[i].Root.SetActive(false);
            }
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (hudRoot != null && hudRoot != gameObject)
        {
            hudRoot.SetActive(visible);
        }

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = visible ? 1f : 0f;
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
        }
    }

    private void ResolveReferences()
    {
        if (director == null)
        {
            director = FindFirstObjectByType<TutorialDirector>();
        }

        hudRoot ??= gameObject;
        hudCanvasGroup ??= GetComponent<CanvasGroup>();
    }
}

[Serializable]
public sealed class TutorialTaskHudStepView
{
    [SerializeField] private TutorialStep step = TutorialStep.Movement;
    [SerializeField] private GameObject root;
    [SerializeField] private Animator animator;
    [SerializeField] private bool restartAnimatorOnShow = true;
    [SerializeField] private string animationStateName;

    public TutorialStep Step => step;
    public GameObject Root => root;

    public void RestartAnimationIfNeeded()
    {
        if (!restartAnimatorOnShow || root == null)
        {
            return;
        }

        Animator targetAnimator = animator != null ? animator : root.GetComponent<Animator>();
        if (targetAnimator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(animationStateName))
        {
            targetAnimator.Play(animationStateName, 0, 0f);
            targetAnimator.Update(0f);
            return;
        }

        targetAnimator.Rebind();
        targetAnimator.Update(0f);
    }
}
