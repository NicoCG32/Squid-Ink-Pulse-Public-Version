using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class PermanentShopSlotVisual
{
    [SerializeField] private Button button;
    [SerializeField] private ButtonVisualState buttonVisualState;
    [SerializeField] private Image normalImage;
    [SerializeField] private Image highlightedImage;
    [SerializeField] private Image pressedImage;
    [SerializeField] private Image fallbackImage;
    [SerializeField] private GameObject purchasedState;
    [SerializeField] private GameObject equippedState;
    [SerializeField] private GameObject legacyBuyedState;
    [SerializeField] private GameObject legacySelectedState;

    public PermanentShopSlotVisual()
    {
    }

    public PermanentShopSlotVisual(
        Button button,
        ButtonVisualState buttonVisualState,
        Image normalImage,
        Image highlightedImage,
        Image pressedImage,
        Image fallbackImage,
        GameObject purchasedState,
        GameObject equippedState,
        GameObject legacyBuyedState,
        GameObject legacySelectedState)
    {
        this.button = button;
        this.buttonVisualState = buttonVisualState;
        this.normalImage = normalImage;
        this.highlightedImage = highlightedImage;
        this.pressedImage = pressedImage;
        this.fallbackImage = fallbackImage;
        this.purchasedState = purchasedState;
        this.equippedState = equippedState;
        this.legacyBuyedState = legacyBuyedState;
        this.legacySelectedState = legacySelectedState;
    }

    public Button Button => button;
    public bool IsConfigured => button != null;

    public void ConfigureSelectedVisualState(bool usePressedStateWhenSelected)
    {
        if (buttonVisualState != null)
        {
            buttonVisualState.SetUsePressedStateWhenSelected(usePressedStateWhenSelected);
        }
    }

    public void ApplySprites(Sprite normalSprite, Sprite pressedSprite, bool usePressedSpriteWhenSelected)
    {
        bool appliedToVisualStates = false;
        appliedToVisualStates |= ApplyStateImage(normalImage, normalSprite);
        appliedToVisualStates |= ApplyStateImage(highlightedImage, normalSprite);
        appliedToVisualStates |= ApplyStateImage(pressedImage, pressedSprite);

        if (!appliedToVisualStates)
        {
            Sprite fallbackSprite = usePressedSpriteWhenSelected ? pressedSprite : normalSprite;
            ApplyFallbackButtonSprite(fallbackSprite);
        }
    }

    public void ApplySkinOwnershipVisuals(bool isOwned, bool isEquipped)
    {
        SetActive(purchasedState, isOwned && !isEquipped);
        SetActive(legacyBuyedState, isOwned && !isEquipped);
        SetActive(equippedState, isEquipped);
        SetActive(legacySelectedState, isEquipped);
    }

    private static bool ApplyStateImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return false;
        }

        image.enabled = sprite != null;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        image.raycastTarget = false;
        return true;
    }

    private void ApplyFallbackButtonSprite(Sprite sprite)
    {
        Image image = fallbackImage;
        if (image == null && button != null)
        {
            image = button.targetGraphic as Image;
        }

        if (image == null && button != null)
        {
            image = button.GetComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        image.enabled = true;
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        Color color = image.color;
        color.a = sprite != null ? 1f : 0f;
        image.color = color;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
