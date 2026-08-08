using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class TouchGameplayCommandButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private SquidInkPulseGameplayCommand command;
    [SerializeField] private Button button;

    private bool hasPointerOwner;
    private int pointerOwnerId;
    private SquidInkPulseGameplayInputReader capturedReader;
    private GameSessionController boundSession;
    private InGameShopManager boundShop;

    public SquidInkPulseGameplayCommand Command => command;
    public Button Button => button;
    public bool HasPointerOwner => hasPointerOwner;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SquidInkPulseInputRuntime.GameplayChanged += HandleGameplayChanged;
        RefreshRuntimeBindings();
    }

    private void Update()
    {
        RefreshRuntimeBindings();
    }

    private void OnDisable()
    {
        SquidInkPulseInputRuntime.GameplayChanged -= HandleGameplayChanged;
        BindSession(null);
        BindShop(null);
        CancelPointer();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelPointer();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CancelPointer();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ResolveReferences();
        if (eventData == null
            || eventData.button != PointerEventData.InputButton.Left
            || hasPointerOwner
            || button == null
            || !button.IsActive()
            || !button.IsInteractable())
        {
            return;
        }

        hasPointerOwner = true;
        pointerOwnerId = eventData.pointerId;
        capturedReader = SquidInkPulseInputRuntime.Gameplay;
        if (capturedReader == null || !capturedReader.IsEnabled)
        {
            CancelPointer();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null
            || !hasPointerOwner
            || eventData.pointerId != pointerOwnerId)
        {
            return;
        }

        SquidInkPulseGameplayInputReader reader = capturedReader;
        bool releasedInside = IsWithinButton(eventData.pointerCurrentRaycast.gameObject);
        bool canDispatch = releasedInside
            && button != null
            && button.IsActive()
            && button.IsInteractable()
            && ReferenceEquals(reader, SquidInkPulseInputRuntime.Gameplay)
            && reader != null
            && reader.IsEnabled;
        CancelPointer();
        if (canDispatch)
        {
            reader.TryRequestTouchCommand(command);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData != null
            && hasPointerOwner
            && eventData.pointerId == pointerOwnerId)
        {
            CancelPointer();
        }
    }

    public void CancelPointer()
    {
        hasPointerOwner = false;
        pointerOwnerId = 0;
        capturedReader = null;
    }

    private bool IsWithinButton(GameObject target)
    {
        return target != null
            && (target == gameObject || target.transform.IsChildOf(transform));
    }

    private void ResolveReferences()
    {
        button ??= GetComponent<Button>();
    }

    private void RefreshRuntimeBindings()
    {
        BindSession(GameSessionController.Instance);
        BindShop(InGameShopManager.Instance);
    }

    private void BindSession(GameSessionController nextSession)
    {
        if (boundSession == nextSession)
        {
            return;
        }

        CancelPointer();
        if (boundSession != null)
        {
            boundSession.StateChanged -= HandleSessionStateChanged;
        }

        boundSession = nextSession;
        if (boundSession != null)
        {
            boundSession.StateChanged += HandleSessionStateChanged;
        }
    }

    private void BindShop(InGameShopManager nextShop)
    {
        if (boundShop == nextShop)
        {
            return;
        }

        CancelPointer();
        if (boundShop != null)
        {
            boundShop.StateChanged -= HandleShopStateChanged;
        }

        boundShop = nextShop;
        if (boundShop != null)
        {
            boundShop.StateChanged += HandleShopStateChanged;
        }
    }

    private void HandleGameplayChanged(SquidInkPulseGameplayInputReader nextReader)
    {
        CancelPointer();
    }

    private void HandleSessionStateChanged(
        GameSessionState previousState,
        GameSessionState nextState)
    {
        CancelPointer();
    }

    private void HandleShopStateChanged(ShopEventState previousState, ShopEventState nextState)
    {
        CancelPointer();
    }
}
