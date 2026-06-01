using UnityEngine;

[DisallowMultipleComponent]
public class GadgetShopItem : MonoBehaviour
{
    [SerializeField] private GadgetId gadgetId = GadgetId.InkBottle;
    [SerializeField] private Sprite hudIcon;
    [SerializeField] private Color hudIconTint = Color.white;

    public GadgetId GadgetId => gadgetId;
    public Sprite HudIcon => hudIcon != null ? hudIcon : ResolveIconFromRenderer();
    public Color HudIconTint => hudIconTint;

    private void Awake()
    {
        hudIcon ??= ResolveIconFromRenderer();
    }

    private void OnValidate()
    {
        hudIcon ??= ResolveIconFromRenderer();
    }

    private Sprite ResolveIconFromRenderer()
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }
}
