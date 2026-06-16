using System.Collections;
using UnityEngine;

[RequireComponent(typeof(InkPulseController))]
[RequireComponent(typeof(AudioSource))]
public class InkPulseAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip pulseClip;
    [SerializeField] private float fadeOutDuration = 0.5f; // How long the fade takes in seconds

    private InkPulseController pulseController;
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        pulseController = GetComponent<InkPulseController>();
        audioSource = GetComponent<AudioSource>();
        
        // Ensure the AudioSource doesn't play on awake
        audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        // Subscribe to your existing events
        pulseController.PulseStarted += HandlePulseStarted;
        pulseController.PulseEnded += HandlePulseEnded;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks!
        pulseController.PulseStarted -= HandlePulseStarted;
        pulseController.PulseEnded -= HandlePulseEnded;
    }

    private void HandlePulseStarted()
    {
        // If a fade is currently happening, stop it so we can start fresh
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        audioSource.clip = pulseClip;
        audioSource.volume = 1f; // Reset volume to max
        audioSource.Play();
    }

    private void HandlePulseEnded()
    {
        // Start fading out dynamically as soon as the pulse ends
        if (gameObject.activeInHierarchy)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutAudio());
        }
    }

    private IEnumerator FadeOutAudio()
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            // Mathf.Lerp smoothly transitions the volume from its current level to 0
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeOutDuration);
            yield return null;
        }

        // Once the volume hits 0, stop the clip entirely
        audioSource.Stop();
        audioSource.volume = startVolume; // Reset the volume so it's ready for the next pulse
    }
}