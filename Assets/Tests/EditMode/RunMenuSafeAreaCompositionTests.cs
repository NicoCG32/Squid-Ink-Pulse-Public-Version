using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class RunMenuSafeAreaCompositionTests
    {
        [TestCase("PauseMenu", true)]
        [TestCase("GameOverMenu", false)]
        public void SharedPrefab_ProtectsContentAndPreservesFullScreenBackground(string name, bool pause)
        {
            GameObject root = PrefabUtility.LoadPrefabContents($"Assets/Content/Prefabs/UI/Menus/{name}.prefab");
            try { AssertContract(root, pause); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [TestCase("Assets/Scenes/Game/ZonaEpipelagica.unity")]
        [TestCase("Assets/Scenes/Game/ZonaAbisopelagica.unity")]
        public void SceneInstances_KeepBothSharedLayoutsAndManagerReferences(string path)
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                GameUIRoot ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameUIRoot>(true)).Single();
                AssertContract(ui.PauseMenuRoot, true);
                AssertContract(ui.GameOverMenuRoot, false);
                foreach (var item in new[] { (manager: (Component)ui.PauseMenuManager, pause: true),
                    (manager: (Component)ui.GameOverMenuManager, pause: false) })
                {
                    // Score references intentionally use the manager's local hierarchy fallback.
                    // Resolve them in memory as Awake does; discard the scene without saving.
                    item.manager.GetType().GetMethod("ResolveUiReferences", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(item.manager, null);
                    var serialized = new SerializedObject(item.manager);
                    GameObject root = serialized.FindProperty("menuRoot").objectReferenceValue as GameObject;
                    Assert.That(root, Is.SameAs(item.pause ? ui.PauseMenuRoot : ui.GameOverMenuRoot));
                    Transform content = root.GetComponentsInChildren<MobileReferenceContentFitter>(true).Single().transform;
                    foreach (string field in item.pause ? new[] { "resumeButton", "optionsButton", "menuButton" }
                        : new[] { "retryButton", "menuButton", "obtainedScoreText", "bestScoreText" })
                    {
                        Component control = serialized.FindProperty(field).objectReferenceValue as Component;
                        Assert.That(control, Is.Not.Null, field);
                        Assert.That(control.transform.IsChildOf(content), Is.True, field);
                    }
                    foreach (string field in new[] { "animatedDecorations", "animatedButtons" })
                    {
                        SerializedProperty elements = serialized.FindProperty(field);
                        for (int i = 0; i < elements.arraySize; i++)
                            Assert.That((elements.GetArrayElementAtIndex(i).objectReferenceValue as Transform).IsChildOf(content), Is.True, field);
                    }
                }
            }
            finally
            {
                if (setup.Any(item => item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void Contract_RejectsAButtonMovedOutsideSafeContent()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab");
            try
            {
                AssertContract(root, true);
                Transform button = root.GetComponentsInChildren<Transform>(true).Single(item => item.name == "ReanudarBoton");
                button.SetParent(root.transform, false);
                var failures = new List<string>();
                RunMenuMobileCompositionContract.Validate(root, true, failures);
                Assert.That(failures.Any(message => message.Contains("ReanudarBoton")), Is.True);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void AssertContract(GameObject root, bool pause)
        {
            var failures = new List<string>();
            RunMenuMobileCompositionContract.Validate(root, pause, failures);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
    }
}
