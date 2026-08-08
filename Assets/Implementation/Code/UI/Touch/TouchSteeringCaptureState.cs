using UnityEngine;

public sealed class TouchSteeringCaptureState
{
    private bool hasActivePointer;
    private int activePointerId;
    private Vector2 screenPosition;

    public bool HasActivePointer => hasActivePointer;
    public int ActivePointerId => activePointerId;
    public Vector2 ScreenPosition => screenPosition;

    public bool TryBegin(
        int pointerId,
        Vector2 initialScreenPosition,
        bool isAvailable,
        bool startedOverInteractiveUi)
    {
        if (!isAvailable || startedOverInteractiveUi || hasActivePointer)
        {
            return false;
        }

        activePointerId = pointerId;
        screenPosition = initialScreenPosition;
        hasActivePointer = true;
        return true;
    }

    public bool TryMove(int pointerId, Vector2 nextScreenPosition)
    {
        if (!hasActivePointer || pointerId != activePointerId)
        {
            return false;
        }

        screenPosition = nextScreenPosition;
        return true;
    }

    public bool TryEnd(int pointerId)
    {
        if (!hasActivePointer || pointerId != activePointerId)
        {
            return false;
        }

        Clear();
        return true;
    }

    public bool Cancel()
    {
        if (!hasActivePointer)
        {
            return false;
        }

        Clear();
        return true;
    }

    private void Clear()
    {
        hasActivePointer = false;
        activePointerId = 0;
        screenPosition = Vector2.zero;
    }
}
