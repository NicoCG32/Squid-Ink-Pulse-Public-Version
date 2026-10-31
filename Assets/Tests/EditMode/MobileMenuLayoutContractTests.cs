using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileMenuLayoutContractTests
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
        private const string ShopMenuScenePath = "Assets/Scenes/ShopMenu/ShopMenu.unity";

        [Test]
        public void MainMenu_SafeAreaProtectsForegroundWithoutAddingInputOrCanvasOwners()
        {
            WithScene(MainMenuScenePath, scene =>
            {
                Transform panel = Find(scene, "Panel");
                Transform navigation = Find(scene, "NavigationSafeArea");
                Transform brand = Find(scene, "BrandSafeArea");
                Transform counter = Find(scene, "CounterSafeArea");
                Assert.That(navigation.parent, Is.SameAs(panel));
                Assert.That(brand.parent, Is.SameAs(panel));
                Assert.That(Find(scene, "Fondo").parent, Is.SameAs(panel));
                Assert.That(Find(scene, "Character").parent, Is.SameAs(panel));
                Assert.That(Find(scene, "Logo").parent, Is.SameAs(brand));
                Assert.That(Find(scene, "ShrimpCounter").parent, Is.SameAs(counter));
                string[] buttons = { "JugarBoton", "ComoJugarBoton", "OpcionesBoton", "SalirBoton", "TiendaBoton", "BotonLog" };
                Canvas.ForceUpdateCanvases();
                foreach (string name in buttons)
                {
                    Transform control = Find(scene, name);
                    Assert.That(control.parent, Is.SameAs(navigation), name);
                    // BotonLog is the legacy fair placeholder, not a gameplay button.
                    if (name == "BotonLog")
                        continue;
                    Button button = control.GetComponentInChildren<Button>(true);
                    Assert.That(button, Is.Not.Null, name);
                    RectTransform hit = button.transform as RectTransform;
                    Assert.That(hit.rect.width, Is.GreaterThanOrEqualTo(64f), name);
                    Assert.That(hit.rect.height, Is.GreaterThanOrEqualTo(64f), name);
                    Assert.That(button.onClick.GetPersistentEventCount(), Is.GreaterThan(0), name);
                }
                foreach (Transform root in new[] { navigation, brand, counter })
                {
                    RectTransform rect = root as RectTransform;
                    SafeAreaAdapter adapter = root.GetComponent<SafeAreaAdapter>();
                    Assert.That(adapter, Is.Not.Null, root.name);
                    Assert.That(adapter.Target, Is.SameAs(rect));
                    Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
                    Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
                    Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
                    Assert.That(root.GetComponent<Canvas>(), Is.Null);
                    Assert.That(root.GetComponent<GraphicRaycaster>(), Is.Null);
                    Assert.That(root.GetComponent<UnityEngine.EventSystems.EventSystem>(), Is.Null);
                }
                Assert.That(navigation.GetSiblingIndex(), Is.LessThan(Find(scene, "Character").GetSiblingIndex()));
                Assert.That(brand.GetSiblingIndex(), Is.GreaterThan(Find(scene, "Character").GetSiblingIndex()));
                Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SafeAreaAdapter>(true))
                    .Count(adapter => adapter.GetComponentInParent<OptionsMenuManager>(true) == null
                        && adapter.GetComponentInParent<LoreComicPresenter>(true) == null), Is.EqualTo(4));
            });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MainMenu_ExitPresentationUsesPlatformPolicy(bool isMobile)
        {
            WithScene(MainMenuScenePath, scene =>
            {
                MainMenu menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenu>(true)).Single();
                SerializedObject serialized = new(menu);
                GameObject exit = serialized.FindProperty("exitButtonRoot").objectReferenceValue as GameObject;
                Assert.That(exit, Is.SameAs(Find(scene, "SalirBoton").gameObject));
                RectTransform tutorial = serialized.FindProperty("tutorialButtonRoot").objectReferenceValue as RectTransform;
                Assert.That(tutorial, Is.SameAs(Find(scene, "ComoJugarBoton")));
                Vector2 authoredAnchor = tutorial.anchorMin;
                Vector2 authoredPosition = tutorial.anchoredPosition;
                RectTransform logo = serialized.FindProperty("logoRoot").objectReferenceValue as RectTransform;
                RectTransform counter = serialized.FindProperty("counterRoot").objectReferenceValue as RectTransform;
                Assert.That(logo, Is.SameAs(Find(scene, "Logo")));
                Assert.That(counter, Is.SameAs(Find(scene, "ShrimpCounter")));
                Vector2 logoAnchor = logo.anchorMin, logoPosition = logo.anchoredPosition;
                Vector2 counterAnchor = counter.anchorMin, counterPosition = counter.anchoredPosition;
                typeof(MainMenu).GetMethod("ApplyPlatformPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(menu, new object[] { isMobile });
                Assert.That(exit.activeSelf, Is.EqualTo(!isMobile));
                Assert.That(Find(scene, "JugarBoton").gameObject.activeSelf, Is.True);
                Assert.That(Find(scene, "OpcionesBoton").gameObject.activeSelf, Is.True);
                if (!isMobile)
                {
                    Assert.That(tutorial.anchorMin, Is.EqualTo(authoredAnchor));
                    Assert.That(tutorial.anchoredPosition, Is.EqualTo(authoredPosition));
                    Assert.That(logo.anchorMin, Is.EqualTo(logoAnchor));
                    Assert.That(logo.anchoredPosition, Is.EqualTo(logoPosition));
                    Assert.That(counter.anchorMin, Is.EqualTo(counterAnchor));
                    Assert.That(counter.anchoredPosition, Is.EqualTo(counterPosition));
                }
                else
                {
                    Assert.That(tutorial.anchorMin.x, Is.Zero);
                    Vector2 mobilePosition = tutorial.anchoredPosition;
                    Assert.That(logo.anchorMin.x, Is.EqualTo(1f));
                    Assert.That(counter.anchorMin.x, Is.EqualTo(1f));
                    Vector2 mobileLogoPosition = logo.anchoredPosition, mobileCounterPosition = counter.anchoredPosition;
                    typeof(MainMenu).GetMethod("ApplyPlatformPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(menu, new object[] { true });
                    Assert.That(tutorial.anchoredPosition, Is.EqualTo(mobilePosition), "Repeated presentation must not accumulate a shift.");
                    Assert.That(logo.anchoredPosition, Is.EqualTo(mobileLogoPosition));
                    Assert.That(counter.anchoredPosition, Is.EqualTo(mobileCounterPosition));
                }
            });
        }

        [Test]
        public void MainMenu_TutorialCanvasesPreserveReferenceHeightOnExtraWideScreens()
        {
            WithScene(MainMenuScenePath, scene =>
            {
                Transform tutorial = Find(scene, "ComicsTutorial");
                Assert.That(tutorial, Is.Not.Null);

                CanvasScaler[] scalers = tutorial.GetComponentsInChildren<CanvasScaler>(true);
                Assert.That(scalers, Has.Length.EqualTo(4));
                foreach (CanvasScaler scaler in scalers)
                {
                    Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                    Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
                    Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
                    Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f), scaler.name);
                }

                RectTransform[] vignettes = tutorial.GetComponentsInChildren<RectTransform>(true)
                    .Where(rect => rect.name == "Vineta")
                    .ToArray();
                Assert.That(vignettes, Has.Length.EqualTo(3));
                foreach (RectTransform vignette in vignettes)
                {
                    Assert.That(vignette.sizeDelta, Is.EqualTo(new Vector2(1280f, 720f)));
                    Assert.That(vignette.GetComponent<Image>().preserveAspect, Is.True);
                }
                Button next = tutorial.GetComponentsInChildren<Button>(true).Single(button => button.name == "Next");
                Assert.That(next.targetGraphic.raycastTarget, Is.True);
            });
        }

        [Test]
        public void MainMenu_TutorialProtectsPagesAndNextInsideSharedSafeContent()
        {
            WithScene(MainMenuScenePath, scene =>
            {
                Transform tutorial = Find(scene, "ComicsTutorial");
                RectTransform safe = tutorial.Find("TutorialSafeArea") as RectTransform;
                RectTransform content = safe.Find("TutorialContent") as RectTransform;
                Assert.That(tutorial.Find("BlackLayer"), Is.Not.Null, "The dimmer must stay full bleed.");
                Assert.That(safe.GetComponent<SafeAreaAdapter>().Target, Is.SameAs(safe));
                Assert.That(content.GetComponent<MobileReferenceContentFitter>(), Is.Not.Null);
                SerializedObject fitter = new(content.GetComponent<MobileReferenceContentFitter>());
                Assert.That(fitter.FindProperty("referenceResolution").vector2Value, Is.EqualTo(new Vector2(1920f, 1080f)));
                foreach (RectTransform root in new[] { safe, content })
                {
                    Assert.That(root.anchorMin, Is.EqualTo(Vector2.zero));
                    Assert.That(root.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(root.offsetMin, Is.EqualTo(Vector2.zero));
                    Assert.That(root.offsetMax, Is.EqualTo(Vector2.zero));
                    Assert.That(root.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(root.GetComponent<Canvas>(), Is.Null);
                    Assert.That(root.GetComponent<GraphicRaycaster>(), Is.Null);
                }
                MainMenu menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenu>(true)).Single();
                SerializedObject serialized = new(menu);
                SerializedProperty pages = serialized.FindProperty("tutorialComicPages");
                for (int i = 0; i < pages.arraySize; i++)
                {
                    GameObject page = pages.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue as GameObject;
                    Assert.That(page.transform.parent, Is.SameAs(content));
                }
                Button next = serialized.FindProperty("tutorialNextButton").objectReferenceValue as Button;
                Assert.That(next.transform.parent, Is.SameAs(content));
            });
        }

        [Test]
        public void MainMenu_MobileInstructionsDescribeTouchAndDesktopCopyIsPreserved()
        {
            WithScene(MainMenuScenePath,scene =>
            {
                MainMenu menu = scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MainMenu>(true)).Single();
                TMPro.TMP_Text[] descriptions = Find(scene,"ComicsTutorial").GetComponentsInChildren<TMPro.TMP_Text>(true);
                string[] authored = descriptions.Select(text=>text.text).ToArray();
                MethodInfo apply = typeof(MainMenu).GetMethod("ApplyPlatformPresentation",BindingFlags.Instance|BindingFlags.NonPublic);
                apply.Invoke(menu,new object[] { true });
                Assert.That(descriptions.Count(text=>text.text.Contains("INK-PULSO")),Is.EqualTo(2));
                Assert.That(descriptions.Any(text=>text.text.StartsWith("Arrastra")),Is.True);
                apply.Invoke(menu,new object[] { false });
                Assert.That(descriptions.Select(text=>text.text).ToArray(),Is.EqualTo(authored));
            });
        }

        [Test]
        public void MainMenu_TutorialSharesMobileScaleAndReturnsToHeightOnPhone()
        {
            WithScene(MainMenuScenePath, scene =>
            {
                MainMenu menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenu>(true)).Single();
                CanvasScaler[] tutorialScalers = Find(scene, "ComicsTutorial").GetComponentsInChildren<CanvasScaler>(true);
                MethodInfo apply = typeof(MainMenu).GetMethod("ApplyCanvasScaling", BindingFlags.Instance | BindingFlags.NonPublic);
                apply.Invoke(menu, new object[] { new Vector2(2560f, 1600f), false });
                foreach (CanvasScaler scaler in tutorialScalers)
                    Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f), "Desktop preserves authored scaling.");
                apply.Invoke(menu, new object[] { new Vector2(2560f, 1600f), true });
                foreach (CanvasScaler scaler in tutorialScalers)
                    Assert.That(scaler.matchWidthOrHeight, Is.Zero, scaler.name);
                apply.Invoke(menu, new object[] { new Vector2(2400f, 1080f), true });
                foreach (CanvasScaler scaler in tutorialScalers)
                    Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f), scaler.name);
            });
        }

        [Test]
        public void MainMenu_DecorativeCharacterKeepsThreeQuartersInsideTheRightEdge()
        {
            WithScene(MainMenuScenePath, scene =>
            {
                RectTransform character = Find(scene, "Character") as RectTransform;
                Assert.That(character, Is.Not.Null);
                Assert.That(character.anchorMin, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(character.anchorMax, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(character.pivot, Is.EqualTo(new Vector2(0.75f, 0.5f)));
                Assert.That(character.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(character.GetComponent<Image>().preserveAspect, Is.True);
            });
        }

        [Test]
        public void ShopMenu_CategorySignsKeepTheirAspectAndRightEdgeMargins()
        {
            WithScene(ShopMenuScenePath, scene =>
            {
                ValidateShopSign(Find(scene, "LetreroMejoras") as RectTransform);
                ValidateShopSign(Find(scene, "LetreroAspectos") as RectTransform);
            });
        }

        private static void ValidateShopSign(RectTransform sign)
        {
            Assert.That(sign, Is.Not.Null);
            Assert.That(sign.anchorMin, Is.EqualTo(new Vector2(1f, 0.5f)));
            Assert.That(sign.anchorMax, Is.EqualTo(new Vector2(1f, 0.5f)));
            Assert.That(sign.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(sign.sizeDelta, Is.EqualTo(new Vector2(300f, 100f)));
            Assert.That(sign.GetComponent<Image>().preserveAspect, Is.True);

            float radians = sign.localEulerAngles.z * Mathf.Deg2Rad;
            float horizontalExtent =
                (Mathf.Abs(Mathf.Cos(radians)) * sign.sizeDelta.x * 0.5f)
                + (Mathf.Abs(Mathf.Sin(radians)) * sign.sizeDelta.y * 0.5f);
            Assert.That(sign.anchoredPosition.x + horizontalExtent, Is.LessThanOrEqualTo(0f),
                $"{sign.name} debe permanecer dentro del borde derecho en cualquier ancho landscape.");
        }

        private static void WithScene(string path, System.Action<Scene> assertion)
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                assertion(scene);
            }
            finally
            {
                if (previousSetup.Any(setup => setup.isLoaded && setup.isActive))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }
        }

        private static Transform Find(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(transform => transform.name == objectName);
        }
    }
}
