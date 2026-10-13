using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileGameplayCanvasScalingTests
    {
        private static string[] ProductGameRootPaths => MobileGameRootTestCatalog.ProductGameRootPaths;

        [TestCaseSource(nameof(ProductGameRootPaths))]
        public void SafeAreaHud_UsesSharedTabletScaleAndReturnsToPhoneScale(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SafeAreaAdapter adapter = root.GetComponentInChildren<SafeAreaAdapter>(true);
                CanvasScaler hud = adapter.GetComponentInParent<CanvasScaler>(true);
                Assert.That(hud, Is.Not.Null);
                adapter.Apply(new Rect(0f, 0f, 2560f, 1600f), new Vector2(2560f, 1600f));
                MethodInfo applyScale = typeof(SafeAreaAdapter).GetMethod("ApplyCanvasScaling",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(applyScale, Is.Not.Null);
                applyScale.Invoke(adapter, new object[] { new Vector2(2560f, 1600f), false });
                Assert.That(hud.matchWidthOrHeight, Is.EqualTo(1f), "Desktop authoring stays intact.");
                applyScale.Invoke(adapter, new object[] { new Vector2(2560f, 1600f), true });
                Assert.That(hud.matchWidthOrHeight, Is.EqualTo(0f));
                foreach (CanvasScaler scaler in root.GetComponentsInChildren<CanvasScaler>(true))
                    if (scaler != hud)
                        Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f), scaler.name);
                Assert.That(adapter.Target.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(adapter.Target.offsetMax, Is.EqualTo(Vector2.zero));
                applyScale.Invoke(adapter, new object[] { new Vector2(2400f, 1080f), true });
                Assert.That(hud.matchWidthOrHeight, Is.EqualTo(1f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCaseSource(nameof(ProductGameRootPaths))]
        public void ActiveGameRoot_CanvasesPreserveReferenceHeightOnExtraWideScreens(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                Assert.That(canvases, Has.Length.EqualTo(5));

                foreach (Canvas canvas in canvases)
                {
                    CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                    Assert.That(scaler, Is.Not.Null, $"{prefabPath}/{canvas.name} no tiene CanvasScaler.");
                    Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                    Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
                    Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
                    Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f),
                        $"{prefabPath}/{canvas.name} debe preservar los 1080 de alto en pantallas extra-wide.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
