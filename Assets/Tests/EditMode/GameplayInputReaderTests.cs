using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

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
        public void TouchSteering_OwnsOnePointer_PrioritizesTouch_AndFallsBackToMouse()
        {
            var firstOwner = new GameObject("FirstTouchSteeringOwner");
            var secondOwner = new GameObject("SecondTouchSteeringOwner");
            var observedSchemes = new List<string>();
            reader.ControlSchemeChanged += observedSchemes.Add;

            try
            {
                reader.Enable();
                AdvanceInputTime();
                Vector2 initialMousePosition = new(120f, 240f);
                Move(mouse.position, initialMousePosition);

                Vector2 firstTouchPosition = new(800f, 500f);
                Assert.That(
                    reader.TryBeginTouchSteering(firstOwner, 101, firstTouchPosition),
                    Is.True);
                Assert.That(reader.HasSteerPosition, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstTouchPosition));
                Assert.That(
                    reader.CurrentControlScheme,
                    Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.Touch));

                Vector2 competingPosition = new(20f, 30f);
                Assert.That(
                    reader.TryBeginTouchSteering(secondOwner, 202, competingPosition),
                    Is.False);
                Assert.That(
                    reader.TryUpdateTouchSteering(secondOwner, 202, competingPosition),
                    Is.False);
                Assert.That(reader.TryEndTouchSteering(secondOwner, 202), Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstTouchPosition));

                Vector2 latestMousePosition = new(360f, 640f);
                Move(mouse.position, latestMousePosition);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstTouchPosition));
                Assert.That(
                    reader.CurrentControlScheme,
                    Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.Touch));

                Vector2 latestTouchPosition = new(900f, 700f);
                Assert.That(
                    reader.TryUpdateTouchSteering(firstOwner, 101, latestTouchPosition),
                    Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(latestTouchPosition));
                Assert.That(reader.TryEndTouchSteering(firstOwner, 101), Is.True);
                Assert.That(reader.HasSteerPosition, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(latestMousePosition));
                Assert.That(observedSchemes, Is.EqualTo(new[]
                {
                    SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse,
                    SquidInkPulseInputContract.ControlSchemes.Touch
                }));

                Move(mouse.position, new Vector2(400f, 700f));
                Assert.That(observedSchemes, Is.EqualTo(new[]
                {
                    SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse,
                    SquidInkPulseInputContract.ControlSchemes.Touch,
                    SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse
                }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstOwner);
                UnityEngine.Object.DestroyImmediate(secondOwner);
            }
        }

        [Test]
        public void TouchSteering_ZeroIsValid_AndClearDoesNotRetainItWithoutFallback()
        {
            InputSystem.RemoveDevice(mouse);
            var owner = new GameObject("ZeroTouchSteeringOwner");

            try
            {
                reader.Enable();
                Assert.That(reader.HasSteerPosition, Is.False);

                Assert.That(reader.TryBeginTouchSteering(owner, -1, Vector2.zero), Is.True);
                Assert.That(reader.HasSteerPosition, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                Assert.That(reader.CancelTouchSteering(owner), Is.True);
                Assert.That(reader.HasSteerPosition, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
                Assert.That(reader.CancelTouchSteering(owner), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void TouchSteering_DisableDropsOwnership_AndLateCleanupIsHarmless()
        {
            var owner = new GameObject("LifecycleTouchSteeringOwner");

            try
            {
                reader.Enable();
                Assert.That(
                    reader.TryBeginTouchSteering(owner, 17, new Vector2(500f, 600f)),
                    Is.True);

                reader.Disable();

                Assert.That(reader.HasSteerPosition, Is.False);
                Assert.That(reader.TryEndTouchSteering(owner, 17), Is.False);
                Assert.That(reader.CancelTouchSteering(owner), Is.False);
                Assert.That(
                    reader.TryUpdateTouchSteering(owner, 17, new Vector2(700f, 800f)),
                    Is.False);

                reader.Enable();
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
                Assert.That(
                    reader.TryBeginTouchSteering(owner, 17, new Vector2(700f, 800f)),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void TouchSteeringSurface_CancelsOnGameplayGates_AndNeverAdoptsAStaleDrag()
        {
            var sessionRoot = new GameObject("TouchSteeringSession");
            var surfaceRoot = new GameObject("TouchSteeringSurface", typeof(RectTransform));
            var eventSystemRoot = new GameObject("TouchSteeringEventSystem");
            GameSessionController session = sessionRoot.AddComponent<GameSessionController>();
            TouchSteeringSurface surface = surfaceRoot.AddComponent<TouchSteeringSurface>();
            EventSystem eventSystem = eventSystemRoot.AddComponent<EventSystem>();
            FieldInfo blockedUntilFrameField = typeof(InGameShopManager).GetField(
                "inkPulseActivationBlockedUntilFrame",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(blockedUntilFrameField, Is.Not.Null);
            int previousBlockedUntilFrame = (int)blockedUntilFrameField.GetValue(null);
            SquidInkPulseGameplayInputReader replacementReader = null;

            try
            {
                blockedUntilFrameField.SetValue(null, -1);
                InvokePrivate(session, "Awake");
                Assert.That(GameSessionController.Instance, Is.SameAs(session));

                reader.Enable();
                InvokePrivate(surface, "HandleGameplayInputChanged", reader);
                InvokePrivate(surface, "BindSession", session);

                var buttonRoot = new GameObject(
                    "InteractiveTouchButton",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Button));
                buttonRoot.transform.SetParent(surfaceRoot.transform, false);
                PointerEventData blockedDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 5,
                    position: new Vector2(300f, 200f),
                    raycastTarget: buttonRoot);
                surface.OnPointerDown(blockedDown);
                blockedDown.position = new Vector2(500f, 300f);
                surface.OnDrag(blockedDown);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                PointerEventData nonTouchDown = new PointerEventData(eventSystem)
                {
                    pointerId = -1,
                    position = new Vector2(600f, 350f),
                    button = PointerEventData.InputButton.Left
                };
                surface.OnPointerDown(nonTouchDown);
                Assert.That(surface.HasActivePointer, Is.False);

                PointerEventData firstDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 11,
                    position: new Vector2(700f, 400f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(firstDown);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstDown.position));

                PointerEventData secondPointer = CreatePointerEvent(
                    eventSystem,
                    pointerId: 22,
                    position: new Vector2(1300f, 800f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(secondPointer);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstDown.position));
                secondPointer.position = new Vector2(1600f, 900f);
                surface.OnDrag(secondPointer);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstDown.position));
                surface.OnPointerUp(secondPointer);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstDown.position));

                firstDown.position = new Vector2(850f, 520f);
                surface.OnDrag(firstDown);
                Assert.That(reader.SteerPosition, Is.EqualTo(firstDown.position));

                Time.timeScale = 0f;
                firstDown.position = new Vector2(880f, 560f);
                surface.OnDrag(firstDown);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
                Time.timeScale = 1f;
                surface.OnDrag(firstDown);
                Assert.That(surface.HasActivePointer, Is.False);

                PointerEventData pauseDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 30,
                    position: new Vector2(950f, 620f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(pauseDown);
                Assert.That(surface.HasActivePointer, Is.True);
                session.RequestPause();
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
                PointerEventData pausedDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 31,
                    position: new Vector2(970f, 630f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(pausedDown);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                session.RequestResume();
                pauseDown.position = new Vector2(980f, 640f);
                surface.OnDrag(pauseDown);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                PointerEventData freshDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 33,
                    position: new Vector2(1000f, 650f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(freshDown);
                Assert.That(surface.HasActivePointer, Is.True);

                InvokePrivate(
                    surface,
                    "HandleShopStateChanged",
                    ShopEventState.Closed,
                    ShopEventState.Offering);
                Assert.That(surface.HasActivePointer, Is.False);
                blockedUntilFrameField.SetValue(null, int.MaxValue);
                PointerEventData shopBlockedDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 34,
                    position: new Vector2(1050f, 680f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(shopBlockedDown);
                Assert.That(surface.HasActivePointer, Is.False);
                freshDown.position = new Vector2(1100f, 700f);
                surface.OnDrag(freshDown);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));
                blockedUntilFrameField.SetValue(null, -1);

                PointerEventData overlayDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 44,
                    position: new Vector2(1200f, 720f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(overlayDown);
                Assert.That(surface.HasActivePointer, Is.True);
                surface.SetOverlayInteractionAllowed(false);
                Assert.That(surface.HasActivePointer, Is.False);
                PointerEventData overlayBlockedDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 45,
                    position: new Vector2(1230f, 730f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(overlayBlockedDown);
                Assert.That(surface.HasActivePointer, Is.False);
                overlayDown.position = new Vector2(1250f, 740f);
                surface.OnDrag(overlayDown);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                surface.SetOverlayInteractionAllowed(true);
                PointerEventData focusDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 55,
                    position: new Vector2(1400f, 780f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(focusDown);
                Assert.That(surface.HasActivePointer, Is.True);
                InvokePrivate(surface, "OnApplicationFocus", false);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                PointerEventData appPauseDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 66,
                    position: new Vector2(1450f, 800f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(appPauseDown);
                Assert.That(surface.HasActivePointer, Is.True);
                InvokePrivate(surface, "OnApplicationPause", true);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(reader.SteerPosition, Is.EqualTo(Vector2.zero));

                PointerEventData rebindDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 77,
                    position: new Vector2(1500f, 820f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(rebindDown);
                Assert.That(surface.HasActivePointer, Is.True);

                reader.Dispose();
                InvokePrivate(surface, "HandleGameplayInputChanged", (object)null);
                Assert.That(surface.HasActivePointer, Is.False);

                replacementReader = new SquidInkPulseGameplayInputReader(inputActions);
                replacementReader.Enable();
                InvokePrivate(surface, "HandleGameplayInputChanged", replacementReader);
                rebindDown.position = new Vector2(1550f, 840f);
                surface.OnDrag(rebindDown);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(replacementReader.SteerPosition, Is.EqualTo(Vector2.zero));

                PointerEventData lifecycleDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 88,
                    position: new Vector2(1600f, 860f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(lifecycleDown);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(replacementReader.SteerPosition, Is.EqualTo(lifecycleDown.position));

                InvokePrivate(surface, "OnDisable");
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(replacementReader.SteerPosition, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                blockedUntilFrameField.SetValue(null, previousBlockedUntilFrame);
                InvokePrivate(surface, "OnDisable");
                replacementReader?.Dispose();
                UnityEngine.Object.DestroyImmediate(surfaceRoot);
                UnityEngine.Object.DestroyImmediate(eventSystemRoot);
                UnityEngine.Object.DestroyImmediate(sessionRoot);
                Time.timeScale = 1f;
            }
        }

        [Test]
        public void TouchSteeringSurface_RebindsThroughTheRealGameplayScopeLifecycle()
        {
            InputActionAsset previousProjectWideActions = InputSystem.actions;
            InputActionAsset projectWideActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
            var sessionRoot = new GameObject("TouchSteeringScopeSession");
            var scopeRoot = new GameObject("TouchSteeringScopeOwner");
            var surfaceRoot = new GameObject("TouchSteeringScopeSurface", typeof(RectTransform));
            var eventSystemRoot = new GameObject("TouchSteeringScopeEventSystem");
            GameSessionController session = sessionRoot.AddComponent<GameSessionController>();
            SquidInkPulseGameplayInputScope scope =
                scopeRoot.AddComponent<SquidInkPulseGameplayInputScope>();
            TouchSteeringSurface surface = surfaceRoot.AddComponent<TouchSteeringSurface>();
            EventSystem eventSystem = eventSystemRoot.AddComponent<EventSystem>();
            FieldInfo blockedUntilFrameField = typeof(InGameShopManager).GetField(
                "inkPulseActivationBlockedUntilFrame",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(blockedUntilFrameField, Is.Not.Null);
            int previousBlockedUntilFrame = (int)blockedUntilFrameField.GetValue(null);

            try
            {
                blockedUntilFrameField.SetValue(null, -1);
                InputSystem.actions = projectWideActions;
                InvokePrivate(session, "Awake");
                InvokePrivate(surface, "OnDisable");
                InvokePrivate(surface, "OnEnable");

                InvokeScopeLifecycle(scope, "OnEnable");
                SquidInkPulseGameplayInputReader firstReader = SquidInkPulseInputRuntime.Gameplay;
                Assert.That(firstReader, Is.Not.Null);

                PointerEventData firstDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 101,
                    position: new Vector2(800f, 500f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(firstDown);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(firstReader.SteerPosition, Is.EqualTo(firstDown.position));

                InvokeScopeLifecycle(scope, "OnDisable");
                Assert.That(firstReader.IsEnabled, Is.False);
                Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Null);
                Assert.That(surface.HasActivePointer, Is.False);

                InvokeScopeLifecycle(scope, "OnEnable");
                SquidInkPulseGameplayInputReader secondReader = SquidInkPulseInputRuntime.Gameplay;
                Assert.That(secondReader, Is.Not.Null);
                Assert.That(secondReader, Is.Not.SameAs(firstReader));
                Assert.That(secondReader.SteerPosition, Is.EqualTo(Vector2.zero));

                firstDown.position = new Vector2(900f, 600f);
                surface.OnDrag(firstDown);
                Assert.That(surface.HasActivePointer, Is.False);
                Assert.That(secondReader.SteerPosition, Is.EqualTo(Vector2.zero));

                PointerEventData freshDown = CreatePointerEvent(
                    eventSystem,
                    pointerId: 202,
                    position: new Vector2(1000f, 650f),
                    raycastTarget: surfaceRoot);
                surface.OnPointerDown(freshDown);
                Assert.That(surface.HasActivePointer, Is.True);
                Assert.That(secondReader.SteerPosition, Is.EqualTo(freshDown.position));
            }
            finally
            {
                InvokePrivate(surface, "OnDisable");
                if (SquidInkPulseInputRuntime.Gameplay != null)
                {
                    InvokeScopeLifecycle(scope, "OnDisable");
                }

                projectWideActions.Disable();
                InputSystem.actions = previousProjectWideActions;
                blockedUntilFrameField.SetValue(null, previousBlockedUntilFrame);
                UnityEngine.Object.DestroyImmediate(surfaceRoot);
                UnityEngine.Object.DestroyImmediate(eventSystemRoot);
                UnityEngine.Object.DestroyImmediate(scopeRoot);
                UnityEngine.Object.DestroyImmediate(sessionRoot);
                Time.timeScale = 1f;
            }
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

        [Test]
        public void TouchCommands_EmitOnlyTheirSemanticRequest_AndMarkTouchScheme()
        {
            int inkPulseRequests = 0;
            int pauseRequests = 0;
            int slot1Requests = 0;
            int slot2Requests = 0;
            reader.InkPulseRequested += () => inkPulseRequests++;
            reader.PauseToggleRequested += () => pauseRequests++;
            reader.GadgetSlot1Requested += () => slot1Requests++;
            reader.GadgetSlot2Requested += () => slot2Requests++;

            Assert.That(
                reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.ActivateInkPulse),
                Is.False);
            reader.Enable();

            Assert.That(
                reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.ActivateInkPulse),
                Is.True);
            Assert.That((inkPulseRequests, pauseRequests, slot1Requests, slot2Requests),
                Is.EqualTo((1, 0, 0, 0)));

            reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.TogglePause);
            Assert.That((inkPulseRequests, pauseRequests, slot1Requests, slot2Requests),
                Is.EqualTo((1, 1, 0, 0)));

            reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.UseGadgetSlot1);
            Assert.That((inkPulseRequests, pauseRequests, slot1Requests, slot2Requests),
                Is.EqualTo((1, 1, 1, 0)));

            reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.UseGadgetSlot2);
            Assert.That((inkPulseRequests, pauseRequests, slot1Requests, slot2Requests),
                Is.EqualTo((1, 1, 1, 1)));
            Assert.That(
                reader.CurrentControlScheme,
                Is.EqualTo(SquidInkPulseInputContract.ControlSchemes.Touch));

            reader.Disable();
            Assert.That(
                reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.TogglePause),
                Is.False);
            Assert.That((inkPulseRequests, pauseRequests, slot1Requests, slot2Requests),
                Is.EqualTo((1, 1, 1, 1)));
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

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"No se encontro {target.GetType().Name}.{methodName}.");
            method.Invoke(target, arguments);
        }

        private static PointerEventData CreatePointerEvent(
            EventSystem eventSystem,
            int pointerId,
            Vector2 position,
            GameObject raycastTarget = null)
        {
            var eventData = new ExtendedPointerEventData(eventSystem)
            {
                pointerId = pointerId,
                position = position,
                button = PointerEventData.InputButton.Left,
                pointerType = UIPointerType.Touch
            };

            if (raycastTarget != null)
            {
                eventData.pointerPressRaycast = new RaycastResult
                {
                    gameObject = raycastTarget
                };
            }

            return eventData;
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
