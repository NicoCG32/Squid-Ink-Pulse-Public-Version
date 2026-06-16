using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChargeBar : MonoBehaviour
{
    [Header("Legacy Slider")]
    [SerializeField] private Slider slider;

    [Header("Presentation")]
    [SerializeField] private InkBarFillPresenter fillPresenter;

    [Header("Error Feedback")]
    [Tooltip("The UI element to physically shake. Drag the main parent of the Charge Bar here.")]
    [SerializeField] private RectTransform shakeTarget; 
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip errorClip;
    
    [Space]
    [SerializeField] private float shakeSpeed = 50f;
    [SerializeField] private float maxShakeAmount = 15f;
    [SerializeField] private float shakeDecayRate = 40f;
    [Tooltip("How much vertical bounce the shake has. 0.5 means it moves up/down half as much as left/right.")]
    [SerializeField] private float verticalShakeMultiplier = 0.5f;

    private float fillRatio;
    private float currentShakeIntensity;
    private Vector2 originalPosition;

    private void Awake()
    {
        ResolveReferences(syncFromSlider: true);
        ApplyFill();

        if (shakeTarget != null)
        {
            originalPosition = shakeTarget.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResolveReferences(syncFromSlider: true);
        ApplyFill();
    }

    private void Update()
    {
        HandleShakeEffect();
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

    public void TriggerErrorFeedback()
    {
        currentShakeIntensity = maxShakeAmount;

        if (audioSource != null && errorClip != null)
        {
            audioSource.PlayOneShot(errorClip);
        }
    }

    private void HandleShakeEffect()
    {
        if (shakeTarget == null) return;

        if (currentShakeIntensity > 0)
        {
            // Decay the shake intensity over time
            currentShakeIntensity -= shakeDecayRate * Time.deltaTime;
            currentShakeIntensity = Mathf.Max(0, currentShakeIntensity);

            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * currentShakeIntensity;
           
            float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * (currentShakeIntensity * verticalShakeMultiplier);

            shakeTarget.anchoredPosition = originalPosition + new Vector2(offsetX, offsetY);
        }
        else if (shakeTarget.anchoredPosition != originalPosition)
        {
            shakeTarget.anchoredPosition = originalPosition;
        }
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