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
    [SerializeField] private string firstSlotKey = "W";
    [SerializeField] private string secondSlotKey = "Q";

    private void Awake()
    {
        ResolveVisualReferences();
    }

    private void OnEnable()
    {
        RuntimeGadgetInventory.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        RuntimeGadgetInventory.Changed -= Refresh;
    }

    private void Refresh()
    {
        ResolveVisualReferences();
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

        firstSlotIcon ??= FindOrCreateSlotIcon(firstSlotRoot);
        secondSlotIcon ??= FindOrCreateSlotIcon(secondSlotRoot);
        firstSlotText ??= FindOrCreateSlotText(firstSlotRoot, "FirstSlotKey");
        secondSlotText ??= FindOrCreateSlotText(secondSlotRoot, "SecondSlotKey");
    }

    private Image FindOrCreateSlotIcon(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        if (!slotRoot.TryGetComponent(out Image image))
        {
            image = slotRoot.gameObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private TMP_Text FindOrCreateSlotText(RectTransform slotRoot, string textName)
    {
        if (slotRoot == null)
        {
            return null;
        }

        TMP_Text existingText = slotRoot.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (existingText != null)
        {
            existingText.gameObject.SetActive(true);
            ConfigureSlotText(existingText);
            return existingText;
        }

        TextMeshProUGUI createdText = CreateText(textName, slotRoot);
        ConfigureSlotText(createdText);
        return createdText;
    }

    private void ConfigureSlotText(TMP_Text text)
    {
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(8f, -6f);
        rectTransform.sizeDelta = new Vector2(44f, 44f);
        rectTransform.SetAsLastSibling();

        text.raycastTarget = false;
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.margin = Vector4.zero;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private TextMeshProUGUI CreateText(string textName, Transform parent)
    {
        GameObject textObject = new GameObject(textName, typeof(RectTransform));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, worldPositionStays: false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        return text;
    }
}
