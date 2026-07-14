using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OutOfGameShopManager : MonoBehaviour
{
    private const int VisibleUpgradeSlotCount = 4;
    private const int VisibleSkinSlotCount = 4;
    private const int UpgradeLevelSegmentsPerDrop = 2;

    [Header("Fixed Upgrade Slots")]
    [SerializeField] private string[] upgradeIds =
    {
        PlayerUnlockableIds.InkPulseDurationUpgrade,
        PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
        PlayerUnlockableIds.ShrimpMultiplierUpgrade,
        PlayerUnlockableIds.ScoreMultiplierUpgrade
    };
    [SerializeField] private Button[] upgradeSlotButtons = new Button[VisibleUpgradeSlotCount];
    [SerializeField] private PermanentShopSlotVisual[] upgradeSlotVisuals = new PermanentShopSlotVisual[VisibleUpgradeSlotCount];

    [Header("Paged Skin Slots")]
    [SerializeField] private Button[] skinSlotButtons = new Button[VisibleSkinSlotCount];
    [SerializeField] private PermanentShopSlotVisual[] skinSlotVisuals = new PermanentShopSlotVisual[VisibleSkinSlotCount];
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

    [Header("Dealer Visual State")]
    [SerializeField] private GameObject defaultShopVisualState;
    [SerializeField] private GameObject happyShopVisualState;

    [Header("Upgrade Level Indicator")]
    [SerializeField] private GameObject upgradeLevelIndicatorRoot;
    [SerializeField] private LevelDropVisual[] upgradeLevelDrops = new LevelDropVisual[5];

    [Header("Events")]
    [SerializeField] private UnityEvent onPurchaseSucceeded = new UnityEvent();
    [SerializeField] private UnityEvent onSelectionChanged = new UnityEvent();

    private ShopSelectionKind selectionKind = ShopSelectionKind.None;
    private string selectedUpgradeId;
    private int selectedSkinIndex = -1;
    private readonly PermanentShopSkinPager skinPager = new(VisibleSkinSlotCount);
    private PermanentShopSlotPresenter slotPresenter;
    private string purchaseResultMessage = string.Empty;
    private bool hasPurchasedInCurrentMenu;

    private enum ShopSelectionKind
    {
        None,
        Upgrade,
        Skin
    }

    [Serializable]
    private struct LevelDropVisual
    {
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject halfState;
        [SerializeField] private GameObject fullState;

        public bool IsConfigured => emptyState != null || halfState != null || fullState != null;

        public void Apply(PermanentShopLevelDropState state)
        {
            SetActive(emptyState, state == PermanentShopLevelDropState.Empty);
            SetActive(halfState, state == PermanentShopLevelDropState.Half);
            SetActive(fullState, state == PermanentShopLevelDropState.Full);
        }
    }

    private void Awake()
    {
        EnsureSlotArrays();
        EnsureSlotPresenter();
        NormalizeRenderableScale();
        ConfigureRaycastTargets();
        hasPurchasedInCurrentMenu = false;
        ApplyShopVisualState(happy: false);
        Refresh();
        WarnIfMissingReferences();
    }

    private void OnEnable()
    {
        PersistentPlayerProfile.ProfileChanged += HandleProfileChanged;
        PersistentPlayerProfile.RecordsChanged += HandleRecordsChanged;
        NormalizeRenderableScale();
        ConfigureRaycastTargets();
        hasPurchasedInCurrentMenu = false;
        ApplyShopVisualState(happy: false);
        Refresh();
    }

    private void OnDisable()
    {
        ApplyShopVisualState(happy: false);
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
        purchaseResultMessage = string.Empty;
        Refresh();
        SelectButtonInEventSystem(GetUpgradeSlotButton(slotIndex));
        onSelectionChanged.Invoke();
    }

    public void SelectSkinSlot(int slotIndex)
    {
        RefreshVisibleShopSkins();
        if (!skinPager.TryGetSkinIndexForSlot(slotIndex, out int skinIndex))
        {
            ClearSelection();
            return;
        }

        selectionKind = ShopSelectionKind.Skin;
        selectedUpgradeId = null;
        selectedSkinIndex = skinIndex;
        purchaseResultMessage = string.Empty;
        Refresh();
        SelectButtonInEventSystem(GetSkinSlotButton(slotIndex));
        onSelectionChanged.Invoke();
    }

    public void PreviousSkinPage()
    {
        RefreshVisibleShopSkins();
        skinPager.PreviousPage();
        ClearSelection();
        Refresh();
    }

    public void NextSkinPage()
    {
        RefreshVisibleShopSkins();
        skinPager.NextPage();
        ClearSelection();
        Refresh();
    }

    public void PurchaseSelected()
    {
        bool isRealPurchase = IsSelectedActionRealPurchase();
        PermanentShopPurchaseResult result = selectionKind switch
        {
            ShopSelectionKind.Upgrade => PurchaseSelectedUpgrade(),
            ShopSelectionKind.Skin => PurchaseSelectedSkin(),
            _ => PermanentShopPurchaseResult.UnknownItem
        };

        purchaseResultMessage = GetPurchaseResultMessage(result);
        if (result == PermanentShopPurchaseResult.Success)
        {
            purchaseResultMessage = string.Empty;
            if (isRealPurchase)
            {
                hasPurchasedInCurrentMenu = true;
                ApplyShopVisualState(happy: true);
            }

            onPurchaseSucceeded.Invoke();
        }

        Refresh();
    }

    public void Refresh()
    {
        RefreshVisibleShopSkins();
        NormalizeSkinPage();
        EnsureSlotPresenter();
        RefreshSlotInteractivity();
        RefreshSlotVisuals();
        RefreshNavigation();
        RefreshSelectionPresentation();
        ApplyShopVisualState(hasPurchasedInCurrentMenu);
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

        if (!PersistentPlayerProfile.HasUnlockedSkin(skin.id))
        {
            return PermanentShopService.TryPurchaseSkin(skin.id);
        }

        bool isEquipped = string.Equals(PersistentPlayerProfile.EquippedSkinId, skin.id, StringComparison.Ordinal);
        return isEquipped
            ? PermanentShopService.TryEquipSkin(PlayerSkinIds.Default)
            : PermanentShopService.TryEquipSkin(skin.id);
    }

    private bool IsSelectedActionRealPurchase()
    {
        if (selectionKind == ShopSelectionKind.Upgrade)
        {
            PermanentUpgradeDefinition upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(selectedUpgradeId);
            if (upgrade == null)
            {
                return false;
            }

            int currentLevel = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgrade.id);
            return currentLevel < upgrade.maxLevel;
        }

        if (selectionKind == ShopSelectionKind.Skin)
        {
            UnlockableSkinDefinition skin = GetSelectedSkin();
            return skin != null && !PersistentPlayerProfile.HasUnlockedSkin(skin.id);
        }

        return false;
    }

    private void RefreshSlotInteractivity()
    {
        for (int index = 0; index < upgradeSlotButtons.Length; index++)
        {
            Button button = GetUpgradeSlotButton(index);
            if (button != null)
            {
                button.interactable = TryGetUpgradeId(index, out string upgradeId)
                    && UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId) != null;
            }
        }

        for (int index = 0; index < skinSlotButtons.Length; index++)
        {
            Button button = GetSkinSlotButton(index);
            if (button == null)
            {
                continue;
            }

            button.interactable = skinPager.TryGetSkinIndexForSlot(index, out _);
        }
    }

    private void RefreshSlotVisuals()
    {
        for (int index = 0; index < upgradeSlotButtons.Length; index++)
        {
            PermanentUpgradeDefinition upgrade = null;
            if (TryGetUpgradeId(index, out string upgradeId))
            {
                upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId);
            }

            slotPresenter.PresentUpgradeSlot(
                GetUpgradeSlotVisual(index),
                upgrade,
                selectionKind == ShopSelectionKind.Upgrade
                    && upgrade != null
                    && string.Equals(selectedUpgradeId, upgrade.id, StringComparison.Ordinal));
        }

        for (int index = 0; index < skinSlotButtons.Length; index++)
        {
            UnlockableSkinDefinition skin = skinPager.GetSkinForSlot(index);
            bool isOwned = skin != null && PersistentPlayerProfile.HasUnlockedSkin(skin.id);
            bool isEquipped = skin != null
                && string.Equals(PersistentPlayerProfile.EquippedSkinId, skin.id, StringComparison.Ordinal);

            slotPresenter.PresentSkinSlot(GetSkinSlotVisual(index), skin, isOwned, isEquipped);
        }
    }

    private void RefreshNavigation()
    {
        int maxSkinPage = GetMaxSkinPage();
        if (previousSkinPageButton != null)
        {
            previousSkinPageButton.interactable = skinPager.Page > 0;
        }

        if (nextSkinPageButton != null)
        {
            nextSkinPageButton.interactable = skinPager.Page < maxSkinPage;
        }

        SetText(skinPageText, maxSkinPage > 0 ? $"{skinPager.Page + 1}/{maxSkinPage + 1}" : string.Empty);
    }

    private void RefreshSelectionPresentation()
    {
        PermanentShopSelectionPresentation presentation = PermanentShopSelectionPresentation.Empty;

        if (selectionKind == ShopSelectionKind.Upgrade)
        {
            PermanentUpgradeDefinition upgrade = UnlockablesCatalogQuery.FindPermanentUpgrade(selectedUpgradeId);
            if (upgrade != null)
            {
                int level = PersistentPlayerProfile.GetPermanentUpgradeLevel(upgrade.id);
                bool isGoalMet = upgrade.defaultUnlocked || UnlockablesCatalogQuery.IsGoalMet(upgrade.unlockGoal);
                int nextPrice = PermanentShopService.GetPermanentUpgradePrice(upgrade.id);

                presentation = PermanentShopSelectionPresenter.ForUpgrade(
                    upgrade,
                    level,
                    isGoalMet,
                    nextPrice,
                    FormatShopPrice);
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

                presentation = PermanentShopSelectionPresenter.ForSkin(
                    skin,
                    isOwned,
                    isEquipped,
                    isGoalMet,
                    FormatShopPrice);
            }
        }

        string state = presentation.State;
        if (!string.IsNullOrWhiteSpace(purchaseResultMessage))
        {
            state = purchaseResultMessage;
        }

        SetText(selectedItemNameText, presentation.DisplayName);
        SetText(selectedItemDescriptionText, presentation.Description);
        SetText(selectedItemPriceText, presentation.Price);
        SetText(selectedItemStateText, state);
        RefreshUpgradeLevelPresentation(
            presentation.ShowUpgradeLevel,
            presentation.UpgradeLevel,
            presentation.UpgradeMaxLevel);

        if (purchaseButton != null)
        {
            purchaseButton.interactable = presentation.CanPurchase;
        }
    }

    private void ClearSelection()
    {
        selectionKind = ShopSelectionKind.None;
        selectedUpgradeId = null;
        selectedSkinIndex = -1;
        purchaseResultMessage = string.Empty;
    }

    private static string GetPurchaseResultMessage(PermanentShopPurchaseResult result)
    {
        return result switch
        {
            PermanentShopPurchaseResult.InsufficientShrimps => "SIN CAMARONES",
            PermanentShopPurchaseResult.LockedByGoal => "BLOQUEADO",
            PermanentShopPurchaseResult.AlreadyOwned => "YA ADQUIRIDO",
            PermanentShopPurchaseResult.MaxLevelReached => "MAX",
            PermanentShopPurchaseResult.InvalidPrice => "PRECIO INVALIDO",
            PermanentShopPurchaseResult.UnknownItem => "NO DISPONIBLE",
            _ => string.Empty
        };
    }

    private UnlockableSkinDefinition GetSelectedSkin()
    {
        return skinPager.GetSkinAtIndex(selectedSkinIndex);
    }

    private int GetMaxSkinPage()
    {
        return skinPager.MaxPage;
    }

    private void NormalizeSkinPage()
    {
        skinPager.NormalizePage();
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

    private void RefreshVisibleShopSkins()
    {
        skinPager.SetCatalogSkins(PersistentPlayerProfile.UnlockablesCatalog.skins);
    }

    private void EnsureSlotPresenter()
    {
        slotPresenter ??= new PermanentShopSlotPresenter(this);
    }

    private static string FormatShopPrice(int amount)
    {
        return ShrimpCounterDisplay.FormatShrimpAmount(amount);
    }

    private void RefreshUpgradeLevelPresentation(bool isVisible, int level, int maxLevel)
    {
        SetActive(upgradeLevelIndicatorRoot, isVisible);

        LevelDropVisual[] drops = upgradeLevelDrops ?? Array.Empty<LevelDropVisual>();
        if (!isVisible || drops.Length == 0)
        {
            return;
        }

        PermanentShopLevelDropState[] dropStates = PermanentShopLevelMeter.CalculateDropStates(
            level,
            maxLevel,
            drops.Length,
            UpgradeLevelSegmentsPerDrop);

        for (int index = 0; index < drops.Length; index++)
        {
            if (drops[index].IsConfigured)
            {
                drops[index].Apply(dropStates[index]);
            }
        }
    }

    private void ApplyShopVisualState(bool happy)
    {
        SetActive(defaultShopVisualState, !happy || happyShopVisualState == null);
        SetActive(happyShopVisualState, happy);
    }

    private void EnsureSlotArrays()
    {
        upgradeIds ??= Array.Empty<string>();
        upgradeSlotButtons ??= Array.Empty<Button>();
        skinSlotButtons ??= Array.Empty<Button>();
        upgradeSlotVisuals = EnsureArrayLength(upgradeSlotVisuals, VisibleUpgradeSlotCount);
        skinSlotVisuals = EnsureArrayLength(skinSlotVisuals, VisibleSkinSlotCount);
        upgradeLevelDrops = EnsureArrayLength(upgradeLevelDrops, 5);

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

    private static T[] EnsureArrayLength<T>(T[] source, int length)
    {
        if (source == null)
        {
            return new T[length];
        }

        if (source.Length == length)
        {
            return source;
        }

        Array.Resize(ref source, length);
        return source;
    }

    private PermanentShopSlotVisual GetUpgradeSlotVisual(int slotIndex)
    {
        return GetArrayItem(upgradeSlotVisuals, slotIndex);
    }

    private PermanentShopSlotVisual GetSkinSlotVisual(int slotIndex)
    {
        return GetArrayItem(skinSlotVisuals, slotIndex);
    }

    private Button GetUpgradeSlotButton(int slotIndex)
    {
        return GetUpgradeSlotVisual(slotIndex)?.Button ?? GetArrayItem(upgradeSlotButtons, slotIndex);
    }

    private Button GetSkinSlotButton(int slotIndex)
    {
        return GetSkinSlotVisual(slotIndex)?.Button ?? GetArrayItem(skinSlotButtons, slotIndex);
    }

    private static T GetArrayItem<T>(T[] source, int index)
        where T : class
    {
        return source != null && index >= 0 && index < source.Length ? source[index] : null;
    }

    private void ConfigureRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(includeInactive: true);
        Button[] buttons = GetComponentsInChildren<Button>(includeInactive: true);
        HashSet<Graphic> buttonTargetGraphics = new();

        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button == null)
            {
                continue;
            }

            Graphic targetGraphic = button.targetGraphic != null
                ? button.targetGraphic
                : button.GetComponent<Graphic>();

            if (targetGraphic == null)
            {
                continue;
            }

            button.targetGraphic = targetGraphic;
            targetGraphic.raycastTarget = true;
            buttonTargetGraphics.Add(targetGraphic);
        }

        for (int index = 0; index < graphics.Length; index++)
        {
            Graphic graphic = graphics[index];
            if (graphic == null || buttonTargetGraphics.Contains(graphic))
            {
                continue;
            }

            if (graphic.GetComponent<Selectable>() != null)
            {
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private void NormalizeRenderableScale()
    {
        RestoreScaleIfCollapsed(transform);
        Canvas ownerCanvas = GetComponentInParent<Canvas>(includeInactive: true);
        RestoreScaleIfCollapsed(ownerCanvas != null ? ownerCanvas.transform : null);
    }

    private static void RestoreScaleIfCollapsed(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 localScale = target.localScale;
        if (Mathf.Approximately(localScale.x, 0f)
            || Mathf.Approximately(localScale.y, 0f)
            || Mathf.Approximately(localScale.z, 0f))
        {
            target.localScale = Vector3.one;
        }
    }

    private static void SelectButtonInEventSystem(Button button)
    {
        if (button == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(button.gameObject);
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

        if (!HasConfiguredSlotVisuals(upgradeSlotVisuals, VisibleUpgradeSlotCount)
            || !HasConfiguredSlotVisuals(skinSlotVisuals, VisibleSkinSlotCount))
        {
            Debug.LogWarning("[OutOfGameShopManager] Faltan referencias visuales serializadas para slots de tienda permanente.", this);
        }

        if (defaultShopVisualState == null || happyShopVisualState == null)
        {
            Debug.LogWarning("[OutOfGameShopManager] Faltan referencias serializadas para estados visuales del dealer.", this);
        }

        if (upgradeLevelIndicatorRoot == null || !HasConfiguredLevelDrops())
        {
            Debug.LogWarning("[OutOfGameShopManager] Faltan referencias serializadas para el indicador de nivel de mejoras.", this);
        }

        if (selectedItemNameText == null || selectedItemDescriptionText == null || selectedItemPriceText == null)
        {
            Debug.LogWarning("[OutOfGameShopManager] Faltan referencias de ProductInfoBlock. Asigna NombreProducto, DescripcionProducto y PrecioProducto desde el Inspector.", this);
        }
    }

    private static bool HasConfiguredSlotVisuals(PermanentShopSlotVisual[] visuals, int expectedCount)
    {
        if (visuals == null || visuals.Length != expectedCount)
        {
            return false;
        }

        for (int index = 0; index < visuals.Length; index++)
        {
            if (visuals[index] == null || !visuals[index].IsConfigured)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasConfiguredLevelDrops()
    {
        if (upgradeLevelDrops == null || upgradeLevelDrops.Length == 0)
        {
            return false;
        }

        for (int index = 0; index < upgradeLevelDrops.Length; index++)
        {
            if (upgradeLevelDrops[index].IsConfigured)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
