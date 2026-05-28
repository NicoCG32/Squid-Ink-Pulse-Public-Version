using UnityEngine;

public class DestroyOffscreen : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    //Funcion que destruye objetos que chocan con la pared GarbageCollector
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Shrimp"))
        {
            Destroy(other.gameObject);
        }
    }
}