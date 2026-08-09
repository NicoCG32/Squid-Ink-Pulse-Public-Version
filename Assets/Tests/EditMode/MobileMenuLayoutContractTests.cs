using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
