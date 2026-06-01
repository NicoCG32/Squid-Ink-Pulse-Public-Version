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
    [SerializeField] private bool startWithShellShield;
    [SerializeField] private bool startWithInkBottle;
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

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryUseActiveSlot(0);
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            TryUseActiveSlot(1);
        }
    }

    public bool TryConsumeShellShield()
    {
        return RuntimeGadgetInventory.TryConsumeShellShield();
    }

    public bool Acquire(GadgetId gadget, Sprite icon, Color iconTint)
    {
        return RuntimeGadgetInventory.Acquire(gadget, icon, iconTint);
    }

    private bool TryUseActiveSlot(int slotIndex)
    {
        GadgetId gadget = RuntimeGadgetInventory.GetSlot(slotIndex);
        if (!GadgetCatalog.IsActive(gadget) || !RuntimeGadgetInventory.HasGadget(gadget))
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

        if (startWithShellShield)
        {
            RuntimeGadgetInventory.Acquire(GadgetId.ShellShield, startingShellShieldIcon, Color.white);
        }

        if (startWithInkBottle)
        {
            RuntimeGadgetInventory.Acquire(GadgetId.InkBottle, startingInkBottleIcon, Color.white);
        }
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
