using System.Collections;
using UnityEngine;

public class FlappyBossController : MonoBehaviour, IBossSpawnContextReceiver
{
    [Header("Prefabs")]
    [SerializeField] private GameObject pillarObstaclePrefab;

    [Header("Movement Settings")]
    [Tooltip("How long it takes to swim to the top center.")]
    [SerializeField] private float introDuration = 2f;
    [Tooltip("How long it takes to run away to the left.")]
    [SerializeField] private float outroDuration = 1.5f;
    
    [Tooltip("Where the boss waits during the attack (X=0.5 is center, Y=0.85 is near top)")]
    [SerializeField] private Vector2 attackViewportPosition = new Vector2(0.5f, 0.85f);

    [Header("Attack Timing")]
    [SerializeField] private float attackDuration = 15f;
    [SerializeField] private float timeBetweenSpawns = 1.5f; 
    [SerializeField] private bool spawnFinalContinuousWall = true;

    [Header("Spawn Layout")]
    [SerializeField] private float spawnDistanceAhead = 15f; 
    
    [Header("Fair Spawn Logic")]
    [SerializeField] private float absoluteMaxVerticalOffset = 6f; 
    [SerializeField] private float maxJumpDistance = 5f; 

    [Header("Gap Settings")]
    [SerializeField] private float minGapSize = 2f; 
    [SerializeField] private float maxGapSize = 4f; 

    [Header("Pillar Reveal")]
    [SerializeField, Min(0f)] private float minRevealDuration = 0.35f;
    [SerializeField, Min(0f)] private float maxRevealDuration = 0.75f;
    [SerializeField, Min(0f)] private float maxRevealStagger = 0.25f;

    [Header("Scene References")]
    [SerializeField] private LevelSpawner levelSpawner;

    [Header("Camera Hold")]
    [SerializeField] private bool holdWideCameraUntilFinalWallResolved = true;
    [SerializeField, Min(0.01f)] private float wideCameraHoldTransitionSmoothTime = 1f;
    [SerializeField, Min(0f)] private float wideCameraHoldExtraTopSpace = 4f;

    private Camera mainCamera;
    private CameraController eventCameraController;
    private float lastGapY = 0f; 
    private RunProgressionDirector progression;
    private Transform pillarParent;
    private PillarObstacle finalWall;
    private bool finalWallResolved;
    private bool bossEventCompleted;
    private bool wideCameraHoldActive;
    
    // This variable tells the Update method where the boss should be on the screen right now
    private Vector2 currentViewportTarget = new Vector2(1.2f, 0.5f); 

    public void InitializeBossSpawnContext(
        GameSessionController sessionReference,
        RunProgressionDirector progressionReference,
        Camera cameraReference,
        Transform parentReference)
    {
        mainCamera = cameraReference;
        eventCameraController = ResolveCameraController(mainCamera);
        BeginWideCameraHold();
        
        // 2. Save the progression reference!
        progression = progressionReference; 
        pillarParent = parentReference;
        
        levelSpawner ??= FindFirstObjectByType<LevelSpawner>();
        if (levelSpawner != null)
        {
            levelSpawner.gameObject.SetActive(false);
        }
        lastGapY = TryResolvePillarVerticalRange(out Vector2 initialRange)
            ? (initialRange.x + initialRange.y) * 0.5f
            : 0f;
        currentViewportTarget = new Vector2(1.2f, 0.5f);
        
        StartCoroutine(BossSequence());
    }

    private void Update()
    {
        // Continuously pin the boss to whatever screen coordinate the Coroutine dictates
        if (mainCamera != null)
        {
            float depth = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 targetWorldPos = mainCamera.ViewportToWorldPoint(new Vector3(currentViewportTarget.x, currentViewportTarget.y, depth));
            
            // Apply the position, locking Z to 0
            transform.position = new Vector3(targetWorldPos.x, targetWorldPos.y, 0f);
        }
    }

    private IEnumerator BossSequence()
    {
        // --- PHASE 1: THE INTRO (Swim to top center) ---
        float elapsed = 0f;
        Vector2 startPos = currentViewportTarget;
        
        while (elapsed < introDuration)
        {
            // Lerp smoothly slides the value from the start position to the attack position
            currentViewportTarget = Vector2.Lerp(startPos, attackViewportPosition, elapsed / introDuration);
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        currentViewportTarget = attackViewportPosition; // Ensure it locks perfectly into place


        // --- PHASE 2: THE ATTACK (Spawn pillars) ---
        elapsed = 0f;
        float safeSpawnInterval = Mathf.Max(0.01f, timeBetweenSpawns);
        while (elapsed < attackDuration)
        {
            bool isFinalPillar = spawnFinalContinuousWall && elapsed + safeSpawnInterval >= attackDuration;
            PillarObstacle spawnedPillar = SpawnPillarObstacle(isFinalPillar);
            if (isFinalPillar && spawnedPillar != null)
            {
                TrackFinalWall(spawnedPillar);
            }

            yield return new WaitForSeconds(safeSpawnInterval);
            elapsed += safeSpawnInterval;
        }

        // --- PHASE 3: THE OUTRO (Run away to the left) ---
        elapsed = 0f;
        Vector2 endPos = new Vector2(-0.3f, attackViewportPosition.y); 
        
        while (elapsed < outroDuration)
        {
            currentViewportTarget = Vector2.Lerp(attackViewportPosition, endPos, elapsed / outroDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (finalWall != null && !finalWallResolved)
        {
            while (finalWall != null && !finalWallResolved)
            {
                yield return null;
            }
        }

        CompleteBossEvent();
    }
    private PillarObstacle SpawnPillarObstacle(bool continuousWall)
    {
        if (pillarObstaclePrefab == null || mainCamera == null) return null;

        float depth = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 rightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth));
        float spawnX = rightEdge.x + spawnDistanceAhead;

        if (!TryResolvePillarVerticalRange(out Vector2 verticalRange))
        {
            return null;
        }

        float availableHeight = Mathf.Max(0.1f, verticalRange.y - verticalRange.x);
        float randomGapSize = continuousWall
            ? 0f
            : Mathf.Clamp(
                Random.Range(Mathf.Min(minGapSize, maxGapSize), Mathf.Max(minGapSize, maxGapSize)),
                0.1f,
                availableHeight);
        float nextGapY = continuousWall
            ? (verticalRange.x + verticalRange.y) * 0.5f
            : CalculateNextGapCenter(verticalRange, randomGapSize);
        lastGapY = nextGapY;

        Vector3 spawnPosition = new Vector3(spawnX, 0f, 0f); 
        GameObject newPillar = Instantiate(pillarObstaclePrefab, spawnPosition, Quaternion.identity, pillarParent);
        
        if (newPillar.TryGetComponent(out PillarObstacle obstacle))
        {
            float minReveal = Mathf.Max(0f, Mathf.Min(minRevealDuration, maxRevealDuration));
            float maxReveal = Mathf.Max(minReveal, Mathf.Max(minRevealDuration, maxRevealDuration));
            float revealStagger = Mathf.Max(0f, maxRevealStagger);
            float topRevealDelay = continuousWall ? 0f : Random.Range(0f, revealStagger);
            float bottomRevealDelay = continuousWall ? 0f : Random.Range(0f, revealStagger);
            obstacle.Setup(
                nextGapY,
                randomGapSize,
                verticalRange.y,
                verticalRange.x,
                Random.Range(minReveal, maxReveal),
                topRevealDelay,
                bottomRevealDelay,
                continuousWall);

            return obstacle;
        }

        return null;
    }

    private void TrackFinalWall(PillarObstacle wall)
    {
        if (finalWall != null)
        {
            finalWall.Resolved -= HandleFinalWallResolved;
        }

        finalWall = wall;
        finalWallResolved = false;
        finalWall.Resolved += HandleFinalWallResolved;
    }

    private void HandleFinalWallResolved(PillarObstacle wall)
    {
        if (wall != finalWall)
        {
            return;
        }

        finalWall.Resolved -= HandleFinalWallResolved;
        finalWallResolved = true;
        finalWall = null;
    }

    private void CompleteBossEvent()
    {
        if (bossEventCompleted)
        {
            return;
        }

        bossEventCompleted = true;

        if (levelSpawner != null)
        {
            levelSpawner.gameObject.SetActive(true);
        }

        EndWideCameraHold();
        progression?.NotifyBossResolved();
        Destroy(gameObject);
    }

    private void BeginWideCameraHold()
    {
        if (!holdWideCameraUntilFinalWallResolved || eventCameraController == null || wideCameraHoldActive)
        {
            return;
        }

        wideCameraHoldActive = eventCameraController.BeginFullVerticalViewHold(
            wideCameraHoldTransitionSmoothTime,
            wideCameraHoldExtraTopSpace);
    }

    private static CameraController ResolveCameraController(Camera cameraReference)
    {
        if (cameraReference == null)
        {
            return null;
        }

        return cameraReference.GetComponent<CameraController>()
            ?? cameraReference.GetComponentInParent<CameraController>()
            ?? cameraReference.GetComponentInChildren<CameraController>();
    }

    private void EndWideCameraHold()
    {
        if (!wideCameraHoldActive || eventCameraController == null)
        {
            wideCameraHoldActive = false;
            return;
        }

        eventCameraController.EndFullVerticalViewHold();
        wideCameraHoldActive = false;
    }

    private void OnDestroy()
    {
        if (finalWall != null)
        {
            finalWall.Resolved -= HandleFinalWallResolved;
            finalWall = null;
        }

        EndWideCameraHold();
    }

    private bool TryResolvePillarVerticalRange(out Vector2 verticalRange)
    {
        if (BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                BoundaryReferenceDomain.Camera,
                0f,
                out verticalRange))
        {
            return true;
        }

        verticalRange = default;
        if (mainCamera == null)
        {
            return false;
        }

        float depth = Mathf.Abs(mainCamera.transform.position.z);
        float topY = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth)).y;
        float bottomY = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)).y;
        if (bottomY > topY)
        {
            return false;
        }

        verticalRange = new Vector2(bottomY, topY);
        return true;
    }

    private float CalculateNextGapCenter(Vector2 verticalRange, float gapSize)
    {
        float halfGap = gapSize * 0.5f;
        float centerY = (verticalRange.x + verticalRange.y) * 0.5f;
        float minCenterY = verticalRange.x + halfGap;
        float maxCenterY = verticalRange.y - halfGap;

        float boundedMin = Mathf.Max(
            minCenterY,
            centerY - Mathf.Max(0f, absoluteMaxVerticalOffset),
            lastGapY - Mathf.Max(0f, maxJumpDistance));
        float boundedMax = Mathf.Min(
            maxCenterY,
            centerY + Mathf.Max(0f, absoluteMaxVerticalOffset),
            lastGapY + Mathf.Max(0f, maxJumpDistance));

        if (boundedMin > boundedMax)
        {
            boundedMin = minCenterY;
            boundedMax = maxCenterY;
        }

        if (boundedMin > boundedMax)
        {
            return centerY;
        }

        return Random.Range(boundedMin, boundedMax);
    }
}
