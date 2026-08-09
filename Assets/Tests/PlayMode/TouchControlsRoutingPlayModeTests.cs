using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class TouchControlsRoutingPlayModeTests : InputTestFixture
    {
        private const string EpipelagicScenePath = "Assets/Scenes/Game/ZonaEpipelagica.unity";
        private const string AbyssopelagicScenePath = "Assets/Scenes/Game/ZonaAbisopelagica.unity";

        private Touchscreen touchscreen;

        public override void Setup()
        {
            base.Setup();
            RuntimeGadgetInventory.ResetForRuntime();
            RuntimeInkPulseState.ResetForRuntime();
            Time.timeScale = 1f;
            touchscreen = InputSystem.AddDevice<Touchscreen>();
        }

        public override void TearDown()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Null);
            RuntimeGadgetInventory.ResetForRuntime();
            RuntimeInkPulseState.ResetForRuntime();
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TouchscreenRoutesSteeringAndCommandsAcrossThePortalSceneChange()
        {
            yield return LoadScene(EpipelagicScenePath, "ZonaEpipelagica");

            SquidInkPulseGameplayInputReader firstReader = SquidInkPulseInputRuntime.Gameplay;
            Assert.That(firstReader, Is.Not.Null);
            Assert.That(firstReader.IsEnabled, Is.True);
            TouchGameplayControlsController firstControls = EnableTouchControlsForEditor();
            yield return null;
            Canvas.ForceUpdateCanvases();
            TouchSteeringSurface firstSurface = Object.FindFirstObjectByType<TouchSteeringSurface>();
            Assert.That(firstSurface, Is.Not.Null);

            int firstInkPulseRequests = 0;
            int firstPauseRequests = 0;
            firstReader.InkPulseRequested += () => firstInkPulseRequests++;
            firstReader.PauseToggleRequested += () => firstPauseRequests++;

            Vector2 steerStart = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 steerMoved = new(Screen.width * 0.42f, Screen.height * 0.68f);
            AssertRaycastReaches<TouchSteeringSurface>(steerStart, firstSurface);

            BeginTouch(1, steerStart, screen: touchscreen);
            yield return null;
            Assert.That(firstSurface.HasActivePointer, Is.True);
            Assert.That(firstReader.HasSteerPosition, Is.True);
            Assert.That(firstReader.SteerPosition, Is.EqualTo(steerStart));

            MoveTouch(1, steerMoved, screen: touchscreen);
            yield return null;
            Assert.That(firstReader.SteerPosition, Is.EqualTo(steerMoved));

            EndTouch(1, steerMoved, screen: touchscreen);
            yield return null;
            Assert.That(firstSurface.HasActivePointer, Is.False);
            Assert.That(firstReader.HasSteerPosition, Is.False);

            yield return TapButton(2, firstControls.InkPulseButton);
            Assert.That(firstInkPulseRequests, Is.EqualTo(1));

            yield return TapButton(3, firstControls.PauseButton);
            yield return null;
            Assert.That(firstPauseRequests, Is.EqualTo(1));
            Assert.That(GameSessionController.Instance.IsPaused, Is.True);

            SceneFlowController sceneFlow = Object.FindFirstObjectByType<SceneFlowController>();
            Assert.That(sceneFlow, Is.Not.Null);
            Assert.That(sceneFlow.TryLoadPortalDestinationFromActiveScene(), Is.True);
            yield return WaitForActiveScene("ZonaAbisopelagica");
            yield return null;

            SquidInkPulseGameplayInputReader secondReader = SquidInkPulseInputRuntime.Gameplay;
            Assert.That(secondReader, Is.Not.Null);
            Assert.That(secondReader, Is.Not.SameAs(firstReader));
            Assert.That(secondReader.IsEnabled, Is.True);
            Assert.That(firstReader.IsEnabled, Is.False);

            TouchGameplayControlsController secondControls = EnableTouchControlsForEditor();
            yield return null;
            Canvas.ForceUpdateCanvases();
            TouchSteeringSurface secondSurface = Object.FindFirstObjectByType<TouchSteeringSurface>();
            Assert.That(secondSurface, Is.Not.Null);

            int secondInkPulseRequests = 0;
            secondReader.InkPulseRequested += () => secondInkPulseRequests++;

            Vector2 secondSteer = new(Screen.width * 0.48f, Screen.height * 0.61f);
            AssertRaycastReaches<TouchSteeringSurface>(secondSteer, secondSurface);
            BeginTouch(4, secondSteer, screen: touchscreen);
            yield return null;
            Assert.That(secondReader.HasSteerPosition, Is.True);
            Assert.That(secondReader.SteerPosition, Is.EqualTo(secondSteer));
            EndTouch(4, secondSteer, screen: touchscreen);
            yield return null;

            yield return TapButton(5, secondControls.InkPulseButton);
            Assert.That(secondInkPulseRequests, Is.EqualTo(1));
            Assert.That(firstInkPulseRequests, Is.EqualTo(1),
                "La escena nueva no debe publicar comandos en el reader ya dispuesto.");
        }

        private static IEnumerator LoadScene(string scenePath, string expectedSceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null, $"No se pudo iniciar la carga de {scenePath}.");
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return WaitForActiveScene(expectedSceneName);
            yield return null;
        }

        private static IEnumerator WaitForActiveScene(string expectedSceneName)
        {
            const int maximumFrames = 180;
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (SceneManager.GetActiveScene().name == expectedSceneName)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"La escena activa no cambió a {expectedSceneName}.");
        }

        private static TouchGameplayControlsController EnableTouchControlsForEditor()
        {
            TouchControlsVisibilityController visibility =
                Object.FindFirstObjectByType<TouchControlsVisibilityController>(FindObjectsInactive.Include);
            Assert.That(visibility, Is.Not.Null);

            Transform controlsRoot = visibility.transform.Find("TouchControls");
            Assert.That(controlsRoot, Is.Not.Null);
            controlsRoot.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();

            TouchGameplayControlsController controls =
                visibility.GetComponentInChildren<TouchGameplayControlsController>(true);
            Assert.That(controls, Is.Not.Null);
            controls.RefreshPresentation();
            Assert.That(controls.InkPulseButton.interactable, Is.True);
            Assert.That(controls.PauseButton.interactable, Is.True);
            return controls;
        }

        private IEnumerator TapButton(int touchId, Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.interactable, Is.True);
            Vector2 position = GetScreenCenter(button.transform as RectTransform);
            TouchGameplayCommandButton commandButton = button.GetComponentInParent<TouchGameplayCommandButton>();
            Assert.That(commandButton, Is.Not.Null);
            AssertRaycastReaches<TouchGameplayCommandButton>(position, commandButton);

            BeginTouch(touchId, position, screen: touchscreen);
            yield return null;
            EndTouch(touchId, position, screen: touchscreen);
            yield return null;
        }

        private static Vector2 GetScreenCenter(RectTransform rectTransform)
        {
            Assert.That(rectTransform, Is.Not.Null);
            return RectTransformUtility.WorldToScreenPoint(
                null,
                rectTransform.TransformPoint(rectTransform.rect.center));
        }

        private static void AssertRaycastReaches<T>(Vector2 position, T expected)
            where T : Component
        {
            EventSystem eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);

            var eventData = new PointerEventData(eventSystem) { position = position };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(eventData, results);
            T routedTarget = results.Count > 0
                ? results[0].gameObject.GetComponentInParent<T>()
                : null;
            string route = results.Count == 0
                ? "<sin resultados>"
                : string.Join(
                    " > ",
                    results.Take(8).Select(result => result.gameObject.name));
            Assert.That(routedTarget, Is.SameAs(expected),
                $"El primer raycast en {position} no llegó a {typeof(T).Name}. Ruta: {route}");
        }
    }
}
