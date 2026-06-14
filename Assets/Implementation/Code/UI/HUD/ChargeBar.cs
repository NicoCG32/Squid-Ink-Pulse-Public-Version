using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChargeBar : MonoBehaviour
{
    [Header("Legacy Slider")]
    [SerializeField] private Slider slider;

    [Header("Presentation")]
    [SerializeField] private InkBarFillPresenter fillPresenter;

    private float fillRatio;

    private void Awake()
    {
        ResolveReferences(syncFromSlider: true);
        ApplyFill();
    }

    private void OnEnable()
    {
        ResolveReferences(syncFromSlider: true);
        ApplyFill();
    }

    public void UpdateBar(float fillPercentage)
    {
        SetFill(fillPercentage);
    }

    public void ResetBar()
    {
        SetFill(0f);
    }

    public void SetFill(float normalizedValue)
    {
        fillRatio = Mathf.Clamp01(normalizedValue);
        ResolveReferences(syncFromSlider: false);
        ApplyFill();
    }

    private void ResolveReferences(bool syncFromSlider)
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        if (fillPresenter == null)
        {
            fillPresenter = GetComponentInChildren<InkBarFillPresenter>(includeInactive: true);
        }

        if (syncFromSlider && slider != null)
        {
            fillRatio = Mathf.Clamp01(slider.normalizedValue);
        }
    }

    private void ApplyFill()
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(fillRatio);
        }

        if (fillPresenter != null)
        {
            fillPresenter.SetFill(fillRatio);
        }
    }
}
