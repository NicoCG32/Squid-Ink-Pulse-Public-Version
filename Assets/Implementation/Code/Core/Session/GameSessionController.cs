using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    private static GameSessionController instance;

    [Header("State")]
    [SerializeField] private GameSessionState initialState = GameSessionState.Playing;
    [SerializeField] private bool pauseTimeOnGameOver = true;

    [Header("Events")]
    public UnityEvent<GameSessionState> onStateChanged = new UnityEvent<GameSessionState>();

    public static GameSessionController Instance
    {
        get => instance;
    }

    public static bool HasInstance => instance != null;

    public static bool IsGameplayActive
    {
        get
        {
            return instance != null && instance.CurrentState == GameSessionState.Playing;
        }
    }

    public GameSessionState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState == GameSessionState.Playing;
    public bool IsPaused => CurrentState == GameSessionState.Paused;
    public bool IsGameOver => CurrentState == GameSessionState.GameOver;

    public event Action<GameSessionState, GameSessionState> StateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ApplyState(initialState, force: true);
    }

    public void RequestPlaying()
    {
        ApplyState(GameSessionState.Playing);
    }

    public void RequestPause()
    {
        if (CurrentState == GameSessionState.Playing)
        {
            ApplyState(GameSessionState.Paused);
        }
    }

    public void RequestResume()
    {
        if (CurrentState == GameSessionState.Paused)
        {
            ApplyState(GameSessionState.Playing);
        }
    }

    public void RequestGameOver()
    {
        if (CurrentState != GameSessionState.GameOver)
        {
            ApplyState(GameSessionState.GameOver);
        }
    }

    private void ApplyState(GameSessionState nextState, bool force = false)
    {
        GameSessionState previousState = CurrentState;
        if (!force && previousState == nextState)
        {
            return;
        }

        CurrentState = nextState;
        ApplyTimeScale(nextState);
        ResetRuntimeStateForGameOver(nextState);

        StateChanged?.Invoke(previousState, nextState);
        onStateChanged?.Invoke(nextState);
    }

    private void ResetRuntimeStateForGameOver(GameSessionState state)
    {
        if (state != GameSessionState.GameOver)
        {
            return;
        }

        long completedScore = RuntimeRunScore.CaptureCompletedScore();
        PersistentPlayerProfile.RecordRunEnded(completedScore);
        RuntimeGadgetInventory.ResetForRuntime();
        RuntimeInkPulseState.ResetForRuntime();
        RuntimeRunScore.ResetForRuntime();
        RuntimePlayerPace.ResetForRuntime();
        RuntimeInGameShopLoreState.ResetForRuntime();
    }

    private void ApplyTimeScale(GameSessionState state)
    {
        switch (state)
        {
            case GameSessionState.Playing:
                Time.timeScale = 1f;
                break;
            case GameSessionState.Paused:
                Time.timeScale = 0f;
                break;
            case GameSessionState.GameOver:
                Time.timeScale = pauseTimeOnGameOver ? 0f : 1f;
                break;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Time.timeScale = 1f;
        }
    }
}
