using System.Collections;
using UnityEngine;

public static class FairModeBootstrap
{
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!FairModeSettings.SupportsCurrentPlatform)
        {
            Debug.Log("[FairMode] Android local play: network bootstrap disabled.");
            return;
        }
        if (initialized || !FairModeSettings.IsEnabled)
        {
            return;
        }

        initialized = true;
        FairModeStartupProbe.EnsureInstance();
    }
}

[DisallowMultipleComponent]
public sealed class FairModeStartupProbe : MonoBehaviour
{
    private static FairModeStartupProbe instance;

    public static void EnsureInstance()
    {
        if (!FairModeSettings.IsEnabled || instance != null)
        {
            return;
        }

        GameObject probeObject = new("FairModeStartupProbe");
        instance = probeObject.AddComponent<FairModeStartupProbe>();
        DontDestroyOnLoad(probeObject);
    }

    private IEnumerator Start()
    {
        if (!FairModeSettings.IsEnabled)
        {
            Destroy(gameObject);
            yield break;
        }
        string serverUrl = FairModeSettings.ServerBaseUrl;
        FairApiClient client = new(serverUrl, FairModeSettings.StartupProbeTimeoutSeconds);
        bool serverAvailable = false;

        yield return client.CheckHealth(result =>
        {
            serverAvailable = result.Success && (result.Value == null || result.Value.ok);
        }, logFailure: false);

        if (!serverAvailable)
        {
            Debug.Log($"Fair mode skipped because no active server was found at {serverUrl}/health.");
            Destroy(gameObject);
            yield break;
        }

        FairParticipantSession.EnsureInstance();
        FairModeMenuManager.EnsureInstance().ShowInitialLogin();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
