using System;

public static class RunGadgetUnlockService
{
    public static event Action<string> RunGadgetUnlocked;

    public static void RefreshUnlockedRunGadgets()
    {
        UnlockablesCatalogSaveData catalog = PersistentPlayerProfile.UnlockablesCatalog;
        foreach (RunGadgetUnlockDefinition gadget in catalog.runGadgets)
        {
            if (gadget == null)
            {
                continue;
            }

            if (!gadget.defaultUnlocked && !UnlockablesCatalogQuery.IsGoalMet(gadget.unlockGoal))
            {
                continue;
            }

            UnlockRunGadgetIfNeeded(gadget.id);
        }
    }

    public static bool IsRunGadgetUnlocked(GadgetId gadget)
    {
        RefreshUnlockedRunGadgets();
        return PersistentPlayerProfile.HasUnlockedRunGadget(gadget);
    }

    public static bool CanOfferAppearInRunShop(ShopGadgetOffer offer)
    {
        return offer != null
            && offer.IsConfigured
            && IsRunGadgetUnlocked(offer.GadgetId);
    }

    private static void UnlockRunGadgetIfNeeded(string gadgetUnlockId)
    {
        if (string.IsNullOrWhiteSpace(gadgetUnlockId) || PersistentPlayerProfile.HasUnlockedRunGadget(gadgetUnlockId))
        {
            return;
        }

        PersistentPlayerProfile.UnlockRunGadget(gadgetUnlockId);
        RunGadgetUnlocked?.Invoke(gadgetUnlockId);
    }
}
