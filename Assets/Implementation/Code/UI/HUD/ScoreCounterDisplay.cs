using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreCounterDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string prefix = string.Empty;
    [SerializeField] private string suffix = string.Empty;
    [SerializeField] private bool forceRightAligned = true;

    private void Awake()
    {
        ResolveTextReference();
        ConfigureTextPresentation();
    }

    private void OnEnable()
    {
        RuntimeRunScore.ScoreChanged += Refresh;
        Refresh(RuntimeRunScore.TotalScore);
    }

    private void OnDisable()
    {
        RuntimeRunScore.ScoreChanged -= Refresh;
    }

    private void Refresh(long score)
    {
        ResolveTextReference();
        ConfigureTextPresentation();

        if (scoreText != null)
        {
            scoreText.text = $"{prefix}{score}{suffix}";
        }
    }

    private void ResolveTextReference()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>(includeInactive: true);
        }
    }

    private void ConfigureTextPresentation()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.textWrappingMode = TextWrappingModes.NoWrap;
        scoreText.overflowMode = TextOverflowModes.Overflow;

        if (forceRightAligned)
        {
            scoreText.alignment = TextAlignmentOptions.TopRight;
        }
    }
}
