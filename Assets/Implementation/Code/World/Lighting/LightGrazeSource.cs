using System.Collections.Generic;
using UnityEngine;

public readonly struct LightGrazeSample
{
    public LightGrazeSample(Vector3 position, Vector2 radiusScale)
    {
        Position = position;
        RadiusScale = radiusScale;
    }

    public Vector3 Position { get; }
    public Vector2 RadiusScale { get; }
}

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
    private Transform resolvedFallbackAnchor;
    private float nextFlickerSwitchTime;
    private bool flickerVisible = true;

    [Header("Shape")]
    [SerializeField] private Transform grazeAnchor;
    [SerializeField] private Vector2 lightShapeScale = Vector2.one;

    [Header("Flicker")]
    [SerializeField] private bool flickerEnabled;
    [SerializeField] private Vector2 flickerOnDurationRange = new(0.08f, 0.18f);
    [SerializeField] private Vector2 flickerOffDurationRange = new(0.05f, 0.2f);
    [SerializeField] private bool randomizeInitialFlicker = true;

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

    public static void CollectActiveSamples(List<LightGrazeSample> results, Rect worldBounds, float baseRadius)
    {
        CollectActiveSamples(results, useBounds: true, worldBounds, baseRadius);
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

            if (!source.IsEmittingLight)
            {
                continue;
            }

            Vector3 position = source.LightWorldPosition;
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

    private static void CollectActiveSamples(List<LightGrazeSample> results, bool useBounds, Rect worldBounds, float baseRadius)
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

            if (!source.isActiveAndEnabled || !source.IsEmittingLight)
            {
                continue;
            }

            Vector3 position = source.LightWorldPosition;
            Vector2 radiusScale = source.EffectiveShapeScale;
            if (useBounds)
            {
                float radiusX = Mathf.Abs(baseRadius * radiusScale.x);
                float radiusY = Mathf.Abs(baseRadius * radiusScale.y);
                if (position.x + radiusX < worldBounds.xMin
                    || position.x - radiusX > worldBounds.xMax
                    || position.y + radiusY < worldBounds.yMin
                    || position.y - radiusY > worldBounds.yMax)
                {
                    continue;
                }
            }

            results.Add(new LightGrazeSample(position, radiusScale));
        }
    }

    private Vector3 LightWorldPosition => ResolveAnchor().position;

    private Vector2 EffectiveShapeScale
    {
        get
        {
            float x = Mathf.Abs(lightShapeScale.x);
            float y = Mathf.Abs(lightShapeScale.y);
            return new Vector2(
                x > 0.0001f ? x : 1f,
                y > 0.0001f ? y : 1f);
        }
    }

    private bool IsEmittingLight => !flickerEnabled || flickerVisible;

    private void OnEnable()
    {
        if (!activeSources.Contains(this))
        {
            activeSources.Add(this);
        }

        ResetFlicker();
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
        TickFlicker();
        RefreshMask();
    }

    private void RefreshMask()
    {
        if (!ZoneLightingController.HasInstance || !isActiveAndEnabled || !IsEmittingLight)
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

        Transform anchor = ResolveAnchor();
        Transform existingMask = anchor.Find(MaskObjectName);
        if (existingMask != null && existingMask.TryGetComponent(out lightMask))
        {
            lightMaskTransform = existingMask;
            return;
        }

        GameObject maskObject = new(MaskObjectName);
        lightMaskTransform = maskObject.transform;
        lightMaskTransform.SetParent(anchor, worldPositionStays: false);
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

        Transform anchor = ResolveAnchor();
        Transform existingFeather = anchor.Find(FeatherObjectName);
        if (existingFeather != null && existingFeather.TryGetComponent(out lightFeather))
        {
            lightFeatherTransform = existingFeather;
            return;
        }

        GameObject featherObject = new(FeatherObjectName);
        lightFeatherTransform = featherObject.transform;
        lightFeatherTransform.SetParent(anchor, worldPositionStays: false);
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
        Transform anchor = ResolveAnchor();
        Vector3 lossyScale = anchor.lossyScale;
        float parentScaleX = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x));
        float parentScaleY = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));
        Vector2 shapeScale = EffectiveShapeScale;

        targetTransform.localPosition = Vector3.zero;
        targetTransform.localRotation = Quaternion.identity;
        targetTransform.localScale = new Vector3(
            diameter * shapeScale.x / Mathf.Max(0.0001f, spriteSize.x) / parentScaleX,
            diameter * shapeScale.y / Mathf.Max(0.0001f, spriteSize.y) / parentScaleY,
            1f);
    }

    private void ResetFlicker()
    {
        if (!flickerEnabled)
        {
            flickerVisible = true;
            nextFlickerSwitchTime = 0f;
            return;
        }

        flickerVisible = !randomizeInitialFlicker || Random.value >= 0.5f;
        ScheduleNextFlickerSwitch();
    }

    private void TickFlicker()
    {
        if (!flickerEnabled)
        {
            flickerVisible = true;
            return;
        }

        if (Time.time < nextFlickerSwitchTime)
        {
            return;
        }

        flickerVisible = !flickerVisible;
        ScheduleNextFlickerSwitch();
    }

    private void ScheduleNextFlickerSwitch()
    {
        Vector2 range = flickerVisible ? flickerOnDurationRange : flickerOffDurationRange;
        float min = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        nextFlickerSwitchTime = Time.time + Random.Range(min, max);
    }

    private Transform ResolveAnchor()
    {
        if (grazeAnchor != null)
        {
            return grazeAnchor;
        }

        if (resolvedFallbackAnchor != null)
        {
            return resolvedFallbackAnchor;
        }

        resolvedFallbackAnchor = FindDescendant(
            transform,
            "GrazeLightAnchor",
            "VisualSupport",
            "Roca",
            "Rock",
            "Visual");

        return resolvedFallbackAnchor != null ? resolvedFallbackAnchor : transform;
    }

    private static Transform FindDescendant(Transform root, params string[] names)
    {
        if (root == null || names == null || names.Length == 0)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
        {
            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                if (children[childIndex] != root && children[childIndex].name == names[nameIndex])
                {
                    return children[childIndex];
                }
            }
        }

        return null;
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
