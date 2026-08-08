using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InputUiModuleContractTests
    {
        private const string OwnedInputActionsPath =
            "Assets/Implementation/Config/Input/InputSystem_Actions.inputactions";
        private const string DefaultUiActionsPath =
            "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu/MainMenu.unity",
            "Assets/Scenes/ShopMenu/ShopMenu.unity"
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaTutorial.prefab",
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab",
            "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab"
        };

        [Test]
        public void CanonicalUiModules_UseTheCompletePackageUiMapInsteadOfOwnedGameplayAsset()
        {
            InputActionAsset ownedActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(OwnedInputActionsPath);
            Assert.That(ownedActions, Is.Not.Null);

            foreach (string prefabPath in PrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                ValidateSingleModule(
                    prefab.GetComponentsInChildren<InputSystemUIInputModule>(includeInactive: true),
                    prefabPath,
                    ownedActions);
            }

            SceneSetup[] previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string scenePath in ScenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    try
                    {
                        InputSystemUIInputModule[] modules = scene.GetRootGameObjects()
                            .SelectMany(root => root.GetComponentsInChildren<InputSystemUIInputModule>(
                                includeInactive: true))
                            .ToArray();
                        ValidateSingleModule(modules, scenePath, ownedActions);
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, removeScene: true);
                    }
                }
            }
            finally
            {
                if (previousSceneSetup.Any(setup => setup.isLoaded && setup.isActive))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
                }
            }
        }

        private static void ValidateSingleModule(
            InputSystemUIInputModule[] modules,
            string ownerPath,
            InputActionAsset ownedActions)
        {
            Assert.That(modules, Has.Length.EqualTo(1), ownerPath);
            InputSystemUIInputModule module = modules[0];
            InputActionAsset uiActions = module.actionsAsset;

            Assert.That(module.enabled, Is.True, ownerPath);
            Assert.That(module.gameObject.activeSelf, Is.True, ownerPath);
            Assert.That(uiActions, Is.Not.Null, ownerPath);
            Assert.That(uiActions, Is.Not.SameAs(ownedActions), ownerPath);
            Assert.That(AssetDatabase.GetAssetPath(uiActions), Is.EqualTo(DefaultUiActionsPath), ownerPath);

            var requiredReferences = new Dictionary<string, InputActionReference>
            {
                { SquidInkPulseInputContract.Ui.Navigate, module.move },
                { SquidInkPulseInputContract.Ui.Submit, module.submit },
                { SquidInkPulseInputContract.Ui.Cancel, module.cancel },
                { SquidInkPulseInputContract.Ui.Point, module.point },
                { SquidInkPulseInputContract.Ui.Click, module.leftClick },
                { SquidInkPulseInputContract.Ui.MiddleClick, module.middleClick },
                { SquidInkPulseInputContract.Ui.RightClick, module.rightClick },
                { SquidInkPulseInputContract.Ui.ScrollWheel, module.scrollWheel },
                { SquidInkPulseInputContract.Ui.TrackedDevicePosition, module.trackedDevicePosition },
                { SquidInkPulseInputContract.Ui.TrackedDeviceOrientation, module.trackedDeviceOrientation }
            };

            foreach (KeyValuePair<string, InputActionReference> required in requiredReferences)
            {
                Assert.That(required.Value, Is.Not.Null, $"{ownerPath}: UI/{required.Key}");
                Assert.That(required.Value.action, Is.Not.Null, $"{ownerPath}: UI/{required.Key}");
                Assert.That(required.Value.action.name, Is.EqualTo(required.Key), ownerPath);
                Assert.That(required.Value.action.actionMap.name, Is.EqualTo("UI"), ownerPath);
                Assert.That(required.Value.action.actionMap.asset, Is.SameAs(uiActions), ownerPath);
            }
        }
    }
}
