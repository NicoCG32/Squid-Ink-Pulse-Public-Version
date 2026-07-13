using System;
using UnityEngine;

[Serializable]
public class JellyfishEnemyTuning
{
    [SerializeField, Min(0f)] private float upwardSpeed = 0.45f;
    [SerializeField, Min(0f)] private float bounceVerticalVelocity = 12f;
    [SerializeField, Min(0f)] private float bounceDuration = 0.25f;
    [SerializeField, Min(0f)] private float bounceCooldown = 0.2f;

    public float UpwardSpeed => Mathf.Max(0f, upwardSpeed);
    public float BounceVerticalVelocity => Mathf.Max(0f, bounceVerticalVelocity);
    public float BounceDuration => Mathf.Max(0f, bounceDuration);
    public float BounceCooldown => Mathf.Max(0f, bounceCooldown);
}
