using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class GadgetPickup : MonoBehaviour
{
    [SerializeField] private GadgetId gadgetId = GadgetId.InkBottle;
    [SerializeField] private Sprite hudIcon;
    [SerializeField] private Color hudIconTint = Color.white;
    [SerializeField] private bool destroyOnPickup = true;

    private void Awake()
    {
        hudIcon ??= ResolveIconFromRenderer();
    }

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }

        hudIcon = ResolveIconFromRenderer();
    }

    private void OnValidate()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }

        hudIcon ??= ResolveIconFromRenderer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerGadgetInventory inventory = other.GetComponentInParent<PlayerGadgetInventory>();
        if (inventory == null)
        {
            return;
        }

        if (!inventory.Acquire(gadgetId, 1, hudIcon, hudIconTint))
        {
            return;
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }

    private Sprite ResolveIconFromRenderer()
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }
}
