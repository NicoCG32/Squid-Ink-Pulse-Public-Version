using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class RunMenuTouchPlayModeTests : InputTestFixture
    {
        private Touchscreen screen;
        private int pointerId;
        private readonly Dictionary<FieldInfo, object> profileState = new();
        private UnityEngine.Random.State randomState;

        public override void Setup()
        {
            base.Setup();
            if (!Application.isEditor) Assert.Ignore("This fixture requires transient Editor profile writes.");
            Assert.That(PlayerProfileRepository.UsesTransientEditorPlayModeProfileSaves, Is.True);
            currentTime = Time.realtimeSinceStartupAsDouble;
            screen = InputSystem.AddDevice<Touchscreen>();
            pointerId = 0;
            Time.timeScale = 1f;
            randomState = UnityEngine.Random.state;
            foreach (string name in new[] { "currentProfile", "currentRecords", "currentCatalog", "loaded" })
            {
                FieldInfo field = typeof(PersistentPlayerProfile).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
                profileState[field] = field.GetValue(null);
            }
            var provider = new ResourcesJsonSeedProvider(PersistentDbPaths.SeedResourcesDirectoryName);
            Assert.That(provider.TryGetSeedText(PersistentDbPaths.UnlockablesCatalogFileName, out string seed), Is.True);
            var catalog = JsonUtility.FromJson<UnlockablesCatalogSaveData>(seed); catalog.Normalize();
            SetProfile("currentProfile", PlayerProfileSaveData.CreateDefault());
            SetProfile("currentRecords", PlayerRecordsSaveData.CreateDefault());
            SetProfile("currentCatalog", catalog);
            SetProfile("loaded", true);
            SceneFlowController.ResetRunScopedRuntimeState();
        }

        public override void TearDown()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            foreach (var entry in profileState) entry.Key.SetValue(null, entry.Value);
            profileState.Clear();
            typeof(ShrimpRuntimeWallet).GetField("initialized", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
            SceneFlowController.ResetRunScopedRuntimeState();
            UnityEngine.Random.state = randomState;
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Touch_PausesResumesTenTimesPerZoneAndReturnsThroughOptionsAndMenu()
        {
            foreach (string zone in new[] { "ZonaEpipelagica", "ZonaAbisopelagica" })
            {
                yield return Load(zone);
                GameUIRoot ui = Object.FindFirstObjectByType<GameUIRoot>();
                PauseMenuManager pause = ui.PauseMenuManager;
                for (int cycle = 0; cycle < 10; cycle++)
                {
                    yield return AssertSteering();
                    yield return Tap(Object.FindFirstObjectByType<TouchGameplayControlsController>().PauseButton);
                    yield return WaitFor(() => !Field<bool>(pause, "isAnimating") && GameSessionController.Instance.IsPaused);
                    ApplyMobileLayout(ui.PauseMenuRoot);
                    Assert.That(Time.timeScale, Is.Zero, $"{zone} pause {cycle}");
                    Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition, Is.False);
                    if (cycle == 0)
                    {
                        yield return Tap(Field<Button>(pause, "optionsButton"));
                        OptionsMenuManager options = Object.FindFirstObjectByType<OptionsMenuManager>(FindObjectsInactive.Include);
                        yield return WaitFor(() => options.IsOpen && Field<CanvasGroup>(options, "canvasGroup").interactable);
                        Assert.That(ui.PauseMenuRoot.activeSelf, Is.False);
                        Assert.That(Time.timeScale, Is.Zero);
                        yield return Tap(Field<Button>(options, "backButton"));
                        yield return WaitFor(() => !options.IsOpen && !Field<bool>(pause, "isAnimating") && ui.PauseMenuRoot.activeSelf);
                        Assert.That(GameSessionController.Instance.IsPaused, Is.True);
                        Assert.That(Time.timeScale, Is.Zero);
                    }
                    yield return Tap(Field<Button>(pause, "resumeButton"));
                    yield return WaitFor(() => GameSessionController.Instance.IsPlaying && !Field<bool>(pause, "isAnimating"));
                    Assert.That(Time.timeScale, Is.EqualTo(1f));
                    Assert.That(ui.PauseMenuRoot.activeSelf, Is.False);
                }
                yield return AssertSteering();
                yield return Tap(Object.FindFirstObjectByType<TouchGameplayControlsController>().PauseButton);
                yield return WaitFor(() => !Field<bool>(pause, "isAnimating") && GameSessionController.Instance.IsPaused);
                yield return Tap(Field<Button>(pause, "menuButton"));
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator Touch_RetriesThreeDefeatsFromAbyssAndReturnsToMenu()
        {
            yield return Load("ZonaAbisopelagica");
            for (int cycle = 0; cycle < 4; cycle++)
            {
                GameUIRoot ui = Object.FindFirstObjectByType<GameUIRoot>();
                GameSessionController session = GameSessionController.Instance;
                yield return AssertSteering();
                // Trigger the real defeat boundary deterministically, without testing hazard physics here.
                session.RequestGameOver();
                yield return WaitFor(() => ui.GameOverMenuRoot.activeSelf
                    && Field<CanvasGroup>(ui.GameOverMenuManager, "canvasGroup").interactable);
                ApplyMobileLayout(ui.GameOverMenuRoot);
                Assert.That(session.IsGameOver, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition, Is.False);
                if (cycle < 3)
                {
                    SquidInkPulseGameplayInputReader previous = SquidInkPulseInputRuntime.Gameplay;
                    yield return Tap(Field<Button>(ui.GameOverMenuManager, "retryButton"));
                    yield return WaitFor(() => GameSessionController.Instance != session && SceneManager.GetActiveScene().name == "ZonaEpipelagica");
                    yield return PrepareLoadedScene();
                    Assert.That(GameSessionController.Instance.IsPlaying, Is.True);
                    Assert.That(Time.timeScale, Is.EqualTo(1f));
                    Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Not.SameAs(previous));
                    Assert.That(previous.IsEnabled, Is.False);
                }
                else
                {
                    yield return Tap(Field<Button>(ui.GameOverMenuManager, "menuButton"));
                    yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu");
                    Assert.That(Time.timeScale, Is.EqualTo(1f));
                    Assert.That(SquidInkPulseInputRuntime.Gameplay, Is.Null);
                }
            }
        }

        private IEnumerator Load(string zone)
        {
            yield return SceneManager.LoadSceneAsync($"Assets/Scenes/Game/{zone}.unity", LoadSceneMode.Single);
            yield return PrepareLoadedScene();
            Assert.That(Mouse.current, Is.Null);
        }

        private static IEnumerator PrepareLoadedScene()
        {
            yield return null;
            Object.FindFirstObjectByType<PlayerMovement>().SetMovementSuppressed(true);
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour.GetType().Name.EndsWith("Spawner") || behaviour is RunProgressionDirector
                    || behaviour.GetType().Name == "BossEventDirector") behaviour.enabled = false;
            TouchControlsVisibilityController visibility = Object.FindFirstObjectByType<TouchControlsVisibilityController>(FindObjectsInactive.Include);
            visibility.transform.Find("TouchControls").gameObject.SetActive(true);
            Object.FindFirstObjectByType<TouchGameplayControlsController>().RefreshPresentation();
            yield return new WaitForSecondsRealtime(0.1f);
        }

        private IEnumerator AssertSteering()
        {
            Vector2 point = new(Screen.width * 0.5f, Screen.height * 0.5f);
            int id = ++pointerId;
            currentTime = Math.Max(currentTime, Time.realtimeSinceStartupAsDouble);
            BeginTouch(id, point, screen: screen); yield return null;
            Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition, Is.True, "Gameplay input must recover after the overlay/scene transition.");
            EndTouch(id, point, screen: screen); yield return null;
            Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition, Is.False);
        }

        private IEnumerator Tap(Button button)
        {
            Assert.That(button.IsInteractable() && button.gameObject.activeInHierarchy, Is.True, button.transform.parent.name);
            Canvas.ForceUpdateCanvases();
            Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransform rect = (RectTransform)button.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.FirstOrDefault().gameObject?.GetComponentInParent<Button>(), Is.SameAs(button), button.transform.parent.name);
            int id = ++pointerId;
            currentTime = Math.Max(currentTime, Time.realtimeSinceStartupAsDouble);
            BeginTouch(id, point, screen: screen); yield return null;
            EndTouch(id, point, screen: screen); yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> predicate)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 12d;
            while (!predicate() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(predicate(), Is.True, "The UI/scene transition did not finish.");
            yield return null;
        }

        private static void ApplyMobileLayout(GameObject root)
        {
            foreach (SafeAreaAdapter adapter in root.GetComponentsInChildren<SafeAreaAdapter>(true)) adapter.Refresh();
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in root.GetComponentsInChildren<MobileReferenceContentFitter>(true))
                fitter.Apply((fitter.transform.parent as RectTransform).rect.size, true);
            Canvas.ForceUpdateCanvases();
        }

        private static T Field<T>(object instance, string name) => (T)instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        private static void SetProfile(string name, object value) => typeof(PersistentPlayerProfile)
            .GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
    }
}
