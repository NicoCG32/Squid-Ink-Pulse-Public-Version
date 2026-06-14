using UnityEngine;

public static class SpawnPositionResolver
{
    public static bool TryCalculateCoinSpawnPosition(
        Camera spawnCamera,
        float verticalPadding,
        float spawnDistanceFromCameraRight,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculateVisibleSpawnRange(spawnCamera, verticalPadding, out Vector2 visibleRange)
            || !TryCalculatePlayerSpawnRange(verticalPadding, out Vector2 playerRange))
        {
            return false;
        }

        float minY = Mathf.Max(visibleRange.x, playerRange.x);
        float maxY = Mathf.Min(visibleRange.y, playerRange.y);
        if (minY > maxY)
        {
            return false;
        }

        spawnPosition = new Vector3(
            GetCameraRightEdgeX(spawnCamera) + Mathf.Max(0f, spawnDistanceFromCameraRight),
            UnityEngine.Random.Range(minY, maxY),
            0f);
        return true;
    }

    public static bool TryCalculatePortalSpawnPosition(
        Camera spawnCamera,
        float verticalPadding,
        float spawnDistanceFromCameraRight,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculatePlayerSpawnRange(verticalPadding, out Vector2 playerRange))
        {
            return false;
        }

        spawnPosition = new Vector3(
            GetCameraRightEdgeX(spawnCamera) + Mathf.Max(0f, spawnDistanceFromCameraRight),
            UnityEngine.Random.Range(playerRange.x, playerRange.y),
            0f);
        return true;
    }

    public static bool TryCalculateDealerFishSpawnPosition(
        Camera spawnCamera,
        float verticalPadding,
        float spawnDistanceFromCameraRight,
        float spawnZoneMin,
        float spawnZoneMax,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (!TryCalculatePlayerSpawnRange(verticalPadding, out Vector2 playerRange))
        {
            return false;
        }

        float minNormalized = Mathf.Clamp(spawnZoneMin, 0f, 0.5f);
        float maxNormalized = Mathf.Clamp(spawnZoneMax, minNormalized, 0.5f);
        float minY = Mathf.Lerp(playerRange.x, playerRange.y, minNormalized);
        float maxY = Mathf.Lerp(playerRange.x, playerRange.y, maxNormalized);

        spawnPosition = new Vector3(
            GetCameraRightEdgeX(spawnCamera) + Mathf.Max(0f, spawnDistanceFromCameraRight),
            UnityEngine.Random.Range(minY, maxY),
            0f);
        return true;
    }

    public static bool TryCalculateEnemySpawnPosition(
        Camera spawnCamera,
        Transform player,
        RunProgressionDirector progression,
        ZoneSpawnProfile zoneProfile,
        string enemyTag,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (spawnCamera == null || zoneProfile == null)
        {
            return false;
        }

        if (!TryCalculatePlayerSpawnRange(zoneProfile.VerticalPadding, out Vector2 playerRange))
        {
            return false;
        }

        float centerY = (playerRange.x + playerRange.y) * 0.5f;
        float spawnX = GetCameraRightEdgeX(spawnCamera) + zoneProfile.SpawnDistanceFromCameraRight;

        if (enemyTag == EnemyTagCatalog.Pufferfish)
        {
            spawnPosition = new Vector3(spawnX, RandomInUpperCoverage(playerRange, centerY, zoneProfile.UpperZoneSpawnCoverage), 0f);
            return true;
        }

        if (enemyTag == EnemyTagCatalog.FishingRod)
        {
            float laneY = CalculateFishingRodPlayerY(player, playerRange, centerY);
            spawnPosition = new Vector3(CalculateFishingRodSpawnX(spawnCamera, player, progression, zoneProfile, laneY), laneY, 0f);
            return true;
        }

        spawnPosition = new Vector3(spawnX, RandomInLowerCoverage(playerRange, centerY, zoneProfile.LowerZoneSpawnCoverage), 0f);
        return true;
    }

    private static float GetCameraRightEdgeX(Camera spawnCamera)
    {
        if (spawnCamera == null)
        {
            return 0f;
        }

        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        Vector3 rightEdge = spawnCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, cameraDepthToWorldZero));
        return rightEdge.x;
    }

    private static bool TryCalculateVisibleSpawnRange(Camera spawnCamera, float verticalPadding, out Vector2 spawnRange)
    {
        spawnRange = default;
        if (spawnCamera == null
            || !BoundaryReferenceResolver.TryResolveInnerVerticalRange(
                BoundaryReferenceDomain.Camera,
                Mathf.Max(0f, verticalPadding),
                out Vector2 boundaryRange))
        {
            return false;
        }

        float cameraDepthToWorldZero = Mathf.Abs(spawnCamera.transform.position.z);
        float cameraMinY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, cameraDepthToWorldZero)).y + verticalPadding;
        float cameraMaxY = spawnCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, cameraDepthToWorldZero)).y - verticalPadding;

        float minY = Mathf.Max(cameraMinY, boundaryRange.x);
        float maxY = Mathf.Min(cameraMaxY, boundaryRange.y);
        if (minY > maxY)
        {
            return false;
        }

        spawnRange = new Vector2(minY, maxY);
        return true;
    }

    private static bool TryCalculatePlayerSpawnRange(float verticalPadding, out Vector2 playerRange)
    {
        return BoundaryReferenceResolver.TryResolveInnerVerticalRange(
            BoundaryReferenceDomain.Player,
            Mathf.Max(0f, verticalPadding),
            out playerRange);
    }

    private static float RandomInUpperCoverage(Vector2 playerRange, float centerY, float coverage)
    {
        float upperHeight = Mathf.Max(0.01f, playerRange.y - centerY);
        float minY = playerRange.y - (upperHeight * Mathf.Clamp(coverage, 0.01f, 1f));
        return UnityEngine.Random.Range(minY, playerRange.y);
    }

    private static float RandomInLowerCoverage(Vector2 playerRange, float centerY, float coverage)
    {
        float lowerHeight = Mathf.Max(0.01f, centerY - playerRange.x);
        float maxY = playerRange.x + (lowerHeight * Mathf.Clamp(coverage, 0.01f, 1f));
        return UnityEngine.Random.Range(playerRange.x, maxY);
    }

    private static float CalculateFishingRodPlayerY(Transform player, Vector2 playerRange, float centerY)
    {
        return player != null
            ? Mathf.Clamp(player.position.y, playerRange.x, playerRange.y)
            : centerY;
    }

    private static float CalculateFishingRodSpawnX(
        Camera spawnCamera,
        Transform player,
        RunProgressionDirector progression,
        ZoneSpawnProfile zoneProfile,
        float targetY)
    {
        FishingRodEnemyTuning tuning = zoneProfile.FishingRodTuning;
        float dropStartY = CalculateFishingRodStartY(targetY, tuning);
        float dropDistance = Mathf.Max(0f, dropStartY - targetY);
        float dropDuration = dropDistance / tuning.DropSpeed;
        float playerSpeed = GetCurrentPlayerHorizontalSpeed(player, progression);
        float dynamicLeadDistance = playerSpeed * (dropDuration + tuning.HorizontalLeadTimePaddingSeconds);
        float minimumDistance = Mathf.Max(zoneProfile.SpawnDistanceFromCameraRight, tuning.MinimumHorizontalLeadDistance);
        float spawnDistance = Mathf.Max(minimumDistance, dynamicLeadDistance);

        return GetCameraRightEdgeX(spawnCamera) + spawnDistance;
    }

    private static float CalculateFishingRodStartY(float targetY, FishingRodEnemyTuning tuning)
    {
        if (BoundaryReferenceResolver.TryResolve(BoundaryReferenceDomain.Player, out Collider2D topBorder, out _))
        {
            float startY = topBorder.bounds.min.y - tuning.StartYOffsetBelowTopBoundary;
            return Mathf.Max(startY, targetY);
        }

        return targetY;
    }

    private static float GetCurrentPlayerHorizontalSpeed(Transform player, RunProgressionDirector progression)
    {
        if (player != null && player.TryGetComponent(out PlayerMovement movement))
        {
            return Mathf.Max(0f, movement.CurrentHorizontalSpeed);
        }

        if (progression != null)
        {
            return Mathf.Max(0f, progression.Current.TargetScrollSpeed);
        }

        return 0f;
    }
}
