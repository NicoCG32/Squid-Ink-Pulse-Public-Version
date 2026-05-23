using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Animación de hover/click para botones del menú de pausa.
/// Simula el efecto de "sacudida con tinta" de los botones del MainMenu.
/// 
/// USO: Agregar como componente a cada botón del Canvas de pausa.
/// </summary>
public class PauseButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Escala")]
    public float hoverScale = 1.08f;
    public float pressScale = 0.95f;
    public float scaleSpeed = 10f;

    [Header("Rotación (sacudida)")]
    public float hoverRotation = 2f;
    public float shakeSpeed = 15f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private float targetScale = 1f;
    private float shakeTimer = 0f;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    private void Update()
    {
        // Animación de escala suave
        float currentScale = transform.localScale.x / originalScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        transform.localScale = originalScale * newScale;

        // Animación de sacudida en hover
        if (isHovered && !isPressed)
        {
            shakeTimer += Time.unscaledDeltaTime * shakeSpeed;
            float angle = Mathf.Sin(shakeTimer) * hoverRotation;
            transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
        }
        else
        {
            shakeTimer = 0f;
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                originalRotation,
                Time.unscaledDeltaTime * scaleSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed) targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed) targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetScale = isHovered ? hoverScale : 1f;
    }
}
