using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameplayUiPrefabSceneMigration
{
    private const string PrimaryScenePath = "Assets/Scenes/Game/ZonaEpipelagica.unity";
    private const string SecondaryScenePath = "Assets/Scenes/Game/ZonaAbisopelagica.unity";
    private const string TutorialScenePath = "Assets/Scenes/Game/ZonaTutorial.unity";

    private const string InkBarHorizontalPrefabPath = "Assets/Content/Prefabs/UI/HUD/InkBarHorizontal.prefab";
    private const string InkBarVerticalPrefabPath = "Assets/Content/Prefabs/UI/HUD/InkBarVertical.prefab";
    private const string InkBarLegacyPrefabPath = "Assets/Content/Prefabs/UI/HUD/InkPulseBarLegacy.prefab";
    private const string GadgetSlotsPrefabPath = "Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab";
    private const string ShrimpCounterPrefabPath = "Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab";
    private const string ScoreCounterPrefabPath = "Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab";
    private const string PauseMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab";
    private const string GameOverMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab";
    private const string InGameShopMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab";

    private static readonly SceneUiPrefabContract[] SceneContracts =
    {
        new(PrimaryScenePath, "InkBar", InkBarHorizontalPrefabPath),
        new(SecondaryScenePath, "InkBar", InkBarVerticalPrefabPath),
        new(TutorialScenePath, "InkPulseBar", InkBarLegacyPrefabPath)
    };

    [MenuItem("Tools/Squid/Migrate Gameplay UI To Prefab Instances")]
    public static void MigrateGameplayUiToPrefabInstances()
    {
        foreach (SceneUiPrefabContract sceneContract in SceneContracts)
        {
            MigrateScene(sceneContract);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameplayUiPrefabSceneMigration] Gameplay UI migrated to prefab instances.");
    }

    [MenuItem("Tools/Squid/Validate Gameplay UI Prefab Instances")]
    public static void ValidateGameplayUiPrefabInstances()
    {
        foreach (SceneUiPrefabContract sceneContract in SceneContracts)
        {
            Scene scene = EditorSceneManager.OpenScene(sceneContract.ScenePath, OpenSceneMode.Single);
            ValidateScene(scene, sceneContract);
        }

        Debug.Log("[GameplayUiPrefabSceneMigration] Gameplay UI prefab instances validated.");
    }

    public static void MigrateAndValidate()
    {
        MigrateGameplayUiToPrefabInstances();
        ValidateGameplayUiPrefabInstances();
    }

    private static void MigrateScene(SceneUiPrefabContract sceneContract)
    {
        Scene scene = EditorSceneManager.OpenScene(sceneContract.ScenePath, OpenSceneMode.Single);

        GameObject inkBar = ReplaceSceneObjectWithPrefab(scene, sceneContract.InkBarObjectName, sceneContract.InkBarPrefabPath);
        ReplaceSceneObjectWithPrefab(scene, "GadgetSlots", GadgetSlotsPrefabPath);
        ReplaceSceneObjectWithPrefab(scene, "ShrimpCounter", ShrimpCounterPrefabPath);
        ReplaceSceneObjectWithPrefab(scene, "Score", ScoreCounterPrefabPath);
        GameObject pauseMenu = ReplaceSceneObjectWithPrefab(scene, "PauseCanvas", PauseMenuPrefabPath);
        GameObject gameOverMenu = ReplaceSceneObjectWithPrefab(scene, "GameOverCanvas", GameOverMenuPrefabPath);
        GameObject inGameShopMenu = ReplaceSceneObjectWithPrefab(scene, "InGameCanvas", InGameShopMenuPrefabPath);
        GameUIRoot gameUIRoot = EnsureGameUIRoot(scene);

        WireInkPulseControllers(scene, inkBar);
        WirePauseMenuManager(scene, pauseMenu);
        WireGameOverMenuManager(scene, gameOverMenu);
        WireInGameShopManager(scene, inGameShopMenu);
        WireGameUIRoot(scene, gameUIRoot, inkBar, pauseMenu, gameOverMenu, inGameShopMenu);
        ValidateScene(scene, sceneContract);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Could not save scene {sceneContract.ScenePath}.");
        }
    }

    private static GameObject ReplaceSceneObjectWithPrefab(Scene scene, string objectName, string prefabPath)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        GameObject existing = FindSceneObjectByName(scene, objectName);

        if (existing != null && IsInstanceOfPrefab(existing, prefabPath))
        {
            return existing;
        }

        if (existing == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} is missing local UI object {objectName}.");
        }

        TransformSnapshot snapshot = TransformSnapshot.Capture(existing.transform);

        UnityEngine.Object.DestroyImmediate(existing);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (instance == null)
        {
            throw new InvalidOperationException($"Could not instantiate prefab {prefabPath} in {scene.path}.");
        }

        instance.name = objectName;
        snapshot.Apply(instance.transform);
        EditorUtility.SetDirty(instance);
        return instance;
    }

    private static void WireInkPulseControllers(Scene scene, GameObject inkBar)
    {
        ChargeBar chargeBar = inkBar != null ? inkBar.GetComponent<ChargeBar>() : null;
        if (chargeBar == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} has no ChargeBar in InkBar prefab instance.");
        }

        foreach (InkPulseController controller in FindSceneComponents<InkPulseController>(scene))
        {
            SerializedObject serializedObject = new(controller);
            SetObjectReference(serializedObject, "chargeBar", chargeBar);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }

    private static void WirePauseMenuManager(Scene scene, GameObject pauseMenu)
    {
        PauseMenuManager manager = FindSceneComponent<PauseMenuManager>(scene);
        if (manager == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} has no PauseMenuManager.");
        }

        SerializedObject serializedObject = new(manager);
        SetObjectReference(serializedObject, "menuRoot", pauseMenu);
        SetObjectReference(serializedObject, "canvasGroup", pauseMenu.GetComponent<CanvasGroup>());
        SetObjectReference(serializedObject, "resumeButton", FindChildComponent<Button>(pauseMenu, "BotonReanudar"));
        SetObjectReference(serializedObject, "optionsButton", FindChildComponent<Button>(pauseMenu, "BotonOpciones"));
        SetObjectReference(serializedObject, "menuButton", FindChildComponent<Button>(pauseMenu, "BotonMenu"));
        SetObjectReference(serializedObject, "exitButton", FindChildComponent<Button>(pauseMenu, "BotonSalir"));
        SetObjectReferenceArray(serializedObject, "animatedDecorations", FindChildComponent<RectTransform>(pauseMenu, "PauseDecoration"));
        SetObjectReferenceArray(
            serializedObject,
            "animatedButtons",
            FindChildComponent<RectTransform>(pauseMenu, "BotonReanudar"),
            FindChildComponent<RectTransform>(pauseMenu, "BotonOpciones"),
            FindChildComponent<RectTransform>(pauseMenu, "BotonMenu"),
            FindChildComponent<RectTransform>(pauseMenu, "BotonSalir"));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void WireGameOverMenuManager(Scene scene, GameObject gameOverMenu)
    {
        GameOverMenuManager manager = FindSceneComponent<GameOverMenuManager>(scene);
        if (manager == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} has no GameOverMenuManager.");
        }

        SerializedObject serializedObject = new(manager);
        SetObjectReference(serializedObject, "menuRoot", gameOverMenu);
        SetObjectReference(serializedObject, "canvasGroup", gameOverMenu.GetComponent<CanvasGroup>());
        SetObjectReference(serializedObject, "retryButton", FindChildComponent<Button>(gameOverMenu, "BotonReintentar"));
        SetObjectReference(serializedObject, "menuButton", FindChildComponent<Button>(gameOverMenu, "BotonMenu"));
        SetObjectReferenceArray(serializedObject, "animatedDecorations", FindChildComponent<RectTransform>(gameOverMenu, "GameOverDecoration"));
        SetObjectReferenceArray(
            serializedObject,
            "animatedButtons",
            FindChildComponent<RectTransform>(gameOverMenu, "BotonReintentar"),
            FindChildComponent<RectTransform>(gameOverMenu, "BotonMenu"));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void WireInGameShopManager(Scene scene, GameObject inGameShopMenu)
    {
        InGameShopManager manager = FindSceneComponent<InGameShopManager>(scene);
        if (manager == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} has no InGameShopManager.");
        }

        SerializedObject serializedObject = new(manager);
        SetObjectReference(serializedObject, "menuRoot", inGameShopMenu);
        SetObjectReference(serializedObject, "canvasGroup", inGameShopMenu.GetComponent<CanvasGroup>());
        SetObjectReference(serializedObject, "gadgetImage", FindChildComponent<Image>(inGameShopMenu, "Gadget"));
        SetObjectReference(serializedObject, "priceText", FindChildComponent<TMP_Text>(inGameShopMenu, "Precio"));
        SetObjectReference(serializedObject, "buyKeyText", FindChildComponent<TMP_Text>(inGameShopMenu, "B"));
        SetObjectReference(serializedObject, "buyButton", FindChildComponent<Button>(inGameShopMenu, "Comprar"));
        SetObjectReference(serializedObject, "insufficientFundsText", FindChildComponent<TMP_Text>(inGameShopMenu, "SinSaldo"));
        SetObjectReference(serializedObject, "timerText", FindChildComponent<TMP_Text>(inGameShopMenu, "Tiempo"));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static GameUIRoot EnsureGameUIRoot(Scene scene)
    {
        GameObject root = FindSceneObjectByName(scene, "GameUIRoot") ?? FindSceneObjectByName(scene, "UI");
        if (root == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} has no UI/GameUIRoot object.");
        }

        root.name = "GameUIRoot";
        if (!root.TryGetComponent(out GameUIRoot gameUIRoot))
        {
            gameUIRoot = root.AddComponent<GameUIRoot>();
        }

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(gameUIRoot);
        return gameUIRoot;
    }

    private static void WireGameUIRoot(Scene scene, GameUIRoot gameUIRoot, GameObject inkBar, GameObject pauseMenu, GameObject gameOverMenu, GameObject inGameShopMenu)
    {
        GameObject hud = FindSceneObjectByName(scene, "HUD");
        GameObject eventSystem = FindSceneObjectByName(scene, "EventSystem");
        GameObject gadgetSlots = FindSceneObjectByName(scene, "GadgetSlots");
        GameObject shrimpCounter = FindSceneObjectByName(scene, "ShrimpCounter");
        GameObject scoreCounter = FindSceneObjectByName(scene, "Score");

        SerializedObject serializedObject = new(gameUIRoot);
        SetObjectReference(serializedObject, "eventSystemRoot", eventSystem != null ? eventSystem.transform : null);
        SetObjectReference(serializedObject, "hudRoot", hud != null ? hud.GetComponent<RectTransform>() : null);
        SetObjectReference(serializedObject, "pauseMenuRoot", pauseMenu);
        SetObjectReference(serializedObject, "gameOverMenuRoot", gameOverMenu);
        SetObjectReference(serializedObject, "inGameShopMenuRoot", inGameShopMenu);
        SetObjectReference(serializedObject, "inkBar", inkBar != null ? inkBar.GetComponent<ChargeBar>() : null);
        SetObjectReference(serializedObject, "gadgetSlots", gadgetSlots != null ? gadgetSlots.GetComponent<GadgetInventoryHud>() : null);
        SetObjectReference(serializedObject, "shrimpCounter", shrimpCounter != null ? shrimpCounter.GetComponent<ShrimpCounterDisplay>() : null);
        SetObjectReference(serializedObject, "scoreCounter", scoreCounter != null ? scoreCounter.GetComponent<ScoreCounterDisplay>() : null);
        SetObjectReference(serializedObject, "pauseMenuManager", FindSceneComponent<PauseMenuManager>(scene));
        SetObjectReference(serializedObject, "gameOverMenuManager", FindSceneComponent<GameOverMenuManager>(scene));
        SetObjectReference(serializedObject, "inGameShopManager", FindSceneComponent<InGameShopManager>(scene));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameUIRoot);
    }

    private static void ValidateScene(Scene scene, SceneUiPrefabContract sceneContract)
    {
        ValidatePrefabInstance(scene, sceneContract.InkBarObjectName, sceneContract.InkBarPrefabPath);
        ValidatePrefabInstance(scene, "GadgetSlots", GadgetSlotsPrefabPath);
        ValidatePrefabInstance(scene, "ShrimpCounter", ShrimpCounterPrefabPath);
        ValidatePrefabInstance(scene, "Score", ScoreCounterPrefabPath);
        ValidatePrefabInstance(scene, "PauseCanvas", PauseMenuPrefabPath);
        ValidatePrefabInstance(scene, "GameOverCanvas", GameOverMenuPrefabPath);
        ValidatePrefabInstance(scene, "InGameCanvas", InGameShopMenuPrefabPath);

        ValidateGameUIRootContract(scene);
        ValidateSceneReferences(scene);
        ValidateNoManagerPersistentButtonEvents(scene);
    }

    private static void ValidateGameUIRootContract(Scene scene)
    {
        GameUIRoot gameUIRoot = FindSceneComponent<GameUIRoot>(scene);
        if (gameUIRoot == null || gameUIRoot.name != "GameUIRoot")
        {
            throw new InvalidOperationException($"Scene {scene.path} has no GameUIRoot contract.");
        }

        RequireManagerReferences(
            scene,
            gameUIRoot,
            nameof(GameUIRoot),
            "eventSystemRoot",
            "hudRoot",
            "pauseMenuRoot",
            "gameOverMenuRoot",
            "inGameShopMenuRoot",
            "inkBar",
            "gadgetSlots",
            "shrimpCounter",
            "scoreCounter",
            "pauseMenuManager",
            "gameOverMenuManager",
            "inGameShopManager");
    }

    private static void ValidatePrefabInstance(Scene scene, string objectName, string prefabPath)
    {
        GameObject sceneObject = FindSceneObjectByName(scene, objectName);
        if (sceneObject == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} is missing {objectName}.");
        }

        if (!IsInstanceOfPrefab(sceneObject, prefabPath))
        {
            throw new InvalidOperationException($"{objectName} in scene {scene.path} is not an instance of {prefabPath}.");
        }
    }

    private static void ValidateSceneReferences(Scene scene)
    {
        foreach (InkPulseController controller in FindSceneComponents<InkPulseController>(scene))
        {
            RequireObjectReference(new SerializedObject(controller), "chargeBar", scene.path, nameof(InkPulseController));
        }

        RequireManagerReferences(scene, FindSceneComponent<PauseMenuManager>(scene), nameof(PauseMenuManager), "menuRoot", "canvasGroup", "resumeButton", "optionsButton", "menuButton", "exitButton");
        RequireManagerReferences(scene, FindSceneComponent<GameOverMenuManager>(scene), nameof(GameOverMenuManager), "menuRoot", "canvasGroup", "retryButton", "menuButton");
        RequireManagerReferences(scene, FindSceneComponent<InGameShopManager>(scene), nameof(InGameShopManager), "menuRoot", "canvasGroup", "gadgetImage", "priceText", "buyKeyText", "buyButton", "insufficientFundsText");
    }

    private static void ValidateNoManagerPersistentButtonEvents(Scene scene)
    {
        foreach (Button button in FindSceneComponents<Button>(scene))
        {
            ValidateNoManagerPersistentButtonEvents(button.onClick, scene.path, button);
        }
    }

    private static void ValidateNoManagerPersistentButtonEvents(UnityEvent unityEvent, string scenePath, Button owner)
    {
        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            UnityEngine.Object target = unityEvent.GetPersistentTarget(i);
            if (target is PauseMenuManager || target is GameOverMenuManager || target is InGameShopManager)
            {
                throw new InvalidOperationException(
                    $"Button {owner.name} in scene {scenePath} has a persistent event pointing to a menu manager. Runtime wiring should own this.");
            }
        }
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Missing UI prefab at {path}.");
        }

        return prefab;
    }

    private static bool IsInstanceOfPrefab(GameObject sceneObject, string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject);
        return prefabRoot != null && AssetDatabase.GetAssetPath(prefabRoot) == prefabPath;
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform child in children)
            {
                if (child.name == objectName)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (T component in FindSceneComponents<T>(scene))
        {
            return component;
        }

        return null;
    }

    private static IEnumerable<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] components = root.GetComponentsInChildren<T>(includeInactive: true);
            foreach (T component in components)
            {
                yield return component;
            }
        }
    }

    private static T FindChildComponent<T>(GameObject root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform child in children)
        {
            if (child.name == childName && child.TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Serialized property {propertyName} was not found on {serializedObject.targetObject.name}.");
        }

        property.objectReferenceValue = value;
    }

    private static void SetObjectReferenceArray(SerializedObject serializedObject, string propertyName, params UnityEngine.Object[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Serialized property {propertyName} was not found on {serializedObject.targetObject.name}.");
        }

        List<UnityEngine.Object> filteredValues = new();
        foreach (UnityEngine.Object value in values)
        {
            if (value != null)
            {
                filteredValues.Add(value);
            }
        }

        property.arraySize = filteredValues.Count;
        for (int i = 0; i < filteredValues.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = filteredValues[i];
        }
    }

    private static void RequireManagerReferences(Scene scene, Component manager, string managerName, params string[] propertyNames)
    {
        if (manager == null)
        {
            throw new InvalidOperationException($"Scene {scene.path} has no {managerName}.");
        }

        SerializedObject serializedObject = new(manager);
        foreach (string propertyName in propertyNames)
        {
            RequireObjectReference(serializedObject, propertyName, scene.path, managerName);
        }
    }

    private static void RequireObjectReference(SerializedObject serializedObject, string propertyName, string scenePath, string ownerName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
        {
            throw new InvalidOperationException($"{ownerName}.{propertyName} is missing in scene {scenePath}.");
        }
    }

    private readonly struct SceneUiPrefabContract
    {
        public SceneUiPrefabContract(string scenePath, string inkBarObjectName, string inkBarPrefabPath)
        {
            ScenePath = scenePath;
            InkBarObjectName = inkBarObjectName;
            InkBarPrefabPath = inkBarPrefabPath;
        }

        public string ScenePath { get; }
        public string InkBarObjectName { get; }
        public string InkBarPrefabPath { get; }
    }

    private readonly struct TransformSnapshot
    {
        private readonly Transform parent;
        private readonly int siblingIndex;
        private readonly bool activeSelf;
        private readonly Vector3 localPosition;
        private readonly Quaternion localRotation;
        private readonly Vector3 localScale;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 pivot;
        private readonly bool isRectTransform;

        private TransformSnapshot(Transform source)
        {
            parent = source.parent;
            siblingIndex = source.GetSiblingIndex();
            activeSelf = source.gameObject.activeSelf;
            localPosition = source.localPosition;
            localRotation = source.localRotation;
            localScale = source.localScale;

            if (source is RectTransform rectTransform)
            {
                isRectTransform = true;
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                pivot = rectTransform.pivot;
            }
            else
            {
                isRectTransform = false;
                anchorMin = Vector2.zero;
                anchorMax = Vector2.zero;
                anchoredPosition = Vector2.zero;
                sizeDelta = Vector2.zero;
                pivot = Vector2.one * 0.5f;
            }
        }

        public static TransformSnapshot Capture(Transform source)
        {
            return new TransformSnapshot(source);
        }

        public void Apply(Transform target)
        {
            target.SetParent(parent, worldPositionStays: false);
            target.localPosition = localPosition;
            target.localRotation = localRotation;
            target.localScale = localScale;
            target.gameObject.SetActive(activeSelf);

            if (isRectTransform && target is RectTransform targetRect)
            {
                targetRect.anchorMin = anchorMin;
                targetRect.anchorMax = anchorMax;
                targetRect.anchoredPosition = anchoredPosition;
                targetRect.sizeDelta = sizeDelta;
                targetRect.pivot = pivot;
            }

            target.SetSiblingIndex(siblingIndex);
        }
    }
}
