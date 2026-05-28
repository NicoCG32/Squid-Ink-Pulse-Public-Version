using UnityEngine;

[DisallowMultipleComponent]
public class BossEventDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private CameraController eventCameraController;
    [SerializeField] private Collider2D topBorder;
    [SerializeField] private Collider2D bottomBorder;
    [SerializeField] private Transform bossParent;

    [Header("Boss Event")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private float triggerAfterSeconds = 45f;
    [SerializeField] private float spawnDistanceFromCameraRight = 4f;
    [SerializeField, Range(0f, 1f)] private float viewportY = 0.5f;
    [SerializeField] private bool triggerOnce;

    [Header("Camera Cue")]
    [SerializeField] private float wideCameraHoldSeconds = 12f;
    [SerializeField] private float wideCameraTransitionSmoothTime = 1f;
    [SerializeField] private float wideCameraExtraTopSpace = 4f;

    private float elapsedGameplaySeconds;
    private bool hasTriggered;

    private void Awake()
    {
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (!CanTick())
        {
            return;
        }

        elapsedGameplaySeconds += Time.deltaTime;
        if (elapsedGameplaySeconds < triggerAfterSeconds)
        {
            return;
        }

        TriggerBossEvent();
    }

    public void ResetDirector()
    {
        elapsedGameplaySeconds = 0f;
        hasTriggered = false;
    }

    private bool CanTick()
    {
        if (session == null || !session.IsPlaying)
        {
            return false;
        }

        if (bossPrefab == null || spawnCamera == null)
        {
            return false;
        }

        return !triggerOnce || !hasTriggered;
    }

    private void TriggerBossEvent()
    {
        hasTriggered = true;
        eventCameraController?.RequestFullVerticalView(
            wideCameraHoldSeconds,
            wideCameraTransitionSmoothTime,
            wideCameraExtraTopSpace);

        Vector3 spawnPosition = CalculateSpawnPosition();
        GameObject bossInstance = Instantiate(bossPrefab, spawnPosition, Quaternion.identity, bossParent);
        InjectSpawnContext(bossInstance);

        if (!triggerOnce)
        {
            elapsedGameplaySeconds = 0f;
        }
    }

    private Vector3 CalculateSpawnPosition()
    {
        float depthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        Vector3 rightEdge = spawnCamera.ViewportToWorldPoint(new Vector3(1f, viewportY, depthToWorldZero));
        return new Vector3(rightEdge.x + spawnDistanceFromCameraRight, rightEdge.y, 0f);
    }

    private void InjectSpawnContext(GameObject bossInstance)
    {
        MonoBehaviour[] behaviours = bossInstance.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IBossSpawnContextReceiver receiver)
            {
                receiver.InitializeBossSpawnContext(session, spawnCamera, topBorder, bottomBorder, bossParent);
            }
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null
            || spawnCamera == null
            || eventCameraController == null
            || topBorder == null
            || bottomBorder == null
            || bossParent == null
            || bossPrefab == null)
        {
            Debug.LogWarning(
                "[BossEventDirector] Faltan referencias. Asigna Session, SpawnCamera, EventCameraController, TopBorder, BottomBorder, BossParent y BossPrefab en el Inspector.",
                this);
        }
    }
}

public interface IBossSpawnContextReceiver
{
    void InitializeBossSpawnContext(
        GameSessionController sessionReference,
        Camera cameraReference,
        Collider2D topBorderReference,
        Collider2D bottomBorderReference,
        Transform parentReference);
}
