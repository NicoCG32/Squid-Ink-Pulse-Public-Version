public enum PauseMenuPresentationAction
{
    None,
    ShowAnimated,
    HideAnimated,
    HideImmediate
}

public static class PauseMenuPresentationPolicy
{
    public static PauseMenuPresentationAction ResolveSessionTransition(
        GameSessionState previousState,
        GameSessionState nextState,
        bool isMenuPaused,
        bool isAnimating)
    {
        if (nextState == GameSessionState.GameOver)
        {
            return PauseMenuPresentationAction.HideImmediate;
        }

        if (isAnimating)
        {
            return PauseMenuPresentationAction.None;
        }

        if (nextState == GameSessionState.Paused && !isMenuPaused)
        {
            return PauseMenuPresentationAction.ShowAnimated;
        }

        if (previousState == GameSessionState.Paused
            && nextState == GameSessionState.Playing
            && isMenuPaused)
        {
            return PauseMenuPresentationAction.HideAnimated;
        }

        return PauseMenuPresentationAction.None;
    }
}
