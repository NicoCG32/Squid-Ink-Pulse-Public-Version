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

    public float ChargeRate => chargeRate;
    public float CurrentCharge => currentCharge;
    public float ChargeRatio => maxCharge > 0f ? currentCharge / maxCharge : 0f;
    public bool IsCharged => currentCharge >= maxCharge;
    public bool IsPulseActive { get; private set; }

    public event Action PulseStarted;
    public event Action PulseEnded;
    public event Action<float> ChargeChanged;

    private void Start()
    {
        WarnIfMissingReferences();
        UpdateChargeBar();
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
    }

    public bool TryActivatePulse()
    {
        if (IsPulseActive || !IsCharged)
        {
            return false;
        }

        StartPulse();
        return true;
    }

    private void HandleActivationInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryActivatePulse();
        }
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
        }
    }

    private void StartPulse()
    {
        IsPulseActive = true;
        pulseTimer = pulseDuration;
        currentCharge = 0f;
        UpdateChargeBar();
        PulseStarted?.Invoke();
    }

    private void EndPulse()
    {
        IsPulseActive = false;
        currentCharge = 0f;
        UpdateChargeBar();
        PulseEnded?.Invoke();
    }

    private void UpdateChargeBar()
    {
        float ratio = ChargeRatio;
        chargeBar?.UpdateBar(ratio);
        ChargeChanged?.Invoke(ratio);
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
