using System;
using UnityEngine;

public enum BoundaryReferenceDomain
{
    Camera,
    Player
}

public static class BoundaryReferenceResolver
{
    public const string CameraBoundaryTag = "CameraBoundary";
    public const string PlayerBoundaryTag = "PlayerBoundary";
    public const string TopBorderTag = "TopBorder";
    public const string BottomBorderTag = "BottomBorder";

    private const string CameraBoundaryRootName = "CameraBoundaries";
    private const string PlayerBoundaryRootName = "PlayerBoundaries";
    private const string TopBoundaryName = "TopBoundary";
    private const string BottomBoundaryName = "BottomBoundary";

    public static bool TryResolve(
        BoundaryReferenceDomain domain,
        out Collider2D topBorder,
        out Collider2D bottomBorder)
    {
        Transform root = FindBoundaryRoot(domain);
        topBorder = root != null ? FindBorderCollider(root, TopBorderTag, TopBoundaryName) : null;
        bottomBorder = root != null ? FindBorderCollider(root, BottomBorderTag, BottomBoundaryName) : null;

        return topBorder != null && bottomBorder != null;
    }

    private static Transform FindBoundaryRoot(BoundaryReferenceDomain domain)
    {
        string tag = domain == BoundaryReferenceDomain.Camera ? CameraBoundaryTag : PlayerBoundaryTag;
        string fallbackName = domain == BoundaryReferenceDomain.Camera ? CameraBoundaryRootName : PlayerBoundaryRootName;

        GameObject taggedRoot = FindFirstTaggedObject(tag);
        if (taggedRoot != null)
        {
            return taggedRoot.transform;
        }

        GameObject namedRoot = GameObject.Find(fallbackName);
        return namedRoot != null ? namedRoot.transform : null;
    }

    private static GameObject FindFirstTaggedObject(string tag)
    {
        try
        {
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject candidate in candidates)
            {
                if (candidate != null && candidate.activeInHierarchy)
                {
                    return candidate;
                }
            }

            return candidates.Length > 0 ? candidates[0] : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static Collider2D FindBorderCollider(Transform root, string tag, string fallbackName)
    {
        Collider2D fallback = null;
        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(includeInactive: true);

        foreach (Collider2D candidate in colliders)
        {
            if (candidate == null)
            {
                continue;
            }

            bool tagMatches = candidate.gameObject.tag == tag;
            bool nameMatches = candidate.gameObject.name.Equals(fallbackName, StringComparison.OrdinalIgnoreCase);

            if (!tagMatches && !nameMatches)
            {
                continue;
            }

            if (candidate.enabled && candidate.gameObject.activeInHierarchy)
            {
                return candidate;
            }

            fallback ??= candidate;
        }

        return fallback;
    }
}
