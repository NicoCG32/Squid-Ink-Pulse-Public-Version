using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerGadgetInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulseController;

    [Header("Runtime Starting Inventory")]
    [SerializeField] private bool grantStartingInventory;
    [SerializeField, Min(0)] private int startingShellShieldCount;
    [SerializeField, Min(0)] private int startingInkBottleCount;
    [SerializeField] private Sprite startingShellShieldIcon;
    [SerializeField] private Sprite startingInkBottleIcon;

    private void Awake()
    {
        ResolveReferences();

        bool wasInventoryInitialized = RuntimeGadgetInventory.IsInitialized;
        RuntimeGadgetInventory.InitializeIfNeeded();
        if (!wasInventoryInitialized)
        {
            GrantStartingInventoryIfNeeded();
        }

        WarnIfMissingReferences();
    }

    private void Update()
    {
        if (session == null || !session.IsPlaying || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            TryUseActiveSlot(0);
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryUseActiveSlot(1);
        }
    }

    public bool TryConsumeShellShield()
    {
        return RuntimeGadgetInventory.TryConsumeShellShield();
    }

    public bool Acquire(GadgetId gadget, int amount, Sprite icon, Color iconTint)
    {
        return RuntimeGadgetInventory.Acquire(gadget, amount, icon, iconTint);
    }

    private bool TryUseActiveSlot(int slotIndex)
    {
        GadgetId gadget = RuntimeGadgetInventory.GetSlot(slotIndex);
        if (!GadgetCatalog.IsActive(gadget) || RuntimeGadgetInventory.GetCount(gadget) <= 0)
        {
            return false;
        }

        bool wasUsed = gadget switch
        {
            GadgetId.InkBottle => TryUseInkBottle(),
            _ => false
        };

        if (!wasUsed)
        {
            return false;
        }

        RuntimeGadgetInventory.TryConsume(gadget);
        return true;
    }

    private void GrantStartingInventoryIfNeeded()
    {
        if (!grantStartingInventory)
        {
            return;
        }

        RuntimeGadgetInventory.Acquire(GadgetId.ShellShield, startingShellShieldCount, startingShellShieldIcon, Color.white);
        RuntimeGadgetInventory.Acquire(GadgetId.InkBottle, startingInkBottleCount, startingInkBottleIcon, Color.white);
    }

    private bool TryUseInkBottle()
    {
        return inkPulseController != null && inkPulseController.TryForceReady();
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

        if (inkPulseController == null)
        {
            inkPulseController = GetComponent<InkPulseController>();
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || inkPulseController == null)
        {
            Debug.LogWarning("[PlayerGadgetInventory] Faltan referencias. Asigna Session e InkPulseController en el Inspector.", this);
        }
    }
}
