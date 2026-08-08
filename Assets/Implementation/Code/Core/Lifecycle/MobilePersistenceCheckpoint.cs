using System;
using UnityEngine;

public static class PlayerPreferencesCheckpoint
{
    private static bool hasPendingChanges;

    public static bool HasPendingChanges => hasPendingChanges;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasPendingChanges = false;
    }

    public static void MarkPending()
    {
        hasPendingChanges = true;
    }

    public static void CommitChanges()
    {
        MarkPending();
        FlushIfPending();
    }

    public static bool FlushIfPending()
    {
        return FlushIfPending(PlayerPrefs.Save);
    }

    public static bool FlushIfPending(Action savePreferences)
    {
        if (!hasPendingChanges)
        {
            return false;
        }

        if (savePreferences == null)
        {
            throw new ArgumentNullException(nameof(savePreferences));
        }

        savePreferences();
        hasPendingChanges = false;
        return true;
    }
}

[DisallowMultipleComponent]
public sealed class MobilePersistenceCheckpoint : MonoBehaviour
{
    private const string RuntimeObjectName = "[MobilePersistenceCheckpoint]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallForMobilePlayer()
    {
        if (!Application.isMobilePlatform
            || FindAnyObjectByType<MobilePersistenceCheckpoint>() != null)
        {
            return;
        }

        var runtimeObject = new GameObject(RuntimeObjectName)
        {
            hideFlags = HideFlags.HideInHierarchy
        };

        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<MobilePersistenceCheckpoint>();
    }

    private void OnApplicationPause(bool isPaused)
    {
        CheckpointForPause(isPaused);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        CheckpointForFocus(hasFocus);
    }

    public static bool CheckpointForPause(bool isPaused)
    {
        return isPaused && PlayerPreferencesCheckpoint.FlushIfPending();
    }

    public static bool CheckpointForFocus(bool hasFocus)
    {
        return !hasFocus && PlayerPreferencesCheckpoint.FlushIfPending();
    }
}
