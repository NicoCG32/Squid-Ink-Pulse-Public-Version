using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class GameplayInputReaderTests : InputTestFixture
    {
        private const string InputActionsAssetPath =
            "Assets/Implementation/Config/Input/InputSystem_Actions.inputactions";
        private const string PlayerPrefabPath =
            "Assets/Content/Prefabs/Player/BabySquid.prefab";

        private InputActionAsset inputActions;
        private SquidInkPulseGameplayInputReader reader;
        private Keyboard keyboard;
        private Mouse mouse;

        public override void Setup()
        {
            base.Setup();

            InputActionAsset source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
            Assert.That(source, Is.Not.Null, $"No se pudo cargar {InputActionsAssetPath}.");

            inputActions = InputActionAsset.FromJson(source.ToJson());
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            reader = new SquidInkPulseGameplayInputReader(inputActions);
        }

        public override void TearDown()
        {
            reader?.Dispose();

            if (inputActions != null)
            {
                UnityEngine.Object.DestroyImmediate(inputActions);
            }

            base.TearDown();
        }

        [Test]
        public void EnableAndDisable_AreIdempotent_AndOwnOnlyGameplayMap()
        {
            InputActionMap gameplay = FindMap(SquidInkPulseInputContract.GameplayMap);
            InputActionMap ui = FindMap(SquidInkPulseInputContract.UiMap);
            int inkPulseRequests = 0;
            reader.InkPulseRequested += () => inkPulseRequests++;

            inputActions.Enable();
            Assert.That(gameplay.enabled, Is.True);
            Assert.That(ui.enabled, Is.True);

            reader.Enable();
            reader.Enable();

            Assert.That(reader.IsEnabled, Is.True);
            Assert.That(gameplay.enabled, Is.True);
            Assert.That(ui.enabled, Is.False);

            AdvanceInputTime();
            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            Assert.That(inkPulseRequests, Is.EqualTo(1));

            reader.Disable();
            reader.Disable();
            Assert.That(reader.IsEnabled, Is.False);
            Assert.That(gameplay.enabled, Is.False);
            Assert.That(ui.enabled, Is.False);

            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            Assert.That(inkPulseRequests, Is.EqualTo(1));
        }

        [Test]
        public void SteerPosition_TracksLatestValue_AndClearsWhenDisabled()
        {
            reader.Enable();
            AdvanceInputTime();

            Move(mouse.position, new Vector2(320f, 240f));
            Assert.That(reader.SteerPosition, Is.EqualTo(new Vector2(320f, 240f)));

            Move(mouse.position, new Vector2(987f, 654f));
            Assert.That(reader.SteerPosition, Is.EqualTo(new Vector2(987f, 654f)));

            reader.Disable();
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void DiscreteCommands_EmitOncePerPress()
        {
            reader.Enable();
            AdvanceInputTime();

            AssertSingleEventPerPress(keyboard.spaceKey, handler => reader.InkPulseRequested += handler);
            AssertSingleEventPerPress(keyboard.pKey, handler => reader.PauseToggleRequested += handler);
            AssertSingleEventPerPress(keyboard.qKey, handler => reader.GadgetSlot1Requested += handler);
            AssertSingleEventPerPress(keyboard.wKey, handler => reader.GadgetSlot2Requested += handler);
        }

        [Test]
        public void ControlScheme_ChangesOnlyWhenPerformedDeviceSchemeChanges()
        {
            InputAction activateInkPulse = FindMap(SquidInkPulseInputContract.GameplayMap).FindAction(
                SquidInkPulseInputContract.Gameplay.ActivateInkPulse,
                throwIfNotFound: true);
            activateInkPulse.AddBinding("<Gamepad>/buttonSouth", groups: SquidInkPulseInputContract.ControlSchemes.Gamepad);
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            var observedSchemes = new List<string>();
            reader.ControlSchemeChanged += observedSchemes.Add;
            reader.Enable();
            AdvanceInputTime();

            Move(mouse.position, new Vector2(100f, 200f));
            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            Press(gamepad.buttonSouth);
            Release(gamepad.buttonSouth);

            Assert.That(observedSchemes, Is.EqualTo(new[]
            {
                SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse,
                SquidInkPulseInputContract.ControlSchemes.Gamepad
            }));
            Assert.That(
                reader.CurrentControlScheme,
                Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.Gamepad));
        }

        [Test]
        public void ReenableBeforeUpdate_DropsACommandQueuedByThePreviousLifecycle()
        {
            int gadgetRequests = 0;
            reader.GadgetSlot1Requested += () => gadgetRequests++;
            reader.Enable();
            AdvanceInputTime();

            Press(keyboard.qKey, queueEventOnly: true);
            reader.Disable();
            reader.Enable();
            InputSystem.Update();

            Assert.That(gadgetRequests, Is.Zero);
            AdvanceInputTime();
            Release(keyboard.qKey);
            Assert.That(gadgetRequests, Is.Zero);

            Press(keyboard.qKey);
            Release(keyboard.qKey);
            Assert.That(gadgetRequests, Is.EqualTo(1));
        }

        [Test]
        public void PlayerPrefab_OwnsExactlyOneGameplayInputScope()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(playerPrefab, Is.Not.Null, $"No se pudo cargar {PlayerPrefabPath}.");

            SquidInkPulseGameplayInputScope[] scopes =
                playerPrefab.GetComponentsInChildren<SquidInkPulseGameplayInputScope>(includeInactive: true);

            Assert.That(scopes, Has.Length.EqualTo(1));
            Assert.That(scopes[0].gameObject, Is.SameAs(playerPrefab));
        }

        private InputActionMap FindMap(string mapName)
        {
            return inputActions.FindActionMap(mapName, throwIfNotFound: true);
        }

        private void AssertSingleEventPerPress(ButtonControl button, Action<Action> subscribe)
        {
            int invocationCount = 0;
            subscribe(() => invocationCount++);

            Press(button);
            Assert.That(invocationCount, Is.EqualTo(1));

            InputSystem.Update();
            Assert.That(invocationCount, Is.EqualTo(1));

            Release(button);
            Assert.That(invocationCount, Is.EqualTo(1));

            Press(button);
            Assert.That(invocationCount, Is.EqualTo(2));
            Release(button);
            Assert.That(invocationCount, Is.EqualTo(2));
        }

        private void AdvanceInputTime()
        {
            currentTime += 0.01d;
        }
    }
}
