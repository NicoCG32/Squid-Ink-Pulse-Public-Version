using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class MobileBackgroundCover : MonoBehaviour
{
    private Vector2 lastSize;
    private Sprite lastSprite;
    private bool hasApplied;

    private void OnEnable() { hasApplied=false; Refresh(); }
    private void LateUpdate() { Refresh(); }

    public void Apply(Vector2 availableSize,bool isMobilePlatform)
    {
        Image image=GetComponent<Image>();
        if (!isMobilePlatform || image.sprite==null || availableSize.x<=0f || availableSize.y<=0f
            || float.IsNaN(availableSize.x) || float.IsNaN(availableSize.y)
            || float.IsInfinity(availableSize.x) || float.IsInfinity(availableSize.y)) return;
        Vector2 source=image.sprite.rect.size;
        if (source.x<=0f || source.y<=0f) return;
        float scale=Mathf.Max(availableSize.x/source.x,availableSize.y/source.y);
        RectTransform rect=(RectTransform)transform;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);
        rect.anchoredPosition=Vector2.zero;
        rect.sizeDelta=source*scale;
        rect.localScale=Vector3.one;
        image.preserveAspect=true;
        lastSize=availableSize; lastSprite=image.sprite; hasApplied=true;
    }

    private void Refresh()
    {
        if (!Application.isMobilePlatform || transform.parent is not RectTransform parent) return;
        Sprite sprite=GetComponent<Image>().sprite;
        if (!hasApplied || parent.rect.size!=lastSize || sprite!=lastSprite) Apply(parent.rect.size,true);
    }
}
