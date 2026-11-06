using UnityEngine;

public static class FairModePlatformPolicy
{
    public static bool Supports(RuntimePlatform platform) => platform != RuntimePlatform.Android;

    public static bool IsEnabled(RuntimePlatform platform, bool disabledByArgument) =>
        Supports(platform) && !disabledByArgument;
}
