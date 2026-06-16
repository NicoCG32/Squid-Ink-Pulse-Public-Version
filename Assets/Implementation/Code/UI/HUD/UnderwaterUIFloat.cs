using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UnderwaterUIFloat : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How fast the UI element drifts.")]
    [SerializeField] private float driftSpeed = 0.5f;
    
    [Tooltip("Maximum pixel movement left and right.")]
    [SerializeField] private float maxDistanceX = 2f;
    
    [Tooltip("Maximum pixel movement up and down.")]
    [SerializeField] private float maxDistanceY = 3f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private float timeOffsetX;
    private float timeOffsetY;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        timeOffsetX = Random.Range(0f, 100f);
        timeOffsetY = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float currentXTime = (Time.time * driftSpeed) + timeOffsetX;
        
        float currentYTime = (Time.time * driftSpeed * 0.8f) + timeOffsetY;

        float offsetX = Mathf.Sin(currentXTime) * maxDistanceX;
        float offsetY = Mathf.Cos(currentYTime) * maxDistanceY;

        rectTransform.anchoredPosition = originalPosition + new Vector2(offsetX, offsetY);
    }
}