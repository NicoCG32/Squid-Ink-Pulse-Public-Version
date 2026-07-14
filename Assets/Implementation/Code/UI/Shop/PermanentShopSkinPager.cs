using System;
using System.Collections.Generic;

public sealed class PermanentShopSkinPager
{
    private readonly int visibleSlotCount;
    private UnlockableSkinDefinition[] visibleSkins = Array.Empty<UnlockableSkinDefinition>();

    public PermanentShopSkinPager(int visibleSlotCount)
    {
        this.visibleSlotCount = Math.Max(1, visibleSlotCount);
    }

    public int Page { get; private set; }
    public int MaxPage => visibleSkins.Length <= visibleSlotCount
        ? 0
        : Math.Max(0, (int)Math.Ceiling(visibleSkins.Length / (float)visibleSlotCount) - 1);
    public int VisibleSkinCount => visibleSkins.Length;

    public void SetCatalogSkins(UnlockableSkinDefinition[] skins)
    {
        if (skins == null || skins.Length == 0)
        {
            visibleSkins = Array.Empty<UnlockableSkinDefinition>();
            Page = 0;
            return;
        }

        List<UnlockableSkinDefinition> filteredSkins = new(skins.Length);
        for (int index = 0; index < skins.Length; index++)
        {
            UnlockableSkinDefinition skin = skins[index];
            if (skin != null && !string.IsNullOrWhiteSpace(skin.shopSpriteResourcePath))
            {
                filteredSkins.Add(skin);
            }
        }

        visibleSkins = filteredSkins.ToArray();
        NormalizePage();
    }

    public void PreviousPage()
    {
        Page = Math.Max(0, Page - 1);
    }

    public void NextPage()
    {
        Page = Math.Min(MaxPage, Page + 1);
    }

    public void NormalizePage()
    {
        Page = Math.Min(Math.Max(Page, 0), MaxPage);
    }

    public bool ContainsSkinIndex(int skinIndex)
    {
        return skinIndex >= 0 && skinIndex < visibleSkins.Length;
    }

    public UnlockableSkinDefinition GetSkinAtIndex(int skinIndex)
    {
        return ContainsSkinIndex(skinIndex) ? visibleSkins[skinIndex] : null;
    }

    public bool TryGetSkinIndexForSlot(int slotIndex, out int skinIndex)
    {
        skinIndex = Page * visibleSlotCount + slotIndex;
        return slotIndex >= 0
            && slotIndex < visibleSlotCount
            && ContainsSkinIndex(skinIndex);
    }

    public UnlockableSkinDefinition GetSkinForSlot(int slotIndex)
    {
        return TryGetSkinIndexForSlot(slotIndex, out int skinIndex)
            ? visibleSkins[skinIndex]
            : null;
    }
}
