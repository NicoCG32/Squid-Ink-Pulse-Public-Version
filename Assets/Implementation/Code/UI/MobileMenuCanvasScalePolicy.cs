using System;
using UnityEngine;

public static class MobileMenuCanvasScalePolicy
{
    public static float ResolveMatch(bool isMobilePlatform, int width, int height,
        Vector2 referenceResolution, float authoredMatch)
    {
        if (!isMobilePlatform || width <= 0 || height <= 0
            || referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            return authoredMatch;

        // The product runs in landscape, even if the initial surface still
        // reports portrait dimensions during Android's orientation transition.
        // Cross-products avoid rounding disagreement at an exact 16:9 ratio.
        double screenRatio = (double)Math.Max(width, height) * referenceResolution.y;
        double referenceRatio = (double)Math.Min(width, height) * referenceResolution.x;
        return screenRatio >= referenceRatio ? 1f : 0f;
    }
}
