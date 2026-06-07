using UnityEngine;

public class GrazeDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulseController;

    private void Awake()
    {
        ResolveReferences();

        if (session == null || inkPulseController == null)
        {
            Debug.LogWarning("[GrazeDetector] Faltan referencias. Asigna Session e InkPulseController en el Inspector.", this);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ResolveReferences();

        if (session == null || !session.IsPlaying)
        {
            return;
        }

        if (!EnemyTagCatalog.IsEnemy(other))
        {
            return;
        }

        if (inkPulseController == null)
        {
            return;
        }

        float chargeAmount = inkPulseController.ChargeRate * Time.deltaTime;
        inkPulseController.AddGrazeCharge(chargeAmount);
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (inkPulseController == null)
        {
            inkPulseController = GetComponentInParent<InkPulseController>();
        }
    }
}
