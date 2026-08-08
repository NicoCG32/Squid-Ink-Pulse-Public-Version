using System;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private Vector2 currentSteerPosition;
    private string currentControlScheme = string.Empty;
    private double minimumAcceptedEventTime;

    public bool IsEnabled => isEnabled && gameplay.enabled;
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
        }

        try
        {
            gameplay.Enable();
            isEnabled = true;
        }
        catch
        {
            gameplay.actionTriggered -= OnGameplayActionTriggered;
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

    private void OnGameplayActionTriggered(InputAction.CallbackContext context)
    {
        if (!isEnabled || context.time <= minimumAcceptedEventTime)
        {
            return;
        }

        if (context.action == steerPosition)
        {
            if (context.performed)
            {
                currentSteerPosition = context.ReadValue<Vector2>();
                UpdateControlScheme(context.control?.device);
            }
            else if (context.canceled)
            {
                currentSteerPosition = Vector2.zero;
            }

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
