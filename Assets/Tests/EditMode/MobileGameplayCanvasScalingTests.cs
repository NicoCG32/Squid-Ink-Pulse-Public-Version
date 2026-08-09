using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobileGameplayCanvasScalingTests
    {
        private static readonly string[] ActiveGameRootPaths =
        {
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab",
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab"
        };

        [TestCaseSource(nameof(ActiveGameRootPaths))]
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
