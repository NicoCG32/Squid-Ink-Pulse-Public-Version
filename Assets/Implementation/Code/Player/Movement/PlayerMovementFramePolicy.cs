using UnityEngine;

public static class PlayerMovementFramePolicy
{
    public static Vector2 ResolveNextPosition(
        Vector2 currentPosition,
        float horizontalSpeed,
        PlayerVerticalMovementStep verticalStep,
        float minY,
        float maxY,
        float deltaTime)
    {
        float nextX = currentPosition.x + horizontalSpeed * deltaTime;
        float candidateY = verticalStep.HasMovement
            ? verticalStep.NextY
            : currentPosition.y;
        float nextY = Mathf.Clamp(candidateY, minY, maxY);
        return new Vector2(nextX, nextY);
    }
}
