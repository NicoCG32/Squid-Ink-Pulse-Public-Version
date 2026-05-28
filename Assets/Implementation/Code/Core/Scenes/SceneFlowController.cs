using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneFlowController : MonoBehaviour
{
    [Header("Known Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private int mainMenuBuildIndex = 0;

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

    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneFlowController] Nombre de escena vacio.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneFlowController] La escena '{sceneName}' no esta disponible en Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
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
}
