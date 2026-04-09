using UnityEngine;
using UnityEngine.InputSystem;

public class SquidController : MonoBehaviour
{
    [Header("Movement States")]
    public bool isEndlessRunnerMode = true; // Para despues permitir movimiento libre en jefes
    public float baseAutoScrollSpeed = 5f;      
    public float boostedAutoScrollSpeed = 15f;
    private float currentAutoScrollSpeed;

    [Header("Speeds (Up/Down)")]
    public float baseSpeed = 5f;
    public float boostedSpeed = 20f;
    public float boostDuration = 3f;
    public float smoothSpeedTransition = 2f;
    
    [Header("Boost System")]
    public float chargeRate = 150f;
    public float boostCharge = 0f;
    public ChargeBar boostChargeBar;

    [Header("Boundaries")]
    public float minY = -9.5f; // Limite inferior
    public float maxY = 9.5f;  // Limite superior
    [SerializeField] private string topBorderTag = "TopBorder";
    [SerializeField] private string bottomBorderTag = "BottomBorder";

    [Header("Tilt")]
    public float baseRotationZ = -90f;
    public float maxTiltAngle = 12f;
    public float tiltSmoothSpeed = 8f;
    
    private float currentSpeed;
    private float boostTimer = 0f;
    private bool isBoosting = false;
    private Camera mainCamera;
    private Collider2D playerCollider;
    private Collider2D topBorder;
    private Collider2D bottomBorder;
    private float previousY;

    [Header("Collectibles")]
    public int shrimps = 0;

    void Start()
    {
        mainCamera = Camera.main;
        currentSpeed = baseSpeed;
        currentAutoScrollSpeed = baseAutoScrollSpeed;
        playerCollider = GetComponent<Collider2D>();

        ResolveBorders();
        UpdateVerticalLimitsFromBorders();
        ClampPlayerInsideLimits();
        previousY = transform.position.y;

        Vector3 startEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(startEuler.x, startEuler.y, baseRotationZ);
    }

    void Update()
    {
        if (topBorder == null || bottomBorder == null)
        {
            ResolveBorders();
            UpdateVerticalLimitsFromBorders();
        }

        HandleMovement();
        HandleBoostLogic();
        ClampPlayerInsideLimits();

        float deltaY = transform.position.y - previousY;
        float verticalSpeed = Time.deltaTime > 0f ? deltaY / Time.deltaTime : 0f;
        UpdateTilt(verticalSpeed);
        previousY = transform.position.y;
    }

    private void UpdateTilt(float verticalSpeed)
    {
        float speedReference = Mathf.Max(baseSpeed, 0.01f);
        float normalizedVertical = Mathf.Clamp(verticalSpeed / speedReference, -1f, 1f);
        float targetZ = baseRotationZ + (normalizedVertical * maxTiltAngle);

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetZ);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, tiltSmoothSpeed * Time.deltaTime);
    }

    private void ResolveBorders()
    {
        GameObject topObj = GameObject.FindGameObjectWithTag(topBorderTag);
        GameObject bottomObj = GameObject.FindGameObjectWithTag(bottomBorderTag);

        if (topObj != null)
        {
            topBorder = topObj.GetComponent<Collider2D>();
        }

        if (bottomObj != null)
        {
            bottomBorder = bottomObj.GetComponent<Collider2D>();
        }
    }

    private void UpdateVerticalLimitsFromBorders()
    {
        if (topBorder == null || bottomBorder == null)
        {
            return;
        }

        float halfHeight = playerCollider != null ? playerCollider.bounds.extents.y : 0f;
        float candidateMinY = bottomBorder.bounds.max.y + halfHeight;
        float candidateMaxY = topBorder.bounds.min.y - halfHeight;

        if (candidateMinY <= candidateMaxY)
        {
            minY = candidateMinY;
            maxY = candidateMaxY;
        }
    }

    private void ClampPlayerInsideLimits()
    {
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
        transform.position = new Vector3(transform.position.x, clampedY, 0f);
    }

    private void HandleMovement()
    //Funcion general de movimiento por mouse y camara
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWithDepth = new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane + 10f);
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mouseWithDepth);
        worldMousePos.z = 0;

        if (isEndlessRunnerMode)
        {
            transform.position += Vector3.right * currentAutoScrollSpeed * Time.deltaTime;
            
            float newY = Mathf.MoveTowards(transform.position.y, worldMousePos.y, currentSpeed * Time.deltaTime);
            newY = Mathf.Clamp(newY, minY, maxY);
            transform.position = new Vector3(transform.position.x, newY, 0);
        }
        else
        {
            Vector3 direction = (worldMousePos - transform.position).normalized;
            Vector3 targetPosition = transform.position + direction * currentSpeed * Time.deltaTime;
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            
            if (Vector3.Distance(transform.position, worldMousePos) > 0.1f)
            {
                transform.position = targetPosition;
            }
        }
    }

    private void HandleBoostLogic()
    //Funcion que maneja la logica del boost
    {
        if (Mouse.current.leftButton.isPressed && !isBoosting && boostCharge >= 100f)
        {
            StartBoost();
        }

        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoosting = false;
                boostCharge = 0f;
                boostChargeBar.ResetBar();
            }
        }

        if (!isBoosting)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, smoothSpeedTransition * Time.deltaTime);
            currentAutoScrollSpeed = Mathf.Lerp(currentAutoScrollSpeed, baseAutoScrollSpeed, smoothSpeedTransition * Time.deltaTime);
        }
    }

    private void StartBoost()
    //Funcion que maneja el boost
    {
        isBoosting = true;
        boostTimer = boostDuration;
        
        currentSpeed = boostedSpeed;
        currentAutoScrollSpeed = boostedAutoScrollSpeed;
        
        boostChargeBar.ResetBar();
    }

    public void AddGrazeCharge(float amount)
    //Funcion que maneja las cargas del boost
    {
        if (!isBoosting)
        {
            boostCharge += amount;
            boostCharge = Mathf.Clamp(boostCharge, 0f, 100f);
            boostChargeBar.UpdateBar(boostCharge / 100f);
        }
    }
    private void OnTriggerEnter2D(Collider2D other) 
    //Hitbox que maneja coleccionables y muerte por enemigos
    {
        if (other.CompareTag("Shrimp"))
        {
            shrimps++;
            Debug.Log("Shrimps: " + shrimps);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            Debug.Log("YOU DIED!");
            
            Destroy(gameObject); 
        }
    }
    
}