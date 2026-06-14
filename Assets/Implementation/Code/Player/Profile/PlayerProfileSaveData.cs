using System;
using System.Linq;

[Serializable]
public class PlayerProfileSaveData
{
    public int version = PlayerProfileRepository.CurrentVersion;
    public PlayerProfilePermanentUpgradesSaveData permanentUpgrades = new();
    public PlayerProfileSkinsSaveData skins = PlayerProfileSkinsSaveData.CreateDefault();
    public PlayerProfileRunGadgetUnlocksSaveData runGadgetUnlocks = PlayerProfileRunGadgetUnlocksSaveData.CreateDefault();

    public static PlayerProfileSaveData CreateDefault()
    {
        PlayerProfileSaveData data = new();
        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        version = Math.Max(1, version);
        permanentUpgrades ??= new PlayerProfilePermanentUpgradesSaveData();
        skins ??= PlayerProfileSkinsSaveData.CreateDefault();
        runGadgetUnlocks ??= PlayerProfileRunGadgetUnlocksSaveData.CreateDefault();

        permanentUpgrades.Normalize();
        skins.Normalize();
        runGadgetUnlocks.Normalize();
    }
}

[Serializable]
public class PlayerProfilePermanentUpgradesSaveData
{
    public int inkPulseDurationLevel;
    public int inkPulseRechargeRateLevel;
    public int shrimpMultiplierLevel;
    public int scoreMultiplierLevel;

    public void Normalize()
    {
        inkPulseDurationLevel = Math.Max(0, inkPulseDurationLevel);
        inkPulseRechargeRateLevel = Math.Max(0, inkPulseRechargeRateLevel);
        shrimpMultiplierLevel = Math.Max(0, shrimpMultiplierLevel);
        scoreMultiplierLevel = Math.Max(0, scoreMultiplierLevel);
    }

    public int GetLevel(string upgradeId)
    {
        return upgradeId switch
        {
            PlayerUnlockableIds.InkPulseDurationUpgrade => inkPulseDurationLevel,
            PlayerUnlockableIds.InkPulseRechargeRateUpgrade => inkPulseRechargeRateLevel,
            PlayerUnlockableIds.ShrimpMultiplierUpgrade => shrimpMultiplierLevel,
            PlayerUnlockableIds.ScoreMultiplierUpgrade => scoreMultiplierLevel,
            _ => 0
        };
    }

    public void SetLevel(string upgradeId, int level)
    {
        int normalizedLevel = Math.Max(0, level);
        switch (upgradeId)
        {
            case PlayerUnlockableIds.InkPulseDurationUpgrade:
                inkPulseDurationLevel = normalizedLevel;
                break;
            case PlayerUnlockableIds.InkPulseRechargeRateUpgrade:
                inkPulseRechargeRateLevel = normalizedLevel;
                break;
            case PlayerUnlockableIds.ShrimpMultiplierUpgrade:
                shrimpMultiplierLevel = normalizedLevel;
                break;
            case PlayerUnlockableIds.ScoreMultiplierUpgrade:
                scoreMultiplierLevel = normalizedLevel;
                break;
        }
    }
}

[Serializable]
public class PlayerProfileRunGadgetUnlocksSaveData
{
    public string[] unlockedRunGadgetIds =
    {
        PlayerUnlockableIds.ShellShieldGadget,
        PlayerUnlockableIds.InkBottleGadget
    };

    public static PlayerProfileRunGadgetUnlocksSaveData CreateDefault()
    {
        return new PlayerProfileRunGadgetUnlocksSaveData
        {
            unlockedRunGadgetIds = new[]
            {
                PlayerUnlockableIds.ShellShieldGadget,
                PlayerUnlockableIds.InkBottleGadget
            }
        };
    }

    public void Normalize()
    {
        unlockedRunGadgetIds = unlockedRunGadgetIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct()
            .ToArray();

        if (unlockedRunGadgetIds == null || unlockedRunGadgetIds.Length == 0)
        {
            unlockedRunGadgetIds = new[]
            {
                PlayerUnlockableIds.ShellShieldGadget,
                PlayerUnlockableIds.InkBottleGadget
            };
        }
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
            .Select(id => id.Trim())
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
