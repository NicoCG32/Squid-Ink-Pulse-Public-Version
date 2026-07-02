using System;

[Serializable]
public class FairCreateParticipantRequest
{
    public string nickname;
    public string machineId;
    public string buildVersion;
}

[Serializable]
public class FairRecoverParticipantRequest
{
    public string nickname;
    public string recoveryCode;
    public string machineId;
    public string buildVersion;
}

[Serializable]
public class FairMachineRequest
{
    public string machineId;
}

[Serializable]
public class FairSnapshotRequest
{
    public string machineId;
    public string buildVersion;
    public FairProfileSnapshot snapshot;
}

[Serializable]
public class FairCheckoutRequest
{
    public string machineId;
    public FairProfileSnapshot finalSnapshot;
}

[Serializable]
public class FairParticipantResponse
{
    public string participantId;
    public string nickname;
    public string recoveryCode;
    public FairProfileSnapshot profileSnapshot;
    public long bestScore;
    public int attemptCount;
    public string activeSessionMachineId;
    public string activeSessionExpiresAt;
}

[Serializable]
public class FairSnapshotResponse
{
    public bool accepted;
    public int rank;
    public long bestScore;
    public int leaderboardCount;
    public FairProfileSnapshot profileSnapshot;
}

[Serializable]
public class FairCheckoutResponse
{
    public bool accepted;
    public int rank;
    public int leaderboardCount;
    public long bestScore;
}

[Serializable]
public class FairErrorResponse
{
    public bool ok;
    public string error;
    public string message;
}

[Serializable]
public class FairHealthResponse
{
    public bool ok;
    public string eventId;
}

[Serializable]
public class FairProfileSnapshot
{
    public int version = 1;
    public string nickname;
    public PlayerRecordsSaveData records = PlayerRecordsSaveData.CreateDefault();
    public FairProfileSnapshotProfile profile = FairProfileSnapshotProfile.CreateDefault();
    public string[] unlockedEvents = Array.Empty<string>();
    public string updatedAt;
}

[Serializable]
public class FairProfileSnapshotProfile
{
    public PlayerProfilePermanentUpgradesSaveData permanentUpgrades = new();
    public PlayerProfileSkinsSaveData skins = PlayerProfileSkinsSaveData.CreateDefault();
    public PlayerProfileRunGadgetUnlocksSaveData runGadgetUnlocks = PlayerProfileRunGadgetUnlocksSaveData.CreateDefault();

    public static FairProfileSnapshotProfile CreateDefault()
    {
        return new FairProfileSnapshotProfile
        {
            permanentUpgrades = new PlayerProfilePermanentUpgradesSaveData(),
            skins = PlayerProfileSkinsSaveData.CreateDefault(),
            runGadgetUnlocks = PlayerProfileRunGadgetUnlocksSaveData.CreateDefault()
        };
    }
}

public readonly struct FairApiResult<T>
{
    public FairApiResult(bool success, T value, string errorCode, string message, long responseCode)
    {
        Success = success;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
        ResponseCode = responseCode;
    }

    public bool Success { get; }
    public T Value { get; }
    public string ErrorCode { get; }
    public string Message { get; }
    public long ResponseCode { get; }

    public static FairApiResult<T> Ok(T value, long responseCode)
    {
        return new FairApiResult<T>(true, value, string.Empty, string.Empty, responseCode);
    }

    public static FairApiResult<T> Fail(string errorCode, string message, long responseCode)
    {
        return new FairApiResult<T>(false, default, errorCode ?? string.Empty, message ?? string.Empty, responseCode);
    }
}
