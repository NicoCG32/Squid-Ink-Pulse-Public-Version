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
    public sealed class MainMenuTouchNavigationPlayModeTests : InputTestFixture
    {
        private Touchscreen screen;
        public override void Setup()
        {
            base.Setup();
            currentTime = Time.realtimeSinceStartupAsDouble;
            Time.timeScale = 1f;
            screen = InputSystem.AddDevice<Touchscreen>();
        }

        public override void TearDown()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                Object.DestroyImmediate(root);
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Touch_OpensOptionsNavigatesTutorialAndLoadsShopWithoutMouse()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu/MainMenu.unity", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(0.1f);
            MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(Mouse.current, Is.Null, "This fixture must route touch without a mouse.");
            yield return Tap(1, Find("OpcionesBoton").GetComponentInChildren<Button>(true));
            yield return new WaitForSecondsRealtime(0.4f);
            OptionsMenuManager options = Field<OptionsMenuManager>(menu, "optionsMenuPanel");
            Assert.That(Field<bool>(options, "isOpen"), Is.True);
            yield return Tap(2, Field<Button>(options, "backButton"));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Field<bool>(options, "isOpen"), Is.False);

            yield return Tap(3, Field<Button>(menu, "tutorialButton"));
            Assert.That(Field<bool>(menu, "tutorialComicsOpen"), Is.True);
            Assert.That(Field<int>(menu, "currentTutorialComicPageIndex"), Is.Zero);
            yield return new WaitForSecondsRealtime(0.1f);
            for (int page = 1; page <= 3; page++)
            {
                yield return Tap(3 + page, Field<Button>(menu, "tutorialNextButton"));
                if (page < 3)
                    Assert.That(Field<int>(menu, "currentTutorialComicPageIndex"), Is.EqualTo(page));
            }
            Assert.That(Field<bool>(menu, "tutorialComicsOpen"), Is.False);
            yield return Tap(7, Find("TiendaBoton").GetComponentInChildren<Button>(true));
            double deadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (SceneManager.GetActiveScene().name != "ShopMenu" && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ShopMenu"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private IEnumerator Tap(int id, Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.interactable && button.gameObject.activeInHierarchy, Is.True, button.name);
            Canvas.ForceUpdateCanvases();
            RectTransform rect = button.transform as RectTransform;
            Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), $"{button.name} at {position}; canvas={canvas.name}/{canvas.renderMode}; screen={Screen.width}x{Screen.height}; graphicDepth={button.targetGraphic.depth}; culled={button.targetGraphic.canvasRenderer.cull}; raycast={button.targetGraphic.raycastTarget}; groups={string.Join(",", button.GetComponentsInParent<CanvasGroup>().Select(group => group.name + ":" + group.blocksRaycasts))}");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button), button.name);
            BeginTouch(id, position, screen: screen);
            yield return null;
            ButtonVisualState visuals = button.GetComponent<ButtonVisualState>();
            if (visuals != null && visuals.PressedState != null)
                Assert.That(visuals.PressedState.activeSelf, Is.True, "A touch down must show feedback without hover.");
            EndTouch(id, position, screen: screen);
            yield return null;
        }

        private static Transform Find(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Single(item => item.name == name);

        private static T Field<T>(object instance, string name) => (T)instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }
}
