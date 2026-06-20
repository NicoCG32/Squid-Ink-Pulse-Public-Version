using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class RunProgressionDirector : MonoBehaviour
{
    private static RunProgressionDirector instance;

    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Transform distanceReference = null;

    [Header("Progression")]
    [SerializeField] private float secondsToMaxIntensity = 120f;
    [SerializeField, Range(0f, 1f)] private float postBossIntensityFloor = 0.25f;

    [Header("Movement")]
    [SerializeField] private float minScrollSpeed = 5f;
    [SerializeField] private float maxScrollSpeed = 9f;
    [SerializeField, Min(0.01f)] private float speedGrowthTimeConstantSeconds = 180f;

    [Header("Spawning")]
    [SerializeField] private float maxSpawnInterval = 1.5f;
    [SerializeField] private float minSpawnInterval = 0.65f;
    [SerializeField, Min(0.01f)] private float bossActiveSpawnIntervalMultiplier = 0.5f;
    [SerializeField, Min(0.01f)] private float postBossSpawnIntervalMultiplier = 1f;

    [Header("Boss Pacing")]
    [SerializeField] private float maxBossInterval = 45f;
    [SerializeField] private float minBossInterval = 30f;
    [SerializeField, FormerlySerializedAs("bossEventLockSeconds")] private float postBossWindowSeconds = 6f;

    [Header("Score")]
    [SerializeField, Min(0f)] private float scorePerSecond = 1250f;
    [SerializeField, Min(0f)] private float scoreIntensityBonusMultiplier = 1f;

    [Header("Events")]
    public UnityEvent<RunEventState> onEventStateChanged = new UnityEvent<RunEventState>();

    private float elapsedSeconds;
    private float cycleElapsedSeconds;
    private float bossCycleElapsedSeconds;
    private float integratedDistance;
    private float eventStateRemainingSeconds;
    private float startX;
    private float scoreAccumulator;

    public static RunProgressionDirector Instance => instance;
    public static bool HasInstance => instance != null;

    public RunDifficultySnapshot Current { get; private set; }
    public int ProgressionCycle { get; private set; }
    public RunEventState EventState { get; private set; } = RunEventState.Normal;
    public float EventStateRemainingSeconds => eventStateRemainingSeconds;
    public bool IsEventBlockingRegularSpawns => EventState == RunEventState.Transitioning;
    public bool CanTriggerBossEvent => session != null
        && session.IsPlaying
        && EventState == RunEventState.Normal
        && bossCycleElapsedSeconds >= Current.BossInterval;

    public event Action<RunEventState, RunEventState> EventStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveSessionReference();

        if (distanceReference != null)
        {
            startX = distanceReference.position.x;
        }

        RefreshSnapshot();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        ResolveSessionReference();

        if (session == null || !session.IsPlaying)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        if (ShouldAdvanceRuntimeProgression())
        {
            elapsedSeconds += deltaTime;
            RuntimePlayerPace.Advance(deltaTime);
            integratedDistance += Mathf.Max(Current.TargetScrollSpeed, minScrollSpeed) * deltaTime;
            UpdateScore(deltaTime);

            if (EventState == RunEventState.Normal)
            {
                cycleElapsedSeconds += deltaTime;
                bossCycleElapsedSeconds += deltaTime;
            }
        }

        UpdateEventStateTimer(deltaTime);
        RefreshSnapshot();
    }

    private bool ShouldAdvanceRuntimeProgression()
    {
        return EventState != RunEventState.Transitioning;
    }

    private void UpdateScore(float deltaTime)
    {
        float intensityMultiplier = 1f + Current.Intensity * scoreIntensityBonusMultiplier;
        scoreAccumulator += scorePerSecond * intensityMultiplier * PermanentUpgradeEffectResolver.PointsMultiplier * deltaTime;

        long wholeScore = (long)Mathf.Floor(scoreAccumulator);
        if (wholeScore <= 0)
        {
            return;
        }

        RuntimeRunScore.Add(wholeScore);
        scoreAccumulator -= wholeScore;
    }

    public bool TryStartBossEvent()
    {
        if (!CanTriggerBossEvent)
        {
            return false;
        }

        ApplyEventState(RunEventState.BossActive);
        RefreshSnapshot();
        return true;
    }

    public void NotifyBossResolved()
    {
        if (EventState != RunEventState.BossActive)
        {
            return;
        }

        ProgressionCycle++;
        cycleElapsedSeconds = Mathf.Max(cycleElapsedSeconds, secondsToMaxIntensity);
        bossCycleElapsedSeconds = 0f;
        ApplyEventState(RunEventState.PostBossWindow);
        RefreshSnapshot();
    }

    public void NotifyBossFailed()
    {
        if (EventState == RunEventState.BossActive)
        {
            ApplyEventState(RunEventState.Normal);
            RefreshSnapshot();
        }
    }

    public bool TryBeginTransition()
    {
        if (EventState != RunEventState.PostBossWindow && EventState != RunEventState.Normal)
        {
            return false;
        }

        ApplyEventState(RunEventState.Transitioning);
        RefreshSnapshot();
        return true;
    }

    public void CompleteTransition()
    {
        if (EventState != RunEventState.Transitioning)
        {
            return;
        }

        cycleElapsedSeconds = 0f;
        bossCycleElapsedSeconds = 0f;
        ProgressionCycle = 0;
        ApplyEventState(RunEventState.Normal);
        RefreshSnapshot();
    }

    public void ResetProgression()
    {
        elapsedSeconds = 0f;
        cycleElapsedSeconds = 0f;
        bossCycleElapsedSeconds = 0f;
        integratedDistance = 0f;
        eventStateRemainingSeconds = 0f;
        scoreAccumulator = 0f;
        ProgressionCycle = 0;
        RuntimePlayerPace.ResetForRuntime();
        ApplyEventState(RunEventState.Normal, force: true);

        if (distanceReference != null)
        {
            startX = distanceReference.position.x;
        }

        RefreshSnapshot();
    }

    private void RefreshSnapshot()
    {
        float rawIntensity = Mathf.Clamp01(cycleElapsedSeconds / Mathf.Max(0.01f, secondsToMaxIntensity));
        float smoothedIntensity = Mathf.SmoothStep(0f, 1f, rawIntensity);
        float floor = ProgressionCycle > 0 ? postBossIntensityFloor : 0f;
        float intensity = EventState == RunEventState.BossActive
            ? 1f
            : Mathf.Clamp01(Mathf.Max(smoothedIntensity, floor));

        float targetScrollSpeed = CalculateAsymptoticScrollSpeed();
        float baseSpawnInterval = Mathf.Lerp(
            Mathf.Max(minSpawnInterval, maxSpawnInterval),
            Mathf.Max(0.01f, minSpawnInterval),
            intensity);
        float spawnInterval = Mathf.Max(0.01f, baseSpawnInterval * GetEventSpawnIntervalMultiplier());
        float bossInterval = Mathf.Lerp(
            Mathf.Max(minBossInterval, maxBossInterval),
            Mathf.Max(0.01f, minBossInterval),
            intensity);

        Current = new RunDifficultySnapshot(
            elapsedSeconds,
            cycleElapsedSeconds,
            CalculateDistance(),
            intensity,
            targetScrollSpeed,
            spawnInterval,
            bossInterval,
            ProgressionCycle,
            EventState);
    }

    private float CalculateAsymptoticScrollSpeed()
    {
        float minSpeed = Mathf.Max(0f, minScrollSpeed);
        float maxSpeed = Mathf.Max(minSpeed, maxScrollSpeed);
        float growthProgress = 1f - Mathf.Exp(-RuntimePlayerPace.ElapsedSpeedSeconds / speedGrowthTimeConstantSeconds);

        return Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(growthProgress));
    }

    private float GetEventSpawnIntervalMultiplier()
    {
        return EventState switch
        {
            RunEventState.BossActive => bossActiveSpawnIntervalMultiplier,
            RunEventState.PostBossWindow => postBossSpawnIntervalMultiplier,
            _ => 1f
        };
    }

    private void UpdateEventStateTimer(float deltaTime)
    {
        if (EventState != RunEventState.PostBossWindow)
        {
            return;
        }

        eventStateRemainingSeconds = Mathf.Max(0f, eventStateRemainingSeconds - deltaTime);
        if (eventStateRemainingSeconds <= 0f)
        {
            ApplyEventState(RunEventState.Normal);
        }
    }

    private void ApplyEventState(RunEventState nextState, bool force = false)
    {
        RunEventState previousState = EventState;
        if (!force && previousState == nextState)
        {
            return;
        }

        EventState = nextState;
        eventStateRemainingSeconds = nextState == RunEventState.PostBossWindow
            ? Mathf.Max(0f, postBossWindowSeconds)
            : 0f;

        EventStateChanged?.Invoke(previousState, nextState);
        onEventStateChanged?.Invoke(nextState);
    }

    private float CalculateDistance()
    {
        if (distanceReference == null)
        {
            return integratedDistance;
        }

        return Mathf.Max(0f, distanceReference.position.x - startX);
    }

    private void ResolveSessionReference()
    {
        if (session != null)
        {
            return;
        }

        if (TryGetComponent(out GameSessionController localSession))
        {
            session = localSession;
            return;
        }

        if (GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null)
        {
            Debug.LogWarning("[RunProgressionDirector] Falta asignar GameSessionController.", this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
