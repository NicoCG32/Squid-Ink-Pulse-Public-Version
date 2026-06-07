using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneFlowController : MonoBehaviour
{
    [Header("Known Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private int mainMenuBuildIndex = 0;
    [SerializeField] private string tutorialSceneName = "Assets/Scenes/Game/ZonaTutorial.unity";
    [SerializeField] private string shopMenuSceneName = "Assets/Scenes/ShopMenu/ShopMenu.unity";
    [SerializeField] private string optionsMenuSceneName = "Assets/Scenes/OptionsMenu/OptionsMenu.unity";

    [Header("Portal Scenes")]
    [SerializeField] private string primaryGameplaySceneName = "Assets/Scenes/Game/ZonaEpipel\u00e1gica.unity";
    [SerializeField] private string secondaryGameplaySceneName = "Assets/Scenes/Game/ZonaExe.unity";

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(mainMenuBuildIndex))
        {
            SceneManager.LoadScene(mainMenuBuildIndex);
            return;
        }

        Debug.LogError("[SceneFlowController] Main Menu no esta disponible en Build Settings.", this);
    }

    public void LoadTutorial()
    {
        LoadSceneByName(tutorialSceneName);
    }

    public void LoadShopMenu()
    {
        LoadSceneByName(shopMenuSceneName);
    }

    public void LoadOptionsMenu()
    {
        LoadSceneByName(optionsMenuSceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        TryLoadSceneByName(sceneName);
    }

    public bool TryLoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneFlowController] Nombre de escena vacio.", this);
            return false;
        }

        string resolvedSceneName = ResolveSceneNameForBuild(sceneName);
        if (string.IsNullOrWhiteSpace(resolvedSceneName))
        {
            Debug.LogError($"[SceneFlowController] La escena '{sceneName}' no esta disponible en Build Settings.", this);
            return false;
        }

        SceneManager.LoadScene(resolvedSceneName);
        return true;
    }

    public void LoadSceneByBuildIndex(int buildIndex)
    {
        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(buildIndex))
        {
            Debug.LogError($"[SceneFlowController] Build index {buildIndex} no esta disponible en Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(buildIndex);
    }

    public void LoadPortalDestinationFromActiveScene()
    {
        TryLoadPortalDestinationFromActiveScene();
    }

    public bool TryLoadPortalDestinationFromActiveScene()
    {
        string targetScene = ResolvePortalDestinationFromActiveScene();
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("[SceneFlowController] No hay escena destino configurada para portal.", this);
            return false;
        }

        return TryLoadSceneByName(targetScene);
    }

    private string ResolvePortalDestinationFromActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (SceneMatches(activeScene, secondaryGameplaySceneName))
        {
            return primaryGameplaySceneName;
        }

        return secondaryGameplaySceneName;
    }

    private bool SceneMatches(Scene scene, string configuredScene)
    {
        if (string.IsNullOrWhiteSpace(configuredScene))
        {
            return false;
        }

        return scene.path == configuredScene
            || scene.name == configuredScene
            || scene.name == SceneNameFromPath(configuredScene);
    }

    private string ResolveSceneNameForBuild(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return sceneName;
        }

        string shortSceneName = SceneNameFromPath(sceneName);
        return Application.CanStreamedLevelBeLoaded(shortSceneName)
            ? shortSceneName
            : null;
    }

    private string SceneNameFromPath(string scenePath)
    {
        string sceneWithoutExtension = scenePath.EndsWith(".unity")
            ? scenePath.Substring(0, scenePath.Length - ".unity".Length)
            : scenePath;

        int lastSlashIndex = sceneWithoutExtension.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex >= sceneWithoutExtension.Length - 1)
        {
            return sceneWithoutExtension;
        }

        return sceneWithoutExtension.Substring(lastSlashIndex + 1);
    }
}
