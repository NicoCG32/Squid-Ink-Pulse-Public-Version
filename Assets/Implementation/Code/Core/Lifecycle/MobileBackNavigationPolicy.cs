public enum MobileBackAction
{
    None,
    CloseOptions,
    CloseTutorial,
    CloseRunShop,
    Pause,
    Resume,
    ReturnToMainMenu
}

public static class MobileBackNavigationPolicy
{
    public static MobileBackAction Resolve(
        bool transitionActive,
        bool optionsOpen,
        bool tutorialOpen,
        bool runShopOpen,
        GameSessionState? sessionState,
        bool pauseAvailable,
        bool permanentShopOpen)
    {
        if (transitionActive) return MobileBackAction.None;
        if (optionsOpen) return MobileBackAction.CloseOptions;
        if (tutorialOpen) return MobileBackAction.CloseTutorial;
        if (runShopOpen) return MobileBackAction.CloseRunShop;
        if (sessionState.HasValue)
        {
            if (!pauseAvailable) return MobileBackAction.None;
            if (sessionState == GameSessionState.Playing) return MobileBackAction.Pause;
            if (sessionState == GameSessionState.Paused) return MobileBackAction.Resume;
            return MobileBackAction.None;
        }

        return permanentShopOpen ? MobileBackAction.ReturnToMainMenu : MobileBackAction.None;
    }
}
