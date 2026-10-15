using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MobileReferenceContentFitter : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);

    private RectTransform content;
    private RectTransform lastParent;
    private Vector2 lastAvailableSize;
    private Vector2 lastReferenceResolution;
    private bool hasObservedParent;

    private void OnEnable()
    {
        hasObservedParent = false;
        RefreshFromParent();
    }

    private void LateUpdate()
    {
        RefreshFromParent();
    }

    public void Apply(Vector2 availableSize, bool isMobilePlatform)
    {
        if (!isMobilePlatform || !IsValidSize(availableSize) || !IsValidSize(referenceResolution))
            return;

        float scale = Mathf.Min(availableSize.x / referenceResolution.x,
            availableSize.y / referenceResolution.y);
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
            return;

        ResolveContent();
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = referenceResolution;
        content.localScale = Vector3.one * scale;
    }

    private void RefreshFromParent()
    {
        if (!Application.isMobilePlatform)
            return;

        ResolveContent();
        RectTransform parent = content.parent as RectTransform;
        if (parent == null)
            return;
        Vector2 availableSize = parent.rect.size;
        if (!hasObservedParent || parent != lastParent || availableSize != lastAvailableSize
            || referenceResolution != lastReferenceResolution)
        {
            Apply(availableSize, true);
            lastParent = parent;
            lastAvailableSize = availableSize;
            lastReferenceResolution = referenceResolution;
            hasObservedParent = true;
        }
    }

    private void ResolveContent()
    {
        if (content == null)
            content = GetComponent<RectTransform>();
    }

    private static bool IsValidSize(Vector2 size) => size.x > 0f && size.y > 0f
        && !float.IsNaN(size.x) && !float.IsNaN(size.y)
        && !float.IsInfinity(size.x) && !float.IsInfinity(size.y);
}
