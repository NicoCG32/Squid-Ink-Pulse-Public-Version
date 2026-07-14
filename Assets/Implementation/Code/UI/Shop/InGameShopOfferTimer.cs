using UnityEngine;

public sealed class InGameShopOfferTimer
{
    public const float MinimumDurationSeconds = 0.5f;

    public float RemainingSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    public void Start(float durationSeconds)
    {
        RemainingSeconds = Mathf.Max(MinimumDurationSeconds, durationSeconds);
        IsRunning = true;
    }

    public bool Tick(float deltaSeconds)
    {
        if (!IsRunning)
        {
            return false;
        }

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaSeconds));
        return RemainingSeconds <= 0f;
    }

    public void Stop()
    {
        RemainingSeconds = 0f;
        IsRunning = false;
    }
}
