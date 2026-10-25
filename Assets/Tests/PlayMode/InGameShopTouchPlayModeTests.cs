using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class InGameShopTouchPlayModeTests : InputTestFixture
    {
        private Touchscreen screen;
        private Keyboard keyboard;
        private int pointer;
        private readonly Dictionary<FieldInfo,object> profileState=new();
        private UnityEngine.Random.State randomState;
        public override void Setup()
        {
            base.Setup();
            if (!Application.isEditor) Assert.Ignore("Transient Editor saves required.");
            Assert.That(PlayerProfileRepository.UsesTransientEditorPlayModeProfileSaves,Is.True);
            currentTime=Time.realtimeSinceStartupAsDouble;
            screen=InputSystem.AddDevice<Touchscreen>(); keyboard=InputSystem.AddDevice<Keyboard>(); pointer=0;
            randomState=UnityEngine.Random.state; Time.timeScale=1f;
            foreach (string name in new[] { "currentProfile","currentRecords","currentCatalog","loaded" })
            {
                FieldInfo field=typeof(PersistentPlayerProfile).GetField(name,BindingFlags.Static|BindingFlags.NonPublic);
                profileState[field]=field.GetValue(null);
            }
            var provider=new ResourcesJsonSeedProvider(PersistentDbPaths.SeedResourcesDirectoryName);
            Assert.That(provider.TryGetSeedText(PersistentDbPaths.UnlockablesCatalogFileName,out string seed),Is.True);
            var catalog=JsonUtility.FromJson<UnlockablesCatalogSaveData>(seed); catalog.Normalize();
            var records=PlayerRecordsSaveData.CreateDefault(); records.totalShrimps=2_000_000;
            records.bestScore=1_000_000; records.totalRuns=1000; records.totalPortalsCrossed=1000; records.totalShrimpsCollected=2_000_000;
            SetProfile("currentProfile",PlayerProfileSaveData.CreateDefault()); SetProfile("currentRecords",records);
            SetProfile("currentCatalog",catalog); SetProfile("loaded",true);
            ShrimpRuntimeWallet.ResetForRuntime(); SceneFlowController.ResetRunScopedRuntimeState();
        }
        public override void TearDown()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            foreach (var entry in profileState) entry.Key.SetValue(null,entry.Value);
            profileState.Clear();
            typeof(ShrimpRuntimeWallet).GetField("initialized",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
            SceneFlowController.ResetRunScopedRuntimeState(); UnityEngine.Random.state=randomState; Time.timeScale=1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Touch_RejectsWithoutFundsExpiresAndBuysWithoutReplayingInkPulseInBothZones()
        {
            foreach (string zone in new[] { "ZonaEpipelagica","ZonaAbisopelagica" })
            {
                yield return Load(zone);
                InGameShopManager shop=InGameShopManager.Instance;
                GameObject root=Field<GameObject>(shop,"menuRoot");
                InkPulseController pulse=Object.FindFirstObjectByType<InkPulseController>();
                pulse.ForceEmptyCharge(); Assert.That(pulse.TryForceReady(),Is.True);
                Assert.That(ShrimpRuntimeWallet.TrySpend(PersistentPlayerProfile.TotalShrimps),Is.True);
                Set(shop,"offerDurationSeconds",1.5f);
                shop.QueueTutorialOffer(GadgetId.ShellShield,7);
                Assert.That(shop.TryOpenTimedShop(),Is.True);
                yield return null;
                ApplyMobile(root);
                Assert.That(Mouse.current,Is.Null);
                Assert.That(GameSessionController.Instance.IsPlaying,Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                Assert.That(InGameShopManager.BlocksInkPulseActivation,Is.True);
                var controls=Object.FindFirstObjectByType<TouchGameplayControlsController>(); controls.RefreshPresentation();
                Assert.That(controls.InkPulseButton.IsInteractable(),Is.False);
                // The full-screen dimmer must own a point outside every control.
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=new Vector2(2f,2f)},hits);
                Assert.That(hits.First().gameObject.name,Is.EqualTo("FondoBurbuja"));
                TMP_Text timer=Field<TMP_Text>(shop,"timerText");
                Assert.That(timer.gameObject.activeInHierarchy,Is.True);
                float before=Field<InGameShopOfferTimer>(shop,"offerTimer").RemainingSeconds;
                yield return Tap(Field<Button>(shop,"buyButton"));
                Assert.That(Field<TMP_Text>(shop,"insufficientFundsText").gameObject.activeInHierarchy,Is.True,
                    "Insufficient funds must remain visible after touch selects/highlights the button.");
                Assert.That(PersistentPlayerProfile.TotalShrimps,Is.Zero);
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield),Is.False);
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(Field<InGameShopOfferTimer>(shop,"offerTimer").RemainingSeconds,Is.LessThan(before));
                Assert.That(Time.timeScale,Is.Zero);
                yield return WaitFor(()=>!InGameShopManager.IsShopOpen);
                Assert.That(Time.timeScale,Is.EqualTo(1f));
                Assert.That(root.activeSelf,Is.False);
                Assert.That(pulse.IsCharged && !pulse.IsPulseActive,Is.True);
                yield return WaitFor(()=>!InGameShopManager.BlocksInkPulseActivation);
                yield return AssertSteering("expiry");

                ShrimpRuntimeWallet.Refund(100); Set(shop,"offerDurationSeconds",5f);
                shop.QueueTutorialOffer(GadgetId.ShellShield,7); Assert.That(shop.TryOpenTimedShop(),Is.True);
                yield return null; ApplyMobile(root);
                int purchases=0, closedFrame=-1, pulseStarts=0;
                bool blockedInClose=false, activatedInClose=true;
                shop.onGadgetPurchased.AddListener(_=>purchases++);
                shop.onShopClosed.AddListener(()=>
                {
                    closedFrame=Time.frameCount; blockedInClose=InGameShopManager.BlocksInkPulseActivation;
                    activatedInClose=pulse.TryActivatePulse();
                });
                pulse.PulseStarted+=()=>pulseStarts++;
                yield return Tap(Field<Button>(shop,"buyButton"));
                Assert.That(purchases,Is.EqualTo(1));
                Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(93));
                Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.ShellShield),Is.True);
                Assert.That(blockedInClose,Is.True,"Close must keep its frame barrier after restoring time.");
                Assert.That(activatedInClose,Is.False);
                Assert.That(InGameShopManager.IsShopOpen,Is.False);
                Assert.That(Time.timeScale,Is.EqualTo(1f));
                while (Time.frameCount<=closedFrame+1) yield return null;
                Assert.That(InGameShopManager.BlocksInkPulseActivation,Is.False);
                Assert.That(pulseStarts,Is.Zero,"The purchase touch must not replay as Ink-Pulse.");
                Assert.That(pulse.IsCharged,Is.True);
                yield return AssertSteering("purchase");
                controls.RefreshPresentation();
                yield return Tap(controls.InkPulseButton);
                Assert.That(pulseStarts,Is.EqualTo(1),"A later deliberate command must still work.");
            }
        }

        [UnityTest]
        public IEnumerator Desktop_KeepsKeyboardHintsAndPurchasesWithB()
        {
            yield return Load("ZonaEpipelagica");
            InGameShopManager shop=InGameShopManager.Instance;
            shop.QueueTutorialOffer(GadgetId.InkBottle,7); Assert.That(shop.TryOpenTimedShop(),Is.True);
            yield return null;
            var presentation=Field<GameObject>(shop,"menuRoot").GetComponent<InGameShopMobilePresentation>();
            foreach (GameObject hint in Field<GameObject[]>(presentation,"keyboardHints")) Assert.That(hint.activeSelf,Is.True);
            Assert.That(Field<GameObject>(presentation,"mobileBuyLabel").activeSelf,Is.False);
            Assert.That(Field<GameObject>(presentation,"mobileTimerRoot").activeSelf,Is.False);
            int before=PersistentPlayerProfile.TotalShrimps;
            currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            Press(keyboard.bKey); yield return null; Release(keyboard.bKey); yield return null;
            Assert.That(InGameShopManager.IsShopOpen,Is.False);
            Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(before-7));
            Assert.That(RuntimeGadgetInventory.HasGadget(GadgetId.InkBottle),Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1f));
        }

        private static IEnumerator Load(string zone)
        {
            SceneFlowController.ResetRunScopedRuntimeState();
            yield return SceneManager.LoadSceneAsync($"Assets/Scenes/Game/{zone}.unity",LoadSceneMode.Single);
            yield return null;
            Object.FindFirstObjectByType<PlayerMovement>().SetMovementSuppressed(true);
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour.GetType().Name.EndsWith("Spawner") || behaviour is RunProgressionDirector
                    || behaviour.GetType().Name=="BossEventDirector") behaviour.enabled=false;
            Object.FindFirstObjectByType<TouchControlsVisibilityController>(FindObjectsInactive.Include)
                .transform.Find("TouchControls").gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.1f);
        }
        private static void ApplyMobile(GameObject root)
        {
            root.GetComponent<InGameShopMobilePresentation>().Apply(true);
            foreach (SafeAreaAdapter adapter in root.GetComponentsInChildren<SafeAreaAdapter>(true)) adapter.Refresh();
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in root.GetComponentsInChildren<MobileReferenceContentFitter>(true))
                fitter.Apply((fitter.transform.parent as RectTransform).rect.size,true);
            Canvas.ForceUpdateCanvases();
        }
        private IEnumerator Tap(Button button)
        {
            Assert.That(button.IsInteractable() && button.gameObject.activeInHierarchy,Is.True,button.transform.parent.name);
            Canvas.ForceUpdateCanvases(); Canvas canvas=button.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransform rect=(RectTransform)button.transform;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=point},hits);
            Assert.That(hits.FirstOrDefault().gameObject?.GetComponentInParent<Button>(),Is.SameAs(button));
            int id=++pointer; currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            // Queue into the player loop; an extra manual InputSystem.Update can
            // reset pointer edge state before the scene's EventSystem processes it.
            BeginTouch(id,point,queueEventOnly:true,screen:screen,time:currentTime);
            yield return WaitForTouch(id,true);
            currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            EndTouch(id,point,queueEventOnly:true,screen:screen,time:currentTime);
            yield return WaitForTouch(id,false);
        }
        private IEnumerator AssertSteering(string phase)
        {
            Vector2 point=new(Screen.width*0.5f,Screen.height*0.5f); int id=++pointer;
            currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
            BeginTouch(id,point,queueEventOnly:true,screen:screen,time:currentTime);
            yield return WaitForTouch(id,true);
            try
            {
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=point},hits);
                var surface=Object.FindFirstObjectByType<TouchSteeringSurface>(FindObjectsInactive.Include);
                bool overlayAllowed=Field<bool>(surface,"overlayInteractionAllowed");
                string targets=string.Join(",",hits.Select(hit=>hit.gameObject.name));
                if (!SquidInkPulseInputRuntime.Gameplay.HasSteerPosition)
                {
                    var module=EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
                    Debug.Log($"SHOP TOUCH DIAGNOSTIC: {phase}; primary={screen.primaryTouch.touchId.ReadValue()}/{screen.primaryTouch.phase.ReadValue()}/{screen.primaryTouch.press.isPressed}; device={screen.enabled}/{screen.lastUpdateTime}; clock={currentTime}; ui={module.isActiveAndEnabled}/{module.leftClick.action.enabled}/{module.leftClick.action.ReadValue<float>()}/{module.leftClick.action.activeControl?.path}; focus={EventSystem.current.isFocused}");
                }
                Assert.That(SquidInkPulseInputRuntime.Gameplay.HasSteerPosition,Is.True,
                    $"phase={phase}; scene={SceneManager.GetActiveScene().name}; time={Time.timeScale}; playing={GameSessionController.Instance.IsPlaying}; shopBlock={InGameShopManager.BlocksInkPulseActivation}; reader={SquidInkPulseInputRuntime.Gameplay.IsEnabled}; surface={surface.gameObject.activeInHierarchy}/{surface.HasActivePointer}; overlay={overlayAllowed}; hits={targets}");
            }
            finally
            {
                currentTime=Math.Max(currentTime,Time.realtimeSinceStartupAsDouble);
                EndTouch(id,point,queueEventOnly:true,screen:screen,time:currentTime);
            }
            yield return WaitForTouch(id,false);
        }
        private IEnumerator WaitForTouch(int id,bool pressed)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+2d;
            while ((screen.primaryTouch.touchId.ReadValue()!=id || screen.primaryTouch.press.isPressed!=pressed)
                && Time.realtimeSinceStartupAsDouble<deadline)
            {
                // The mock clock must move beyond the queued event, and the
                // device must consume it before asserting the EventSystem result.
                currentTime=Math.Max(currentTime+0.001d,Time.realtimeSinceStartupAsDouble);
                yield return null;
            }
            Assert.That(screen.primaryTouch.touchId.ReadValue(),Is.EqualTo(id),"Queued touch was not delivered.");
            Assert.That(screen.primaryTouch.press.isPressed,Is.EqualTo(pressed));
            yield return null;
        }
        private static IEnumerator WaitFor(Func<bool> predicate)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+6d;
            while (!predicate() && Time.realtimeSinceStartupAsDouble<deadline) yield return null;
            Assert.That(predicate(),Is.True); yield return null;
        }
        private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static void SetProfile(string name,object value) => typeof(PersistentPlayerProfile).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,value);
    }
}
