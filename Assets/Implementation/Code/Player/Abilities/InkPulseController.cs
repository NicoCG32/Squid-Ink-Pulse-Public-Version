using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private bool activationSuppressed;

    public float ChargeRate => Mathf.Max(0f, chargeRate + PermanentUpgradeEffectResolver.InkPulseRechargeRateBonus);
    public float PulseDuration => pulseDuration * PermanentUpgradeEffectResolver.InkPulseDurationMultiplier;
    public float PulseRemainingSeconds => IsPulseActive ? Mathf.Max(0f, pulseTimer) : 0f;
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
        ResolveReferences();
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
        ResolveReferences();
        RestoreRuntimeStateFromStore();
        WarnIfMissingReferences();
        UpdateChargeBar();
        ApplyState(ResolveState(), force: true);
        PersistRuntimeState();
    }

    private void Update()
    {
        ResolveReferences();

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

            if (IsGameplayActive() && !activationSuppressed && !InGameShopManager.IsShopOpen && !IsPulseActive && !IsCharged)
            {
                if (chargeBar != null)
                {
                    chargeBar.TriggerErrorFeedback();
                }
            }
            return false;
        }

        StartPulse();
        return true;
    }

    public bool TryForceReady()
    {
        if (!IsGameplayActive() || activationSuppressed || IsPulseActive || IsCharged)
        {
            return false;
        }

        currentCharge = maxCharge;
        UpdateChargeBar();
        ApplyState(ResolveState());
        PersistRuntimeState();
        return CurrentState == InkPulseState.Ready;
    }

    public void SetActivationSuppressed(bool suppressed)
    {
        activationSuppressed = suppressed;
    }

    private void HandleActivationInput()
    {
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (mousePressed || spacePressed)
        {
            TryActivatePulse();
        }
    }

    private bool CanActivatePulse()
    {
        return IsGameplayActive()
            && !activationSuppressed
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
        pulseTimer = PulseDuration;
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

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (chargeBar == null)
        {
            chargeBar = FindFirstObjectByType<ChargeBar>();
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || chargeBar == null)
        {
            Debug.LogWarning("[InkPulseController] Faltan referencias. Asigna Session y ChargeBar en el Inspector.", this);
        }
    }
}
