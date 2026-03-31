using UnityEngine;

public class GrazeDetector : MonoBehaviour
{
    [Header("Link to Parent")]
    public SquidController playerController;

    private void OnTriggerStay2D(Collider2D other)
    //Hitbox que maneja la carga de boost por roce
    {
        if (other.CompareTag("Enemy"))
        {
            float chargeAmount = playerController.chargeRate * Time.deltaTime;
            playerController.AddGrazeCharge(chargeAmount);
        }
    }
}