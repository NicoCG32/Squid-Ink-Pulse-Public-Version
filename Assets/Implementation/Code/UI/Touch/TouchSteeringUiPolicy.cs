using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class TouchSteeringUiPolicy
{
    public static bool StartedOverInteractiveUi(Transform surfaceRoot, GameObject raycastTarget)
    {
        if (surfaceRoot == null || raycastTarget == null)
        {
            return false;
        }

        Transform current = raycastTarget.transform;
        if (current != surfaceRoot && !current.IsChildOf(surfaceRoot))
        {
            return true;
        }

        while (current != null)
        {
            if (current.GetComponent<Selectable>() != null)
            {
                return true;
            }

            if (current != surfaceRoot && HasInteractiveEventHandler(current.gameObject))
            {
                return true;
            }

            if (current == surfaceRoot)
            {
                break;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasInteractiveEventHandler(GameObject candidate)
    {
        MonoBehaviour[] behaviours = candidate.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour is IPointerClickHandler
                || behaviour is IPointerDownHandler
                || behaviour is ISubmitHandler
                || behaviour is IDragHandler)
            {
                return true;
            }
        }

        return false;
    }
}
