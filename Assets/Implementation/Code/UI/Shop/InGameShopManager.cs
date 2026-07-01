using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InGameShopManager : MonoBehaviour
{
    private static InGameShopManager instance;

    [Header("References")]
    [SerializeField] private GameSessionController session;

    [Header("Scene UI References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image gadgetImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text buyKeyText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text insufficientFundsText;
    [SerializeField] private TMP_Text timerText;

    [Header("Shop Timing")]
    [SerializeField, Min(0.5f)] private float offerDurationSeconds = 5f;
    [SerializeField] private bool pauseGameplayWhileOpen = true;

    [Header("Pricing")]
    [SerializeField, Min(0.01f)] private float globalPriceMultiplier = 1f;
    [SerializeField, Min(1f)] private float scorePriceStep = 100000f;
    [SerializeField, Min(0f)] private float randomPriceMultiplierMin = 1f;
    [SerializeField, Min(0f)] private float randomPriceMultiplierMax = 2f;

    [Header("Offers")]
    [SerializeField] private ShopGadgetOffer[] offers = new ShopGadgetOffer[0];

    [Header("Attention Animation")]
    [SerializeField, Min(0f)] private float textPulseAmplitude = 0.12f;
    [SerializeField, Min(0.01f)] private float textPulseFrequency = 2.5f;

    [Header("Events")]
    public UnityEvent onShopOpened = new UnityEvent();
    public UnityEvent onShopClosed = new UnityEvent();
    public UnityEvent<GadgetId> onGadgetPurchased = new UnityEvent<GadgetId>();
    public UnityEvent<ShopEventState> onStateChanged = new UnityEvent<ShopEventState>();

    private static int inkPulseActivationBlockedUntilFrame = -1;

    private ShopGadgetOffer currentOffer;
    private float remainingSeconds;
    private int currentPrice;
    private float currentRandomPriceMultiplier = 1f;
    private bool isOpen;
    private bool isHoldingTimeScale;
    private float previousTimeScale = 1f;
    private Vector3 priceTextBaseScale = Vector3.one;
    private Vector3 buyKeyTextBaseScale = Vector3.one;
    private GadgetId queuedTutorialOffer = GadgetId.None;
    private int queuedTutorialPriceOverride = -1;
    private Coroutine pendingWorldShopRoutine;
    private bool currentShopOpenedFromDealerFish;
    private bool currentShopPurchased;

    public static InGameShopManager Instance => instance;
    public static bool HasInstance => instance != null;
    public static bool IsShopOpen => instance != null && instance.CurrentState == ShopEventState.Offering;
    public static bool BlocksInkPulseActivation => Time.frameCount <= inkPulseActivationBlockedUntilFrame
        || (instance != null
            && (instance.isOpen
                || instance.CurrentState == ShopEventState.Offering
                || instance.pendingWorldShopRoutine != null));
    public ShopEventState CurrentState { get; private set; } = ShopEventState.Closed;
    public event Action<ShopEventState, ShopEventState> StateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveReferences();
        ResolveUiReferences();
        WireButtons();
        CacheAnimatedTextScales();
        HideImmediate();
        WarnIfMissingReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (!isOpen)
        {
            return;
        }

        if (session != null && session.IsGameOver)
        {
            CloseShop();
            return;
        }

        bool externalPause = session != null && !session.IsPlaying && !isHoldingTimeScale;
        if (externalPause)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            BuyCurrentOffer();
        }

        AnimateAttentionTexts();

        float deltaTime = isHoldingTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        remainingSeconds = Mathf.Max(0f, remainingSeconds - deltaTime);
        RefreshTimer();

        if (remainingSeconds <= 0f)
        {
            CloseShop();
        }
    }

    public static bool TryOpenShopFromWorld()
    {
        return instance != null && instance.TryOpenShopFromWorldInternal();
    }

    private bool TryOpenShopFromWorldInternal()
    {
        if (pendingWorldShopRoutine != null)
        {
            return true;
        }

        if (!CanAttemptOpenTimedShop())
        {
            ClearQueuedTutorialOffer();
            return false;
        }

        if (RuntimeInGameShopLoreState.TryMarkFirstDealerShopAccess())
        {
            BlockInkPulseActivationBriefly();
            pendingWorldShopRoutine = StartCoroutine(OpenWorldShopAfterFirstComicRoutine());
            return true;
        }

        return TryOpenTimedShop(openedFromDealerFish: true);
    }

    private IEnumerator OpenWorldShopAfterFirstComicRoutine()
    {
        yield return LoreComicPresenter.PlayInGameShopFirstIfAvailable();
        BlockInkPulseActivationBriefly();
        pendingWorldShopRoutine = null;
        TryOpenTimedShop(openedFromDealerFish: true);
    }

    public void QueueTutorialOffer(GadgetId gadget, int priceOverride = -1)
    {
        queuedTutorialOffer = gadget;
        queuedTutorialPriceOverride = priceOverride;
    }

    public bool TryOpenTutorialOffer(GadgetId gadget, int priceOverride = -1)
    {
        QueueTutorialOffer(gadget, priceOverride);
        return TryOpenTimedShop();
    }

    public bool TryOpenTimedShop()
    {
        return TryOpenTimedShop(openedFromDealerFish: false);
    }

    private bool TryOpenTimedShop(bool openedFromDealerFish)
    {
        ResolveReferences();
        ResolveUiReferences();
        WireButtons();

        if (isOpen || session == null || !session.IsPlaying)
        {
            ClearQueuedTutorialOffer();
            return false;
        }

        RunGadgetUnlockService.RefreshUnlockedRunGadgets();
        currentOffer = SelectCurrentOffer();
        if (currentOffer == null)
        {
            ClearQueuedTutorialOffer();
            return false;
        }

        currentRandomPriceMultiplier = RollRandomPriceMultiplier();
        currentPrice = CalculateCurrentPrice();
        remainingSeconds = Mathf.Max(0.5f, offerDurationSeconds);
        ClearQueuedTutorialOffer();
        currentShopOpenedFromDealerFish = openedFromDealerFish;
        currentShopPurchased = false;

        if (pauseGameplayWhileOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isHoldingTimeScale = true;
        }

        BlockInkPulseActivationBriefly();
        isOpen = true;
        ApplyState(ShopEventState.Offering);
        SetVisible(true);
        RefreshOfferUi();
        onShopOpened.Invoke();
        return true;
    }

    private bool CanAttemptOpenTimedShop()
    {
        ResolveReferences();
        ResolveUiReferences();
        WireButtons();

        if (isOpen || session == null || !session.IsPlaying)
        {
            return false;
        }

        RunGadgetUnlockService.RefreshUnlockedRunGadgets();
        if (queuedTutorialOffer != GadgetId.None)
        {
            return FindOfferByGadget(queuedTutorialOffer) != null;
        }

        return ShopOfferSelector.HasAnyOffer(offers, RunGadgetUnlockService.CanOfferAppearInRunShop);
    }

    private ShopGadgetOffer SelectCurrentOffer()
    {
        if (queuedTutorialOffer != GadgetId.None)
        {
            return FindOfferByGadget(queuedTutorialOffer);
        }

        return ShopOfferSelector.SelectOffer(offers, RunGadgetUnlockService.CanOfferAppearInRunShop);
    }

    private ShopGadgetOffer FindOfferByGadget(GadgetId gadget)
    {
        if (offers == null)
        {
            return null;
        }

        for (int i = 0; i < offers.Length; i++)
        {
            if (offers[i] != null && offers[i].GadgetId == gadget)
            {
                return offers[i];
            }
        }

        return null;
    }

    private int CalculateCurrentPrice()
    {
        if (queuedTutorialPriceOverride >= 0)
        {
            return queuedTutorialPriceOverride;
        }

        return ShopPriceCalculator.CalculatePrice(
            currentOffer,
            RuntimeRunScore.TotalScore,
            scorePriceStep,
            globalPriceMultiplier,
            currentRandomPriceMultiplier);
    }

    private void ClearQueuedTutorialOffer()
    {
        queuedTutorialOffer = GadgetId.None;
        queuedTutorialPriceOverride = -1;
    }

    public void BuyCurrentOffer()
    {
        if (!isOpen || currentOffer == null)
        {
            return;
        }

        GadgetId gadget = currentOffer.GadgetId;
        if (RuntimeGadgetInventory.HasGadget(gadget) || currentShopPurchased)
        {
            SetInsufficientFundsVisible(false);
            SetBuyButtonInteractable(false);
            return;
        }

        if (!ShrimpRuntimeWallet.TrySpend(currentPrice))
        {
            SetInsufficientFundsVisible(true);
            return;
        }

        if (!RuntimeGadgetInventory.Acquire(gadget, currentOffer.Icon, currentOffer.IconTint))
        {
            ShrimpRuntimeWallet.Refund(currentPrice);
            SetInsufficientFundsVisible(false);
            return;
        }

        currentShopPurchased = true;
        onGadgetPurchased.Invoke(gadget);
        CloseShop();
    }

    public void CloseShop()
    {
        if (!isOpen)
        {
            return;
        }

        bool shouldPlayFirstExitComic = currentShopOpenedFromDealerFish
            && (session == null || !session.IsGameOver)
            && RuntimeInGameShopLoreState.TryMarkFirstDealerShopExit();
        bool purchasedDuringDealerShop = currentShopPurchased;

        isOpen = false;
        currentOffer = null;
        currentShopOpenedFromDealerFish = false;
        currentShopPurchased = false;
        BlockInkPulseActivationBriefly();
        RestoreTimeScaleIfNeeded();
        HideImmediate();
        ApplyState(ShopEventState.Closed);
        onShopClosed.Invoke();

        if (shouldPlayFirstExitComic)
        {
            StartCoroutine(PlayFirstDealerShopExitComicRoutine(purchasedDuringDealerShop));
        }
    }

    private IEnumerator PlayFirstDealerShopExitComicRoutine(bool purchased)
    {
        yield return LoreComicPresenter.PlayInGameShopLastIfAvailable(purchased);
    }

    private void ApplyState(ShopEventState nextState)
    {
        ShopEventState previousState = CurrentState;
        if (previousState == nextState)
        {
            return;
        }

        CurrentState = nextState;
        StateChanged?.Invoke(previousState, nextState);
        onStateChanged.Invoke(nextState);
    }

    private float RollRandomPriceMultiplier()
    {
        float min = Mathf.Min(randomPriceMultiplierMin, randomPriceMultiplierMax);
        float max = Mathf.Max(randomPriceMultiplierMin, randomPriceMultiplierMax);

        if (Mathf.Approximately(min, max))
        {
            return Mathf.Max(0f, min);
        }

        return UnityEngine.Random.Range(min, max);
    }

    private void RefreshOfferUi()
    {
        if (currentOffer == null)
        {
            ApplyIcon(null, Color.clear);
            SetText(priceText, "-");
            SetText(timerText, Mathf.CeilToInt(remainingSeconds).ToString());
            SetInsufficientFundsVisible(false);
            SetBuyButtonInteractable(false);
            return;
        }

        ApplyIcon(currentOffer.Icon, currentOffer.IconTint);
        SetText(priceText, currentPrice.ToString());
        RefreshTimer();
        SetInsufficientFundsVisible(false);
        SetBuyButtonInteractable(!currentShopPurchased && !RuntimeGadgetInventory.HasGadget(currentOffer.GadgetId));
    }

    private void RefreshTimer()
    {
        SetText(timerText, Mathf.CeilToInt(remainingSeconds).ToString());
    }

    private void ApplyIcon(Sprite icon, Color tint)
    {
        if (gadgetImage == null)
        {
            return;
        }

        gadgetImage.enabled = icon != null;
        gadgetImage.sprite = icon;
        gadgetImage.color = icon != null ? tint : Color.clear;
        gadgetImage.preserveAspect = true;
    }

    private void AnimateAttentionTexts()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * textPulseFrequency) * textPulseAmplitude;
        if (priceText != null)
        {
            priceText.rectTransform.localScale = priceTextBaseScale * pulse;
        }

        if (buyKeyText != null)
        {
            buyKeyText.rectTransform.localScale = buyKeyTextBaseScale * pulse;
        }
    }

    private void CacheAnimatedTextScales()
    {
        if (priceText != null)
        {
            priceTextBaseScale = priceText.rectTransform.localScale;
        }

        if (buyKeyText != null)
        {
            buyKeyTextBaseScale = buyKeyText.rectTransform.localScale;
        }
    }

    private void ResetAnimatedTextScales()
    {
        if (priceText != null)
        {
            priceText.rectTransform.localScale = priceTextBaseScale;
        }

        if (buyKeyText != null)
        {
            buyKeyText.rectTransform.localScale = buyKeyTextBaseScale;
        }
    }

    private void SetVisible(bool visible)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(visible);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private void HideImmediate()
    {
        SetVisible(false);
        ApplyIcon(null, Color.clear);
        SetInsufficientFundsVisible(false);
        SetBuyButtonInteractable(false);
        ResetAnimatedTextScales();
    }

    private void SetInsufficientFundsVisible(bool visible)
    {
        if (insufficientFundsText != null)
        {
            insufficientFundsText.gameObject.SetActive(visible);
        }
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!isHoldingTimeScale)
        {
            return;
        }

        isHoldingTimeScale = false;
        if (session != null && !session.IsPlaying)
        {
            return;
        }

        if (session == null || session.IsPlaying)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }

    }

    private void ResolveUiReferences()
    {
        Transform uiRoot = menuRoot != null ? menuRoot.transform : transform.Find("InGameCanvas");
        if (menuRoot == null && uiRoot != null)
        {
            menuRoot = uiRoot.gameObject;
        }

        if (canvasGroup == null && menuRoot != null)
        {
            canvasGroup = menuRoot.GetComponent<CanvasGroup>();
        }

        gadgetImage ??= FindChildComponent<Image>(uiRoot, "Gadget");
        priceText ??= FindChildComponent<TMP_Text>(uiRoot, "Precio");
        buyKeyText ??= FindChildComponent<TMP_Text>(uiRoot, "B");
        buyButton ??= UiButtonContract.FindButton(uiRoot, "ComprarBoton", "Comprar");
        insufficientFundsText ??= FindChildComponent<TMP_Text>(uiRoot, "SinSaldo");
        timerText ??= FindChildComponent<TMP_Text>(uiRoot, "Tiempo");
        timerText ??= FindChildComponent<TMP_Text>(uiRoot, "Timer");
    }

    private void WireButtons()
    {
        if (buyButton == null)
        {
            return;
        }

        if (buyButton.targetGraphic == null)
        {
            buyButton.targetGraphic = buyButton.GetComponent<Graphic>();
        }

        if (buyButton.targetGraphic != null)
        {
            buyButton.targetGraphic.raycastTarget = true;
        }

        buyButton.onClick.RemoveListener(BuyCurrentOffer);
        buyButton.onClick.AddListener(BuyCurrentOffer);
    }

    private void SetBuyButtonInteractable(bool interactable)
    {
        if (buyButton != null)
        {
            buyButton.interactable = interactable;
        }
    }

    private T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName && children[i].TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || menuRoot == null || gadgetImage == null || priceText == null || buyKeyText == null || buyButton == null || insufficientFundsText == null)
        {
            Debug.LogWarning(
                "[InGameShopManager] Faltan referencias. Asigna Session, MenuRoot/InGameCanvas, Gadget, Precio, B, Comprar y SinSaldo en el canvas de tienda.",
                this);
        }

        if (offers == null || offers.Length == 0)
        {
            Debug.LogWarning("[InGameShopManager] No hay ofertas configuradas. Asigna prefabs con GadgetShopItem en Offers.", this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            RestoreTimeScaleIfNeeded();
            pendingWorldShopRoutine = null;
            instance = null;
        }
    }

    private static void BlockInkPulseActivationBriefly()
    {
        inkPulseActivationBlockedUntilFrame = Mathf.Max(inkPulseActivationBlockedUntilFrame, Time.frameCount + 1);
    }
}
