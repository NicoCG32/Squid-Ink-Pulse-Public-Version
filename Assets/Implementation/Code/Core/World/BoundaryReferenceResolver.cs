using UnityEngine;

public static class BoundaryReferenceResolver
{
    public const string BoundaryGroupName = "Boundaries";
    public const string CameraBoundaryRootName = "CameraBoundaries";
    public const string PlayerBoundaryRootName = "PlayerBoundaries";
    public const string TopBoundaryName = "TopBoundary";
    public const string BottomBoundaryName = "BottomBoundary";

    public static bool TryResolve(
        BoundaryReferenceDomain domain,
        out Collider2D topBorder,
        out Collider2D bottomBorder)
    {
        Transform root = FindBoundaryRoot(domain);
        topBorder = root != null ? FindDirectChildCollider(root, TopBoundaryName) : null;
        bottomBorder = root != null ? FindDirectChildCollider(root, BottomBoundaryName) : null;

        return topBorder != null && bottomBorder != null;
    }

    public static bool TryResolveInnerVerticalRange(
        BoundaryReferenceDomain domain,
        float inset,
        out Vector2 range)
    {
        range = default;
        if (!TryResolve(domain, out Collider2D topBorder, out Collider2D bottomBorder))
        {
            return false;
        }

        float safeInset = Mathf.Max(0f, inset);
        float minY = bottomBorder.bounds.max.y + safeInset;
        float maxY = topBorder.bounds.min.y - safeInset;

        if (minY > maxY)
        {
            return false;
        }

        range = new Vector2(minY, maxY);
        return true;
    }

    public static string GetRequiredHierarchyDescription(BoundaryReferenceDomain domain)
    {
        string rootName = GetBoundaryRootName(domain);
        return $"{BoundaryGroupName}/{rootName}/{TopBoundaryName} y {BoundaryGroupName}/{rootName}/{BottomBoundaryName}";
    }

    private static Transform FindBoundaryRoot(BoundaryReferenceDomain domain)
    {
        string rootName = GetBoundaryRootName(domain);
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate.name != BoundaryGroupName)
            {
                continue;
            }

            Transform root = candidate.Find(rootName);
            if (root != null)
            {
                return root;
            }
        }

        return null;
    }

    private static string GetBoundaryRootName(BoundaryReferenceDomain domain)
    {
        return domain == BoundaryReferenceDomain.Camera
            ? CameraBoundaryRootName
            : PlayerBoundaryRootName;
    }

    private static Collider2D FindDirectChildCollider(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        return child != null ? child.GetComponent<Collider2D>() : null;
    }
}
