using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ShrimpCollector : MonoBehaviour
{
    [SerializeField] private GameSessionController session;

    public int ShrimpCount => shrimpCount;
    public event Action<int> ShrimpsChanged;

    private int shrimpCount;

    private void Awake()
    {
        ResolveReferences();
        shrimpCount = ShrimpRuntimeWallet.TotalShrimp;
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        ShrimpRuntimeWallet.TotalChanged += HandleRuntimeShrimpChanged;
        HandleRuntimeShrimpChanged(ShrimpRuntimeWallet.TotalShrimp);
    }

    private void OnDisable()
    {
        ShrimpRuntimeWallet.TotalChanged -= HandleRuntimeShrimpChanged;
    }

    public void Collect(GameObject shrimpObject)
    {
        ResolveReferences();

        if (session == null || !session.IsPlaying)
        {
            return;
        }

        int collectedAmount = 1;
        if (shrimpObject != null && shrimpObject.TryGetComponent(out ShrimpValue value))
        {
            collectedAmount = value.Amount;
        }

        ShrimpRuntimeWallet.Add(collectedAmount);

        if (shrimpObject != null)
        {
            Destroy(shrimpObject);
        }
    }

    private void HandleRuntimeShrimpChanged(int totalShrimp)
    {
        shrimpCount = totalShrimp;
        ShrimpsChanged?.Invoke(shrimpCount);
    }

    private void WarnIfMissingReferences()
    {
        if (session == null)
        {
            Debug.LogWarning("[ShrimpCollector] Falta asignar GameSessionController en el Inspector.", this);
        }
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }
    }
}
