using UnityEngine;

public class PillarObstacle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topPillar;
    [SerializeField] private Transform bottomPillar;

    [Header("Settings")]
    [SerializeField] private float lifetime = 15f; 
    
    [Tooltip("How tall the pillar sprites are. Make this a huge number!")]
    [SerializeField] private float pillarHeight = 50f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Setup(float gapCenterY, float gapSize, float screenTopY, float screenBottomY)
    {
        if (topPillar == null || bottomPillar == null) return;

        float halfGap = gapSize / 2f;
        
        // Calculate exactly where the edges of the gap are
        float gapTopEdge = gapCenterY + halfGap;
        float gapBottomEdge = gapCenterY - halfGap;

        // Force the scale to be massive vertically
        topPillar.localScale = new Vector3(topPillar.localScale.x, pillarHeight, 1f);
        bottomPillar.localScale = new Vector3(bottomPillar.localScale.x, pillarHeight, 1f);

        // Position the center of the massive pillars so their edges perfectly touch the gap
        // (This assumes your sprite's Pivot is set to "Center")
        float halfPillarHeight = pillarHeight / 2f;
        
        topPillar.position = new Vector3(transform.position.x, gapTopEdge + halfPillarHeight, 0f);
        bottomPillar.position = new Vector3(transform.position.x, gapBottomEdge - halfPillarHeight, 0f);
    }
}