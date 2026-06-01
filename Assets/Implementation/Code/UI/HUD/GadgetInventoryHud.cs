using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GadgetInventoryHud : MonoBehaviour
{
    [Header("Slot References")]
    [SerializeField] private RectTransform firstSlotRoot;
    [SerializeField] private RectTransform secondSlotRoot;
    [SerializeField] private Image firstSlotIcon;
    [SerializeField] private Image secondSlotIcon;
    [SerializeField] private TMP_Text firstSlotText;
    [SerializeField] private TMP_Text secondSlotText;

    [Header("Labels")]
    [SerializeField] private string firstSlotKey = "Q";
    [SerializeField] private string secondSlotKey = "W";

    [Header("Attention Animation")]
    [SerializeField, Min(0f)] private float textPulseAmplitude = 0.12f;
    [SerializeField, Min(0.01f)] private float textPulseFrequency = 2.5f;

    private TMP_Text cachedFirstSlotText;
    private TMP_Text cachedSecondSlotText;
    private Vector3 firstSlotTextBaseScale = Vector3.one;
    private Vector3 secondSlotTextBaseScale = Vector3.one;

    private void Awake()
    {
        ResolveVisualReferences();
        CacheAnimatedTextScalesIfNeeded();
    }

    private void OnEnable()
    {
        RuntimeGadgetInventory.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        RuntimeGadgetInventory.Changed -= Refresh;
        ResetAnimatedTextScales();
    }

    private void Update()
    {
        AnimateVisibleKeyLabels();
    }

    private void Refresh()
    {
        ResolveVisualReferences();
        CacheAnimatedTextScalesIfNeeded();
        RefreshSlot(firstSlotIcon, firstSlotText, 0, firstSlotKey);
        RefreshSlot(secondSlotIcon, secondSlotText, 1, secondSlotKey);
    }

    private void RefreshSlot(Image targetIcon, TMP_Text targetText, int slotIndex, string keyLabel)
    {
        GadgetId gadget = RuntimeGadgetInventory.GetSlot(slotIndex);
        bool hasGadget = gadget != GadgetId.None && RuntimeGadgetInventory.HasGadget(gadget);

        ApplyIcon(targetIcon, gadget, hasGadget);
        ApplyKeyLabel(targetText, gadget, hasGadget, keyLabel);
    }

    private void ApplyIcon(Image targetIcon, GadgetId gadget, bool shouldShow)
    {
        if (targetIcon == null)
        {
            return;
        }

        targetIcon.raycastTarget = false;
        targetIcon.preserveAspect = true;

        if (!shouldShow)
        {
            targetIcon.enabled = false;
            targetIcon.sprite = null;
            return;
        }

        Sprite icon = RuntimeGadgetInventory.GetIcon(gadget);
        targetIcon.enabled = icon != null;
        targetIcon.sprite = icon;
        targetIcon.color = icon != null ? RuntimeGadgetInventory.GetIconTint(gadget) : Color.clear;
    }

    private void ApplyKeyLabel(TMP_Text targetText, GadgetId gadget, bool hasGadget, string keyLabel)
    {
        if (targetText == null)
        {
            return;
        }

        bool shouldShowKey = hasGadget && GadgetCatalog.IsActive(gadget);
        targetText.gameObject.SetActive(shouldShowKey);
        targetText.text = shouldShowKey ? keyLabel : string.Empty;
        targetText.raycastTarget = false;

        if (!shouldShowKey)
        {
            ResetTextScale(targetText);
        }
    }

    private void ResolveVisualReferences()
    {
        if (firstSlotRoot == null || secondSlotRoot == null)
        {
            RectTransform[] children = GetComponentsInChildren<RectTransform>(includeInactive: true);
            foreach (RectTransform child in children)
            {
                if (firstSlotRoot == null && child.name == "Gadget1")
                {
                    firstSlotRoot = child;
                }

                if (secondSlotRoot == null && child.name == "Gadget2")
                {
                    secondSlotRoot = child;
                }
            }
        }

        firstSlotIcon ??= FindSlotIcon(firstSlotRoot);
        secondSlotIcon ??= FindSlotIcon(secondSlotRoot);
        firstSlotText ??= FindSlotText(firstSlotRoot);
        secondSlotText ??= FindSlotText(secondSlotRoot);
    }

    private Image FindSlotIcon(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        return slotRoot.TryGetComponent(out Image image) ? image : null;
    }

    private TMP_Text FindSlotText(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        TMP_Text existingText = slotRoot.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (existingText != null)
        {
            existingText.raycastTarget = false;
            return existingText;
        }

        return null;
    }

    private void CacheAnimatedTextScalesIfNeeded()
    {
        if (firstSlotText != null && cachedFirstSlotText != firstSlotText)
        {
            cachedFirstSlotText = firstSlotText;
            firstSlotTextBaseScale = firstSlotText.rectTransform.localScale;
        }

        if (secondSlotText != null && cachedSecondSlotText != secondSlotText)
        {
            cachedSecondSlotText = secondSlotText;
            secondSlotTextBaseScale = secondSlotText.rectTransform.localScale;
        }
    }

    private void AnimateVisibleKeyLabels()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * textPulseFrequency) * textPulseAmplitude;
        ApplyTextPulse(firstSlotText, firstSlotTextBaseScale, pulse);
        ApplyTextPulse(secondSlotText, secondSlotTextBaseScale, pulse);
    }

    private void ApplyTextPulse(TMP_Text text, Vector3 baseScale, float pulse)
    {
        if (text != null && text.gameObject.activeInHierarchy)
        {
            text.rectTransform.localScale = baseScale * pulse;
        }
    }

    private void ResetAnimatedTextScales()
    {
        ResetTextScale(firstSlotText);
        ResetTextScale(secondSlotText);
    }

    private void ResetTextScale(TMP_Text text)
    {
        if (text == firstSlotText && text != null)
        {
            text.rectTransform.localScale = firstSlotTextBaseScale;
            return;
        }

        if (text == secondSlotText && text != null)
        {
            text.rectTransform.localScale = secondSlotTextBaseScale;
        }
    }
}
