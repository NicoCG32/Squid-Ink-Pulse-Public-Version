using UnityEngine;

public static class RuntimePlayerPace
{
    private static float elapsedSpeedSeconds;

    public static float ElapsedSpeedSeconds => elapsedSpeedSeconds;

    public static void Advance(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        elapsedSpeedSeconds = Mathf.Max(0f, elapsedSpeedSeconds + deltaTime);
    }

    public static void ResetForRuntime()
    {
        elapsedSpeedSeconds = 0f;
    }
}
