using System;
using System.Linq;
using UnityEngine;

public static class PersistentPlayerProfile
{
    private static PlayerProfileSaveData current;
    private static bool loaded;

    public static event Action<PlayerProfileSaveData> ProfileChanged;

    public static PlayerProfileSaveData Current
    {
        get
        {
            EnsureLoaded();
            return current;
        }
    }

    public static int TotalShrimps
    {
        get
        {
            EnsureLoaded();
            return current.wallet.totalShrimps;
        }
    }

    public static string EquippedSkinId
    {
        get
        {
            EnsureLoaded();
            return current.skins.equippedSkinId;
        }
    }

    public static void AddShrimps(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureLoaded();
        current.wallet.totalShrimps = Mathf.Max(0, current.wallet.totalShrimps + amount);
        current.stats.totalShrimpsCollected = Mathf.Max(0, current.stats.totalShrimpsCollected + amount);
        SaveAndNotify();
    }

    public static void RefundShrimps(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureLoaded();
        current.wallet.totalShrimps = Mathf.Max(0, current.wallet.totalShrimps + amount);
        SaveAndNotify();
    }

    public static bool TrySpendShrimps(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        EnsureLoaded();
        if (current.wallet.totalShrimps < amount)
        {
            return false;
        }

        current.wallet.totalShrimps -= amount;
        SaveAndNotify();
        return true;
    }

    public static void RecordRunEnded(long score)
    {
        EnsureLoaded();
        current.stats.totalRuns = Mathf.Max(0, current.stats.totalRuns + 1);
        current.stats.bestScore = Math.Max(current.stats.bestScore, Math.Max(0, score));
        SaveAndNotify();
    }

    public static void RecordPortalCrossed()
    {
        EnsureLoaded();
        current.stats.totalPortalsCrossed = Mathf.Max(0, current.stats.totalPortalsCrossed + 1);
        SaveAndNotify();
    }

    public static bool HasUnlockedSkin(string skinId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(skinId)
            && current.skins.unlockedSkinIds.Contains(skinId);
    }

    public static void UnlockSkin(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
        {
            return;
        }

        EnsureLoaded();
        if (current.skins.unlockedSkinIds.Contains(skinId))
        {
            return;
        }

        current.skins.unlockedSkinIds = current.skins.unlockedSkinIds
            .Concat(new[] { skinId })
            .Distinct()
            .ToArray();
        SaveAndNotify();
    }

    public static bool TryEquipSkin(string skinId)
    {
        if (!HasUnlockedSkin(skinId))
        {
            return false;
        }

        current.skins.equippedSkinId = skinId;
        SaveAndNotify();
        return true;
    }

    public static int GetInkPulseDurationLevel()
    {
        EnsureLoaded();
        return current.upgrades.inkPulseDurationLevel;
    }

    public static int GetInkPulseRechargeRateLevel()
    {
        EnsureLoaded();
        return current.upgrades.inkPulseRechargeRateLevel;
    }

    public static void SetInkPulseDurationLevel(int level)
    {
        EnsureLoaded();
        current.upgrades.inkPulseDurationLevel = Mathf.Max(0, level);
        SaveAndNotify();
    }

    public static void SetInkPulseRechargeRateLevel(int level)
    {
        EnsureLoaded();
        current.upgrades.inkPulseRechargeRateLevel = Mathf.Max(0, level);
        SaveAndNotify();
    }

    public static void Reload()
    {
        loaded = false;
        EnsureLoaded();
        ProfileChanged?.Invoke(current);
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        current = PlayerProfileRepository.Load();
        current.Normalize();
        loaded = true;
    }

    private static void SaveAndNotify()
    {
        current.Normalize();
        PlayerProfileRepository.Save(current);
        ProfileChanged?.Invoke(current);
    }
}
