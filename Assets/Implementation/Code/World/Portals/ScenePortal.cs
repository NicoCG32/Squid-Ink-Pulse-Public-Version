using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private SceneFlowController sceneFlow;

    private bool isTransitioning;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning || !other.CompareTag(GameplayTagCatalog.Player))
        {
            return;
        }

        if (sceneFlow == null)
        {
            Debug.LogError("[ScenePortal] No encontro SceneFlowController; no puede resolver destino.", this);
            return;
        }

        isTransitioning = true;
        SetCollidersEnabled(false);
        sceneFlow.LoadPortalDestinationFromActiveScene();
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
