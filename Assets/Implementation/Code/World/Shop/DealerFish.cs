using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DealerFish : MonoBehaviour
{
    private bool consumed;

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
        if (consumed || !other.CompareTag(GameplayTagCatalog.Player))
        {
            return;
        }

        consumed = true;
        InGameShopManager.TryOpenShopFromWorld();
    }
}
