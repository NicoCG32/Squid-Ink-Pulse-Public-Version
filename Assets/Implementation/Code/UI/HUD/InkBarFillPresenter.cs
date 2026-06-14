using UnityEngine;

[DisallowMultipleComponent]
public class InkBarFillPresenter : MonoBehaviour
{
    private const float EmptyThreshold = 0.0001f;
    private const float FullThreshold = 0.9999f;

    public enum EffectPresentationMode
    {
        FollowFillTip = 0,
        RevealThroughFill = 1
    }

    [Header("Mode")]
    [SerializeField] private EffectPresentationMode effectPresentationMode = EffectPresentationMode.FollowFillTip;
    [SerializeField] private bool refreshLayoutEveryFrame = true;

    [Header("References")]
    [SerializeField] private RectTransform fillViewport;
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform effectAnchor;
    [SerializeField] private RectTransform effectVisual;
    [SerializeField] private Vector2 effectOffset = Vector2.zero;

    private float fillRatio;

    private void Awake()
    {
        ResolveReferences();
        ApplyLayout();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyLayout();
    }

    private void LateUpdate()
    {
        if (refreshLayoutEveryFrame)
        {
            ApplyLayout();
        }
    }

    public void SetFill(float normalizedValue)
    {
        fillRatio = Mathf.Clamp01(normalizedValue);
        ResolveReferences();
        ApplyLayout();
    }

    private void ResolveReferences()
    {
        if (fillViewport == null)
        {
            fillViewport = FindChildRectTransform("FillViewport");
        }

        if (fill == null && fillViewport != null)
        {
            fill = FindDirectChildRectTransform(fillViewport, "Fill");
        }

        if (effectAnchor == null && fillViewport != null)
        {
            effectAnchor = FindDirectChildRectTransform(fillViewport, "EffectAnchor");
        }

        if (effectAnchor == null && fill != null)
        {
            effectAnchor = FindDirectChildRectTransform(fill, "EffectAnchor");
        }

        if (effectVisual == null && effectAnchor != null)
        {
            effectVisual = FindDirectChildRectTransform(effectAnchor, "InkBarEffectVisual");
        }
    }

    private RectTransform FindChildRectTransform(string childName)
    {
        RectTransform[] children = GetComponentsInChildren<RectTransform>(includeInactive: true);
        foreach (RectTransform child in children)
        {
            if (child != null && child.transform != transform && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static RectTransform FindDirectChildRectTransform(RectTransform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        return child as RectTransform;
    }

    private void ApplyLayout()
    {
        if (fillViewport == null)
        {
            return;
        }

        float viewportHeight = Mathf.Max(0f, fillViewport.rect.height);
        float fillHeight = viewportHeight * fillRatio;
        ApplyFillLayout(fillHeight);

        if (effectPresentationMode == EffectPresentationMode.RevealThroughFill)
        {
            ApplyRevealThroughFillLayout(viewportHeight);
            return;
        }

        ApplyFollowFillTipLayout(fillHeight, viewportHeight);
    }

    private void ApplyFillLayout(float fillHeight)
    {
        if (fill == null)
        {
            return;
        }

        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(1f, 0f);
        fill.pivot = new Vector2(0.5f, 0f);
        fill.anchoredPosition = Vector2.zero;
        fill.sizeDelta = new Vector2(0f, fillHeight);
    }

    private void ApplyFollowFillTipLayout(float fillHeight, float viewportHeight)
    {
        if (effectAnchor != null)
        {
            effectAnchor.anchorMin = new Vector2(0f, 0f);
            effectAnchor.anchorMax = new Vector2(1f, 0f);
            effectAnchor.pivot = new Vector2(0.5f, 0f);
            effectAnchor.sizeDelta = Vector2.zero;
            effectAnchor.anchoredPosition = new Vector2(0f, ResolveEffectAnchorY(fillHeight, viewportHeight));
        }

        if (effectVisual != null)
        {
            effectVisual.anchorMin = new Vector2(0.5f, 0f);
            effectVisual.anchorMax = new Vector2(0.5f, 0f);
            effectVisual.pivot = new Vector2(0.5f, 0f);
            effectVisual.anchoredPosition = effectOffset;
        }
    }

    private void ApplyRevealThroughFillLayout(float viewportHeight)
    {
        if (effectAnchor == null)
        {
            return;
        }

        effectAnchor.anchorMin = new Vector2(0f, 0f);
        effectAnchor.anchorMax = new Vector2(1f, 0f);
        effectAnchor.pivot = new Vector2(0.5f, 0f);
        effectAnchor.sizeDelta = new Vector2(0f, viewportHeight);
        effectAnchor.anchoredPosition = Vector2.zero;
    }

    private float ResolveEffectAnchorY(float fillHeight, float viewportHeight)
    {
        if (fillRatio <= EmptyThreshold)
        {
            return -GetEffectVisualHeight();
        }

        if (fillRatio >= FullThreshold)
        {
            return viewportHeight;
        }

        return fillHeight;
    }

    private float GetEffectVisualHeight()
    {
        return effectVisual != null
            ? Mathf.Abs(effectVisual.rect.height)
            : 0f;
    }
}
