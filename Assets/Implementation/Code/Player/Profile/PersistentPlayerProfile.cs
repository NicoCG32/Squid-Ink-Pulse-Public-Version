using System;
using System.Linq;
using UnityEngine;

public static class PersistentPlayerProfile
{
    private static PlayerProfileSaveData currentProfile;
    private static PlayerRecordsSaveData currentRecords;
    private static UnlockablesCatalogSaveData currentCatalog;
    private static bool loaded;

    public static event Action<PlayerProfileSaveData> ProfileChanged;
    public static event Action<PlayerRecordsSaveData> RecordsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        currentProfile = null;
        currentRecords = null;
        currentCatalog = null;
        loaded = false;
        ProfileChanged = null;
        RecordsChanged = null;
    }

    public static PlayerProfileSaveData Current
    {
        get
        {
            EnsureLoaded();
            return currentProfile;
        }
    }

    public static PlayerRecordsSaveData Records
    {
        get
        {
            EnsureLoaded();
            return currentRecords;
        }
    }

    public static UnlockablesCatalogSaveData UnlockablesCatalog
    {
        get
        {
            EnsureLoaded();
            return currentCatalog;
        }
    }

    public static int TotalShrimps
    {
        get
        {
            EnsureLoaded();
            return currentRecords.totalShrimps;
        }
    }

    public static long BestScore
    {
        get
        {
            EnsureLoaded();
            return currentRecords.bestScore;
        }
    }

    public static string EquippedSkinId
    {
        get
        {
            EnsureLoaded();
            return currentProfile.skins.equippedSkinId;
        }
    }

    public static void AddShrimps(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureLoaded();
        currentRecords.totalShrimps = Mathf.Max(0, currentRecords.totalShrimps + amount);
        currentRecords.totalShrimpsCollected = Mathf.Max(0, currentRecords.totalShrimpsCollected + amount);
        SaveRecordsAndNotify();
        RunGadgetUnlockService.RefreshUnlockedRunGadgets();
    }

    public static void RefundShrimps(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureLoaded();
        currentRecords.totalShrimps = Mathf.Max(0, currentRecords.totalShrimps + amount);
        SaveRecordsAndNotify();
    }

    public static bool TrySpendShrimps(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        EnsureLoaded();
        if (currentRecords.totalShrimps < amount)
        {
            return false;
        }

        currentRecords.totalShrimps -= amount;
        SaveRecordsAndNotify();
        return true;
    }

    public static void RecordRunEnded(long score)
    {
        EnsureLoaded();
        currentRecords.totalRuns = Mathf.Max(0, currentRecords.totalRuns + 1);
        currentRecords.bestScore = Math.Max(currentRecords.bestScore, Math.Max(0, score));
        SaveRecordsAndNotify();
        RunGadgetUnlockService.RefreshUnlockedRunGadgets();
    }

    public static void RecordPortalCrossed()
    {
        EnsureLoaded();
        currentRecords.totalPortalsCrossed = Mathf.Max(0, currentRecords.totalPortalsCrossed + 1);
        SaveRecordsAndNotify();
        RunGadgetUnlockService.RefreshUnlockedRunGadgets();
    }

    public static bool HasUnlockedSkin(string skinId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(skinId)
            && currentProfile.skins.unlockedSkinIds.Contains(skinId);
    }

    public static void UnlockSkin(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
        {
            return;
        }

        EnsureLoaded();
        string normalizedSkinId = skinId.Trim();
        if (currentProfile.skins.unlockedSkinIds.Contains(normalizedSkinId))
        {
            return;
        }

        currentProfile.skins.unlockedSkinIds = currentProfile.skins.unlockedSkinIds
            .Concat(new[] { normalizedSkinId })
            .Distinct()
            .ToArray();
        SaveProfileAndNotify();
    }

    public static bool TryEquipSkin(string skinId)
    {
        if (!HasUnlockedSkin(skinId))
        {
            return false;
        }

        currentProfile.skins.equippedSkinId = skinId.Trim();
        SaveProfileAndNotify();
        return true;
    }

    public static int GetInkPulseDurationLevel()
    {
        EnsureLoaded();
        return currentProfile.permanentUpgrades.inkPulseDurationLevel;
    }

    public static int GetInkPulseRechargeRateLevel()
    {
        EnsureLoaded();
        return currentProfile.permanentUpgrades.inkPulseRechargeRateLevel;
    }

    public static int GetShrimpMultiplierLevel()
    {
        EnsureLoaded();
        return currentProfile.permanentUpgrades.shrimpMultiplierLevel;
    }

    public static int GetScoreMultiplierLevel()
    {
        EnsureLoaded();
        return currentProfile.permanentUpgrades.scoreMultiplierLevel;
    }

    public static int GetPermanentUpgradeLevel(string upgradeId)
    {
        EnsureLoaded();
        return currentProfile.permanentUpgrades.GetLevel(upgradeId);
    }

    public static void SetInkPulseDurationLevel(int level)
    {
        EnsureLoaded();
        currentProfile.permanentUpgrades.inkPulseDurationLevel = Mathf.Max(0, level);
        SaveProfileAndNotify();
    }

    public static void SetInkPulseRechargeRateLevel(int level)
    {
        EnsureLoaded();
        currentProfile.permanentUpgrades.inkPulseRechargeRateLevel = Mathf.Max(0, level);
        SaveProfileAndNotify();
    }

    public static void SetPermanentUpgradeLevel(string upgradeId, int level)
    {
        EnsureLoaded();
        currentProfile.permanentUpgrades.SetLevel(upgradeId, level);
        SaveProfileAndNotify();
    }

    public static bool HasUnlockedRunGadget(string gadgetId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(gadgetId)
            && currentProfile.runGadgetUnlocks.unlockedRunGadgetIds.Contains(gadgetId);
    }

    public static bool HasUnlockedRunGadget(GadgetId gadget)
    {
        return HasUnlockedRunGadget(GadgetCatalog.GetUnlockId(gadget));
    }

    public static void UnlockRunGadget(string gadgetId)
    {
        if (string.IsNullOrWhiteSpace(gadgetId))
        {
            return;
        }

        EnsureLoaded();
        string normalizedGadgetId = gadgetId.Trim();
        if (currentProfile.runGadgetUnlocks.unlockedRunGadgetIds.Contains(normalizedGadgetId))
        {
            return;
        }

        currentProfile.runGadgetUnlocks.unlockedRunGadgetIds = currentProfile.runGadgetUnlocks.unlockedRunGadgetIds
            .Concat(new[] { normalizedGadgetId })
            .Distinct()
            .ToArray();
        SaveProfileAndNotify();
    }

    public static void UnlockRunGadget(GadgetId gadget)
    {
        UnlockRunGadget(GadgetCatalog.GetUnlockId(gadget));
    }

    public static bool HasSeenLoreComic(string comicEventId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(comicEventId)
            && currentProfile.lore.viewedComicEventIds.Contains(comicEventId.Trim());
    }

    public static bool TryMarkLoreComicSeen(string comicEventId)
    {
        if (string.IsNullOrWhiteSpace(comicEventId))
        {
            return false;
        }

        EnsureLoaded();
        string normalizedComicEventId = comicEventId.Trim();
        if (currentProfile.lore.viewedComicEventIds.Contains(normalizedComicEventId))
        {
            return false;
        }

        currentProfile.lore.viewedComicEventIds = currentProfile.lore.viewedComicEventIds
            .Concat(new[] { normalizedComicEventId })
            .Distinct()
            .ToArray();
        SaveProfileAndNotify();
        return true;
    }

    public static bool HasUnlockedGadget(string gadgetId)
    {
        return HasUnlockedRunGadget(gadgetId);
    }

    public static void UnlockGadget(string gadgetId)
    {
        UnlockRunGadget(gadgetId);
    }

    public static void Reload()
    {
        loaded = false;
        EnsureLoaded();
        ProfileChanged?.Invoke(currentProfile);
        RecordsChanged?.Invoke(currentRecords);
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        currentCatalog = PlayerProfileRepository.LoadUnlockablesCatalog();
        currentProfile = PlayerProfileRepository.LoadProfile();
        currentRecords = PlayerProfileRepository.LoadRecords();

        currentCatalog.Normalize();
        currentProfile.Normalize();
        currentRecords.Normalize();
        loaded = true;
    }

    private static void SaveProfileAndNotify()
    {
        currentProfile.Normalize();
        PlayerProfileRepository.Save(currentProfile);
        ProfileChanged?.Invoke(currentProfile);
    }

    private static void SaveRecordsAndNotify()
    {
        currentRecords.Normalize();
        PlayerProfileRepository.SaveRecords(currentRecords);
        RecordsChanged?.Invoke(currentRecords);
    }
}
