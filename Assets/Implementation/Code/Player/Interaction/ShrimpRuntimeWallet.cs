using System;
using UnityEngine;

public static class ShrimpRuntimeWallet
{
    private static int totalShrimp;
    private static bool initialized;

    public static int TotalShrimp
    {
        get
        {
            EnsureInitialized();
            return totalShrimp;
        }
    }

    public static event Action<int> TotalChanged;

    public static void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureInitialized();
        PersistentPlayerProfile.AddShrimps(amount);
        totalShrimp = PersistentPlayerProfile.TotalShrimps;
        TotalChanged?.Invoke(totalShrimp);
    }

    public static bool TrySpend(int amount)
    {
        EnsureInitialized();
        if (amount <= 0 || !PersistentPlayerProfile.TrySpendShrimps(amount))
        {
            return false;
        }

        totalShrimp = PersistentPlayerProfile.TotalShrimps;
        TotalChanged?.Invoke(totalShrimp);
        return true;
    }

    public static void Refund(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureInitialized();
        PersistentPlayerProfile.RefundShrimps(amount);
        totalShrimp = PersistentPlayerProfile.TotalShrimps;
        TotalChanged?.Invoke(totalShrimp);
    }

    public static void ResetForRuntime()
    {
        initialized = false;
        EnsureInitialized();
        TotalChanged?.Invoke(totalShrimp);
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        totalShrimp = Mathf.Max(0, PersistentPlayerProfile.TotalShrimps);
        initialized = true;
    }
}
