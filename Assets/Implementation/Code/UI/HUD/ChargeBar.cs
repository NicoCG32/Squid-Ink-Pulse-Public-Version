using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChargeBar : MonoBehaviour
{
    [Header("Legacy Slider")]
    [SerializeField] private Slider slider;

    [Header("Presentation")]
    [SerializeField] private InkBarFillPresenter fillPresenter;

    [Header("Full Prompt")]
    [SerializeField] private TMP_Text fullPromptText;
    [SerializeField, Range(0f, 1f)] private float fullPromptThreshold = 0.999f;
    [SerializeField, Min(0f)] private float fullPromptPulseAmplitude = 0.12f;
    [SerializeField, Min(0.01f)] private float fullPromptPulseFrequency = 2.5f;

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
    private RectTransform fullPromptTransform;
    private Vector3 fullPromptBaseScale = Vector3.one;
    private bool fullPromptVisible;
    private bool fullPromptSuppressed;
    private float fullPromptPulseTimer;

    private void Awake()
    {
        ResolveReferences(syncFromSlider: true);
        CacheFullPromptScale();
        ApplyFill();
        ApplyFullPromptVisibility(immediate: true);

        if (shakeTarget != null)
        {
            originalPosition = shakeTarget.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResolveReferences(syncFromSlider: true);
        CacheFullPromptScale();
        ApplyFill();
        ApplyFullPromptVisibility(immediate: true);
    }

    private void Update()
    {
        HandleShakeEffect();
        HandleFullPromptAnimation();
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
        ApplyFullPromptVisibility(immediate: false);
    }

    public void SetFullPromptSuppressed(bool suppressed)
    {
        if (fullPromptSuppressed == suppressed)
        {
            return;
        }

        fullPromptSuppressed = suppressed;
        ApplyFullPromptVisibility(immediate: false);
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

        if (fullPromptText == null)
        {
            fullPromptText = FindFullPromptText();
        }

        if (fullPromptText != null)
        {
            fullPromptTransform = fullPromptText.rectTransform;
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

    private void CacheFullPromptScale()
    {
        if (fullPromptTransform != null)
        {
            fullPromptBaseScale = fullPromptTransform.localScale;
        }
    }

    private void ApplyFullPromptVisibility(bool immediate)
    {
        if (fullPromptText == null || fullPromptTransform == null)
        {
            return;
        }

        bool shouldShow = !fullPromptSuppressed && fillRatio >= fullPromptThreshold;
        if (fullPromptVisible == shouldShow && !immediate)
        {
            return;
        }

        fullPromptVisible = shouldShow;
        fullPromptPulseTimer = 0f;
        fullPromptText.gameObject.SetActive(shouldShow);
        fullPromptTransform.localScale = fullPromptBaseScale;
    }

    private void HandleFullPromptAnimation()
    {
        if (!fullPromptVisible || fullPromptTransform == null)
        {
            return;
        }

        fullPromptPulseTimer += Time.unscaledDeltaTime;
        float pulse = 1f + Mathf.Sin(fullPromptPulseTimer * Mathf.PI * 2f * fullPromptPulseFrequency) * fullPromptPulseAmplitude;
        fullPromptTransform.localScale = fullPromptBaseScale * pulse;
    }

    private TMP_Text FindFullPromptText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(includeInactive: true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            if (text.name == "ClickSign" || string.Equals(text.text?.Trim(), "CLICK", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }
        }

        return null;
    }
}
