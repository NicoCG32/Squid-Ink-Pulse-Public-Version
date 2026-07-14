using System;

public sealed class InGameShopTimeScaleHold
{
    private readonly Func<float> getTimeScale;
    private readonly Action<float> setTimeScale;
    private float previousTimeScale = 1f;

    public InGameShopTimeScaleHold(Func<float> getTimeScale, Action<float> setTimeScale)
    {
        this.getTimeScale = getTimeScale ?? throw new ArgumentNullException(nameof(getTimeScale));
        this.setTimeScale = setTimeScale ?? throw new ArgumentNullException(nameof(setTimeScale));
    }

    public bool IsHolding { get; private set; }

    public void Begin(bool shouldHold)
    {
        if (!shouldHold || IsHolding)
        {
            return;
        }

        previousTimeScale = getTimeScale();
        setTimeScale(0f);
        IsHolding = true;
    }

    public bool End(bool canRestore)
    {
        if (!IsHolding)
        {
            return false;
        }

        IsHolding = false;
        if (!canRestore)
        {
            return false;
        }

        setTimeScale(previousTimeScale);
        return true;
    }
}
