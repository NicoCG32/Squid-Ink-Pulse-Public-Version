using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileHudSafeAreaCompositionTests
    {
        private static string[] ProductGameRootPaths => MobileGameRootTestCatalog.ProductGameRootPaths;

        private static readonly string[] ExpectedHudChildren =
        {
            "GadgetSlots",
            "InkBar",
            "Score",
            "ShrimpCounter",
            "TouchControlsOwner"
        };

        [Test]
        public void ProductGameRootCatalog_KeepsTutorialOutAndActiveZonesIn()
        {
            string[] paths = ProductGameRootPaths;
            Assert.That(paths, Is.Not.Empty);
            Assert.That(paths, Does.Not.Contain(MobileGameRootTestCatalog.TutorialGameRootPath));

            foreach (string requiredPath in MobileGameRootTestCatalog.RequiredProductGameRootPaths)
            {
                Assert.That(paths, Does.Contain(requiredPath));
            }
        }

        [TestCaseSource(nameof(ProductGameRootPaths))]
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
                Assert.That(root.GetComponentsInChildren<SafeAreaAdapter>(true), Has.Length.EqualTo(1));
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
                AssertHudReferenceLivesUnder(uiRoot.InkBar, safeAreaRoot, prefabPath);
                AssertHudReferenceLivesUnder(uiRoot.GadgetSlots, safeAreaRoot, prefabPath);
                AssertHudReferenceLivesUnder(uiRoot.ShrimpCounter, safeAreaRoot, prefabPath);
                AssertHudReferenceLivesUnder(uiRoot.ScoreCounter, safeAreaRoot, prefabPath);
                Assert.That(
                    safeAreaRoot.GetComponentInChildren<TouchGameplayControlsController>(true)
                        .transform.GetSiblingIndex(),
                    Is.EqualTo(safeAreaRoot.childCount - 1));
                var failures = new List<string>();
                MobileHudLayoutContract.Validate(CanonicalSafeArea(), safeAreaRoot, prefabPath, failures);
                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase("InkBar", "anchoredPosition")]
        [TestCase("Score", "anchorMin")]
        [TestCase("TouchControlsOwner", "sizeDelta")]
        public void SharedHudContract_RejectsLocalLayoutOverrides(string containerName, string property)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MobileGameRootCatalog.AbyssopelagicGameRootPath);
            try
            {
                Transform safeArea = root.GetComponentInChildren<GameUIRoot>(true).HudRoot.Find("SafeAreaRoot");
                RectTransform rect = (RectTransform)safeArea.Find(containerName);
                switch (property)
                {
                    case "anchoredPosition": rect.anchoredPosition += Vector2.right * 10f; break;
                    case "anchorMin": rect.anchorMin += Vector2.right * 0.1f; break;
                    case "sizeDelta": rect.sizeDelta += Vector2.right * 10f; break;
                }

                var failures = new List<string>();
                MobileHudLayoutContract.Validate(CanonicalSafeArea(), safeArea, "mutated", failures);
                Assert.That(failures.Any(message => message.Contains($"{containerName}: {property}")), Is.True,
                    string.Join("\n", failures));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase("localScale")]
        [TestCase("localRotation")]
        [TestCase("offsetMin")]
        public void SharedHudContract_RejectsSafeAreaDistortion(string property)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MobileGameRootCatalog.AbyssopelagicGameRootPath);
            try
            {
                RectTransform safeArea = (RectTransform)root.GetComponentInChildren<GameUIRoot>(true)
                    .HudRoot.Find("SafeAreaRoot");
                switch (property)
                {
                    case "localScale": safeArea.localScale = new Vector3(0.5f, 1f, 1f); break;
                    case "localRotation": safeArea.localRotation = Quaternion.Euler(0f, 0f, 10f); break;
                    case "offsetMin": safeArea.offsetMin = new Vector2(10f, 0f); break;
                }

                var failures = new List<string>();
                MobileHudLayoutContract.Validate(CanonicalSafeArea(), safeArea, "mutated", failures);
                Assert.That(failures.Any(message => message.Contains($"SafeAreaRoot: {property}")), Is.True,
                    string.Join("\n", failures));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform CanonicalSafeArea()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(MobileGameRootCatalog.CanonicalGameRootPath);
            return root.GetComponentInChildren<GameUIRoot>(true).HudRoot.Find("SafeAreaRoot");
        }

        private static void AssertHudReferenceLivesUnder(Component component, Transform safeAreaRoot, string prefabPath)
        {
            Assert.That(component, Is.Not.Null, prefabPath);
            Assert.That(
                component.transform.IsChildOf(safeAreaRoot),
                Is.True,
                $"{prefabPath}: {component.name} debe vivir bajo SafeAreaRoot.");
        }
    }
}
