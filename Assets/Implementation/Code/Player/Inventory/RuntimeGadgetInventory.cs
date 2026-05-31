using System;
using UnityEngine;

public static class RuntimeGadgetInventory
{
    public const int SlotCount = 2;

    private static bool initialized;
    private static bool hasShellShield;
    private static bool hasInkBottle;
    private static readonly GadgetId[] inventorySlots = { GadgetId.None, GadgetId.None };
    private static Sprite shellShieldIcon;
    private static Sprite inkBottleIcon;
    private static Color shellShieldIconTint = Color.white;
    private static Color inkBottleIconTint = Color.white;

    public static bool IsInitialized => initialized;
    public static event Action Changed;

    public static void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        Changed?.Invoke();
    }

    public static bool HasGadget(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => hasShellShield,
            GadgetId.InkBottle => hasInkBottle,
            _ => false
        };
    }

    public static GadgetId GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
        {
            return GadgetId.None;
        }

        return inventorySlots[slotIndex];
    }

    public static Sprite GetIcon(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => shellShieldIcon,
            GadgetId.InkBottle => inkBottleIcon,
            _ => null
        };
    }

    public static Color GetIconTint(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => shellShieldIconTint,
            GadgetId.InkBottle => inkBottleIconTint,
            _ => Color.white
        };
    }

    public static bool Acquire(GadgetId gadget, Sprite icon, Color iconTint)
    {
        if (gadget == GadgetId.None)
        {
            return false;
        }

        InitializeIfNeeded();
        if (HasGadget(gadget))
        {
            return false;
        }

        if (!AssignSlotIfMissing(gadget))
        {
            return false;
        }

        SetOwned(gadget, true);
        RegisterIcon(gadget, icon, iconTint);
        Changed?.Invoke();
        return true;
    }

    private static void SetOwned(GadgetId gadget, bool isOwned)
    {
        switch (gadget)
        {
            case GadgetId.ShellShield:
                hasShellShield = isOwned;
                break;
            case GadgetId.InkBottle:
                hasInkBottle = isOwned;
                break;
        }
    }

    private static void RegisterIcon(GadgetId gadget, Sprite icon, Color iconTint)
    {
        if (icon == null)
        {
            return;
        }

        switch (gadget)
        {
            case GadgetId.ShellShield:
                shellShieldIcon = icon;
                shellShieldIconTint = iconTint;
                break;
            case GadgetId.InkBottle:
                inkBottleIcon = icon;
                inkBottleIconTint = iconTint;
                break;
        }
    }

    private static bool AssignSlotIfMissing(GadgetId gadget)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (inventorySlots[i] == gadget)
            {
                return true;
            }
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (inventorySlots[i] == GadgetId.None)
            {
                inventorySlots[i] = gadget;
                return true;
            }
        }

        return false;
    }

    public static bool TryConsume(GadgetId gadget)
    {
        if (!HasGadget(gadget))
        {
            return false;
        }

        switch (gadget)
        {
            case GadgetId.ShellShield:
                hasShellShield = false;
                break;
            case GadgetId.InkBottle:
                hasInkBottle = false;
                break;
            default:
                return false;
        }

        ReleaseSlot(gadget);
        Changed?.Invoke();
        return true;
    }

    private static void ReleaseSlot(GadgetId gadget)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (inventorySlots[i] == gadget)
            {
                inventorySlots[i] = GadgetId.None;
            }
        }
    }

    public static bool TryConsumeShellShield()
    {
        return TryConsume(GadgetId.ShellShield);
    }

    public static void ResetForRuntime()
    {
        initialized = false;
        hasShellShield = false;
        hasInkBottle = false;
        inventorySlots[0] = GadgetId.None;
        inventorySlots[1] = GadgetId.None;
        shellShieldIcon = null;
        inkBottleIcon = null;
        shellShieldIconTint = Color.white;
        inkBottleIconTint = Color.white;
        Changed?.Invoke();
    }
}
