using System.Collections;
using UnityEngine;

public static class MenuScreenAnimation
{
    public static RectTransform[] BuildElementList(RectTransform[] decorations, RectTransform[] buttons)
    {
        int count = CountValidElements(decorations) + CountValidElements(buttons);
        if (count == 0)
        {
            return new RectTransform[0];
        }

        RectTransform[] result = new RectTransform[count];
        int index = 0;
        CopyValidElements(decorations, result, ref index);
        CopyValidElements(buttons, result, ref index);
        return result;
    }

    public static Vector2[] CachePositions(RectTransform[] elements)
    {
        if (elements == null || elements.Length == 0)
        {
            return new Vector2[0];
        }

        Vector2[] positions = new Vector2[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] != null)
            {
                positions[i] = elements[i].anchoredPosition;
            }
        }

        return positions;
    }

    public static void PlaceElementsAtOffset(RectTransform[] elements, Vector2[] originalPositions, float horizontalOffset)
    {
        if (!CanAnimate(elements, originalPositions))
        {
            return;
        }

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] == null)
            {
                continue;
            }

            elements[i].anchoredPosition = GetOffsetPosition(originalPositions[i], horizontalOffset, i);
        }
    }

    public static void ResetElements(RectTransform[] elements, Vector2[] originalPositions)
    {
        if (!CanAnimate(elements, originalPositions))
        {
            return;
        }

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] != null)
            {
                elements[i].anchoredPosition = originalPositions[i];
            }
        }
    }

    public static Vector2 GetOffsetPosition(Vector2 originalPosition, float horizontalOffset, int elementIndex)
    {
        float direction = (elementIndex % 2 == 0) ? -1f : 1f;
        return originalPosition + new Vector2(horizontalOffset * direction, 0f);
    }

    public static IEnumerator FadeCanvas(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    public static IEnumerator SlideElement(RectTransform element, Vector2 targetPosition, float duration)
    {
        if (element == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            element.anchoredPosition = targetPosition;
            yield break;
        }

        Vector2 startPosition = element.anchoredPosition;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            element.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, EaseOutBack(t));
            yield return null;
        }

        element.anchoredPosition = targetPosition;
    }

    private static float EaseOutBack(float t)
    {
        float shifted = t - 1f;
        return 1f + 2.7f * shifted * shifted * shifted + 1.7f * shifted * shifted;
    }

    private static bool CanAnimate(RectTransform[] elements, Vector2[] originalPositions)
    {
        return elements != null && originalPositions != null && elements.Length == originalPositions.Length;
    }

    private static int CountValidElements(RectTransform[] elements)
    {
        if (elements == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private static void CopyValidElements(RectTransform[] source, RectTransform[] target, ref int targetIndex)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
            {
                continue;
            }

            target[targetIndex] = source[i];
            targetIndex++;
        }
    }
}
