using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FairParticipantSession : MonoBehaviour
{
    private static FairParticipantSession instance;

    private FairApiClient apiClient;
    private Coroutine heartbeatRoutine;
    private bool checkoutInProgress;

    public static FairParticipantSession Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public static bool HasActiveSession => instance != null && instance.IsActive;

    public bool IsActive => !string.IsNullOrWhiteSpace(ParticipantId);
    public string ParticipantId { get; private set; }
    public string Nickname { get; private set; }
    public string RecoveryCode { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        apiClient = new FairApiClient(FairModeSettings.ServerBaseUrl, FairModeSettings.RequestTimeoutSeconds);
    }

    public static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject sessionObject = new("FairParticipantSession");
        sessionObject.AddComponent<FairParticipantSession>();
    }

    public IEnumerator CreateNewParticipant(string nickname, Action<FairApiResult<FairParticipantResponse>> onCompleted)
    {
        RefreshClient();
        yield return apiClient.CreateParticipant(nickname, result =>
        {
            if (result.Success)
            {
                ActivateFromResponse(result.Value);
            }

            onCompleted?.Invoke(result);
        });

        if (IsActive)
        {
            yield return SyncCurrentSnapshot(null);
        }
    }

    public IEnumerator RecoverParticipant(string nickname, string recoveryCode, Action<FairApiResult<FairParticipantResponse>> onCompleted)
    {
        RefreshClient();
        yield return apiClient.RecoverParticipant(nickname, recoveryCode, result =>
        {
            if (result.Success)
            {
                ActivateFromResponse(result.Value);
            }

            onCompleted?.Invoke(result);
        });

        if (IsActive)
        {
            yield return SyncCurrentSnapshot(null);
        }
    }

    public IEnumerator SyncCurrentSnapshot(Action<FairApiResult<FairSnapshotResponse>> onCompleted)
    {
        if (!IsActive)
        {
            onCompleted?.Invoke(FairApiResult<FairSnapshotResponse>.Fail("no_active_session", "No hay sesion de feria activa.", 0));
            yield break;
        }

        FairProfileSnapshot snapshot = FairProfileMapper.CreateSnapshotFromLocalProfile(Nickname);
        yield return apiClient.SyncSnapshot(ParticipantId, snapshot, result =>
        {
            if (result.Success && result.Value?.profileSnapshot != null)
            {
                FairProfileMapper.ApplySnapshotToLocalProfile(result.Value.profileSnapshot);
            }

            onCompleted?.Invoke(result);
        });
    }

    public static bool TryCheckoutAndQuit(MonoBehaviour runner)
    {
        if (!HasActiveSession || runner == null)
        {
            return false;
        }

        Instance.StartCoroutine(Instance.CheckoutThenQuit());
        return true;
    }

    private IEnumerator CheckoutThenQuit()
    {
        if (checkoutInProgress)
        {
            yield break;
        }

        checkoutInProgress = true;
        yield return CheckoutCurrentSession(_ => { });
        QuitApplication();
    }

    public IEnumerator CheckoutCurrentSession(Action<FairApiResult<FairCheckoutResponse>> onCompleted)
    {
        if (!IsActive)
        {
            onCompleted?.Invoke(FairApiResult<FairCheckoutResponse>.Fail("no_active_session", "No hay sesion de feria activa.", 0));
            yield break;
        }

        StopHeartbeat();
        FairProfileSnapshot finalSnapshot = FairProfileMapper.CreateSnapshotFromLocalProfile(Nickname);
        yield return apiClient.Checkout(ParticipantId, finalSnapshot, result =>
        {
            if (result.Success)
            {
                ParticipantId = null;
                Nickname = null;
                RecoveryCode = null;
            }

            onCompleted?.Invoke(result);
        });
    }

    private void ActivateFromResponse(FairParticipantResponse response)
    {
        if (response == null)
        {
            return;
        }

        ParticipantId = response.participantId;
        Nickname = response.nickname;
        RecoveryCode = response.recoveryCode;

        if (response.profileSnapshot != null)
        {
            FairProfileMapper.ApplySnapshotToLocalProfile(response.profileSnapshot);
        }

        StartHeartbeat();
    }

    private void StartHeartbeat()
    {
        RefreshClient();
        StopHeartbeat();
        heartbeatRoutine = StartCoroutine(HeartbeatLoop());
    }

    private void RefreshClient()
    {
        apiClient = new FairApiClient(FairModeSettings.ServerBaseUrl, FairModeSettings.RequestTimeoutSeconds);
    }

    private void StopHeartbeat()
    {
        if (heartbeatRoutine != null)
        {
            StopCoroutine(heartbeatRoutine);
            heartbeatRoutine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        WaitForSecondsRealtime wait = new(FairModeSettings.HeartbeatIntervalSeconds);
        while (IsActive)
        {
            yield return wait;
            yield return apiClient.Heartbeat(ParticipantId, _ => { });
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            StopHeartbeat();
            instance = null;
        }
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
