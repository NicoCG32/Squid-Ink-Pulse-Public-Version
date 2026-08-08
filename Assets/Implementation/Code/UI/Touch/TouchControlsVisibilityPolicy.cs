using UnityEngine;

public static class TouchControlsVisibilityPolicy
{
    public static bool ShouldShow(
        RuntimePlatform platform,
        bool editorOverrideEnabled)
    {
        return platform switch
        {
            RuntimePlatform.Android => true,
            RuntimePlatform.WindowsEditor => editorOverrideEnabled,
            RuntimePlatform.OSXEditor => editorOverrideEnabled,
            RuntimePlatform.LinuxEditor => editorOverrideEnabled,
            _ => false
        };
    }
}
