using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class MobilePlatformPlayModeTests : InputTestFixture
    {
        private readonly Dictionary<FieldInfo, object> profileState = new();
        private GameObject platformRoot;
        private Keyboard keyboard;
        private UnityEngine.Random.State randomState;

        public override void Setup()
        {
            base.Setup();
            if (!Application.isEditor) Assert.Ignore("Requires transient Editor profile saves.");
            Assert.That(PlayerProfileRepository.UsesTransientEditorPlayModeProfileSaves, Is.True);
            currentTime = Time.realtimeSinceStartupAsDouble;
            keyboard = InputSystem.AddDevice<Keyboard>();
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
            Time.timeScale = 1f;
            platformRoot = new GameObject("MobilePlatformTest");
            Object.DontDestroyOnLoad(platformRoot);
            platformRoot.AddComponent<MobileBackNavigation>();
        }

        public override void TearDown()
        {
            if (platformRoot != null) Object.DestroyImmediate(platformRoot);
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
        public IEnumerator Back_ClosesMenusWithoutStartingOrAbandoningARun()
        {
            yield return Load("MainMenu/MainMenu");
            MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
            yield return Back();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            menu.Opciones();
            OptionsMenuManager options = Object.FindFirstObjectByType<OptionsMenuManager>(FindObjectsInactive.Include);
            yield return Back();
            yield return WaitFor(() => !options.IsOpen);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            menu.AbrirTutorial();
            Assert.That(menu.IsTutorialOpen, Is.True);
            yield return Back();
            Assert.That(menu.IsTutorialOpen, Is.False);
            menu.AbrirTienda();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "ShopMenu");
            yield return null;
            yield return Back();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu");
            yield return null;
            menu = Object.FindFirstObjectByType<MainMenu>();
            menu.Jugar();
            yield return null;
            LoreComicPresenter comic = Object.FindFirstObjectByType<LoreComicPresenter>();
            Assert.That(comic.IsPresenting, Is.True);
            yield return Back();
            Assert.That(comic.IsPresenting, Is.True, "Back must not acknowledge a comic or start gameplay.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        [UnityTest]
        public IEnumerator Back_PausesOnceResumesAndClosesShopInBothZones()
        {
            foreach (string zone in new[] { "ZonaEpipelagica", "ZonaAbisopelagica" })
            {
                yield return LoadZone(zone);
                GameSessionController session = GameSessionController.Instance;
                PauseMenuManager pause = Object.FindFirstObjectByType<PauseMenuManager>();
                int legacyPauseRequests = 0;
                SquidInkPulseInputRuntime.Gameplay.PauseToggleRequested += () => legacyPauseRequests++;
                yield return Back();
                yield return WaitFor(() => pause.CanTogglePauseNow);
                Assert.That(session.IsPaused, Is.True, "Escape/Cancel must not toggle twice in one frame.");
                Assert.That(legacyPauseRequests, Is.Zero);
                pause.Opciones();
                OptionsMenuManager options = Object.FindFirstObjectByType<OptionsMenuManager>(FindObjectsInactive.Include);
                yield return Back();
                yield return WaitFor(() => !options.IsOpen && pause.CanTogglePauseNow);
                Assert.That(session.IsPaused, Is.True, "Closing options must preserve pause.");
                yield return Back();
                yield return WaitFor(() => session.IsPlaying && pause.CanTogglePauseNow);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                InGameShopManager shop = InGameShopManager.Instance;
                Assert.That(shop.TryOpenTutorialOffer(GadgetId.ShellShield, 7), Is.True);
                int balance = PersistentPlayerProfile.TotalShrimps;
                yield return Back();
                Assert.That(InGameShopManager.IsShopOpen, Is.False);
                Assert.That(session.IsPlaying, Is.True);
                Assert.That(PersistentPlayerProfile.TotalShrimps, Is.EqualTo(balance));
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield), Is.False);
                session.RequestGameOver();
                yield return Back();
                Assert.That(session.IsGameOver, Is.True, "Back must not retry or abandon a defeat.");
            }
        }

        private IEnumerator Back()
        {
            currentTime = Math.Max(currentTime + 0.01d, Time.realtimeSinceStartupAsDouble);
            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            yield return null;
            currentTime = Math.Max(currentTime + 0.01d, Time.realtimeSinceStartupAsDouble);
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
        }

        private static IEnumerator Load(string path)
        {
            yield return SceneManager.LoadSceneAsync($"Assets/Scenes/{path}.unity");
            yield return null;
        }

        private static IEnumerator LoadZone(string zone)
        {
            yield return Load($"Game/{zone}");
            Object.FindFirstObjectByType<PlayerMovement>().SetMovementSuppressed(true);
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour.GetType().Name.EndsWith("Spawner") || behaviour is RunProgressionDirector
                    || behaviour.GetType().Name == "BossEventDirector") behaviour.enabled = false;
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 12d;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for the actual UI/session transition.");
        }

        private static void SetProfile(string name, object value) =>
            typeof(PersistentPlayerProfile).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
