using System;
using UnityEngine;

public static class AndroidFramePacing
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Application.platform != RuntimePlatform.Android) return;

        // Keep the same player for a controlled 30/60 comparison. Nothing is persisted.
        string[] args = Environment.GetCommandLineArgs();
        bool benchmark30 = Array.IndexOf(args, "--sip-perf-30fps") >= 0;
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android supplies Unity launch arguments through the activity's "unity" extra.
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity?.Call<AndroidJavaObject>("getIntent"))
            {
                string launchArgs = intent?.Call<string>("getStringExtra", "unity");
                if (!string.IsNullOrEmpty(launchArgs))
                    benchmark30 |= Array.IndexOf(launchArgs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), "--sip-perf-30fps") >= 0;
            }
        }
        catch (AndroidJavaException) { Debug.LogWarning("[AndroidFramePacing] Optional benchmark arguments unavailable; using normal target."); }
#endif
        Application.targetFrameRate = benchmark30 ? 30 : 60;
        Debug.Log($"[AndroidFramePacing] Target={Application.targetFrameRate}; Development={Debug.isDebugBuild}; Build={Application.buildGUID}; Quality={QualitySettings.GetQualityLevel()}");
    }
}
