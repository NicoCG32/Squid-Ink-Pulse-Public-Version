using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LoreComicSetupUtility
{
    private const string PrefabPath = "Assets/Content/Prefabs/UI/Menus/LoreComic.prefab";
    private const string ComicArtRoot = "Assets/Content/Art/ComicLore";
    private const string StartFolder = ComicArtRoot + "/Inicio";
    private const string PortalsFolder = ComicArtRoot + "/Portales";
    private const string EpipelagicDefeatFolder = ComicArtRoot + "/Derrota/Epipelagica";
    private const string AbyssopelagicDefeatFolder = ComicArtRoot + "/Derrota/Abisopelagica";
    private const string ShopFolder = ComicArtRoot + "/Tienda";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string EpipelagicGameRootPath = "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab";
    private const string AbyssopelagicGameRootPath = "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab";
    private const string TutorialGameRootPath = "Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaTutorial.prefab";
    private const string PressedSfxPath = "Assets/Content/Audio/SFX/MainMenu/Splat.mp3";

    [MenuItem("Tools/Squid/Lore/Setup Lore Comic")]
    public static void SetupLoreComics()
    {
        EnsureFolders();
        Dictionary<string, Sprite> sprites = EnsureComicSprites();
        GameObject prefab = EnsureLoreComicPrefab(sprites);

        int changedAssets = 0;
        changedAssets += InstallInMainMenuScene(prefab);
        changedAssets += InstallInGameRootPrefab(EpipelagicGameRootPath, prefab);
        changedAssets += InstallInGameRootPrefab(AbyssopelagicGameRootPath, prefab);
        changedAssets += InstallInGameRootPrefab(TutorialGameRootPath, prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LoreComicSetupUtility] Lore comic setup finished. Changed assets: {changedAssets}.");
    }

    public static void SetupLoreComicsBatch()
    {
        SetupLoreComics();
    }

    [MenuItem("Tools/Squid/Lore/Repair Lore Comic Visibility")]
    public static void RepairLoreComicVisibility()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            Transform comic = root.transform.Find("Comic");
            if (comic == null || !comic.TryGetComponent(out RectTransform comicRect))
            {
                throw new InvalidOperationException("[LoreComicSetupUtility] LoreComic requiere el nodo Comic con RectTransform.");
            }

            if (comicRect.localScale == Vector3.one)
            {
                return;
            }

            // Comic is a Screen Space Overlay canvas. A zero scale prevents every comic from rendering.
            comicRect.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[LoreComicSetupUtility] LoreComic visibility repaired without changing its art or layout.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Content/Art", "ComicLore");
        EnsureFolder(ComicArtRoot, "Inicio");
        EnsureFolder(ComicArtRoot, "Portales");
        EnsureFolder(ComicArtRoot, "Derrota");
        EnsureFolder(ComicArtRoot + "/Derrota", "Epipelagica");
        EnsureFolder(ComicArtRoot + "/Derrota", "Abisopelagica");
        EnsureFolder(ComicArtRoot, "Tienda");
        EnsureFolder("Assets/Content/Prefabs/UI", "Menus");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Dictionary<string, Sprite> EnsureComicSprites()
    {
        Dictionary<string, Color32> colors = new()
        {
            { "Comic_Inicio", new Color32(233, 238, 245, 255) },
            { "Comic_Portal_Epi_Abi", new Color32(78, 174, 204, 255) },
            { "Comic_Portal_Abi_Epi", new Color32(48, 92, 144, 255) },
            { "Comic_Derrota_Epi_01", new Color32(199, 95, 111, 255) },
            { "Comic_Derrota_Epi_02", new Color32(214, 128, 102, 255) },
            { "Comic_Derrota_Epi_03", new Color32(184, 83, 127, 255) },
            { "Comic_Derrota_Abi_01", new Color32(54, 72, 129, 255) },
            { "Comic_Derrota_Abi_02", new Color32(84, 65, 137, 255) },
            { "Comic_Derrota_Abi_03", new Color32(40, 104, 118, 255) },
            { "Comic_ShopInGameFirst", new Color32(71, 128, 120, 255) },
            { "Comic_ShopInGameLastPurchased", new Color32(101, 92, 154, 255) },
            { "Comic_ShopInGameLastNoPurchase", new Color32(118, 123, 139, 255) }
        };

        Dictionary<string, string> paths = new()
        {
            { "Comic_Inicio", $"{StartFolder}/Comic_Inicio.png" },
            { "Comic_Portal_Epi_Abi", $"{PortalsFolder}/Comic_Portal_Epi_Abi.png" },
            { "Comic_Portal_Abi_Epi", $"{PortalsFolder}/Comic_Portal_Abi_Epi.png" },
            { "Comic_Derrota_Epi_01", $"{EpipelagicDefeatFolder}/Comic_Derrota_Epi_01.png" },
            { "Comic_Derrota_Epi_02", $"{EpipelagicDefeatFolder}/Comic_Derrota_Epi_02.png" },
            { "Comic_Derrota_Epi_03", $"{EpipelagicDefeatFolder}/Comic_Derrota_Epi_03.png" },
            { "Comic_Derrota_Abi_01", $"{AbyssopelagicDefeatFolder}/Comic_Derrota_Abi_01.png" },
            { "Comic_Derrota_Abi_02", $"{AbyssopelagicDefeatFolder}/Comic_Derrota_Abi_02.png" },
            { "Comic_Derrota_Abi_03", $"{AbyssopelagicDefeatFolder}/Comic_Derrota_Abi_03.png" },
            { "Comic_ShopInGameFirst", $"{ShopFolder}/Comic_ShopInGameFirst.png" },
            { "Comic_ShopInGameLastPurchased", $"{ShopFolder}/Comic_ShopInGameLastPurchased.png" },
            { "Comic_ShopInGameLastNoPurchase", $"{ShopFolder}/Comic_ShopInGameLastNoPurchase.png" }
        };

        Dictionary<string, Sprite> sprites = new();
        foreach (KeyValuePair<string, string> comic in paths)
        {
            string assetPath = comic.Value;
            if (!File.Exists(assetPath))
            {
                CreatePlaceholderPng(assetPath, colors[comic.Key]);
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100f;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                sprites[comic.Key] = sprite;
            }
        }

        return sprites;
    }

    private static void CreatePlaceholderPng(string assetPath, Color32 baseColor)
    {
        const int width = 160;
        const int height = 90;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32 borderColor = new Color32(255, 255, 255, 255);
        Color32 shadowColor = new Color32(
            (byte)Mathf.RoundToInt(baseColor.r * 0.55f),
            (byte)Mathf.RoundToInt(baseColor.g * 0.55f),
            (byte)Mathf.RoundToInt(baseColor.b * 0.55f),
            255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool border = x < 4 || x >= width - 4 || y < 4 || y >= height - 4;
                bool innerPanel = x > 16 && x < width - 16 && y > 14 && y < height - 14;
                texture.SetPixel(x, y, border ? borderColor : innerPanel ? baseColor : shadowColor);
            }
        }

        texture.Apply();
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath);
    }

    private static GameObject EnsureLoreComicPrefab(Dictionary<string, Sprite> sprites)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        bool loadedPrefabContents = prefab != null;
        GameObject root = loadedPrefabContents ? PrefabUtility.LoadPrefabContents(PrefabPath) : CreateLoreComicHierarchy();

        try
        {
            ConfigureLoreComicHierarchy(root, sprites);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            if (loadedPrefabContents)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static GameObject CreateLoreComicHierarchy()
    {
        GameObject root = new GameObject("LoreComicRoot", typeof(RectTransform), typeof(LoreComicPresenter));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        GameObject comic = new GameObject("Comic", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        comic.transform.SetParent(root.transform, false);
        Stretch(comic.GetComponent<RectTransform>());

        GameObject dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimmer.transform.SetParent(comic.transform, false);
        Stretch(dimmer.GetComponent<RectTransform>());

        GameObject vignette = new GameObject("Vineta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vignette.transform.SetParent(comic.transform, false);
        RectTransform vignetteRect = vignette.GetComponent<RectTransform>();
        vignetteRect.anchorMin = new Vector2(0.5f, 0.5f);
        vignetteRect.anchorMax = new Vector2(0.5f, 0.5f);
        vignetteRect.pivot = new Vector2(0.5f, 0.5f);
        vignetteRect.anchoredPosition = Vector2.zero;
        vignetteRect.sizeDelta = new Vector2(1280f, 720f);

        GameObject buttonRoot = new GameObject("ContinuarBoton", typeof(RectTransform));
        buttonRoot.transform.SetParent(comic.transform, false);
        RectTransform buttonRootRect = buttonRoot.GetComponent<RectTransform>();
        buttonRootRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRootRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRootRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRootRect.anchoredPosition = new Vector2(0f, 118f);
        buttonRootRect.sizeDelta = new Vector2(260f, 92f);

        GameObject button = new GameObject(UiButtonContract.ButtonChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonVisualState), typeof(AudioSource));
        button.transform.SetParent(buttonRoot.transform, false);
        Stretch(button.GetComponent<RectTransform>());

        GameObject visual = new GameObject(UiButtonContract.VisualChildName, typeof(RectTransform));
        visual.transform.SetParent(buttonRoot.transform, false);
        Stretch(visual.GetComponent<RectTransform>());

        CreateButtonVisualState(visual.transform, UiButtonContract.NormalStateName);
        CreateButtonVisualState(visual.transform, UiButtonContract.HighlightedStateName);
        CreateButtonVisualState(visual.transform, UiButtonContract.PressedStateName);

        return root;
    }

    private static void ConfigureLoreComicHierarchy(GameObject root, Dictionary<string, Sprite> sprites)
    {
        root.name = "LoreComicRoot";
        LoreComicPresenter presenter = root.GetComponent<LoreComicPresenter>() ?? root.AddComponent<LoreComicPresenter>();

        Transform comic = FindOrCreateChild(root.transform, "Comic", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        comic.localScale = Vector3.one;
        Canvas canvas = comic.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = comic.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = comic.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Transform dimmer = FindOrCreateChild(comic, "Dimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Stretch(dimmer.GetComponent<RectTransform>());
        Image dimmerImage = dimmer.GetComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.76f);
        dimmerImage.raycastTarget = true;

        Transform vignette = FindOrCreateChild(comic, "Vineta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform vignetteRect = vignette.GetComponent<RectTransform>();
        vignetteRect.anchorMin = new Vector2(0.5f, 0.5f);
        vignetteRect.anchorMax = new Vector2(0.5f, 0.5f);
        vignetteRect.pivot = new Vector2(0.5f, 0.5f);
        vignetteRect.anchoredPosition = Vector2.zero;
        vignetteRect.sizeDelta = new Vector2(1280f, 720f);
        Image vignetteImage = vignette.GetComponent<Image>();
        vignetteImage.preserveAspect = true;
        vignetteImage.color = Color.white;
        vignetteImage.raycastTarget = false;

        Transform buttonRoot = FindOrCreateChild(comic, "ContinuarBoton", typeof(RectTransform));
        RectTransform buttonRootRect = buttonRoot.GetComponent<RectTransform>();
        buttonRootRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRootRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRootRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRootRect.anchoredPosition = new Vector2(0f, 118f);
        buttonRootRect.sizeDelta = new Vector2(260f, 92f);

        Transform buttonTransform = FindOrCreateChild(buttonRoot, UiButtonContract.ButtonChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonVisualState), typeof(AudioSource));
        Stretch(buttonTransform.GetComponent<RectTransform>());
        Image buttonImage = buttonTransform.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);
        buttonImage.raycastTarget = true;

        Button button = buttonTransform.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityEventTools.AddPersistentListener(button.onClick, presenter.Continue);

        Transform visual = FindOrCreateChild(buttonRoot, UiButtonContract.VisualChildName, typeof(RectTransform));
        Stretch(visual.GetComponent<RectTransform>());
        Image normal = EnsureButtonState(visual, UiButtonContract.NormalStateName, new Color(1f, 1f, 1f, 1f));
        Image highlighted = EnsureButtonState(visual, UiButtonContract.HighlightedStateName, new Color(1f, 1f, 1f, 1f));
        Image pressed = EnsureButtonState(visual, UiButtonContract.PressedStateName, new Color(1f, 1f, 1f, 1f));
        highlighted.gameObject.SetActive(false);
        pressed.gameObject.SetActive(false);

        AudioSource audioSource = buttonTransform.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PressedSfxPath);

        ButtonVisualState visualState = buttonTransform.GetComponent<ButtonVisualState>();
        SerializedObject visualStateObject = new SerializedObject(visualState);
        visualStateObject.FindProperty("button").objectReferenceValue = button;
        visualStateObject.FindProperty("normalState").objectReferenceValue = normal.gameObject;
        visualStateObject.FindProperty("highlightedState").objectReferenceValue = highlighted.gameObject;
        visualStateObject.FindProperty("pressedState").objectReferenceValue = pressed.gameObject;
        visualStateObject.FindProperty("pressedAudioSource").objectReferenceValue = audioSource;
        visualStateObject.FindProperty("pressedSfx").objectReferenceValue = audioSource.clip;
        visualStateObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject presenterObject = new SerializedObject(presenter);
        presenterObject.FindProperty("comicRoot").objectReferenceValue = comic.gameObject;
        presenterObject.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        presenterObject.FindProperty("comicImage").objectReferenceValue = vignetteImage;
        presenterObject.FindProperty("continueButton").objectReferenceValue = button;
        presenterObject.FindProperty("continueButtonRoot").objectReferenceValue = buttonRoot.gameObject;
        presenterObject.FindProperty("defaultDisplaySeconds").floatValue = 3f;
        presenterObject.FindProperty("defaultStartDisplaySeconds").floatValue = 0f;
        presenterObject.FindProperty("defaultStartWaitsForContinue").boolValue = true;
        presenterObject.FindProperty("defaultStartShowsContinueButton").boolValue = true;
        presenterObject.FindProperty("pauseTimeWhileShowing").boolValue = true;
        presenterObject.FindProperty("allowExternalContinueWithoutButton").boolValue = false;
        presenterObject.FindProperty("hideOnAwake").boolValue = true;
        ConfigureEntries(presenterObject.FindProperty("entries"), sprites);
        presenterObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEntries(SerializedProperty entries, Dictionary<string, Sprite> sprites)
    {
        entries.arraySize = 8;
        SetEntry(entries.GetArrayElementAtIndex(0), LoreComicEvent.GameStart, LoreComicZone.Unknown, 0f, true, true, SpriteArray(sprites, "Comic_Inicio"));
        SetEntry(entries.GetArrayElementAtIndex(1), LoreComicEvent.PortalEpipelagicToAbyssopelagic, LoreComicZone.Abyssopelagic, 3f, false, false, SpriteArray(sprites, "Comic_Portal_Epi_Abi"));
        SetEntry(entries.GetArrayElementAtIndex(2), LoreComicEvent.PortalAbyssopelagicToEpipelagic, LoreComicZone.Epipelagic, 3f, false, false, SpriteArray(sprites, "Comic_Portal_Abi_Epi"));
        SetEntry(entries.GetArrayElementAtIndex(3), LoreComicEvent.Defeat, LoreComicZone.Epipelagic, 3f, false, false, SpriteArray(sprites, "Comic_Derrota_Epi_01", "Comic_Derrota_Epi_02", "Comic_Derrota_Epi_03"));
        SetEntry(entries.GetArrayElementAtIndex(4), LoreComicEvent.Defeat, LoreComicZone.Abyssopelagic, 3f, false, false, SpriteArray(sprites, "Comic_Derrota_Abi_01", "Comic_Derrota_Abi_02", "Comic_Derrota_Abi_03"));
        SetEntry(entries.GetArrayElementAtIndex(5), LoreComicEvent.ShopInGameFirst, LoreComicZone.Unknown, 3f, false, false, SpriteArray(sprites, "Comic_ShopInGameFirst"));
        SetEntry(entries.GetArrayElementAtIndex(6), LoreComicEvent.ShopInGameLastPurchased, LoreComicZone.Unknown, 3f, false, false, SpriteArray(sprites, "Comic_ShopInGameLastPurchased"));
        SetEntry(entries.GetArrayElementAtIndex(7), LoreComicEvent.ShopInGameLastNoPurchase, LoreComicZone.Unknown, 3f, false, false, SpriteArray(sprites, "Comic_ShopInGameLastNoPurchase"));
    }

    private static Sprite[] SpriteArray(Dictionary<string, Sprite> sprites, params string[] keys)
    {
        Sprite[] result = new Sprite[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            sprites.TryGetValue(keys[i], out result[i]);
        }

        return result;
    }

    private static void SetEntry(SerializedProperty entry, LoreComicEvent comicEvent, LoreComicZone zone, float displaySeconds, bool waitForContinue, bool showContinueButton, Sprite[] sprites)
    {
        entry.FindPropertyRelative("comicEvent").intValue = (int)comicEvent;
        entry.FindPropertyRelative("zone").intValue = (int)zone;
        entry.FindPropertyRelative("displaySeconds").floatValue = displaySeconds;
        entry.FindPropertyRelative("waitForContinue").boolValue = waitForContinue;
        entry.FindPropertyRelative("showContinueButton").boolValue = showContinueButton;

        SerializedProperty spriteArray = entry.FindPropertyRelative("sprites");
        spriteArray.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            spriteArray.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    private static int InstallInMainMenuScene(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        bool changed = false;

        LoreComicPresenter presenter = FindSceneComponent<LoreComicPresenter>(scene);
        if (presenter == null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance != null)
            {
                instance.name = "LoreComicRoot";
                changed = true;
            }
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MainMenu[] menus = root.GetComponentsInChildren<MainMenu>(includeInactive: true);
            foreach (MainMenu menu in menus)
            {
                SerializedObject menuObject = new SerializedObject(menu);
                SerializedProperty property = menuObject.FindProperty("showStartLoreComicBeforePlay");
                if (property != null && !property.boolValue)
                {
                    property.boolValue = true;
                    menuObject.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return 1;
        }

        return 0;
    }

    private static int InstallInGameRootPrefab(string gameRootPrefabPath, GameObject loreComicPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(gameRootPrefabPath);
        bool changed = false;

        try
        {
            Transform uiRoot = FindDeepChild(root.transform, "GameUIRoot") ?? root.transform;
            LoreComicPresenter presenter = root.GetComponentInChildren<LoreComicPresenter>(includeInactive: true);
            if (presenter == null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(loreComicPrefab, uiRoot) as GameObject;
                if (instance != null)
                {
                    instance.name = "LoreComicRoot";
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, gameRootPrefabPath);
                return 1;
            }

            return 0;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform FindOrCreateChild(Transform parent, string childName, params Type[] componentTypes)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            EnsureComponents(existing.gameObject, componentTypes);
            return existing;
        }

        GameObject child = new GameObject(childName, componentTypes);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void EnsureComponents(GameObject gameObject, params Type[] componentTypes)
    {
        foreach (Type componentType in componentTypes)
        {
            if (gameObject.GetComponent(componentType) == null)
            {
                gameObject.AddComponent(componentType);
            }
        }
    }

    private static void CreateButtonVisualState(Transform parent, string stateName)
    {
        GameObject state = new GameObject(stateName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        state.transform.SetParent(parent, false);
        Stretch(state.GetComponent<RectTransform>());
        state.GetComponent<Image>().color = Color.white;
    }

    private static Image EnsureButtonState(Transform visualRoot, string stateName, Color color)
    {
        Transform state = FindOrCreateChild(visualRoot, stateName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Stretch(state.GetComponent<RectTransform>());
        Image image = state.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(includeInactive: true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
