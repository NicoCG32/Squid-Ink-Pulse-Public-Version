using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class PermanentShopMobileCompositionTests
    {
        [Test]
        public void Shop_ProtectsForegroundAndCounterWithoutAddingCanvasOrInputOwners()
        {
            SceneSetup[] setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene=EditorSceneManager.OpenScene("Assets/Scenes/ShopMenu/ShopMenu.unity",OpenSceneMode.Single);
                OutOfGameShopManager shop=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OutOfGameShopManager>(true)).Single();
                Transform panel=shop.transform.Find("Panel");
                RectTransform safe=panel.Find("ShopSafeArea") as RectTransform;
                Transform content=safe.Find("ShopContent");
                Assert.That(safe.GetComponent<SafeAreaAdapter>().Target,Is.SameAs(safe));
                Assert.That(content.GetComponent<MobileReferenceContentFitter>(),Is.Not.Null);
                Assert.That(panel.Find("Fondo").GetComponent<MobileBackgroundCover>(),Is.Not.Null);
                Assert.That(panel.Find("Fondo").GetSiblingIndex(),Is.Zero);
                foreach (Button button in shop.GetComponentsInChildren<Button>(true)) Assert.That(button.transform.IsChildOf(content),Is.True);
                Assert.That(content.Find("ShrimpCounter"),Is.Not.Null);
                Assert.That(shop.GetComponentsInChildren<Canvas>(true),Has.Length.EqualTo(1));
                Assert.That(shop.GetComponentsInChildren<GraphicRaycaster>(true),Has.Length.EqualTo(1));
                var navigation=new SerializedObject(shop.GetComponent<ShopMenuCancelNavigation>());
                foreach (string field in new[] { "menu","options","inputModule" })
                    Assert.That(navigation.FindProperty(field).objectReferenceValue,Is.Not.Null,field);
                var serializedShop=new SerializedObject(shop);
                GameObject feedback=serializedShop.FindProperty("mobilePurchaseFeedbackRoot").objectReferenceValue as GameObject;
                Assert.That(feedback,Is.Not.Null);
                Assert.That(feedback.activeSelf,Is.False,"Authoring and desktop keep the new mobile feedback hidden.");
                Assert.That(feedback.transform.IsChildOf(content),Is.True);
                Assert.That(serializedShop.FindProperty("selectedItemStateText").objectReferenceValue,Is.SameAs(feedback.GetComponent<TMPro.TMP_Text>()));
            }
            finally
            {
                if (setup.Any(s=>s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
