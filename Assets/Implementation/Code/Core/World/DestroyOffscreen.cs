using UnityEngine;

public class DestroyOffscreen : MonoBehaviour
{
    private const string ShrimpTag = "Shrimp";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EnemyTagCatalog.IsEnemy(other) || other.CompareTag(ShrimpTag))
        {
            Destroy(other.gameObject);
        }
    }
}
