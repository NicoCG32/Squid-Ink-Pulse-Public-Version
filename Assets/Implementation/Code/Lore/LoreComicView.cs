using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class LoreComicView
{
    private readonly GameObject comicRoot;
    private readonly CanvasGroup canvasGroup;
    private readonly Image comicImage;
    private readonly Button continueButton;
    private readonly GameObject continueButtonRoot;
    private readonly Transform ownerTransform;
    private readonly int minimumSortingOrder;

    public LoreComicView(
        GameObject comicRoot,
        CanvasGroup canvasGroup,
        Image comicImage,
        Button continueButton,
        GameObject continueButtonRoot,
        Transform ownerTransform,
        int minimumSortingOrder)
    {
        this.comicRoot = comicRoot;
        this.canvasGroup = canvasGroup;
        this.comicImage = comicImage;
        this.continueButton = continueButton;
        this.continueButtonRoot = continueButtonRoot;
        this.ownerTransform = ownerTransform;
        this.minimumSortingOrder = minimumSortingOrder;
    }

    public bool HasRoot => comicRoot != null;
    public bool HasContinueButton => continueButton != null;

    public void WireContinueButton(UnityAction continueAction)
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.onClick.RemoveListener(continueAction);
        continueButton.onClick.AddListener(continueAction);
    }

    public void UnwireContinueButton(UnityAction continueAction)
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(continueAction);
        }
    }

    public void Show(Sprite sprite, bool showContinue)
    {
        NormalizeRenderableScale();
        ApplySprite(sprite);
        SetVisible(visible: true, showContinue);
    }

    public void HideImmediate()
    {
        SetVisible(visible: false, showContinue: false);
    }

    public void SetContinueVisible(bool visible)
    {
        if (continueButtonRoot != null)
        {
            continueButtonRoot.SetActive(visible);
        }

        if (continueButton != null)
        {
            continueButton.interactable = visible;
        }
    }

    public void NormalizeRenderableScale()
    {
        Transform rootTransform = comicRoot != null ? comicRoot.transform : ownerTransform;
        Canvas ownerCanvas = rootTransform != null
            ? rootTransform.GetComponentInParent<Canvas>(includeInactive: true)
            : ownerTransform != null
                ? ownerTransform.GetComponentInParent<Canvas>(includeInactive: true)
                : null;

        RestoreScaleIfCollapsed(ownerTransform);
        RestoreScaleIfCollapsed(ownerCanvas != null ? ownerCanvas.transform : null);
        RestoreScaleIfCollapsed(rootTransform);
        NormalizeCanvasLayer(ownerCanvas);
    }

    private void ApplySprite(Sprite sprite)
    {
        if (comicImage == null)
        {
            return;
        }

        comicImage.sprite = sprite;
        comicImage.enabled = sprite != null;
    }

    private void SetVisible(bool visible, bool showContinue)
    {
        if (comicRoot != null && visible && !comicRoot.activeSelf)
        {
            comicRoot.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        SetContinueVisible(showContinue);

        if (comicRoot != null && !visible && ownerTransform != null && comicRoot != ownerTransform.gameObject)
        {
            comicRoot.SetActive(false);
        }
    }

    private void NormalizeCanvasLayer(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, minimumSortingOrder);
    }

    private static void RestoreScaleIfCollapsed(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 localScale = target.localScale;
        if (Mathf.Approximately(localScale.x, 0f)
            || Mathf.Approximately(localScale.y, 0f)
            || Mathf.Approximately(localScale.z, 0f))
        {
            target.localScale = Vector3.one;
        }
    }
}
