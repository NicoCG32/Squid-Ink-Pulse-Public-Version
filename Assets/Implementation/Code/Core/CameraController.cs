using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 

    public Vector3 offset = new Vector3(3f, 0f, -10f); 

    [Header("Dynamics")]
    public float smoothTime = 0.25f;

  
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {

        if (target == null) return;


        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
}