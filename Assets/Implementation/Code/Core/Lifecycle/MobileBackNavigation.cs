using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MobileBackNavigation : MonoBehaviour
{
    private static MobileBackNavigation instance;
    private InputAction cancel;
    private bool cancelRequested;
    private MainMenu menu;
    private OptionsMenuManager options;
    private PauseMenuManager pause;
    private LoreComicPresenter comic;
    private bool permanentShopOpen;

    public static bool IsActive => instance != null && instance.isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallForMobilePlayer()
    {
        if (!Application.isMobilePlatform || IsActive) return;
        var root = new GameObject("[MobileBackNavigation]");
        DontDestroyOnLoad(root);
        root.AddComponent<MobileBackNavigation>();
    }

    private void OnEnable()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        // Own a separate Cancel action; the gameplay scope may disable its asset.
        InputAction source = InputSystem.actions?.FindAction("UI/Cancel", false);
        cancel = source != null ? source.Clone() : new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        cancel.performed += HandleCancel;
        cancel.Enable();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResolveContext();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (cancel != null)
        {
            cancel.performed -= HandleCancel;
            cancel.Dispose();
            cancel = null;
        }
        cancelRequested = false;
        if (instance == this) instance = null;
    }

    public static void DiscardPendingCancel()
    {
        if (instance != null) instance.cancelRequested = false;
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        if (!MobileApplicationLifecycle.IsSuspended) cancelRequested = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cancelRequested = false;
        ResolveContext();
    }

    private void ResolveContext()
    {
        menu = FindFirstObjectByType<MainMenu>();
        options = FindFirstObjectByType<OptionsMenuManager>(FindObjectsInactive.Include);
        pause = FindFirstObjectByType<PauseMenuManager>();
        comic = FindFirstObjectByType<LoreComicPresenter>();
        permanentShopOpen = FindFirstObjectByType<OutOfGameShopManager>() != null;
    }

    private void LateUpdate()
    {
        if (!cancelRequested) return;
        cancelRequested = false;
        if (!MobileApplicationLifecycle.CanAdvancePresentation) return;
        GameSessionController session = GameSessionController.Instance;
        MobileBackAction action = MobileBackNavigationPolicy.Resolve(
            (menu != null && menu.IsLoading) || (comic != null && comic.IsPresenting),
            options != null && options.IsOpen,
            menu != null && menu.IsTutorialOpen,
            InGameShopManager.IsShopOpen,
            session != null ? (GameSessionState?)session.CurrentState : null,
            pause != null && pause.CanTogglePauseNow,
            permanentShopOpen);

        switch (action)
        {
            case MobileBackAction.CloseOptions: options.Close(); break;
            case MobileBackAction.CloseTutorial: menu.CerrarTutorial(); break;
            case MobileBackAction.CloseRunShop: InGameShopManager.Instance.CloseShop(); break;
            case MobileBackAction.Pause:
            case MobileBackAction.Resume: pause.TogglePause(); break;
            case MobileBackAction.ReturnToMainMenu: menu?.VolverAlMenuPrincipal(); break;
        }
    }
}
