using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Animacion de hover/click para botones de menus runtime.
/// Simula el efecto de sacudida con tinta usado por la interfaz.
///
/// USO: Agregar como componente a cada boton interactivo de menus in-game.
/// </summary>
public class MenuButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Escala")]
    public float hoverScale = 1.08f;
    public float pressScale = 0.95f;
    public float scaleSpeed = 10f;

    [Header("Rotacion")]
    public float hoverRotation = 2f;
    public float shakeSpeed = 15f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Transform animatedTransform;
    private float targetScale = 1f;
    private float shakeTimer;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        animatedTransform = ResolveAnimatedTransform();
        originalScale = animatedTransform.localScale;
        originalRotation = animatedTransform.localRotation;
    }

    private void Update()
    {
        if (animatedTransform == null)
        {
            return;
        }

        float currentScale = animatedTransform.localScale.x / originalScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        animatedTransform.localScale = originalScale * newScale;

        if (isHovered && !isPressed)
        {
            shakeTimer += Time.unscaledDeltaTime * shakeSpeed;
            float angle = Mathf.Sin(shakeTimer) * hoverRotation;
            animatedTransform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
            return;
        }

        shakeTimer = 0f;
        animatedTransform.localRotation = Quaternion.Lerp(
            animatedTransform.localRotation,
            originalRotation,
            Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            targetScale = hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            targetScale = 1f;
        }
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

    private Transform ResolveAnimatedTransform()
    {
        Button parentButton = GetComponentInParent<Button>();
        return parentButton != null ? parentButton.transform : transform;
    }
}
