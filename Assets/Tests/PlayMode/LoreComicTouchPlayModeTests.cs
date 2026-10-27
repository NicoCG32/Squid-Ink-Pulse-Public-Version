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
using Object=UnityEngine.Object;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class LoreComicTouchPlayModeTests : InputTestFixture
    {
        private Touchscreen screen;
        private Keyboard keyboard;
        private int pointer;
        private readonly Dictionary<FieldInfo,object> profileState=new();
        private UnityEngine.Random.State randomState;
        private readonly Dictionary<string,int> loads=new();
        private static readonly MethodInfo DynamicUpdate=typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,
            null,new[] {typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType)},null);
        public override void Setup()
        {
            base.Setup();
            Assert.That(PlayerProfileRepository.UsesTransientEditorPlayModeProfileSaves,Is.True);
            currentTime=PlayerInputTestClock.StartAfterEditorTransition(); Time.timeScale=1f; pointer=0;
            screen=InputSystem.AddDevice<Touchscreen>(); keyboard=InputSystem.AddDevice<Keyboard>();
            randomState=UnityEngine.Random.state;
            foreach (string name in new[] {"currentProfile","currentRecords","currentCatalog","loaded"})
            {
                FieldInfo field=typeof(PersistentPlayerProfile).GetField(name,BindingFlags.Static|BindingFlags.NonPublic);
                profileState[field]=field.GetValue(null);
            }
            var provider=new ResourcesJsonSeedProvider(PersistentDbPaths.SeedResourcesDirectoryName);
            Assert.That(provider.TryGetSeedText(PersistentDbPaths.UnlockablesCatalogFileName,out string seed),Is.True);
            var catalog=JsonUtility.FromJson<UnlockablesCatalogSaveData>(seed); catalog.Normalize();
            var records=PlayerRecordsSaveData.CreateDefault(); records.totalShrimps=100;
            SetProfile("currentProfile",PlayerProfileSaveData.CreateDefault()); SetProfile("currentRecords",records);
            SetProfile("currentCatalog",catalog); SetProfile("loaded",true);
            ShrimpRuntimeWallet.ResetForRuntime(); SceneFlowController.ResetRunScopedRuntimeState();
            loads.Clear(); SceneManager.sceneLoaded+=RecordLoad;
        }
        public override void TearDown()
        {
            SceneManager.sceneLoaded-=RecordLoad;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            foreach (var entry in profileState) entry.Key.SetValue(null,entry.Value);
            profileState.Clear();
            typeof(ShrimpRuntimeWallet).GetField("initialized",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
            SceneFlowController.ResetRunScopedRuntimeState(); UnityEngine.Random.state=randomState; Time.timeScale=1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator KeyboardSubmit_ContinuesStartComicAndLoadsOnce()
        {
            yield return Load("MainMenu");
            MainMenu menu=Object.FindFirstObjectByType<MainMenu>();
            Button play=menu.GetComponentsInChildren<Transform>(true).Single(item=>item.name=="JugarBoton").GetComponentInChildren<Button>(true);
            yield return Tap(play);
            LoreComicPresenter lore=Object.FindFirstObjectByType<LoreComicPresenter>();
            Button next=Field<Button>(lore,"continueButton");
            yield return WaitFor(()=>next.gameObject.activeInHierarchy);
            Assert.That(Time.timeScale,Is.Zero);
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(next.gameObject),"Submit must belong to the visible comic.");
            currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            Press(keyboard.enterKey); yield return null; Release(keyboard.enterKey); yield return null;
            yield return WaitFor(()=>SceneManager.GetActiveScene().name=="ZonaEpipelagica");
            yield return null;
            Assert.That(loads["ZonaEpipelagica"],Is.EqualTo(1));
            Assert.That(Time.timeScale,Is.EqualTo(1f));
            Assert.That(Mouse.current,Is.Null);
        }

        [UnityTest]
        public IEnumerator Portals_RestoreControlAndLoadEachDestinationOnceWithoutReplayingSeenComics()
        {
            yield return Load("ZonaEpipelagica");
            foreach (string target in new[] {"ZonaAbisopelagica","ZonaEpipelagica"})
            {
                LoreComicPresenter lore=Object.FindFirstObjectByType<LoreComicPresenter>();
                ShortenTimedEntries(lore);
                var controls=Object.FindFirstObjectByType<TouchGameplayControlsController>();
                EventSystem.current.SetSelectedGameObject(controls.PauseButton.gameObject);
                int before=loads.TryGetValue(target,out int count) ? count : 0;
                GameObject player=GameObject.FindGameObjectWithTag(GameplayTagCatalog.Player);
                var portal=new GameObject("PortalForComicTest"); portal.transform.position=player.transform.position;
                portal.AddComponent<CircleCollider2D>(); portal.AddComponent<ScenePortal>();
                yield return WaitFor(()=>Visible(lore));
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null,"Timed comics must consume Submit context behind them.");
                yield return Submit();
                yield return WaitFor(()=>SceneManager.GetActiveScene().name==target);
                yield return PrepareGameplay();
                Assert.That(loads[target],Is.EqualTo(before+1));
                Assert.That(Time.timeScale,Is.EqualTo(1f));
            }
            Assert.That(PersistentPlayerProfile.HasSeenLoreComic(LoreComicEvent.PortalEpipelagicToAbyssopelagic.ToString()),Is.True);
            Assert.That(PersistentPlayerProfile.HasSeenLoreComic(LoreComicEvent.PortalAbyssopelagicToEpipelagic.ToString()),Is.True);
            yield return LoreComicPresenter.PlayPortalTransitionIfAvailable("Assets/Scenes/Game/ZonaAbisopelagica.unity");
            Assert.That(Visible(Object.FindFirstObjectByType<LoreComicPresenter>()),Is.False);
            Assert.That(Time.timeScale,Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator DealerEntryAndPurchase_ReturnToShopAndGameplayOnceInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone); SetProfile("currentProfile",PlayerProfileSaveData.CreateDefault());
                LoreComicPresenter lore=Object.FindFirstObjectByType<LoreComicPresenter>(); ShortenTimedEntries(lore);
                InGameShopManager shop=InGameShopManager.Instance;
                int opened=0,closed=0; shop.onShopOpened.AddListener(()=>opened++); shop.onShopClosed.AddListener(()=>closed++);
                int balance=PersistentPlayerProfile.TotalShrimps;
                shop.QueueTutorialOffer(GadgetId.ShellShield,7);
                Assert.That(InGameShopManager.TryOpenShopFromWorld(),Is.True);
                yield return WaitFor(()=>Visible(lore));
                Assert.That(InGameShopManager.IsShopOpen,Is.False);
                Assert.That(InGameShopManager.BlocksInkPulseActivation,Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null);
                yield return WaitFor(()=>InGameShopManager.IsShopOpen);
                Assert.That(opened,Is.EqualTo(1)); Assert.That(Visible(lore),Is.False);
                Assert.That(Time.timeScale,Is.Zero,"The shop must retain its own time hold after the entry comic.");
                yield return Tap(Field<Button>(shop,"buyButton"));
                yield return WaitFor(()=>Visible(lore));
                Assert.That(closed,Is.EqualTo(1)); Assert.That(InGameShopManager.IsShopOpen,Is.False);
                Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(balance-7));
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield),Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                yield return WaitFor(()=>!Visible(lore));
                Assert.That(Time.timeScale,Is.EqualTo(1f));
                Assert.That(PersistentPlayerProfile.HasSeenLoreComic(LoreComicEvent.ShopInGameFirst.ToString()),Is.True);
                Assert.That(PersistentPlayerProfile.HasSeenLoreComic(LoreComicEvent.ShopInGameLastPurchased.ToString()),Is.True);
                yield return LoreComicPresenter.PlayInGameShopLastPurchasedIfAvailable();
                Assert.That(Visible(lore),Is.False); Assert.That(closed,Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator DealerWithoutPurchase_OmitsUnconfiguredExitAndRestoresTimeInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone); SetProfile("currentProfile",PlayerProfileSaveData.CreateDefault());
                LoreComicPresenter lore=Object.FindFirstObjectByType<LoreComicPresenter>(); ShortenTimedEntries(lore);
                InGameShopManager shop=InGameShopManager.Instance;
                typeof(InGameShopManager).GetField("offerDurationSeconds",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(shop,0.75f);
                int opened=0,closed=0; shop.onShopOpened.AddListener(()=>opened++); shop.onShopClosed.AddListener(()=>closed++);
                int balance=PersistentPlayerProfile.TotalShrimps;
                shop.QueueTutorialOffer(GadgetId.InkBottle,7);
                Assert.That(InGameShopManager.TryOpenShopFromWorld(),Is.True);
                yield return WaitFor(()=>InGameShopManager.IsShopOpen);
                yield return WaitFor(()=>!InGameShopManager.IsShopOpen);
                yield return null;
                Assert.That(opened,Is.EqualTo(1)); Assert.That(closed,Is.EqualTo(1));
                Assert.That(Visible(lore),Is.False); Assert.That(Time.timeScale,Is.EqualTo(1f));
                Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(balance));
                Assert.That(PersistentPlayerProfile.HasSeenLoreComic(LoreComicEvent.ShopInGameLastNoPurchase.ToString()),Is.False,
                    "A missing sprite must not become a seen comic or a stuck time hold.");
            }
        }

        [UnityTest]
        public IEnumerator Defeat_ReturnsToGameOverOnceAndRetryRestartsPrimaryZone()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone);
                LoreComicPresenter lore=Object.FindFirstObjectByType<LoreComicPresenter>(); ShortenTimedEntries(lore);
                GameUIRoot ui=Object.FindFirstObjectByType<GameUIRoot>();
                GameSessionController.Instance.RequestGameOver();
                yield return WaitFor(()=>Visible(lore));
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null);
                yield return Submit();
                yield return WaitFor(()=>ui.GameOverMenuRoot.activeSelf && Field<CanvasGroup>(ui.GameOverMenuManager,"canvasGroup").interactable);
                Assert.That(Visible(lore),Is.False);
                Assert.That(GameSessionController.Instance.IsGameOver,Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                int before=loads.TryGetValue("ZonaEpipelagica",out int count) ? count : 0;
                yield return Tap(Field<Button>(ui.GameOverMenuManager,"retryButton"));
                yield return WaitFor(()=>SceneManager.GetActiveScene().name=="ZonaEpipelagica" && loads["ZonaEpipelagica"]==before+1);
                Assert.That(Time.timeScale,Is.EqualTo(1f));
            }
        }

        [UnityTest]
        public IEnumerator ShopAndComic_RejectPauseAndCompetingOpenRequestsInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone);
                var pause=Object.FindFirstObjectByType<PauseMenuManager>();
                var lore=Object.FindFirstObjectByType<LoreComicPresenter>();
                ShortenTimedEntries(lore);
                InGameShopManager shop=InGameShopManager.Instance;
                SetField(shop,"offerDurationSeconds",5f);
                shop.QueueTutorialOffer(GadgetId.ShellShield,7);
                Assert.That(shop.TryOpenTimedShop(),Is.True);
                yield return PauseShortcut();
                Assert.That(GameSessionController.Instance.IsPlaying,Is.True,"Pause must not take the shop's context.");
                Assert.That(pause.CanTogglePauseNow,Is.False);
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(Field<GameObject>(pause,"menuRoot").activeSelf,Is.False);
                LoreComicRequest request=PortalRequest(zone);
                Assert.That(lore.TryPlay(request,()=>Assert.Fail("Rejected comic cannot complete.")),Is.False,
                    "An unrelated comic must not replace an open shop.");
                shop.CloseShop();
                yield return WaitFor(()=>!InGameShopManager.BlocksInkPulseActivation);

                int completions=0;
                Assert.That(lore.TryPlay(request,()=>completions++),Is.True);
                yield return PauseShortcut();
                Assert.That(GameSessionController.Instance.IsPlaying,Is.True,"Pause must not take the comic's context.");
                Assert.That(pause.CanTogglePauseNow,Is.False);
                shop.QueueTutorialOffer(GadgetId.ShellShield,7);
                Assert.That(shop.TryOpenTimedShop(),Is.False,"Opening a shop during a comic must not capture a zero time scale.");
                Assert.That(InGameShopManager.TryOpenShopFromWorld(),Is.False);
                Assert.That(Time.timeScale,Is.Zero);
                yield return WaitFor(()=>completions==1);
                Assert.That(Time.timeScale,Is.EqualTo(1f));
                Assert.That(InGameShopManager.IsShopOpen,Is.False);
            }
        }

        [UnityTest]
        public IEnumerator OptionsFromPause_ConsumesPauseShortcutUntilClosedInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone);
                var pause=Object.FindFirstObjectByType<PauseMenuManager>();
                pause.TogglePause();
                yield return WaitFor(()=>Field<Button>(pause,"optionsButton").IsInteractable()
                    && Field<CanvasGroup>(pause,"canvasGroup").interactable);
                yield return Tap(Field<Button>(pause,"optionsButton"));
                var options=Field<OptionsMenuManager>(pause,"optionsMenu");
                yield return WaitFor(()=>options.IsOpen);
                yield return PauseShortcut();
                yield return new WaitForSecondsRealtime(2f);
                Assert.That(GameSessionController.Instance.IsPaused,Is.True,"P must not resume gameplay behind Options.");
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(pause.CanTogglePauseNow,Is.False);
                Assert.That(Field<GameObject>(pause,"menuRoot").activeSelf,Is.False);
                options.Close();
                yield return WaitFor(()=>!options.IsOpen && pause.CanTogglePauseNow);
                yield return Tap(Field<Button>(pause,"resumeButton"));
                yield return WaitFor(()=>GameSessionController.Instance.IsPlaying);
                Assert.That(Time.timeScale,Is.EqualTo(1f));
            }
        }

        [UnityTest]
        public IEnumerator ConcurrentComics_PreserveFirstOwnerAndCompleteOnceInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone);
                var lore=Object.FindFirstObjectByType<LoreComicPresenter>();
                ShortenTimedEntries(lore);
                int first=0, second=0;
                Assert.That(lore.TryPlay(PortalRequest(zone),()=>first++),Is.True);
                Assert.That(lore.TryPlay(PortalRequest(zone),()=>second++),Is.False,
                    "A competing request must not abandon the first consumer's callback.");
                yield return WaitFor(()=>first==1);
                Assert.That(second,Is.Zero);
                Assert.That(Time.timeScale,Is.EqualTo(1f));
                Assert.That(Visible(lore),Is.False);
            }
        }

        [UnityTest]
        public IEnumerator GameOver_PreemptsShopAndKeepsTimeStoppedAfterDefeatInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone);
                var lore=Object.FindFirstObjectByType<LoreComicPresenter>();
                ShortenTimedEntries(lore);
                InGameShopManager shop=InGameShopManager.Instance;
                shop.QueueTutorialOffer(GadgetId.ShellShield,7);
                Assert.That(shop.TryOpenTimedShop(),Is.True);
                int closed=0; shop.onShopClosed.AddListener(()=>closed++);
                GameSessionController.Instance.RequestGameOver();
                int balance=PersistentPlayerProfile.TotalShrimps;
                shop.BuyCurrentOffer();
                Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(balance),"A terminal session must reject a purchase before the shop's next Update.");
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield),Is.False);
                yield return WaitFor(()=>!InGameShopManager.IsShopOpen && !Visible(lore));
                var gameOver=Object.FindFirstObjectByType<GameOverMenuManager>();
                yield return WaitFor(()=>Field<CanvasGroup>(gameOver,"canvasGroup").interactable);
                yield return PauseShortcut();
                Assert.That(GameSessionController.Instance.IsGameOver,Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(closed,Is.EqualTo(1));
                Assert.That(Field<GameObject>(shop,"menuRoot").activeSelf,Is.False);
            }
        }

        [UnityTest]
        public IEnumerator Comic_CancelsHeldSteeringAndConsumesCommandsUntilANewGestureInBothZones()
        {
            foreach (string zone in new[] {"ZonaEpipelagica","ZonaAbisopelagica"})
            {
                yield return Load(zone);
                var lore=Object.FindFirstObjectByType<LoreComicPresenter>();
                ShortenTimedEntries(lore);
                var pulse=Object.FindFirstObjectByType<InkPulseController>();
                pulse.ForceEmptyCharge(); Assert.That(pulse.TryForceReady(),Is.True);
                int starts=0; pulse.PulseStarted+=()=>starts++;
                Vector2 point=new(Screen.width*0.5f,Screen.height*0.5f);
                int id=++pointer;
                SyncInputClock(); BeginTouch(id,point,queueEventOnly:true,screen:screen);
                currentTime+=0.02d; ProcessPlayerInput(); yield return WaitForTouch(id,true);
                Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition,Is.True);
                int completed=0;
                Assert.That(lore.TryPlay(PortalRequest(zone),()=>completed++),Is.True);
                Press(keyboard.spaceKey); yield return null; Release(keyboard.spaceKey); yield return null;
                Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition,Is.False);
                yield return WaitFor(()=>completed==1);
                SyncInputClock(); EndTouch(id,point,queueEventOnly:true,screen:screen);
                currentTime+=0.02d; ProcessPlayerInput(); yield return WaitForTouch(id,false);
                Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition,Is.False,"The old pointer cannot regain ownership when the comic closes.");
                Assert.That(starts,Is.Zero);
                pulse.ForceEmptyCharge();
                Assert.That(RuntimeGadgetInventory.Acquire(GadgetId.InkBottle,null,Color.white),Is.True);
                int gadgetRequests=0;
                SquidInkPulseInputRuntime.Gameplay.GadgetSlot1Requested+=()=>gadgetRequests++;
                completed=0;
                Assert.That(lore.TryPlay(PortalRequest(zone),()=>completed++),Is.True);
                Press(keyboard.qKey); yield return null; Release(keyboard.qKey); yield return null;
                Assert.That(gadgetRequests,Is.EqualTo(1),"The configured gadget action must actually reach its consumer.");
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle),Is.True);
                Assert.That(pulse.IsCharged,Is.False);
                yield return WaitFor(()=>completed==1);
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle),Is.True,"A blocked gadget command must not replay after close.");
                var controls=Object.FindFirstObjectByType<TouchGameplayControlsController>();
                controls.RefreshPresentation();
                yield return Tap(controls.GadgetSlot1Button);
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle),Is.False);
                Assert.That(pulse.IsCharged,Is.True);
                controls.RefreshPresentation();
                yield return Tap(controls.InkPulseButton);
                Assert.That(starts,Is.EqualTo(1),"A new deliberate command remains available.");
            }
        }

        [UnityTest]
        public IEnumerator GameOver_DuringPortalComicCancelsSceneLoadAndSerializesDefeat()
        {
            yield return Load("ZonaEpipelagica");
            var lore=Object.FindFirstObjectByType<LoreComicPresenter>();
            ShortenTimedEntries(lore);
            var portal=new GameObject("PortalForTerminalOverlayTest");
            portal.transform.position=GameObject.FindGameObjectWithTag(GameplayTagCatalog.Player).transform.position;
            portal.AddComponent<CircleCollider2D>(); portal.AddComponent<ScenePortal>();
            yield return WaitFor(()=>Visible(lore));
            GameSessionController.Instance.RequestGameOver();
            var gameOver=Object.FindFirstObjectByType<GameOverMenuManager>();
            Assert.That(Field<GameObject>(gameOver,"menuRoot").activeSelf,Is.False);
            yield return WaitFor(()=>Field<CanvasGroup>(gameOver,"canvasGroup").interactable);
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("ZonaEpipelagica"));
            Assert.That(loads.ContainsKey("ZonaAbisopelagica"),Is.False);
            Assert.That(GameSessionController.Instance.IsGameOver,Is.True);
            Assert.That(Time.timeScale,Is.Zero);
            Assert.That(Visible(lore),Is.False);
        }

        private IEnumerator PauseShortcut()
        {
            currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            Press(keyboard.pKey); yield return null; Release(keyboard.pKey); yield return null;
        }
        private static LoreComicRequest PortalRequest(string zone) => zone=="ZonaEpipelagica"
            ? new LoreComicRequest(LoreComicEvent.PortalEpipelagicToAbyssopelagic,LoreComicZone.Abyssopelagic)
            : new LoreComicRequest(LoreComicEvent.PortalAbyssopelagicToEpipelagic,LoreComicZone.Epipelagic);
        private static void SetField(object target,string name,object value) => target.GetType()
            .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);

        private static IEnumerator Load(string zone)
        {
            SceneFlowController.ResetRunScopedRuntimeState();
            string path=zone=="MainMenu" ? "Assets/Scenes/MainMenu/MainMenu.unity" : $"Assets/Scenes/Game/{zone}.unity";
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);
            yield return null; yield return new WaitForSecondsRealtime(0.1f);
            yield return PrepareGameplay();
        }
        private static IEnumerator PrepareGameplay()
        {
            PlayerMovement movement=Object.FindFirstObjectByType<PlayerMovement>();
            if (movement!=null) movement.SetMovementSuppressed(true);
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour.GetType().Name.EndsWith("Spawner") || behaviour is RunProgressionDirector
                    || behaviour.GetType().Name=="BossEventDirector") behaviour.enabled=false;
            var visibility=Object.FindFirstObjectByType<TouchControlsVisibilityController>(FindObjectsInactive.Include);
            if (visibility!=null) visibility.ControlsRoot.SetActive(true);
            yield return null;
        }
        private IEnumerator Submit()
        {
            currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            Press(keyboard.enterKey); yield return null; Release(keyboard.enterKey); yield return null;
        }
        private static bool Visible(LoreComicPresenter lore) => lore!=null && Field<CanvasGroup>(lore,"canvasGroup").alpha>0f;
        private static void ShortenTimedEntries(LoreComicPresenter lore)
        {
            foreach (LoreComicEntry entry in Field<LoreComicEntry[]>(lore,"entries"))
                if (entry.comicEvent!=LoreComicEvent.GameStart) entry.displaySeconds=0.75f;
        }
        private IEnumerator Tap(Button button)
        {
            Assert.That(button.IsInteractable() && button.gameObject.activeInHierarchy,Is.True);
            Canvas.ForceUpdateCanvases(); Canvas canvas=button.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect=(RectTransform)button.transform;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=point},hits);
            Assert.That(hits.FirstOrDefault().gameObject?.GetComponentInParent<Button>(),Is.SameAs(button));
            int id=++pointer; SyncInputClock();
            BeginTouch(id,point,queueEventOnly:true,screen:screen);
            currentTime+=0.02d; ProcessPlayerInput();
            yield return WaitForTouch(id,true);
            SyncInputClock();
            EndTouch(id,point,queueEventOnly:true,screen:screen);
            currentTime+=0.02d; ProcessPlayerInput();
            yield return WaitForTouch(id,false);
        }
        private IEnumerator WaitForTouch(int id,bool pressed)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+2d;
            while ((screen.primaryTouch.touchId.ReadValue()!=id || screen.primaryTouch.press.isPressed!=pressed)
                && Time.realtimeSinceStartupAsDouble<deadline)
            {
                currentTime=Math.Max(currentTime+0.001d,Time.realtimeSinceStartupAsDouble);
                yield return null;
            }
            Assert.That(screen.primaryTouch.touchId.ReadValue(),Is.EqualTo(id),$"Queued touch was not delivered: enabled={screen.enabled}; last={screen.lastUpdateTime}; input={UnityEngine.InputSystem.LowLevel.InputState.currentTime}; clock={currentTime}");
            Assert.That(screen.primaryTouch.press.isPressed,Is.EqualTo(pressed),$"Touch edge missing: last={screen.lastUpdateTime}; input={UnityEngine.InputSystem.LowLevel.InputState.currentTime}; clock={currentTime}");
            yield return null;
        }
        private void SyncInputClock() => currentTime=Math.Max(Math.Max(currentTime,
            UnityEngine.InputSystem.LowLevel.InputState.currentTime),screen.lastUpdateTime)+0.02d;
        private static void ProcessPlayerInput()
        {
            // InputSystem.Update() may select the Editor buffer in batch mode.
            // Use the SDK's player overload, which is internal in this package.
            Assert.That(DynamicUpdate,Is.Not.Null,"InputSystem player-update entry point changed.");
            DynamicUpdate.Invoke(null,new object[] {UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic});
        }
        private void RecordLoad(Scene scene,LoadSceneMode mode) => loads[scene.name]=loads.TryGetValue(scene.name,out int count) ? count+1 : 1;
        private static IEnumerator WaitFor(Func<bool> predicate)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+10d;
            while (!predicate() && Time.realtimeSinceStartupAsDouble<deadline) yield return null;
            Assert.That(predicate(),Is.True); yield return null;
        }
        private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        private static void SetProfile(string name,object value) => typeof(PersistentPlayerProfile).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,value);
    }

    internal static class PlayerInputTestClock
    {
        public static double StartAfterEditorTransition()
        {
            double time=Time.realtimeSinceStartupAsDouble;
#if UNITY_EDITOR
            object system=typeof(InputSystem).GetField("s_SystemObject",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null);
            if (system!=null)
            {
                foreach (string name in new[] {"exitEditModeTime","enterPlayModeTime"})
                {
                    var field=system.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    if (field!=null) time=Math.Max(time,(double)field.GetValue(system)+1d);
                }
            }
#endif
            return time;
        }
    }
}
