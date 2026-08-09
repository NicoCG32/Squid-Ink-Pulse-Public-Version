using UnityEngine;

public readonly struct SafeAreaAnchors
{
    public SafeAreaAnchors(Vector2 minimum, Vector2 maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public Vector2 Minimum { get; }
    public Vector2 Maximum { get; }
}

public static class SafeAreaAnchorPolicy
{
    public static SafeAreaAnchors Resolve(Rect safeArea, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f)
        {
            return new SafeAreaAnchors(Vector2.zero, Vector2.one);
        }

        Vector2 minimum = new(
            Mathf.Clamp01(safeArea.xMin / screenSize.x),
            Mathf.Clamp01(safeArea.yMin / screenSize.y));
        Vector2 maximum = new(
            Mathf.Clamp01(safeArea.xMax / screenSize.x),
            Mathf.Clamp01(safeArea.yMax / screenSize.y));

        if (maximum.x < minimum.x || maximum.y < minimum.y)
        {
            return new SafeAreaAnchors(Vector2.zero, Vector2.one);
        }

        return new SafeAreaAnchors(minimum, maximum);
    }
}
