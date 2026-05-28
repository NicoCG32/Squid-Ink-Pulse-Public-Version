using UnityEngine;

public class GrazeDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulseController;

    [Header("Tags")]
    [SerializeField] private string enemyTag = "Enemy";

    private void Awake()
    {
        if (session == null || inkPulseController == null)
        {
            Debug.LogWarning("[GrazeDetector] Faltan referencias. Asigna Session e InkPulseController en el Inspector.", this);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (session == null || !session.IsPlaying)
        {
            return;
        }

        if (!other.CompareTag(enemyTag))
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
}
