using System;
using UnityEngine;

public static class ShrimpRuntimeWallet
{
    private static int totalShrimp;

    public static int TotalShrimp => totalShrimp;
    public static event Action<int> TotalChanged;

    public static void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        totalShrimp = Mathf.Max(0, totalShrimp + amount);
        TotalChanged?.Invoke(totalShrimp);
    }

    public static bool TrySpend(int amount)
    {
        if (amount <= 0 || totalShrimp < amount)
        {
            return false;
        }

        totalShrimp -= amount;
        TotalChanged?.Invoke(totalShrimp);
        return true;
    }

    public static void ResetForRuntime()
    {
        totalShrimp = 0;
        TotalChanged?.Invoke(totalShrimp);
    }
}
