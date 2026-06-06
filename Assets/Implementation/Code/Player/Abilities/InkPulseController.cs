using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InkPulseState
{
    Idle,
    Charging,
    Ready,
    Active
}

[DisallowMultipleComponent]
public class InkPulseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;

    [Header("Charge")]
    [SerializeField] private float chargeRate = 150f;
    [SerializeField] private float maxCharge = 100f;
    [SerializeField] private float currentCharge;
    [SerializeField] private ChargeBar chargeBar;

    [Header("Pulse")]
    [SerializeField] private float pulseDuration = 3f;

    private float pulseTimer;
    private bool runtimeStateRestored;

    public float ChargeRate => chargeRate;
    public float CurrentCharge => currentCharge;
    public float ChargeRatio => maxCharge > 0f ? currentCharge / maxCharge : 0f;
    public bool IsCharged => currentCharge >= maxCharge;
    public bool IsPulseActive { get; private set; }
    public InkPulseState CurrentState { get; private set; } = InkPulseState.Idle;
    public InkPulseState CurrentChargeState => CurrentState;

    public event Action PulseStarted;
    public event Action PulseEnded;
    public event Action<float> ChargeChanged;
    public event Action<InkPulseState, InkPulseState> StateChanged;
    public event Action<InkPulseState, InkPulseState> ChargeStateChanged;

    private void Awake()
    {
        RestoreRuntimeStateFromStore();
    }

    private void OnEnable()
    {
        RuntimeInkPulseState.Changed += HandleRuntimeInkPulseChanged;
    }

    private void OnDisable()
    {
        RuntimeInkPulseState.Changed -= HandleRuntimeInkPulseChanged;
    }

    private void Start()
    {
        RestoreRuntimeStateFromStore();
        WarnIfMissingReferences();
        UpdateChargeBar();
        ApplyState(ResolveState(), force: true);
        PersistRuntimeState();
    }

    private void Update()
    {
        if (!IsGameplayActive())
        {
            return;
        }

        HandleActivationInput();
        UpdatePulseTimer();
    }

    public void AddGrazeCharge(float amount)
    {
        if (!IsGameplayActive() || IsPulseActive)
        {
            return;
        }

        currentCharge = Mathf.Clamp(currentCharge + amount, 0f, maxCharge);
        UpdateChargeBar();
        ApplyState(ResolveState());
        PersistRuntimeState();
    }

    public bool TryActivatePulse()
    {
        if (!CanActivatePulse())
        {
            return false;
        }

        StartPulse();
        return true;
    }

    public bool TryForceReady()
    {
        if (!IsGameplayActive() || IsPulseActive || IsCharged)
        {
            return false;
        }

        currentCharge = maxCharge;
        UpdateChargeBar();
        ApplyState(ResolveState());
        PersistRuntimeState();
        return CurrentState == InkPulseState.Ready;
    }

    private void HandleActivationInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryActivatePulse();
        }
    }

    private bool CanActivatePulse()
    {
        return IsGameplayActive()
            && !InGameShopManager.IsShopOpen
            && !IsPulseActive
            && IsCharged;
    }

    private void UpdatePulseTimer()
    {
        if (!IsPulseActive)
        {
            return;
        }

        pulseTimer -= Time.deltaTime;
        if (pulseTimer <= 0f)
        {
            EndPulse();
            return;
        }

        PersistRuntimeState();
    }

    private void StartPulse()
    {
        IsPulseActive = true;
        pulseTimer = pulseDuration;
        currentCharge = 0f;
        UpdateChargeBar();
        ApplyState(ResolveState());
        PersistRuntimeState();
        PulseStarted?.Invoke();
    }

    private void EndPulse()
    {
        IsPulseActive = false;
        currentCharge = 0f;
        UpdateChargeBar();
        ApplyState(ResolveState());
        PersistRuntimeState();
        PulseEnded?.Invoke();
    }

    private void RestoreRuntimeState()
    {
        currentCharge = Mathf.Clamp(RuntimeInkPulseState.CurrentCharge, 0f, maxCharge);
        IsPulseActive = RuntimeInkPulseState.IsPulseActive && RuntimeInkPulseState.PulseRemainingSeconds > 0f;
        pulseTimer = IsPulseActive ? RuntimeInkPulseState.PulseRemainingSeconds : 0f;
    }

    private void RestoreRuntimeStateFromStore()
    {
        if (runtimeStateRestored)
        {
            return;
        }

        RuntimeInkPulseState.InitializeIfNeeded(currentCharge, IsPulseActive, pulseTimer);
        RestoreRuntimeState();
        runtimeStateRestored = true;
    }

    private void PersistRuntimeState()
    {
        RuntimeInkPulseState.Save(currentCharge, IsPulseActive, pulseTimer);
    }

    private void HandleRuntimeInkPulseChanged()
    {
        RestoreRuntimeState();
        UpdateChargeBar();
        ApplyState(ResolveState());
    }

    private void UpdateChargeBar()
    {
        float ratio = ChargeRatio;
        chargeBar?.UpdateBar(ratio);
        ChargeChanged?.Invoke(ratio);
    }

    private InkPulseState ResolveState()
    {
        if (IsPulseActive)
        {
            return InkPulseState.Active;
        }

        if (currentCharge <= 0f)
        {
            return InkPulseState.Idle;
        }

        return IsCharged
            ? InkPulseState.Ready
            : InkPulseState.Charging;
    }

    private void ApplyState(InkPulseState nextState, bool force = false)
    {
        InkPulseState previousState = CurrentState;
        if (!force && previousState == nextState)
        {
            return;
        }

        CurrentState = nextState;
        StateChanged?.Invoke(previousState, nextState);
        ChargeStateChanged?.Invoke(previousState, nextState);
    }

    private bool IsGameplayActive()
    {
        return session != null && session.IsPlaying;
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || chargeBar == null)
        {
            Debug.LogWarning("[InkPulseController] Faltan referencias. Asigna Session y ChargeBar en el Inspector.", this);
        }
    }
}
