using System;
using UnityEngine;

[Serializable]
public class JellyfishEnemyTuning
{
    [SerializeField, Min(0f)] private float upwardSpeed = 0.45f;

    public float UpwardSpeed => Mathf.Max(0f, upwardSpeed);
}
