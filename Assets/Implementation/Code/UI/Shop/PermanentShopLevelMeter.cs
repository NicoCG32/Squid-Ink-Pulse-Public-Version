using System;

public enum PermanentShopLevelDropState
{
    Empty,
    Half,
    Full
}

public static class PermanentShopLevelMeter
{
    public static PermanentShopLevelDropState[] CalculateDropStates(
        int level,
        int maxLevel,
        int dropCount,
        int segmentsPerDrop)
    {
        int safeDropCount = Math.Max(0, dropCount);
        int safeSegmentsPerDrop = Math.Max(1, segmentsPerDrop);
        PermanentShopLevelDropState[] states = new PermanentShopLevelDropState[safeDropCount];

        if (safeDropCount == 0)
        {
            return states;
        }

        int totalSegments = safeDropCount * safeSegmentsPerDrop;
        int filledSegments = maxLevel > 0
            ? RoundToInt(Clamp01(level / (float)maxLevel) * totalSegments)
            : 0;

        for (int index = 0; index < states.Length; index++)
        {
            int dropSegments = Math.Min(
                Math.Max(filledSegments - index * safeSegmentsPerDrop, 0),
                safeSegmentsPerDrop);

            states[index] = dropSegments switch
            {
                0 => PermanentShopLevelDropState.Empty,
                1 => PermanentShopLevelDropState.Half,
                _ => PermanentShopLevelDropState.Full
            };
        }

        return states;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static int RoundToInt(float value)
    {
        return (int)Math.Floor(value + 0.5f);
    }
}
