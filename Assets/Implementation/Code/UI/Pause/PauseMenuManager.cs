using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
    [SerializeField] private Button menuButton;
    [SerializeField] private Button exitButton;

    [Header("Animated Elements")]
    [SerializeField] private RectTransform[] animatedDecorations;
    [SerializeField, FormerlySerializedAs("botones")] private RectTransform[] animatedButtons;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float zigzagOffset = 800f;
    [SerializeField] private float zigzagDuration = 0.35f;
    [SerializeField] private float zigzagDelay = 0.08f;

    private bool isPaused;
    private bool isAnimating;
    private RectTransform[] animatedElements = new RectTransform[0];
    private Vector2[] originalPositions = new Vector2[0];

    private void Awake()
    {
        ResolveUiReferences();
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
        if (session == null)
        {
            Debug.LogError("[PauseMenuManager] Falta asignar GameSessionController en el Inspector.", this);
            return;
        }

        session.StateChanged += HandleSessionStateChanged;
        isPaused = session.IsPaused;
        SetVisible(isPaused);
    }

    private void Update()
    {
        if (Keyboard.current == null || isAnimating)
        {
            return;
        }

        if (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnDisable()
    {
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
        if (session == null || menuRoot == null || isAnimating)
        {
            return;
        }

        if (session.IsPlaying)
        {
            session.RequestPause();
        }
        else if (session.IsPaused)
        {
            Reanudar();
        }
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
        Debug.Log("Opciones: aun no implementado");
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

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleSessionStateChanged(GameSessionState previousState, GameSessionState nextState)
    {
        if (menuRoot == null)
        {
            return;
        }

        if (nextState == GameSessionState.GameOver)
        {
            StopAllCoroutines();
            isAnimating = false;
            isPaused = false;
            SetVisible(false);
            return;
        }

        if (isAnimating)
        {
            return;
        }

        if (nextState == GameSessionState.Paused && !isPaused)
        {
            isPaused = true;
            SetMenuRootActive(true);
            StartCoroutine(AnimateIn());
            return;
        }

        if (previousState == GameSessionState.Paused && nextState == GameSessionState.Playing && isPaused)
        {
            isPaused = false;
            StartCoroutine(AnimateOut());
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
        WireButton(exitButton, Salir);
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

        resumeButton ??= FindChildComponent<Button>(uiRoot, "BotonReanudar");
        optionsButton ??= FindChildComponent<Button>(uiRoot, "BotonOpciones");
        menuButton ??= FindChildComponent<Button>(uiRoot, "BotonMenu");
        exitButton ??= FindChildComponent<Button>(uiRoot, "BotonSalir");

        if (animatedDecorations == null || animatedDecorations.Length == 0)
        {
            animatedDecorations = FindChildRectTransforms(uiRoot, "PauseDecoration");
        }

        if (animatedButtons == null || animatedButtons.Length == 0)
        {
            animatedButtons = FindChildRectTransforms(
                uiRoot,
                "BotonReanudar",
                "BotonOpciones",
                "BotonMenu",
                "BotonSalir");
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
        if (session == null || sceneFlow == null || menuRoot == null || canvasGroup == null || resumeButton == null || optionsButton == null || menuButton == null || exitButton == null)
        {
            Debug.LogWarning(
                "[PauseMenuManager] Faltan referencias. Asigna Session, SceneFlow, MenuRoot, CanvasGroup, ResumeButton, OptionsButton, MenuButton y ExitButton en este componente.",
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
