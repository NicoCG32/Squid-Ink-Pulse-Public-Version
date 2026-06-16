using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

[DisallowMultipleComponent]
public class OptionsMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backButton;

    [Header("Settings References")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Action onClosedCallback;
    private Resolution[] resolutions;

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(Close);
        }

        // Setup the dropdown before we load saved settings
        SetupResolutionDropdown();
        LoadSavedSettings();

        // Add listeners so the game updates immediately when the player tweaks a setting
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(SetVolume);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        
        SetVisible(false);
    }

    public void Open(Action onClosed = null)
    {
        onClosedCallback = onClosed;
        SetVisible(true);
        StartCoroutine(AnimateIn());
    }

    public void Close()
    {
        StartCoroutine(AnimateOutThenClose());
    }

    // --- SETTINGS LOGIC ---

    public void SetVolume(float volume)
    {
        // Unity AudioMixers use logarithmic decibels, not a linear 0 to 1 scale.
        // We convert the slider value (0.0001 to 1) into decibels (-80dB to 0dB).
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        // Get every resolution supported by the player's monitor
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Check if this is the resolution we are currently using
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        
        // Load the saved resolution, or default to the monitor's native resolution
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private void LoadSavedSettings()
    {
        if (volumeSlider != null)
        {
            // Default to 75% volume if they haven't saved a preference yet
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            SetVolume(volumeSlider.value);
        }

        if (fullscreenToggle != null)
        {
            // Default to fullscreen (1)
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            fullscreenToggle.isOn = isFullscreen;
            SetFullscreen(isFullscreen);
        }
    }

    // --- ANIMATION & VISIBILITY ---

    private IEnumerator AnimateIn()
    {
        canvasGroup.interactable = false;
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 1f, fadeDuration);
        canvasGroup.interactable = true;
    }

    private IEnumerator AnimateOutThenClose()
    {
        canvasGroup.interactable = false;
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 0f, fadeDuration);
        
        SetVisible(false);
        onClosedCallback?.Invoke(); 
    }

    private void SetVisible(bool visible)
    {
        if (menuRoot != null) menuRoot.SetActive(visible);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}