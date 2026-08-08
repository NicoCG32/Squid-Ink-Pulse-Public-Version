using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InputActionContractTests
    {
        private const string InputActionsAssetPath =
            "Assets/Implementation/Config/Input/InputSystem_Actions.inputactions";

        private InputActionAsset inputActions;

        [SetUp]
        public void SetUp()
        {
            inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
            Assert.That(inputActions, Is.Not.Null, $"No se pudo cargar {InputActionsAssetPath}.");
        }

        [Test]
        public void GameplayMap_ExposesOnlySquidInkPulseCommands()
        {
            InputActionMap gameplay = inputActions.FindActionMap(
                SquidInkPulseInputContract.GameplayMap,
                throwIfNotFound: true);

            Assert.That(
                gameplay.actions.Select(action => action.name),
                Is.EquivalentTo(new[]
                {
                    SquidInkPulseInputContract.Gameplay.SteerPosition,
                    SquidInkPulseInputContract.Gameplay.ActivateInkPulse,
                    SquidInkPulseInputContract.Gameplay.TogglePause,
                    SquidInkPulseInputContract.Gameplay.UseGadgetSlot1,
                    SquidInkPulseInputContract.Gameplay.UseGadgetSlot2
                }));
            Assert.That(inputActions.FindActionMap("Player", throwIfNotFound: false), Is.Null);
            Assert.That(gameplay.FindAction("Attack", throwIfNotFound: false), Is.Null);
        }

        [Test]
        public void GameplayMap_PreservesDesktopBindings()
        {
            InputActionMap gameplay = inputActions.FindActionMap(
                SquidInkPulseInputContract.GameplayMap,
                throwIfNotFound: true);

            AssertAction(
                gameplay,
                SquidInkPulseInputContract.Gameplay.SteerPosition,
                InputActionType.Value,
                "Vector2",
                "<Mouse>/position");
            AssertAction(
                gameplay,
                SquidInkPulseInputContract.Gameplay.ActivateInkPulse,
                InputActionType.Button,
                "Button",
                "<Mouse>/leftButton",
                "<Keyboard>/space");
            AssertAction(
                gameplay,
                SquidInkPulseInputContract.Gameplay.TogglePause,
                InputActionType.Button,
                "Button",
                "<Keyboard>/p",
                "<Keyboard>/escape");
            AssertAction(
                gameplay,
                SquidInkPulseInputContract.Gameplay.UseGadgetSlot1,
                InputActionType.Button,
                "Button",
                "<Keyboard>/q");
            AssertAction(
                gameplay,
                SquidInkPulseInputContract.Gameplay.UseGadgetSlot2,
                InputActionType.Button,
                "Button",
                "<Keyboard>/w");

            Assert.That(
                gameplay.bindings.Select(binding => binding.groups),
                Has.All.EqualTo(SquidInkPulseInputContract.ControlSchemes.KeyboardAndMouse));
        }

        [Test]
        public void TouchscreenBindings_AreReservedForUiPointAndClick()
        {
            InputActionMap gameplay = inputActions.FindActionMap(
                SquidInkPulseInputContract.GameplayMap,
                throwIfNotFound: true);
            InputActionMap ui = inputActions.FindActionMap(
                SquidInkPulseInputContract.UiMap,
                throwIfNotFound: true);

            Assert.That(
                gameplay.bindings,
                Has.None.Matches<InputBinding>(binding => IsTouchscreenPath(binding.path)));

            var touchBindings = ui.bindings
                .Where(binding => IsTouchscreenPath(binding.path))
                .ToArray();

            Assert.That(touchBindings.Select(binding => binding.action), Is.EquivalentTo(new[]
            {
                SquidInkPulseInputContract.Ui.Point,
                SquidInkPulseInputContract.Ui.Click
            }));
            Assert.That(touchBindings.Select(binding => binding.path), Is.EquivalentTo(new[]
            {
                "<Touchscreen>/touch*/position",
                "<Touchscreen>/touch*/press"
            }));
            Assert.That(
                inputActions.controlSchemes.Select(scheme => scheme.name),
                Does.Contain(SquidInkPulseInputContract.ControlSchemes.Touch));
        }

        [Test]
        public void UiMap_RemainsCompatibleWithInputSystemUiInputModule()
        {
            InputActionMap ui = inputActions.FindActionMap(
                SquidInkPulseInputContract.UiMap,
                throwIfNotFound: true);

            Assert.That(ui.actions.Select(action => action.name), Is.EquivalentTo(new[]
            {
                SquidInkPulseInputContract.Ui.Navigate,
                SquidInkPulseInputContract.Ui.Submit,
                SquidInkPulseInputContract.Ui.Cancel,
                SquidInkPulseInputContract.Ui.Point,
                SquidInkPulseInputContract.Ui.Click,
                SquidInkPulseInputContract.Ui.RightClick,
                SquidInkPulseInputContract.Ui.MiddleClick,
                SquidInkPulseInputContract.Ui.ScrollWheel,
                SquidInkPulseInputContract.Ui.TrackedDevicePosition,
                SquidInkPulseInputContract.Ui.TrackedDeviceOrientation
            }));

            AssertActionType(ui, SquidInkPulseInputContract.Ui.Navigate, InputActionType.PassThrough, "Vector2");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.Submit, InputActionType.Button, "Button");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.Cancel, InputActionType.Button, "Button");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.Point, InputActionType.PassThrough, "Vector2");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.Click, InputActionType.PassThrough, "Button");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.RightClick, InputActionType.PassThrough, "Button");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.MiddleClick, InputActionType.PassThrough, "Button");
            AssertActionType(ui, SquidInkPulseInputContract.Ui.ScrollWheel, InputActionType.PassThrough, "Vector2");
            AssertActionType(
                ui,
                SquidInkPulseInputContract.Ui.TrackedDevicePosition,
                InputActionType.PassThrough,
                "Vector3");
            AssertActionType(
                ui,
                SquidInkPulseInputContract.Ui.TrackedDeviceOrientation,
                InputActionType.PassThrough,
                "Quaternion");
        }

        [Test]
        public void MapsActionsAndBindings_HaveUniqueIds()
        {
            var mapIds = inputActions.actionMaps.Select(map => map.id).ToArray();
            var actionIds = inputActions.actionMaps
                .SelectMany(map => map.actions)
                .Select(action => action.id)
                .ToArray();
            var bindingIds = inputActions.actionMaps
                .SelectMany(map => map.bindings)
                .Select(binding => binding.id)
                .ToArray();

            Assert.That(mapIds.Distinct().Count(), Is.EqualTo(mapIds.Length));
            Assert.That(actionIds.Distinct().Count(), Is.EqualTo(actionIds.Length));
            Assert.That(bindingIds.Distinct().Count(), Is.EqualTo(bindingIds.Length));
        }

        private static void AssertAction(
            InputActionMap map,
            string actionName,
            InputActionType type,
            string expectedControlType,
            params string[] expectedPaths)
        {
            InputAction action = map.FindAction(actionName, throwIfNotFound: true);

            Assert.That(action.type, Is.EqualTo(type), actionName);
            Assert.That(action.expectedControlType, Is.EqualTo(expectedControlType), actionName);
            if (type == InputActionType.Button)
            {
                Assert.That(action.wantsInitialStateCheck, Is.False, actionName);
            }
            else if (type == InputActionType.Value)
            {
                Assert.That(action.wantsInitialStateCheck, Is.True, actionName);
            }
            Assert.That(
                action.bindings.Select(binding => binding.path),
                Is.EquivalentTo(expectedPaths),
                actionName);
        }

        private static void AssertActionType(
            InputActionMap map,
            string actionName,
            InputActionType type,
            string expectedControlType)
        {
            InputAction action = map.FindAction(actionName, throwIfNotFound: true);

            Assert.That(action.type, Is.EqualTo(type), actionName);
            Assert.That(action.expectedControlType, Is.EqualTo(expectedControlType), actionName);
        }

        private static bool IsTouchscreenPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith("<Touchscreen>");
        }
    }
}
