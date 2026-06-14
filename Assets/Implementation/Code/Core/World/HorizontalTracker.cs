using UnityEngine;

[DisallowMultipleComponent]
public class HorizontalTracker : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private float startingY;

    private void Start()
    {
        ResolveCameraTransform();
        if (cameraTransform == null)
        {
            Debug.LogWarning("[HorizontalTracker] Falta asignar CameraTransform en el Inspector.", this);
        }

        startingY = transform.position.y;
    }

    private void LateUpdate()
    {
        ResolveCameraTransform();
        if (cameraTransform != null)
        {
            transform.position = new Vector3(cameraTransform.position.x, startingY, transform.position.z);
        }
    }

    private void ResolveCameraTransform()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }
}
