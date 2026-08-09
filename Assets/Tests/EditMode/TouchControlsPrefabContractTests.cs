using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class TouchControlsPrefabContractTests : InputTestFixture
    {
        private const string PrefabPath =
            "Assets/Content/Prefabs/UI/Touch/TouchControls.prefab";
        private const string InputActionsPath =
            "Assets/Implementation/Config/Input/InputSystem_Actions.inputactions";

        private InputActionAsset previousProjectWideActions;
        private InputActionAsset inputActions;
        private GameObject scopeRoot;
        private SquidInkPulseGameplayInputScope scope;
        private GameObject eventSystemRoot;
        private EventSystem eventSystem;

        public override void Setup()
        {
            base.Setup();
            InputActionAsset source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(source, Is.Not.Null);
            inputActions = source;
            previousProjectWideActions = InputSystem.actions;
            InputSystem.actions = inputActions;

            scopeRoot = new GameObject("TouchControlsScope");
            scopeRoot.SetActive(false);
            scope = scopeRoot.AddComponent<SquidInkPulseGameplayInputScope>();
            InvokeScope(scope, "OnEnable");

            eventSystemRoot = new GameObject("TouchControlsEventSystem");
            eventSystem = eventSystemRoot.AddComponent<EventSystem>();
        }

        public override void TearDown()
        {
            if (scope != null)
            {
                InvokeScope(scope, "OnDisable");
            }

            InputSystem.actions = previousProjectWideActions;
            if (eventSystemRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(eventSystemRoot);
            }

            if (scopeRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(scopeRoot);
            }

            if (inputActions != null)
            {
                inputActions.Disable();
            }

            base.TearDown();
        }

        [Test]
        public void Prefab_HasOneSurfaceFourAccessibleCommandsAndNoCompetingUiInfrastructure()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"No se pudo cargar {PrefabPath}.");

            Assert.That(prefab.GetComponentsInChildren<TouchControlsVisibilityController>(true), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<TouchGameplayControlsController>(true), Has.Length.EqualTo(1));
            TouchSteeringSurface[] surfaces = prefab.GetComponentsInChildren<TouchSteeringSurface>(true);
            Assert.That(surfaces, Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<TouchGameplayCommandButton>(true), Has.Length.EqualTo(4));
            Assert.That(prefab.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(4));

            Assert.That(prefab.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(CountMissingScripts(prefab), Is.Zero);

            Transform controlsRoot = prefab.transform.Find("TouchControls");
            Assert.That(controlsRoot, Is.Not.Null);
            Assert.That(surfaces[0].transform.parent, Is.SameAs(controlsRoot));
            Assert.That(surfaces[0].transform.GetSiblingIndex(), Is.EqualTo(0));
            Transform controlsLayer = controlsRoot.Find("ControlsLayer");
            Assert.That(controlsLayer, Is.Not.Null);
            Assert.That(controlsLayer.GetSiblingIndex(), Is.GreaterThan(surfaces[0].transform.GetSiblingIndex()));

            var commands = new HashSet<SquidInkPulseGameplayCommand>();
            foreach (TouchGameplayCommandButton commandButton in
                     prefab.GetComponentsInChildren<TouchGameplayCommandButton>(true))
            {
                Assert.That(commands.Add(commandButton.Command), Is.True, "Cada comando debe pertenecer a un boton distinto.");
                Button button = commandButton.GetComponent<Button>();
                Assert.That(button, Is.Not.Null);
                Assert.That(button.onClick.GetPersistentEventCount(), Is.Zero);
                Assert.That(button.targetGraphic, Is.SameAs(commandButton.GetComponent<Image>()));
                Assert.That(button.targetGraphic.raycastTarget, Is.True);
                Assert.That(UiButtonContract.IsCompliantButton(button), Is.True);

                RectTransform hitRoot = button.transform.parent as RectTransform;
                Assert.That(hitRoot, Is.Not.Null);
                Assert.That(hitRoot.sizeDelta.x, Is.GreaterThanOrEqualTo(120f));
                Assert.That(hitRoot.sizeDelta.y, Is.GreaterThanOrEqualTo(120f));

                TMP_Text action = hitRoot.Find("Accion")?.GetComponent<TMP_Text>();
                TMP_Text status = hitRoot.Find("Estado")?.GetComponent<TMP_Text>();
                Assert.That(action, Is.Not.Null);
                Assert.That(status, Is.Not.Null);
                Assert.That(action.text, Is.Not.Empty);
                Assert.That(status.text, Is.Not.Empty);
                Assert.That(action.raycastTarget, Is.False);
                Assert.That(status.raycastTarget, Is.False);
            }

            Assert.That(commands, Is.EquivalentTo(new[]
            {
                SquidInkPulseGameplayCommand.ActivateInkPulse,
                SquidInkPulseGameplayCommand.TogglePause,
                SquidInkPulseGameplayCommand.UseGadgetSlot1,
                SquidInkPulseGameplayCommand.UseGadgetSlot2
            }));

            Graphic[] raycastTargets = Array.FindAll(
                prefab.GetComponentsInChildren<Graphic>(true),
                graphic => graphic.raycastTarget);
            Assert.That(raycastTargets, Has.Length.EqualTo(5));
            Assert.That(raycastTargets, Does.Contain(surfaces[0].GetComponent<Image>()));
        }

        [Test]
        public void CommandButtons_OwnPointersIndependently_AndNeverAffectSteering()
        {
            GameObject instance = InstantiateVisiblePrefab();
            SquidInkPulseGameplayInputReader reader = SquidInkPulseInputRuntime.Gameplay;
            var steeringOwner = new GameObject("SteeringOwner");
            Vector2 steeringPosition = new(640f, 420f);
            Assert.That(reader.TryBeginTouchSteering(steeringOwner, 1, steeringPosition), Is.True);

            int ink = 0;
            int pause = 0;
            int slot1 = 0;
            int slot2 = 0;
            reader.InkPulseRequested += () => ink++;
            reader.PauseToggleRequested += () => pause++;
            reader.GadgetSlot1Requested += () => slot1++;
            reader.GadgetSlot2Requested += () => slot2++;

            TouchGameplayCommandButton[] buttons =
                instance.GetComponentsInChildren<TouchGameplayCommandButton>(true);
            try
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].Button.interactable = true;
                    buttons[i].OnPointerDown(CreatePointer(10 + i, buttons[i].gameObject));
                    Assert.That(reader.SteerPosition, Is.EqualTo(steeringPosition));
                }

                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].OnPointerUp(CreatePointer(10 + i, buttons[i].gameObject));
                    Assert.That(reader.SteerPosition, Is.EqualTo(steeringPosition));
                }

                Assert.That((ink, pause, slot1, slot2), Is.EqualTo((1, 1, 1, 1)));
                Assert.That(reader.TryEndTouchSteering(steeringOwner, 1), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(steeringOwner);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CommandButton_SecondPointerCannotSteal_AndScopeChangeCancelsOldPress()
        {
            GameObject instance = InstantiateVisiblePrefab();
            TouchGameplayCommandButton button = Array.Find(
                instance.GetComponentsInChildren<TouchGameplayCommandButton>(true),
                item => item.Command == SquidInkPulseGameplayCommand.TogglePause);
            button.Button.interactable = true;
            SquidInkPulseGameplayInputReader firstReader = SquidInkPulseInputRuntime.Gameplay;
            int firstCount = 0;
            firstReader.PauseToggleRequested += () => firstCount++;

            try
            {
                button.OnPointerDown(CreatePointer(21, button.gameObject));
                button.OnPointerDown(CreatePointer(22, button.gameObject));
                button.OnPointerUp(CreatePointer(22, button.gameObject));
                Assert.That(firstCount, Is.Zero);
                Assert.That(button.HasPointerOwner, Is.True);
                button.OnPointerUp(CreatePointer(21, button.gameObject));
                Assert.That(firstCount, Is.EqualTo(1));

                button.OnPointerDown(CreatePointer(31, button.gameObject));
                InvokeScope(scope, "OnDisable");
                InvokeScope(scope, "OnEnable");
                SquidInkPulseGameplayInputReader secondReader = SquidInkPulseInputRuntime.Gameplay;
                Assert.That(secondReader, Is.Not.SameAs(firstReader));
                int secondCount = 0;
                secondReader.PauseToggleRequested += () => secondCount++;

                button.Button.interactable = true;
                button.OnPointerUp(CreatePointer(31, button.gameObject));
                Assert.That(firstCount, Is.EqualTo(1));
                Assert.That(secondCount, Is.Zero);

                button.OnPointerDown(CreatePointer(32, button.gameObject));
                button.OnPointerUp(CreatePointer(32, button.gameObject));
                Assert.That(secondCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ReplacingZoneControls_LeavesOldReaderFrozen_AndDoesNotDuplicateNewCommands()
        {
            var firstCounts = CreateCommandCounter(SquidInkPulseInputRuntime.Gameplay);
            GameObject firstControls = InstantiateVisiblePrefab();
            ClickAllCommands(firstControls, 100);
            AssertAllCommandCounts(firstCounts, 1);

            UnityEngine.Object.DestroyImmediate(firstControls);
            InvokeScope(scope, "OnDisable");
            InvokeScope(scope, "OnEnable");

            var secondCounts = CreateCommandCounter(SquidInkPulseInputRuntime.Gameplay);
            GameObject secondControls = InstantiateVisiblePrefab();
            try
            {
                ClickAllCommands(secondControls, 200);
                AssertAllCommandCounts(firstCounts, 1);
                AssertAllCommandCounts(secondCounts, 1);

                Transform controlsRoot = secondControls.transform.Find("TouchControls");
                controlsRoot.gameObject.SetActive(false);
                controlsRoot.gameObject.SetActive(true);
                ClickAllCommands(secondControls, 300);

                AssertAllCommandCounts(firstCounts, 1);
                AssertAllCommandCounts(secondCounts, 2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondControls);
            }
        }

        private GameObject InstantiateVisiblePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            Transform controlsRoot = instance.transform.Find("TouchControls");
            Assert.That(controlsRoot, Is.Not.Null);
            controlsRoot.gameObject.SetActive(true);
            return instance;
        }

        private PointerEventData CreatePointer(int pointerId, GameObject target)
        {
            return new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                button = PointerEventData.InputButton.Left,
                position = new Vector2(500f, 500f),
                pointerCurrentRaycast = new RaycastResult { gameObject = target }
            };
        }

        private Dictionary<SquidInkPulseGameplayCommand, int> CreateCommandCounter(
            SquidInkPulseGameplayInputReader targetReader)
        {
            var counts = new Dictionary<SquidInkPulseGameplayCommand, int>
            {
                [SquidInkPulseGameplayCommand.ActivateInkPulse] = 0,
                [SquidInkPulseGameplayCommand.TogglePause] = 0,
                [SquidInkPulseGameplayCommand.UseGadgetSlot1] = 0,
                [SquidInkPulseGameplayCommand.UseGadgetSlot2] = 0
            };
            targetReader.InkPulseRequested += () => counts[SquidInkPulseGameplayCommand.ActivateInkPulse]++;
            targetReader.PauseToggleRequested += () => counts[SquidInkPulseGameplayCommand.TogglePause]++;
            targetReader.GadgetSlot1Requested += () => counts[SquidInkPulseGameplayCommand.UseGadgetSlot1]++;
            targetReader.GadgetSlot2Requested += () => counts[SquidInkPulseGameplayCommand.UseGadgetSlot2]++;
            return counts;
        }

        private void ClickAllCommands(GameObject controls, int firstPointerId)
        {
            TouchGameplayCommandButton[] buttons =
                controls.GetComponentsInChildren<TouchGameplayCommandButton>(true);
            Assert.That(buttons, Has.Length.EqualTo(4));
            for (int index = 0; index < buttons.Length; index++)
            {
                buttons[index].Button.interactable = true;
                buttons[index].OnPointerDown(CreatePointer(firstPointerId + index, buttons[index].gameObject));
                buttons[index].OnPointerUp(CreatePointer(firstPointerId + index, buttons[index].gameObject));
            }
        }

        private static void AssertAllCommandCounts(
            Dictionary<SquidInkPulseGameplayCommand, int> counts,
            int expected)
        {
            Assert.That(counts.Values, Is.All.EqualTo(expected));
        }

        private static int CountMissingScripts(GameObject root)
        {
            int count = 0;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            }

            return count;
        }

        private static void InvokeScope(SquidInkPulseGameplayInputScope target, string methodName)
        {
            MethodInfo method = typeof(SquidInkPulseGameplayInputScope).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}
