using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InGameShopMobilePresentation : MonoBehaviour
{
    [SerializeField] private GameObject[] keyboardHints;
    [SerializeField] private GameObject mobileBuyLabel;
    [SerializeField] private GameObject mobileTimerRoot;
    [SerializeField] private TMP_Text priceText;
    private bool[] authoredHintStates;
    private bool authoredAutoSize;
    private TextWrappingModes authoredWrapping;
    private float authoredFontSize, authoredMinSize, authoredMaxSize;

    private void OnEnable() => Apply(Application.isMobilePlatform);

    public void Apply(bool mobile)
    {
        if (authoredHintStates == null)
        {
            authoredHintStates = new bool[keyboardHints != null ? keyboardHints.Length : 0];
            for (int i = 0; i < authoredHintStates.Length; i++)
                authoredHintStates[i] = keyboardHints[i] != null && keyboardHints[i].activeSelf;
            if (priceText != null)
            {
                authoredAutoSize = priceText.enableAutoSizing;
                authoredWrapping = priceText.textWrappingMode;
                authoredFontSize = priceText.fontSize;
                authoredMinSize = priceText.fontSizeMin;
                authoredMaxSize = priceText.fontSizeMax;
            }
        }
        for (int i = 0; i < authoredHintStates.Length; i++)
            if (keyboardHints[i] != null) keyboardHints[i].SetActive(!mobile && authoredHintStates[i]);
        if (mobileBuyLabel != null) mobileBuyLabel.SetActive(mobile);
        if (mobileTimerRoot != null) mobileTimerRoot.SetActive(mobile);
        if (priceText == null) return;
        priceText.enableAutoSizing = mobile || authoredAutoSize;
        priceText.textWrappingMode = mobile ? TextWrappingModes.NoWrap : authoredWrapping;
        priceText.fontSizeMin = mobile ? Mathf.Min(48f, authoredFontSize) : authoredMinSize;
        priceText.fontSizeMax = mobile ? authoredFontSize : authoredMaxSize;
        if (!mobile) priceText.fontSize = authoredFontSize;
    }
}
