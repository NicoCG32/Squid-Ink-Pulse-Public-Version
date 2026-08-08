using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class SquidInkPulseInputRuntime
{
    public static SquidInkPulseGameplayInputReader Gameplay { get; private set; }
    public static event Action<SquidInkPulseGameplayInputReader> GameplayChanged;

    private static UnityEngine.Object gameplayOwner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Gameplay?.Dispose();
        Gameplay = null;
        gameplayOwner = null;
        GameplayChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void DisableProjectWideActions()
    {
        InputActionAsset projectWideActions = InputSystem.actions;
        if (projectWideActions == null)
        {
            Debug.LogError("[SquidInkPulseInputRuntime] Project-wide Input Actions no esta configurado.");
            return;
        }

        projectWideActions.Disable();
    }

    internal static void ActivateGameplayScope(UnityEngine.Object owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (gameplayOwner != null)
        {
            if (gameplayOwner != owner)
            {
                Debug.LogError("[SquidInkPulseInputRuntime] Ya existe un scope de gameplay activo.", owner);
            }

            return;
        }

        InputActionAsset projectWideActions = InputSystem.actions;
        if (projectWideActions == null)
        {
            Debug.LogError("[SquidInkPulseInputRuntime] Project-wide Input Actions no esta configurado.", owner);
            return;
        }

        try
        {
            Gameplay = new SquidInkPulseGameplayInputReader(projectWideActions);
            Gameplay.Enable();
            gameplayOwner = owner;
        }
        catch (Exception exception)
        {
            Gameplay?.Dispose();
            Gameplay = null;
            gameplayOwner = null;
            Debug.LogException(exception);
            return;
        }

        GameplayChanged?.Invoke(Gameplay);
    }

    internal static void DeactivateGameplayScope(UnityEngine.Object owner)
    {
        if (gameplayOwner != owner)
        {
            return;
        }

        SquidInkPulseGameplayInputReader previousGameplay = Gameplay;
        Gameplay = null;
        gameplayOwner = null;
        previousGameplay?.Dispose();
        GameplayChanged?.Invoke(null);
    }
}
