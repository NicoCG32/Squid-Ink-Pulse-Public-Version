using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileHudSafeAreaCompositionTests
    {
        private static readonly string[] ActiveGameRootPaths =
        {
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab",
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab"
        };

        private static readonly string[] ExpectedHudChildren =
        {
            "GadgetSlots",
            "InkBar",
            "Score",
            "ShrimpCounter",
            "TouchControlsOwner"
        };

        [TestCaseSource(nameof(ActiveGameRootPaths))]
        public void ActiveGameRoot_HudContentLivesUnderOneSafeAreaRoot(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                GameUIRoot uiRoot = root.GetComponentInChildren<GameUIRoot>(true);
                Assert.That(uiRoot, Is.Not.Null);
                Assert.That(uiRoot.HudRoot.childCount, Is.EqualTo(1));

                Transform safeAreaRoot = uiRoot.HudRoot.GetChild(0);
                Assert.That(safeAreaRoot.name, Is.EqualTo("SafeAreaRoot"));
                Assert.That(safeAreaRoot.GetComponent<SafeAreaAdapter>(), Is.Not.Null);
                RectTransform rect = safeAreaRoot as RectTransform;
                Assert.That(rect, Is.Not.Null);
                Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));

                string[] childNames = safeAreaRoot.Cast<Transform>()
                    .Select(child => child.name)
                    .OrderBy(name => name)
                    .ToArray();
                Assert.That(childNames, Is.EqualTo(ExpectedHudChildren));
                Assert.That(
                    safeAreaRoot.GetComponentInChildren<TouchGameplayControlsController>(true)
                        .transform.GetSiblingIndex(),
                    Is.EqualTo(safeAreaRoot.childCount - 1));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
