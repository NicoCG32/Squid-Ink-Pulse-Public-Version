public static class MainMenuPlatformPolicy
{
    public static bool CanExitFromMenu(bool isMobilePlatform) => !isMobilePlatform;
}
