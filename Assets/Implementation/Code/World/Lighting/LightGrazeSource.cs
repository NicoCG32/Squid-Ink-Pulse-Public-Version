using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LightGrazeSource : MonoBehaviour
{
    private static readonly List<LightGrazeSource> activeSources = new();

    private Collider2D[] colliders;
    private Renderer[] renderers;

    public static int ActiveSourceCount => activeSources.Count;

    public static LightGrazeSource EnsureOn(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        LightGrazeSource source = target.GetComponent<LightGrazeSource>();
        return source != null ? source : target.AddComponent<LightGrazeSource>();
    }

    public static LightGrazeSource GetActiveSource(int index)
    {
        return index >= 0 && index < activeSources.Count
            ? activeSources[index]
            : null;
    }

    public Vector3 GetClosestPoint(Vector3 worldPosition)
    {
        RefreshGeometryIfNeeded();

        bool hasPoint = false;
        Vector3 closestPoint = transform.position;
        float closestSqrDistance = float.PositiveInfinity;

        if (colliders != null)
        {
            foreach (Collider2D sourceCollider in colliders)
            {
                if (sourceCollider == null || !sourceCollider.enabled)
                {
                    continue;
                }

                Vector3 candidate = sourceCollider.ClosestPoint(worldPosition);
                float sqrDistance = (candidate - worldPosition).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    hasPoint = true;
                    closestPoint = candidate;
                    closestSqrDistance = sqrDistance;
                }
            }
        }

        if (!hasPoint && renderers != null)
        {
            foreach (Renderer sourceRenderer in renderers)
            {
                if (sourceRenderer == null || !sourceRenderer.enabled)
                {
                    continue;
                }

                Vector3 candidate = sourceRenderer.bounds.ClosestPoint(worldPosition);
                float sqrDistance = (candidate - worldPosition).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    hasPoint = true;
                    closestPoint = candidate;
                    closestSqrDistance = sqrDistance;
                }
            }
        }

        return hasPoint ? closestPoint : transform.position;
    }

    private void Awake()
    {
        RefreshGeometry();
    }

    private void OnEnable()
    {
        RefreshGeometry();

        if (!activeSources.Contains(this))
        {
            activeSources.Add(this);
        }
    }

    private void OnDisable()
    {
        activeSources.Remove(this);
    }

    private void RefreshGeometryIfNeeded()
    {
        if (colliders == null || renderers == null)
        {
            RefreshGeometry();
        }
    }

    private void RefreshGeometry()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<Renderer>();
    }
}
