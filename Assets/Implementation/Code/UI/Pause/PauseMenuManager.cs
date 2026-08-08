using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private GameSessionController session = null;
    [SerializeField] private SceneFlowController sceneFlow = null;

    [Header("UI References")]
    [SerializeField, FormerlySerializedAs("pauseMenuUI")] private GameObject menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private OptionsMenuManager optionsMenu;
    [SerializeField] private Button menuButton;

    [Header("Animated Elements")]
    [SerializeField] private RectTransform[] animatedDecorations;
    [SerializeField, FormerlySerializedAs("botones")] private RectTransform[] animatedButtons;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float zigzagOffset = 800f;
    [SerializeField] private float zigzagDuration = 0.35f;
    [SerializeField] private float zigzagDelay = 0.08f;

    // Autoridad: coordina entrada, sesion y navegacion; la politica de transicion y
    // la resolucion de jerarquia UI se delegan a clases sin estado de MonoBehaviour.
    private bool isPaused;
    private bool isAnimating;
    private RectTransform[] animatedElements = new RectTransform[0];
    private Vector2[] originalPositions = new Vector2[0];
    private GameplayCommandInputBinding pauseInputBinding;

    public bool CanTogglePauseNow => PauseMenuCommandPolicy.ResolveToggle(
        session != null ? (GameSessionState?)session.CurrentState : null,
        menuRoot != null,
        isAnimating) != PauseMenuCommandAction.None;

    private void Awake()
    {
        ResolveUiReferences();
        NormalizeRenderableScale();
        WireButtons();
        CacheAnimatedElementPositions();
        WarnIfMissingReferences();

        if (menuRoot != null)
        {
            SetVisible(false);
        }
    }

    private void OnEnable()
    {
        SquidInkPulseInputRuntime.GameplayChanged += HandleGameplayInputChanged;
        HandleGameplayInputChanged(SquidInkPulseInputRuntime.Gameplay);

        if (session == null)
        {
            Debug.LogError("[PauseMenuManager] Falta asignar GameSessionController en el Inspector.", this);
            return;
        }

        session.StateChanged += HandleSessionStateChanged;
        isPaused = session.IsPaused;
        NormalizeRenderableScale();
        SetVisible(isPaused);
    }

    private void Update()
    {
        if (pauseInputBinding?.TryConsumeRequest() ?? false)
        {
            TogglePause();
        }
    }

    private void OnDisable()
    {
        SquidInkPulseInputRuntime.GameplayChanged -= HandleGameplayInputChanged;
        HandleGameplayInputChanged(null);

        if (session != null)
        {
            session.StateChanged -= HandleSessionStateChanged;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        PauseMenuCommandAction action = PauseMenuCommandPolicy.ResolveToggle(
            session != null ? (GameSessionState?)session.CurrentState : null,
            menuRoot != null,
            isAnimating);

        if (action == PauseMenuCommandAction.RequestPause)
        {
            session.RequestPause();
        }
        else if (action == PauseMenuCommandAction.RequestResume)
        {
            Reanudar();
        }
    }

    private void HandleGameplayInputChanged(SquidInkPulseGameplayInputReader inputReader)
    {
        pauseInputBinding?.Dispose();
        pauseInputBinding = inputReader != null
            ? new GameplayCommandInputBinding(
                inputReader,
                SquidInkPulseGameplayCommand.TogglePause)
            : null;
    }

    public void Reanudar()
    {
        if (session == null || !isPaused || isAnimating)
        {
            return;
        }

        isPaused = false;
        StartCoroutine(AnimateOutThenResume());
    }

    public void Opciones()
    {
        ResolveUiReferences();

        if (optionsMenu == null)
        {
            Debug.LogError("[PauseMenuManager] Faltó asignar el OptionsMenuManager.");
            return;
        }

        SetVisible(false);
        optionsMenu.Open(OnOptionsClosed);
    }

    private void OnOptionsClosed()
    {
        SetVisible(true);
        StartCoroutine(AnimateIn());
    }

    public void IrAlMenu()
    {
        if (sceneFlow == null)
        {
            Debug.LogError("[PauseMenuManager] Falta asignar SceneFlowController en el Inspector.", this);
            return;
        }

        sceneFlow.LoadMainMenu();
    }

    private void HandleSessionStateChanged(GameSessionState previousState, GameSessionState nextState)
    {
        if (menuRoot == null)
        {
            return;
        }

        PauseMenuPresentationAction action = PauseMenuPresentationPolicy.ResolveSessionTransition(
            previousState,
            nextState,
            isPaused,
            isAnimating);

        switch (action)
        {
            case PauseMenuPresentationAction.HideImmediate:
                StopAllCoroutines();
                isAnimating = false;
                isPaused = false;
                SetVisible(false);
                break;
            case PauseMenuPresentationAction.ShowAnimated:
                isPaused = true;
                NormalizeRenderableScale();
                SetMenuRootActive(true);
                StartCoroutine(AnimateIn());
                break;
            case PauseMenuPresentationAction.HideAnimated:
                isPaused = false;
                StartCoroutine(AnimateOut());
                break;
        }
    }

    private IEnumerator AnimateIn()
    {
        isAnimating = true;
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
        SetCanvasAlpha(1f);
        SetCanvasInteractable(true);
        isAnimating = false;
    }

    private IEnumerator AnimateOut()
    {
        isAnimating = true;
        SetCanvasInteractable(false);

        for (int i = animatedElements.Length - 1; i >= 0; i--)
        {
            if (animatedElements[i] == null)
            {
                continue;
            }

            Vector2 target = MenuScreenAnimation.GetOffsetPosition(originalPositions[i], zigzagOffset, i);
            StartCoroutine(MenuScreenAnimation.SlideElement(animatedElements[i], target, zigzagDuration));
            yield return new WaitForSecondsRealtime(zigzagDelay);
        }

        yield return new WaitForSecondsRealtime(zigzagDuration);
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 0f, fadeDuration);

        SetVisible(false);
        MenuScreenAnimation.ResetElements(animatedElements, originalPositions);
        isAnimating = false;
    }

    private IEnumerator AnimateOutThenResume()
    {
        yield return AnimateOut();

        if (session != null && session.IsPaused)
        {
            session.RequestResume();
        }
    }

    private void WireButtons()
    {
        WireButton(resumeButton, Reanudar);
        WireButton(optionsButton, Opciones);
        WireButton(menuButton, IrAlMenu);
    }

    private void ResolveUiReferences()
    {
        Transform uiRoot = menuRoot != null ? menuRoot.transform : transform.Find("PauseCanvas");
        if (menuRoot == null && uiRoot != null)
        {
            menuRoot = uiRoot.gameObject;
        }

        if (canvasGroup == null && menuRoot != null)
        {
            canvasGroup = menuRoot.GetComponent<CanvasGroup>();
        }

        resumeButton ??= UiButtonContract.FindButton(uiRoot, "ReanudarBoton", "BotonReanudar");
        optionsButton ??= UiButtonContract.FindButton(uiRoot, "OpcionesBoton", "BotonOpciones");
        menuButton ??= UiButtonContract.FindButton(uiRoot, "MenuBoton", "BotonMenu");
        optionsMenu ??= FindInScene<OptionsMenuManager>();

        if (animatedDecorations == null || animatedDecorations.Length == 0)
        {
            animatedDecorations = MenuHierarchyResolver.FindChildRectTransforms(uiRoot, "PauseDecoration");
        }

        if (animatedButtons == null || animatedButtons.Length == 0)
        {
            animatedButtons = UiButtonContract.FindButtonRootRects(
                uiRoot,
                "ReanudarBoton",
                "OpcionesBoton",
                "MenuBoton");

            if (animatedButtons.Length == 0)
            {
                animatedButtons = MenuHierarchyResolver.FindChildRectTransforms(
                    uiRoot,
                    "BotonReanudar",
                    "BotonOpciones",
                    "BotonMenu");
            }
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

    private void SetVisible(bool visible)
    {
        if (visible)
        {
            NormalizeRenderableScale();
        }

        SetMenuRootActive(visible);

        SetCanvasAlpha(visible ? 1f : 0f);

        if (canvasGroup != null)
        {
            SetCanvasInteractable(visible);
        }
    }

    private void SetCanvasAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private void SetCanvasInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || sceneFlow == null || menuRoot == null || canvasGroup == null || resumeButton == null || optionsButton == null || optionsMenu == null || menuButton == null)
        {
            Debug.LogWarning(
                "[PauseMenuManager] Faltan referencias. Asigna Session, SceneFlow, MenuRoot, CanvasGroup, ResumeButton, OptionsButton, OptionsMenu y MenuButton en este componente.",
                this);
        }

        if (menuRoot == gameObject)
        {
            Debug.LogError("[PauseMenuManager] MenuRoot no debe ser el mismo GameObject del manager. Asigna el Canvas o panel visual hijo.", this);
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

    private void WireButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private T FindInScene<T>() where T : Component
    {
        UnityEngine.SceneManagement.Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(includeInactive: true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private void NormalizeRenderableScale()
    {
        Transform rootTransform = menuRoot != null ? menuRoot.transform : transform;
        Canvas ownerCanvas = rootTransform != null
            ? rootTransform.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);

        MenuHierarchyResolver.RestoreScaleIfCollapsed(ownerCanvas != null ? ownerCanvas.transform : null);
        MenuHierarchyResolver.RestoreScaleIfCollapsed(rootTransform);
    }
}
