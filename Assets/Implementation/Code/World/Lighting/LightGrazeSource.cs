using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LightGrazeSource : MonoBehaviour
{
    private const string MaskObjectName = "LightGrazeMask";
    private const string FeatherObjectName = "LightGrazeFeather";

    private static readonly List<LightGrazeSource> activeSources = new();

    private SpriteMask lightMask;
    private Transform lightMaskTransform;
    private SpriteRenderer lightFeather;
    private Transform lightFeatherTransform;

    public static int ActiveSourceCount => activeSources.Count;

    public static LightGrazeSource EnsureOn(GameObject target)
    {
        if (target == null || !ZoneLightingController.HasInstance)
        {
            return null;
        }

        LightGrazeSource source = target.GetComponent<LightGrazeSource>();
        return source != null ? source : target.AddComponent<LightGrazeSource>();
    }

    public static void RefreshAllActiveSources()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            LightGrazeSource source = activeSources[i];
            if (source == null)
            {
                activeSources.RemoveAt(i);
                continue;
            }

            source.RefreshMask();
        }
    }

    public static void CollectActiveWorldPositions(List<Vector3> results)
    {
        CollectActiveWorldPositions(results, useBounds: false, default);
    }

    public static void CollectActiveWorldPositions(List<Vector3> results, Rect worldBounds)
    {
        CollectActiveWorldPositions(results, useBounds: true, worldBounds);
    }

    private static void CollectActiveWorldPositions(List<Vector3> results, bool useBounds, Rect worldBounds)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            LightGrazeSource source = activeSources[i];
            if (source == null)
            {
                activeSources.RemoveAt(i);
                continue;
            }

            if (!source.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 position = source.transform.position;
            if (useBounds
                && (position.x < worldBounds.xMin
                    || position.x > worldBounds.xMax
                    || position.y < worldBounds.yMin
                    || position.y > worldBounds.yMax))
            {
                continue;
            }

            results.Add(position);
        }
    }

    private void OnEnable()
    {
        if (!activeSources.Contains(this))
        {
            activeSources.Add(this);
        }

        RefreshMask();
    }

    private void OnDisable()
    {
        activeSources.Remove(this);
        SetMaskEnabled(false);
        SetFeatherEnabled(false);
    }

    private void LateUpdate()
    {
        RefreshMask();
    }

    private void RefreshMask()
    {
        if (!ZoneLightingController.HasInstance || !isActiveAndEnabled)
        {
            SetMaskEnabled(false);
            SetFeatherEnabled(false);
            return;
        }

        ZoneLightingController controller = ZoneLightingController.Instance;
        if (controller.UsesCompositeLightOverlay)
        {
            SetMaskEnabled(false);
            SetFeatherEnabled(false);
            return;
        }

        EnsureMaskObject();
        controller.ConfigureLightMask(lightMask);
        SetMaskEnabled(true);
        FitMaskRadius(controller.LightHoleRadius);

        if (!controller.UsesLightFeather)
        {
            SetFeatherEnabled(false);
            return;
        }

        EnsureFeatherObject();
        controller.ConfigureLightFeather(lightFeather);
        SetFeatherEnabled(true);
        FitFeatherRadius(controller.LightHoleRadius);
    }

    private void EnsureMaskObject()
    {
        if (lightMask != null)
        {
            return;
        }

        Transform existingMask = transform.Find(MaskObjectName);
        if (existingMask != null && existingMask.TryGetComponent(out lightMask))
        {
            lightMaskTransform = existingMask;
            return;
        }

        GameObject maskObject = new(MaskObjectName);
        lightMaskTransform = maskObject.transform;
        lightMaskTransform.SetParent(transform, worldPositionStays: false);
        lightMaskTransform.localPosition = Vector3.zero;
        lightMaskTransform.localRotation = Quaternion.identity;
        lightMask = maskObject.AddComponent<SpriteMask>();
    }

    private void FitMaskRadius(float radius)
    {
        FitRadius(lightMaskTransform, lightMask != null ? lightMask.sprite : null, radius);
    }

    private void EnsureFeatherObject()
    {
        if (lightFeather != null)
        {
            return;
        }

        Transform existingFeather = transform.Find(FeatherObjectName);
        if (existingFeather != null && existingFeather.TryGetComponent(out lightFeather))
        {
            lightFeatherTransform = existingFeather;
            return;
        }

        GameObject featherObject = new(FeatherObjectName);
        lightFeatherTransform = featherObject.transform;
        lightFeatherTransform.SetParent(transform, worldPositionStays: false);
        lightFeatherTransform.localPosition = Vector3.zero;
        lightFeatherTransform.localRotation = Quaternion.identity;
        lightFeather = featherObject.AddComponent<SpriteRenderer>();
    }

    private void FitFeatherRadius(float radius)
    {
        FitRadius(lightFeatherTransform, lightFeather != null ? lightFeather.sprite : null, radius);
    }

    private void FitRadius(Transform targetTransform, Sprite sprite, float radius)
    {
        if (targetTransform == null || sprite == null)
        {
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        float diameter = radius * 2f;
        Vector3 lossyScale = transform.lossyScale;
        float parentScaleX = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x));
        float parentScaleY = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));

        targetTransform.localPosition = Vector3.zero;
        targetTransform.localRotation = Quaternion.identity;
        targetTransform.localScale = new Vector3(
            diameter / Mathf.Max(0.0001f, spriteSize.x) / parentScaleX,
            diameter / Mathf.Max(0.0001f, spriteSize.y) / parentScaleY,
            1f);
    }

    private void SetMaskEnabled(bool enabled)
    {
        if (lightMask != null)
        {
            lightMask.enabled = enabled;
        }
    }

    private void SetFeatherEnabled(bool enabled)
    {
        if (lightFeather != null)
        {
            lightFeather.enabled = enabled;
        }
    }
}
