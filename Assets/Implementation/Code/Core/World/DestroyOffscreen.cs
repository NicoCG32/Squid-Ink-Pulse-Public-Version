using UnityEngine;

public class DestroyOffscreen : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EnemyTagCatalog.IsEnemy(other) || other.CompareTag(GameplayTagCatalog.Shrimp))
        {
            Destroy(other.gameObject);
        }
    }
}
