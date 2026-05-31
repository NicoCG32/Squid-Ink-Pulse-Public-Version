public enum GadgetId
{
    None,
    ShellShield,
    InkBottle
}

public enum GadgetActivationKind
{
    Passive,
    Active
}

public static class GadgetCatalog
{
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
}
