public enum PauseMenuCommandAction
{
    None,
    RequestPause,
    RequestResume
}

public static class PauseMenuCommandPolicy
{
    public static PauseMenuCommandAction ResolveToggle(
        GameSessionState? sessionState,
        bool hasMenuRoot,
        bool isAnimating)
    {
        if (!sessionState.HasValue || !hasMenuRoot || isAnimating)
        {
            return PauseMenuCommandAction.None;
        }

        return sessionState.Value switch
        {
            GameSessionState.Playing => PauseMenuCommandAction.RequestPause,
            GameSessionState.Paused => PauseMenuCommandAction.RequestResume,
            _ => PauseMenuCommandAction.None
        };
    }
}
