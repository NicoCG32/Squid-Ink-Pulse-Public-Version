using System;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerRuntimeState
{
    Moving,
    InkPulse,
    Death
}

[DisallowMultipleComponent]
public class PlayerStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private PlayerMovement movement;

    [Header("Events")]
    public UnityEvent<PlayerRuntimeState> onStateChanged = new UnityEvent<PlayerRuntimeState>();

    public PlayerRuntimeState CurrentState { get; private set; } = PlayerRuntimeState.Moving;
    public event Action<PlayerRuntimeState, PlayerRuntimeState> StateChanged;

    private void Awake()
    {
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        if (inkPulse != null)
        {
            inkPulse.PulseStarted += HandlePulseStarted;
            inkPulse.PulseEnded += HandlePulseEnded;
        }

        if (session != null)
        {
            session.StateChanged += HandleSessionStateChanged;
        }

        ApplyState(ResolveCurrentState(), force: true);
    }

    private void OnDisable()
    {
        if (inkPulse != null)
        {
            inkPulse.PulseStarted -= HandlePulseStarted;
            inkPulse.PulseEnded -= HandlePulseEnded;
        }

        if (session != null)
        {
            session.StateChanged -= HandleSessionStateChanged;
        }

        movement?.SetInkPulseActive(false);
    }

    private void HandlePulseStarted()
    {
        if (session != null && session.IsPlaying)
        {
            ApplyState(PlayerRuntimeState.InkPulse);
        }
    }

    private void HandlePulseEnded()
    {
        ApplyState(ResolveCurrentState());
    }

    private void HandleSessionStateChanged(GameSessionState previousState, GameSessionState nextState)
    {
        if (nextState == GameSessionState.Paused)
        {
            return;
        }

        ApplyState(ResolveCurrentState());
    }

    private PlayerRuntimeState ResolveCurrentState()
    {
        if (session != null && session.IsGameOver)
        {
            return PlayerRuntimeState.Death;
        }

        if (session != null && !session.IsPlaying)
        {
            return CurrentState;
        }

        return inkPulse != null && inkPulse.IsPulseActive
            ? PlayerRuntimeState.InkPulse
            : PlayerRuntimeState.Moving;
    }

    private void ApplyState(PlayerRuntimeState nextState, bool force = false)
    {
        PlayerRuntimeState previousState = CurrentState;
        if (!force && previousState == nextState)
        {
            return;
        }

        CurrentState = nextState;
        movement?.SetInkPulseActive(nextState == PlayerRuntimeState.InkPulse);

        StateChanged?.Invoke(previousState, nextState);
        onStateChanged?.Invoke(nextState);
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || inkPulse == null || movement == null)
        {
            Debug.LogWarning("[PlayerStateController] Faltan referencias. Asigna Session, InkPulse y Movement en el Inspector.", this);
        }
    }
}
