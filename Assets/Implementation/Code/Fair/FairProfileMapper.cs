using System;
using System.Linq;

public static class FairProfileMapper
{
    public static FairProfileSnapshot CreateSnapshotFromLocalProfile(string nickname)
    {
        PlayerProfileSaveData profile = PersistentPlayerProfile.Current;
        PlayerRecordsSaveData records = PersistentPlayerProfile.Records;
        return new FairProfileSnapshot
        {
            version = 1,
            nickname = nickname ?? string.Empty,
            records = new PlayerRecordsSaveData
            {
                totalShrimps = records.totalShrimps,
                bestScore = records.bestScore,
                totalRuns = records.totalRuns,
                totalPortalsCrossed = records.totalPortalsCrossed,
                totalShrimpsCollected = records.totalShrimpsCollected
            },
            profile = new FairProfileSnapshotProfile
            {
                permanentUpgrades = new PlayerProfilePermanentUpgradesSaveData
                {
                    inkPulseDurationLevel = profile.permanentUpgrades.inkPulseDurationLevel,
                    inkPulseRechargeRateLevel = profile.permanentUpgrades.inkPulseRechargeRateLevel,
                    shrimpMultiplierLevel = profile.permanentUpgrades.shrimpMultiplierLevel,
                    scoreMultiplierLevel = profile.permanentUpgrades.scoreMultiplierLevel
                },
                skins = new PlayerProfileSkinsSaveData
                {
                    unlockedSkinIds = CopyStrings(profile.skins.unlockedSkinIds),
                    equippedSkinId = profile.skins.equippedSkinId
                },
                runGadgetUnlocks = new PlayerProfileRunGadgetUnlocksSaveData
                {
                    unlockedRunGadgetIds = CopyStrings(profile.runGadgetUnlocks.unlockedRunGadgetIds)
                }
            },
            unlockedEvents = CopyStrings(profile.lore.viewedComicEventIds),
            updatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    public static void ApplySnapshotToLocalProfile(FairProfileSnapshot snapshot)
    {
        FairProfileSnapshot normalized = NormalizeSnapshot(snapshot);
        PlayerProfileSaveData profile = PlayerProfileSaveData.CreateDefault();
        profile.permanentUpgrades = normalized.profile.permanentUpgrades;
        profile.skins = normalized.profile.skins;
        profile.runGadgetUnlocks = normalized.profile.runGadgetUnlocks;
        profile.lore = new PlayerProfileLoreSaveData
        {
            viewedComicEventIds = CopyStrings(normalized.unlockedEvents)
        };

        PlayerRecordsSaveData records = normalized.records ?? PlayerRecordsSaveData.CreateDefault();
        PersistentPlayerProfile.ReplaceForFairMode(profile, records);
        ShrimpRuntimeWallet.ResetForRuntime();
        RunGadgetUnlockService.RefreshUnlockedRunGadgets();
    }

    private static FairProfileSnapshot NormalizeSnapshot(FairProfileSnapshot snapshot)
    {
        FairProfileSnapshot normalized = snapshot ?? new FairProfileSnapshot();
        normalized.records ??= PlayerRecordsSaveData.CreateDefault();
        normalized.profile ??= FairProfileSnapshotProfile.CreateDefault();
        normalized.profile.permanentUpgrades ??= new PlayerProfilePermanentUpgradesSaveData();
        normalized.profile.skins ??= PlayerProfileSkinsSaveData.CreateDefault();
        normalized.profile.runGadgetUnlocks ??= PlayerProfileRunGadgetUnlocksSaveData.CreateDefault();
        normalized.unlockedEvents = CopyStrings(normalized.unlockedEvents);
        normalized.records.Normalize();
        normalized.profile.permanentUpgrades.Normalize();
        normalized.profile.skins.Normalize();
        normalized.profile.runGadgetUnlocks.Normalize();
        return normalized;
    }

    private static string[] CopyStrings(string[] values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct()
            .ToArray() ?? Array.Empty<string>();
    }
}
