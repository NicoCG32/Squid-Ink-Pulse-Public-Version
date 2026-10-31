using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class LoreComicMobileCompositionTests
    {
        [Test]
        public void SharedPrefab_ProtectsTheVignetteAndContinueButton()
        {
            GameObject root=PrefabUtility.LoadPrefabContents("Assets/Content/Prefabs/UI/Menus/LoreComic.prefab");
            try { AssertLayout(root.GetComponent<LoreComicPresenter>()); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        [TestCase("Assets/Scenes/MainMenu/MainMenu.unity")]
        [TestCase("Assets/Scenes/Game/ZonaEpipelagica.unity")]
        [TestCase("Assets/Scenes/Game/ZonaAbisopelagica.unity")]
        public void Scene_UsesTheSharedSafeViewAndKeepsAuthoredTiming(string path)
        {
            SceneSetup[] setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var presenter=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<LoreComicPresenter>(true)).Single();
                AssertLayout(presenter);
                var entries=new SerializedObject(presenter).FindProperty("entries");
                for (int i=0;i<entries.arraySize;i++)
                {
                    var entry=entries.GetArrayElementAtIndex(i);
                    bool start=entry.FindPropertyRelative("comicEvent").intValue==(int)LoreComicEvent.GameStart;
                    Assert.That(entry.FindPropertyRelative("displaySeconds").floatValue,Is.EqualTo(start ? 0f : 3f));
                    Assert.That(entry.FindPropertyRelative("waitForContinue").boolValue,Is.EqualTo(start));
                    Assert.That(entry.FindPropertyRelative("showContinueButton").boolValue,Is.EqualTo(start));
                }
            }
            finally
            {
                if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            }
        }
        private static void AssertLayout(LoreComicPresenter presenter)
        {
            var failures=new List<string>(); LoreComicMobileCompositionContract.Validate(presenter,failures);
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
        }
    }
}
