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
    [SerializeField] private Transform player;
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private InGameShopManager shopManager;

    [Header("Flow")]
    [SerializeField] private TutorialStep initialStep = TutorialStep.Movement;
    [SerializeField] private bool autoStart = true;
    [SerializeField, Min(0f)] private float movementRequiredVerticalDelta = 1.25f;
    [SerializeField, Range(0f, 1f)] private float grazeRequiredChargeRatio = 0.25f;

    [Header("System Gates")]
    [SerializeField] private bool controlLevelSpawner = false;
    [SerializeField] private TutorialStep levelSpawnerEnabledFromStep = TutorialStep.Shop;
    [SerializeField] private bool controlBossDirector = false;
    [SerializeField] private TutorialStep bossDirectorEnabledFromStep = TutorialStep.BossAndNet;

    [Header("Events")]
    public UnityEvent<TutorialStep> onStepEntered = new UnityEvent<TutorialStep>();

    private float movementStartY;
    private bool hasMovementStart;

    public TutorialStep CurrentStep { get; private set; } = TutorialStep.Inactive;
    public bool IsCompleted => CurrentStep == TutorialStep.Completed;

    public event Action<TutorialStep, TutorialStep> StepChanged;

    private void Awake()
    {
        ResolveReferences();
        ApplySystemGates();
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        SubscribeToRuntimeEvents();
    }

    private void Start()
    {
        ResolveReferences();
        if (autoStart && CurrentStep == TutorialStep.Inactive)
        {
            BeginTutorial();
        }
    }

    private void Update()
    {
        ResolveReferences();
        ApplySystemGates();

        if (session == null || !session.IsPlaying || CurrentStep == TutorialStep.Inactive || CurrentStep == TutorialStep.Completed)
        {
            return;
        }

        EvaluateCurrentStep();
    }

    private void OnDisable()
    {
        UnsubscribeFromRuntimeEvents();
    }

    public void BeginTutorial()
    {
        TutorialStep startStep = initialStep == TutorialStep.Inactive || initialStep == TutorialStep.Completed
            ? TutorialStep.Movement
            : initialStep;

        SetStep(startStep, force: true);
    }

    public void ResetTutorial()
    {
        hasMovementStart = false;
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
        TryAdvanceFrom(TutorialStep.Shop);
    }

    public void NotifyGadgetAcquiredOrUsed()
    {
        TryAdvanceFrom(TutorialStep.Gadgets);
    }

    public void NotifyBossTutorialResolved()
    {
        TryAdvanceFrom(TutorialStep.BossAndNet);
    }

    public void NotifyPortalEntered()
    {
        TryAdvanceFrom(TutorialStep.Portal);
    }

    private void EvaluateCurrentStep()
    {
        switch (CurrentStep)
        {
            case TutorialStep.Movement:
                EvaluateMovementStep();
                break;
            case TutorialStep.Graze:
                if (inkPulse != null && inkPulse.ChargeRatio >= grazeRequiredChargeRatio)
                {
                    Advance();
                }
                break;
            case TutorialStep.InkPulse:
                if (inkPulse != null && inkPulse.IsPulseActive)
                {
                    Advance();
                }
                break;
            case TutorialStep.Shop:
                if (shopManager != null && shopManager.CurrentState == ShopEventState.Offering)
                {
                    Advance();
                }
                break;
            case TutorialStep.Gadgets:
                if (HasAnyRuntimeGadget())
                {
                    Advance();
                }
                break;
            case TutorialStep.BossAndNet:
                if (progression != null && progression.EventState == RunEventState.PostBossWindow)
                {
                    Advance();
                }
                break;
            case TutorialStep.Portal:
                if (progression != null && progression.EventState == RunEventState.Transitioning)
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

    private void SetStep(TutorialStep nextStep, bool force = false)
    {
        TutorialStep previousStep = CurrentStep;
        if (!force && previousStep == nextStep)
        {
            return;
        }

        CurrentStep = nextStep;
        PrepareStep(nextStep);
        ApplySystemGates();
        StepChanged?.Invoke(previousStep, nextStep);
        onStepEntered.Invoke(nextStep);
    }

    private void PrepareStep(TutorialStep step)
    {
        if (step == TutorialStep.Movement)
        {
            hasMovementStart = false;
        }
    }

    private TutorialStep GetNextStep(TutorialStep step)
    {
        return step switch
        {
            TutorialStep.Inactive => TutorialStep.Movement,
            TutorialStep.Movement => TutorialStep.Graze,
            TutorialStep.Graze => TutorialStep.InkPulse,
            TutorialStep.InkPulse => TutorialStep.Shop,
            TutorialStep.Shop => TutorialStep.Gadgets,
            TutorialStep.Gadgets => TutorialStep.BossAndNet,
            TutorialStep.BossAndNet => TutorialStep.Portal,
            TutorialStep.Portal => TutorialStep.Completed,
            _ => TutorialStep.Completed
        };
    }

    private bool HasAnyRuntimeGadget()
    {
        for (int i = 0; i < RuntimeGadgetInventory.SlotCount; i++)
        {
            if (RuntimeGadgetInventory.GetSlot(i) != GadgetId.None)
            {
                return true;
            }
        }

        return RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield)
            || RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle);
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

    private void SubscribeToRuntimeEvents()
    {
        if (inkPulse != null)
        {
            inkPulse.PulseStarted += HandlePulseStarted;
        }

        if (shopManager != null)
        {
            shopManager.StateChanged += HandleShopStateChanged;
            shopManager.onGadgetPurchased.AddListener(HandleGadgetPurchased);
        }

        if (progression != null)
        {
            progression.EventStateChanged += HandleRunEventStateChanged;
        }

        RuntimeGadgetInventory.Changed += HandleRuntimeGadgetInventoryChanged;
    }

    private void UnsubscribeFromRuntimeEvents()
    {
        if (inkPulse != null)
        {
            inkPulse.PulseStarted -= HandlePulseStarted;
        }

        if (shopManager != null)
        {
            shopManager.StateChanged -= HandleShopStateChanged;
            shopManager.onGadgetPurchased.RemoveListener(HandleGadgetPurchased);
        }

        if (progression != null)
        {
            progression.EventStateChanged -= HandleRunEventStateChanged;
        }

        RuntimeGadgetInventory.Changed -= HandleRuntimeGadgetInventoryChanged;
    }

    private void HandlePulseStarted()
    {
        TryAdvanceFrom(TutorialStep.InkPulse);
    }

    private void HandleShopStateChanged(ShopEventState previousState, ShopEventState nextState)
    {
        if (nextState == ShopEventState.Offering)
        {
            NotifyShopPresented();
        }
    }

    private void HandleGadgetPurchased(GadgetId gadget)
    {
        NotifyGadgetAcquiredOrUsed();
    }

    private void HandleRuntimeGadgetInventoryChanged()
    {
        if (CurrentStep == TutorialStep.Gadgets && HasAnyRuntimeGadget())
        {
            Advance();
        }
    }

    private void HandleRunEventStateChanged(RunEventState previousState, RunEventState nextState)
    {
        if (CurrentStep == TutorialStep.BossAndNet && nextState == RunEventState.PostBossWindow)
        {
            Advance();
            return;
        }

        if (CurrentStep == TutorialStep.Portal && nextState == RunEventState.Transitioning)
        {
            Advance();
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
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || progression == null || sceneFlow == null || player == null || inkPulse == null)
        {
            Debug.LogWarning(
                "[TutorialDirector] Faltan referencias base. Configura Session, Progression, SceneFlow, Player e InkPulse para activar la secuencia tutorial.",
                this);
        }
    }
}
