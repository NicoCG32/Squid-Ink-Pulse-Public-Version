using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TutorialDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private SceneFlowController sceneFlow;
    [SerializeField] private LevelSpawner levelSpawner;
    [SerializeField] private BossEventDirector bossDirector;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Transform player;
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private PlayerCollision playerCollision;
    [SerializeField] private InGameShopManager shopManager;
    [SerializeField] private TutorialPresentationController presentationController;

    [Header("Directed Prefabs")]
    [SerializeField] private GameObject grazeEnemyPrefab;
    [SerializeField] private string grazeEnemyTag = EnemyTagCatalog.Pufferfish;
    [SerializeField] private GameObject inkPulseObstaclePrefab;
    [SerializeField] private string inkPulseObstacleTag = EnemyTagCatalog.Generic;
    [SerializeField] private GameObject inkBottleBarrierEnemyPrefab;
    [SerializeField] private string inkBottleBarrierEnemyTag = EnemyTagCatalog.Generic;
    [SerializeField] private GameObject shrimpPrefab;
    [SerializeField] private GameObject dealerFishPrefab;
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private GameObject protectedHitEnemyPrefab;
    [SerializeField] private string protectedHitEnemyTag = EnemyTagCatalog.Generic;
    [SerializeField] private GameObject finalEnemyPrefab;
    [SerializeField] private string finalEnemyTag = EnemyTagCatalog.Generic;

    [Header("Directed Parents")]
    [SerializeField] private Transform tutorialSpawnParent;
    [SerializeField] private Transform tutorialPortalParent;

    [Header("Flow")]
    [SerializeField] private TutorialStep initialStep = TutorialStep.Movement;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool resetRuntimeStateOnBegin = true;
    [SerializeField] private bool suppressScoreDuringTutorial = true;
    [SerializeField, Min(0f)] private float movementRequiredVerticalDelta = 1.25f;
    [SerializeField, Range(0f, 1f)] private float grazeRequiredChargeRatio = 1f;
    [SerializeField, Min(1)] private int requiredShrimpCount = 10;
    [SerializeField, Min(1)] private int firstShopInkBottlePrice = 6;
    [SerializeField, Min(1)] private int secondShopShellShieldPrice = 4;
    [SerializeField, Min(1)] private int inkBottleBarrierEnemyCount = 4;
    [SerializeField] private bool emptyInkPulseBeforeInkBottleBarrier = true;
    [SerializeField, Min(0f)] private float forcedShopOpenFallbackSeconds = 4f;
    [SerializeField, Min(0f)] private float inkPulseAssistDelaySeconds = 1.25f;
    [SerializeField, Min(0f)] private float protectedHitSetupSeconds = 0.25f;
    [SerializeField, Min(0f)] private float finalEnemySetupSeconds = 0.25f;

    [Header("Phase Timing")]
    [SerializeField] private bool usePresentationPhase = true;
    [SerializeField] private bool freezeGameplayDuringPresentation = true;
    [SerializeField, Min(0f)] private float defaultPresentationSeconds = 7f;
    [SerializeField, Min(0f)] private float defaultPracticeSeconds = 10f;
    [SerializeField] private bool autoAdvanceWhenPracticeExpires = true;
    [SerializeField] private TutorialStepTimingOverride[] stepTimingOverrides = Array.Empty<TutorialStepTimingOverride>();

    [Header("Spawn Placement")]
    [SerializeField, Range(0f, 1.5f)] private float directedSpawnViewportX = 0.82f;
    [SerializeField, Range(0f, 1.5f)] private float portalViewportX = 0.82f;
    [SerializeField, Min(0f)] private float verticalBoundaryInset = 0.75f;
    [SerializeField, Min(0f)] private float grazeEnemyYOffset = 0.75f;
    [SerializeField, Min(0f)] private float shrimpSpacingX = 1.15f;
    [SerializeField, Min(0f)] private float inkBottleBarrierOffsetX = 1.2f;
    [SerializeField, Min(0f)] private float hazardPlayerOffsetX = 0.25f;

    [Header("System Gates")]
    [SerializeField] private bool controlLevelSpawner = true;
    [SerializeField] private TutorialStep levelSpawnerEnabledFromStep = TutorialStep.Completed;
    [SerializeField] private bool controlBossDirector = true;
    [SerializeField] private TutorialStep bossDirectorEnabledFromStep = TutorialStep.Completed;

    [Header("Local Zone Shift")]
    [SerializeField] private GameObject[] firstZoneVisualRoots = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] secondZoneVisualRoots = Array.Empty<GameObject>();

    [Header("Events")]
    public UnityEvent<TutorialStep> onStepEntered = new UnityEvent<TutorialStep>();
    public UnityEvent<TutorialStep> onPresentationStarted = new UnityEvent<TutorialStep>();
    public UnityEvent<TutorialStep> onPracticeStarted = new UnityEvent<TutorialStep>();
    public UnityEvent<TutorialStep, TutorialPhase> onPhaseStarted = new UnityEvent<TutorialStep, TutorialPhase>();
    public UnityEvent onSecondZoneVisualActivated = new UnityEvent();

    private float movementStartY;
    private float stepElapsedSeconds;
    private float phaseElapsedSeconds;
    private float timeScaleBeforePresentation = 1f;
    private int shrimpBaselineTotal;
    private bool hasMovementStart;
    private bool protectedHitResolved;
    private bool finalDeathResolved;
    private bool localPortalEntered;
    private bool secondZoneVisualApplied;
    private bool shopFallbackAttempted;
    private bool presentationFreezeActive;

    private GameObject activeGrazeEnemy;
    private GameObject activeInkPulseObstacle;
    private GameObject[] activeInkBottleBarrierEnemies = Array.Empty<GameObject>();
    private GameObject activeDealerFish;
    private GameObject activePortal;
    private GameObject activeProtectedEnemy;
    private GameObject activeFinalEnemy;
    private SSCarnageController activeCarnage;

    private InkPulseController subscribedInkPulse;
    private PlayerCollision subscribedPlayerCollision;
    private InGameShopManager subscribedShopManager;
    private RunProgressionDirector subscribedProgression;
    private bool runtimeGadgetInventorySubscribed;

    public TutorialStep CurrentStep { get; private set; } = TutorialStep.Inactive;
    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Inactive;
    public InkPulseController InkPulse => inkPulse;
    public bool IsCompleted => CurrentStep == TutorialStep.Completed;
    public float CurrentPhaseElapsedSeconds => phaseElapsedSeconds;
    public float CurrentPhaseDurationSeconds => CurrentPhase switch
    {
        TutorialPhase.Presentation => GetPresentationSeconds(CurrentStep),
        TutorialPhase.Practice => GetPracticeSeconds(CurrentStep),
        _ => 0f
    };

    public event Action<TutorialStep, TutorialStep> StepChanged;
    public event Action<TutorialStep, TutorialPhase> PhaseStarted;

    private void Awake()
    {
        ResolveReferences();
        RefreshRuntimeEventSubscriptions();
        ApplySystemGates();
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshRuntimeEventSubscriptions();
    }

    private void Start()
    {
        ResolveReferences();
        RefreshRuntimeEventSubscriptions();

        if (autoStart && CurrentStep == TutorialStep.Inactive)
        {
            BeginTutorial();
        }
    }

    private void Update()
    {
        ResolveReferences();
        RefreshRuntimeEventSubscriptions();
        ApplySystemGates();

        if (CurrentStep == TutorialStep.Inactive || CurrentStep == TutorialStep.Completed)
        {
            return;
        }

        SuppressTutorialScoreIfNeeded();

        if (session == null || !session.IsPlaying)
        {
            return;
        }

        EvaluateCurrentPhase();
    }

    private void OnDisable()
    {
        UnsubscribeFromRuntimeEvents();
        UnsubscribeFromCarnage();
        ReleasePresentationFreeze();
        SetInkPulseSuppressed(false);
    }

    public void BeginTutorial()
    {
        if (resetRuntimeStateOnBegin)
        {
            ResetVolatileRuntimeState();
        }

        TutorialStep startStep = initialStep == TutorialStep.Inactive || initialStep == TutorialStep.Completed
            ? TutorialStep.Movement
            : initialStep;

        SetStep(startStep, force: true);
    }

    public void ResetTutorial()
    {
        CleanupAllDirectedObjects();
        UnsubscribeFromCarnage();
        hasMovementStart = false;
        protectedHitResolved = false;
        finalDeathResolved = false;
        localPortalEntered = false;
        secondZoneVisualApplied = false;
        CurrentPhase = TutorialPhase.Inactive;
        ReleasePresentationFreeze();
        SetInkPulseSuppressed(false);
        SetStep(TutorialStep.Inactive, force: true);

        if (autoStart)
        {
            BeginTutorial();
        }
    }

    public void Advance()
    {
        SetStep(GetNextStep(CurrentStep));
    }

    public bool TryAdvanceFrom(TutorialStep expectedStep)
    {
        if (CurrentStep != expectedStep)
        {
            return false;
        }

        Advance();
        return true;
    }

    public void NotifyShopPresented()
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.FirstShopOpen || CurrentStep == TutorialStep.SecondShopOpen)
        {
            Advance();
        }
    }

    public void NotifyGadgetAcquiredOrUsed()
    {
        AdvanceIfRequiredGadgetOwned();
    }

    public void NotifyBossTutorialResolved()
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        TryAdvanceFrom(TutorialStep.CarnageInkPulseResolve);
    }

    public void NotifyPortalEntered()
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.PortalEnter)
        {
            localPortalEntered = true;
            Advance();
        }
    }

    private void EvaluateCurrentPhase()
    {
        phaseElapsedSeconds += Time.unscaledDeltaTime;

        if (CurrentPhase == TutorialPhase.Presentation)
        {
            if (phaseElapsedSeconds >= GetPresentationSeconds(CurrentStep))
            {
                BeginPracticePhase();
            }

            return;
        }

        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        stepElapsedSeconds += Time.unscaledDeltaTime;
        EvaluateCurrentStep();

        if (CurrentStep == TutorialStep.Inactive
            || CurrentStep == TutorialStep.Completed
            || CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        float practiceSeconds = GetPracticeSeconds(CurrentStep);
        if (practiceSeconds > 0f && phaseElapsedSeconds >= practiceSeconds)
        {
            HandlePracticeExpired();
        }
    }

    private void EvaluateCurrentStep()
    {
        switch (CurrentStep)
        {
            case TutorialStep.Movement:
                EvaluateMovementStep();
                break;
            case TutorialStep.GrazeCharge:
                if (inkPulse != null && inkPulse.ChargeRatio >= grazeRequiredChargeRatio)
                {
                    Advance();
                }
                break;
            case TutorialStep.CollectShrimps10:
                if (ShrimpRuntimeWallet.TotalShrimp - shrimpBaselineTotal >= requiredShrimpCount)
                {
                    Advance();
                }
                break;
            case TutorialStep.FirstShopOpen:
                EvaluateShopOpenStep(GadgetId.InkBottle, firstShopInkBottlePrice);
                break;
            case TutorialStep.BuyInkBottle:
                if (RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle))
                {
                    Advance();
                }
                break;
            case TutorialStep.InkBottleBarrier:
                if (!RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle))
                {
                    SetStep(TutorialStep.BuyInkBottle);
                }
                break;
            case TutorialStep.CarnageIntro:
                if (activeCarnage != null && activeCarnage.CurrentAttackState == SSCarnageAttackState.NetActive)
                {
                    Advance();
                }
                break;
            case TutorialStep.CarnageInkPulseAssist:
                if (inkPulse != null && !inkPulse.IsCharged && stepElapsedSeconds >= inkPulseAssistDelaySeconds)
                {
                    inkPulse.TryForceReady();
                }

                if (inkPulse == null || inkPulse.IsCharged || inkPulse.IsPulseActive)
                {
                    Advance();
                }
                break;
            case TutorialStep.CarnageInkPulseResolve:
                if (progression != null && progression.EventState == RunEventState.PostBossWindow)
                {
                    Advance();
                }
                break;
            case TutorialStep.SecondShopOpen:
                EvaluateShopOpenStep(GadgetId.ShellShield, secondShopShellShieldPrice);
                break;
            case TutorialStep.BuyShellShield:
                if (RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield))
                {
                    Advance();
                }
                break;
            case TutorialStep.ProtectedHitSetup:
                if (protectedHitResolved || stepElapsedSeconds >= protectedHitSetupSeconds)
                {
                    Advance();
                }
                break;
            case TutorialStep.ProtectedHitResolved:
                if (protectedHitResolved || !RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield))
                {
                    Advance();
                }
                break;
            case TutorialStep.PortalSpawn:
                if (activePortal != null)
                {
                    Advance();
                }
                break;
            case TutorialStep.PortalEnter:
                if (localPortalEntered)
                {
                    Advance();
                }
                break;
            case TutorialStep.VisualZoneShift:
                if (secondZoneVisualApplied)
                {
                    Advance();
                }
                break;
            case TutorialStep.FinalEnemy:
                if (finalDeathResolved || stepElapsedSeconds >= finalEnemySetupSeconds)
                {
                    Advance();
                }
                break;
            case TutorialStep.FinalDeath:
                if (finalDeathResolved || (session != null && session.IsGameOver))
                {
                    Advance();
                }
                break;
        }
    }

    private void EvaluateMovementStep()
    {
        if (player == null)
        {
            return;
        }

        if (!hasMovementStart)
        {
            movementStartY = player.position.y;
            hasMovementStart = true;
            return;
        }

        float delta = Mathf.Abs(player.position.y - movementStartY);
        if (delta >= movementRequiredVerticalDelta)
        {
            Advance();
        }
    }

    private void EvaluateShopOpenStep(GadgetId forcedGadget, int forcedPrice)
    {
        if (shopManager != null && shopManager.CurrentState == ShopEventState.Offering)
        {
            Advance();
            return;
        }

        if (shopFallbackAttempted || shopManager == null || stepElapsedSeconds < forcedShopOpenFallbackSeconds)
        {
            return;
        }

        shopFallbackAttempted = shopManager.TryOpenTutorialOffer(forcedGadget, forcedPrice);
    }

    private void SetStep(TutorialStep nextStep, bool force = false)
    {
        TutorialStep previousStep = CurrentStep;
        if (!force && previousStep == nextStep)
        {
            return;
        }

        CleanupStepArtifacts(previousStep, nextStep);
        CurrentStep = nextStep;
        ResetPhaseStateForStep(nextStep);
        ApplySystemGates();
        StepChanged?.Invoke(previousStep, nextStep);
        onStepEntered.Invoke(nextStep);
        BeginStepPhase(nextStep);
    }

    private void ResetPhaseStateForStep(TutorialStep step)
    {
        phaseElapsedSeconds = 0f;
        stepElapsedSeconds = 0f;
        shopFallbackAttempted = false;
        if (step == TutorialStep.Inactive || step == TutorialStep.Completed)
        {
            CurrentPhase = TutorialPhase.Inactive;
        }
    }

    private void BeginStepPhase(TutorialStep step)
    {
        if (step == TutorialStep.Inactive || step == TutorialStep.Completed)
        {
            ReleasePresentationFreeze();
            SetInkPulseSuppressed(false);
            StartPhase(TutorialPhase.Inactive);
            return;
        }

        if (usePresentationPhase && GetPresentationSeconds(step) > 0f)
        {
            BeginPresentationPhase();
            return;
        }

        BeginPracticePhase();
    }

    private void BeginPresentationPhase()
    {
        StartPhase(TutorialPhase.Presentation);

        if (!HasPresentationController())
        {
            SetInkPulseSuppressed(true);

            if (freezeGameplayDuringPresentation)
            {
                ApplyPresentationFreeze();
            }
        }

        onPresentationStarted.Invoke(CurrentStep);
    }

    private void BeginPracticePhase()
    {
        if (!HasPresentationController())
        {
            ReleasePresentationFreeze();
            SetInkPulseSuppressed(false);
        }

        StartPhase(TutorialPhase.Practice);
        PrepareStep(CurrentStep);
        onPracticeStarted.Invoke(CurrentStep);
    }

    private void StartPhase(TutorialPhase phase)
    {
        CurrentPhase = phase;
        phaseElapsedSeconds = 0f;
        stepElapsedSeconds = 0f;
        PhaseStarted?.Invoke(CurrentStep, phase);
        onPhaseStarted.Invoke(CurrentStep, phase);
    }

    private void PrepareStep(TutorialStep step)
    {
        stepElapsedSeconds = 0f;
        shopFallbackAttempted = false;

        switch (step)
        {
            case TutorialStep.Movement:
                hasMovementStart = false;
                break;
            case TutorialStep.GrazeCharge:
                SpawnGrazeEnemy();
                break;
            case TutorialStep.InkPulseObstacle:
                SpawnInkPulseObstacle();
                break;
            case TutorialStep.CollectShrimps10:
                shrimpBaselineTotal = ShrimpRuntimeWallet.TotalShrimp;
                SpawnShrimpLine();
                break;
            case TutorialStep.FirstShopOpen:
                QueueTutorialOffer(GadgetId.InkBottle, firstShopInkBottlePrice);
                SpawnDealerFish();
                break;
            case TutorialStep.InkBottleBarrier:
                PrepareInkBottleBarrier();
                break;
            case TutorialStep.CarnageIntro:
                SpawnCarnage();
                break;
            case TutorialStep.SecondShopOpen:
                QueueTutorialOffer(GadgetId.ShellShield, secondShopShellShieldPrice);
                SpawnDealerFish();
                break;
            case TutorialStep.ProtectedHitSetup:
                protectedHitResolved = false;
                SpawnProtectedHitEnemy();
                break;
            case TutorialStep.PortalSpawn:
                SpawnLocalPortal();
                break;
            case TutorialStep.VisualZoneShift:
                ApplySecondZoneVisuals();
                break;
            case TutorialStep.FinalEnemy:
                finalDeathResolved = false;
                SpawnFinalEnemy();
                break;
        }
    }

    private TutorialStep GetNextStep(TutorialStep step)
    {
        return step switch
        {
            TutorialStep.Inactive => TutorialStep.Movement,
            TutorialStep.Movement => TutorialStep.GrazeCharge,
            TutorialStep.GrazeCharge => TutorialStep.InkPulseObstacle,
            TutorialStep.InkPulseObstacle => TutorialStep.CollectShrimps10,
            TutorialStep.CollectShrimps10 => TutorialStep.FirstShopOpen,
            TutorialStep.FirstShopOpen => TutorialStep.BuyInkBottle,
            TutorialStep.BuyInkBottle => TutorialStep.InkBottleBarrier,
            TutorialStep.InkBottleBarrier => TutorialStep.CarnageIntro,
            TutorialStep.CarnageIntro => TutorialStep.CarnageInkPulseAssist,
            TutorialStep.CarnageInkPulseAssist => TutorialStep.CarnageInkPulseResolve,
            TutorialStep.CarnageInkPulseResolve => TutorialStep.SecondShopOpen,
            TutorialStep.SecondShopOpen => TutorialStep.BuyShellShield,
            TutorialStep.BuyShellShield => TutorialStep.ProtectedHitSetup,
            TutorialStep.ProtectedHitSetup => TutorialStep.ProtectedHitResolved,
            TutorialStep.ProtectedHitResolved => TutorialStep.PortalSpawn,
            TutorialStep.PortalSpawn => TutorialStep.PortalEnter,
            TutorialStep.PortalEnter => TutorialStep.VisualZoneShift,
            TutorialStep.VisualZoneShift => TutorialStep.FinalEnemy,
            TutorialStep.FinalEnemy => TutorialStep.FinalDeath,
            TutorialStep.FinalDeath => TutorialStep.Completed,
            _ => TutorialStep.Completed
        };
    }

    private void HandlePracticeExpired()
    {
        if (CurrentStep == TutorialStep.GrazeCharge && inkPulse != null && !inkPulse.IsCharged)
        {
            inkPulse.TryForceReady();
        }

        if (CanAdvanceWhenPracticeExpires(CurrentStep))
        {
            Advance();
        }
    }

    private bool CanAdvanceWhenPracticeExpires(TutorialStep step)
    {
        TutorialStepTimingOverride timingOverride = FindTimingOverride(step);
        if (timingOverride != null && timingOverride.overrideAutoAdvanceWhenPracticeExpires)
        {
            return timingOverride.autoAdvanceWhenPracticeExpires;
        }

        if (!autoAdvanceWhenPracticeExpires)
        {
            return false;
        }

        return !RequiresCompletionBeforeTimeout(step);
    }

    private bool RequiresCompletionBeforeTimeout(TutorialStep step)
    {
        return step switch
        {
            TutorialStep.InkPulseObstacle => true,
            TutorialStep.CollectShrimps10 => true,
            TutorialStep.FirstShopOpen => true,
            TutorialStep.BuyInkBottle => true,
            TutorialStep.InkBottleBarrier => true,
            TutorialStep.CarnageIntro => true,
            TutorialStep.CarnageInkPulseResolve => true,
            TutorialStep.SecondShopOpen => true,
            TutorialStep.BuyShellShield => true,
            TutorialStep.ProtectedHitResolved => true,
            TutorialStep.PortalEnter => true,
            TutorialStep.FinalDeath => true,
            _ => false
        };
    }

    private float GetPresentationSeconds(TutorialStep step)
    {
        TutorialStepTimingOverride timingOverride = FindTimingOverride(step);
        if (timingOverride != null && timingOverride.overridePresentationSeconds)
        {
            return Mathf.Max(0f, timingOverride.presentationSeconds);
        }

        return Mathf.Max(0f, defaultPresentationSeconds);
    }

    private float GetPracticeSeconds(TutorialStep step)
    {
        TutorialStepTimingOverride timingOverride = FindTimingOverride(step);
        if (timingOverride != null && timingOverride.overridePracticeSeconds)
        {
            return Mathf.Max(0f, timingOverride.practiceSeconds);
        }

        return Mathf.Max(0f, defaultPracticeSeconds);
    }

    private TutorialStepTimingOverride FindTimingOverride(TutorialStep step)
    {
        if (stepTimingOverrides == null)
        {
            return null;
        }

        for (int i = 0; i < stepTimingOverrides.Length; i++)
        {
            TutorialStepTimingOverride timingOverride = stepTimingOverrides[i];
            if (timingOverride != null && timingOverride.step == step)
            {
                return timingOverride;
            }
        }

        return null;
    }

    private void SpawnGrazeEnemy()
    {
        if (activeGrazeEnemy != null || grazeEnemyPrefab == null)
        {
            return;
        }

        Vector3 position = GetViewportSpawnPosition(directedSpawnViewportX, GetPlayerY() + grazeEnemyYOffset);
        activeGrazeEnemy = SpawnDirectedEnemy(grazeEnemyPrefab, grazeEnemyTag, position);
    }

    private void SpawnInkPulseObstacle()
    {
        if (activeInkPulseObstacle != null || inkPulseObstaclePrefab == null)
        {
            return;
        }

        Vector3 position = GetViewportSpawnPosition(directedSpawnViewportX, GetPlayerY());
        activeInkPulseObstacle = SpawnDirectedEnemy(inkPulseObstaclePrefab, inkPulseObstacleTag, position);
    }

    private void PrepareInkBottleBarrier()
    {
        if (emptyInkPulseBeforeInkBottleBarrier)
        {
            inkPulse?.ForceEmptyCharge();
        }

        SpawnInkBottleBarrier();
    }

    private void SpawnInkBottleBarrier()
    {
        if (HasAnyAlive(activeInkBottleBarrierEnemies))
        {
            return;
        }

        GameObject prefab = inkBottleBarrierEnemyPrefab != null
            ? inkBottleBarrierEnemyPrefab
            : inkPulseObstaclePrefab;

        if (prefab == null)
        {
            return;
        }

        int enemyCount = Mathf.Max(1, inkBottleBarrierEnemyCount);
        activeInkBottleBarrierEnemies = new GameObject[enemyCount];

        Vector3 anchor = GetViewportSpawnPosition(directedSpawnViewportX, GetPlayerY());
        float x = anchor.x + inkBottleBarrierOffsetX;
        Vector2 verticalRange = GetPlayerVerticalRangeOrFallback(anchor.y, enemyCount);

        for (int i = 0; i < enemyCount; i++)
        {
            float t = enemyCount == 1 ? 0.5f : (i + 0.5f) / enemyCount;
            float y = Mathf.Lerp(verticalRange.x, verticalRange.y, t);
            Vector3 position = new Vector3(x, y, 0f);
            activeInkBottleBarrierEnemies[i] = SpawnDirectedEnemy(prefab, inkBottleBarrierEnemyTag, position);
        }
    }

    private void SpawnShrimpLine()
    {
        if (shrimpPrefab == null)
        {
            return;
        }

        Vector3 basePosition = GetViewportSpawnPosition(directedSpawnViewportX, GetPlayerY());
        for (int i = 0; i < requiredShrimpCount; i++)
        {
            float yOffset = i % 2 == 0 ? 0f : verticalBoundaryInset * 0.5f;
            Vector3 position = new Vector3(
                basePosition.x + shrimpSpacingX * i,
                ClampToPlayerVerticalRange(basePosition.y + yOffset),
                0f);
            GameObject shrimp = Instantiate(shrimpPrefab, position, Quaternion.identity, GetSpawnParent());
            SpawnedObjectConfigurator.ConfigureCollectible(shrimp, GameplayTagCatalog.Shrimp);
        }
    }

    private void SpawnDealerFish()
    {
        if (activeDealerFish != null || dealerFishPrefab == null)
        {
            return;
        }

        Vector3 position = GetViewportSpawnPosition(directedSpawnViewportX, GetPlayerY());
        activeDealerFish = Instantiate(dealerFishPrefab, position, Quaternion.identity, GetSpawnParent());
        SpawnedObjectConfigurator.ConfigureCollectible(activeDealerFish, GameplayTagCatalog.Collectible);
    }

    private void SpawnCarnage()
    {
        if (activeCarnage != null || bossDirector == null)
        {
            return;
        }

        GameObject bossInstance = bossDirector.TriggerBossEventManually();
        activeCarnage = bossInstance != null ? bossInstance.GetComponentInChildren<SSCarnageController>() : null;
        if (activeCarnage != null)
        {
            activeCarnage.AttackStateChanged += HandleCarnageAttackStateChanged;
        }
    }

    private void SpawnProtectedHitEnemy()
    {
        if (activeProtectedEnemy != null || protectedHitEnemyPrefab == null)
        {
            return;
        }

        Vector3 position = GetPlayerContactPosition();
        activeProtectedEnemy = SpawnDirectedEnemy(protectedHitEnemyPrefab, protectedHitEnemyTag, position);
    }

    private void SpawnLocalPortal()
    {
        if (activePortal != null || portalPrefab == null)
        {
            return;
        }

        Vector3 position = GetViewportSpawnPosition(portalViewportX, GetPlayerY());
        Transform parent = tutorialPortalParent != null ? tutorialPortalParent : GetSpawnParent();
        activePortal = Instantiate(portalPrefab, position, Quaternion.identity, parent);
        SpawnedObjectConfigurator.ConfigureCollectible(activePortal, GameplayTagCatalog.Portal);

        if (activePortal.TryGetComponent(out ScenePortal portal))
        {
            portal.ConfigureLocalTransition(HandleLocalPortalEntered);
        }
    }

    private void SpawnFinalEnemy()
    {
        if (activeFinalEnemy != null)
        {
            return;
        }

        GameObject prefab = finalEnemyPrefab != null ? finalEnemyPrefab : protectedHitEnemyPrefab;
        if (prefab == null)
        {
            return;
        }

        Vector3 position = GetPlayerContactPosition();
        activeFinalEnemy = SpawnDirectedEnemy(prefab, finalEnemyTag, position);
    }

    private GameObject SpawnDirectedEnemy(GameObject prefab, string enemyTag, Vector3 position)
    {
        GameObject spawnedEnemy = Instantiate(prefab, position, Quaternion.identity, GetSpawnParent());
        SpawnedObjectConfigurator.ConfigureEnemy(spawnedEnemy, enemyTag, BuildEnemySpawnContext());
        return spawnedEnemy;
    }

    private EnemySpawnContext BuildEnemySpawnContext()
    {
        return new EnemySpawnContext(
            gameplayCamera,
            player,
            new PufferfishEnemyTuning(),
            new FishingRodEnemyTuning(),
            new RayEnemyTuning(),
            new JellyfishEnemyTuning());
    }

    private Transform GetSpawnParent()
    {
        if (tutorialSpawnParent != null)
        {
            return tutorialSpawnParent;
        }

        return levelSpawner != null ? levelSpawner.transform : null;
    }

    private Vector3 GetViewportSpawnPosition(float viewportX, float targetY)
    {
        if (gameplayCamera == null)
        {
            Vector3 fallback = player != null ? player.position : transform.position;
            return new Vector3(fallback.x + 6f, ClampToPlayerVerticalRange(targetY), 0f);
        }

        float depthToWorldZero = Mathf.Abs(gameplayCamera.transform.position.z);
        Vector3 viewportPosition = gameplayCamera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, depthToWorldZero));
        return new Vector3(viewportPosition.x, ClampToPlayerVerticalRange(targetY), 0f);
    }

    private Vector3 GetPlayerContactPosition()
    {
        Vector3 basePosition = player != null ? player.position : GetViewportSpawnPosition(directedSpawnViewportX, 0f);
        return new Vector3(basePosition.x + hazardPlayerOffsetX, ClampToPlayerVerticalRange(basePosition.y), 0f);
    }

    private float GetPlayerY()
    {
        return player != null ? player.position.y : transform.position.y;
    }

    private float ClampToPlayerVerticalRange(float y)
    {
        if (BoundaryReferenceResolver.TryResolveInnerVerticalRange(BoundaryReferenceDomain.Player, verticalBoundaryInset, out Vector2 range))
        {
            return Mathf.Clamp(y, range.x, range.y);
        }

        return y;
    }

    private Vector2 GetPlayerVerticalRangeOrFallback(float centerY, int slotCount)
    {
        if (BoundaryReferenceResolver.TryResolveInnerVerticalRange(BoundaryReferenceDomain.Player, verticalBoundaryInset, out Vector2 range))
        {
            return range;
        }

        float fallbackHalfHeight = Mathf.Max(1f, slotCount * 0.45f);
        return new Vector2(centerY - fallbackHalfHeight, centerY + fallbackHalfHeight);
    }

    private void QueueTutorialOffer(GadgetId gadget, int price)
    {
        if (shopManager != null)
        {
            shopManager.QueueTutorialOffer(gadget, price);
        }
    }

    private void ApplySecondZoneVisuals()
    {
        SetRootsActive(firstZoneVisualRoots, false);
        SetRootsActive(secondZoneVisualRoots, true);
        secondZoneVisualApplied = true;
        onSecondZoneVisualActivated.Invoke();
    }

    private void SetRootsActive(GameObject[] roots, bool active)
    {
        if (roots == null)
        {
            return;
        }

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
            {
                roots[i].SetActive(active);
            }
        }
    }

    private void HandleLocalPortalEntered(Collider2D playerCollider)
    {
        localPortalEntered = true;
        progression?.TryBeginTransition();
        progression?.CompleteTransition();
        NotifyPortalEntered();
    }

    private void CleanupStepArtifacts(TutorialStep previousStep, TutorialStep nextStep)
    {
        if (previousStep == TutorialStep.GrazeCharge)
        {
            DestroyIfAlive(ref activeGrazeEnemy);
        }

        if (previousStep == TutorialStep.InkPulseObstacle && nextStep != TutorialStep.InkPulseObstacle)
        {
            DestroyIfAlive(ref activeInkPulseObstacle);
        }

        if (previousStep == TutorialStep.InkBottleBarrier && nextStep != TutorialStep.InkBottleBarrier)
        {
            DestroyInkBottleBarrier();
        }

        if (previousStep == TutorialStep.FirstShopOpen || previousStep == TutorialStep.SecondShopOpen)
        {
            DestroyIfAlive(ref activeDealerFish);
        }

        if (previousStep == TutorialStep.ProtectedHitResolved)
        {
            DestroyIfAlive(ref activeProtectedEnemy);
        }

        if (previousStep == TutorialStep.PortalEnter)
        {
            DestroyIfAlive(ref activePortal);
        }
    }

    private void CleanupAllDirectedObjects()
    {
        DestroyIfAlive(ref activeGrazeEnemy);
        DestroyIfAlive(ref activeInkPulseObstacle);
        DestroyInkBottleBarrier();
        DestroyIfAlive(ref activeDealerFish);
        DestroyIfAlive(ref activePortal);
        DestroyIfAlive(ref activeProtectedEnemy);
        DestroyIfAlive(ref activeFinalEnemy);
    }

    private void DestroyInkBottleBarrier()
    {
        if (activeInkBottleBarrierEnemies == null)
        {
            activeInkBottleBarrierEnemies = Array.Empty<GameObject>();
            return;
        }

        for (int i = 0; i < activeInkBottleBarrierEnemies.Length; i++)
        {
            DestroyIfAlive(ref activeInkBottleBarrierEnemies[i]);
        }

        activeInkBottleBarrierEnemies = Array.Empty<GameObject>();
    }

    private void DestroyIfAlive(ref GameObject target)
    {
        if (target != null)
        {
            Destroy(target);
            target = null;
        }
    }

    private static bool HasAnyAlive(GameObject[] targets)
    {
        if (targets == null)
        {
            return false;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetVolatileRuntimeState()
    {
        RuntimeGadgetInventory.ResetForRuntime();
        RuntimeInkPulseState.ResetForRuntime();
        RuntimeRunScore.ResetForRuntime();
        RuntimeInGameShopLoreState.ResetForRuntime();
    }

    private void SuppressTutorialScoreIfNeeded()
    {
        if (suppressScoreDuringTutorial && RuntimeRunScore.TotalScore != 0)
        {
            RuntimeRunScore.ResetForRuntime();
        }
    }

    private void ApplyPresentationFreeze()
    {
        if (presentationFreezeActive)
        {
            return;
        }

        timeScaleBeforePresentation = Time.timeScale;
        Time.timeScale = 0f;
        presentationFreezeActive = true;
    }

    private void ReleasePresentationFreeze()
    {
        if (!presentationFreezeActive)
        {
            return;
        }

        presentationFreezeActive = false;
        if (session == null || session.IsPlaying)
        {
            Time.timeScale = timeScaleBeforePresentation;
        }
    }

    private void SetInkPulseSuppressed(bool suppressed)
    {
        if (inkPulse != null)
        {
            inkPulse.SetActivationSuppressed(suppressed);
        }
    }

    private bool HasPresentationController()
    {
        return presentationController != null && presentationController.isActiveAndEnabled;
    }

    private void ApplySystemGates()
    {
        if (controlLevelSpawner && levelSpawner != null)
        {
            levelSpawner.enabled = IsAtOrAfter(levelSpawnerEnabledFromStep);
        }

        if (controlBossDirector && bossDirector != null)
        {
            bossDirector.enabled = IsAtOrAfter(bossDirectorEnabledFromStep);
        }
    }

    private bool IsAtOrAfter(TutorialStep step)
    {
        return CurrentStep != TutorialStep.Inactive && CurrentStep >= step;
    }

    private void RefreshRuntimeEventSubscriptions()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (subscribedInkPulse != inkPulse)
        {
            if (subscribedInkPulse != null)
            {
                subscribedInkPulse.PulseStarted -= HandlePulseStarted;
            }

            subscribedInkPulse = inkPulse;
            if (subscribedInkPulse != null)
            {
                subscribedInkPulse.PulseStarted += HandlePulseStarted;
            }
        }

        if (subscribedPlayerCollision != playerCollision)
        {
            if (subscribedPlayerCollision != null)
            {
                subscribedPlayerCollision.HazardIgnoredByInkPulse -= HandleHazardIgnoredByInkPulse;
                subscribedPlayerCollision.HazardBlockedByShellShield -= HandleHazardBlockedByShellShield;
                subscribedPlayerCollision.HazardCausedGameOver -= HandleHazardCausedGameOver;
            }

            subscribedPlayerCollision = playerCollision;
            if (subscribedPlayerCollision != null)
            {
                subscribedPlayerCollision.HazardIgnoredByInkPulse += HandleHazardIgnoredByInkPulse;
                subscribedPlayerCollision.HazardBlockedByShellShield += HandleHazardBlockedByShellShield;
                subscribedPlayerCollision.HazardCausedGameOver += HandleHazardCausedGameOver;
            }
        }

        if (subscribedShopManager != shopManager)
        {
            if (subscribedShopManager != null)
            {
                subscribedShopManager.StateChanged -= HandleShopStateChanged;
                subscribedShopManager.onGadgetPurchased.RemoveListener(HandleGadgetPurchased);
            }

            subscribedShopManager = shopManager;
            if (subscribedShopManager != null)
            {
                subscribedShopManager.StateChanged += HandleShopStateChanged;
                subscribedShopManager.onGadgetPurchased.AddListener(HandleGadgetPurchased);
            }
        }

        if (subscribedProgression != progression)
        {
            if (subscribedProgression != null)
            {
                subscribedProgression.EventStateChanged -= HandleRunEventStateChanged;
            }

            subscribedProgression = progression;
            if (subscribedProgression != null)
            {
                subscribedProgression.EventStateChanged += HandleRunEventStateChanged;
            }
        }

        if (!runtimeGadgetInventorySubscribed)
        {
            RuntimeGadgetInventory.Changed += HandleRuntimeGadgetInventoryChanged;
            runtimeGadgetInventorySubscribed = true;
        }
    }

    private void UnsubscribeFromRuntimeEvents()
    {
        if (subscribedInkPulse != null)
        {
            subscribedInkPulse.PulseStarted -= HandlePulseStarted;
            subscribedInkPulse = null;
        }

        if (subscribedPlayerCollision != null)
        {
            subscribedPlayerCollision.HazardIgnoredByInkPulse -= HandleHazardIgnoredByInkPulse;
            subscribedPlayerCollision.HazardBlockedByShellShield -= HandleHazardBlockedByShellShield;
            subscribedPlayerCollision.HazardCausedGameOver -= HandleHazardCausedGameOver;
            subscribedPlayerCollision = null;
        }

        if (subscribedShopManager != null)
        {
            subscribedShopManager.StateChanged -= HandleShopStateChanged;
            subscribedShopManager.onGadgetPurchased.RemoveListener(HandleGadgetPurchased);
            subscribedShopManager = null;
        }

        if (subscribedProgression != null)
        {
            subscribedProgression.EventStateChanged -= HandleRunEventStateChanged;
            subscribedProgression = null;
        }

        if (runtimeGadgetInventorySubscribed)
        {
            RuntimeGadgetInventory.Changed -= HandleRuntimeGadgetInventoryChanged;
            runtimeGadgetInventorySubscribed = false;
        }
    }

    private void HandlePulseStarted()
    {
        if (CurrentPhase == TutorialPhase.Practice && CurrentStep == TutorialStep.CarnageInkPulseAssist)
        {
            Advance();
        }
    }

    private void HandleHazardIgnoredByInkPulse(Collider2D hazard)
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.InkPulseObstacle || CurrentStep == TutorialStep.InkBottleBarrier)
        {
            Advance();
        }
    }

    private void HandleHazardBlockedByShellShield(Collider2D hazard)
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep != TutorialStep.ProtectedHitSetup && CurrentStep != TutorialStep.ProtectedHitResolved)
        {
            return;
        }

        protectedHitResolved = true;
        if (CurrentStep == TutorialStep.ProtectedHitResolved)
        {
            Advance();
        }
    }

    private void HandleHazardCausedGameOver(Collider2D hazard)
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.FinalEnemy || CurrentStep == TutorialStep.FinalDeath)
        {
            finalDeathResolved = true;
        }
    }

    private void HandleShopStateChanged(ShopEventState previousState, ShopEventState nextState)
    {
        if (CurrentPhase == TutorialPhase.Practice && nextState == ShopEventState.Offering)
        {
            NotifyShopPresented();
        }
    }

    private void HandleGadgetPurchased(GadgetId gadget)
    {
        AdvanceIfRequiredGadgetOwned();
    }

    private void HandleRuntimeGadgetInventoryChanged()
    {
        AdvanceIfRequiredGadgetOwned();
    }

    private void AdvanceIfRequiredGadgetOwned()
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.BuyInkBottle && RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle))
        {
            Advance();
            return;
        }

        if (CurrentStep == TutorialStep.BuyShellShield && RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield))
        {
            Advance();
        }
    }

    private void HandleRunEventStateChanged(RunEventState previousState, RunEventState nextState)
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.CarnageInkPulseResolve && nextState == RunEventState.PostBossWindow)
        {
            Advance();
            return;
        }

        if (CurrentStep == TutorialStep.PortalEnter && nextState == RunEventState.Transitioning)
        {
            localPortalEntered = true;
            Advance();
        }
    }

    private void HandleCarnageAttackStateChanged(SSCarnageAttackState previousState, SSCarnageAttackState nextState)
    {
        if (CurrentPhase != TutorialPhase.Practice)
        {
            return;
        }

        if (CurrentStep == TutorialStep.CarnageIntro && nextState == SSCarnageAttackState.NetActive)
        {
            Advance();
            return;
        }

        if (CurrentStep == TutorialStep.CarnageInkPulseResolve
            && (nextState == SSCarnageAttackState.Resolved || nextState == SSCarnageAttackState.Finished))
        {
            Advance();
        }
    }

    private void UnsubscribeFromCarnage()
    {
        if (activeCarnage != null)
        {
            activeCarnage.AttackStateChanged -= HandleCarnageAttackStateChanged;
            activeCarnage = null;
        }
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (progression == null && RunProgressionDirector.HasInstance)
        {
            progression = RunProgressionDirector.Instance;
        }

        sceneFlow ??= FindFirstObjectByType<SceneFlowController>();
        levelSpawner ??= FindFirstObjectByType<LevelSpawner>();
        bossDirector ??= FindFirstObjectByType<BossEventDirector>();
        shopManager ??= InGameShopManager.HasInstance ? InGameShopManager.Instance : FindFirstObjectByType<InGameShopManager>();
        presentationController ??= GetComponent<TutorialPresentationController>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        ResolvePlayerReferences();
    }

    private void ResolvePlayerReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(GameplayTagCatalog.Player);
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (inkPulse == null && player != null)
        {
            inkPulse = player.GetComponentInChildren<InkPulseController>();
        }

        if (playerCollision == null && player != null)
        {
            playerCollision = player.GetComponentInChildren<PlayerCollision>();
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || progression == null || player == null || inkPulse == null || playerCollision == null || shopManager == null)
        {
            Debug.LogWarning(
                "[TutorialDirector] Faltan referencias base. Configura Session, Progression, Player, InkPulse, PlayerCollision e InGameShopManager para activar la secuencia tutorial.",
                this);
        }

        if (grazeEnemyPrefab == null
            || inkPulseObstaclePrefab == null
            || shrimpPrefab == null
            || dealerFishPrefab == null
            || portalPrefab == null
            || protectedHitEnemyPrefab == null)
        {
            Debug.LogWarning(
                "[TutorialDirector] Faltan prefabs dirigidos. Asigna GrazeEnemy, InkPulseObstacle, Shrimp, DealerFish, Portal y ProtectedHitEnemy para poder probar el tutorial completo.",
                this);
        }

        if (bossDirector == null)
        {
            Debug.LogWarning("[TutorialDirector] Falta BossEventDirector; el paso SS Carnage no podra dispararse de forma dirigida.", this);
        }
    }
}

[Serializable]
public sealed class TutorialStepTimingOverride
{
    public TutorialStep step = TutorialStep.Movement;
    public bool overridePresentationSeconds;
    [Min(0f)] public float presentationSeconds = 7f;
    public bool overridePracticeSeconds;
    [Min(0f)] public float practiceSeconds = 10f;
    public bool overrideAutoAdvanceWhenPracticeExpires;
    public bool autoAdvanceWhenPracticeExpires = true;
}
