using System;
using UnityEngine;

[Serializable]
public class FishingRodEnemyTuning
{
    [SerializeField, Min(0.01f)] private float dropSpeed = 10f;
    [SerializeField, Min(0f)] private float startYOffsetBelowTopBoundary = 0.15f;
    [SerializeField, Min(0f)] private float descentStartViewportX = 1.05f;
    [SerializeField, Min(0f)] private float descentWindupSeconds = 0.15f;
    [SerializeField] private bool enableFastPaceHorizontalHold = true;
    [SerializeField, Min(0f)] private float horizontalHoldMinScrollSpeed = 10f;
    [SerializeField, Min(0f)] private float horizontalHoldViewportX = 1.05f;
    [SerializeField, Min(0.001f)] private float arriveDistance = 0.03f;
    [SerializeField, Min(0f)] private float horizontalLeadTimePaddingSeconds = 0.25f;
    [SerializeField, Min(0f)] private float minimumHorizontalLeadDistance = 2f;

    public float DropSpeed => Mathf.Max(0.01f, dropSpeed);
    public float StartYOffsetBelowTopBoundary => Mathf.Max(0f, startYOffsetBelowTopBoundary);
    public float DescentStartViewportX => Mathf.Max(0f, descentStartViewportX);
    public float DescentWindupSeconds => Mathf.Max(0f, descentWindupSeconds);
    public bool EnableFastPaceHorizontalHold => enableFastPaceHorizontalHold;
    public float HorizontalHoldMinScrollSpeed => Mathf.Max(0f, horizontalHoldMinScrollSpeed);
    public float HorizontalHoldViewportX => Mathf.Max(0f, horizontalHoldViewportX);
    public float ArriveDistance => Mathf.Max(0.001f, arriveDistance);
    public float HorizontalLeadTimePaddingSeconds => Mathf.Max(0f, horizontalLeadTimePaddingSeconds);
    public float MinimumHorizontalLeadDistance => Mathf.Max(0f, minimumHorizontalLeadDistance);
}
