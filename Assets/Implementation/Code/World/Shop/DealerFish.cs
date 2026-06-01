using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DealerFish : MonoBehaviour
{
    [SerializeField] private bool destroyOnOpen = true;

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        gameObject.tag = GameplayTagCatalog.Collectible;
    }

    private void OnValidate()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(GameplayTagCatalog.Player))
        {
            return;
        }

        if (!InGameShopManager.TryOpenShopFromWorld())
        {
            return;
        }

        if (destroyOnOpen)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
