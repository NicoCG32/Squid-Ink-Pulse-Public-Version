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
        private bool previousAudioPause;
        private static readonly MethodInfo DynamicUpdate = typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType) }, null);

        public override void Setup()
        {
            base.Setup();
            if (!Application.isEditor) Assert.Ignore("Requires transient Editor profile saves.");
            Assert.That(PlayerProfileRepository.UsesTransientEditorPlayModeProfileSaves, Is.True);
            currentTime = PlayerInputTestClock.StartAfterEditorTransition();
            keyboard = InputSystem.AddDevice<Keyboard>();
            randomState = UnityEngine.Random.state;
            previousAudioPause = AudioListener.pause;
            AudioListener.pause = false;
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
            AudioListener.pause = previousAudioPause;
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
                Assert.That(InGameShopManager.IsShopOpen, Is.False,
                    $"{zone}: active={MobileBackNavigation.IsActive}, suspended={MobileApplicationLifecycle.IsSuspended}, "
                    + $"advance={MobileApplicationLifecycle.CanAdvancePresentation}, options={options.IsOpen}, "
                    + $"comic={Object.FindFirstObjectByType<LoreComicPresenter>().IsPresenting}, "
                    + $"cancelPending={Field<bool>(Object.FindFirstObjectByType<MobileBackNavigation>(), "cancelRequested")}");
                Assert.That(session.IsPlaying, Is.True);
                Assert.That(PersistentPlayerProfile.TotalShrimps, Is.EqualTo(balance));
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield), Is.False);
                session.RequestGameOver();
                yield return Back();
                Assert.That(session.IsGameOver, Is.True, "Back must not retry or abandon a defeat.");
            }
        }

        [UnityTest]
        public IEnumerator Interruption_WaitsForBothCallbacksAndDiscardsQueuedCommandsInBothZones()
        {
            var lifecycle = platformRoot.AddComponent<MobileApplicationLifecycle>();
            foreach (string zone in new[] { "ZonaEpipelagica", "ZonaAbisopelagica" })
            {
                yield return LoadZone(zone);
                GameSessionController session = GameSessionController.Instance;
                PauseMenuManager pause = Object.FindFirstObjectByType<PauseMenuManager>();
                SquidInkPulseGameplayInputReader reader = SquidInkPulseInputRuntime.Gameplay;
                using var queued = new GameplayCommandInputBinding(reader, SquidInkPulseGameplayCommand.UseGadgetSlot1);
                int transitions = 0;
                session.StateChanged += (_, _) => transitions++;
                Assert.That(reader.TryBeginTouchSteering(platformRoot, 32, new Vector2(200, 200)), Is.True);
                Assert.That(reader.TryRequestTouchCommand(SquidInkPulseGameplayCommand.UseGadgetSlot1), Is.True);
                lifecycle.HandleApplicationFocus(false);
                lifecycle.HandleApplicationPause(true);
                lifecycle.HandleApplicationPause(true);
                Assert.That(session.IsPaused, Is.True);
                Assert.That(reader.IsEnabled, Is.False);
                Assert.That(reader.HasSteerPosition, Is.False);
                Assert.That(queued.TryConsumeRequest(), Is.False, "A pre-interruption command must not replay.");
                Assert.That(AudioListener.pause, Is.True);
                session.RequestResume();
                Assert.That(session.IsPaused, Is.True, "Background resume must be rejected.");
                lifecycle.HandleApplicationFocus(true);
                Assert.That(MobileApplicationLifecycle.IsSuspended, Is.True);
                Assert.That(reader.IsEnabled, Is.False);
                lifecycle.HandleApplicationPause(false);
                yield return null;
                Assert.That(reader.IsEnabled, Is.True);
                Assert.That(reader.HasSteerPosition, Is.False);
                Assert.That(AudioListener.pause, Is.False);
                Assert.That(session.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(transitions, Is.EqualTo(1));
                pause.Reanudar();
                yield return WaitFor(() => session.IsPlaying && pause.CanTogglePauseNow);
                Assert.That(reader.TryBeginTouchSteering(platformRoot, 33, new Vector2(300, 300)), Is.True);
                reader.CancelTouchSteering(platformRoot);
                Assert.That(transitions, Is.EqualTo(2));
                session.RequestPause();
                lifecycle.HandleApplicationPause(true);
                lifecycle.HandleApplicationFocus(false);
                lifecycle.HandleApplicationPause(false);
                lifecycle.HandleApplicationFocus(true);
                yield return null;
                Assert.That(session.IsPaused, Is.True, "A user pause must survive either callback order.");
                Assert.That(transitions, Is.EqualTo(3));
            }
        }

        [UnityTest]
        public IEnumerator Interruption_CancelsResumeAnimationAndKeepsReplacementScenePaused()
        {
            var lifecycle = platformRoot.AddComponent<MobileApplicationLifecycle>();
            yield return LoadZone("ZonaEpipelagica");
            GameSessionController original = GameSessionController.Instance;
            PauseMenuManager pause = Object.FindFirstObjectByType<PauseMenuManager>();
            original.RequestPause();
            yield return WaitFor(() => pause.CanTogglePauseNow);
            pause.Reanudar();
            yield return null;
            lifecycle.HandleApplicationPause(true);
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(original.IsPaused, Is.True);
            Assert.That(Object.FindFirstObjectByType<GameUIRoot>().PauseMenuRoot.activeSelf, Is.True);
            lifecycle.HandleApplicationPause(false);
            yield return null;
            Assert.That(original.IsPaused, Is.True);
            pause.Reanudar();
            yield return WaitFor(() => original.IsPlaying);
            lifecycle.HandleApplicationFocus(false);
            yield return LoadZone("ZonaAbisopelagica");
            Assert.That(GameSessionController.Instance, Is.Not.SameAs(original));
            Assert.That(GameSessionController.Instance.IsPaused, Is.True);
            Assert.That(SquidInkPulseInputRuntime.Gameplay.IsEnabled, Is.False);
            lifecycle.HandleApplicationFocus(true);
            yield return null;
            pause = Object.FindFirstObjectByType<PauseMenuManager>();
            yield return WaitFor(() => pause.CanTogglePauseNow);
            Assert.That(GameSessionController.Instance.IsPaused, Is.True);
            pause.Reanudar();
            yield return WaitFor(() => GameSessionController.Instance.IsPlaying);
            GameSessionController.Instance.RequestGameOver();
            lifecycle.HandleApplicationPause(true);
            lifecycle.HandleApplicationPause(false);
            Assert.That(GameSessionController.Instance.IsGameOver, Is.True);
        }

        [UnityTest]
        public IEnumerator Interruption_PreservesShopTimerThenShowsPauseWhenTheOfferCloses()
        {
            var lifecycle = platformRoot.AddComponent<MobileApplicationLifecycle>();
            yield return LoadZone("ZonaEpipelagica");
            InGameShopManager shop = InGameShopManager.Instance;
            Assert.That(shop.TryOpenTutorialOffer(GadgetId.ShellShield, 7), Is.True);
            lifecycle.HandleApplicationFocus(false);
            float remaining = Field<InGameShopOfferTimer>(shop, "offerTimer").RemainingSeconds;
            int balance = PersistentPlayerProfile.TotalShrimps;
            yield return new WaitForSecondsRealtime(0.3f);
            shop.BuyCurrentOffer();
            Assert.That(Field<InGameShopOfferTimer>(shop, "offerTimer").RemainingSeconds, Is.EqualTo(remaining));
            Assert.That(PersistentPlayerProfile.TotalShrimps, Is.EqualTo(balance));
            Assert.That(InGameShopManager.IsShopOpen, Is.True);
            lifecycle.HandleApplicationFocus(true);
            yield return null;
            yield return null;
            shop.CloseShop();
            Assert.That(GameSessionController.Instance.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero, "Closing the overlay must not release interrupted gameplay.");
            PauseMenuManager pause = Object.FindFirstObjectByType<PauseMenuManager>();
            yield return WaitFor(() => pause.CanTogglePauseNow);
            pause.Reanudar();
            yield return WaitFor(() => GameSessionController.Instance.IsPlaying);
        }

        [UnityTest]
        public IEnumerator Interruption_FreezesComicTimerAndPreservesThePreviousAudioPause()
        {
            var lifecycle = platformRoot.AddComponent<MobileApplicationLifecycle>();
            yield return LoadZone("ZonaAbisopelagica");
            LoreComicPresenter comic = Object.FindFirstObjectByType<LoreComicPresenter>();
            LoreComicEntry entry = Array.Find(Field<LoreComicEntry[]>(comic, "entries"),
                item => item.comicEvent == LoreComicEvent.GameStart && LoreComicEntrySelector.HasDisplayableSprite(item));
            Assert.That(entry, Is.Not.Null);
            entry.displaySeconds = 0.2f;
            entry.waitForContinue = false;
            entry.showContinueButton = false;
            bool completed = false;
            Assert.That(comic.TryPlay(new LoreComicRequest(LoreComicEvent.GameStart, LoreComicZone.Unknown), () => completed = true), Is.True);
            AudioListener.pause = true;
            lifecycle.HandleApplicationPause(true);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(completed, Is.False);
            Assert.That(comic.IsPresenting, Is.True);
            lifecycle.HandleApplicationPause(false);
            Assert.That(AudioListener.pause, Is.True, "Restore the previous audio state, not a hardcoded unpause.");
            yield return WaitFor(() => completed);
            Assert.That(GameSessionController.Instance.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
        }

        private IEnumerator Back()
        {
            SyncInputClock();
            Press(keyboard.escapeKey, time: currentTime, queueEventOnly: true);
            ProcessPlayerInput();
            yield return WaitFor(() => keyboard.escapeKey.isPressed);
            yield return null;
            yield return null;
            SyncInputClock();
            Release(keyboard.escapeKey, time: currentTime, queueEventOnly: true);
            ProcessPlayerInput();
            yield return WaitFor(() => !keyboard.escapeKey.isPressed);
            yield return null;
        }

        private void SyncInputClock() => currentTime = Math.Max(Math.Max(currentTime,
            UnityEngine.InputSystem.LowLevel.InputState.currentTime), keyboard.lastUpdateTime) + 0.02d;

        private static void ProcessPlayerInput()
        {
            // Match the existing comic fixture: batch Editor updates use another buffer.
            Assert.That(DynamicUpdate, Is.Not.Null, "Input System player-update entry point changed.");
            DynamicUpdate.Invoke(null, new object[] { UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic });
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

        private static IEnumerator WaitFor(Func<bool> condition, [System.Runtime.CompilerServices.CallerLineNumber] int callLine = 0)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 12d;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(condition(), Is.True, $"Timed out waiting for UI/session transition at line {callLine}.");
        }

        private static void SetProfile(string name, object value) =>
            typeof(PersistentPlayerProfile).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
