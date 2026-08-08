using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TouchGameplayControlsController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button inkPulseButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button gadgetSlot1Button;
    [SerializeField] private Button gadgetSlot2Button;

    [Header("Action Labels")]
    [SerializeField] private TMP_Text pauseActionLabel;

    [Header("Status Labels")]
    [SerializeField] private TMP_Text inkPulseStatusLabel;
    [SerializeField] private TMP_Text pauseStatusLabel;
    [SerializeField] private TMP_Text gadgetSlot1StatusLabel;
    [SerializeField] private TMP_Text gadgetSlot2StatusLabel;

    [Header("Runtime References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulse;
    [SerializeField] private PauseMenuManager pauseMenu;

    private GameSessionController subscribedSession;
    private InkPulseController subscribedInkPulse;
    private SquidInkPulseGameplayInputReader inputReader;
    public Button InkPulseButton => inkPulseButton;
    public Button PauseButton => pauseButton;
    public Button GadgetSlot1Button => gadgetSlot1Button;
    public Button GadgetSlot2Button => gadgetSlot2Button;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        RuntimeGadgetInventory.Changed += RefreshPresentation;
        ResolveReferences();
        RefreshPresentation();
    }

    private void Update()
    {
        ResolveReferences();
        RefreshPresentation();
    }

    private void OnDisable()
    {
        RuntimeGadgetInventory.Changed -= RefreshPresentation;
        BindSession(null);
        BindInkPulse(null);
    }

    public void RefreshPresentation()
    {
        GameSessionState sessionState = session != null
            ? session.CurrentState
            : GameSessionState.GameOver;
        bool timeAdvancing = Time.timeScale > 0f;
        bool commandChannelAvailable = SquidInkPulseInputRuntime.Gameplay?.IsEnabled == true;
        bool shopBlocking = InGameShopManager.BlocksInkPulseActivation;

        TouchGameplayControlPresentation pulsePresentation =
            TouchGameplayControlsPolicy.ResolveInkPulse(
                sessionState,
                timeAdvancing,
                commandChannelAvailable,
                shopBlocking,
                inkPulse != null && inkPulse.IsActivationSuppressed,
                inkPulse != null && inkPulse.IsPulseActive,
                inkPulse != null && inkPulse.IsCharged);
        ApplyPresentation(
            inkPulseButton,
            inkPulseStatusLabel,
            pulsePresentation);

        TouchGameplayControlPresentation pausePresentation =
            TouchGameplayControlsPolicy.ResolvePause(
                sessionState,
                commandChannelAvailable,
                pauseMenu != null && pauseMenu.CanTogglePauseNow);
        ApplyPresentation(pauseButton, pauseStatusLabel, pausePresentation);
        SetText(
            pauseActionLabel,
            pausePresentation.State == TouchGameplayControlState.Resume
                ? "REANUDAR"
                : "PAUSA");

        ApplyGadgetPresentation(0, gadgetSlot1Button, gadgetSlot1StatusLabel);
        ApplyGadgetPresentation(1, gadgetSlot2Button, gadgetSlot2StatusLabel);
    }

    private void ResolveReferences()
    {
        GameSessionController nextSession = session;
        if (nextSession == null)
        {
            nextSession = GameSessionController.Instance;
        }

        InkPulseController nextInkPulse = inkPulse;
        if (nextInkPulse == null)
        {
            nextInkPulse = FindFirstObjectByType<InkPulseController>();
        }

        session = nextSession;
        inkPulse = nextInkPulse;
        pauseMenu ??= FindFirstObjectByType<PauseMenuManager>();
        BindSession(nextSession);
        BindInkPulse(nextInkPulse);
    }

    private void BindSession(GameSessionController nextSession)
    {
        if (subscribedSession == nextSession)
        {
            return;
        }

        if (subscribedSession != null)
        {
            subscribedSession.StateChanged -= HandleSessionStateChanged;
        }

        subscribedSession = nextSession;
        if (subscribedSession != null)
        {
            subscribedSession.StateChanged += HandleSessionStateChanged;
        }
    }

    private void BindInkPulse(InkPulseController nextInkPulse)
    {
        if (subscribedInkPulse == nextInkPulse)
        {
            return;
        }

        if (subscribedInkPulse != null)
        {
            subscribedInkPulse.StateChanged -= HandleInkPulseStateChanged;
            subscribedInkPulse.ChargeChanged -= HandleInkPulseChargeChanged;
        }

        subscribedInkPulse = nextInkPulse;
        if (subscribedInkPulse != null)
        {
            subscribedInkPulse.StateChanged += HandleInkPulseStateChanged;
            subscribedInkPulse.ChargeChanged += HandleInkPulseChargeChanged;
        }
    }

    private void ApplyGadgetPresentation(int slotIndex, Button button, TMP_Text statusLabel)
    {
        GadgetId gadget = RuntimeGadgetInventory.GetSlot(slotIndex);
        TouchGameplayControlPresentation presentation =
            TouchGameplayControlsPolicy.ResolveGadget(
                session != null ? session.CurrentState : GameSessionState.GameOver,
                Time.timeScale > 0f,
                SquidInkPulseInputRuntime.Gameplay?.IsEnabled == true,
                InGameShopManager.BlocksInkPulseActivation,
                gadget,
                RuntimeGadgetInventory.HasGadget(gadget),
                CanApplyGadgetEffect(gadget));
        ApplyPresentation(button, statusLabel, presentation);
    }

    private bool CanApplyGadgetEffect(GadgetId gadget)
    {
        if (gadget != GadgetId.InkBottle)
        {
            return false;
        }

        return inkPulse != null
            && !inkPulse.IsPulseActive
            && !inkPulse.IsCharged
            && !inkPulse.IsActivationSuppressed;
    }

    private static void ApplyPresentation(
        Button button,
        TMP_Text statusLabel,
        TouchGameplayControlPresentation presentation)
    {
        if (button != null)
        {
            button.interactable = presentation.Interactable;
        }

        SetText(statusLabel, GetStatusText(presentation.State));
    }

    private static string GetStatusText(TouchGameplayControlState state)
    {
        return state switch
        {
            TouchGameplayControlState.Ready => "LISTO",
            TouchGameplayControlState.Charging => "CARGANDO",
            TouchGameplayControlState.Active => "ACTIVO",
            TouchGameplayControlState.Empty => "VACIO",
            TouchGameplayControlState.Passive => "PASIVO",
            TouchGameplayControlState.Pause => "TOCA",
            TouchGameplayControlState.Resume => "TOCA",
            _ => "BLOQUEADO"
        };
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null && label.text != value)
        {
            label.text = value;
        }
    }

    private void HandleSessionStateChanged(
        GameSessionState previousState,
        GameSessionState nextState)
    {
        RefreshPresentation();
    }

    private void HandleInkPulseStateChanged(
        InkPulseState previousState,
        InkPulseState nextState)
    {
        RefreshPresentation();
    }

    private void HandleInkPulseChargeChanged(float chargeRatio)
    {
        RefreshPresentation();
    }

}
