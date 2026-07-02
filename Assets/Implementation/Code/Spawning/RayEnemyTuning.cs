using System;
using UnityEngine;

[Serializable]
public class RayEnemyTuning
{
    [SerializeField, Min(0f)] private float horizontalSpeed = 1.8f;
    [SerializeField, Min(0f)] private float verticalSpeed = 1.1f;

    public float HorizontalSpeed => Mathf.Max(0f, horizontalSpeed);
    public float VerticalSpeed => Mathf.Max(0f, verticalSpeed);
}
