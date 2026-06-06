using UnityEngine;

[DisallowMultipleComponent]
public class LightGrazeProbe : MonoBehaviour
{
    [SerializeField] private GameSessionController session;

    private void Update()
    {
        if (!IsGameplayActive() || !ZoneLightingController.HasInstance)
        {
            return;
        }

        ZoneLightingController controller = ZoneLightingController.Instance;
        float radiusSqr = controller.LightGrazeRadius * controller.LightGrazeRadius;
        Vector3 origin = transform.position;

        for (int i = LightGrazeSource.ActiveSourceCount - 1; i >= 0; i--)
        {
            LightGrazeSource source = LightGrazeSource.GetActiveSource(i);
            if (source == null || SharesRootWithProbe(source.transform))
            {
                continue;
            }

            if ((source.GetClosestPoint(origin) - origin).sqrMagnitude <= radiusSqr)
            {
                controller.NotifyLightGraze();
                return;
            }
        }
    }

    private bool SharesRootWithProbe(Transform source)
    {
        return source != null && source.root == transform.root;
    }

    private bool IsGameplayActive()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        return session != null && session.IsPlaying;
    }
}
