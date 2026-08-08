using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class TouchControlsVisibilityPolicyTests
    {
        private const string InvalidHierarchyMessage =
            "[TouchControlsVisibilityController] El root de controles touch debe ser un descendiente distinto del owner.";

        [TestCase(false)]
        [TestCase(true)]
        public void ShouldShow_AndroidPlayerIsVisibleRegardlessOfEditorOverride(
            bool editorOverrideEnabled)
        {
            Assert.That(
                TouchControlsVisibilityPolicy.ShouldShow(
                    RuntimePlatform.Android,
                    editorOverrideEnabled),
                Is.True);
        }

        [TestCase(RuntimePlatform.WindowsEditor, false, false)]
        [TestCase(RuntimePlatform.WindowsEditor, true, true)]
        [TestCase(RuntimePlatform.OSXEditor, false, false)]
        [TestCase(RuntimePlatform.OSXEditor, true, true)]
        [TestCase(RuntimePlatform.LinuxEditor, false, false)]
        [TestCase(RuntimePlatform.LinuxEditor, true, true)]
        public void ShouldShow_EditorHostsUseExplicitOverride(
            RuntimePlatform platform,
            bool editorOverrideEnabled,
            bool expected)
        {
            Assert.That(
                TouchControlsVisibilityPolicy.ShouldShow(platform, editorOverrideEnabled),
                Is.EqualTo(expected));
        }

        [TestCase(RuntimePlatform.WindowsPlayer, false)]
        [TestCase(RuntimePlatform.WindowsPlayer, true)]
        [TestCase(RuntimePlatform.OSXPlayer, false)]
        [TestCase(RuntimePlatform.OSXPlayer, true)]
        [TestCase(RuntimePlatform.LinuxPlayer, false)]
        [TestCase(RuntimePlatform.LinuxPlayer, true)]
        public void ShouldShow_DesktopPlayersRemainHidden(
            RuntimePlatform platform,
            bool editorOverrideEnabled)
        {
            Assert.That(
                TouchControlsVisibilityPolicy.ShouldShow(platform, editorOverrideEnabled),
                Is.False);
        }

        [TestCase(RuntimePlatform.IPhonePlayer, false)]
        [TestCase(RuntimePlatform.IPhonePlayer, true)]
        [TestCase(RuntimePlatform.WebGLPlayer, false)]
        [TestCase(RuntimePlatform.WebGLPlayer, true)]
        public void ShouldShow_PlatformsOutsideTheAndroidPortRemainHidden(
            RuntimePlatform platform,
            bool editorOverrideEnabled)
        {
            Assert.That(
                TouchControlsVisibilityPolicy.ShouldShow(platform, editorOverrideEnabled),
                Is.False);
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void Controller_OnEnableAppliesEditorOverrideAndRefreshesChildRoot(
            bool showInEditor,
            bool expectedVisible)
        {
            var owner = new GameObject("TouchControlsVisibilityOwner");
            var controlsRoot = new GameObject("TouchControlsRoot");
            owner.SetActive(false);
            controlsRoot.transform.SetParent(owner.transform, worldPositionStays: false);

            try
            {
                TouchControlsVisibilityController controller =
                    owner.AddComponent<TouchControlsVisibilityController>();
                ConfigureController(controller, controlsRoot, showInEditor);

                controlsRoot.SetActive(!expectedVisible);
                owner.SetActive(true);
                InvokeOnEnable(controller);
                Assert.That(controlsRoot.activeSelf, Is.EqualTo(expectedVisible));
                Assert.That(owner.activeSelf, Is.True);

                controlsRoot.SetActive(!expectedVisible);
                controller.RefreshVisibility();
                Assert.That(controlsRoot.activeSelf, Is.EqualTo(expectedVisible));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Controller_RejectsAncestorAndUnrelatedRoots(bool useAncestor)
        {
            var hierarchyRoot = new GameObject("TouchControlsHierarchyRoot");
            var owner = new GameObject("TouchControlsVisibilityOwner");
            var unrelatedRoot = new GameObject("UnrelatedTouchControlsRoot");
            owner.SetActive(false);
            owner.transform.SetParent(hierarchyRoot.transform, worldPositionStays: false);
            GameObject invalidRoot = useAncestor ? hierarchyRoot : unrelatedRoot;

            try
            {
                TouchControlsVisibilityController controller =
                    owner.AddComponent<TouchControlsVisibilityController>();
                ConfigureController(controller, invalidRoot, showInEditor: false);

                LogAssert.Expect(LogType.Error, InvalidHierarchyMessage);
                controller.RefreshVisibility();

                Assert.That(hierarchyRoot.activeSelf, Is.True);
                Assert.That(unrelatedRoot.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(hierarchyRoot);
                Object.DestroyImmediate(unrelatedRoot);
            }
        }

        [Test]
        public void Controller_RejectsOwnerAsRootWithoutDisablingIt()
        {
            var owner = new GameObject("TouchControlsVisibilityOwner");
            owner.SetActive(false);

            try
            {
                TouchControlsVisibilityController controller =
                    owner.AddComponent<TouchControlsVisibilityController>();
                ConfigureController(controller, owner, showInEditor: false);
                owner.SetActive(true);

                LogAssert.Expect(LogType.Error, InvalidHierarchyMessage);
                controller.RefreshVisibility();

                Assert.That(owner.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static void ConfigureController(
            TouchControlsVisibilityController controller,
            GameObject controlsRoot,
            bool showInEditor)
        {
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("controlsRoot").objectReferenceValue = controlsRoot;
            serializedController.FindProperty("showInEditor").boolValue = showInEditor;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeOnEnable(TouchControlsVisibilityController controller)
        {
            MethodInfo onEnable = typeof(TouchControlsVisibilityController).GetMethod(
                "OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onEnable, Is.Not.Null);
            onEnable.Invoke(controller, null);
        }
    }
}
