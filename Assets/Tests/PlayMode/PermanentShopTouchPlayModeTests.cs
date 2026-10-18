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

namespace SquidInkPulse.Tests.PlayMode
{
    public sealed class PermanentShopTouchPlayModeTests : InputTestFixture
    {
        private Touchscreen screen;
        private Keyboard keyboard;
        private int pointerId;
        private readonly Dictionary<FieldInfo,object> profileState=new();

        public override void Setup()
        {
            base.Setup();
            if (!Application.isEditor) Assert.Ignore("This fixture relies on transient Editor profile saves.");
            Assert.That(PlayerProfileRepository.UsesTransientEditorPlayModeProfileSaves,Is.True);
            currentTime=Time.realtimeSinceStartupAsDouble;
            screen=InputSystem.AddDevice<Touchscreen>(); keyboard=InputSystem.AddDevice<Keyboard>();
            Time.timeScale=1f;
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
            SetProfile("currentProfile",PlayerProfileSaveData.CreateDefault());
            SetProfile("currentRecords",records); SetProfile("currentCatalog",catalog); SetProfile("loaded",true);
            ShrimpRuntimeWallet.ResetForRuntime();
        }

        public override void TearDown()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            foreach (var entry in profileState) entry.Key.SetValue(null,entry.Value);
            profileState.Clear();
            // Re-sync the wallet with the restored profile without loading any data.
            typeof(ShrimpRuntimeWallet).GetField("initialized",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
            Time.timeScale=1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Touch_BuysUpgradePagesSkinsEquipsAndReturnsWithContextualCancel()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/ShopMenu/ShopMenu.unity",LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(Mouse.current,Is.Null);
            OutOfGameShopManager shop=Object.FindFirstObjectByType<OutOfGameShopManager>();
            ShopMenuCancelNavigation navigation=shop.GetComponent<ShopMenuCancelNavigation>();
            Assert.That(navigation,Is.Not.Null);
            typeof(ShopMenuCancelNavigation).GetMethod("BindForPlatform",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(navigation,new object[] { true });
            var purchase=Field<Button>(shop,"purchaseButton");
            Assert.That(purchase.interactable,Is.False,"No action before selection.");
            int initialBalance=PersistentPlayerProfile.TotalShrimps;
            int upgradePrice=PermanentShopService.GetPermanentUpgradePrice(PlayerUnlockableIds.InkPulseDurationUpgrade);
            yield return Tap(Field<Button[]>(shop,"upgradeSlotButtons")[0]);
            yield return Tap(purchase);
            Assert.That(PersistentPlayerProfile.GetInkPulseDurationLevel(),Is.EqualTo(1));
            Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(initialBalance-upgradePrice));
            Assert.That(Field<GameObject>(shop,"happyShopVisualState").activeSelf,Is.True);

            yield return Tap(Field<Button>(shop,"nextSkinPageButton"));
            Assert.That(Field<Button>(shop,"nextSkinPageButton").interactable,Is.False);
            Assert.That(purchase.interactable,Is.False,"Changing page clears the prior purchase target.");
            yield return Tap(Field<Button>(shop,"previousSkinPageButton"));
            Assert.That(Field<Button>(shop,"previousSkinPageButton").interactable,Is.False);
            UnlockableSkinDefinition skin=PersistentPlayerProfile.UnlockablesCatalog.skins.First(s=>!string.IsNullOrWhiteSpace(s.shopSpriteResourcePath));
            int beforeSkin=PersistentPlayerProfile.TotalShrimps;
            yield return Tap(Field<Button[]>(shop,"skinSlotButtons")[0]);
            yield return Tap(purchase);
            Assert.That(PersistentPlayerProfile.HasUnlockedSkin(skin.id),Is.True);
            Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(beforeSkin-skin.basePrice));
            Assert.That(PersistentPlayerProfile.EquippedSkinId,Is.EqualTo(PlayerSkinIds.Default),"Purchase does not implicitly equip.");
            yield return Tap(purchase);
            Assert.That(PersistentPlayerProfile.EquippedSkinId,Is.EqualTo(skin.id));
            yield return Tap(purchase);
            Assert.That(PersistentPlayerProfile.EquippedSkinId,Is.EqualTo(PlayerSkinIds.Default));
            Assert.That(PersistentPlayerProfile.TotalShrimps,Is.EqualTo(beforeSkin-skin.basePrice),"Equip and remove must not spend again.");

            PersistentPlayerProfile.Records.totalShrimps=0;
            yield return Tap(Field<Button[]>(shop,"upgradeSlotButtons")[1]);
            yield return Tap(purchase);
            Assert.That(Field<string>(shop,"purchaseResultMessage"),Is.EqualTo("SIN CAMARONES"));
            Assert.That(PersistentPlayerProfile.GetInkPulseRechargeRateLevel(),Is.Zero);
            // A pending catalogue entry has no shop sprite; the existing pager must keep it out.
            var pending=new UnlockableSkinDefinition { id="skin.pending",displayName="Pending" };
            PersistentPlayerProfile.UnlockablesCatalog.skins=PersistentPlayerProfile.UnlockablesCatalog.skins.Concat(new[] { pending }).ToArray();
            shop.Refresh();
            Assert.That(Field<PermanentShopSkinPager>(shop,"skinPager").VisibleSkinCount,Is.EqualTo(8));

            yield return Tap(Find("OpcionesBoton"));
            yield return new WaitForSecondsRealtime(0.4f);
            OptionsMenuManager options=Object.FindFirstObjectByType<OptionsMenuManager>(FindObjectsInactive.Include);
            Assert.That(options.IsOpen,Is.True);
            yield return Cancel();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(options.IsOpen,Is.False);
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("ShopMenu"),"Cancel closes options first.");
            yield return Cancel();
            double deadline=Time.realtimeSinceStartupAsDouble+10d;
            while (SceneManager.GetActiveScene().name!="MainMenu" && Time.realtimeSinceStartupAsDouble<deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
        }

        private IEnumerator Cancel()
        {
            Press(keyboard.escapeKey); yield return null;
            Release(keyboard.escapeKey); yield return null;
        }

        private IEnumerator Tap(Button button)
        {
            Assert.That(button.interactable && button.gameObject.activeInHierarchy,Is.True,button.transform.parent.name);
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)button.transform;
            Canvas canvas=button.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position=point },hits);
            Assert.That(hits.FirstOrDefault().gameObject?.GetComponentInParent<Button>(),Is.SameAs(button),button.transform.parent.name);
            int id=++pointerId;
            BeginTouch(id,point,screen:screen); yield return null;
            var visual=button.GetComponent<ButtonVisualState>();
            if (visual?.PressedState!=null) Assert.That(visual.PressedState.activeSelf,Is.True,"Pressed feedback without hover.");
            EndTouch(id,point,screen:screen); yield return null;
        }
        private static Button Find(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==name).GetComponentInChildren<Button>(true);
        private static T Field<T>(object instance,string name) => (T)instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(instance);
        private static void SetProfile(string name,object value) => typeof(PersistentPlayerProfile).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,value);
    }
}
