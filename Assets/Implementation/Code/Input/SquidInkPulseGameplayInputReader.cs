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

    private bool isEnabled;
    private bool isDisposed;
    private bool hasSteerPosition;
    private Vector2 currentSteerPosition;
    private string currentControlScheme = string.Empty;
    private double minimumAcceptedEventTime;

    public bool IsEnabled => isEnabled && gameplay.enabled;
    public bool HasSteerPosition => IsEnabled && hasSteerPosition;
    public Vector2 SteerPosition => IsEnabled ? currentSteerPosition : Vector2.zero;
    public string CurrentControlScheme => currentControlScheme;

    public event Action InkPulseRequested;
    public event Action PauseToggleRequested;
    public event Action GadgetSlot1Requested;
    public event Action GadgetSlot2Requested;
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
                currentSteerPosition = vector2Control.ReadValue();
                hasSteerPosition = true;
                return;
            }
        }

        currentSteerPosition = Vector2.zero;
        hasSteerPosition = false;
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
                hasSteerPosition = true;
                currentSteerPosition = context.ReadValue<Vector2>();
                UpdateControlScheme(context.control?.device);
            }
            else if (context.canceled)
            {
                // Position is a Value action, so returning to the valid screen
                // coordinate (0, 0) is reported as canceled. Device removal can
                // also cancel it, so availability must be resolved again.
                InitializeSteerPositionFromResolvedControl();
                if (hasSteerPosition)
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

        if (!resolvedScheme.HasValue || resolvedScheme.Value.name == currentControlScheme)
        {
            return;
        }

        currentControlScheme = resolvedScheme.Value.name;
        ControlSchemeChanged?.Invoke(currentControlScheme);
    }

    private void ResetTransientState()
    {
        hasSteerPosition = false;
        currentSteerPosition = Vector2.zero;
        currentControlScheme = string.Empty;
    }

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(SquidInkPulseGameplayInputReader));
        }
    }
}
