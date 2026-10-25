using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class LoreComicMobileLayout : MonoBehaviour
{
    [SerializeField] private Image comicImage;
    [SerializeField] private RectTransform continueRoot;
    private RectTransform content;
    private Vector2 authoredMin, authoredMax, authoredPivot, authoredPosition, authoredSize;
    private readonly Vector3[] buttonCorners = new Vector3[4];
    private Rect lastAvailable;
    private float lastBottom;
    private bool captured, applied;

    private void OnEnable() => Apply(Application.isMobilePlatform);
    private void LateUpdate() => Apply(Application.isMobilePlatform);

    public void Apply(bool mobile)
    {
        if (comicImage == null) return;
        RectTransform image = comicImage.rectTransform;
        if (!captured)
        {
            authoredMin = image.anchorMin; authoredMax = image.anchorMax;
            authoredPivot = image.pivot; authoredPosition = image.anchoredPosition; authoredSize = image.sizeDelta;
            captured = true;
        }
        if (!mobile)
        {
            if (!applied) return;
            image.anchorMin = authoredMin; image.anchorMax = authoredMax;
            image.pivot = authoredPivot; image.anchoredPosition = authoredPosition; image.sizeDelta = authoredSize;
            applied = false;
            return;
        }
        content ??= GetComponent<RectTransform>();
        if (content == null || content.rect.width <= 96f || content.rect.height <= 60f) return;
        Rect available = content.rect;
        float bottom = available.yMin + 30f;
        if (continueRoot != null && continueRoot.gameObject.activeSelf)
        {
            continueRoot.GetWorldCorners(buttonCorners);
            foreach (Vector3 corner in buttonCorners)
                bottom = Mathf.Max(bottom, content.InverseTransformPoint(corner).y + 24f);
        }
        float top = available.yMax - 30f;
        if (top <= bottom) return;
        if (applied && available == lastAvailable && Mathf.Approximately(bottom, lastBottom)) return;
        image.anchorMin = image.anchorMax = image.pivot = new Vector2(0.5f, 0.5f);
        image.anchoredPosition = new Vector2(available.center.x, (top + bottom) * 0.5f);
        image.sizeDelta = new Vector2(available.width - 96f, top - bottom);
        lastAvailable = available; lastBottom = bottom;
        applied = true;
    }
}
