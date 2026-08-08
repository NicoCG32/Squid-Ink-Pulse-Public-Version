using UnityEngine;

public enum PlayerVerticalMovementSource
{
    None,
    PlayerTarget,
    ExternalImpulse
}

public readonly struct PlayerVerticalImpulseState
{
    public PlayerVerticalImpulseState(float velocity, float remainingSeconds)
    {
        Velocity = velocity;
        RemainingSeconds = remainingSeconds;
    }

    public float Velocity { get; }
    public float RemainingSeconds { get; }
    public bool IsActive => Velocity > 0f && RemainingSeconds > 0f;
}

public readonly struct PlayerVerticalMovementStep
{
    public PlayerVerticalMovementStep(
        PlayerVerticalMovementSource source,
        float nextY,
        PlayerVerticalImpulseState externalImpulse)
    {
        Source = source;
        NextY = nextY;
        ExternalImpulse = externalImpulse;
    }

    public PlayerVerticalMovementSource Source { get; }
    public float NextY { get; }
    public PlayerVerticalImpulseState ExternalImpulse { get; }
    public bool HasMovement => Source != PlayerVerticalMovementSource.None;
}

public static class PlayerVerticalMovementPolicy
{
    public static PlayerVerticalImpulseState ApplyImpulse(
        PlayerVerticalImpulseState current,
        float requestedVelocity,
        float requestedDurationSeconds)
    {
        float safeVelocity = Mathf.Max(0f, requestedVelocity);
        float safeDuration = Mathf.Max(0f, requestedDurationSeconds);
        if (safeVelocity <= 0f || safeDuration <= 0f)
        {
            return current;
        }

        return new PlayerVerticalImpulseState(
            Mathf.Max(current.Velocity, safeVelocity),
            Mathf.Max(current.RemainingSeconds, safeDuration));
    }

    public static PlayerVerticalMovementStep Resolve(
        float currentY,
        bool hasPlayerTarget,
        float playerTargetY,
        float playerVerticalSpeed,
        PlayerVerticalImpulseState externalImpulse,
        float deltaTime)
    {
        if (externalImpulse.IsActive)
        {
            float appliedDuration = Mathf.Min(deltaTime, externalImpulse.RemainingSeconds);
            float nextY = currentY + externalImpulse.Velocity * appliedDuration;
            float remainingSeconds = externalImpulse.RemainingSeconds - deltaTime;
            float velocity = externalImpulse.Velocity;

            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                velocity = 0f;
            }

            return new PlayerVerticalMovementStep(
                PlayerVerticalMovementSource.ExternalImpulse,
                nextY,
                new PlayerVerticalImpulseState(velocity, remainingSeconds));
        }

        if (!hasPlayerTarget)
        {
            return new PlayerVerticalMovementStep(
                PlayerVerticalMovementSource.None,
                currentY,
                default);
        }

        return new PlayerVerticalMovementStep(
            PlayerVerticalMovementSource.PlayerTarget,
            Mathf.MoveTowards(currentY, playerTargetY, playerVerticalSpeed * deltaTime),
            default);
    }
}
