using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class BossEventDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private RunProgressionDirector progression;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private CameraController eventCameraController;
    [SerializeField] private Transform bossParent = null;

    [Header("Boss Event")]
    [SerializeField] private GameObject bossPrefab = null;
    [SerializeField] private float triggerAfterSeconds = 45f;
    [SerializeField, FormerlySerializedAs("spawnDistanceFromCameraLeft")] private float spawnDistanceFromCameraRight = 1f;
    [SerializeField, Range(0f, 1f)] private float viewportY = 0.5f;
    [SerializeField] private bool triggerOnce = false;

    [Header("Camera Cue")]
    [SerializeField] private float wideCameraHoldSeconds = 12f;
    [SerializeField] private float wideCameraTransitionSmoothTime = 1f;
    [SerializeField] private float wideCameraExtraTopSpace = 4f;

    private float elapsedGameplaySeconds;
    private bool hasTriggered;

    private void Awake()
    {
        ResolveSceneReferences();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (!CanTick())
        {
            return;
        }

        ResolveSceneReferences();
        if (progression != null)
        {
            if (!progression.TryStartBossEvent())
            {
                return;
            }

            TriggerBossEvent(resetLocalTimer: false);
            return;
        }

        elapsedGameplaySeconds += Time.deltaTime;
        if (elapsedGameplaySeconds < triggerAfterSeconds)
        {
            return;
        }

        TriggerBossEvent(resetLocalTimer: true);
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

    private void TriggerBossEvent(bool resetLocalTimer)
    {
        hasTriggered = true;
        eventCameraController?.RequestFullVerticalView(
            wideCameraHoldSeconds,
            wideCameraTransitionSmoothTime,
            wideCameraExtraTopSpace);

        Vector3 spawnPosition = CalculateSpawnPosition();
        GameObject bossInstance = Instantiate(bossPrefab, spawnPosition, Quaternion.identity, bossParent);
        InjectSpawnContext(bossInstance);

        if (resetLocalTimer && !triggerOnce)
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
                receiver.InitializeBossSpawnContext(session, progression, spawnCamera, bossParent);
            }
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null
            || spawnCamera == null
            || eventCameraController == null
            || !BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Camera, out _, out _)
            || bossParent == null
            || bossPrefab == null)
        {
            Debug.LogWarning(
                $"[BossEventDirector] Faltan referencias. Configura Session, SpawnCamera, EventCameraController, BossParent, BossPrefab y la jerarquia {BoundaryReferenceResolver.GetRequiredHierarchyDescription(BoundaryReferenceDomain.Camera)}.",
                this);
        }
    }

    private void ResolveSceneReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (progression == null && RunProgressionDirector.HasInstance)
        {
            progression = RunProgressionDirector.Instance;
        }

        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }

        if (eventCameraController == null && spawnCamera != null)
        {
            eventCameraController = spawnCamera.GetComponent<CameraController>();
        }

    }
}

public interface IBossSpawnContextReceiver
{
    void InitializeBossSpawnContext(
        GameSessionController sessionReference,
        RunProgressionDirector progressionReference,
        Camera cameraReference,
        Transform parentReference);
}
