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
    private const string EmptyDropStateName = "Vacia";
    private const string HalfDropStateName = "Media";
    private const string FullDropStateName = "Llena";
    private static readonly string[] DefaultShopVisualNames = { "Default", "DealerDefault", "OctoDealerDefault", "ShopDefault" };
    private static readonly string[] HappyShopVisualNames = { "AfterBuy", "Happy", "DealerHappy", "OctoDealerHappy", "Feliz" };

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

    [Header("Dealer Visual State")]
    [SerializeField] private GameObject defaultShopVisualState;
    [SerializeField] private GameObject happyShopVisualState;

    [Header("Events")]
    [SerializeField] private UnityEvent onPurchaseSucceeded = new UnityEvent();
    [SerializeField] private UnityEvent onSelectionChanged = new UnityEvent();

    private ShopSelectionKind selectionKind = ShopSelectionKind.None;
    private string selectedUpgradeId;
    private int selectedSkinIndex = -1;
    private int skinPage;
    private readonly Dictionary<string, Sprite> shopSpriteCache = new();
    private readonly HashSet<string> missingShopSpritePaths = new();
    private UnlockableSkinDefinition[] visibleShopSkins = Array.Empty<UnlockableSkinDefinition>();
    private LevelDropVisual[] levelDropVisuals = Array.Empty<LevelDropVisual>();
    private GameObject levelIndicatorRoot;
    private bool levelDropVisualsResolved;
    private string purchaseResultMessage = string.Empty;
    private bool hasPurchasedInCurrentMenu;

    private enum ShopSelectionKind
    {
        None,
        Upgrade,
        Skin
    }

    private enum LevelDropState
    {
        Empty,
        Half,
        Full
    }

    private readonly struct LevelDropVisual
    {
        private readonly GameObject emptyState;
        private readonly GameObject halfState;
        private readonly GameObject fullState;

        public LevelDropVisual(Transform dropRoot)
        {
            Transform visualRoot = dropRoot != null ? dropRoot.Find(UiButtonContract.VisualChildName) : null;
            emptyState = ResolveDropState(visualRoot, EmptyDropStateName);
            halfState = ResolveDropState(visualRoot, HalfDropStateName);
            fullState = ResolveDropState(visualRoot, FullDropStateName);
        }

        public bool IsConfigured => emptyState != null || halfState != null || fullState != null;

        public void Apply(LevelDropState state)
        {
            SetActive(emptyState, state == LevelDropState.Empty);
            SetActive(halfState, state == LevelDropState.Half);
            SetActive(fullState, state == LevelDropState.Full);
        }

        private static GameObject ResolveDropState(Transform visualRoot, string stateName)
        {
            Transform state = visualRoot != null ? visualRoot.Find(stateName) : null;
            return state != null ? state.gameObject : null;
        }
    }

    private void Awake()
    {
        EnsureSlotArrays();
        NormalizeRenderableScale();
        ConfigureRaycastTargets();
        ResolveShopVisualStates();
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
        ResolveShopVisualStates();
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
        SelectButtonInEventSystem(upgradeSlotButtons[slotIndex]);
        onSelectionChanged.Invoke();
    }

    public void SelectSkinSlot(int slotIndex)
    {
        RefreshVisibleShopSkins();
        int skinIndex = skinPage * VisibleSkinSlotCount + slotIndex;
        if (slotIndex < 0 || slotIndex >= VisibleSkinSlotCount || skinIndex < 0 || skinIndex >= visibleShopSkins.Length)
        {
            ClearSelection();
            return;
        }

        selectionKind = ShopSelectionKind.Skin;
        selectedUpgradeId = null;
        selectedSkinIndex = skinIndex;
        purchaseResultMessage = string.Empty;
        Refresh();
        SelectButtonInEventSystem(skinSlotButtons[slotIndex]);
        onSelectionChanged.Invoke();
    }

    public void PreviousSkinPage()
    {
        RefreshVisibleShopSkins();
        skinPage = Mathf.Max(0, skinPage - 1);
        ClearSelection();
        Refresh();
    }

    public void NextSkinPage()
    {
        RefreshVisibleShopSkins();
        skinPage = Mathf.Min(GetMaxSkinPage(), skinPage + 1);
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
            Button button = upgradeSlotButtons[index];
            if (button != null)
            {
                button.interactable = TryGetUpgradeId(index, out string upgradeId)
                    && UnlockablesCatalogQuery.FindPermanentUpgrade(upgradeId) != null;
            }
        }

        for (int index = 0; index < skinSlotButtons.Length; index++)
        {
            Button button = skinSlotButtons[index];
            if (button == null)
            {
                continue;
            }

            int skinIndex = skinPage * VisibleSkinSlotCount + index;
            button.interactable = skinIndex >= 0 && skinIndex < visibleShopSkins.Length;
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

            ConfigureSelectedVisualState(upgradeSlotButtons[index], usePressedStateWhenSelected: true);
            ApplyShopSprites(
                upgradeSlotButtons[index],
                upgrade?.shopSpriteResourcePath,
                upgrade?.shopHighlightedSpriteResourcePath,
                selectionKind == ShopSelectionKind.Upgrade
                    && upgrade != null
                    && string.Equals(selectedUpgradeId, upgrade.id, StringComparison.Ordinal));
        }

        for (int index = 0; index < skinSlotButtons.Length; index++)
        {
            int skinIndex = skinPage * VisibleSkinSlotCount + index;
            UnlockableSkinDefinition skin = skinIndex >= 0 && skinIndex < visibleShopSkins.Length
                ? visibleShopSkins[skinIndex]
                : null;
            bool isOwned = skin != null && PersistentPlayerProfile.HasUnlockedSkin(skin.id);
            bool isEquipped = skin != null
                && string.Equals(PersistentPlayerProfile.EquippedSkinId, skin.id, StringComparison.Ordinal);

            ApplySkinShopSprites(skinSlotButtons[index], skin, isOwned, isEquipped);
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
        bool showUpgradeLevel = false;
        int upgradeLevel = 0;
        int upgradeMaxLevel = 10;

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
                description = upgrade.description;
                price = nextPrice > 0 ? FormatShopPrice(nextPrice) : "MAX";
                state = !isGoalMet ? "BLOQUEADO" : isMaxed ? "MAX" : string.Empty;
                canPurchase = isGoalMet && !isMaxed;
                showUpgradeLevel = true;
                upgradeLevel = level;
                upgradeMaxLevel = upgrade.maxLevel;
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
                bool canUnequipToDefault = isOwned
                    && isEquipped
                    && !string.Equals(skin.id, PlayerSkinIds.Default, StringComparison.Ordinal);

                name = skin.displayName;
                description = skin.description;
                price = isOwned ? (isEquipped ? (canUnequipToDefault ? "QUITAR" : "EQUIPADA") : "USAR") : FormatShopPrice(skin.basePrice);
                state = !isGoalMet ? "BLOQUEADO" : isEquipped ? "EQUIPADA" : string.Empty;
                canPurchase = isGoalMet && (!isOwned || !isEquipped || canUnequipToDefault);
            }
        }

        if (!string.IsNullOrWhiteSpace(purchaseResultMessage))
        {
            state = purchaseResultMessage;
        }

        SetText(selectedItemNameText, name);
        SetText(selectedItemDescriptionText, description);
        SetText(selectedItemPriceText, price);
        SetText(selectedItemStateText, state);
        RefreshUpgradeLevelPresentation(showUpgradeLevel, upgradeLevel, upgradeMaxLevel);

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
        return selectedSkinIndex >= 0 && selectedSkinIndex < visibleShopSkins.Length
            ? visibleShopSkins[selectedSkinIndex]
            : null;
    }

    private int GetMaxSkinPage()
    {
        int skinCount = visibleShopSkins.Length;
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

    private void RefreshVisibleShopSkins()
    {
        UnlockableSkinDefinition[] skins = PersistentPlayerProfile.UnlockablesCatalog.skins;
        if (skins == null || skins.Length == 0)
        {
            visibleShopSkins = Array.Empty<UnlockableSkinDefinition>();
            return;
        }

        List<UnlockableSkinDefinition> visibleSkins = new(skins.Length);
        for (int index = 0; index < skins.Length; index++)
        {
            UnlockableSkinDefinition skin = skins[index];
            if (skin != null && !string.IsNullOrWhiteSpace(skin.shopSpriteResourcePath))
            {
                visibleSkins.Add(skin);
            }
        }

        visibleShopSkins = visibleSkins.ToArray();
    }

    private void ApplyShopSprites(Button button, string normalSpritePath, string pressedSpritePath, bool usePressedSpriteWhenSelected)
    {
        Transform visualRoot = button != null && button.transform.parent != null
            ? button.transform.parent.Find(UiButtonContract.VisualChildName)
            : null;
        if (button == null)
        {
            return;
        }

        Sprite normalSprite = LoadShopSprite(normalSpritePath);
        Sprite pressedSprite = LoadShopSprite(pressedSpritePath) ?? normalSprite;

        bool appliedToVisualStates = false;
        if (visualRoot != null)
        {
            appliedToVisualStates |= ApplyStateSprite(visualRoot, UiButtonContract.NormalStateName, normalSprite);
            appliedToVisualStates |= ApplyStateSprite(visualRoot, UiButtonContract.HighlightedStateName, normalSprite);
            appliedToVisualStates |= ApplyStateSprite(visualRoot, UiButtonContract.PressedStateName, pressedSprite);
        }

        if (!appliedToVisualStates)
        {
            Sprite fallbackSprite = usePressedSpriteWhenSelected ? pressedSprite : normalSprite;
            ApplyFallbackButtonSprite(button, fallbackSprite);
        }
    }

    private void ApplySkinShopSprites(Button button, UnlockableSkinDefinition skin, bool isOwned, bool isEquipped)
    {
        ConfigureSelectedVisualState(button, usePressedStateWhenSelected: false);
        ApplyShopSprites(
            button,
            skin?.shopSpriteResourcePath,
            pressedSpritePath: null,
            usePressedSpriteWhenSelected: false);
        ApplySkinOwnershipVisuals(button, isOwned, isEquipped);
    }

    private void ApplySkinOwnershipVisuals(Button button, bool isOwned, bool isEquipped)
    {
        Transform visualRoot = button != null && button.transform.parent != null
            ? button.transform.parent.Find(UiButtonContract.VisualChildName)
            : null;
        if (visualRoot == null)
        {
            return;
        }

        SetSkinStatusVisualActive(
            visualRoot,
            UiButtonContract.PurchasedStateName,
            UiButtonContract.LegacyBuyedStateName,
            isOwned && !isEquipped);
        SetSkinStatusVisualActive(
            visualRoot,
            UiButtonContract.EquippedStateName,
            UiButtonContract.LegacySelectedStateName,
            isEquipped);
    }

    private static string FormatShopPrice(int amount)
    {
        return ShrimpCounterDisplay.FormatShrimpAmount(amount);
    }

    private static void ConfigureSelectedVisualState(Button button, bool usePressedStateWhenSelected)
    {
        if (button != null && button.TryGetComponent(out ButtonVisualState visualState))
        {
            visualState.SetUsePressedStateWhenSelected(usePressedStateWhenSelected);
        }
    }

    private Sprite LoadShopSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string normalizedPath = resourcePath.Trim().Replace('\\', '/');
        if (shopSpriteCache.TryGetValue(normalizedPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>(normalizedPath);
        if (sprite == null && missingShopSpritePaths.Add(normalizedPath))
        {
            Debug.LogWarning($"[OutOfGameShopManager] No se encontro el sprite de tienda Resources/{normalizedPath}.", this);
        }

        shopSpriteCache[normalizedPath] = sprite;
        return sprite;
    }

    private static bool ApplyStateSprite(Transform visualRoot, string stateName, Sprite sprite)
    {
        Transform state = visualRoot.Find(stateName);
        if (state == null)
        {
            return false;
        }

        Image image = state.GetComponent<Image>() ?? state.GetComponentInChildren<Image>(includeInactive: true);
        if (image == null)
        {
            return false;
        }

        image.enabled = sprite != null;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        image.raycastTarget = false;
        return true;
    }

    private static void SetVisualStateActive(Transform visualRoot, string stateName, bool active)
    {
        Transform state = visualRoot != null ? visualRoot.Find(stateName) : null;
        SetActive(state != null ? state.gameObject : null, active);
    }

    private static void SetSkinStatusVisualActive(
        Transform visualRoot,
        string primaryStateName,
        string legacyStateName,
        bool active)
    {
        SetVisualStateActive(visualRoot, primaryStateName, active);
        if (!string.Equals(primaryStateName, legacyStateName, StringComparison.Ordinal))
        {
            SetVisualStateActive(visualRoot, legacyStateName, active);
        }
    }

    private static void ApplyFallbackButtonSprite(Button button, Sprite sprite)
    {
        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        image.enabled = true;
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        Color color = image.color;
        color.a = sprite != null ? 1f : 0f;
        image.color = color;
    }

    private void RefreshUpgradeLevelPresentation(bool isVisible, int level, int maxLevel)
    {
        EnsureLevelDropVisuals();
        SetActive(levelIndicatorRoot, isVisible);
        if (!isVisible || levelDropVisuals.Length == 0)
        {
            return;
        }

        int totalSegments = levelDropVisuals.Length * UpgradeLevelSegmentsPerDrop;
        int filledSegments = maxLevel > 0
            ? Mathf.RoundToInt(Mathf.Clamp01(level / (float)maxLevel) * totalSegments)
            : 0;

        for (int index = 0; index < levelDropVisuals.Length; index++)
        {
            int dropSegments = Mathf.Clamp(filledSegments - index * UpgradeLevelSegmentsPerDrop, 0, UpgradeLevelSegmentsPerDrop);
            LevelDropState dropState = dropSegments switch
            {
                0 => LevelDropState.Empty,
                1 => LevelDropState.Half,
                _ => LevelDropState.Full
            };

            levelDropVisuals[index].Apply(dropState);
        }
    }

    private void EnsureLevelDropVisuals()
    {
        if (levelDropVisualsResolved)
        {
            return;
        }

        levelDropVisualsResolved = true;
        Transform mejorable = FindDescendant(transform, "Mejorable");
        levelIndicatorRoot = mejorable != null ? mejorable.gameObject : null;
        if (mejorable == null)
        {
            levelDropVisuals = Array.Empty<LevelDropVisual>();
            return;
        }

        List<LevelDropVisual> drops = new();
        for (int index = 1; index <= 5; index++)
        {
            Transform dropRoot = mejorable.Find($"Gota{index}");
            LevelDropVisual drop = new(dropRoot);
            if (drop.IsConfigured)
            {
                drops.Add(drop);
            }
        }

        levelDropVisuals = drops.ToArray();
    }

    private static Transform FindDescendant(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int index = 0; index < children.Length; index++)
        {
            if (children[index].name == childName)
            {
                return children[index];
            }
        }

        return null;
    }

    private void ResolveShopVisualStates()
    {
        if (defaultShopVisualState != null && happyShopVisualState != null)
        {
            return;
        }

        Transform searchRoot = GetComponentInParent<Canvas>(includeInactive: true)?.transform ?? transform.root ?? transform;
        if (happyShopVisualState == null)
        {
            happyShopVisualState = FindStateWithSibling(searchRoot, HappyShopVisualNames, DefaultShopVisualNames);
        }

        if (defaultShopVisualState == null)
        {
            defaultShopVisualState = FindStateWithSibling(searchRoot, DefaultShopVisualNames, HappyShopVisualNames);
        }

        if (happyShopVisualState != null && defaultShopVisualState == null)
        {
            defaultShopVisualState = FindDirectChild(happyShopVisualState.transform.parent, DefaultShopVisualNames);
        }

        if (defaultShopVisualState != null && happyShopVisualState == null)
        {
            happyShopVisualState = FindDirectChild(defaultShopVisualState.transform.parent, HappyShopVisualNames);
        }
    }

    private void ApplyShopVisualState(bool happy)
    {
        ResolveShopVisualStates();
        SetActive(defaultShopVisualState, !happy || happyShopVisualState == null);
        SetActive(happyShopVisualState, happy);
    }

    private static GameObject FindStateWithSibling(Transform root, string[] targetNames, string[] siblingNames)
    {
        if (root == null || targetNames == null || siblingNames == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int index = 0; index < children.Length; index++)
        {
            Transform candidate = children[index];
            if (!NameMatches(candidate.name, targetNames) || candidate.parent == null)
            {
                continue;
            }

            if (FindDirectChild(candidate.parent, siblingNames) != null)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindDirectChild(Transform parent, string[] childNames)
    {
        if (parent == null || childNames == null)
        {
            return null;
        }

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (NameMatches(child.name, childNames))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static bool NameMatches(string candidate, string[] names)
    {
        if (string.IsNullOrWhiteSpace(candidate) || names == null)
        {
            return false;
        }

        for (int index = 0; index < names.Length; index++)
        {
            if (string.Equals(candidate, names[index], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
