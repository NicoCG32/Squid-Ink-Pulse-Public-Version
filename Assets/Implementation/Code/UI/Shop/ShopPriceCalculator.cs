using UnityEngine;

public static class ShopPriceCalculator
{
    public static int CalculatePrice(
        ShopGadgetOffer offer,
        long score,
        float scorePriceStep,
        float globalPriceMultiplier,
        float randomPriceMultiplier)
    {
        if (offer == null)
        {
            return 0;
        }

        float scoreMultiplier = (score / Mathf.Max(1f, scorePriceStep)) + 1f;
        float rawPrice = offer.GetBasePrice()
            * Mathf.Max(0.01f, globalPriceMultiplier)
            * Mathf.Max(0f, randomPriceMultiplier)
            * Mathf.Max(1f, scoreMultiplier);

        return Mathf.Max(1, Mathf.CeilToInt(rawPrice));
    }
}
