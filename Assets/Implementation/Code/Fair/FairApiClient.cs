using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class FairApiClient
{
    private readonly string baseUrl;
    private readonly int timeoutSeconds;

    public FairApiClient(string baseUrl, float timeoutSeconds)
    {
        this.baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:8080" : baseUrl.TrimEnd('/');
        this.timeoutSeconds = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
    }

    public IEnumerator CheckHealth(Action<FairApiResult<FairHealthResponse>> onCompleted)
    {
        using UnityWebRequest webRequest = UnityWebRequest.Get($"{baseUrl}/health");
        webRequest.timeout = timeoutSeconds;
        webRequest.SetRequestHeader("Accept", "application/json");

        yield return webRequest.SendWebRequest();

        string text = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : string.Empty;
        bool success = webRequest.result == UnityWebRequest.Result.Success
            && webRequest.responseCode >= 200
            && webRequest.responseCode < 300;

        if (success)
        {
            FairHealthResponse response = string.IsNullOrWhiteSpace(text)
                ? new FairHealthResponse { ok = true }
                : JsonUtility.FromJson<FairHealthResponse>(text);
            onCompleted?.Invoke(FairApiResult<FairHealthResponse>.Ok(response, webRequest.responseCode));
            yield break;
        }

        FairErrorResponse error = TryParseError(text);
        string errorCode = !string.IsNullOrWhiteSpace(error?.error)
            ? error.error
            : webRequest.result.ToString();
        string message = !string.IsNullOrWhiteSpace(error?.message)
            ? error.message
            : webRequest.error;
        onCompleted?.Invoke(FairApiResult<FairHealthResponse>.Fail(errorCode, message, webRequest.responseCode));
    }

    public IEnumerator CreateParticipant(
        string nickname,
        Action<FairApiResult<FairParticipantResponse>> onCompleted)
    {
        FairCreateParticipantRequest request = new()
        {
            nickname = nickname,
            machineId = FairModeSettings.MachineId,
            buildVersion = FairModeSettings.BuildVersion
        };
        yield return SendJson("POST", "/participants", request, onCompleted);
    }

    public IEnumerator RecoverParticipant(
        string nickname,
        string recoveryCode,
        Action<FairApiResult<FairParticipantResponse>> onCompleted)
    {
        FairRecoverParticipantRequest request = new()
        {
            nickname = nickname,
            recoveryCode = recoveryCode,
            machineId = FairModeSettings.MachineId,
            buildVersion = FairModeSettings.BuildVersion
        };
        yield return SendJson("POST", "/participants/recover", request, onCompleted);
    }

    public IEnumerator SyncSnapshot(
        string participantId,
        FairProfileSnapshot snapshot,
        Action<FairApiResult<FairSnapshotResponse>> onCompleted)
    {
        FairSnapshotRequest request = new()
        {
            machineId = FairModeSettings.MachineId,
            buildVersion = FairModeSettings.BuildVersion,
            snapshot = snapshot
        };
        yield return SendJson("PUT", $"/participants/{participantId}/snapshot", request, onCompleted);
    }

    public IEnumerator Heartbeat(
        string participantId,
        Action<FairApiResult<FairSnapshotResponse>> onCompleted)
    {
        FairMachineRequest request = new()
        {
            machineId = FairModeSettings.MachineId
        };
        yield return SendJson("POST", $"/participants/{participantId}/heartbeat", request, onCompleted);
    }

    public IEnumerator Checkout(
        string participantId,
        FairProfileSnapshot finalSnapshot,
        Action<FairApiResult<FairCheckoutResponse>> onCompleted)
    {
        FairCheckoutRequest request = new()
        {
            machineId = FairModeSettings.MachineId,
            finalSnapshot = finalSnapshot
        };
        yield return SendJson("POST", $"/participants/{participantId}/checkout", request, onCompleted);
    }

    private IEnumerator SendJson<TRequest, TResponse>(
        string method,
        string path,
        TRequest request,
        Action<FairApiResult<TResponse>> onCompleted)
    {
        string json = JsonUtility.ToJson(request);
        byte[] body = Encoding.UTF8.GetBytes(json);
        using UnityWebRequest webRequest = new($"{baseUrl}{path}", method)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = timeoutSeconds
        };
        webRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        webRequest.SetRequestHeader("Accept", "application/json");

        yield return webRequest.SendWebRequest();

        string text = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : string.Empty;
        bool success = webRequest.result == UnityWebRequest.Result.Success
            && webRequest.responseCode >= 200
            && webRequest.responseCode < 300;

        if (success)
        {
            TResponse response = string.IsNullOrWhiteSpace(text)
                ? default
                : JsonUtility.FromJson<TResponse>(text);
            onCompleted?.Invoke(FairApiResult<TResponse>.Ok(response, webRequest.responseCode));
            yield break;
        }

        FairErrorResponse error = TryParseError(text);
        string errorCode = !string.IsNullOrWhiteSpace(error?.error)
            ? error.error
            : webRequest.result.ToString();
        string message = !string.IsNullOrWhiteSpace(error?.message)
            ? error.message
            : webRequest.error;
        onCompleted?.Invoke(FairApiResult<TResponse>.Fail(errorCode, message, webRequest.responseCode));
    }

    private static FairErrorResponse TryParseError(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<FairErrorResponse>(text);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
