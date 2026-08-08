using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class GameplayPauseInputPlayModeTests : InputTestFixture
    {
        private InputActionAsset inputActions;
        private SquidInkPulseGameplayInputReader reader;
        private GameObject inputScopeRoot;
        private GameObject sessionRoot;
        private GameObject inkPulseRoot;
        private GameSessionController session;
        private InkPulseController inkPulse;
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            RuntimeInkPulseState.ResetForRuntime();

            inputActions = InputSystem.actions;
            Assert.That(inputActions, Is.Not.Null, "Falta el asset project-wide de input.");
            keyboard = InputSystem.AddDevice<Keyboard>();

            inputScopeRoot = new GameObject("PauseInputPlayModeScope");
            inputScopeRoot.AddComponent<SquidInkPulseGameplayInputScope>();
            reader = SquidInkPulseInputRuntime.Gameplay;
            Assert.That(reader, Is.Not.Null, "El scope real no publico el reader de gameplay.");

            Assert.That(GameSessionController.HasInstance, Is.False,
                "La escena de prueba ya contenia una sesion y no permite aislar el ciclo real.");
            sessionRoot = new GameObject("PauseInputPlayModeSession");
            session = sessionRoot.AddComponent<GameSessionController>();
            Assert.That(GameSessionController.Instance, Is.SameAs(session));

            inkPulseRoot = new GameObject("PauseInputPlayModeInkPulse");
            inkPulse = inkPulseRoot.AddComponent<InkPulseController>();
        }

        public override void TearDown()
        {
            if (inkPulse != null)
            {
                inkPulse.ForceEmptyCharge();
            }

            if (inkPulseRoot != null)
            {
                Object.DestroyImmediate(inkPulseRoot);
            }

            if (sessionRoot != null)
            {
                Object.DestroyImmediate(sessionRoot);
            }

            if (inputScopeRoot != null)
            {
                Object.DestroyImmediate(inputScopeRoot);
            }

            Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Null);
            RuntimeInkPulseState.ResetForRuntime();
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PausedSession_ConsumesInkPulseWithoutExecutingOrReplaying()
        {
            LogAssert.Expect(
                LogType.Warning,
                "[InkPulseController] Faltan referencias. Asigna Session y ChargeBar en el Inspector.");
            yield return null;

            inkPulse.ForceEmptyCharge();
            Assert.That(inkPulse.TryForceReady(), Is.True);
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(reader.IsEnabled, Is.True);

            int pauseRequests = 0;
            int inkPulseRequests = 0;
            int pulseStarts = 0;
            InputAction pauseAction = inputActions.FindAction(
                $"{SquidInkPulseInputContract.GameplayMap}/{SquidInkPulseInputContract.Gameplay.TogglePause}",
                throwIfNotFound: true);
            int rawPausePerformed = 0;
            pauseAction.performed += _ => rawPausePerformed++;
            reader.PauseToggleRequested += () => pauseRequests++;
            reader.InkPulseRequested += () => inkPulseRequests++;
            inkPulse.PulseStarted += () => pulseStarts++;

            session.RequestPause();
            Assert.That(session.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(reader.IsEnabled, Is.True);
            Assert.That(pauseAction.enabled, Is.True);
            Assert.That(pauseAction.controls.Any(control => control.device == keyboard), Is.True);

            AdvanceInputTime();
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That(rawPausePerformed, Is.EqualTo(1));
            Assert.That(pauseRequests, Is.EqualTo(1));
            AdvanceInputTime();
            Release(keyboard.escapeKey);
            yield return null;

            AdvanceInputTime();
            Press(keyboard.spaceKey);
            yield return null;
            Assert.That(inkPulseRequests, Is.EqualTo(1));
            Assert.That(pulseStarts, Is.Zero);
            Assert.That(inkPulse.IsPulseActive, Is.False);
            Assert.That(inkPulse.IsCharged, Is.True);
            AdvanceInputTime();
            Release(keyboard.spaceKey);
            yield return null;

            session.RequestResume();
            yield return null;

            Assert.That(session.IsPlaying, Is.True);
            Assert.That(pulseStarts, Is.Zero);
            Assert.That(inkPulse.IsPulseActive, Is.False);
            Assert.That(inkPulse.IsCharged, Is.True);

            AdvanceInputTime();
            Press(keyboard.spaceKey);
            yield return null;
            Assert.That(inkPulseRequests, Is.EqualTo(2));
            Assert.That(pulseStarts, Is.EqualTo(1));
            Assert.That(inkPulse.IsPulseActive, Is.True);
            Assert.That(inkPulse.IsCharged, Is.False);
            AdvanceInputTime();
            Release(keyboard.spaceKey);
            yield return null;
        }

        private void AdvanceInputTime()
        {
            currentTime += 0.01d;
        }
    }
}
