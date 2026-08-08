using System.Collections.Generic;

public static class OptionsResolutionPolicy
{
    public static bool SupportsResolutionSelection(bool isMobilePlatform)
    {
        return !isMobilePlatform;
    }

    public static DisplayResolutionOption ResolveMobileLandscapeResolution(int width, int height)
    {
        int safeWidth = ClampDimension(width);
        int safeHeight = ClampDimension(height);
        return new DisplayResolutionOption(
            System.Math.Max(safeWidth, safeHeight),
            System.Math.Min(safeWidth, safeHeight));
    }

    public static DisplayResolutionOption[] BuildUniqueResolutionList(
        IReadOnlyList<DisplayResolutionOption> source,
        DisplayResolutionOption fallback)
    {
        if (source == null || source.Count == 0)
        {
            return new[]
            {
                new DisplayResolutionOption(
                    ClampDimension(fallback.Width),
                    ClampDimension(fallback.Height))
            };
        }

        List<DisplayResolutionOption> unique = new List<DisplayResolutionOption>();
        for (int i = 0; i < source.Count; i++)
        {
            DisplayResolutionOption candidate = source[i];
            int existingIndex = unique.FindIndex(resolution =>
                resolution.Width == candidate.Width
                && resolution.Height == candidate.Height);

            if (existingIndex >= 0)
            {
                unique[existingIndex] = candidate;
                continue;
            }

            unique.Add(candidate);
        }

        unique.Sort((left, right) =>
        {
            int widthComparison = left.Width.CompareTo(right.Width);
            return widthComparison != 0
                ? widthComparison
                : left.Height.CompareTo(right.Height);
        });

        return unique.ToArray();
    }

    public static int ResolvePreferredIndex(
        IReadOnlyList<DisplayResolutionOption> resolutions,
        bool hasSavedSize,
        int savedWidth,
        int savedHeight,
        bool hasLegacyIndex,
        int legacyIndex,
        DisplayResolutionOption fallback)
    {
        if (resolutions == null || resolutions.Count == 0)
        {
            return 0;
        }

        if (!hasSavedSize && hasLegacyIndex)
        {
            return ClampIndex(legacyIndex, resolutions.Count);
        }

        int targetWidth = hasSavedSize ? savedWidth : ClampDimension(fallback.Width);
        int targetHeight = hasSavedSize ? savedHeight : ClampDimension(fallback.Height);
        return FindClosestResolutionIndex(resolutions, targetWidth, targetHeight);
    }

    public static int FindClosestResolutionIndex(
        IReadOnlyList<DisplayResolutionOption> resolutions,
        int targetWidth,
        int targetHeight)
    {
        if (resolutions == null || resolutions.Count == 0)
        {
            return 0;
        }

        int closestIndex = 0;
        int closestDistance = int.MaxValue;
        for (int i = 0; i < resolutions.Count; i++)
        {
            int distance = System.Math.Abs(resolutions[i].Width - targetWidth)
                + System.Math.Abs(resolutions[i].Height - targetHeight);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public static bool IsValidResolutionIndex(IReadOnlyList<DisplayResolutionOption> resolutions, int resolutionIndex)
    {
        return resolutions != null
            && resolutionIndex >= 0
            && resolutionIndex < resolutions.Count;
    }

    private static int ClampIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return 0;
        }

        return index >= count ? count - 1 : index;
    }

    private static int ClampDimension(int value)
    {
        return value < 1 ? 1 : value;
    }
}
