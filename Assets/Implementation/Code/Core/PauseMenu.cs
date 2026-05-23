using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

// Menú de pausa con animaciones de entrada zigzag y fade.
// Tecla P para pausar/reanudar.
//
// USO:
//   1. Crear un Canvas con overlay oscuro, título, botones y hint text
//   2. Agregar CanvasGroup al Canvas
//   3. Asignar el Canvas y los botones (RectTransform) en el Inspector
//   4. En cada botón, asignar Reanudar(), Opciones(), IrAlMenu(), Salir()
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;
    public CanvasGroup canvasGroup;

    [Header("Botones (en orden para animación zigzag)")]
    public RectTransform[] botones;

    [Header("Animación")]
    public float fadeDuration = 0.3f;
    public float zigzagOffset = 800f;
    public float zigzagDuration = 0.35f;
    public float zigzagDelay = 0.08f;

    [Header("Configuración")]
    public int mainMenuBuildIndex = 0;

    private bool isPaused = false;
    private bool isAnimating = false;
    private Vector2[] posicionesOriginales;

    void Start()
    {
        // Guardar posiciones originales de los botones
        if (botones != null && botones.Length > 0)
        {
            posicionesOriginales = new Vector2[botones.Length];
            for (int i = 0; i < botones.Length; i++)
                posicionesOriginales[i] = botones[i].anchoredPosition;
        }

        if (pauseMenuUI != null)
        {
            canvasGroup.alpha = 0f;
            pauseMenuUI.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && !isAnimating)
            TogglePause();
    }

    public void TogglePause()
    {
        if (pauseMenuUI == null || isAnimating) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            StartCoroutine(AnimarEntrada());
        }
        else
        {
            StartCoroutine(AnimarSalida());
        }
    }

    // ========================
    // ANIMACIONES
    // ========================

    IEnumerator AnimarEntrada()
    {
        isAnimating = true;

        // Mover botones fuera de pantalla en zigzag (alternando izquierda/derecha)
        for (int i = 0; i < botones.Length; i++)
        {
            float direccion = (i % 2 == 0) ? -1f : 1f;
            botones[i].anchoredPosition = posicionesOriginales[i] + new Vector2(zigzagOffset * direccion, 0);
        }

        // Fade in del overlay
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Entrada zigzag de cada botón con delay
        for (int i = 0; i < botones.Length; i++)
        {
            StartCoroutine(DeslizarBoton(botones[i], posicionesOriginales[i]));
            yield return new WaitForSecondsRealtime(zigzagDelay);
        }

        yield return new WaitForSecondsRealtime(zigzagDuration);
        isAnimating = false;
    }

    IEnumerator AnimarSalida()
    {
        isAnimating = true;

        // Sacar botones en zigzag inverso
        for (int i = botones.Length - 1; i >= 0; i--)
        {
            float direccion = (i % 2 == 0) ? -1f : 1f;
            Vector2 destino = posicionesOriginales[i] + new Vector2(zigzagOffset * direccion, 0);
            StartCoroutine(DeslizarBoton(botones[i], destino));
            yield return new WaitForSecondsRealtime(zigzagDelay);
        }

        yield return new WaitForSecondsRealtime(zigzagDuration);

        // Fade out
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isAnimating = false;
    }

    IEnumerator DeslizarBoton(RectTransform boton, Vector2 destino)
    {
        Vector2 inicio = boton.anchoredPosition;
        float timer = 0f;

        while (timer < zigzagDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / zigzagDuration;
            // Ease out back: rebote suave al llegar
            float ease = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
            boton.anchoredPosition = Vector2.LerpUnclamped(inicio, destino, ease);
            yield return null;
        }

        boton.anchoredPosition = destino;
    }

    // ========================
    // ACCIONES DE LOS BOTONES
    // ========================

    public void Reanudar()
    {
        if (isPaused && !isAnimating)
            TogglePause();
    }

    public void Opciones()
    {
        // TO DO: Abrir submenú de opciones dentro de la pausa
        Debug.Log("Opciones: aún no implementado");
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuBuildIndex);
    }

    public void Salir()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
