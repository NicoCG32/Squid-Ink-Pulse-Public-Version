using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
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

    private void Awake()
    {
        ResolveUiReferences();
    }

    private void OnValidate()
    {
        ResolveUiReferences();
    }

    public void Jugar()
    {
        LoadConfiguredScene(playSceneName, "juego", showStartLoreComicBeforePlay);
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

    private void LoadConfiguredScene(string sceneName, string sceneLabel, bool showStartLoreComic = false)
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

        StartCoroutine(LoadSceneAfterDelay(resolvedSceneName, showStartLoreComic));
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

    private IEnumerator LoadSceneAfterDelay(string sceneName, bool showStartLoreComic)
    {
        isLoading = true;

        if (showStartLoreComic)
        {
            yield return LoreComicPresenter.PlayGameStartIfAvailable();
        }

        yield return new WaitForSecondsRealtime(timeDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void ResolveUiReferences()
    {
        optionsMenuPanel ??= GetComponentInChildren<OptionsMenuManager>(includeInactive: true);
        optionsMenuPanel ??= FindInScene<OptionsMenuManager>();
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
