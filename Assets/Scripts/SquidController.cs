using UnityEngine;
using UnityEngine.InputSystem;

public class SquidController : MonoBehaviour
{
  public float baseSpeed = 5f;    // Normal speed of the player
    public float boostedSpeed = 20f; // Speed when boosted
    public float boostDuration = 3f; // Duration of the speed boost
    public float smoothSpeedTransition = 2f; // How fast the speed returns to normal after boost

    private float currentSpeed;
    private float boostTimer = 0f;
    private bool isBoosting = false;
    private Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
        currentSpeed = baseSpeed; // Set initial speed to normal speed
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;

        Vector3 direction = (worldMousePos - transform.position).normalized;

        transform.position += direction * currentSpeed * Time.deltaTime;

        if(Mouse.current.leftButton.isPressed && !isBoosting)
        {
            StartBoost();
        }
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoosting = false;
            }
        }
        if (!isBoosting)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, smoothSpeedTransition * Time.deltaTime);
        }
    }
    private void StartBoost()
    {
        isBoosting = true;
        boostTimer = boostDuration;
        currentSpeed = boostedSpeed;
    }

}
