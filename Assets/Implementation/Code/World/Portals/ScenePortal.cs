using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour, IOffscreenCleanupEligibility
{
    [Header("Scene Flow")]
    [SerializeField] private SceneFlowController sceneFlow;
    [SerializeField, Min(0f)] private float fallbackTransitionDelay = 0.75f;

    private bool isTransitioning;
    private Action<Collider2D> localTransitionHandler;
    public bool CanBeCleanedUpOffscreen => !isTransitioning;

    private void Awake()
    {
        gameObject.tag = GameplayTagCatalog.Portal;

        if (TryGetComponent(out Collider2D portalCollider))
        {
            portalCollider.isTrigger = true;
        }

        ResolveReferences();
        WarnIfMissingReferences();
    }

    public void ConfigureLocalTransition(Action<Collider2D> transitionHandler)
    {
        localTransitionHandler = transitionHandler;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning || !other.CompareTag(GameplayTagCatalog.Player))
        {
            return;
        }

        if (localTransitionHandler != null)
        {
            isTransitioning = true;
            SetCollidersEnabled(false);
            localTransitionHandler.Invoke(other);
            return;
        }

        if (sceneFlow == null)
        {
            Debug.LogError("[ScenePortal] No encontro SceneFlowController; no puede resolver destino.", this);
            return;
        }

        StartCoroutine(RunPortalTransition(other));
    }

    private IEnumerator RunPortalTransition(Collider2D playerCollider)
    {
        isTransitioning = true;
        SetCollidersEnabled(false);

        PlayerStateController playerState = playerCollider.GetComponentInParent<PlayerStateController>();
        PlayerVisualStateController playerVisual = playerCollider.GetComponentInParent<PlayerVisualStateController>();
        RunProgressionDirector progression = RunProgressionDirector.HasInstance
            ? RunProgressionDirector.Instance
            : null;

        playerState?.BeginPortalTransition();
        progression?.TryBeginTransition();

        float transitionDelay = playerVisual != null
            ? playerVisual.PortalTransitionDuration
            : fallbackTransitionDelay;

        if (transitionDelay > 0f)
        {
            yield return new WaitForSeconds(transitionDelay);
        }

        string targetScene = sceneFlow.TryResolvePortalDestinationFromActiveScene(out string resolvedTargetScene)
            ? resolvedTargetScene
            : null;

        if (!string.IsNullOrWhiteSpace(targetScene))
        {
            yield return LoreComicPresenter.PlayPortalTransitionIfAvailable(targetScene);
        }

        if (sceneFlow.TryLoadPortalDestinationFromActiveScene())
        {
            PersistentPlayerProfile.RecordPortalCrossed();
            yield break;
        }

        playerState?.CompletePortalTransition();
        progression?.CompleteTransition();
        SetCollidersEnabled(true);
        isTransitioning = false;
    }

    private void ResolveReferences()
    {
        if (sceneFlow != null)
        {
            return;
        }

        sceneFlow = FindFirstObjectByType<SceneFlowController>();
    }

    private void SetCollidersEnabled(bool enabled)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = enabled;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (sceneFlow == null)
        {
            Debug.LogWarning("[ScenePortal] No encontro SceneFlowController. Los destinos se configuran en SceneFlowController.", this);
        }
    }
}
