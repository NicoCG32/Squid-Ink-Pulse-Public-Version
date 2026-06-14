using System;
using UnityEngine;

[Serializable]
public class ShopGadgetOffer
{
    [SerializeField] private GameObject gadgetPrefab = null;
    [SerializeField, Min(0)] private int basePriceOverride = 0;

    public GameObject GadgetPrefab => gadgetPrefab;

    public GadgetShopItem ShopItem => gadgetPrefab != null
        ? gadgetPrefab.GetComponent<GadgetShopItem>()
        : null;

    public GadgetId GadgetId => ShopItem != null ? ShopItem.GadgetId : GadgetId.None;
    public Sprite Icon => ShopItem != null ? ShopItem.HudIcon : null;
    public Color IconTint => ShopItem != null ? ShopItem.HudIconTint : Color.white;

    public bool IsConfigured => gadgetPrefab != null && GadgetId != GadgetId.None;

    public int GetBasePrice()
    {
        return basePriceOverride > 0
            ? basePriceOverride
            : GadgetCatalog.GetBaseShopPrice(GadgetId);
    }
}
