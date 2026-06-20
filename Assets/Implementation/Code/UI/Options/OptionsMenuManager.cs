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
    private const string VolumePrefsKey = "MasterVolume";
    private const string FullscreenPrefsKey = "Fullscreen";
    private const string ResolutionIndexPrefsKey = "ResolutionIndex";
    private const string ResolutionWidthPrefsKey = "ResolutionWidth";
    private const string ResolutionHeightPrefsKey = "ResolutionHeight";
    private const float MinimumMixerVolume = 0.0001f;
    private const float MutedMixerDecibels = -80f;

    [Header("UI References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button backButton;

    [Header("Settings References")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = string.Empty;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.ExclusiveFullScreen;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Action onClosedCallback;
    private Resolution[] resolutions;
    private Coroutine animationRoutine;

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
        StartAnimation(AnimateIn());
    }

    public void Close()
    {
        StartAnimation(AnimateOutThenClose());
    }

    // --- SETTINGS LOGIC ---

    public void SetVolume(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        ApplyGlobalVolume(normalizedVolume);
        PlayerPrefs.SetFloat(VolumePrefsKey, normalizedVolume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        ApplyDisplaySettings();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (!IsValidResolutionIndex(resolutionIndex))
        {
            return;
        }

        SaveResolutionPreference(resolutions[resolutionIndex], resolutionIndex);
        ApplyDisplaySettings();
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = BuildUniqueResolutionList();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);
        
        // Load the saved resolution, or default to the monitor's native resolution
        int savedResolutionIndex = GetPreferredResolutionIndex();

        resolutionDropdown.SetValueWithoutNotify(savedResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private void LoadSavedSettings()
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, 0.75f));
        if (volumeSlider != null)
        {
            // Default to 75% volume if they haven't saved a preference yet
            volumeSlider.SetValueWithoutNotify(savedVolume);
        }

        ApplyGlobalVolume(savedVolume);

        if (fullscreenToggle != null)
        {
            // Default to fullscreen (1)
            bool isFullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 1) == 1;
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        }

        ApplyDisplaySettings();
    }

    private void ApplyDisplaySettings()
    {
        if (resolutions == null || resolutions.Length == 0)
        {
            return;
        }

        int resolutionIndex = resolutionDropdown != null
            ? resolutionDropdown.value
            : GetPreferredResolutionIndex();

        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);
        Resolution resolution = resolutions[resolutionIndex];
        bool isFullscreen = fullscreenToggle != null
            ? fullscreenToggle.isOn
            : PlayerPrefs.GetInt(FullscreenPrefsKey, 1) == 1;
        FullScreenMode targetMode = isFullscreen ? fullscreenMode : FullScreenMode.Windowed;

        Screen.SetResolution(resolution.width, resolution.height, targetMode);
        SaveResolutionPreference(resolution, resolutionIndex);
        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyGlobalVolume(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        AudioListener.volume = normalizedVolume;

        if (audioMixer == null || string.IsNullOrWhiteSpace(masterVolumeParameter))
        {
            return;
        }

        float decibels = normalizedVolume <= 0f
            ? MutedMixerDecibels
            : Mathf.Log10(Mathf.Max(MinimumMixerVolume, normalizedVolume)) * 20f;
        audioMixer.SetFloat(masterVolumeParameter.Trim(), decibels);
    }

    private Resolution[] BuildUniqueResolutionList()
    {
        Resolution[] source = Screen.resolutions;
        if (source == null || source.Length == 0)
        {
            return new[]
            {
                new Resolution
                {
                    width = Mathf.Max(1, Screen.width),
                    height = Mathf.Max(1, Screen.height)
                }
            };
        }

        List<Resolution> unique = new List<Resolution>();
        for (int i = 0; i < source.Length; i++)
        {
            Resolution candidate = source[i];
            int existingIndex = unique.FindIndex(resolution =>
                resolution.width == candidate.width
                && resolution.height == candidate.height);

            if (existingIndex >= 0)
            {
                unique[existingIndex] = candidate;
                continue;
            }

            unique.Add(candidate);
        }

        unique.Sort((left, right) =>
        {
            int widthComparison = left.width.CompareTo(right.width);
            return widthComparison != 0
                ? widthComparison
                : left.height.CompareTo(right.height);
        });

        return unique.ToArray();
    }

    private int GetPreferredResolutionIndex()
    {
        if (resolutions == null || resolutions.Length == 0)
        {
            return 0;
        }

        if (!PlayerPrefs.HasKey(ResolutionWidthPrefsKey) && PlayerPrefs.HasKey(ResolutionIndexPrefsKey))
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(ResolutionIndexPrefsKey), 0, resolutions.Length - 1);
        }

        int targetWidth = PlayerPrefs.GetInt(ResolutionWidthPrefsKey, Mathf.Max(1, Screen.width));
        int targetHeight = PlayerPrefs.GetInt(ResolutionHeightPrefsKey, Mathf.Max(1, Screen.height));
        return FindClosestResolutionIndex(targetWidth, targetHeight);
    }

    private int FindClosestResolutionIndex(int targetWidth, int targetHeight)
    {
        int closestIndex = 0;
        int closestDistance = int.MaxValue;
        for (int i = 0; i < resolutions.Length; i++)
        {
            int distance = Mathf.Abs(resolutions[i].width - targetWidth)
                + Mathf.Abs(resolutions[i].height - targetHeight);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void SaveResolutionPreference(Resolution resolution, int resolutionIndex)
    {
        PlayerPrefs.SetInt(ResolutionIndexPrefsKey, resolutionIndex);
        PlayerPrefs.SetInt(ResolutionWidthPrefsKey, resolution.width);
        PlayerPrefs.SetInt(ResolutionHeightPrefsKey, resolution.height);
    }

    private bool IsValidResolutionIndex(int resolutionIndex)
    {
        return resolutions != null
            && resolutionIndex >= 0
            && resolutionIndex < resolutions.Length;
    }

    // --- ANIMATION & VISIBILITY ---

    private IEnumerator AnimateIn()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.interactable = false;
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 1f, fadeDuration);
        canvasGroup.interactable = true;
    }

    private IEnumerator AnimateOutThenClose()
    {
        if (canvasGroup == null)
        {
            SetVisible(false);
            onClosedCallback?.Invoke();
            onClosedCallback = null;
            yield break;
        }

        canvasGroup.interactable = false;
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 0f, fadeDuration);
        
        SetVisible(false);
        onClosedCallback?.Invoke();
        onClosedCallback = null;
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        animationRoutine = StartCoroutine(routine);
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
