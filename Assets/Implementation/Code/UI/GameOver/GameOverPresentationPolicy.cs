public enum GameOverPresentationAction
{
    None,
    PlayDefeatComicThenShow,
    HideImmediate
}

public static class GameOverPresentationPolicy
{
    public static GameOverPresentationAction ResolveSessionState(
        GameSessionState state,
        bool menuAlreadyShownForState,
        bool presentationRoutineRunning)
    {
        if (state != GameSessionState.GameOver)
        {
            return GameOverPresentationAction.HideImmediate;
        }

        if (menuAlreadyShownForState || presentationRoutineRunning)
        {
            return GameOverPresentationAction.None;
        }

        return GameOverPresentationAction.PlayDefeatComicThenShow;
    }
}
