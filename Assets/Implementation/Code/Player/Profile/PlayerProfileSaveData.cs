using System;
using System.Linq;

[Serializable]
public class PlayerProfileSaveData
{
    public int version = PlayerProfileRepository.CurrentVersion;
    public PlayerProfileWalletSaveData wallet = new();
    public PlayerProfileUpgradesSaveData upgrades = new();
    public PlayerProfileSkinsSaveData skins = PlayerProfileSkinsSaveData.CreateDefault();
    public PlayerProfileStatsSaveData stats = new();

    public static PlayerProfileSaveData CreateDefault()
    {
        PlayerProfileSaveData data = new();
        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        version = Math.Max(1, version);
        wallet ??= new PlayerProfileWalletSaveData();
        upgrades ??= new PlayerProfileUpgradesSaveData();
        skins ??= PlayerProfileSkinsSaveData.CreateDefault();
        stats ??= new PlayerProfileStatsSaveData();

        wallet.Normalize();
        upgrades.Normalize();
        skins.Normalize();
        stats.Normalize();
    }
}

[Serializable]
public class PlayerProfileWalletSaveData
{
    public int totalShrimps;

    public void Normalize()
    {
        totalShrimps = Math.Max(0, totalShrimps);
    }
}

[Serializable]
public class PlayerProfileUpgradesSaveData
{
    public int inkPulseDurationLevel;
    public int inkPulseRechargeRateLevel;

    public void Normalize()
    {
        inkPulseDurationLevel = Math.Max(0, inkPulseDurationLevel);
        inkPulseRechargeRateLevel = Math.Max(0, inkPulseRechargeRateLevel);
    }
}

[Serializable]
public class PlayerProfileSkinsSaveData
{
    public string[] unlockedSkinIds = { PlayerSkinIds.Default };
    public string equippedSkinId = PlayerSkinIds.Default;

    public static PlayerProfileSkinsSaveData CreateDefault()
    {
        return new PlayerProfileSkinsSaveData
        {
            unlockedSkinIds = new[] { PlayerSkinIds.Default },
            equippedSkinId = PlayerSkinIds.Default
        };
    }

    public void Normalize()
    {
        unlockedSkinIds = unlockedSkinIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();

        if (unlockedSkinIds == null || unlockedSkinIds.Length == 0)
        {
            unlockedSkinIds = new[] { PlayerSkinIds.Default };
        }

        if (!unlockedSkinIds.Contains(PlayerSkinIds.Default))
        {
            unlockedSkinIds = unlockedSkinIds
                .Concat(new[] { PlayerSkinIds.Default })
                .Distinct()
                .ToArray();
        }

        if (string.IsNullOrWhiteSpace(equippedSkinId) || !unlockedSkinIds.Contains(equippedSkinId))
        {
            equippedSkinId = PlayerSkinIds.Default;
        }
    }
}

[Serializable]
public class PlayerProfileStatsSaveData
{
    public long bestScore;
    public int totalRuns;
    public int totalPortalsCrossed;
    public int totalShrimpsCollected;

    public void Normalize()
    {
        bestScore = Math.Max(0, bestScore);
        totalRuns = Math.Max(0, totalRuns);
        totalPortalsCrossed = Math.Max(0, totalPortalsCrossed);
        totalShrimpsCollected = Math.Max(0, totalShrimpsCollected);
    }
}
