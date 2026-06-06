using System;
using UnityEngine;

public static class RuntimeInkPulseState
{
    private static bool initialized;
    private static float currentCharge;
    private static bool isPulseActive;
    private static float pulseRemainingSeconds;

    public static bool IsInitialized => initialized;
    public static float CurrentCharge => currentCharge;
    public static bool IsPulseActive => isPulseActive;
    public static float PulseRemainingSeconds => pulseRemainingSeconds;

    public static event Action Changed;

    public static void InitializeIfNeeded(float initialCharge, bool initialPulseActive, float initialPulseRemainingSeconds)
    {
        if (initialized)
        {
            return;
        }

        Save(initialCharge, initialPulseActive, initialPulseRemainingSeconds);
    }

    public static void Save(float charge, bool pulseActive, float pulseRemaining)
    {
        initialized = true;
        currentCharge = Mathf.Max(0f, charge);
        isPulseActive = pulseActive;
        pulseRemainingSeconds = Mathf.Max(0f, pulseRemaining);
    }

    public static void ResetForRuntime()
    {
        initialized = false;
        currentCharge = 0f;
        isPulseActive = false;
        pulseRemainingSeconds = 0f;
        Changed?.Invoke();
    }
}
