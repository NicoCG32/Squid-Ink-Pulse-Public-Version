using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InGameShopMobileCompositionTests
    {
        [Test]
        public void SharedPrefab_ProtectsContentAndKeepsFeedbackIndependentOfButtonState()
        {
            GameObject root=PrefabUtility.LoadPrefabContents("Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab");
            try { AssertLayout(root); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        [TestCase("Assets/Scenes/Game/ZonaEpipelagica.unity")]
        [TestCase("Assets/Scenes/Game/ZonaAbisopelagica.unity")]
        public void SceneInstance_BindsTheVisibleCountdownAndSafeControls(string path)
        {
            SceneSetup[] setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                GameUIRoot ui=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameUIRoot>(true)).Single();
                AssertLayout(ui.InGameShopMenuRoot);
                var serialized=new SerializedObject(ui.InGameShopManager);
                Transform content=ui.InGameShopMenuRoot.transform.Find("InGameShopSafeArea/InGameShopContent");
                foreach (string name in new[] { "gadgetImage","priceText","buyKeyText","buyButton","insufficientFundsText","timerText" })
                {
                    Component control=serialized.FindProperty(name).objectReferenceValue as Component;
                    Assert.That(control,Is.Not.Null,name);
                    Assert.That(control.transform.IsChildOf(content),Is.True,name);
                    if (name=="timerText") Assert.That(control.name,Is.EqualTo("SegundosRestantes"));
                }
            }
            finally
            {
                if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
        [Test]
        public void Contract_RejectsFeedbackInsideNormalVisualState()
        {
            GameObject root=PrefabUtility.LoadPrefabContents("Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab");
            try
            {
                AssertLayout(root);
                Transform buy=root.transform.Find("InGameShopSafeArea/InGameShopContent/ComprarBoton");
                buy.Find("SinSaldo").SetParent(buy.Find("Visual/Normal"),false);
                var failures=new List<string>(); InGameShopMobileCompositionContract.Validate(root,failures);
                Assert.That(failures.Any(message=>message.Contains("SinSaldo")),Is.True);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void AssertLayout(GameObject root)
        {
            var failures=new List<string>(); InGameShopMobileCompositionContract.Validate(root,failures);
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
        }
    }
}
