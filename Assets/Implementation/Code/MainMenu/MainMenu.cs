using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    private bool isLoading;
    private int demoShrimpSecretProgress;
    private Keyboard demoShrimpTextInputKeyboard;
    private int lastDemoShrimpTextInputFrame = -1;

    private void Awake()
    {
        ResolveUiReferences();
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

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
