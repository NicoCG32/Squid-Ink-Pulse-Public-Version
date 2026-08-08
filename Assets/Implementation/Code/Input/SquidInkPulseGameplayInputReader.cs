using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public sealed class SquidInkPulseGameplayInputReader : IDisposable
{
    private readonly InputActionAsset inputActions;
    private readonly InputActionMap gameplay;
    private readonly InputAction steerPosition;
    private readonly InputAction activateInkPulse;
    private readonly InputAction togglePause;
    private readonly InputAction useGadgetSlot1;
    private readonly InputAction useGadgetSlot2;
    private readonly InputAction buyShopOffer;

    private bool isEnabled;
    private bool isDisposed;
    private bool hasDeviceSteerPosition;
    private Vector2 currentDeviceSteerPosition;
    private UnityEngine.Object touchSteeringOwner;
    private int touchSteeringPointerId;
    private bool hasTouchSteering;
    private Vector2 currentTouchSteerPosition;
    private string currentControlScheme = string.Empty;
    private double minimumAcceptedEventTime;

    public bool IsEnabled => isEnabled && gameplay.enabled;
    public bool HasSteerPosition => IsEnabled && (hasTouchSteering || hasDeviceSteerPosition);
    public Vector2 SteerPosition => IsEnabled
        ? hasTouchSteering
            ? currentTouchSteerPosition
            : currentDeviceSteerPosition
        : Vector2.zero;
    public string CurrentControlScheme => currentControlScheme;

    public event Action InkPulseRequested;
    public event Action PauseToggleRequested;
    public event Action GadgetSlot1Requested;
    public event Action GadgetSlot2Requested;
    public event Action ShopPurchaseRequested;
    public event Action<string> ControlSchemeChanged;

    public SquidInkPulseGameplayInputReader(InputActionAsset inputActions)
    {
        this.inputActions = inputActions != null
            ? inputActions
            : throw new ArgumentNullException(nameof(inputActions));

        gameplay = inputActions.FindActionMap(
            SquidInkPulseInputContract.GameplayMap,
            throwIfNotFound: true);
        steerPosition = FindGameplayAction(SquidInkPulseInputContract.Gameplay.SteerPosition);
        activateInkPulse = FindGameplayAction(SquidInkPulseInputContract.Gameplay.ActivateInkPulse);
        togglePause = FindGameplayAction(SquidInkPulseInputContract.Gameplay.TogglePause);
        useGadgetSlot1 = FindGameplayAction(SquidInkPulseInputContract.Gameplay.UseGadgetSlot1);
        useGadgetSlot2 = FindGameplayAction(SquidInkPulseInputContract.Gameplay.UseGadgetSlot2);
        buyShopOffer = FindGameplayAction(SquidInkPulseInputContract.Gameplay.BuyShopOffer);
    }

    public void Enable()
    {
        ThrowIfDisposed();
        if (isEnabled && gameplay.enabled)
        {
            return;
        }

        // Project-wide actions arrive fully enabled in Play Mode. Normalize the
        // owned asset first so its UI map cannot compete with the UI module asset.
        inputActions.Disable();
        minimumAcceptedEventTime = InputState.currentTime;

        if (!isEnabled)
        {
            gameplay.actionTriggered += OnGameplayActionTriggered;
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }

        try
        {
            gameplay.Enable();
            isEnabled = true;
            InitializeSteerPositionFromResolvedControl();
        }
        catch
        {
            gameplay.actionTriggered -= OnGameplayActionTriggered;
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            gameplay.Disable();
            isEnabled = false;
            throw;
        }
    }

    public void Disable()
    {
        if (!isEnabled)
        {
            return;
        }

        isEnabled = false;
        gameplay.actionTriggered -= OnGameplayActionTriggered;
        InputSystem.onDeviceChange -= OnInputDeviceChange;
        gameplay.Disable();
        ResetTransientState();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        Disable();
        isDisposed = true;
    }

    public bool TryBeginTouchSteering(
        UnityEngine.Object owner,
        int pointerId,
        Vector2 screenPosition)
    {
        if (!IsEnabled || owner == null)
        {
            return false;
        }

        if (hasTouchSteering
            && (!ReferenceEquals(touchSteeringOwner, owner)
                || touchSteeringPointerId != pointerId))
        {
            return false;
        }

        touchSteeringOwner = owner;
        touchSteeringPointerId = pointerId;
        currentTouchSteerPosition = screenPosition;
        hasTouchSteering = true;
        UpdateLogicalControlScheme(SquidInkPulseInputContract.ControlSchemes.Touch);
        return true;
    }

    public bool TryUpdateTouchSteering(
        UnityEngine.Object owner,
        int pointerId,
        Vector2 screenPosition)
    {
        if (!IsEnabled
            || !hasTouchSteering
            || !ReferenceEquals(touchSteeringOwner, owner)
            || touchSteeringPointerId != pointerId)
        {
            return false;
        }

        currentTouchSteerPosition = screenPosition;
        UpdateLogicalControlScheme(SquidInkPulseInputContract.ControlSchemes.Touch);
        return true;
    }

    public bool TryEndTouchSteering(UnityEngine.Object owner, int pointerId)
    {
        if (!hasTouchSteering
            || !ReferenceEquals(touchSteeringOwner, owner)
            || touchSteeringPointerId != pointerId)
        {
            return false;
        }

        ClearTouchSteering();
        return true;
    }

    public bool CancelTouchSteering(UnityEngine.Object owner)
    {
        if (!hasTouchSteering || !ReferenceEquals(touchSteeringOwner, owner))
        {
            return false;
        }

        ClearTouchSteering();
        return true;
    }

    public bool TryRequestTouchCommand(SquidInkPulseGameplayCommand command)
    {
        if (!IsEnabled)
        {
            return false;
        }

        UpdateLogicalControlScheme(SquidInkPulseInputContract.ControlSchemes.Touch);
        switch (command)
        {
            case SquidInkPulseGameplayCommand.ActivateInkPulse:
                InkPulseRequested?.Invoke();
                break;
            case SquidInkPulseGameplayCommand.TogglePause:
                PauseToggleRequested?.Invoke();
                break;
            case SquidInkPulseGameplayCommand.UseGadgetSlot1:
                GadgetSlot1Requested?.Invoke();
                break;
            case SquidInkPulseGameplayCommand.UseGadgetSlot2:
                GadgetSlot2Requested?.Invoke();
                break;
            case SquidInkPulseGameplayCommand.BuyShopOffer:
                ShopPurchaseRequested?.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }

        return true;
    }

    private InputAction FindGameplayAction(string actionName)
    {
        return gameplay.FindAction(actionName, throwIfNotFound: true);
    }

    private void InitializeSteerPositionFromResolvedControl()
    {
        foreach (InputControl control in steerPosition.controls)
        {
            if (control is Vector2Control vector2Control && control.device.added)
            {
                currentDeviceSteerPosition = vector2Control.ReadValue();
                hasDeviceSteerPosition = true;
                return;
            }
        }

        currentDeviceSteerPosition = Vector2.zero;
        hasDeviceSteerPosition = false;
    }

    private void OnGameplayActionTriggered(InputAction.CallbackContext context)
    {
        if (!isEnabled)
        {
            return;
        }

        if (context.action == steerPosition)
        {
            if (context.time <= minimumAcceptedEventTime)
            {
                // A queued event from the previous lifecycle must not leave the
                // continuous cache stale after it updates the underlying control.
                InitializeSteerPositionFromResolvedControl();
                return;
            }

            if (context.performed)
            {
                hasDeviceSteerPosition = true;
                currentDeviceSteerPosition = context.ReadValue<Vector2>();
                if (!hasTouchSteering)
                {
                    UpdateControlScheme(context.control?.device);
                }
            }
            else if (context.canceled)
            {
                // Position is a Value action, so returning to the valid screen
                // coordinate (0, 0) is reported as canceled. Device removal can
                // also cancel it, so availability must be resolved again.
                InitializeSteerPositionFromResolvedControl();
                if (hasDeviceSteerPosition && !hasTouchSteering)
                {
                    UpdateControlScheme(context.control?.device);
                }
            }

            return;
        }

        if (context.time <= minimumAcceptedEventTime)
        {
            return;
        }

        if (!context.performed)
        {
            return;
        }

        UpdateControlScheme(context.control?.device);

        if (context.action == activateInkPulse)
        {
            InkPulseRequested?.Invoke();
        }
        else if (context.action == togglePause)
        {
            PauseToggleRequested?.Invoke();
        }
        else if (context.action == useGadgetSlot1)
        {
            GadgetSlot1Requested?.Invoke();
        }
        else if (context.action == useGadgetSlot2)
        {
            GadgetSlot2Requested?.Invoke();
        }
        else if (context.action == buyShopOffer)
        {
            ShopPurchaseRequested?.Invoke();
        }
    }

    private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!isEnabled)
        {
            return;
        }

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Enabled:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.Disabled:
            case InputDeviceChange.ConfigurationChanged:
                InitializeSteerPositionFromResolvedControl();
                break;
        }
    }

    private void UpdateControlScheme(InputDevice device)
    {
        if (device == null)
        {
            return;
        }

        InputControlScheme? resolvedScheme = InputControlScheme.FindControlSchemeForDevices(
            InputSystem.devices,
            inputActions.controlSchemes,
            mustIncludeDevice: device);

        if (!resolvedScheme.HasValue)
        {
            foreach (InputControlScheme scheme in inputActions.controlSchemes)
            {
                if (scheme.SupportsDevice(device))
                {
                    resolvedScheme = scheme;
                    break;
                }
            }
        }

        if (!resolvedScheme.HasValue)
        {
            return;
        }

        UpdateLogicalControlScheme(resolvedScheme.Value.name);
    }

    private void UpdateLogicalControlScheme(string schemeName)
    {
        if (string.IsNullOrEmpty(schemeName) || schemeName == currentControlScheme)
        {
            return;
        }

        currentControlScheme = schemeName;
        ControlSchemeChanged?.Invoke(currentControlScheme);
    }

    private void ResetTransientState()
    {
        hasDeviceSteerPosition = false;
        currentDeviceSteerPosition = Vector2.zero;
        ClearTouchSteering();
        currentControlScheme = string.Empty;
    }

    private void ClearTouchSteering()
    {
        touchSteeringOwner = null;
        touchSteeringPointerId = 0;
        hasTouchSteering = false;
        currentTouchSteerPosition = Vector2.zero;
    }

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(SquidInkPulseGameplayInputReader));
        }
    }
}
