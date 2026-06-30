using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopMenuSetupUtility
{
    private const string ShopMenuScenePath = "Assets/Scenes/ShopMenu/ShopMenu.unity";
    private const string CanvasName = "Canvas";
    private const string PanelName = "Panel";
    private const string InteractionRootName = "ShopInteractionRoot";
    private const string DefaultPressedSfxPath = "Assets/Content/Audio/SFX/MainMenu/Splat.mp3";
    private const string PurchasedSkinStatusSpritePath = "Assets/Content/Art/UI/ShopMenu/Comprado.png";
    private const string EquippedSkinStatusSpritePath = "Assets/Content/Art/UI/ShopMenu/Seleccionado.png";
    private const int UiLayer = 5;

    private static readonly Vector2[] UpgradeSlotPositions =
    {
        new(-180f, 206f),
        new(-60f, 206f),
        new(60f, 206f),
        new(180f, 206f)
    };

    private static readonly Vector2[] SkinSlotPositions =
    {
        new(-180f, 72f),
        new(-60f, 72f),
        new(60f, 72f),
        new(180f, 72f)
    };

    [MenuItem("Tools/Squid/Shop/Setup ShopMenu Interaction")]
    public static void SetupShopMenuInteraction()
    {
        OptionsMenuPresenceUtility.InstallMissingGlobalOptionsMenus();
        Scene scene = EditorSceneManager.OpenScene(ShopMenuScenePath, OpenSceneMode.Single);
        GameObject canvas = FindSceneObjectByName(scene, CanvasName);
        GameObject panel = FindSceneObjectByName(scene, PanelName);
        if (canvas == null || panel == null)
        {
            throw new InvalidOperationException("[ShopMenuSetupUtility] ShopMenu requiere Canvas y Panel para serializar la interaccion.");
        }

        OutOfGameShopManager manager = canvas.GetComponent<OutOfGameShopManager>() ?? canvas.AddComponent<OutOfGameShopManager>();
        RectTransform interactionRoot = EnsureInteractionRoot(panel.transform);

        Button[] upgradeButtons = new Button[UpgradeSlotPositions.Length];
        for (int index = 0; index < upgradeButtons.Length; index++)
        {
            Button button = EnsureTransparentButton(
                interactionRoot,
                $"Upgrade{index + 1:00}Boton",
                UpgradeSlotPositions[index],
                new Vector2(108f, 112f));
            EnsureIntPersistentListener(button, manager, manager.SelectUpgradeSlot, index);
            upgradeButtons[index] = button;
        }

        Button[] skinButtons = new Button[SkinSlotPositions.Length];
        for (int index = 0; index < skinButtons.Length; index++)
        {
            Button button = EnsureTransparentButton(
                interactionRoot,
                $"Skin{index + 1:00}Boton",
                SkinSlotPositions[index],
                new Vector2(108f, 112f),
                includeSkinOwnershipStates: true);
            EnsureIntPersistentListener(button, manager, manager.SelectSkinSlot, index);
            skinButtons[index] = button;
        }

        Button previousSkinPageButton = EnsureTransparentButton(
            interactionRoot,
            "SkinAnteriorBoton",
            new Vector2(-300f, 72f),
            new Vector2(64f, 112f));
        EnsurePersistentListener(previousSkinPageButton, manager, manager.PreviousSkinPage);

        Button nextSkinPageButton = EnsureTransparentButton(
            interactionRoot,
            "SkinSiguienteBoton",
            new Vector2(300f, 72f),
            new Vector2(64f, 112f));
        EnsurePersistentListener(nextSkinPageButton, manager, manager.NextSkinPage);

        Button purchaseButton = UiButtonContract.FindButton(panel.transform, "ComprarBoton");
        if (purchaseButton == null)
        {
            throw new InvalidOperationException("[ShopMenuSetupUtility] No se encontro ComprarBoton/Button.");
        }

        EnsurePersistentListener(purchaseButton, manager, manager.PurchaseSelected);
        ConfigureManager(manager, upgradeButtons, skinButtons, previousSkinPageButton, nextSkinPageButton, purchaseButton);

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopMenuSetupUtility] Controles transparentes y referencias de ShopMenu serializados.");
    }

    [MenuItem("Tools/Squid/Shop/Setup Skin Status Visuals")]
    public static void SetupSkinStatusVisuals()
    {
        Scene scene = EditorSceneManager.OpenScene(ShopMenuScenePath, OpenSceneMode.Single);
        for (int index = 0; index < SkinSlotPositions.Length; index++)
        {
            GameObject skinSlot = FindSceneObjectByName(scene, $"Skin{index + 1:00}Boton");
            if (skinSlot == null)
            {
                throw new InvalidOperationException($"[ShopMenuSetupUtility] No se encontro Skin{index + 1:00}Boton.");
            }

            Transform visualTransform = skinSlot.transform.Find(UiButtonContract.VisualChildName);
            if (visualTransform == null)
            {
                throw new InvalidOperationException($"[ShopMenuSetupUtility] Skin{index + 1:00}Boton no contiene Visual.");
            }

            EnsureSkinStatusVisual(
                visualTransform,
                UiButtonContract.PurchasedStateName,
                UiButtonContract.LegacyBuyedStateName,
                PurchasedSkinStatusSpritePath);
            EnsureSkinStatusVisual(
                visualTransform,
                UiButtonContract.EquippedStateName,
                UiButtonContract.LegacySelectedStateName,
                EquippedSkinStatusSpritePath);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopMenuSetupUtility] Skin status visuals serialized.");
    }

    private static RectTransform EnsureInteractionRoot(Transform panel)
    {
        Transform existing = panel.Find(InteractionRootName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject root = new GameObject(InteractionRootName, typeof(RectTransform));
        root.layer = UiLayer;
        RectTransform rectTransform = root.GetComponent<RectTransform>();
        rectTransform.SetParent(panel, false);
        Stretch(rectTransform);
        return rectTransform;
    }

    private static Button EnsureTransparentButton(
        Transform parent,
        string ownerName,
        Vector2 position,
        Vector2 size,
        bool includeSkinOwnershipStates = false)
    {
        Transform ownerTransform = parent.Find(ownerName);
        bool ownerCreated = ownerTransform == null;
        if (ownerCreated)
        {
            GameObject owner = new GameObject(ownerName, typeof(RectTransform));
            owner.layer = UiLayer;
            ownerTransform = owner.transform;
            ownerTransform.SetParent(parent, false);

            RectTransform ownerRect = owner.GetComponent<RectTransform>();
            ownerRect.anchorMin = new Vector2(0.5f, 0.5f);
            ownerRect.anchorMax = new Vector2(0.5f, 0.5f);
            ownerRect.pivot = new Vector2(0.5f, 0.5f);
            ownerRect.anchoredPosition = position;
            ownerRect.sizeDelta = size;
        }

        Transform buttonTransform = ownerTransform.Find(UiButtonContract.ButtonChildName);
        if (buttonTransform == null)
        {
            GameObject buttonObject = new GameObject(
                UiButtonContract.ButtonChildName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(ButtonVisualState));
            buttonObject.layer = UiLayer;
            buttonTransform = buttonObject.transform;
            buttonTransform.SetParent(ownerTransform, false);
            Stretch(buttonObject.GetComponent<RectTransform>());
        }

        Transform visualTransform = ownerTransform.Find(UiButtonContract.VisualChildName);
        if (visualTransform == null)
        {
            GameObject visualObject = new GameObject(UiButtonContract.VisualChildName, typeof(RectTransform));
            visualObject.layer = UiLayer;
            visualTransform = visualObject.transform;
            visualTransform.SetParent(ownerTransform, false);
            Stretch(visualObject.GetComponent<RectTransform>());
        }

        EnsureVisualState(visualTransform, UiButtonContract.NormalStateName);
        EnsureVisualState(visualTransform, UiButtonContract.HighlightedStateName);
        EnsureVisualState(visualTransform, UiButtonContract.PressedStateName);
        if (includeSkinOwnershipStates)
        {
            EnsureSkinStatusVisual(
                visualTransform,
                UiButtonContract.PurchasedStateName,
                UiButtonContract.LegacyBuyedStateName,
                PurchasedSkinStatusSpritePath);
            EnsureSkinStatusVisual(
                visualTransform,
                UiButtonContract.EquippedStateName,
                UiButtonContract.LegacySelectedStateName,
                EquippedSkinStatusSpritePath);
        }

        Button button = buttonTransform.GetComponent<Button>();
        Image targetGraphic = buttonTransform.GetComponent<Image>();
        targetGraphic.color = new Color(1f, 1f, 1f, 0f);
        targetGraphic.raycastTarget = true;
        button.targetGraphic = targetGraphic;
        button.transition = Selectable.Transition.None;

        SerializedObject buttonVisualState = new(buttonTransform.GetComponent<ButtonVisualState>());
        buttonVisualState.FindProperty("button").objectReferenceValue = button;
        buttonVisualState.FindProperty("normalState").objectReferenceValue = visualTransform.Find(UiButtonContract.NormalStateName).gameObject;
        buttonVisualState.FindProperty("highlightedState").objectReferenceValue = visualTransform.Find(UiButtonContract.HighlightedStateName).gameObject;
        buttonVisualState.FindProperty("pressedState").objectReferenceValue = visualTransform.Find(UiButtonContract.PressedStateName).gameObject;
        ConfigurePressedSfx(buttonTransform.gameObject, buttonVisualState);
        buttonVisualState.ApplyModifiedPropertiesWithoutUndo();

        if (ownerCreated)
        {
            EditorUtility.SetDirty(ownerTransform.gameObject);
        }

        return button;
    }

    private static void ConfigurePressedSfx(GameObject buttonObject, SerializedObject buttonVisualState)
    {
        AudioClip pressedSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultPressedSfxPath);
        if (pressedSfx == null)
        {
            throw new InvalidOperationException($"[ShopMenuSetupUtility] No se encontro el SFX de presion en {DefaultPressedSfxPath}.");
        }

        AudioSource audioSource = buttonObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = buttonObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        buttonVisualState.FindProperty("pressedAudioSource").objectReferenceValue = audioSource;
        buttonVisualState.FindProperty("pressedSfx").objectReferenceValue = pressedSfx;
        EditorUtility.SetDirty(audioSource);
        EditorUtility.SetDirty(buttonVisualState.targetObject);
    }

    private static void EnsureVisualState(Transform visualRoot, string stateName)
    {
        if (visualRoot.Find(stateName) != null)
        {
            return;
        }

        GameObject state = new GameObject(stateName, typeof(RectTransform));
        state.layer = UiLayer;
        state.transform.SetParent(visualRoot, false);
        Stretch(state.GetComponent<RectTransform>());
    }

    private static void EnsureSkinStatusVisual(
        Transform visualRoot,
        string stateName,
        string legacyStateName,
        string spritePath)
    {
        Transform stateTransform = visualRoot.Find(stateName);
        if (stateTransform == null)
        {
            stateTransform = visualRoot.Find(legacyStateName);
            if (stateTransform != null)
            {
                stateTransform.name = stateName;
            }
        }

        if (stateTransform == null)
        {
            GameObject state = new GameObject(
                stateName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            state.layer = UiLayer;
            state.transform.SetParent(visualRoot, false);
            RectTransform rectTransform = state.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(108f, 36f);
            stateTransform = state.transform;
        }

        Image image = stateTransform.GetComponent<Image>();
        if (image == null)
        {
            image = stateTransform.gameObject.AddComponent<Image>();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            throw new InvalidOperationException($"[ShopMenuSetupUtility] No se encontro el sprite de estado de skin en {spritePath}.");
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        stateTransform.SetAsLastSibling();
        stateTransform.gameObject.SetActive(false);
        EditorUtility.SetDirty(stateTransform.gameObject);
    }

    private static void ConfigureManager(
        OutOfGameShopManager manager,
        Button[] upgradeButtons,
        Button[] skinButtons,
        Button previousSkinPageButton,
        Button nextSkinPageButton,
        Button purchaseButton)
    {
        SerializedObject serializedObject = new(manager);
        SetButtonArray(serializedObject.FindProperty("upgradeSlotButtons"), upgradeButtons);
        SetButtonArray(serializedObject.FindProperty("skinSlotButtons"), skinButtons);
        serializedObject.FindProperty("previousSkinPageButton").objectReferenceValue = previousSkinPageButton;
        serializedObject.FindProperty("nextSkinPageButton").objectReferenceValue = nextSkinPageButton;
        serializedObject.FindProperty("purchaseButton").objectReferenceValue = purchaseButton;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetButtonArray(SerializedProperty property, Button[] buttons)
    {
        property.arraySize = buttons.Length;
        for (int index = 0; index < buttons.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = buttons[index];
        }
    }

    private static void EnsurePersistentListener(Button button, OutOfGameShopManager manager, UnityAction action)
    {
        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentTarget(index) == manager
                && button.onClick.GetPersistentMethodName(index) == action.Method.Name)
            {
                return;
            }
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static void EnsureIntPersistentListener(Button button, OutOfGameShopManager manager, UnityAction<int> action, int argument)
    {
        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentTarget(index) == manager
                && button.onClick.GetPersistentMethodName(index) == action.Method.Name)
            {
                return;
            }
        }

        UnityEventTools.AddIntPersistentListener(button.onClick, action, argument);
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform transform in transforms)
            {
                if (transform.name == objectName)
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }
}
