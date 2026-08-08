using System;
using System.Collections.Generic;
using System.Reflection;
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
            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
            AdvanceInputTime();

            Move(mouse.position, new Vector2(320f, 240f));
            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(new Vector2(320f, 240f)));

            Move(mouse.position, new Vector2(987f, 654f));
            Assert.That(reader.SteerPosition, Is.EqualTo(new Vector2(987f, 654f)));

            Move(mouse.position, Vector2.zero);
            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

            reader.Disable();
            Assert.That(reader.HasSteerPosition, Is.False);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SteerPosition_UsesTheMouseStatePresentBeforeEnable()
        {
            Move(mouse.position, new Vector2(640f, 360f));

            reader.Enable();

            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(new Vector2(640f, 360f)));
        }

        [Test]
        public void SteerPosition_ReenableAcceptsAnExistingMouseAtZero()
        {
            reader.Enable();
            reader.Disable();

            reader.Enable();

            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SteerPosition_DeviceRemovalInvalidatesTarget_AndHotAddRestoresZero()
        {
            reader.Enable();
            AdvanceInputTime();
            Move(mouse.position, new Vector2(640f, 360f));

            InputSystem.RemoveDevice(mouse);

            Assert.That(reader.HasSteerPosition, Is.False);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

            mouse = InputSystem.AddDevice<Mouse>();

            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SteerPosition_QueuedBeforeReenableReconcilesWithoutKeepingStaleValue()
        {
            reader.Enable();
            AdvanceInputTime();
            Move(mouse.position, new Vector2(640f, 360f));
            Move(mouse.position, Vector2.zero, queueEventOnly: true);

            reader.Disable();
            reader.Enable();
            Assert.That(reader.SteerPosition, Is.EqualTo(new Vector2(640f, 360f)));

            InputSystem.Update();

            Assert.That(reader.HasSteerPosition, Is.True);
            Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void DiscreteCommands_EmitOncePerPress()
        {
            reader.Enable();
            AdvanceInputTime();

            AssertSingleEventPerPress(keyboard.spaceKey, handler => reader.InkPulseRequested += handler);
            AssertSingleEventPerPress(keyboard.pKey, handler => reader.PauseToggleRequested += handler);
            AssertSingleEventPerPress(keyboard.escapeKey, handler => reader.PauseToggleRequested += handler);
            AssertSingleEventPerPress(keyboard.qKey, handler => reader.GadgetSlot1Requested += handler);
            AssertSingleEventPerPress(keyboard.wKey, handler => reader.GadgetSlot2Requested += handler);
            AssertSingleEventPerPress(keyboard.bKey, handler => reader.ShopPurchaseRequested += handler);
        }

        [Test]
        public void InkPulseInputBinding_CoalescesRequestsPerFrame_AndUnsubscribesIdempotently()
        {
            var binding = new InkPulseInputBinding(reader);

            try
            {
                reader.Enable();
                AdvanceInputTime();
                Press(mouse.leftButton);
                Press(keyboard.spaceKey);
                Assert.That(binding.TryConsumeActivationRequest(), Is.True);
                Assert.That(binding.TryConsumeActivationRequest(), Is.False);
                Release(mouse.leftButton);
                Release(keyboard.spaceKey);

                AdvanceInputTime();
                Press(keyboard.spaceKey);
                Assert.That(binding.TryConsumeActivationRequest(), Is.True);
                Release(keyboard.spaceKey);

                binding.Dispose();
                binding.Dispose();
                Press(keyboard.spaceKey);
                Release(keyboard.spaceKey);
                Assert.That(binding.TryConsumeActivationRequest(), Is.False);
            }
            finally
            {
                binding.Dispose();
            }
        }

        [Test]
        public void GameplayRuntime_OwnsMapsAndDropsOldStateWhenScopeToggles()
        {
            var observedReaders = new List<SquidInkPulseGameplayInputReader>();
            void HandleGameplayChanged(SquidInkPulseGameplayInputReader changedReader) =>
                observedReaders.Add(changedReader);

            InputActionAsset previousProjectWideActions = InputSystem.actions;
            InputActionAsset projectWideActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
            InputActionMap projectGameplay = projectWideActions.FindActionMap(
                SquidInkPulseInputContract.GameplayMap,
                throwIfNotFound: true);
            InputActionMap projectUi = projectWideActions.FindActionMap(
                SquidInkPulseInputContract.UiMap,
                throwIfNotFound: true);
            InputSystem.actions = projectWideActions;
            SquidInkPulseInputRuntime.GameplayChanged += HandleGameplayChanged;
            var root = new GameObject("GameplayInputScopeLifecycleTest");

            try
            {
                projectWideActions.Enable();
                Assert.That(projectGameplay.enabled, Is.True);
                Assert.That(projectUi.enabled, Is.True);

                SquidInkPulseGameplayInputScope scope =
                    root.AddComponent<SquidInkPulseGameplayInputScope>();
                InvokeScopeLifecycle(scope, "OnEnable");
                SquidInkPulseGameplayInputReader firstReader = SquidInkPulseInputRuntime.Gameplay;

                Assert.That(firstReader, Is.Not.Null);
                Assert.That(firstReader.IsEnabled, Is.True);
                Assert.That(projectGameplay.enabled, Is.True);
                Assert.That(projectUi.enabled, Is.False);

                int oldReaderRequests = 0;
                firstReader.GadgetSlot1Requested += () => oldReaderRequests++;
                AdvanceInputTime();
                Move(mouse.position, new Vector2(640f, 360f));
                Assert.That(
                    firstReader.CurrentControlScheme,
                    Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse));
                Press(keyboard.qKey, queueEventOnly: true);
                Move(mouse.position, new Vector2(900f, 600f), queueEventOnly: true);

                InvokeScopeLifecycle(scope, "OnDisable");
                Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Null);
                Assert.That(firstReader.IsEnabled, Is.False);
                Assert.That(firstReader.HasSteerPosition, Is.False);
                Assert.That(firstReader.CurrentControlScheme, Is.Empty);
                Assert.That(projectGameplay.enabled, Is.False);
                Assert.That(projectUi.enabled, Is.False);

                InputSystem.Update();
                Assert.That(oldReaderRequests, Is.Zero);

                InvokeScopeLifecycle(scope, "OnEnable");
                SquidInkPulseGameplayInputReader secondReader = SquidInkPulseInputRuntime.Gameplay;

                Assert.That(secondReader, Is.Not.Null);
                Assert.That(secondReader, Is.Not.SameAs(firstReader));
                Assert.That(secondReader.IsEnabled, Is.True);
                Assert.That(projectGameplay.enabled, Is.True);
                Assert.That(projectUi.enabled, Is.False);
                Assert.That(observedReaders, Has.Count.EqualTo(3));
                Assert.That(observedReaders[0], Is.SameAs(firstReader));
                Assert.That(observedReaders[1], Is.Null);
                Assert.That(observedReaders[2], Is.SameAs(secondReader));
            }
            finally
            {
                if (SquidInkPulseInputRuntime.Gameplay != null)
                {
                    InvokeScopeLifecycle(
                        root.GetComponent<SquidInkPulseGameplayInputScope>(),
                        "OnDisable");
                }

                UnityEngine.Object.DestroyImmediate(root);
                SquidInkPulseInputRuntime.GameplayChanged -= HandleGameplayChanged;
                projectWideActions.Disable();
                InputSystem.actions = previousProjectWideActions;
            }
        }

        [Test]
        public void CanonicalUiTouchPress_IsReceivedByUiOnly_AndLeavesGameplayUntouched()
        {
            var defaultUiActions = new DefaultInputActions();
            Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
            int uiPresses = 0;
            int uiPointEvents = 0;
            int gameplayRequests = 0;
            Vector2 lastUiPoint = Vector2.zero;
            InputDevice lastUiDevice = null;

            defaultUiActions.UI.Click.performed += context =>
            {
                if (context.ReadValueAsButton())
                {
                    uiPresses++;
                    lastUiDevice = context.control.device;
                }
            };
            defaultUiActions.UI.Point.performed += context =>
            {
                uiPointEvents++;
                lastUiPoint = context.ReadValue<Vector2>();
                lastUiDevice = context.control.device;
            };
            reader.InkPulseRequested += () => gameplayRequests++;
            reader.PauseToggleRequested += () => gameplayRequests++;
            reader.GadgetSlot1Requested += () => gameplayRequests++;
            reader.GadgetSlot2Requested += () => gameplayRequests++;
            reader.ShopPurchaseRequested += () => gameplayRequests++;

            try
            {
                reader.Enable();
                defaultUiActions.UI.Enable();
                AdvanceInputTime();
                Vector2 mousePosition = new(320f, 240f);
                Move(mouse.position, mousePosition);

                Assert.That(
                    reader.CurrentControlScheme,
                    Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse));

                AdvanceInputTime();
                Vector2 touchPosition = new(780f, 420f);
                BeginTouch(1, touchPosition, screen: touchscreen);

                Assert.That(uiPresses, Is.EqualTo(1));
                Assert.That(uiPointEvents, Is.GreaterThan(0));
                Assert.That(lastUiPoint, Is.EqualTo(touchPosition));
                Assert.That(lastUiDevice, Is.SameAs(touchscreen));
                Assert.That(gameplayRequests, Is.Zero);
                Assert.That(reader.SteerPosition, Is.EqualTo(mousePosition));
                Assert.That(
                    reader.CurrentControlScheme,
                    Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse));

                EndTouch(1, touchPosition, screen: touchscreen);
                Assert.That(uiPresses, Is.EqualTo(1));
                Assert.That(gameplayRequests, Is.Zero);
            }
            finally
            {
                defaultUiActions.UI.Disable();
                UnityEngine.Object.DestroyImmediate(defaultUiActions.asset);
            }
        }

        [Test]
        public void GameplayCommandBindings_BufferIndependently_AndDisposeIdempotently()
        {
            var pauseBinding = new GameplayCommandInputBinding(
                reader,
                SquidInkPulseGameplayCommand.TogglePause);
            var slot1Binding = new GameplayCommandInputBinding(
                reader,
                SquidInkPulseGameplayCommand.UseGadgetSlot1);
            var slot2Binding = new GameplayCommandInputBinding(
                reader,
                SquidInkPulseGameplayCommand.UseGadgetSlot2);
            var shopBinding = new GameplayCommandInputBinding(
                reader,
                SquidInkPulseGameplayCommand.BuyShopOffer);

            try
            {
                reader.Enable();
                AdvanceInputTime();
                Press(keyboard.pKey);
                Press(keyboard.escapeKey);
                Press(keyboard.qKey);
                Press(keyboard.wKey);
                Press(keyboard.bKey);

                Assert.That(pauseBinding.TryConsumeRequest(), Is.True);
                Assert.That(pauseBinding.TryConsumeRequest(), Is.False);
                Assert.That(slot1Binding.TryConsumeRequest(), Is.True);
                Assert.That(slot1Binding.TryConsumeRequest(), Is.False);
                Assert.That(slot2Binding.TryConsumeRequest(), Is.True);
                Assert.That(slot2Binding.TryConsumeRequest(), Is.False);
                Assert.That(shopBinding.TryConsumeRequest(), Is.True);
                Assert.That(shopBinding.TryConsumeRequest(), Is.False);

                Release(keyboard.pKey);
                Release(keyboard.escapeKey);
                Release(keyboard.qKey);
                Release(keyboard.wKey);
                Release(keyboard.bKey);

                pauseBinding.Dispose();
                slot1Binding.Dispose();
                slot2Binding.Dispose();
                shopBinding.Dispose();
                pauseBinding.Dispose();
                slot1Binding.Dispose();
                slot2Binding.Dispose();
                shopBinding.Dispose();

                Press(keyboard.pKey);
                Press(keyboard.qKey);
                Press(keyboard.wKey);
                Press(keyboard.bKey);
                Assert.That(pauseBinding.TryConsumeRequest(), Is.False);
                Assert.That(slot1Binding.TryConsumeRequest(), Is.False);
                Assert.That(slot2Binding.TryConsumeRequest(), Is.False);
                Assert.That(shopBinding.TryConsumeRequest(), Is.False);
            }
            finally
            {
                pauseBinding.Dispose();
                slot1Binding.Dispose();
                slot2Binding.Dispose();
                shopBinding.Dispose();
            }
        }

        [Test]
        public void ControlScheme_ChangesOnlyWhenPerformedGameplayDeviceSchemeChanges()
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
            Move(mouse.position, new Vector2(300f, 400f));

            Assert.That(observedSchemes, Is.EqualTo(new[]
            {
                SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse,
                SquidInkPulseInputContract.ControlSchemes.Gamepad,
                SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse
            }));
            Assert.That(
                reader.CurrentControlScheme,
                Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse));
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

        private static void InvokeScopeLifecycle(
            SquidInkPulseGameplayInputScope scope,
            string methodName)
        {
            MethodInfo lifecycleMethod = typeof(SquidInkPulseGameplayInputScope).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lifecycleMethod, Is.Not.Null);
            lifecycleMethod.Invoke(scope, null);
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
