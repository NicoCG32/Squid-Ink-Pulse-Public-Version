using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneCompositionValidator : IPreprocessBuildWithReport
{
    private const string MenuPath = "Tools/Squid Ink Pulse/Validate Scene Composition";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string EpipelagicScenePath = "Assets/Scenes/Game/ZonaEpipelagica.unity";
    private const string AbyssopelagicScenePath = "Assets/Scenes/Game/ZonaAbisopelagica.unity";
    private const string ShopMenuScenePath = "Assets/Scenes/ShopMenu/ShopMenu.unity";
    private const string OptionsMenuPrefabPath = "Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab";
    private const string TouchControlsPrefabPath =
        "Assets/Content/Prefabs/UI/Touch/TouchControls.prefab";
    private const string EpipelagicGameRootPrefabPath =
        MobileGameRootCatalog.CanonicalGameRootPath;
    private const string AbyssopelagicGameRootPrefabPath =
        MobileGameRootCatalog.AbyssopelagicGameRootPath;
    private const string TutorialGameRootPrefabPath =
        MobileGameRootCatalog.TutorialGameRootPath;

    private static readonly string[] ExpectedBuildScenes =
    {
        MainMenuScenePath,
        EpipelagicScenePath,
        AbyssopelagicScenePath,
        ShopMenuScenePath
    };

    private static readonly string[] RequiredProductGameRootPrefabPaths =
    {
        EpipelagicGameRootPrefabPath,
        AbyssopelagicGameRootPrefabPath
    };

    private static readonly string[] FixedCanonicalPrefabPaths =
    {
        TouchControlsPrefabPath,
        OptionsMenuPrefabPath,
        "Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab",
        "Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab",
        "Assets/Content/Prefabs/Player/BabySquid.prefab",
        "Assets/Content/Prefabs/World/CleanUp.prefab",
        "Assets/Content/Prefabs/Portals/ScenePortal.prefab",
        "Assets/Content/Prefabs/Shop/DealerFish.prefab",
        "Assets/Content/Prefabs/Shop/DealerFish_ZonaAbisopelagica.prefab"
    };

    private static readonly string[] ExpectedHudSafeAreaChildren =
    {
        "GadgetSlots",
        "InkBar",
        "Score",
        "ShrimpCounter",
        "TouchControlsOwner"
    };

    public int callbackOrder => -80;

    public void OnPreprocessBuild(BuildReport report)
    {
        ValidateSceneComposition();
    }

    [MenuItem(MenuPath)]
    public static void ValidateSceneComposition()
    {
        List<string> failures = new();

        ValidateBuildSettings(failures);
        ValidateCanonicalPrefabs(failures);
        ValidateBuildScenes(failures);

        if (failures.Count > 0)
        {
            string message = "[SceneCompositionValidator] Fallo la validacion de composicion:\n- "
                + string.Join("\n- ", failures);
            throw new BuildFailedException(message);
        }

        Debug.Log("[SceneCompositionValidator] Composicion de escenas y prefabs canonicos validada.");
    }

    private static void ValidateBuildSettings(List<string> failures)
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (!enabledScenes.SequenceEqual(ExpectedBuildScenes))
        {
            failures.Add("EditorBuildSettings debe contener exactamente MainMenu, ZonaEpipelagica, ZonaAbisopelagica y ShopMenu en ese orden.");
        }
    }

    private static void ValidateCanonicalPrefabs(List<string> failures)
    {
        string[] productGameRootPrefabPaths = MobileGameRootCatalog.ProductGameRootPaths;
        foreach (string requiredPath in RequiredProductGameRootPrefabPaths)
        {
            if (!productGameRootPrefabPaths.Contains(requiredPath, StringComparer.Ordinal))
            {
                failures.Add($"GameRoot de producto requerido no encontrado o excluido del contrato movil: {requiredPath}");
            }
        }

        foreach (string prefabPath in productGameRootPrefabPaths
            .Concat(FixedCanonicalPrefabPaths)
            .Distinct(StringComparer.Ordinal))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add($"Prefab canonico no encontrado: {prefabPath}");
                continue;
            }

            ValidateMissingScripts(prefab, prefabPath, failures);
            if (prefabPath == OptionsMenuPrefabPath)
                ValidateOptionsMenu(prefab.GetComponentInChildren<OptionsMenuManager>(true),failures);
            if (prefabPath == "Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab"
                || prefabPath == "Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab")
                RunMenuMobileCompositionContract.Validate(prefab, prefabPath.EndsWith("/PauseMenu.prefab"), failures);

            if (productGameRootPrefabPaths.Contains(prefabPath, StringComparer.Ordinal))
            {
                ValidateMobileGameRootComposition(prefab, prefabPath, failures);
            }
        }

        GameObject tutorialRoot = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialGameRootPrefabPath);
        if (tutorialRoot != null
            && (tutorialRoot.GetComponentsInChildren<TouchGameplayControlsController>(true).Length > 0
                || tutorialRoot.GetComponentsInChildren<TouchControlsVisibilityController>(true).Length > 0
                || tutorialRoot.GetComponentsInChildren<TouchSteeringSurface>(true).Length > 0
                || tutorialRoot.GetComponentsInChildren<TouchGameplayCommandButton>(true).Length > 0))
        {
            failures.Add("GameRoot_ZonaTutorial debe permanecer sin controles touch durante el port de las zonas activas.");
        }
    }

    private static void ValidateBuildScenes(List<string> failures)
    {
        foreach (string scenePath in ExpectedBuildScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateMissingScripts(scene, failures);
            OptionsMenuManager options = RequireSingle<OptionsMenuManager>(scene,"OptionsMenuManager",failures);
            if (options != null) ValidateOptionsMenu(options,failures);

            if (scenePath == EpipelagicScenePath)
            {
                ValidateGameplayScene(scene, "ZonaEpipelagica", expectedBossPrefabName: "SSCarnage", failures);
            }
            else if (scenePath == AbyssopelagicScenePath)
            {
                ValidateGameplayScene(scene, "ZonaAbisopelagica", expectedBossPrefabName: "UnknownBoss", failures);
            }
            else if (scenePath == MainMenuScenePath)
            {
                MainMenu mainMenu = RequireSingle<MainMenu>(scene, "MainMenu", failures);
                if (mainMenu != null)
                {
                    ValidateMainMenu(mainMenu, failures);
                }

                RequireSingle<EventSystem>(scene, "EventSystem", failures);
            }
            else if (scenePath == ShopMenuScenePath)
            {
                OutOfGameShopManager shopManager = RequireSingle<OutOfGameShopManager>(scene, "OutOfGameShopManager", failures);
                if (shopManager != null)
                {
                    ValidateShopMenu(shopManager, failures);
                }

                RequireSingle<EventSystem>(scene, "EventSystem", failures);
            }
        }
    }

    private static void ValidateGameplayScene(
        Scene scene,
        string zoneName,
        string expectedBossPrefabName,
        List<string> failures)
    {
        RequireSingle<GameSessionController>(scene, "GameSessionController", failures);
        RequireSingle<RunProgressionDirector>(scene, "RunProgressionDirector", failures);
        RequireSingle<SceneFlowController>(scene, "SceneFlowController", failures);
        RequireSingle<TouchGameplayControlsController>(scene, "TouchGameplayControlsController", failures);
        RequireSingle<TouchSteeringSurface>(scene, "TouchSteeringSurface", failures);

        LevelSpawner spawner = RequireSingle<LevelSpawner>(scene, "LevelSpawner", failures);
        if (spawner != null)
        {
            RequireObjectReference(spawner, "zoneSpawnProfile", $"{zoneName}/LevelSpawner.zoneSpawnProfile", failures);
        }

        GameUIRoot uiRoot = RequireSingle<GameUIRoot>(scene, "GameUIRoot", failures);
        if (uiRoot != null)
        {
            ValidateGameUIRoot(uiRoot, zoneName, failures);
            ValidateInGameShopManager(uiRoot.InGameShopManager, zoneName, failures);
            ValidateMobileGameRootComposition(uiRoot.transform.root.gameObject, scene.path, failures);
        }

        RequirePlayer(scene, zoneName, failures);
        ValidateBoundaries(scene, zoneName, failures);
        ValidateCleanup(scene, zoneName, failures);
        ValidateBoss(scene, zoneName, expectedBossPrefabName, failures);
        ValidateNoFixedSpawnedWorldEvents(scene, zoneName, failures);
    }

    private static void ValidateMobileGameRootComposition(
        GameObject gameRoot,
        string prefabPath,
        List<string> failures)
    {
        ValidateGameplayCanvasScaling(gameRoot, prefabPath, failures);

        TouchGameplayControlsController[] controllers =
            gameRoot.GetComponentsInChildren<TouchGameplayControlsController>(true);
        TouchControlsVisibilityController[] visibilityControllers =
            gameRoot.GetComponentsInChildren<TouchControlsVisibilityController>(true);
        TouchSteeringSurface[] surfaces =
            gameRoot.GetComponentsInChildren<TouchSteeringSurface>(true);
        TouchGameplayCommandButton[] commandButtons =
            gameRoot.GetComponentsInChildren<TouchGameplayCommandButton>(true);

        if (controllers.Length != 1
            || visibilityControllers.Length != 1
            || surfaces.Length != 1
            || commandButtons.Length != 4)
        {
            failures.Add(
                $"{prefabPath}: debe contener exactamente un controller, una visibilidad, una superficie y cuatro botones touch.");
            return;
        }

        string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
            controllers[0].gameObject);
        if (sourcePath != TouchControlsPrefabPath)
        {
            failures.Add($"{prefabPath}: los controles deben ser una instancia anidada de {TouchControlsPrefabPath}; actual: {sourcePath}.");
        }

        GameUIRoot uiRoot = gameRoot.GetComponentInChildren<GameUIRoot>(true);
        if (uiRoot != null)
        {
            RunMenuMobileCompositionContract.Validate(uiRoot.PauseMenuRoot, true, failures);
            RunMenuMobileCompositionContract.Validate(uiRoot.GameOverMenuRoot, false, failures);
        }
        SafeAreaAdapter[] safeAreaAdapters =
            uiRoot != null && uiRoot.HudRoot != null
                ? uiRoot.HudRoot.GetComponentsInChildren<SafeAreaAdapter>(true)
                : Array.Empty<SafeAreaAdapter>();
        if (uiRoot == null
            || safeAreaAdapters.Length != 1
            || safeAreaAdapters[0].transform.parent != uiRoot.HudRoot
            || controllers[0].transform.parent != safeAreaAdapters[0].transform)
        {
            failures.Add(
                $"{prefabPath}: HUD debe contener un SafeAreaRoot y TouchControls debe vivir directamente bajo esa raiz.");
        }
        else
        {
            ValidateHudSafeAreaContract(
                uiRoot,
                safeAreaAdapters[0],
                controllers[0],
                prefabPath,
                failures);
            GameObject canonicalRoot = AssetDatabase.LoadAssetAtPath<GameObject>(
                MobileGameRootCatalog.CanonicalGameRootPath);
            GameUIRoot canonicalUi = canonicalRoot != null
                ? canonicalRoot.GetComponentInChildren<GameUIRoot>(true)
                : null;
            MobileHudLayoutContract.Validate(
                canonicalUi != null && canonicalUi.HudRoot != null
                    ? canonicalUi.HudRoot.Find("SafeAreaRoot")
                    : null,
                safeAreaAdapters[0].transform,
                prefabPath,
                failures);
        }

        if (controllers[0].GetComponentsInChildren<Canvas>(true).Length > 0
            || controllers[0].GetComponentsInChildren<GraphicRaycaster>(true).Length > 0
            || controllers[0].GetComponentsInChildren<EventSystem>(true).Length > 0)
        {
            failures.Add($"{prefabPath}: TouchControls no debe duplicar Canvas, GraphicRaycaster ni EventSystem.");
        }
    }

    private static void ValidateGameplayCanvasScaling(
        GameObject gameRoot,
        string prefabPath,
        List<string> failures)
    {
        // Las escenas agregan el prefab de opciones: su adaptacion corresponde
        // al corte de menus, no a los cinco Canvas base del GameRoot jugable.
        Canvas[] canvases = gameRoot.GetComponentsInChildren<Canvas>(true)
            .Where(canvas => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(canvas.gameObject)
                != "Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab")
            .ToArray();
        if (canvases.Length != 5)
        {
            failures.Add($"{prefabPath}: debe contener exactamente 5 Canvas de gameplay; actual: {canvases.Length}. "
                + string.Join(", ", canvases.Select(canvas => $"{canvas.name} ({PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(canvas.gameObject)})")));
            return;
        }

        foreach (Canvas canvas in canvases)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                failures.Add($"{prefabPath}/{canvas.name}: falta CanvasScaler.");
                continue;
            }

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.referenceResolution != new Vector2(1920f, 1080f)
                || scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                || !Mathf.Approximately(scaler.matchWidthOrHeight, 1f))
            {
                failures.Add(
                    $"{prefabPath}/{canvas.name}: debe usar referencia 1920x1080 y preservar el alto en formatos moviles.");
            }
        }
    }

    private static void ValidateHudSafeAreaContract(
        GameUIRoot uiRoot,
        SafeAreaAdapter safeAreaAdapter,
        TouchGameplayControlsController controlsController,
        string prefabPath,
        List<string> failures)
    {
        Transform safeAreaRoot = safeAreaAdapter.transform;
        // Target null usa el RectTransform local; un target externo no protege el HUD.
        if (safeAreaAdapter.Target != null && safeAreaAdapter.Target.transform != safeAreaRoot)
        {
            failures.Add($"{prefabPath}: SafeAreaAdapter debe actuar sobre su propio SafeAreaRoot.");
        }
        if (uiRoot.HudRoot.childCount != 1
            || safeAreaRoot.name != "SafeAreaRoot"
            || safeAreaRoot.parent != uiRoot.HudRoot)
        {
            failures.Add($"{prefabPath}: HUD debe tener exactamente un hijo SafeAreaRoot.");
        }

        if (safeAreaRoot is not RectTransform safeAreaRect
            || safeAreaRect.anchorMin != Vector2.zero
            || safeAreaRect.anchorMax != Vector2.one
            || safeAreaRect.offsetMin != Vector2.zero
            || safeAreaRect.offsetMax != Vector2.zero)
        {
            failures.Add($"{prefabPath}: SafeAreaRoot debe cubrir todo el HUD antes de aplicar safe area runtime.");
        }

        string[] childNames = safeAreaRoot.Cast<Transform>()
            .Select(child => child.name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!childNames.SequenceEqual(ExpectedHudSafeAreaChildren))
        {
            failures.Add(
                $"{prefabPath}: SafeAreaRoot debe contener solo {string.Join(", ", ExpectedHudSafeAreaChildren)}.");
        }

        RequireHudReferenceUnderSafeArea(uiRoot.InkBar, safeAreaRoot, $"{prefabPath}/InkBar", failures);
        RequireHudReferenceUnderSafeArea(uiRoot.GadgetSlots, safeAreaRoot, $"{prefabPath}/GadgetSlots", failures);
        RequireHudReferenceUnderSafeArea(uiRoot.ShrimpCounter, safeAreaRoot, $"{prefabPath}/ShrimpCounter", failures);
        RequireHudReferenceUnderSafeArea(uiRoot.ScoreCounter, safeAreaRoot, $"{prefabPath}/ScoreCounter", failures);

        if (controlsController.transform.GetSiblingIndex() != safeAreaRoot.childCount - 1)
        {
            failures.Add($"{prefabPath}: TouchControls debe ser el ultimo hijo de SafeAreaRoot.");
        }
    }

    private static void RequireHudReferenceUnderSafeArea(
        Component component,
        Transform safeAreaRoot,
        string label,
        List<string> failures)
    {
        if (component == null)
        {
            failures.Add($"{label}: referencia HUD no asignada.");
            return;
        }

        if (!component.transform.IsChildOf(safeAreaRoot))
        {
            failures.Add($"{label}: debe vivir bajo SafeAreaRoot.");
        }
    }

    private static void ValidateGameUIRoot(GameUIRoot uiRoot, string zoneName, List<string> failures)
    {
        string[] requiredProperties =
        {
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
            "inGameShopManager"
        };

        foreach (string propertyName in requiredProperties)
        {
            RequireObjectReference(uiRoot, propertyName, $"{zoneName}/GameUIRoot.{propertyName}", failures);
        }
    }

    private static void ValidateOptionsMenu(OptionsMenuManager manager,List<string> failures)
    {
        if (manager == null) { failures.Add("OptionsMenu: falta manager."); return; }
        string[] controls = { "volumeSlider","resolutionDropdown","fullscreenToggle","backButton" };
        foreach (string field in controls.Concat(new[] { "menuRoot","background","canvasGroup" }))
            RequireObjectReference(manager,field,$"OptionsMenu.{field}",failures);
        var serialized = new SerializedObject(manager);
        GameObject panel = serialized.FindProperty("menuRoot").objectReferenceValue as GameObject;
        GameObject background = serialized.FindProperty("background").objectReferenceValue as GameObject;
        RectTransform safe = panel != null ? panel.transform.Find("OptionsSafeArea") as RectTransform : null;
        Transform content = safe != null ? safe.Find("OptionsContent") : null;
        if (safe == null || safe.GetComponent<SafeAreaAdapter>() == null
            || safe.GetComponent<SafeAreaAdapter>().Target != safe
            || content == null || content.GetComponent<MobileReferenceContentFitter>() == null)
        { failures.Add("OptionsMenu: falta contenedor seguro con ajuste de referencia."); return; }
        Image backdrop = background != null ? background.GetComponent<Image>() : null;
        if (background == null || background.transform.parent != panel.transform
            || background.transform.GetSiblingIndex() != 0 || backdrop == null || !backdrop.raycastTarget)
            failures.Add("OptionsMenu: el fondo debe bloquear raycasts fuera del contenido seguro.");
        foreach (string field in controls)
        {
            Component control = serialized.FindProperty(field).objectReferenceValue as Component;
            if (control != null && !control.transform.IsChildOf(content))
                failures.Add($"OptionsMenu.{field}: el control omite OptionsContent.");
        }
        Canvas owner = manager.GetComponentInParent<Canvas>(true);
        if (owner == null || owner.GetComponentsInChildren<Canvas>(true).Length != 1
            || owner.GetComponentsInChildren<GraphicRaycaster>(true).Length != 1)
            failures.Add("OptionsMenu: conservar un Canvas y GraphicRaycaster propios.");
    }

    private static void ValidateMainMenu(MainMenu mainMenu, List<string> failures)
    {
        RequireObjectReference(mainMenu, "optionsMenuPanel", "MainMenu.optionsMenuPanel", failures);
        RequireObjectReference(mainMenu, "tutorialComicsPanel", "MainMenu.tutorialComicsPanel", failures);
        RequireObjectReference(mainMenu, "tutorialComicsCanvasGroup", "MainMenu.tutorialComicsCanvasGroup", failures);
        RequireObjectReference(mainMenu, "tutorialLayerBlack", "MainMenu.tutorialLayerBlack", failures);
        RequireObjectReference(mainMenu, "tutorialButton", "MainMenu.tutorialButton", failures);
        RequireObjectReference(mainMenu, "tutorialButtonRoot", "MainMenu.tutorialButtonRoot", failures);
        RequireObjectReference(mainMenu, "tutorialNextButton", "MainMenu.tutorialNextButton", failures);
        RequireMainMenuTutorialPages(mainMenu, expectedCount: 3, failures);
        RequireObjectReference(mainMenu, "exitButtonRoot", "MainMenu.exitButtonRoot", failures);
        RequireObjectReference(mainMenu, "logoRoot", "MainMenu.logoRoot", failures);
        RequireObjectReference(mainMenu, "counterRoot", "MainMenu.counterRoot", failures);
        GameObject tutorialPanel = new SerializedObject(mainMenu).FindProperty("tutorialComicsPanel").objectReferenceValue as GameObject;
        RectTransform tutorialSafe = tutorialPanel != null ? tutorialPanel.transform.Find("TutorialSafeArea") as RectTransform : null;
        Transform tutorialContent = tutorialSafe != null ? tutorialSafe.Find("TutorialContent") : null;
        if (tutorialSafe == null || tutorialSafe.GetComponent<SafeAreaAdapter>() == null
            || tutorialSafe.GetComponent<SafeAreaAdapter>().Target != tutorialSafe
            || tutorialContent == null || tutorialContent.GetComponent<MobileReferenceContentFitter>() == null)
            failures.Add("MainMenu tutorial debe conservar TutorialSafeArea y TutorialContent con ajuste movil de referencia.");
        foreach (string name in new[] { "NavigationSafeArea", "BrandSafeArea", "CounterSafeArea" })
        {
            RectTransform root = mainMenu.GetComponentsInChildren<RectTransform>(true).SingleOrDefault(rect => rect.name == name);
            if (root == null || root.GetComponent<SafeAreaAdapter>() == null
                || root.GetComponent<SafeAreaAdapter>().Target != root
                || root.anchorMin != Vector2.zero || root.anchorMax != Vector2.one
                || root.offsetMin != Vector2.zero || root.offsetMax != Vector2.zero)
                failures.Add($"MainMenu.{name} debe conservar una raiz completa con SafeAreaAdapter propio.");
        }
        RequireSceneNameDoesNotContain(mainMenu, "playSceneName", "tutorial", "MainMenu.playSceneName", failures);
        RequireSceneNameDoesNotContain(mainMenu, "shopMenuSceneName", "tutorial", "MainMenu.shopMenuSceneName", failures);
    }

    private static void ValidateInGameShopManager(InGameShopManager shopManager, string zoneName, List<string> failures)
    {
        if (shopManager == null)
        {
            return;
        }

        string label = $"{zoneName}/InGameShopManager";
        RequireObjectReference(shopManager, "session", $"{label}.session", failures);
        RequireObjectReference(shopManager, "menuRoot", $"{label}.menuRoot", failures);
        RequireObjectReference(shopManager, "canvasGroup", $"{label}.canvasGroup", failures);
        RequireObjectReference(shopManager, "gadgetImage", $"{label}.gadgetImage", failures);
        RequireObjectReference(shopManager, "priceText", $"{label}.priceText", failures);
        RequireObjectReference(shopManager, "buyKeyText", $"{label}.buyKeyText", failures);
        RequireObjectReference(shopManager, "buyButton", $"{label}.buyButton", failures);
        RequireObjectReference(shopManager, "insufficientFundsText", $"{label}.insufficientFundsText", failures);
        RequireInGameShopOfferArray(shopManager, $"{label}.offers", failures);
    }

    private static void ValidateShopMenu(OutOfGameShopManager shopManager, List<string> failures)
    {
        Transform panel=shopManager.transform.Find("Panel");
        RectTransform safe=panel!=null ? panel.Find("ShopSafeArea") as RectTransform : null;
        Transform content=safe!=null ? safe.Find("ShopContent") : null;
        if (safe==null || safe.GetComponent<SafeAreaAdapter>()==null
            || safe.GetComponent<SafeAreaAdapter>().Target!=safe || content==null
            || content.GetComponent<MobileReferenceContentFitter>()==null)
            failures.Add("ShopMenu: falta ShopSafeArea/ShopContent con protección móvil.");
        else
        {
            foreach (Button button in shopManager.GetComponentsInChildren<Button>(true))
                if (!button.transform.IsChildOf(content)) failures.Add($"ShopMenu/{button.name}: botón fuera del contenido seguro.");
            if (content.Find("ShrimpCounter")==null) failures.Add("ShopMenu: contador fuera del contenido seguro.");
        }
        if (panel==null || panel.Find("Fondo")==null || panel.Find("Fondo").GetComponent<MobileBackgroundCover>()==null)
            failures.Add("ShopMenu: falta cobertura proporcional del fondo móvil.");
        ShopMenuCancelNavigation navigation=shopManager.GetComponent<ShopMenuCancelNavigation>();
        if (navigation==null) failures.Add("ShopMenu: falta navegación Cancel móvil.");
        else foreach (string field in new[] { "menu","options","inputModule" })
                RequireObjectReference(navigation,field,$"ShopMenu.Cancel.{field}",failures);
        RequireObjectReferenceArray(shopManager, "upgradeSlotButtons", 4, "ShopMenu.upgradeSlotButtons", failures);
        RequireObjectReferenceArray(shopManager, "skinSlotButtons", 4, "ShopMenu.skinSlotButtons", failures);
        RequireObjectReference(shopManager, "purchaseButton", "ShopMenu.purchaseButton", failures);
        RequireObjectReference(shopManager, "selectedItemNameText", "ShopMenu.selectedItemNameText", failures);
        RequireObjectReference(shopManager, "selectedItemDescriptionText", "ShopMenu.selectedItemDescriptionText", failures);
        RequireObjectReference(shopManager, "selectedItemPriceText", "ShopMenu.selectedItemPriceText", failures);
        RequireObjectReference(shopManager, "selectedItemStateText", "ShopMenu.selectedItemStateText", failures);
        RequireObjectReference(shopManager, "mobilePurchaseFeedbackRoot", "ShopMenu.mobilePurchaseFeedbackRoot", failures);
        RequireObjectReference(shopManager, "defaultShopVisualState", "ShopMenu.defaultShopVisualState", failures);
        RequireObjectReference(shopManager, "happyShopVisualState", "ShopMenu.happyShopVisualState", failures);
        RequireObjectReference(shopManager, "upgradeLevelIndicatorRoot", "ShopMenu.upgradeLevelIndicatorRoot", failures);

        SerializedObject serializedManager = new(shopManager);
        RequireSlotVisualArray(serializedManager, "upgradeSlotVisuals", 4, requireSkinStates: false, "ShopMenu.upgradeSlotVisuals", failures);
        RequireSlotVisualArray(serializedManager, "skinSlotVisuals", 4, requireSkinStates: true, "ShopMenu.skinSlotVisuals", failures);
        RequireLevelDropArray(serializedManager, "upgradeLevelDrops", 5, "ShopMenu.upgradeLevelDrops", failures);
    }

    private static void RequireSlotVisualArray(
        SerializedObject serializedObject,
        string propertyName,
        int expectedCount,
        bool requireSkinStates,
        string label,
        List<string> failures)
    {
        SerializedProperty array = RequireArray(serializedObject, propertyName, expectedCount, label, failures);
        if (array == null)
        {
            return;
        }

        for (int index = 0; index < array.arraySize; index++)
        {
            SerializedProperty slot = array.GetArrayElementAtIndex(index);
            string slotLabel = $"{label}[{index}]";
            RequireRelativeObjectReference(slot, "button", $"{slotLabel}.button", failures);
            RequireRelativeObjectReference(slot, "buttonVisualState", $"{slotLabel}.buttonVisualState", failures);
            RequireRelativeObjectReference(slot, "fallbackImage", $"{slotLabel}.fallbackImage", failures);

            if (requireSkinStates)
            {
                RequireRelativeObjectReference(slot, "purchasedState", $"{slotLabel}.purchasedState", failures);
                RequireRelativeObjectReference(slot, "equippedState", $"{slotLabel}.equippedState", failures);
            }
        }
    }

    private static void RequireLevelDropArray(
        SerializedObject serializedObject,
        string propertyName,
        int expectedCount,
        string label,
        List<string> failures)
    {
        SerializedProperty array = RequireArray(serializedObject, propertyName, expectedCount, label, failures);
        if (array == null)
        {
            return;
        }

        for (int index = 0; index < array.arraySize; index++)
        {
            SerializedProperty drop = array.GetArrayElementAtIndex(index);
            string dropLabel = $"{label}[{index}]";
            RequireRelativeObjectReference(drop, "emptyState", $"{dropLabel}.emptyState", failures);
            RequireRelativeObjectReference(drop, "halfState", $"{dropLabel}.halfState", failures);
            RequireRelativeObjectReference(drop, "fullState", $"{dropLabel}.fullState", failures);
        }
    }

    private static void RequireMainMenuTutorialPages(MainMenu mainMenu, int expectedCount, List<string> failures)
    {
        SerializedObject serializedObject = new(mainMenu);
        SerializedProperty pages = RequireArray(serializedObject, "tutorialComicPages", expectedCount, "MainMenu.tutorialComicPages", failures);
        if (pages == null)
        {
            return;
        }

        for (int index = 0; index < pages.arraySize; index++)
        {
            SerializedProperty page = pages.GetArrayElementAtIndex(index);
            string label = $"MainMenu.tutorialComicPages[{index}]";
            RequireRelativeObjectReference(page, "root", $"{label}.root", failures);
            RequireRelativeObjectReference(page, "vignette", $"{label}.vignette", failures);
            RequireRelativeObjectReferenceArray(page, "descriptionTexts", 1, $"{label}.descriptionTexts", failures);
        }
    }

    private static void RequirePlayer(Scene scene, string zoneName, List<string> failures)
    {
        GameObject[] playerObjects = FindSceneObjects<Transform>(scene)
            .Where(transform => transform.CompareTag(GameplayTagCatalog.Player))
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToArray();

        if (playerObjects.Length != 1)
        {
            failures.Add($"{zoneName} debe tener exactamente un objeto con tag Player; encontrados: {playerObjects.Length}.");
            return;
        }

        if (playerObjects[0].GetComponentInChildren<PlayerSkinApplier>(includeInactive: true) == null)
        {
            failures.Add($"{zoneName}/Player debe incluir PlayerSkinApplier para aplicar skins del catalogo.");
        }
    }

    private static void ValidateBoundaries(Scene scene, string zoneName, List<string> failures)
    {
        GameObject boundariesRoot = FindSceneObjects<Transform>(scene)
            .FirstOrDefault(transform => transform.name == "Boundaries")
            ?.gameObject;

        if (boundariesRoot == null)
        {
            failures.Add($"{zoneName} debe contener root Boundaries.");
            return;
        }

        RequireBoundaryPair(boundariesRoot.transform, "PlayerBoundaries", zoneName, failures);
        RequireBoundaryPair(boundariesRoot.transform, "CameraBoundaries", zoneName, failures);
    }

    private static void RequireBoundaryPair(Transform boundariesRoot, string pairName, string zoneName, List<string> failures)
    {
        Transform pairRoot = FindDescendant(boundariesRoot, pairName);
        if (pairRoot == null)
        {
            failures.Add($"{zoneName}/Boundaries debe contener {pairName}.");
            return;
        }

        RequireBoundaryCollider(pairRoot, "TopBoundary", zoneName, failures);
        RequireBoundaryCollider(pairRoot, "BottomBoundary", zoneName, failures);
    }

    private static void RequireBoundaryCollider(Transform pairRoot, string childName, string zoneName, List<string> failures)
    {
        Transform boundary = FindDescendant(pairRoot, childName);
        if (boundary == null)
        {
            failures.Add($"{zoneName}/{pairRoot.name} debe contener {childName}.");
            return;
        }

        if (boundary.GetComponent<Collider2D>() == null)
        {
            failures.Add($"{zoneName}/{pairRoot.name}/{childName} debe tener Collider2D.");
        }
    }

    private static void ValidateCleanup(Scene scene, string zoneName, List<string> failures)
    {
        DestroyOffscreen[] cleanupComponents = FindSceneObjects<DestroyOffscreen>(scene);
        if (cleanupComponents.Length != 1)
        {
            failures.Add($"{zoneName} debe contener exactamente un DestroyOffscreen bajo CleanUp; encontrados: {cleanupComponents.Length}.");
            return;
        }

        if (!HasAncestorNamed(cleanupComponents[0].transform, "CleanUp"))
        {
            failures.Add($"{zoneName}/DestroyOffscreen debe vivir bajo root CleanUp.");
        }
    }

    private static void ValidateBoss(Scene scene, string zoneName, string expectedBossPrefabName, List<string> failures)
    {
        BossEventDirector director = RequireSingle<BossEventDirector>(scene, "BossEventDirector", failures);
        if (director == null)
        {
            return;
        }

        UnityEngine.Object bossPrefab = RequireObjectReference(
            director,
            "bossPrefab",
            $"{zoneName}/BossEventDirector.bossPrefab",
            failures);

        if (bossPrefab != null && !bossPrefab.name.Contains(expectedBossPrefabName, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{zoneName}/BossEventDirector.bossPrefab debe apuntar a {expectedBossPrefabName}; actual: {bossPrefab.name}.");
        }
    }

    private static void ValidateNoFixedSpawnedWorldEvents(Scene scene, string zoneName, List<string> failures)
    {
        DealerFish[] dealerFish = FindSceneObjects<DealerFish>(scene);
        if (dealerFish.Length > 0)
        {
            failures.Add($"{zoneName} no debe contener DealerFish fijo en escena; debe nacer desde LevelSpawner.");
        }

        ScenePortal[] portals = FindSceneObjects<ScenePortal>(scene);
        if (portals.Length > 0)
        {
            failures.Add($"{zoneName} no debe contener ScenePortal fijo en escena; debe nacer desde LevelSpawner.");
        }
    }

    private static T RequireSingle<T>(Scene scene, string label, List<string> failures) where T : Component
    {
        T[] components = FindSceneObjects<T>(scene);
        if (components.Length != 1)
        {
            failures.Add($"{scene.path}: se esperaba exactamente un {label}; encontrados: {components.Length}.");
            return null;
        }

        return components[0];
    }

    private static UnityEngine.Object RequireObjectReference(
        UnityEngine.Object target,
        string propertyName,
        string label,
        List<string> failures)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            failures.Add($"{label}: propiedad serializada no encontrada.");
            return null;
        }

        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            failures.Add($"{label}: la propiedad no es ObjectReference.");
            return null;
        }

        if (property.objectReferenceValue == null)
        {
            failures.Add($"{label}: referencia obligatoria no asignada.");
        }

        return property.objectReferenceValue;
    }

    private static void RequireObjectReferenceArray(
        UnityEngine.Object target,
        string propertyName,
        int expectedCount,
        string label,
        List<string> failures)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = RequireArray(serializedObject, propertyName, expectedCount, label, failures);
        if (property == null)
        {
            return;
        }

        for (int index = 0; index < property.arraySize; index++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(index);
            if (item.propertyType != SerializedPropertyType.ObjectReference)
            {
                failures.Add($"{label}[{index}]: el elemento no es ObjectReference.");
                continue;
            }

            if (item.objectReferenceValue == null)
            {
                failures.Add($"{label}[{index}]: referencia obligatoria no asignada.");
            }
        }
    }

    private static void RequireInGameShopOfferArray(
        InGameShopManager shopManager,
        string label,
        List<string> failures)
    {
        SerializedObject serializedObject = new(shopManager);
        SerializedProperty offers = serializedObject.FindProperty("offers");
        if (offers == null)
        {
            failures.Add($"{label}: propiedad serializada no encontrada.");
            return;
        }

        if (!offers.isArray)
        {
            failures.Add($"{label}: la propiedad no es un array serializado.");
            return;
        }

        if (offers.arraySize == 0)
        {
            failures.Add($"{label}: debe tener al menos una oferta configurada.");
            return;
        }

        for (int index = 0; index < offers.arraySize; index++)
        {
            SerializedProperty offer = offers.GetArrayElementAtIndex(index);
            SerializedProperty gadgetPrefab = offer.FindPropertyRelative("gadgetPrefab");
            string offerLabel = $"{label}[{index}].gadgetPrefab";

            if (gadgetPrefab == null)
            {
                failures.Add($"{offerLabel}: propiedad serializada no encontrada.");
                continue;
            }

            if (gadgetPrefab.propertyType != SerializedPropertyType.ObjectReference)
            {
                failures.Add($"{offerLabel}: la propiedad no es ObjectReference.");
                continue;
            }

            if (gadgetPrefab.objectReferenceValue == null)
            {
                failures.Add($"{offerLabel}: referencia obligatoria no asignada.");
                continue;
            }

            if (gadgetPrefab.objectReferenceValue is GameObject prefab
                && prefab.GetComponent<GadgetShopItem>() == null)
            {
                failures.Add($"{offerLabel}: el prefab debe incluir GadgetShopItem.");
            }
        }
    }

    private static SerializedProperty RequireArray(
        SerializedObject serializedObject,
        string propertyName,
        int expectedCount,
        string label,
        List<string> failures)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            failures.Add($"{label}: propiedad serializada no encontrada.");
            return null;
        }

        if (!property.isArray)
        {
            failures.Add($"{label}: la propiedad no es un array serializado.");
            return null;
        }

        if (property.arraySize != expectedCount)
        {
            failures.Add($"{label}: debe tener {expectedCount} elementos; actual: {property.arraySize}.");
            return null;
        }

        return property;
    }

    private static void RequireRelativeObjectReference(
        SerializedProperty parent,
        string childPropertyName,
        string label,
        List<string> failures)
    {
        SerializedProperty property = parent.FindPropertyRelative(childPropertyName);
        if (property == null)
        {
            failures.Add($"{label}: propiedad serializada no encontrada.");
            return;
        }

        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            failures.Add($"{label}: la propiedad no es ObjectReference.");
            return;
        }

        if (property.objectReferenceValue == null)
        {
            failures.Add($"{label}: referencia obligatoria no asignada.");
        }
    }

    private static void RequireRelativeObjectReferenceArray(
        SerializedProperty parent,
        string childPropertyName,
        int minimumCount,
        string label,
        List<string> failures)
    {
        SerializedProperty property = parent.FindPropertyRelative(childPropertyName);
        if (property == null)
        {
            failures.Add($"{label}: propiedad serializada no encontrada.");
            return;
        }

        if (!property.isArray)
        {
            failures.Add($"{label}: la propiedad no es un array serializado.");
            return;
        }

        if (property.arraySize < minimumCount)
        {
            failures.Add($"{label}: debe tener al menos {minimumCount} elementos; actual: {property.arraySize}.");
            return;
        }

        for (int index = 0; index < property.arraySize; index++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(index);
            if (item.propertyType != SerializedPropertyType.ObjectReference)
            {
                failures.Add($"{label}[{index}]: el elemento no es ObjectReference.");
                continue;
            }

            if (item.objectReferenceValue == null)
            {
                failures.Add($"{label}[{index}]: referencia obligatoria no asignada.");
            }
        }
    }

    private static void RequireSceneNameDoesNotContain(
        UnityEngine.Object target,
        string propertyName,
        string forbiddenText,
        string label,
        List<string> failures)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            failures.Add($"{label}: propiedad serializada no encontrada.");
            return;
        }

        if (property.propertyType != SerializedPropertyType.String)
        {
            failures.Add($"{label}: la propiedad no es String.");
            return;
        }

        string value = property.stringValue ?? string.Empty;
        if (value.Contains(forbiddenText, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{label}: no debe cargar la escena tutorial pendiente; actual: {value}.");
        }
    }

    private static T[] FindSceneObjects<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(includeInactive: true))
            .Where(component => component != null)
            .ToArray();
    }

    private static void ValidateMissingScripts(Scene scene, List<string> failures)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            ValidateMissingScripts(root, scene.path, failures);
        }
    }

    private static void ValidateMissingScripts(GameObject root, string context, List<string> failures)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform transform in transforms)
        {
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            if (missingCount > 0)
            {
                failures.Add($"{context}: {GetPath(transform)} tiene {missingCount} script(s) faltante(s).");
            }
        }
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(includeInactive: true);
        return descendants.FirstOrDefault(descendant => descendant.name == name);
    }

    private static bool HasAncestorNamed(Transform transform, string ancestorName)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == ancestorName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static string GetPath(Transform transform)
    {
        Stack<string> names = new();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
