using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PufferfishEnemyTuning
{
    [SerializeField, Min(0f)] private float fallSpeed = 0.55f;
    [FormerlySerializedAs("expandedRiseSpeedMultiplier")]
    [SerializeField, Min(0f)] private float expandedSpeedMultiplier = 2.2f;
    [SerializeField, Min(0f)] private float proximityRadius = 2.5f;
    [SerializeField, Min(1f)] private float expandedScaleMultiplier = 2f;
    [SerializeField, Min(0f)] private float expansionSmoothSpeed = 8f;
    [SerializeField, Min(0f)] private float erraticDirectionChangeIntervalMin = 1.2f;
    [SerializeField, Min(0f)] private float erraticDirectionChangeIntervalMax = 2.6f;
    [SerializeField, Range(0f, 1f)] private float erraticDirectionChangeChance = 0.45f;

    public float FallSpeed => Mathf.Max(0f, fallSpeed);
    public float ExpandedSpeedMultiplier => Mathf.Max(0f, expandedSpeedMultiplier);
    public float ProximityRadius => Mathf.Max(0f, proximityRadius);
    public float ExpandedScaleMultiplier => Mathf.Max(1f, expandedScaleMultiplier);
    public float ExpansionSmoothSpeed => Mathf.Max(0f, expansionSmoothSpeed);
    public float ErraticDirectionChangeIntervalMin => Mathf.Max(0f, erraticDirectionChangeIntervalMin);
    public float ErraticDirectionChangeIntervalMax => Mathf.Max(ErraticDirectionChangeIntervalMin, erraticDirectionChangeIntervalMax);
    public float ErraticDirectionChangeChance => Mathf.Clamp01(erraticDirectionChangeChance);
}
