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
    private readonly InGameShopOfferTimer offerTimer = new();
    private InGameShopTimeScaleHold timeScaleHold;
    private InGameShopOverlayPresenter overlayPresenter;
    private int currentPrice;
    private float currentRandomPriceMultiplier = 1f;
    private bool isOpen;
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
        EnsureTimeScaleHold();
        ResolveReferences();
        PrepareOverlayPresenter();
        overlayPresenter.HideImmediate();
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

        bool externalPause = session != null && !session.IsPlaying && !IsHoldingTimeScale;
        if (externalPause)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            BuyCurrentOffer();
        }

        overlayPresenter?.AnimateAttentionTexts(Time.unscaledTime, textPulseAmplitude, textPulseFrequency);

        float deltaTime = IsHoldingTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        bool expired = offerTimer.Tick(deltaTime);
        overlayPresenter?.RefreshTimer(offerTimer.RemainingSeconds);

        if (expired)
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
        PrepareOverlayPresenter();

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
        offerTimer.Start(offerDurationSeconds);
        ClearQueuedTutorialOffer();
        currentShopOpenedFromDealerFish = openedFromDealerFish;
        currentShopPurchased = false;

        EnsureTimeScaleHold();
        timeScaleHold.Begin(pauseGameplayWhileOpen);

        BlockInkPulseActivationBriefly();
        isOpen = true;
        ApplyState(ShopEventState.Offering);
        overlayPresenter.Show();
        RefreshOfferUi();
        onShopOpened.Invoke();
        return true;
    }

    private bool CanAttemptOpenTimedShop()
    {
        ResolveReferences();
        PrepareOverlayPresenter();

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

        if (currentShopPurchased)
        {
            overlayPresenter.SetInsufficientFundsVisible(false);
            overlayPresenter.SetBuyButtonInteractable(false);
            return;
        }

        InGameShopPurchaseResult result = InGameShopPurchaseService.TryPurchase(
            currentOffer.GadgetId,
            currentOffer.Icon,
            currentOffer.IconTint,
            currentPrice,
            RuntimeGadgetInventory.HasGadget,
            ShrimpRuntimeWallet.TrySpend,
            ShrimpRuntimeWallet.Refund,
            RuntimeGadgetInventory.Acquire);

        if (result == InGameShopPurchaseResult.AlreadyOwned)
        {
            overlayPresenter.SetInsufficientFundsVisible(false);
            overlayPresenter.SetBuyButtonInteractable(false);
            return;
        }

        if (result == InGameShopPurchaseResult.InsufficientFunds)
        {
            overlayPresenter.SetInsufficientFundsVisible(true);
            return;
        }

        if (result != InGameShopPurchaseResult.Success)
        {
            overlayPresenter.SetInsufficientFundsVisible(false);
            return;
        }

        currentShopPurchased = true;
        onGadgetPurchased.Invoke(currentOffer.GadgetId);
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
        offerTimer.Stop();
        currentShopOpenedFromDealerFish = false;
        currentShopPurchased = false;
        BlockInkPulseActivationBriefly();
        RestoreTimeScaleIfNeeded();
        overlayPresenter?.HideImmediate();
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
            overlayPresenter.PresentEmpty(offerTimer.RemainingSeconds);
            return;
        }

        bool canPurchase = !currentShopPurchased && !RuntimeGadgetInventory.HasGadget(currentOffer.GadgetId);
        overlayPresenter.PresentOffer(
            currentOffer.Icon,
            currentOffer.IconTint,
            currentPrice,
            offerTimer.RemainingSeconds,
            canPurchase);
    }

    private void RestoreTimeScaleIfNeeded()
    {
        EnsureTimeScaleHold();
        timeScaleHold.End(session == null || session.IsPlaying);
    }

    private bool IsHoldingTimeScale => timeScaleHold != null && timeScaleHold.IsHolding;

    private void EnsureTimeScaleHold()
    {
        timeScaleHold ??= new InGameShopTimeScaleHold(
            getTimeScale: () => Time.timeScale,
            setTimeScale: value => Time.timeScale = value);
    }

    private void ResolveReferences()
    {
        if (session == null && GameSessionController.HasInstance)
        {
            session = GameSessionController.Instance;
        }
    }

    private void RefreshOverlayPresenter()
    {
        overlayPresenter = new InGameShopOverlayPresenter(
            menuRoot,
            canvasGroup,
            gadgetImage,
            priceText,
            buyKeyText,
            buyButton,
            insufficientFundsText,
            timerText);
    }

    private void PrepareOverlayPresenter()
    {
        RefreshOverlayPresenter();
        overlayPresenter.WireBuyButton(BuyCurrentOffer);
        overlayPresenter.CacheAnimatedTextScales();
    }

    private void WarnIfMissingReferences()
    {
        if (session == null || menuRoot == null || canvasGroup == null || gadgetImage == null || priceText == null || buyKeyText == null || buyButton == null || insufficientFundsText == null)
        {
            Debug.LogWarning(
                "[InGameShopManager] Faltan referencias serializadas. Asigna Session, MenuRoot, CanvasGroup, Gadget, Precio, B, Comprar y SinSaldo desde GameRoot/GameUIRoot.",
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
