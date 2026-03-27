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
    
    private float currentSpeed;
    private float boostTimer = 0f;
    private bool isBoosting = false;
    private Camera mainCamera;

    [Header("Collectibles")]
    public int shrimps = 0;

    void Start()
    {
        mainCamera = Camera.main;
        currentSpeed = baseSpeed;
        currentAutoScrollSpeed = baseAutoScrollSpeed;
    }

    void Update()
    {
        HandleMovement();
        HandleBoostLogic();
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