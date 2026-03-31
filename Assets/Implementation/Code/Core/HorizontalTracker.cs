using UnityEngine;

public class HorizontalTracker : MonoBehaviour
{
    private Transform cameraTransform;
    private float startingY;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        
        startingY = transform.position.y;
    }

    void LateUpdate()
    {
        if (cameraTransform != null)
        {
            transform.position = new Vector3(cameraTransform.position.x, startingY, transform.position.z);
        }
    }
}