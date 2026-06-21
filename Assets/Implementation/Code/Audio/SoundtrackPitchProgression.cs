using UnityEngine;

[DisallowMultipleComponent]
public class SoundtrackPitchProgression : MonoBehaviour
{
    [SerializeField] private AudioSource[] tracks;
    [SerializeField, Min(0f)] private float pitchIncreasePerSecond = 0.0005f;
    [SerializeField, Min(0f)] private float maxPitchOffset = 0.18f;

    private float[] basePitches = System.Array.Empty<float>();

    private void Awake()
    {
        ResolveTracksIfNeeded();
        CacheBasePitchesIfNeeded();
        ApplyPitch();
    }

    private void OnEnable()
    {
        ResolveTracksIfNeeded();
        CacheBasePitchesIfNeeded();
        ApplyPitch();
    }

    private void Update()
    {
        ResolveTracksIfNeeded();
        CacheBasePitchesIfNeeded();
        ApplyPitch();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        pitchIncreasePerSecond = Mathf.Max(0f, pitchIncreasePerSecond);
        maxPitchOffset = Mathf.Max(0f, maxPitchOffset);

        if (!Application.isPlaying)
        {
            ResolveTracksIfNeeded();
        }
    }
#endif

    private void ResolveTracksIfNeeded()
    {
        if (tracks != null && tracks.Length > 0)
        {
            return;
        }

        tracks = GetComponents<AudioSource>();
    }

    private void CacheBasePitchesIfNeeded()
    {
        if (tracks == null || tracks.Length == 0)
        {
            basePitches = System.Array.Empty<float>();
            return;
        }

        if (basePitches != null && basePitches.Length == tracks.Length)
        {
            return;
        }

        basePitches = new float[tracks.Length];
        for (int i = 0; i < tracks.Length; i++)
        {
            basePitches[i] = tracks[i] != null ? Mathf.Max(0.01f, tracks[i].pitch) : 1f;
        }
    }

    private void ApplyPitch()
    {
        if (tracks == null || tracks.Length == 0)
        {
            return;
        }

        float pitchOffset = Mathf.Min(
            maxPitchOffset,
            RuntimePlayerPace.ElapsedSpeedSeconds * pitchIncreasePerSecond);

        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i] == null)
            {
                continue;
            }

            tracks[i].pitch = Mathf.Max(0.01f, basePitches[i] + pitchOffset);
        }
    }
}
