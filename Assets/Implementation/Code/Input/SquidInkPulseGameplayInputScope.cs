using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class SquidInkPulseGameplayInputScope : MonoBehaviour
{
    private void OnEnable()
    {
        SquidInkPulseInputRuntime.ActivateGameplayScope(this);
    }

    private void OnDisable()
    {
        SquidInkPulseInputRuntime.DeactivateGameplayScope(this);
    }
}
