using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class OptionsPlatformPresentationTests
    {
        private const string PrefabPath = "Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab";

        [TestCase(true)]
        [TestCase(false)]
        public void RealPrefab_ShowsOnlyApplicableControlsAndPreservesSettings(bool isMobilePlatform)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                OptionsMenuManager manager = root.GetComponentInChildren<OptionsMenuManager>(true);
                var serialized = new SerializedObject(manager);
                TMP_Dropdown resolution = serialized.FindProperty("resolutionDropdown").objectReferenceValue as TMP_Dropdown;
                Toggle fullscreen = serialized.FindProperty("fullscreenToggle").objectReferenceValue as Toggle;
                Slider volume = serialized.FindProperty("volumeSlider").objectReferenceValue as Slider;
                Button back = serialized.FindProperty("backButton").objectReferenceValue as Button;
                RectTransform backdrop = (serialized.FindProperty("background").objectReferenceValue as GameObject).transform as RectTransform;
                Vector2 backgroundAnchor = backdrop.anchorMin, backgroundSize = backdrop.sizeDelta;
                Assert.That(resolution, Is.Not.Null);
                Assert.That(fullscreen, Is.Not.Null);
                Assert.That(volume, Is.Not.Null);
                Assert.That(back, Is.Not.Null);
                Vector3[] positions = { resolution.transform.localPosition, fullscreen.transform.localPosition,
                    volume.transform.localPosition, back.transform.localPosition };
                float originalVolume = volume.value;
                bool originalFullscreen = fullscreen.isOn;

                Apply(manager, isMobilePlatform);
                if (isMobilePlatform)
                {
                    Assert.That(backdrop.anchorMin, Is.EqualTo(Vector2.zero));
                    Assert.That(backdrop.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(backdrop.offsetMin, Is.EqualTo(Vector2.zero));
                    Assert.That(backdrop.offsetMax, Is.EqualTo(Vector2.zero));
                }
                else
                {
                    Assert.That(backdrop.anchorMin, Is.EqualTo(backgroundAnchor));
                    Assert.That(backdrop.sizeDelta, Is.EqualTo(backgroundSize));
                }
                Assert.That(resolution.gameObject.activeSelf, Is.EqualTo(!isMobilePlatform));
                Assert.That(fullscreen.gameObject.activeSelf, Is.EqualTo(!isMobilePlatform));
                Assert.That(volume.gameObject.activeSelf, Is.True);
                Assert.That(back.gameObject.activeSelf, Is.True);
                Assert.That(volume.value, Is.EqualTo(originalVolume));
                Assert.That(fullscreen.isOn, Is.EqualTo(originalFullscreen));
                Assert.That(resolution.transform.localPosition, Is.EqualTo(positions[0]));
                Assert.That(fullscreen.transform.localPosition, Is.EqualTo(positions[1]));
                Assert.That(volume.transform.localPosition, Is.EqualTo(positions[2]));
                Assert.That(back.transform.localPosition, Is.EqualTo(positions[3]));

                // Reapplication is idempotent, and desktop presentation can be
                // restored without replacing controls or their serialized refs.
                Apply(manager, isMobilePlatform);
                Apply(manager, false);
                Assert.That(resolution.gameObject.activeSelf, Is.True);
                Assert.That(fullscreen.gameObject.activeSelf, Is.True);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Apply(OptionsMenuManager manager, bool isMobilePlatform)
        {
            MethodInfo apply = typeof(OptionsMenuManager).GetMethod("ApplyPlatformPresentation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(apply, Is.Not.Null);
            apply.Invoke(manager, new object[] { isMobilePlatform });
        }
    }
}
