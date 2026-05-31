using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ShrimpCollector : MonoBehaviour
{
    [SerializeField] private GameSessionController session;
    [SerializeField] private int shrimpCount;

    public int ShrimpCount => shrimpCount;
    public event Action<int> ShrimpsChanged;

    private void Awake()
    {
        WarnIfMissingReferences();
    }

    public void Collect(GameObject shrimpObject)
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

        int collectedAmount = 1;
        if (shrimpObject != null && shrimpObject.TryGetComponent(out ShrimpValue value))
        {
            collectedAmount = value.Amount;
        }

        shrimpCount += collectedAmount;
        ShrimpsChanged?.Invoke(shrimpCount);

        if (shrimpObject != null)
        {
            Destroy(shrimpObject);
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null)
        {
            Debug.LogWarning("[ShrimpCollector] Falta asignar GameSessionController en el Inspector.", this);
        }
    }
}
