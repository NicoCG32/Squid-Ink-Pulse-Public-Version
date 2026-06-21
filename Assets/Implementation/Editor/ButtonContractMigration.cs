using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ButtonContractMigration
{
    private const string DefaultPressedSfxPath = "Assets/Content/Audio/SFX/MainMenu/Splat.mp3";

    [MenuItem("Tools/Squid/UI/Migrate Button Contracts")]
    public static void MigrateAllButtonContracts()
    {
        int changedAssets = 0;

        try
        {
            changedAssets += MigratePrefabs();
            changedAssets += MigrateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ButtonContractMigration] Button contract migration finished. Changed assets: {changedAssets}.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ButtonContractMigration] Migration failed: {exception}");
            throw;
        }
    }

    public static void MigrateAllButtonContractsBatch()
    {
        MigrateAllButtonContracts();
    }

    private static int MigratePrefabs()
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        int changedAssets = 0;
        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                bool changed = NormalizeButtonsInHierarchy(prefabRoot.transform, skipNestedPrefabInstances: true);
                changed |= RepairManagerReferences(prefabRoot.transform);

                if (!changed)
                {
                    continue;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                changedAssets++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        return changedAssets;
    }

    private static int MigrateScenes()
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        int changedAssets = 0;
        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                changed |= NormalizeButtonsInHierarchy(root.transform, skipNestedPrefabInstances: true);
                changed |= RepairManagerReferences(root.transform);
            }

            if (!changed)
            {
                continue;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            changedAssets++;
        }

        return changedAssets;
    }

    private static bool NormalizeButtonsInHierarchy(Transform root, bool skipNestedPrefabInstances)
    {
        bool changed = false;
        Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: true).ToArray();

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (skipNestedPrefabInstances && PrefabUtility.IsPartOfPrefabInstance(button.gameObject))
            {
                continue;
            }

            changed |= NormalizeButton(button);
        }

        return changed;
    }

    private static bool NormalizeButton(Button button)
    {
        if (UiButtonContract.IsCompliantButton(button))
        {
            return ConfigureExistingContract(button);
        }

        Transform contractRoot = button.transform;
        GameObject contractObject = contractRoot.gameObject;
        string normalizedName = ToContractRootName(contractObject.name);

        if (contractObject.name != normalizedName)
        {
            Undo.RecordObject(contractObject, "Normalize button contract name");
            contractObject.name = normalizedName;
        }

        List<AudioSource> audioSources = contractRoot.GetComponentsInChildren<AudioSource>(includeInactive: true)
            .Where(source => source != null)
            .ToList();

        RectTransform visualRoot = EnsureRectChild(contractRoot, UiButtonContract.VisualChildName);
        RectTransform buttonRoot = EnsureRectChild(contractRoot, UiButtonContract.ButtonChildName);
        visualRoot.SetSiblingIndex(0);
        buttonRoot.SetSiblingIndex(1);

        Button migratedButton = buttonRoot.GetComponent<Button>();
        if (migratedButton == null)
        {
            migratedButton = buttonRoot.gameObject.AddComponent<Button>();
            EditorUtility.CopySerialized(button, migratedButton);
        }

        Image hitbox = buttonRoot.GetComponent<Image>();
        if (hitbox == null)
        {
            hitbox = buttonRoot.gameObject.AddComponent<Image>();
        }

        ConfigureHitbox(hitbox);
        migratedButton.targetGraphic = hitbox;
        migratedButton.transition = Selectable.Transition.None;

        AudioSource pressedAudioSource = ConsolidatePressedAudioSource(migratedButton, audioSources);
        if (pressedAudioSource != null)
        {
            RemovePersistentAudioSourcePlayCalls(migratedButton);
        }

        RectTransform normalState = BuildVisualStateFromLegacyButton(
            contractRoot,
            visualRoot,
            UiButtonContract.NormalStateName,
            button);
        RectTransform highlightedState = DuplicateState(
            normalState,
            visualRoot,
            UiButtonContract.HighlightedStateName,
            button.spriteState.highlightedSprite);
        RectTransform pressedState = DuplicateState(
            normalState,
            visualRoot,
            UiButtonContract.PressedStateName,
            button.spriteState.pressedSprite);

        normalState.gameObject.SetActive(true);
        highlightedState.gameObject.SetActive(false);
        pressedState.gameObject.SetActive(false);

        DisableVisualRaycasts(visualRoot);
        RemoveLegacyButtonAnimations(contractRoot);
        ConfigureVisualState(
            migratedButton,
            normalState.gameObject,
            highlightedState.gameObject,
            pressedState.gameObject,
            pressedAudioSource);

        if (button != migratedButton)
        {
            UnityEngine.Object.DestroyImmediate(button);
        }

        RemoveRootVisualComponents(contractObject);
        EditorUtility.SetDirty(contractObject);
        return true;
    }

    private static bool ConfigureExistingContract(Button button)
    {
        Transform contractRoot = button.transform.parent;
        Transform visualRoot = contractRoot.Find(UiButtonContract.VisualChildName);
        Transform normalState = visualRoot.Find(UiButtonContract.NormalStateName);
        Transform highlightedState = visualRoot.Find(UiButtonContract.HighlightedStateName);
        Transform pressedState = visualRoot.Find(UiButtonContract.PressedStateName);
        Image hitbox = button.GetComponent<Image>();
        if (hitbox != null)
        {
            ConfigureHitbox(hitbox);
            button.targetGraphic = hitbox;
            button.transition = Selectable.Transition.None;
        }

        DisableVisualRaycasts(visualRoot);
        AudioSource pressedAudioSource = ConsolidatePressedAudioSource(
            button,
            button.GetComponentsInChildren<AudioSource>(includeInactive: true));

        ConfigureVisualState(
            button,
            normalState.gameObject,
            highlightedState.gameObject,
            pressedState.gameObject,
            pressedAudioSource);
        RemoveLegacyButtonAnimations(contractRoot);
        return true;
    }

    private static RectTransform BuildVisualStateFromLegacyButton(
        Transform contractRoot,
        RectTransform visualRoot,
        string stateName,
        Button legacyButton)
    {
        RectTransform state = EnsureRectChild(visualRoot, stateName);
        ClearChildren(state);

        Graphic rootGraphic = legacyButton.targetGraphic != null
            && legacyButton.targetGraphic.transform == contractRoot
                ? legacyButton.targetGraphic
                : contractRoot.GetComponent<Graphic>();

        if (rootGraphic != null)
        {
            CopyGraphic(rootGraphic, state.gameObject);
        }

        List<Transform> childrenToMove = new();
        foreach (Transform child in contractRoot)
        {
            if (child == visualRoot || child.name == UiButtonContract.ButtonChildName)
            {
                continue;
            }

            if (child.GetComponent<AudioSource>() != null)
            {
                continue;
            }

            childrenToMove.Add(child);
        }

        foreach (Transform child in childrenToMove)
        {
            child.SetParent(state, worldPositionStays: false);
        }

        return state;
    }

    private static RectTransform DuplicateState(
        RectTransform sourceState,
        RectTransform visualRoot,
        string stateName,
        Sprite spriteOverride)
    {
        Transform existing = visualRoot.Find(stateName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject clone = UnityEngine.Object.Instantiate(sourceState.gameObject, visualRoot);
        clone.name = stateName;
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        StretchFullRect(cloneRect);
        ApplySpriteOverride(clone, spriteOverride);
        return cloneRect;
    }

    private static RectTransform EnsureRectChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            StretchFullRect(existingRect);
            return existingRect;
        }

        GameObject child = new(childName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, worldPositionStays: false);
        RectTransform rectTransform = child.GetComponent<RectTransform>();
        StretchFullRect(rectTransform);
        return rectTransform;
    }

    private static void StretchFullRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    private static void ConfigureHitbox(Image hitbox)
    {
        hitbox.color = new Color(1f, 1f, 1f, 0f);
        hitbox.raycastTarget = true;
        hitbox.sprite = null;
        hitbox.type = Image.Type.Simple;
        hitbox.preserveAspect = false;
    }

    private static void CopyGraphic(Graphic source, GameObject destination)
    {
        Type graphicType = source.GetType();
        Component copiedComponent = destination.GetComponent(graphicType);
        if (copiedComponent == null)
        {
            copiedComponent = destination.AddComponent(graphicType);
        }

        EditorUtility.CopySerialized(source, copiedComponent);
        if (copiedComponent is Graphic copiedGraphic)
        {
            copiedGraphic.raycastTarget = false;
        }
    }

    private static void ApplySpriteOverride(GameObject stateRoot, Sprite spriteOverride)
    {
        if (spriteOverride == null)
        {
            return;
        }

        Image targetImage = stateRoot.GetComponentsInChildren<Image>(includeInactive: true)
            .FirstOrDefault(image => image.color.a > 0f)
            ?? stateRoot.GetComponentInChildren<Image>(includeInactive: true);

        if (targetImage != null)
        {
            targetImage.sprite = spriteOverride;
        }
    }

    private static void RemovePersistentAudioSourcePlayCalls(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEngine.Object target = button.onClick.GetPersistentTarget(i);
            string methodName = button.onClick.GetPersistentMethodName(i);
            if (target is AudioSource && methodName == nameof(AudioSource.Play))
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }
    }

    private static void ConfigureVisualState(
        Button button,
        GameObject normalState,
        GameObject highlightedState,
        GameObject pressedState,
        AudioSource pressedAudioSource)
    {
        AudioSource resolvedPressedAudioSource = EnsurePressedAudioSource(button, pressedAudioSource);
        ButtonVisualState visualState = button.GetComponent<ButtonVisualState>();
        if (visualState == null)
        {
            visualState = button.gameObject.AddComponent<ButtonVisualState>();
        }

        SerializedObject serializedObject = new(visualState);
        SetObjectReference(serializedObject, "button", button);
        SetObjectReference(serializedObject, "normalState", normalState);
        SetObjectReference(serializedObject, "highlightedState", highlightedState);
        SetObjectReference(serializedObject, "pressedState", pressedState);
        SetObjectReference(serializedObject, "pressedAudioSource", resolvedPressedAudioSource);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AudioSource ConsolidatePressedAudioSource(
        Button button,
        IEnumerable<AudioSource> candidateSources)
    {
        List<AudioSource> sources = candidateSources?
            .Where(source => source != null)
            .OrderBy(source => source.transform == button.transform ? 0 : 1)
            .ThenBy(source => source.clip == null ? 1 : 0)
            .ToList()
            ?? new List<AudioSource>();

        AudioSource buttonAudioSource = button.GetComponent<AudioSource>();
        AudioSource sourceToCopy = sources.FirstOrDefault(source => source != buttonAudioSource);
        if (buttonAudioSource == null)
        {
            buttonAudioSource = button.gameObject.AddComponent<AudioSource>();
        }

        if (sourceToCopy != null)
        {
            EditorUtility.CopySerialized(sourceToCopy, buttonAudioSource);
        }

        foreach (AudioSource source in sources)
        {
            if (source == null || source == buttonAudioSource)
            {
                continue;
            }

            RemoveConsolidatedAudioSource(source, button.transform);
        }

        return EnsurePressedAudioSource(button, buttonAudioSource);
    }

    private static AudioSource EnsurePressedAudioSource(Button button, AudioSource existingAudioSource)
    {
        AudioSource audioSource = existingAudioSource != null
            ? existingAudioSource
            : button.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = button.gameObject.AddComponent<AudioSource>();
        }

        AudioClip defaultPressedSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultPressedSfxPath);
        if (audioSource.clip == null && defaultPressedSfx != null)
        {
            audioSource.clip = defaultPressedSfx;
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private static void RemoveConsolidatedAudioSource(AudioSource audioSource, Transform buttonTransform)
    {
        GameObject audioObject = audioSource.gameObject;
        if (audioObject.transform == buttonTransform)
        {
            UnityEngine.Object.DestroyImmediate(audioSource);
            return;
        }

        Component[] components = audioObject.GetComponents<Component>();
        bool onlyAudioAndTransform = components.All(component =>
            component == null
            || component is Transform
            || component is AudioSource);
        if (onlyAudioAndTransform && audioObject.transform.childCount == 0)
        {
            UnityEngine.Object.DestroyImmediate(audioObject);
            return;
        }

        UnityEngine.Object.DestroyImmediate(audioSource);
    }

    private static void DisableVisualRaycasts(Transform visualRoot)
    {
        foreach (Graphic graphic in visualRoot.GetComponentsInChildren<Graphic>(includeInactive: true))
        {
            graphic.raycastTarget = false;
        }
    }

    private static void RemoveLegacyButtonAnimations(Transform contractRoot)
    {
        foreach (MenuButtonAnimation animation in contractRoot.GetComponentsInChildren<MenuButtonAnimation>(includeInactive: true))
        {
            UnityEngine.Object.DestroyImmediate(animation);
        }
    }

    private static void RemoveRootVisualComponents(GameObject contractObject)
    {
        foreach (Graphic graphic in contractObject.GetComponents<Graphic>())
        {
            UnityEngine.Object.DestroyImmediate(graphic);
        }

        CanvasRenderer canvasRenderer = contractObject.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            UnityEngine.Object.DestroyImmediate(canvasRenderer);
        }
    }

    private static bool RepairManagerReferences(Transform root)
    {
        bool changed = false;

        foreach (PauseMenuManager manager in root.GetComponentsInChildren<PauseMenuManager>(includeInactive: true))
        {
            SerializedObject serializedObject = new(manager);
            Transform menuRoot = GetObjectReference<GameObject>(serializedObject, "menuRoot")?.transform
                ?? manager.transform.Find("PauseCanvas");
            changed |= SetObjectReference(serializedObject, "resumeButton", UiButtonContract.FindButton(menuRoot, "ReanudarBoton", "BotonReanudar"));
            changed |= SetObjectReference(serializedObject, "optionsButton", UiButtonContract.FindButton(menuRoot, "OpcionesBoton", "BotonOpciones"));
            changed |= SetObjectReference(serializedObject, "menuButton", UiButtonContract.FindButton(menuRoot, "MenuBoton", "BotonMenu"));
            changed |= SetObjectReference(serializedObject, "exitButton", UiButtonContract.FindButton(menuRoot, "SalirBoton", "BotonSalir"));
            changed |= SetObjectArray(serializedObject, "animatedButtons", UiButtonContract.FindButtonRootRects(menuRoot, "ReanudarBoton", "OpcionesBoton", "MenuBoton", "SalirBoton"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (GameOverMenuManager manager in root.GetComponentsInChildren<GameOverMenuManager>(includeInactive: true))
        {
            SerializedObject serializedObject = new(manager);
            Transform menuRoot = GetObjectReference<GameObject>(serializedObject, "menuRoot")?.transform
                ?? manager.transform.Find("GameOverCanvas");
            changed |= SetObjectReference(serializedObject, "retryButton", UiButtonContract.FindButton(menuRoot, "ReintentarBoton", "BotonReintentar"));
            changed |= SetObjectReference(serializedObject, "menuButton", UiButtonContract.FindButton(menuRoot, "MenuBoton", "BotonMenu"));
            changed |= SetObjectArray(serializedObject, "animatedButtons", UiButtonContract.FindButtonRootRects(menuRoot, "ReintentarBoton", "MenuBoton"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (InGameShopManager manager in root.GetComponentsInChildren<InGameShopManager>(includeInactive: true))
        {
            SerializedObject serializedObject = new(manager);
            Transform menuRoot = GetObjectReference<GameObject>(serializedObject, "menuRoot")?.transform
                ?? manager.transform.Find("InGameCanvas");
            changed |= SetObjectReference(serializedObject, "buyButton", UiButtonContract.FindButton(menuRoot, "ComprarBoton", "Comprar"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (OptionsMenuManager manager in root.GetComponentsInChildren<OptionsMenuManager>(includeInactive: true))
        {
            SerializedObject serializedObject = new(manager);
            Transform menuRoot = GetObjectReference<GameObject>(serializedObject, "menuRoot")?.transform
                ?? manager.transform.Find("Canvas");
            changed |= SetObjectReference(serializedObject, "backButton", UiButtonContract.FindButton(menuRoot, "VolverBoton", "BackBoton", "BackButton"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        return changed;
    }

    private static string ToContractRootName(string currentName)
    {
        string actionName = currentName?.Trim() ?? string.Empty;
        actionName = RemovePrefix(actionName, "Button");
        actionName = RemovePrefix(actionName, "Boton");
        actionName = RemoveSuffix(actionName, "Button");
        actionName = RemoveSuffix(actionName, "Boton");

        if (string.IsNullOrWhiteSpace(actionName))
        {
            actionName = "Accion";
        }

        return $"{actionName}Boton";
    }

    private static string RemovePrefix(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value.Substring(prefix.Length)
            : value;
    }

    private static string RemoveSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }

    private static T GetObjectReference<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.ObjectReference
            ? property.objectReferenceValue as T
            : null;
    }

    private static bool SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
        {
            return false;
        }

        if (property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }

    private static bool SetObjectArray(SerializedObject serializedObject, string propertyName, UnityEngine.Object[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return false;
        }

        values ??= Array.Empty<UnityEngine.Object>();
        bool changed = property.arraySize != values.Length;
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue != values[i])
            {
                element.objectReferenceValue = values[i];
                changed = true;
            }
        }

        return changed;
    }
}
