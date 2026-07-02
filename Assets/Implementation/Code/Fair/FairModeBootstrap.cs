using System.Collections;
using UnityEngine;

public static class FairModeBootstrap
{
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
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
        if (instance != null)
        {
            return;
        }

        GameObject probeObject = new("FairModeStartupProbe");
        instance = probeObject.AddComponent<FairModeStartupProbe>();
        DontDestroyOnLoad(probeObject);
    }

    private IEnumerator Start()
    {
        string serverUrl = FairModeSettings.ServerBaseUrl;
        FairApiClient client = new(serverUrl, FairModeSettings.StartupProbeTimeoutSeconds);
        bool serverAvailable = false;

        yield return client.CheckHealth(result =>
        {
            serverAvailable = result.Success && (result.Value == null || result.Value.ok);
        });

        if (!serverAvailable)
        {
            if (!FairModeSettings.HasExplicitServerArgument)
            {
                Destroy(gameObject);
                yield break;
            }

            Debug.LogWarning($"Fair mode opened from explicit server argument, but /health did not answer at {serverUrl}.");
        }

        FairParticipantSession.EnsureInstance();
        FairModeMenuManager.EnsureInstance().ShowInitialLogin(serverAvailable);
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
