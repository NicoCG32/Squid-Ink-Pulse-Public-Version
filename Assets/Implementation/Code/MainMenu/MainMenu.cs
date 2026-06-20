using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string playSceneName = "Assets/Scenes/Game/ZonaEpipelagica.unity";
    [SerializeField] private float timeDelay = 0.6f;
    
    [Header("UI Panels")]
    [SerializeField] private OptionsMenuManager optionsMenuPanel;
    [SerializeField] private MainMenuStoreManager storeMenuPanel; // Added Store Reference

    private bool isLoading;

    public void Jugar()
    {
        LoadConfiguredScene(playSceneName, "juego");
    }

    public void Opciones()
    {
        if (optionsMenuPanel == null)
        {
            Debug.LogWarning("[MainMenu] El panel de opciones no está asignado.", this);
            return;
        }

        optionsMenuPanel.Open(); 
    }

    // New method to open the store
    public void AbrirTienda()
    {
        if (storeMenuPanel == null)
        {
            Debug.LogWarning("[MainMenu] El panel de la tienda no está asignado.", this);
            return;
        }

        storeMenuPanel.Open();
    }

    private void LoadConfiguredScene(string sceneName, string sceneLabel)
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

        StartCoroutine(LoadSceneAfterDelay(resolvedSceneName));
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

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        isLoading = true;
        yield return new WaitForSecondsRealtime(timeDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
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
