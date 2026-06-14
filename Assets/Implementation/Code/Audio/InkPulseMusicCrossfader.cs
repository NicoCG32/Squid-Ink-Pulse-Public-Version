using UnityEngine;

[DisallowMultipleComponent]
public class InkPulseMusicCrossfader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private AudioSource normalTrack;
    [SerializeField] private AudioSource inkTrack;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float normalTargetVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float inkTargetVolume = 0.6f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.45f;
    [SerializeField] private bool useEqualPowerCrossfade = false;
    [SerializeField, Min(0f)] private float syncStartDelay = 0.05f;

    private InkPulseController subscribedInkPulse;
    private float currentBlend;
    private float targetBlend;
    private bool playbackStarted;
    private bool warnedMissingReferences;

    private void Awake()
    {
        ResolveReferences();
        ConfigureSources();
        SetBlendImmediate(ResolveTargetBlend());
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToInkPulse();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToInkPulse();
        ConfigureSources();
        SetBlendImmediate(ResolveTargetBlend());
        StartSyncedPlayback();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (subscribedInkPulse == null)
        {
            ResolveReferences();
            SubscribeToInkPulse();
        }

        targetBlend = ResolveTargetBlend();
        currentBlend = fadeSeconds <= Mathf.Epsilon
            ? targetBlend
            : Mathf.MoveTowards(currentBlend, targetBlend, Time.unscaledDeltaTime / fadeSeconds);

        ApplyBlend(currentBlend);
    }

    private void OnDisable()
    {
        ClearInkPulseSubscription();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        normalTargetVolume = Mathf.Clamp01(normalTargetVolume);
        inkTargetVolume = Mathf.Clamp01(inkTargetVolume);
        fadeSeconds = Mathf.Max(0.01f, fadeSeconds);
        syncStartDelay = Mathf.Max(0f, syncStartDelay);
        ApplyBlend(currentBlend);
    }
#endif

    private void ResolveReferences()
    {
        if (inkPulse == null)
        {
            inkPulse = FindFirstObjectByType<InkPulseController>();
        }

        if (normalTrack != null && inkTrack != null)
        {
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        if (normalTrack == null && sources.Length > 0)
        {
            normalTrack = sources[0];
        }

        if (inkTrack != null)
        {
            return;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null && sources[i] != normalTrack)
            {
                inkTrack = sources[i];
                return;
            }
        }
    }

    private void ConfigureSources()
    {
        ConfigureSource(normalTrack);
        ConfigureSource(inkTrack);
    }

    private static void ConfigureSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    private void SubscribeToInkPulse()
    {
        if (subscribedInkPulse == inkPulse)
        {
            return;
        }

        ClearInkPulseSubscription();

        if (inkPulse == null)
        {
            return;
        }

        subscribedInkPulse = inkPulse;
        subscribedInkPulse.PulseStarted += HandlePulseStarted;
        subscribedInkPulse.PulseEnded += HandlePulseEnded;
        subscribedInkPulse.StateChanged += HandleInkPulseStateChanged;
    }

    private void ClearInkPulseSubscription()
    {
        if (subscribedInkPulse == null)
        {
            return;
        }

        subscribedInkPulse.PulseStarted -= HandlePulseStarted;
        subscribedInkPulse.PulseEnded -= HandlePulseEnded;
        subscribedInkPulse.StateChanged -= HandleInkPulseStateChanged;
        subscribedInkPulse = null;
    }

    private void HandlePulseStarted()
    {
        targetBlend = 1f;
    }

    private void HandlePulseEnded()
    {
        targetBlend = 0f;
    }

    private void HandleInkPulseStateChanged(InkPulseState previousState, InkPulseState nextState)
    {
        targetBlend = nextState == InkPulseState.Active ? 1f : 0f;
    }

    private void StartSyncedPlayback()
    {
        if (playbackStarted || normalTrack == null || inkTrack == null)
        {
            return;
        }

        normalTrack.Stop();
        inkTrack.Stop();

        double startTime = AudioSettings.dspTime + syncStartDelay;
        normalTrack.PlayScheduled(startTime);
        inkTrack.PlayScheduled(startTime);
        playbackStarted = true;
    }

    private float ResolveTargetBlend()
    {
        return inkPulse != null && inkPulse.IsPulseActive ? 1f : 0f;
    }

    private void SetBlendImmediate(float blend)
    {
        targetBlend = blend;
        currentBlend = blend;
        ApplyBlend(blend);
    }

    private void ApplyBlend(float blend)
    {
        float normalizedBlend = Mathf.Clamp01(blend);
        float normalWeight;
        float inkWeight;

        if (useEqualPowerCrossfade)
        {
            normalWeight = Mathf.Cos(normalizedBlend * Mathf.PI * 0.5f);
            inkWeight = Mathf.Sin(normalizedBlend * Mathf.PI * 0.5f);
        }
        else
        {
            normalWeight = 1f - normalizedBlend;
            inkWeight = normalizedBlend;
        }

        if (normalTrack != null)
        {
            normalTrack.volume = normalTargetVolume * normalWeight;
        }

        if (inkTrack != null)
        {
            inkTrack.volume = inkTargetVolume * inkWeight;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (warnedMissingReferences)
        {
            return;
        }

        if (inkPulse == null || normalTrack == null || inkTrack == null)
        {
            Debug.LogWarning("[InkPulseMusicCrossfader] Faltan referencias. Asigna InkPulse, NormalTrack e InkTrack en el nodo Soundtrack.", this);
            warnedMissingReferences = true;
        }
    }
}
