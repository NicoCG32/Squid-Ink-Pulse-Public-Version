using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    private const string DemoShrimpSecretCode = "SONICYNOTA7";
    private const int DemoShrimpGrantAmount = 676700;
    private static readonly Key[] DemoShrimpSecretKeys =
    {
        Key.S,
        Key.O,
        Key.N,
        Key.I,
        Key.C,
        Key.Y,
        Key.N,
        Key.O,
        Key.T,
        Key.A,
        Key.Digit7
    };

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string playSceneName = "Assets/Scenes/Game/ZonaEpipelagica.unity";
    [SerializeField] private string shopMenuSceneName = "Assets/Scenes/ShopMenu/ShopMenu.unity";
    [SerializeField] private float timeDelay = 0.6f;

    [Header("Lore Comics")]
    [SerializeField] private bool showStartLoreComicBeforePlay = true;

    [Header("UI Panels")]
    [SerializeField] private OptionsMenuManager optionsMenuPanel;

    [Header("Tutorial Comics")]
    [SerializeField] private GameObject tutorialComicsPanel;
    [SerializeField] private CanvasGroup tutorialComicsCanvasGroup;
    [SerializeField] private GameObject tutorialLayerBlack;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button tutorialNextButton;
    [SerializeField] private TutorialComicPage[] tutorialComicPages = Array.Empty<TutorialComicPage>();

    private bool isLoading;
    private bool tutorialComicsOpen;
    private int currentTutorialComicPageIndex = -1;
    private int demoShrimpSecretProgress;
    private Keyboard demoShrimpTextInputKeyboard;
    private int lastDemoShrimpTextInputFrame = -1;

    private void Awake()
    {
        ResolveUiReferences();
        BindRuntimeTutorialButtons();
        CloseTutorialImmediate();
    }

    private void OnEnable()
    {
        SyncDemoShrimpTextInputKeyboard();
    }

    private void OnDisable()
    {
        UnsubscribeDemoShrimpTextInputKeyboard();
    }

    private void Update()
    {
        SyncDemoShrimpTextInputKeyboard();
        if (lastDemoShrimpTextInputFrame == Time.frameCount)
        {
            return;
        }

        ListenForDemoShrimpSecretCode();
    }

    private void OnValidate()
    {
        ResolveUiReferences();
        EnsureTutorialComicsVisibleInEditor();
    }

    public void Jugar()
    {
        LoadConfiguredScene(playSceneName, "juego", showStartLoreComicBeforePlay, resetRunStateBeforeLoad: true);
    }

    public void Opciones()
    {
        ResolveUiReferences();

        if (optionsMenuPanel == null)
        {
            Debug.LogWarning("[MainMenu] El panel de opciones no esta asignado.", this);
            return;
        }

        optionsMenuPanel.Open();
    }

    public void AbrirTienda()
    {
        LoadConfiguredScene(shopMenuSceneName, "tienda");
    }

    public void AbrirTutorial()
    {
        ResolveUiReferences();

        if (!HasTutorialComicPages())
        {
            Debug.LogWarning("[MainMenu] El tutorial no tiene comics configurados.", this);
            CloseTutorialImmediate();
            return;
        }

        tutorialComicsOpen = true;
        currentTutorialComicPageIndex = 0;
        SetTutorialComicsPanelVisible(true);
        ShowTutorialComicPage(currentTutorialComicPageIndex);
    }

    public void AvanzarTutorial()
    {
        if (!tutorialComicsOpen)
        {
            AbrirTutorial();
            return;
        }

        int nextPageIndex = currentTutorialComicPageIndex + 1;
        if (nextPageIndex >= tutorialComicPages.Length)
        {
            CloseTutorialImmediate();
            return;
        }

        currentTutorialComicPageIndex = nextPageIndex;
        ShowTutorialComicPage(currentTutorialComicPageIndex);
    }

    public void VolverAlMenuPrincipal()
    {
        LoadConfiguredScene(mainMenuSceneName, "menu principal");
    }

    private void LoadConfiguredScene(
        string sceneName,
        string sceneLabel,
        bool showStartLoreComic = false,
        bool resetRunStateBeforeLoad = false)
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[MainMenu] No hay escena configurada para {sceneLabel}.", this);
            return;
        }

        string resolvedSceneName = ResolveLoadableSceneName(sceneName);
        if (string.IsNullOrEmpty(resolvedSceneName))
        {
            Debug.LogError($"[MainMenu] La escena de {sceneLabel} ('{sceneName}') no esta disponible en Build Settings.", this);
            return;
        }

        StartCoroutine(LoadSceneAfterDelay(resolvedSceneName, showStartLoreComic, resetRunStateBeforeLoad));
    }

    private string ResolveLoadableSceneName(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return sceneName;
        }

        string sceneWithoutExtension = sceneName.EndsWith(".unity")
            ? sceneName.Substring(0, sceneName.Length - ".unity".Length)
            : sceneName;

        if (Application.CanStreamedLevelBeLoaded(sceneWithoutExtension))
        {
            return sceneWithoutExtension;
        }

        int lastSlashIndex = sceneWithoutExtension.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex >= sceneWithoutExtension.Length - 1)
        {
            return null;
        }

        string shortSceneName = sceneWithoutExtension.Substring(lastSlashIndex + 1);
        return Application.CanStreamedLevelBeLoaded(shortSceneName) ? shortSceneName : null;
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, bool showStartLoreComic, bool resetRunStateBeforeLoad)
    {
        isLoading = true;

        if (showStartLoreComic)
        {
            yield return LoreComicPresenter.PlayGameStartIfAvailable();
        }

        yield return new WaitForSecondsRealtime(timeDelay);
        if (resetRunStateBeforeLoad)
        {
            SceneFlowController.ResetRunScopedRuntimeState();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void ResolveUiReferences()
    {
        optionsMenuPanel ??= GetComponentInChildren<OptionsMenuManager>(includeInactive: true);
        optionsMenuPanel ??= FindInScene<OptionsMenuManager>();

        ResolveTutorialReferences();
    }

    private void ResolveTutorialReferences()
    {
        if (tutorialComicsPanel == null)
        {
            tutorialComicsPanel = FindNamedGameObjectInScene("ComicsTutorial");
        }

        if (tutorialComicsPanel != null)
        {
            tutorialComicsCanvasGroup ??= tutorialComicsPanel.GetComponent<CanvasGroup>();
            tutorialLayerBlack ??= FindNamedDescendant(tutorialComicsPanel.transform, "BlackLayer");
            tutorialLayerBlack ??= FindNamedDescendant(tutorialComicsPanel.transform, "LayerBlack");
            tutorialLayerBlack ??= FindNamedDescendant(tutorialComicsPanel.transform, "Dimmer");
        }

        tutorialButton ??= FindButtonUnderNamedObject("ComoJugarBoton");
        tutorialNextButton ??= FindButtonUnderNamedObject("Next");

        if ((tutorialComicPages == null || tutorialComicPages.Length == 0) && tutorialComicsPanel != null)
        {
            tutorialComicPages = CollectTutorialComicPages();
        }
    }

    private void BindRuntimeTutorialButtons()
    {
        if (tutorialButton != null && !HasPersistentButtonMethod(tutorialButton, nameof(AbrirTutorial)))
        {
            tutorialButton.onClick.RemoveListener(AbrirTutorial);
            tutorialButton.onClick.AddListener(AbrirTutorial);
        }

        if (tutorialNextButton != null && !HasPersistentButtonMethod(tutorialNextButton, nameof(AvanzarTutorial)))
        {
            tutorialNextButton.onClick.RemoveListener(AvanzarTutorial);
            tutorialNextButton.onClick.AddListener(AvanzarTutorial);
        }
    }

    private void EnsureTutorialComicsVisibleInEditor()
    {
        if (Application.isPlaying || tutorialComicsPanel == null)
        {
            return;
        }

        tutorialComicsPanel.SetActive(true);
        EnsureNonZeroScaleRecursive(tutorialComicsPanel.transform);
        SetCanvasGroupState(tutorialComicsCanvasGroup, visible: true, interactive: false);

        if (tutorialLayerBlack != null)
        {
            tutorialLayerBlack.SetActive(true);
            tutorialLayerBlack.transform.SetAsFirstSibling();
        }

        if (tutorialComicPages == null)
        {
            return;
        }

        for (int i = 0; i < tutorialComicPages.Length; i++)
        {
            TutorialComicPage page = tutorialComicPages[i];
            page?.TouchInspectorReferences();

            GameObject pageRoot = page?.Root;
            if (pageRoot == null)
            {
                continue;
            }

            pageRoot.SetActive(true);
            SetCanvasGroupState(pageRoot.GetComponent<CanvasGroup>(), visible: true, interactive: false);
        }

        if (tutorialNextButton != null)
        {
            tutorialNextButton.gameObject.SetActive(true);
        }
    }

    private void CloseTutorialImmediate()
    {
        tutorialComicsOpen = false;
        currentTutorialComicPageIndex = -1;
        SetTutorialComicsPanelVisible(false);
        HideAllTutorialComicPages();
    }

    private void SetTutorialComicsPanelVisible(bool visible)
    {
        if (tutorialComicsPanel == null)
        {
            return;
        }

        tutorialComicsPanel.SetActive(true);
        EnsureNonZeroScale(tutorialComicsPanel.transform);
        SetCanvasGroupState(tutorialComicsCanvasGroup, visible, visible);

        if (tutorialLayerBlack != null)
        {
            tutorialLayerBlack.SetActive(visible);
        }

        if (tutorialNextButton != null)
        {
            tutorialNextButton.gameObject.SetActive(visible);
            tutorialNextButton.interactable = visible;
        }
    }

    private void ShowTutorialComicPage(int pageIndex)
    {
        if (tutorialComicPages == null)
        {
            return;
        }

        for (int i = 0; i < tutorialComicPages.Length; i++)
        {
            GameObject pageRoot = tutorialComicPages[i]?.Root;
            if (pageRoot == null)
            {
                continue;
            }

            bool visible = i == pageIndex;
            pageRoot.SetActive(visible);
            EnsureNonZeroScale(pageRoot.transform);
            SetCanvasGroupState(pageRoot.GetComponent<CanvasGroup>(), visible, false);
        }
    }

    private void HideAllTutorialComicPages()
    {
        if (tutorialComicPages == null)
        {
            return;
        }

        for (int i = 0; i < tutorialComicPages.Length; i++)
        {
            GameObject pageRoot = tutorialComicPages[i]?.Root;
            if (pageRoot == null)
            {
                continue;
            }

            SetCanvasGroupState(pageRoot.GetComponent<CanvasGroup>(), visible: false, interactive: false);
            pageRoot.SetActive(false);
        }
    }

    private bool HasTutorialComicPages()
    {
        if (tutorialComicPages == null)
        {
            return false;
        }

        for (int i = 0; i < tutorialComicPages.Length; i++)
        {
            if (tutorialComicPages[i]?.Root != null)
            {
                return true;
            }
        }

        return false;
    }

    private TutorialComicPage[] CollectTutorialComicPages()
    {
        if (tutorialComicsPanel == null)
        {
            return Array.Empty<TutorialComicPage>();
        }

        var pageRoots = new List<Transform>();
        Transform tutorialTransform = tutorialComicsPanel.transform;
        for (int i = 0; i < tutorialTransform.childCount; i++)
        {
            Transform child = tutorialTransform.GetChild(i);
            if (IsTutorialComicPage(child.name))
            {
                pageRoots.Add(child);
            }
        }

        pageRoots.Sort(CompareTutorialComicPages);

        var pages = new TutorialComicPage[pageRoots.Count];
        for (int i = 0; i < pageRoots.Count; i++)
        {
            pages[i] = TutorialComicPage.FromRoot(pageRoots[i]);
        }

        return pages;
    }

    private Button FindButtonUnderNamedObject(string objectName)
    {
        GameObject root = FindNamedGameObjectInScene(objectName);
        return root != null ? root.GetComponentInChildren<Button>(includeInactive: true) : null;
    }

    private GameObject FindNamedGameObjectInScene(string objectName)
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        string normalizedTargetName = NormalizeObjectName(objectName);
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindNamedDescendant(roots[i].transform, normalizedTargetName, objectNameAlreadyNormalized: true);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindNamedDescendant(Transform root, string objectName)
    {
        Transform found = FindNamedDescendant(root, NormalizeObjectName(objectName), objectNameAlreadyNormalized: true);
        return found != null ? found.gameObject : null;
    }

    private static Transform FindNamedDescendant(Transform root, string normalizedObjectName, bool objectNameAlreadyNormalized)
    {
        if (root == null)
        {
            return null;
        }

        string targetName = objectNameAlreadyNormalized ? normalizedObjectName : NormalizeObjectName(normalizedObjectName);
        if (NormalizeObjectName(root.name) == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedDescendant(root.GetChild(i), targetName, objectNameAlreadyNormalized: true);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool HasPersistentButtonMethod(Button button, string methodName)
    {
        if (button == null)
        {
            return false;
        }

        int eventCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < eventCount; i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetCanvasGroupState(CanvasGroup canvasGroup, bool visible, bool interactive)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = interactive;
        canvasGroup.blocksRaycasts = interactive;
    }

    private static void EnsureNonZeroScale(Transform target)
    {
        if (target != null && target.localScale == Vector3.zero)
        {
            target.localScale = Vector3.one;
        }
    }

    private static void EnsureNonZeroScaleRecursive(Transform target)
    {
        if (target == null)
        {
            return;
        }

        EnsureNonZeroScale(target);
        for (int i = 0; i < target.childCount; i++)
        {
            EnsureNonZeroScaleRecursive(target.GetChild(i));
        }
    }

    private static bool IsTutorialComicPage(string objectName)
    {
        return NormalizeObjectName(objectName).StartsWith("comic", StringComparison.Ordinal);
    }

    private static int CompareTutorialComicPages(Transform left, Transform right)
    {
        int leftNumber = ExtractFirstNumber(left.name);
        int rightNumber = ExtractFirstNumber(right.name);

        int numberComparison = leftNumber.CompareTo(rightNumber);
        return numberComparison != 0
            ? numberComparison
            : left.GetSiblingIndex().CompareTo(right.GetSiblingIndex());
    }

    private static int ExtractFirstNumber(string text)
    {
        int number = 0;
        bool foundDigit = false;

        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i]))
            {
                if (foundDigit)
                {
                    break;
                }

                continue;
            }

            foundDigit = true;
            number = (number * 10) + (text[i] - '0');
        }

        return foundDigit ? number : int.MaxValue;
    }

    private static string NormalizeObjectName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        for (int i = 0; i < decomposed.Length; i++)
        {
            char current = decomposed[i];
            if (CharUnicodeInfo.GetUnicodeCategory(current) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(current))
            {
                builder.Append(char.ToLowerInvariant(current));
            }
        }

        return builder.ToString();
    }

    private void ListenForDemoShrimpSecretCode()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.anyKey.wasPressedThisFrame)
        {
            return;
        }

        if (WasDemoShrimpSecretKeyPressed(keyboard, demoShrimpSecretProgress))
        {
            AdvanceDemoShrimpSecretCode(DemoShrimpSecretCode[demoShrimpSecretProgress]);
        }
        else if (WasDemoShrimpSecretKeyPressed(keyboard, 0))
        {
            AdvanceDemoShrimpSecretCode(DemoShrimpSecretCode[0]);
        }
        else if (WasIgnoredDemoShrimpSecretKeyPressed(keyboard))
        {
            return;
        }
        else
        {
            demoShrimpSecretProgress = 0;
        }
    }

    private void HandleDemoShrimpTextInput(char input)
    {
        if (!TryNormalizeDemoShrimpInput(input, out char normalizedInput))
        {
            return;
        }

        lastDemoShrimpTextInputFrame = Time.frameCount;
        AdvanceDemoShrimpSecretCode(normalizedInput);
    }

    private void AdvanceDemoShrimpSecretCode(char input)
    {
        if (demoShrimpSecretProgress >= 0
            && demoShrimpSecretProgress < DemoShrimpSecretCode.Length
            && input == DemoShrimpSecretCode[demoShrimpSecretProgress])
        {
            demoShrimpSecretProgress++;
        }
        else if (input == DemoShrimpSecretCode[0])
        {
            demoShrimpSecretProgress = 1;
        }
        else
        {
            demoShrimpSecretProgress = 0;
        }

        if (demoShrimpSecretProgress < DemoShrimpSecretCode.Length)
        {
            return;
        }

        demoShrimpSecretProgress = 0;
        ShrimpRuntimeWallet.Refund(DemoShrimpGrantAmount);
        Debug.Log($"[MainMenu] Codigo secreto de muestra '{DemoShrimpSecretCode}' aplicado: +{ShrimpCounterDisplay.FormatShrimpAmount(DemoShrimpGrantAmount)} camarones. Saldo actual: {ShrimpCounterDisplay.FormatShrimpAmount(ShrimpRuntimeWallet.TotalShrimp)}.", this);
    }

    private static bool WasDemoShrimpSecretKeyPressed(Keyboard keyboard, int keyIndex)
    {
        if (keyboard == null || keyIndex < 0 || keyIndex >= DemoShrimpSecretKeys.Length)
        {
            return false;
        }

        if (keyboard[DemoShrimpSecretKeys[keyIndex]]?.wasPressedThisFrame == true)
        {
            return true;
        }

        return keyIndex == DemoShrimpSecretKeys.Length - 1
            && keyboard[Key.Numpad7]?.wasPressedThisFrame == true;
    }

    private static bool WasIgnoredDemoShrimpSecretKeyPressed(Keyboard keyboard)
    {
        return keyboard[Key.LeftShift]?.wasPressedThisFrame == true
            || keyboard[Key.RightShift]?.wasPressedThisFrame == true
            || keyboard[Key.LeftCtrl]?.wasPressedThisFrame == true
            || keyboard[Key.RightCtrl]?.wasPressedThisFrame == true
            || keyboard[Key.LeftAlt]?.wasPressedThisFrame == true
            || keyboard[Key.RightAlt]?.wasPressedThisFrame == true
            || keyboard[Key.CapsLock]?.wasPressedThisFrame == true;
    }

    private static bool TryNormalizeDemoShrimpInput(char input, out char normalizedInput)
    {
        normalizedInput = char.ToUpperInvariant(input);
        return char.IsLetterOrDigit(normalizedInput);
    }

    private void SyncDemoShrimpTextInputKeyboard()
    {
        Keyboard currentKeyboard = Keyboard.current;
        if (demoShrimpTextInputKeyboard == currentKeyboard)
        {
            return;
        }

        UnsubscribeDemoShrimpTextInputKeyboard();
        demoShrimpTextInputKeyboard = currentKeyboard;

        if (demoShrimpTextInputKeyboard != null)
        {
            demoShrimpTextInputKeyboard.onTextInput += HandleDemoShrimpTextInput;
        }
    }

    private void UnsubscribeDemoShrimpTextInputKeyboard()
    {
        if (demoShrimpTextInputKeyboard != null)
        {
            demoShrimpTextInputKeyboard.onTextInput -= HandleDemoShrimpTextInput;
            demoShrimpTextInputKeyboard = null;
        }
    }

    private T FindInScene<T>() where T : Component
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(includeInactive: true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    [Serializable]
    private sealed class TutorialComicPage
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image vignette;
        [SerializeField] private TMP_Text[] descriptionTexts = Array.Empty<TMP_Text>();

        public GameObject Root => root;

        public void TouchInspectorReferences()
        {
            _ = vignette;
            _ = descriptionTexts;
        }

        public static TutorialComicPage FromRoot(Transform pageRoot)
        {
            var page = new TutorialComicPage
            {
                root = pageRoot != null ? pageRoot.gameObject : null,
                vignette = FindFirstComponentByName<Image>(pageRoot, "Vineta", "Vignette"),
                descriptionTexts = FindComponentsByName<TMP_Text>(
                    pageRoot,
                    "Descripcion",
                    "Description",
                    "Texto",
                    "Text")
            };

            return page;
        }

        private static T FindFirstComponentByName<T>(Transform root, params string[] acceptedNames)
            where T : Component
        {
            if (root == null)
            {
                return null;
            }

            if (root.TryGetComponent(out T component) && NameMatches(root.name, acceptedNames))
            {
                return component;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                T found = FindFirstComponentByName<T>(root.GetChild(i), acceptedNames);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T[] FindComponentsByName<T>(Transform root, params string[] acceptedNames)
            where T : Component
        {
            if (root == null)
            {
                return Array.Empty<T>();
            }

            var results = new List<T>();
            CollectComponentsByName(root, acceptedNames, results);
            return results.ToArray();
        }

        private static void CollectComponentsByName<T>(Transform root, string[] acceptedNames, List<T> results)
            where T : Component
        {
            if (root.TryGetComponent(out T component) && NameMatches(root.name, acceptedNames))
            {
                results.Add(component);
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectComponentsByName(root.GetChild(i), acceptedNames, results);
            }
        }

        private static bool NameMatches(string objectName, string[] acceptedNames)
        {
            string normalizedObjectName = NormalizeObjectName(objectName);
            for (int i = 0; i < acceptedNames.Length; i++)
            {
                if (normalizedObjectName.StartsWith(NormalizeObjectName(acceptedNames[i]), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void Salir()
    {
        if (FairParticipantSession.TryCheckoutAndQuit(this))
        {
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
