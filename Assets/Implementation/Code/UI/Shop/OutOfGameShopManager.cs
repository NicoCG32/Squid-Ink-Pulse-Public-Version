using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OutOfGameShopManager : MonoBehaviour
{
    private const int VisibleUpgradeSlotCount = 4;
    private const int VisibleSkinSlotCount = 4;

    [Header("Fixed Upgrade Slots")]
    [SerializeField] private string[] upgradeIds =
    {
        PlayerUnlockableIds.InkPulseDurationUpgrade,
        PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
        PlayerUnlockableIds.ShrimpMultiplierUpgrade,
        PlayerUnlockableIds.ScoreMultiplierUpgrade
    };
    [SerializeField] private Button[] upgradeSlotButtons = new Button[VisibleUpgradeSlotCount];

    [Header("Paged Skin Slots")]
    [SerializeField] private Button[] skinSlotButtons = new Button[VisibleSkinSlotCount];
    [SerializeField] private Button previousSkinPageButton;
    [SerializeField] private Button nextSkinPageButton;

    [Header("Selection Action")]
    [SerializeField] private Button purchaseButton;

    [Header("Optional Text References")]
    [SerializeField] private TMP_Text selectedItemNameText;
    [SerializeField] private TMP_Text selectedItemDescriptionText;
    [SerializeField] private TMP_Text selectedItemPriceText;
    [SerializeField] private TMP_Text selectedItemStateText;
    [SerializeField] private TMP_Text skinPageText;

    [Header("Events")]
    [SerializeField] private UnityEvent onPurchaseSucceeded = new UnityEvent();
    [SerializeField] private UnityEvent onSelectionChanged = new UnityEvent();

    private ShopSelectionKind selectionKind = ShopSelectionKind.None;
    private string selectedUpgradeId;
    private int selectedSkinIndex = -1;
    private int skinPage;

    private enum ShopSelectionKind
    {
        None,
        Upgrade,
        Skin
    }

    private void Awake()
    {
        EnsureSlotArrays();
        Refresh();
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        PersistentPlayerProfile.ProfileChanged += HandleProfileChanged;
        PersistentPlayerProfile.RecordsChanged += HandleRecordsChanged;
        Refresh();
    }

    private void OnDisable()
    {
        PersistentPlayerProfile.ProfileChanged -= HandleProfileChanged;
        PersistentPlayerProfile.RecordsChanged -= HandleRecordsChanged;
    }

    private void OnValidate()
    {
        EnsureSlotArrays();
    }

    public void SelectUpgradeSlot(int slotIndex)
    {
        if (!TryGetUpgradeId(slotIndex, out string upgradeId)
            || UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId) == null)
        {
            ClearSelection();
            return;
        }

        selectionKind = ShopSelectionKind.Upgrade;
        selectedUpgradeId = upgradeId;
        selectedSkinIndex = -1;
        Refresh();
        onSelectionChanged.Invoke();
    }

    public void SelectSkinSlot(int slotIndex)
    {
        int skinIndex = skinPage * VisibleSkinSlotCount + slotIndex;
        UnlockableSkinDefinition[] skins = PersistentPlayerProfile.UnlockablesCatalog.skins;
        if (slotIndex < 0 || slotIndex >= VisibleSkinSlotCount || skins == null || skinIndex < 0 || skinIndex >= skins.Length)
        {
            ClearSelection();
            return;
        }

        selectionKind = ShopSelectionKind.Skin;
        selectedUpgradeId = null;
        selectedSkinIndex = skinIndex;
        Refresh();
        onSelectionChanged.Invoke();
    }

    public void PreviousSkinPage()
    {
        skinPage = Mathf.Max(0, skinPage - 1);
        ClearSelection();
        Refresh();
    }

    public void NextSkinPage()
    {
        skinPage = Mathf.Min(GetMaxSkinPage(), skinPage + 1);
        ClearSelection();
        Refresh();
    }

    public void PurchaseSelected()
    {
        PermanentShopPurchaseResult result = selectionKind switch
        {
            ShopSelectionKind.Upgrade => PurchaseSelectedUpgrade(),
            ShopSelectionKind.Skin => PurchaseSelectedSkin(),
            _ => PermanentShopPurchaseResult.UnknownItem
        };

        if (result == PermanentShopPurchaseResult.Success)
        {
            onPurchaseSucceeded.Invoke();
        }

        Refresh();
    }

    public void Refresh()
    {
        NormalizeSkinPage();
        RefreshSlotInteractivity();
        RefreshNavigation();
        RefreshSelectionPresentation();
    }

    private PermanentShopPurchaseResult PurchaseSelectedUpgrade()
    {
        return string.IsNullOrWhiteSpace(selectedUpgradeId)
            ? PermanentShopPurchaseResult.UnknownItem
            : PermanentShopService.TryPurchasePermanentUpgradeLevel(selectedUpgradeId);
    }

    private PermanentShopPurchaseResult PurchaseSelectedSkin()
    {
        UnlockableSkinDefinition skin = GetSelectedSkin();
        if (skin == null)
        {
            return PermanentShopPurchaseResult.UnknownItem;
        }

        return PersistentPlayerProfile.HasUnlockedSkin(skin.id)
            ? PermanentShopService.TryEquipSkin(skin.id)
            : PermanentShopService.TryPurchaseSkin(skin.id);
    }

    private void RefreshSlotInteractivity()
    {
        for (int index = 0; index < upgradeSlotButtons.Length; index++)
        {
            Button button = upgradeSlotButtons[index];
            if (button != null)
            {
                button.interactable = TryGetUpgradeId(index, out string upgradeId)
                    && UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId) != null;
            }
        }

        UnlockableSkinDefinition[] skins = PersistentPlayerProfile.UnlockablesCatalog.skins;
        for (int index = 0; index < skinSlotButtons.Length; index++)
        {
            Button button = skinSlotButtons[index];
            if (button == null)
            {
                continue;
            }

            int skinIndex = skinPage * VisibleSkinSlotCount + index;
            button.interactable = skins != null && skinIndex >= 0 && skinIndex < skins.Length;
        }
    }

    private void RefreshNavigation()
    {
        int maxSkinPage = GetMaxSkinPage();
        if (previousSkinPageButton != null)
        {
            previousSkinPageButton.interactable = skinPage > 0;
        }

        if (nextSkinPageButton != null)
        {
            nextSkinPageButton.interactable = skinPage < maxSkinPage;
        }

        SetText(skinPageText, maxSkinPage > 0 ? $"{skinPage + 1}/{maxSkinPage + 1}" : string.Empty);
    }

    private void RefreshSelectionPresentation()
    {
        string name = string.Empty;
        string description = string.Empty;
        string price = string.Empty;
        string state = string.Empty;
        bool canPurchase = false;

        if (selectionKind == ShopSelectionKind.Upgrade)
        {
            PermanentUpgradeDefinition upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(selectedUpgradeId);
            if (upgrade != null)
            {
                int level = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgrade.id);
                bool isGoalMet = upgrade.defaultUnlocked || UnlockablesCatalogQuery.IsGoalMet(upgrade.unlockGoal);
                bool isMaxed = level >= upgrade.maxLevel;
                int nextPrice = PermanentShopService.GetPermanentUpgradePrice(upgrade.id);

                name = upgrade.displayName;
                description = $"Nivel {level}/{upgrade.maxLevel}";
                price = nextPrice > 0 ? nextPrice.ToString() : "MAX";
                state = !isGoalMet ? "BLOQUEADO" : isMaxed ? "MAX" : string.Empty;
                canPurchase = isGoalMet && !isMaxed;
            }
        }
        else if (selectionKind == ShopSelectionKind.Skin)
        {
            UnlockableSkinDefinition skin = GetSelectedSkin();
            if (skin != null)
            {
                bool isOwned = PersistentPlayerProfile.HasUnlockedSkin(skin.id);
                bool isEquipped = string.Equals(PersistentPlayerProfile.EquippedSkinId, skin.id, StringComparison.Ordinal);
                bool isGoalMet = skin.defaultUnlocked || UnlockablesCatalogQuery.IsGoalMet(skin.unlockGoal);

                name = skin.displayName;
                price = isOwned ? (isEquipped ? "EQUIPADA" : "USAR") : skin.basePrice.ToString();
                state = !isGoalMet ? "BLOQUEADO" : isEquipped ? "EQUIPADA" : string.Empty;
                canPurchase = isGoalMet && !isEquipped;
            }
        }

        SetText(selectedItemNameText, name);
        SetText(selectedItemDescriptionText, description);
        SetText(selectedItemPriceText, price);
        SetText(selectedItemStateText, state);

        if (purchaseButton != null)
        {
            purchaseButton.interactable = canPurchase;
        }
    }

    private void ClearSelection()
    {
        selectionKind = ShopSelectionKind.None;
        selectedUpgradeId = null;
        selectedSkinIndex = -1;
    }

    private UnlockableSkinDefinition GetSelectedSkin()
    {
        UnlockableSkinDefinition[] skins = PersistentPlayerProfile.UnlockablesCatalog.skins;
        return skins != null && selectedSkinIndex >= 0 && selectedSkinIndex < skins.Length
            ? skins[selectedSkinIndex]
            : null;
    }

    private int GetMaxSkinPage()
    {
        int skinCount = PersistentPlayerProfile.UnlockablesCatalog.skins?.Length ?? 0;
        return skinCount <= VisibleSkinSlotCount
            ? 0
            : Mathf.Max(0, Mathf.CeilToInt(skinCount / (float)VisibleSkinSlotCount) - 1);
    }

    private void NormalizeSkinPage()
    {
        skinPage = Mathf.Clamp(skinPage, 0, GetMaxSkinPage());
        if (selectionKind == ShopSelectionKind.Skin && GetSelectedSkin() == null)
        {
            ClearSelection();
        }
    }

    private bool TryGetUpgradeId(int slotIndex, out string upgradeId)
    {
        upgradeId = null;
        if (upgradeIds == null || slotIndex < 0 || slotIndex >= upgradeIds.Length)
        {
            return false;
        }

        upgradeId = upgradeIds[slotIndex];
        return !string.IsNullOrWhiteSpace(upgradeId);
    }

    private void EnsureSlotArrays()
    {
        upgradeIds ??= Array.Empty<string>();
        upgradeSlotButtons ??= Array.Empty<Button>();
        skinSlotButtons ??= Array.Empty<Button>();

        if (upgradeIds.Length == 0)
        {
            upgradeIds = new[]
            {
                PlayerUnlockableIds.InkPulseDurationUpgrade,
                PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
                PlayerUnlockableIds.ShrimpMultiplierUpgrade,
                PlayerUnlockableIds.ScoreMultiplierUpgrade
            };
        }
    }

    private void HandleProfileChanged(PlayerProfileSaveData _)
    {
        Refresh();
    }

    private void HandleRecordsChanged(PlayerRecordsSaveData _)
    {
        Refresh();
    }

    private void WarnIfMissingReferences()
    {
        if (purchaseButton == null)
        {
            Debug.LogWarning("[OutOfGameShopManager] Falta PurchaseButton. Asigna ComprarBoton/Button desde el Inspector.", this);
        }

        if (upgradeSlotButtons.Length != VisibleUpgradeSlotCount || skinSlotButtons.Length != VisibleSkinSlotCount)
        {
            Debug.LogWarning("[OutOfGameShopManager] Deben asignarse 4 botones de upgrades y 4 botones de skins desde el Inspector.", this);
        }

        if (selectedItemNameText == null || selectedItemDescriptionText == null || selectedItemPriceText == null)
        {
            Debug.LogWarning("[OutOfGameShopManager] Faltan referencias de ProductInfoBlock. Asigna NombreProducto, DescripcionProducto y PrecioProducto desde el Inspector.", this);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
