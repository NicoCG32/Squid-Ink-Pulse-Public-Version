using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class TouchControlsGameRootContractTests
    {
        private const string TouchPrefabPath =
            "Assets/Content/Prefabs/UI/Touch/TouchControls.prefab";
        private const string TutorialGameRootPath =
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaTutorial.prefab";

        private static readonly string[] ActiveGameRootPaths =
        {
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab",
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab"
        };

        private static readonly string[] ActiveScenePaths =
        {
            "Assets/Scenes/Game/ZonaEpipelagica.unity",
            "Assets/Scenes/Game/ZonaAbisopelagica.unity"
        };

        [Test]
        public void ActiveGameRoots_ContainOneNestedTouchPrefabUnderHud_WithoutDuplicatingUiInfrastructure()
        {
            foreach (string path in ActiveGameRootPaths)
            {
                GameObject gameRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(gameRoot, Is.Not.Null, $"No se pudo cargar {path}.");

                TouchGameplayControlsController[] controllers =
                    gameRoot.GetComponentsInChildren<TouchGameplayControlsController>(true);
                Assert.That(controllers, Has.Length.EqualTo(1), path);
                TouchGameplayControlsController controller = controllers[0];
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(controller.gameObject),
                    Is.EqualTo(TouchPrefabPath),
                    path);

                GameUIRoot uiRoot = gameRoot.GetComponentInChildren<GameUIRoot>(true);
                Assert.That(uiRoot, Is.Not.Null, path);
                SafeAreaAdapter[] safeAreaAdapters =
                    gameRoot.GetComponentsInChildren<SafeAreaAdapter>(true);
                Assert.That(safeAreaAdapters, Has.Length.EqualTo(1), path);
                Transform safeAreaRoot = safeAreaAdapters[0].transform;
                Assert.That(safeAreaRoot.parent, Is.SameAs(uiRoot.HudRoot), path);
                Assert.That(uiRoot.HudRoot.childCount, Is.EqualTo(1), path);
                Assert.That(controller.transform.parent, Is.SameAs(safeAreaRoot), path);
                Assert.That(
                    controller.transform.GetSiblingIndex(),
                    Is.EqualTo(safeAreaRoot.childCount - 1),
                    path);

                Assert.That(gameRoot.GetComponentsInChildren<TouchControlsVisibilityController>(true), Has.Length.EqualTo(1));
                Assert.That(gameRoot.GetComponentsInChildren<TouchSteeringSurface>(true), Has.Length.EqualTo(1));
                Assert.That(gameRoot.GetComponentsInChildren<TouchGameplayCommandButton>(true), Has.Length.EqualTo(4));
                Assert.That(gameRoot.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(5));
                Assert.That(gameRoot.GetComponentsInChildren<GraphicRaycaster>(true), Has.Length.EqualTo(5));
                Assert.That(gameRoot.GetComponentsInChildren<EventSystem>(true), Has.Length.EqualTo(1));
                Assert.That(gameRoot.GetComponentsInChildren<InputSystemUIInputModule>(true), Has.Length.EqualTo(1));

                Canvas hudCanvas = uiRoot.HudRoot.GetComponent<Canvas>();
                Assert.That(hudCanvas, Is.Not.Null);
                Transform hudTopLevel = GetTopLevelChild(uiRoot.transform, hudCanvas.transform);
                foreach (Canvas overlayCanvas in gameRoot.GetComponentsInChildren<Canvas>(true))
                {
                    if (overlayCanvas == hudCanvas)
                    {
                        continue;
                    }

                    Transform overlayTopLevel = GetTopLevelChild(uiRoot.transform, overlayCanvas.transform);
                    bool rendersAfterHud = overlayCanvas.sortingOrder > hudCanvas.sortingOrder
                        || (overlayCanvas.sortingOrder == hudCanvas.sortingOrder
                            && overlayTopLevel.GetSiblingIndex() > hudTopLevel.GetSiblingIndex());
                    Assert.That(
                        rendersAfterHud,
                        Is.True,
                        $"{path}: {overlayCanvas.name} debe conservar precedencia sobre el HUD touch.");
                }

                Assert.That(uiRoot.EventSystemRoot, Is.Not.Null);
                Assert.That(uiRoot.HudRoot, Is.Not.Null);
                Assert.That(uiRoot.PauseMenuRoot, Is.Not.Null);
                Assert.That(uiRoot.GameOverMenuRoot, Is.Not.Null);
                Assert.That(uiRoot.InGameShopMenuRoot, Is.Not.Null);
                Assert.That(uiRoot.InkBar, Is.Not.Null);
                Assert.That(uiRoot.GadgetSlots, Is.Not.Null);
                Assert.That(uiRoot.ShrimpCounter, Is.Not.Null);
                Assert.That(uiRoot.ScoreCounter, Is.Not.Null);
                Assert.That(uiRoot.PauseMenuManager, Is.Not.Null);
                Assert.That(uiRoot.GameOverMenuManager, Is.Not.Null);
                Assert.That(uiRoot.InGameShopManager, Is.Not.Null);
            }
        }

        [Test]
        public void ActiveScenes_InheritExactlyOneTouchControlsInstance()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in ActiveScenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    TouchGameplayControlsController[] controllers = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<TouchGameplayControlsController>(true))
                        .ToArray();
                    Assert.That(controllers, Has.Length.EqualTo(1), path);
                    Assert.That(
                        controllers[0].GetComponentsInChildren<TouchSteeringSurface>(true),
                        Has.Length.EqualTo(1),
                        path);
                    Assert.That(
                        controllers[0].GetComponentsInChildren<TouchGameplayCommandButton>(true),
                        Has.Length.EqualTo(4),
                        path);
                }
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        [Test]
        public void Tutorial_RemainsOutsideBuildAndContainsNoTouchControls()
        {
            GameObject tutorialRoot = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialGameRootPath);
            Assert.That(tutorialRoot, Is.Not.Null);
            Assert.That(
                tutorialRoot.GetComponentsInChildren<TouchGameplayControlsController>(true),
                Is.Empty);
            Assert.That(
                tutorialRoot.GetComponentsInChildren<TouchControlsVisibilityController>(true),
                Is.Empty);
            Assert.That(
                tutorialRoot.GetComponentsInChildren<TouchSteeringSurface>(true),
                Is.Empty);
            Assert.That(
                tutorialRoot.GetComponentsInChildren<TouchGameplayCommandButton>(true),
                Is.Empty);
            Assert.That(
                EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled && scene.path.Contains("Tutorial")),
                Is.False);
        }

        private static Transform GetTopLevelChild(Transform root, Transform descendant)
        {
            Transform current = descendant;
            while (current.parent != null && current.parent != root)
            {
                current = current.parent;
            }

            Assert.That(current.parent, Is.SameAs(root));
            return current;
        }
    }
}
