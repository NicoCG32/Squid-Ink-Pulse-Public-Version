using System.Collections.Generic;
using UnityEngine;

public sealed class PermanentShopSlotPresenter
{
    private readonly UnityEngine.Object logContext;
    private readonly Dictionary<string, Sprite> shopSpriteCache = new();
    private readonly HashSet<string> missingShopSpritePaths = new();

    public PermanentShopSlotPresenter(UnityEngine.Object logContext)
    {
        this.logContext = logContext;
    }

    public void PresentUpgradeSlot(PermanentShopSlotVisual visual, PermanentUpgradeDefinition upgrade, bool isSelected)
    {
        if (visual == null)
        {
            return;
        }

        visual.ConfigureSelectedVisualState(usePressedStateWhenSelected: true);
        ApplyShopSprites(
            visual,
            upgrade?.shopSpriteResourcePath,
            upgrade?.shopHighlightedSpriteResourcePath,
            isSelected);
    }

    public void PresentSkinSlot(PermanentShopSlotVisual visual, UnlockableSkinDefinition skin, bool isOwned, bool isEquipped)
    {
        if (visual == null)
        {
            return;
        }

        visual.ConfigureSelectedVisualState(usePressedStateWhenSelected: false);
        ApplyShopSprites(
            visual,
            skin?.shopSpriteResourcePath,
            pressedSpritePath: null,
            usePressedSpriteWhenSelected: false);
        visual.ApplySkinOwnershipVisuals(isOwned, isEquipped);
    }

    private void ApplyShopSprites(
        PermanentShopSlotVisual visual,
        string normalSpritePath,
        string pressedSpritePath,
        bool usePressedSpriteWhenSelected)
    {
        Sprite normalSprite = LoadShopSprite(normalSpritePath);
        Sprite pressedSprite = LoadShopSprite(pressedSpritePath) ?? normalSprite;
        visual.ApplySprites(normalSprite, pressedSprite, usePressedSpriteWhenSelected);
    }

    private Sprite LoadShopSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string normalizedPath = resourcePath.Trim().Replace('\\', '/');
        if (shopSpriteCache.TryGetValue(normalizedPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>(normalizedPath);
        if (sprite == null && missingShopSpritePaths.Add(normalizedPath))
        {
            Debug.LogWarning($"[PermanentShopSlotPresenter] No se encontro el sprite de tienda Resources/{normalizedPath}.", logContext);
        }

        shopSpriteCache[normalizedPath] = sprite;
        return sprite;
    }

}
