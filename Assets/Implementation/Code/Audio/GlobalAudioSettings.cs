using System;
using UnityEngine;
using UnityEngine.Audio;

public static class GlobalAudioSettings
{
    public const string VolumePrefsKey = "MasterVolume";
    public const float DefaultMasterVolume = 0.75f;

    private const float MinimumMixerVolume = 0.0001f;
    private const float MutedMixerDecibels = -80f;

    public static event Action<float> MasterVolumeChanged;

    public static float MasterVolume =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, DefaultMasterVolume));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        MasterVolumeChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedVolumeBeforeSceneLoad()
    {
        ApplyRuntimeVolume(MasterVolume);
    }

    public static void SetMasterVolume(float volume, bool save = true)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        ApplyRuntimeVolume(normalizedVolume);

        if (save)
        {
            PlayerPrefs.SetFloat(VolumePrefsKey, normalizedVolume);
            PlayerPrefs.Save();
        }

        MasterVolumeChanged?.Invoke(normalizedVolume);
    }

    public static void ApplyToMixer(AudioMixer mixer, string exposedParameter)
    {
        ApplyToMixer(mixer, exposedParameter, MasterVolume);
    }

    public static void ApplyToMixer(AudioMixer mixer, string exposedParameter, float volume)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(exposedParameter))
        {
            return;
        }

        mixer.SetFloat(exposedParameter.Trim(), ToDecibels(volume));
    }

    private static void ApplyRuntimeVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    private static float ToDecibels(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        return normalizedVolume <= 0f
            ? MutedMixerDecibels
            : Mathf.Log10(Mathf.Max(MinimumMixerVolume, normalizedVolume)) * 20f;
    }
}
