using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class OptionsSafeAreaCompositionTests
    {
        [Test]
        public void SharedPrefab_ProtectsControlsAndKeepsDimmerOutsideSafeArea()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab");
            try { AssertLayout(root.GetComponentInChildren<OptionsMenuManager>(true)); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        [TestCase("Assets/Scenes/MainMenu/MainMenu.unity")]
        [TestCase("Assets/Scenes/ShopMenu/ShopMenu.unity")]
        [TestCase("Assets/Scenes/Game/ZonaEpipelagica.unity")]
        [TestCase("Assets/Scenes/Game/ZonaAbisopelagica.unity")]
        public void RealSceneInstance_PreservesSharedSafeLayout(string path)
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                AssertLayout(scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<OptionsMenuManager>(true)).Single());
            }
            finally
            {
                if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
        [Test]
        public void MainMenuNestedCanvas_MatchesViewportOnMobileAndTracksResize()
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu/MainMenu.unity",OpenSceneMode.Single);
                OptionsMenuManager manager = scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<OptionsMenuManager>(true)).Single();
                Canvas canvas = manager.GetComponentInParent<Canvas>(true);
                Assert.That(canvas.isRootCanvas,Is.False,"Keep coverage of the nested Canvas behind an ordinary Transform.");
                RectTransform viewport = canvas.rootCanvas.transform as RectTransform;
                RectTransform rect = canvas.transform as RectTransform;
                Vector2 authoredSize = rect.sizeDelta;
                MethodInfo apply = typeof(OptionsMenuManager).GetMethod("ApplyPlatformPresentation",BindingFlags.Instance|BindingFlags.NonPublic);
                apply.Invoke(manager,new object[] { false });
                Assert.That(rect.sizeDelta,Is.EqualTo(authoredSize));
                canvas.rootCanvas.renderMode=RenderMode.WorldSpace;
                foreach (Vector2 size in new[] { new Vector2(1920f,1200f),new Vector2(2400f,1080f) })
                {
                    viewport.sizeDelta=size;
                    apply.Invoke(manager,new object[] { true });
                    Assert.That(rect.rect.size,Is.EqualTo(viewport.rect.size));
                    Assert.That(Vector3.Distance(rect.TransformPoint(rect.rect.center),viewport.TransformPoint(viewport.rect.center)),Is.LessThan(0.01f));
                }
            }
            finally
            {
                if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
        private static void AssertLayout(OptionsMenuManager manager)
        {
            var serialized = new SerializedObject(manager);
            GameObject panel = serialized.FindProperty("menuRoot").objectReferenceValue as GameObject;
            GameObject background = serialized.FindProperty("background").objectReferenceValue as GameObject;
            RectTransform safe = panel.transform.Find("OptionsSafeArea") as RectTransform;
            Assert.That(safe,Is.Not.Null);
            RectTransform content = safe.Find("OptionsContent") as RectTransform;
            Assert.That(content,Is.Not.Null);
            Assert.That(safe.GetComponent<SafeAreaAdapter>().Target,Is.SameAs(safe));
            Assert.That(new SerializedObject(content.GetComponent<MobileReferenceContentFitter>())
                .FindProperty("referenceResolution").vector2Value,Is.EqualTo(new Vector2(1920f,1080f)));
            Assert.That(background.transform.parent,Is.SameAs(panel.transform));
            Assert.That(background.transform.GetSiblingIndex(),Is.Zero);
            Assert.That(background.GetComponent<Image>().raycastTarget,Is.True);
            foreach (string field in new[] { "volumeSlider","resolutionDropdown","fullscreenToggle","backButton" })
            {
                Component control = serialized.FindProperty(field).objectReferenceValue as Component;
                Assert.That(control.transform.IsChildOf(content),Is.True,field);
            }
            foreach (RectTransform rect in new[] { safe,content })
            {
                Assert.That(rect.anchorMin,Is.EqualTo(Vector2.zero));
                Assert.That(rect.anchorMax,Is.EqualTo(Vector2.one));
                Assert.That(rect.offsetMin,Is.EqualTo(Vector2.zero));
                Assert.That(rect.offsetMax,Is.EqualTo(Vector2.zero));
                Assert.That(rect.localScale,Is.EqualTo(Vector3.one));
                Assert.That(rect.GetComponent<Canvas>(),Is.Null);
                Assert.That(rect.GetComponent<GraphicRaycaster>(),Is.Null);
            }
            Assert.That(manager.GetComponentsInChildren<SafeAreaAdapter>(true),Has.Length.EqualTo(1));
            Canvas canvas = manager.GetComponentInParent<Canvas>(true);
            Assert.That(canvas.GetComponentsInChildren<Canvas>(true),Has.Length.EqualTo(1));
            Assert.That(canvas.GetComponentsInChildren<GraphicRaycaster>(true),Has.Length.EqualTo(1));
            Assert.That(canvas.GetComponentsInChildren<EventSystem>(true),Is.Empty);
        }
    }
}
