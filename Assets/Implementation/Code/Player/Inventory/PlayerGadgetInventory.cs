using UnityEngine;

[DisallowMultipleComponent]
public class PlayerGadgetInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSessionController session;
    [SerializeField] private InkPulseController inkPulseController;

    [Header("Runtime Starting Inventory")]
    [SerializeField] private bool grantStartingInventory = false;
    [SerializeField] private bool startWithShellShield = false;
    [SerializeField] private bool startWithInkBottle = false;
    [SerializeField] private Sprite startingShellShieldIcon = null;
    [SerializeField] private Sprite startingInkBottleIcon = null;

    private GameplayCommandInputBinding slot1InputBinding;
    private GameplayCommandInputBinding slot2InputBinding;

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
        bool useSlot1Requested = slot1InputBinding?.TryConsumeRequest() ?? false;
        bool useSlot2Requested = slot2InputBinding?.TryConsumeRequest() ?? false;

        if (useSlot1Requested)
        {
            TryUseSlot1();
        }

        if (useSlot2Requested)
        {
            TryUseSlot2();
        }
    }

    private void OnEnable()
    {
        SquidInkPulseInputRuntime.GameplayChanged += HandleGameplayInputChanged;
        HandleGameplayInputChanged(SquidInkPulseInputRuntime.Gameplay);
    }

    private void OnDisable()
    {
        SquidInkPulseInputRuntime.GameplayChanged -= HandleGameplayInputChanged;
        HandleGameplayInputChanged(null);
    }

    public bool TryConsumeShellShield()
    {
        return RuntimeGadgetInventory.TryConsumeShellShield();
    }

    public bool Acquire(GadgetId gadget, Sprite icon, Color iconTint)
    {
        return RuntimeGadgetInventory.Acquire(gadget, icon, iconTint);
    }

    public bool TryUseSlot1()
    {
        return TryUseActiveSlot(0);
    }

    public bool TryUseSlot2()
    {
        return TryUseActiveSlot(1);
    }

    private bool TryUseActiveSlot(int slotIndex)
    {
        if (session == null || !session.IsPlaying || InGameShopManager.BlocksInkPulseActivation)
        {
            return false;
        }

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

        return RuntimeGadgetInventory.TryConsume(gadget);
    }

    private void HandleGameplayInputChanged(SquidInkPulseGameplayInputReader inputReader)
    {
        slot1InputBinding?.Dispose();
        slot2InputBinding?.Dispose();
        slot1InputBinding = null;
        slot2InputBinding = null;

        if (inputReader == null)
        {
            return;
        }

        slot1InputBinding = new GameplayCommandInputBinding(
            inputReader,
            SquidInkPulseGameplayCommand.UseGadgetSlot1);
        slot2InputBinding = new GameplayCommandInputBinding(
            inputReader,
            SquidInkPulseGameplayCommand.UseGadgetSlot2);
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
