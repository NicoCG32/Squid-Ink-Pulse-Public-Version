using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public float timeDelay = 0.6f;

    public void Jugar()
    {
        // 0 Main Menu
        // 1 Sample Scene
        // 2 Zona Epipelágica
        StartCoroutine(CargarEscenaConDelay(2));
    }

    public void Opciones()
    {
        // 0 Main Menu
        // 2 Options Scene (Aún no existe, se va a crear después)
        // TO DO: Crear la escena de opciones y luego cambiar el número del build index
        StartCoroutine(CargarEscenaConDelay(2));
    }

    IEnumerator CargarEscenaConDelay(int sceneOffset)
    {
        // Para retrasar la carga de escena y permitir que se reproduzcan las animaciones de los botones
        yield return new WaitForSeconds(timeDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + sceneOffset);
    }

    public void Salir()
    {
        Application.Quit();
    }

}
