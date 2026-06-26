using System;
using UnityEngine;

public static class ShopOfferSelector
{
    public static bool HasAnyOffer(ShopGadgetOffer[] offers, Func<ShopGadgetOffer, bool> offerFilter = null)
    {
        return CountConfiguredOffers(offers, offerFilter) > 0;
    }

    public static ShopGadgetOffer SelectOffer(ShopGadgetOffer[] offers, Func<ShopGadgetOffer, bool> offerFilter = null)
    {
        int configuredCount = CountConfiguredOffers(offers, offerFilter);
        if (configuredCount == 0)
        {
            return null;
        }

        int selectedIndex = UnityEngine.Random.Range(0, configuredCount);
        for (int i = 0; i < offers.Length; i++)
        {
            if (!CanShowOffer(offers[i], offerFilter))
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return offers[i];
            }

            selectedIndex--;
        }

        return null;
    }

    private static int CountConfiguredOffers(ShopGadgetOffer[] offers, Func<ShopGadgetOffer, bool> offerFilter)
    {
        if (offers == null || offers.Length == 0)
        {
            return 0;
        }

        int configuredCount = 0;
        for (int i = 0; i < offers.Length; i++)
        {
            if (CanShowOffer(offers[i], offerFilter))
            {
                configuredCount++;
            }
        }

        return configuredCount;
    }

    private static bool CanShowOffer(ShopGadgetOffer offer, Func<ShopGadgetOffer, bool> offerFilter)
    {
        return offer != null
            && offer.IsConfigured
            && offer.GetBasePrice() > 0
            && (offerFilter == null || offerFilter(offer));
    }
}
