using System;
using UnityEngine;

public static class InGameShopPurchaseService
{
    public static InGameShopPurchaseResult TryPurchase(
        GadgetId gadget,
        Sprite icon,
        Color iconTint,
        int price,
        Func<GadgetId, bool> hasGadget,
        Func<int, bool> trySpend,
        Action<int> refund,
        Func<GadgetId, Sprite, Color, bool> acquire)
    {
        if (gadget == GadgetId.None)
        {
            return InGameShopPurchaseResult.InvalidOffer;
        }

        if (hasGadget != null && hasGadget(gadget))
        {
            return InGameShopPurchaseResult.AlreadyOwned;
        }

        if (trySpend == null || !trySpend(price))
        {
            return InGameShopPurchaseResult.InsufficientFunds;
        }

        if (acquire == null || !acquire(gadget, icon, iconTint))
        {
            refund?.Invoke(price);
            return InGameShopPurchaseResult.InventoryRejected;
        }

        return InGameShopPurchaseResult.Success;
    }
}
