using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverMenuManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private GameSessionController session = null;
    [SerializeField] private SceneFlowController sceneFlow = null;

    [Header("UI References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    [Header("Animated Elements")]
    [SerializeField] private RectTransform[] animatedDecorations;
    [SerializeField] private RectTransform[] animatedButtons;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float zigzagOffset = 800f;
    [SerializeField] private float zigzagDuration = 0.35f;
    [SerializeField] private float zigzagDelay = 0.08f;

    private RectTransform[] animatedElements = new RectTransform[0];
    private Vector2[] originalPositions = new Vector2[0];

    private void Awake()
    {
        ResolveUiReferences();
        WireButtons();
        CacheAnimatedElementPositions();
        HideImmediate();
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        if (session == null)
        {
            Debug.LogError("[GameOverMenuManager] Falta asignar GameSessionController en el Inspector.", this);
            return;
        }

        session.StateChanged += HandleSessionStateChanged;
        ApplyState(session.CurrentState);
    }

    private void OnDisable()
    {
        if (session != null)
        {
            session.StateChanged -= HandleSessionStateChanged;
        }
    }

    public void Retry()
    {
        if (sceneFlow == null)
        {
            Debug.LogError("[GameOverMenuManager] Falta asignar SceneFlowController en el Inspector.", this);
            return;
        }

        sceneFlow.RestartRunFromPrimaryGameplayScene();
    }

    public void GoToMainMenu()
    {
        if (sceneFlow == null)
        {
            Debug.LogError("[GameOverMenuManager] Falta asignar SceneFlowController en el Inspector.", this);
            return;
        }

        sceneFlow.LoadMainMenu();
    }

    private void HandleSessionStateChanged(GameSessionState previousState, GameSessionState nextState)
    {
        ApplyState(nextState);
    }

    private void ApplyState(GameSessionState state)
    {
        if (state == GameSessionState.GameOver)
        {
            Show();
        }
        else
        {
            HideImmediate();
        }
    }

    private void Show()
    {
        StopAllCoroutines();
        SetMenuRootActive(true);

        if (canvasGroup == null)
        {
            return;
        }

        StartCoroutine(AnimateIn());
    }

    private void HideImmediate()
    {
        StopAllCoroutines();
        SetMenuRootActive(false);
        MenuScreenAnimation.ResetElements(animatedElements, originalPositions);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            SetCanvasInteractable(false);
        }
    }

    private IEnumerator AnimateIn()
    {
        canvasGroup.alpha = 0f;
        SetCanvasInteractable(false);

        MenuScreenAnimation.PlaceElementsAtOffset(animatedElements, originalPositions, zigzagOffset);
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 1f, fadeDuration);

        for (int i = 0; i < animatedElements.Length; i++)
        {
            if (animatedElements[i] == null)
            {
                continue;
            }

            StartCoroutine(MenuScreenAnimation.SlideElement(animatedElements[i], originalPositions[i], zigzagDuration));
            yield return new WaitForSecondsRealtime(zigzagDelay);
        }

        yield return new WaitForSecondsRealtime(zigzagDuration);
        MenuScreenAnimation.ResetElements(animatedElements, originalPositions);
        canvasGroup.alpha = 1f;
        SetCanvasInteractable(true);
    }

    private void WireButtons()
    {
        WireButton(retryButton, Retry);
        WireButton(menuButton, GoToMainMenu);
    }

    private void ResolveUiReferences()
    {
        Transform uiRoot = menuRoot != null ? menuRoot.transform : transform.Find("GameOverCanvas");
        if (menuRoot == null && uiRoot != null)
        {
            menuRoot = uiRoot.gameObject;
        }

        if (canvasGroup == null && menuRoot != null)
        {
            canvasGroup = menuRoot.GetComponent<CanvasGroup>();
        }

        retryButton ??= FindChildComponent<Button>(uiRoot, "BotonReintentar");
        menuButton ??= FindChildComponent<Button>(uiRoot, "BotonMenu");

        if (animatedDecorations == null || animatedDecorations.Length == 0)
        {
            animatedDecorations = FindChildRectTransforms(uiRoot, "GameOverDecoration");
        }

        if (animatedButtons == null || animatedButtons.Length == 0)
        {
            animatedButtons = FindChildRectTransforms(uiRoot, "BotonReintentar", "BotonMenu");
        }
    }

    private void CacheAnimatedElementPositions()
    {
        if (animatedDecorations == null)
        {
            animatedDecorations = new RectTransform[0];
        }

        if (animatedButtons == null)
        {
            animatedButtons = new RectTransform[0];
        }

        animatedElements = MenuScreenAnimation.BuildElementList(animatedDecorations, animatedButtons);
        originalPositions = MenuScreenAnimation.CachePositions(animatedElements);
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || sceneFlow == null || menuRoot == null || canvasGroup == null || retryButton == null || menuButton == null)
        {
            Debug.LogWarning(
                "[GameOverMenuManager] Faltan referencias. Asigna Session, SceneFlow, MenuRoot, CanvasGroup, RetryButton y MenuButton en este componente.",
                this);
        }

        if (menuRoot == gameObject)
        {
            Debug.LogError("[GameOverMenuManager] MenuRoot no debe ser el mismo GameObject del manager. Asigna el Canvas o panel visual hijo.", this);
        }
    }

    private void SetMenuRootActive(bool active)
    {
        if (menuRoot == null || menuRoot == gameObject)
        {
            return;
        }

        menuRoot.SetActive(active);
    }

    private void SetCanvasInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }

    private void WireButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        DisablePersistentOnClick(button);
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void DisablePersistentOnClick(Button button)
    {
        int persistentEventCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentEventCount; i++)
        {
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
    }

    private T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName && children[i].TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    private RectTransform[] FindChildRectTransforms(Transform root, params string[] childNames)
    {
        if (root == null || childNames == null || childNames.Length == 0)
        {
            return new RectTransform[0];
        }

        RectTransform[] results = new RectTransform[childNames.Length];
        int count = 0;
        for (int i = 0; i < childNames.Length; i++)
        {
            RectTransform rectTransform = FindChildComponent<RectTransform>(root, childNames[i]);
            if (rectTransform != null)
            {
                results[count] = rectTransform;
                count++;
            }
        }

        if (count == results.Length)
        {
            return results;
        }

        RectTransform[] compact = new RectTransform[count];
        for (int i = 0; i < count; i++)
        {
            compact[i] = results[i];
        }

        return compact;
    }
}
