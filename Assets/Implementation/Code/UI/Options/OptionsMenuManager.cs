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
    private const int MinimumOptionsSortingOrder = 100;
    private static readonly string[] BackgroundNames = { "Background", "Fondo" };
    private const string FullscreenPrefsKey = "Fullscreen";
    private const string ResolutionIndexPrefsKey = "ResolutionIndex";
    private const string ResolutionWidthPrefsKey = "ResolutionWidth";
    private const string ResolutionHeightPrefsKey = "ResolutionHeight";

    [Header("UI References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject background;
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

    // Autoridad: coordina apertura/cierre, eventos de controles y aplicacion a Unity.
    // La seleccion de resoluciones queda en OptionsResolutionPolicy y la vista solo refleja valores.
    private Action onClosedCallback;
    private DisplayResolutionOption[] resolutionOptions = new DisplayResolutionOption[0];
    private Coroutine animationRoutine;
    private bool isOpen;

    private bool SupportsResolutionSelection =>
        OptionsResolutionPolicy.SupportsResolutionSelection(Application.isMobilePlatform);

    private void Awake()
    {
        ResolveUiReferences();
        NormalizeRenderableScale();
    }

    private void Start()
    {
        ResolveUiReferences();
        NormalizeRenderableScale();

        if (SupportsResolutionSelection)
        {
            SetupResolutionDropdown();
        }

        LoadSavedSettings();
        WireSettingsEvents();

        if (!isOpen)
        {
            SetVisible(false);
        }
    }

    public void Open(Action onClosed = null)
    {
        ResolveUiReferences();
        NormalizeRenderableScale();
        onClosedCallback = onClosed;
        isOpen = true;
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
        GlobalAudioSettings.SetMasterVolume(normalizedVolume);
        GlobalAudioSettings.ApplyToMixer(audioMixer, masterVolumeParameter, normalizedVolume);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (!SupportsResolutionSelection)
        {
            return;
        }

        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        PlayerPreferencesCheckpoint.MarkPending();
        ApplyDisplaySettings();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (!SupportsResolutionSelection || !IsValidResolutionIndex(resolutionIndex))
        {
            return;
        }

        SaveResolutionPreference(resolutionOptions[resolutionIndex], resolutionIndex);
        ApplyDisplaySettings();
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutionOptions = BuildUniqueResolutionList();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < resolutionOptions.Length; i++)
        {
            options.Add(resolutionOptions[i].ToString());
        }

        resolutionDropdown.AddOptions(options);
        
        // Load the saved resolution, or default to the monitor's native resolution
        int savedResolutionIndex = GetPreferredResolutionIndex();

        resolutionDropdown.SetValueWithoutNotify(savedResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private void LoadSavedSettings()
    {
        float savedVolume = GlobalAudioSettings.MasterVolume;
        if (volumeSlider != null)
        {
            // Default to 75% volume if they haven't saved a preference yet
            volumeSlider.SetValueWithoutNotify(savedVolume);
        }

        GlobalAudioSettings.SetMasterVolume(savedVolume, save: false);
        GlobalAudioSettings.ApplyToMixer(audioMixer, masterVolumeParameter, savedVolume);

        if (!SupportsResolutionSelection)
        {
            RestoreMobileLandscapeResolution();
            return;
        }

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
        if (!SupportsResolutionSelection
            || resolutionOptions == null
            || resolutionOptions.Length == 0)
        {
            return;
        }

        int resolutionIndex = resolutionDropdown != null
            ? resolutionDropdown.value
            : GetPreferredResolutionIndex();

        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutionOptions.Length - 1);
        DisplayResolutionOption resolution = resolutionOptions[resolutionIndex];
        bool isFullscreen = fullscreenToggle != null
            ? fullscreenToggle.isOn
            : PlayerPrefs.GetInt(FullscreenPrefsKey, 1) == 1;
        FullScreenMode targetMode = isFullscreen ? fullscreenMode : FullScreenMode.Windowed;

        Screen.SetResolution(resolution.Width, resolution.Height, targetMode);
        SaveResolutionPreference(resolution, resolutionIndex);
        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        PlayerPreferencesCheckpoint.CommitChanges();
    }

    private static void RestoreMobileLandscapeResolution()
    {
        DisplayResolutionOption targetResolution =
            OptionsResolutionPolicy.ResolveMobileLandscapeResolution(Screen.width, Screen.height);

        if (Screen.width == targetResolution.Width
            && Screen.height == targetResolution.Height
            && Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
        {
            return;
        }

        Screen.SetResolution(
            targetResolution.Width,
            targetResolution.Height,
            FullScreenMode.FullScreenWindow);
        PlayerPreferencesCheckpoint.MarkPending();
    }

    private DisplayResolutionOption[] BuildUniqueResolutionList()
    {
        return OptionsResolutionPolicy.BuildUniqueResolutionList(
            ConvertResolutionOptions(Screen.resolutions),
            new DisplayResolutionOption(Screen.width, Screen.height));
    }

    private static DisplayResolutionOption[] ConvertResolutionOptions(Resolution[] source)
    {
        if (source == null || source.Length == 0)
        {
            return new DisplayResolutionOption[0];
        }

        DisplayResolutionOption[] options = new DisplayResolutionOption[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            options[i] = new DisplayResolutionOption(source[i].width, source[i].height);
        }

        return options;
    }

    private int GetPreferredResolutionIndex()
    {
        bool hasSavedSize = PlayerPrefs.HasKey(ResolutionWidthPrefsKey);
        return OptionsResolutionPolicy.ResolvePreferredIndex(
            resolutionOptions,
            hasSavedSize,
            PlayerPrefs.GetInt(ResolutionWidthPrefsKey, Mathf.Max(1, Screen.width)),
            PlayerPrefs.GetInt(ResolutionHeightPrefsKey, Mathf.Max(1, Screen.height)),
            PlayerPrefs.HasKey(ResolutionIndexPrefsKey),
            PlayerPrefs.GetInt(ResolutionIndexPrefsKey, 0),
            new DisplayResolutionOption(Screen.width, Screen.height));
    }

    private void SaveResolutionPreference(DisplayResolutionOption resolution, int resolutionIndex)
    {
        PlayerPrefs.SetInt(ResolutionIndexPrefsKey, resolutionIndex);
        PlayerPrefs.SetInt(ResolutionWidthPrefsKey, resolution.Width);
        PlayerPrefs.SetInt(ResolutionHeightPrefsKey, resolution.Height);
        PlayerPreferencesCheckpoint.MarkPending();
    }

    private bool IsValidResolutionIndex(int resolutionIndex)
    {
        return OptionsResolutionPolicy.IsValidResolutionIndex(resolutionOptions, resolutionIndex);
    }

    private void WireSettingsEvents()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
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
            isOpen = false;
            onClosedCallback?.Invoke();
            onClosedCallback = null;
            yield break;
        }

        canvasGroup.interactable = false;
        yield return MenuScreenAnimation.FadeCanvas(canvasGroup, 0f, fadeDuration);
        
        SetVisible(false);
        isOpen = false;
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
        if (visible)
        {
            NormalizeRenderableScale();
        }

        bool menuRootIsManagerObject = menuRoot == gameObject;
        if (visible && menuRootIsManagerObject && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (menuRoot != null && !menuRootIsManagerObject)
        {
            menuRoot.SetActive(visible);
        }

        if (background != null)
        {
            background.SetActive(visible);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private void ResolveUiReferences()
    {
        Transform uiRoot = menuRoot != null ? menuRoot.transform : transform.Find("Canvas");
        if (menuRoot == null && uiRoot != null)
        {
            menuRoot = uiRoot.gameObject;
        }

        if (canvasGroup == null && menuRoot != null)
        {
            canvasGroup = menuRoot.GetComponentInChildren<CanvasGroup>(includeInactive: true);
        }

        backButton ??= UiButtonContract.FindButton(
            uiRoot,
            "VolverBoton",
            "BackBoton",
            "BackButton");

        ResolveBackgroundReference();
    }

    private void NormalizeRenderableScale()
    {
        Transform rootTransform = menuRoot != null ? menuRoot.transform : transform;
        Canvas ownerCanvas = rootTransform != null
            ? rootTransform.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);

        MenuHierarchyResolver.RestoreScaleIfCollapsed(ownerCanvas != null ? ownerCanvas.transform : null);
        MenuHierarchyResolver.RestoreScaleIfCollapsed(rootTransform);
        NormalizeCanvasLayer(ownerCanvas);
        NormalizeBackground(ownerCanvas);
    }

    private static void NormalizeCanvasLayer(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, MinimumOptionsSortingOrder);
    }

    private void NormalizeBackground(Canvas ownerCanvas)
    {
        ResolveBackgroundReference(ownerCanvas);
        if (background == null)
        {
            return;
        }

        MenuHierarchyResolver.RestoreScaleIfCollapsed(background.transform);

        if (menuRoot != null && background.transform.parent == menuRoot.transform)
        {
            background.transform.SetAsFirstSibling();
            return;
        }

        if (ownerCanvas != null && background.transform.parent == ownerCanvas.transform)
        {
            background.transform.SetAsFirstSibling();
        }
    }

    private void ResolveBackgroundReference(Canvas ownerCanvas = null)
    {
        if (background != null)
        {
            return;
        }

        Transform menuRootTransform = menuRoot != null ? menuRoot.transform : null;
        Transform menuBackground = MenuHierarchyResolver.FindDirectChildTransform(menuRootTransform, BackgroundNames);
        if (menuBackground != null)
        {
            background = menuBackground.gameObject;
            return;
        }

        Canvas canvas = ownerCanvas != null
            ? ownerCanvas
            : menuRoot != null
                ? menuRoot.GetComponentInParent<Canvas>(includeInactive: true)
                : GetComponentInChildren<Canvas>(includeInactive: true);

        Transform canvasTransform = canvas != null ? canvas.transform : null;
        if (canvasTransform == null)
        {
            return;
        }

        Transform existingLayer = MenuHierarchyResolver.FindDirectChildTransform(canvasTransform, BackgroundNames);
        background = existingLayer != null ? existingLayer.gameObject : null;
    }
}
