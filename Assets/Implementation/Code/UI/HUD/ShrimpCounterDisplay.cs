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
            amountText.text = $"{prefix}{FormatShrimpAmount(amount)}";
        }
    }

    public static string FormatShrimpAmount(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount < 1000)
        {
            return safeAmount.ToString();
        }

        if (safeAmount < 1000000)
        {
            return FormatCompactAmount(safeAmount, 1000, 10000, "K");
        }

        return FormatCompactAmount(safeAmount, 1000000, 10000000, "M");
    }

    private static string FormatCompactAmount(int amount, int unit, int decimalCutoff, string suffix)
    {
        int whole = amount / unit;
        if (amount >= decimalCutoff)
        {
            return $"{whole}{suffix}";
        }

        int decimalDigit = (amount % unit) / (unit / 10);
        return decimalDigit > 0
            ? $"{whole}.{decimalDigit}{suffix}"
            : $"{whole}{suffix}";
    }

    private void ResolveTextReference()
    {
        if (amountText == null)
        {
            amountText = GetComponentInChildren<TMP_Text>(includeInactive: true);
        }
    }
}
