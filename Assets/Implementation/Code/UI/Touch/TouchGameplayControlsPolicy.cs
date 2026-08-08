public enum TouchGameplayControlState
{
    Blocked,
    Ready,
    Charging,
    Active,
    Empty,
    Passive,
    Pause,
    Resume
}

public readonly struct TouchGameplayControlPresentation
{
    public TouchGameplayControlPresentation(bool interactable, TouchGameplayControlState state)
    {
        Interactable = interactable;
        State = state;
    }

    public bool Interactable { get; }
    public TouchGameplayControlState State { get; }
}

public static class TouchGameplayControlsPolicy
{
    public static TouchGameplayControlPresentation ResolveInkPulse(
        GameSessionState sessionState,
        bool timeAdvancing,
        bool commandChannelAvailable,
        bool shopBlocking,
        bool activationSuppressed,
        bool isPulseActive,
        bool isCharged)
    {
        if (sessionState != GameSessionState.Playing
            || !timeAdvancing
            || !commandChannelAvailable
            || shopBlocking
            || activationSuppressed)
        {
            return new TouchGameplayControlPresentation(false, TouchGameplayControlState.Blocked);
        }

        if (isPulseActive)
        {
            return new TouchGameplayControlPresentation(false, TouchGameplayControlState.Active);
        }

        return isCharged
            ? new TouchGameplayControlPresentation(true, TouchGameplayControlState.Ready)
            : new TouchGameplayControlPresentation(true, TouchGameplayControlState.Charging);
    }

    public static TouchGameplayControlPresentation ResolvePause(
        GameSessionState sessionState,
        bool commandChannelAvailable,
        bool authorityCanToggle)
    {
        if (!commandChannelAvailable || !authorityCanToggle)
        {
            return new TouchGameplayControlPresentation(false, TouchGameplayControlState.Blocked);
        }

        return sessionState switch
        {
            GameSessionState.Playing => new TouchGameplayControlPresentation(
                true,
                TouchGameplayControlState.Pause),
            GameSessionState.Paused => new TouchGameplayControlPresentation(
                true,
                TouchGameplayControlState.Resume),
            _ => new TouchGameplayControlPresentation(
                false,
                TouchGameplayControlState.Blocked)
        };
    }

    public static TouchGameplayControlPresentation ResolveGadget(
        GameSessionState sessionState,
        bool timeAdvancing,
        bool commandChannelAvailable,
        bool shopBlocking,
        GadgetId gadget,
        bool isOwned,
        bool effectAvailable)
    {
        if (gadget == GadgetId.None || !isOwned)
        {
            return new TouchGameplayControlPresentation(false, TouchGameplayControlState.Empty);
        }

        if (GadgetCatalog.IsPassive(gadget))
        {
            return new TouchGameplayControlPresentation(false, TouchGameplayControlState.Passive);
        }

        bool canUse = sessionState == GameSessionState.Playing
            && timeAdvancing
            && commandChannelAvailable
            && !shopBlocking
            && GadgetCatalog.IsActive(gadget)
            && effectAvailable;
        return new TouchGameplayControlPresentation(
            canUse,
            canUse ? TouchGameplayControlState.Ready : TouchGameplayControlState.Blocked);
    }
}
