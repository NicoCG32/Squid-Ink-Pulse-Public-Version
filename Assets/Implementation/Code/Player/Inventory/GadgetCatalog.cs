using UnityEngine;

public static class GadgetCatalog
{
    public static int GetBaseShopPrice(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => 8,
            GadgetId.InkBottle => 6,
            _ => 0
        };
    }

    public static Color GetDefaultShopTint(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => new Color(0.45f, 0.85f, 1f, 1f),
            GadgetId.InkBottle => new Color(0.08f, 0.18f, 0.28f, 1f),
            _ => Color.white
        };
    }

    public static GadgetActivationKind GetActivationKind(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.InkBottle => GadgetActivationKind.Active,
            _ => GadgetActivationKind.Passive
        };
    }

    public static bool IsActive(GadgetId gadget)
    {
        return gadget != GadgetId.None && GetActivationKind(gadget) == GadgetActivationKind.Active;
    }

    public static bool IsPassive(GadgetId gadget)
    {
        return gadget != GadgetId.None && GetActivationKind(gadget) == GadgetActivationKind.Passive;
    }

    public static string GetDisplayName(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => "Shell Shield",
            GadgetId.InkBottle => "Ink-Bottle",
            _ => string.Empty
        };
    }

    public static string GetUnlockId(GadgetId gadget)
    {
        return gadget switch
        {
            GadgetId.ShellShield => PlayerUnlockableIds.ShellShieldGadget,
            GadgetId.InkBottle => PlayerUnlockableIds.InkBottleGadget,
            _ => string.Empty
        };
    }

    public static bool TryGetGadgetId(string unlockId, out GadgetId gadget)
    {
        gadget = unlockId switch
        {
            PlayerUnlockableIds.ShellShieldGadget => GadgetId.ShellShield,
            PlayerUnlockableIds.InkBottleGadget => GadgetId.InkBottle,
            _ => GadgetId.None
        };

        return gadget != GadgetId.None;
    }
}
