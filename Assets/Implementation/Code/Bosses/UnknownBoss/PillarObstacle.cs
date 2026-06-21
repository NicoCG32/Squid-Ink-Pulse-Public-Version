using System.Collections;
using UnityEngine;

public class PillarObstacle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topPillar;
    [SerializeField] private Transform bottomPillar;

    [Header("Settings")]
    [SerializeField] private float lifetime = 15f; 

    private Vector3 topPillarBaseScale = Vector3.one;
    private Vector3 bottomPillarBaseScale = Vector3.one;
    private bool hasCapturedBaseScales;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Setup(
        float gapCenterY,
        float gapSize,
        float boundaryTopY,
        float boundaryBottomY,
        float revealDuration,
        float topRevealDelay,
        float bottomRevealDelay)
    {
        if (topPillar == null || bottomPillar == null) return;

        CaptureBaseScales();
        StopAllCoroutines();

        float halfGap = gapSize / 2f;
        
        float gapTopEdge = gapCenterY + halfGap;
        float gapBottomEdge = gapCenterY - halfGap;

        float topHeight = Mathf.Max(0f, boundaryTopY - gapTopEdge);
        float bottomHeight = Mathf.Max(0f, gapBottomEdge - boundaryBottomY);
        float safeRevealDuration = Mathf.Max(0f, revealDuration);
        
        SetPillarGeometry(topPillar, topPillarBaseScale, PillarAnchor.Top, boundaryTopY, 0f);
        SetPillarGeometry(bottomPillar, bottomPillarBaseScale, PillarAnchor.Bottom, boundaryBottomY, 0f);

        if (safeRevealDuration <= 0f)
        {
            SetPillarGeometry(topPillar, topPillarBaseScale, PillarAnchor.Top, boundaryTopY, topHeight);
            SetPillarGeometry(bottomPillar, bottomPillarBaseScale, PillarAnchor.Bottom, boundaryBottomY, bottomHeight);
            return;
        }

        StartCoroutine(RevealPillar(
            topPillar,
            topPillarBaseScale,
            PillarAnchor.Top,
            boundaryTopY,
            topHeight,
            safeRevealDuration,
            Mathf.Max(0f, topRevealDelay)));
        StartCoroutine(RevealPillar(
            bottomPillar,
            bottomPillarBaseScale,
            PillarAnchor.Bottom,
            boundaryBottomY,
            bottomHeight,
            safeRevealDuration,
            Mathf.Max(0f, bottomRevealDelay)));
    }

    private IEnumerator RevealPillar(
        Transform pillar,
        Vector3 baseScale,
        PillarAnchor anchor,
        float boundaryY,
        float targetHeight,
        float revealDuration,
        float revealDelay)
    {
        if (revealDelay > 0f)
        {
            yield return new WaitForSeconds(revealDelay);
        }

        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / revealDuration);
            SetPillarGeometry(pillar, baseScale, anchor, boundaryY, targetHeight * t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetPillarGeometry(pillar, baseScale, anchor, boundaryY, targetHeight);
    }

    private void SetPillarGeometry(
        Transform pillar,
        Vector3 baseScale,
        PillarAnchor anchor,
        float boundaryY,
        float height)
    {
        if (pillar == null)
        {
            return;
        }

        float safeHeight = Mathf.Max(0f, height);
        float unitHeight = ResolveAuthoredUnitHeight(pillar);
        float parentScaleY = pillar.parent != null ? Mathf.Abs(pillar.parent.lossyScale.y) : 1f;
        parentScaleY = Mathf.Max(0.01f, parentScaleY);
        float scaleSignY = Mathf.Sign(baseScale.y);
        if (Mathf.Approximately(scaleSignY, 0f))
        {
            scaleSignY = 1f;
        }

        float localScaleY = safeHeight / (unitHeight * parentScaleY);
        pillar.localScale = new Vector3(baseScale.x, localScaleY * scaleSignY, baseScale.z);

        float centerY = anchor == PillarAnchor.Top
            ? boundaryY - safeHeight * 0.5f
            : boundaryY + safeHeight * 0.5f;
        pillar.position = new Vector3(transform.position.x, centerY, pillar.position.z);
    }

    private void CaptureBaseScales()
    {
        if (hasCapturedBaseScales)
        {
            return;
        }

        if (topPillar != null)
        {
            topPillarBaseScale = topPillar.localScale;
        }

        if (bottomPillar != null)
        {
            bottomPillarBaseScale = bottomPillar.localScale;
        }

        hasCapturedBaseScales = true;
    }

    private float ResolveAuthoredUnitHeight(Transform pillar)
    {
        if (pillar.TryGetComponent(out SpriteRenderer renderer) && renderer.sprite != null)
        {
            return Mathf.Max(0.01f, renderer.sprite.bounds.size.y);
        }

        if (pillar.TryGetComponent(out BoxCollider2D boxCollider))
        {
            return Mathf.Max(0.01f, boxCollider.size.y);
        }

        return 1f;
    }

    private enum PillarAnchor
    {
        Top,
        Bottom
    }
}
