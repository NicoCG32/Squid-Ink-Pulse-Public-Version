using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class InGameShopOverlayPresenter
{
    private readonly GameObject menuRoot;
    private readonly CanvasGroup canvasGroup;
    private readonly Image gadgetImage;
    private readonly TMP_Text priceText;
    private readonly TMP_Text buyKeyText;
    private readonly Button buyButton;
    private readonly TMP_Text insufficientFundsText;
    private readonly TMP_Text timerText;
    private Vector3 priceTextBaseScale = Vector3.one;
    private Vector3 buyKeyTextBaseScale = Vector3.one;

    public InGameShopOverlayPresenter(
        GameObject menuRoot,
        CanvasGroup canvasGroup,
        Image gadgetImage,
        TMP_Text priceText,
        TMP_Text buyKeyText,
        Button buyButton,
        TMP_Text insufficientFundsText,
        TMP_Text timerText)
    {
        this.menuRoot = menuRoot;
        this.canvasGroup = canvasGroup;
        this.gadgetImage = gadgetImage;
        this.priceText = priceText;
        this.buyKeyText = buyKeyText;
        this.buyButton = buyButton;
        this.insufficientFundsText = insufficientFundsText;
        this.timerText = timerText;
    }

    public void WireBuyButton(UnityAction buyAction)
    {
        if (buyButton == null || buyAction == null)
        {
            return;
        }

        if (buyButton.targetGraphic == null)
        {
            buyButton.targetGraphic = buyButton.GetComponent<Graphic>();
        }

        if (buyButton.targetGraphic != null)
        {
            buyButton.targetGraphic.raycastTarget = true;
        }

        buyButton.onClick.RemoveListener(buyAction);
        buyButton.onClick.AddListener(buyAction);
    }

    public void CacheAnimatedTextScales()
    {
        if (priceText != null)
        {
            priceTextBaseScale = priceText.rectTransform.localScale;
        }

        if (buyKeyText != null)
        {
            buyKeyTextBaseScale = buyKeyText.rectTransform.localScale;
        }
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void HideImmediate()
    {
        SetVisible(false);
        ApplyIcon(null, Color.clear);
        SetInsufficientFundsVisible(false);
        SetBuyButtonInteractable(false);
        ResetAnimatedTextScales();
    }

    public void PresentOffer(Sprite icon, Color tint, int price, float remainingSeconds, bool canBuy)
    {
        ApplyIcon(icon, tint);
        SetText(priceText, price.ToString());
        RefreshTimer(remainingSeconds);
        SetInsufficientFundsVisible(false);
        SetBuyButtonInteractable(canBuy);
    }

    public void PresentEmpty(float remainingSeconds)
    {
        ApplyIcon(null, Color.clear);
        SetText(priceText, "-");
        RefreshTimer(remainingSeconds);
        SetInsufficientFundsVisible(false);
        SetBuyButtonInteractable(false);
    }

    public void RefreshTimer(float remainingSeconds)
    {
        SetText(timerText, Mathf.CeilToInt(remainingSeconds).ToString());
    }

    public void SetInsufficientFundsVisible(bool visible)
    {
        if (insufficientFundsText != null)
        {
            insufficientFundsText.gameObject.SetActive(visible);
        }
    }

    public void SetBuyButtonInteractable(bool interactable)
    {
        if (buyButton != null)
        {
            buyButton.interactable = interactable;
        }
    }

    public void AnimateAttentionTexts(float unscaledTime, float amplitude, float frequency)
    {
        float pulse = 1f + Mathf.Sin(unscaledTime * Mathf.PI * 2f * frequency) * amplitude;
        if (priceText != null)
        {
            priceText.rectTransform.localScale = priceTextBaseScale * pulse;
        }

        if (buyKeyText != null)
        {
            buyKeyText.rectTransform.localScale = buyKeyTextBaseScale * pulse;
        }
    }

    private void ApplyIcon(Sprite icon, Color tint)
    {
        if (gadgetImage == null)
        {
            return;
        }

        gadgetImage.enabled = icon != null;
        gadgetImage.sprite = icon;
        gadgetImage.color = icon != null ? tint : Color.clear;
        gadgetImage.preserveAspect = true;
    }

    private void ResetAnimatedTextScales()
    {
        if (priceText != null)
        {
            priceText.rectTransform.localScale = priceTextBaseScale;
        }

        if (buyKeyText != null)
        {
            buyKeyText.rectTransform.localScale = buyKeyTextBaseScale;
        }
    }

    private void SetVisible(bool visible)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(visible);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
