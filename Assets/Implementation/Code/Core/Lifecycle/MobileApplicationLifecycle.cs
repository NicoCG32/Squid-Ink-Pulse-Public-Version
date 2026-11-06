using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MobileApplicationLifecycle : MonoBehaviour
{
    private static MobileApplicationLifecycle instance;
    private bool applicationPaused;
    private bool applicationFocused = true;
    private bool resumeRequired;
    private bool ownsAudioPause;
    private bool previousAudioPause;
    private int foregroundFrame = -1;
    private GameSessionController session;
    private SquidInkPulseGameplayInputReader suspendedInput;
    private MainMenu menu;
    private PauseMenuManager pauseMenu;

    public static bool IsSuspended => instance != null && instance.Suspended;
    public static bool CanAdvancePresentation => !IsSuspended
        && (instance == null || Time.frameCount > instance.foregroundFrame);
    private bool Suspended => applicationPaused || !applicationFocused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallForMobilePlayer()
    {
        if (!Application.isMobilePlatform || instance != null) return;
        var root = new GameObject("[MobileApplicationLifecycle]");
        DontDestroyOnLoad(root);
        root.AddComponent<MobileApplicationLifecycle>();
    }

    private void OnEnable()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }
        instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SquidInkPulseInputRuntime.GameplayChanged += HandleGameplayChanged;
        BindScene();
    }

    private void OnDisable()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SquidInkPulseInputRuntime.GameplayChanged -= HandleGameplayChanged;
        BindSession(null);
        RestoreAudioAndInput();
        instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (Application.isMobilePlatform) HandleApplicationPause(paused);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (Application.isMobilePlatform) HandleApplicationFocus(focused);
    }

    public void HandleApplicationPause(bool paused)
    {
        bool wasSuspended = Suspended;
        applicationPaused = paused;
        ApplyInterruption(wasSuspended);
    }

    public void HandleApplicationFocus(bool focused)
    {
        bool wasSuspended = Suspended;
        applicationFocused = focused;
        ApplyInterruption(wasSuspended);
    }

    private void ApplyInterruption(bool wasSuspended)
    {
        if (wasSuspended == Suspended) return;
        if (Suspended)
        {
            resumeRequired |= (session != null && session.IsPlaying) || (menu != null && menu.IsLoading);
            previousAudioPause = AudioListener.pause;
            ownsAudioPause = true;
            AudioListener.pause = true;
            MobileBackNavigation.DiscardPendingCancel();
            HandleGameplayChanged(SquidInkPulseInputRuntime.Gameplay);
            ApplyPendingPause();
            pauseMenu?.InterruptForApplicationPause();
        }
        else
        {
            foregroundFrame = Time.frameCount;
            RestoreAudioAndInput();
            ApplyPendingPause();
        }
    }

    private void HandleGameplayChanged(SquidInkPulseGameplayInputReader reader)
    {
        suspendedInput = null;
        if (!Suspended || reader == null || !reader.IsEnabled) return;
        suspendedInput = reader;
        reader.Disable();
    }

    private void RestoreAudioAndInput()
    {
        if (ownsAudioPause)
        {
            AudioListener.pause = previousAudioPause;
            ownsAudioPause = false;
        }
        if (suspendedInput != null && ReferenceEquals(suspendedInput, SquidInkPulseInputRuntime.Gameplay))
            suspendedInput.Enable();
        suspendedInput = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => BindScene();

    private void BindScene()
    {
        menu = FindFirstObjectByType<MainMenu>();
        pauseMenu = FindFirstObjectByType<PauseMenuManager>();
        BindSession(GameSessionController.Instance);
        if (Suspended && session != null && session.IsPlaying) resumeRequired = true;
        if (!Suspended && session == null && (menu == null || !menu.IsLoading)) resumeRequired = false;
        ApplyPendingPause();
        if (Suspended) pauseMenu?.InterruptForApplicationPause();
    }

    private void BindSession(GameSessionController next)
    {
        if (session == next) return;
        if (session != null) session.StateChanged -= HandleSessionChanged;
        session = next;
        if (session != null) session.StateChanged += HandleSessionChanged;
    }

    private void HandleSessionChanged(GameSessionState previous, GameSessionState next)
    {
        if (previous == GameSessionState.Paused && next == GameSessionState.Playing && !Suspended)
            resumeRequired = false;
    }

    private void LateUpdate() => ApplyPendingPause();

    private void ApplyPendingPause()
    {
        if (!resumeRequired || session == null || !session.IsPlaying || Time.timeScale <= 0f) return;
        // A comic/shop already owns a time hold. Let it finish before showing pause.
        session.RequestPause();
    }

    public static bool PauseBeforeGameplayTimeRestored()
    {
        if (instance == null || !instance.resumeRequired) return false;
        GameSessionController current = GameSessionController.Instance;
        if (current == null) return false;
        instance.BindSession(current);
        if (current.IsPlaying) current.RequestPause();
        return !current.IsPlaying;
    }
}
