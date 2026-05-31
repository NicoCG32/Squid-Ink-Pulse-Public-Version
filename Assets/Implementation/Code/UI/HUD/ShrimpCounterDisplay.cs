using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ShrimpCounterDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private string prefix = string.Empty;

    private void Awake()
    {
        ResolveTextReference();
    }

    private void OnEnable()
    {
        ShrimpRuntimeWallet.TotalChanged += Refresh;
        Refresh(ShrimpRuntimeWallet.TotalShrimp);
    }

    private void OnDisable()
    {
        ShrimpRuntimeWallet.TotalChanged -= Refresh;
    }

    private void Refresh(int amount)
    {
        ResolveTextReference();

        if (amountText != null)
        {
            amountText.text = $"{prefix}{amount}";
        }
    }

    private void ResolveTextReference()
    {
        if (amountText == null)
        {
            amountText = GetComponentInChildren<TMP_Text>(includeInactive: true);
        }
    }
}
