using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class TouchSteeringSurface : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler,
    IEndDragHandler
{
    private readonly TouchSteeringCaptureState captureState = new();

    private SquidInkPulseGameplayInputReader inputReader;
    private GameSessionController boundSession;
    private InGameShopManager boundShop;
    private bool overlayInteractionAllowed = true;

    public bool HasActivePointer => captureState.HasActivePointer;

    private void OnEnable()
    {
        SquidInkPulseInputRuntime.GameplayChanged += HandleGameplayInputChanged;
        HandleGameplayInputChanged(SquidInkPulseInputRuntime.Gameplay);
        RefreshRuntimeBindings();
        CancelIfUnavailable();
    }

    private void Update()
    {
        RefreshRuntimeBindings();
        CancelIfUnavailable();
    }

    private void OnDisable()
    {
        SquidInkPulseInputRuntime.GameplayChanged -= HandleGameplayInputChanged;
        CancelActivePointer();
        BindSession(null);
        BindShop(null);
        inputReader = null;
    }

    private void OnDestroy()
    {
        CancelActivePointer();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelActivePointer();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CancelActivePointer();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null
            || eventData.button != PointerEventData.InputButton.Left
            || !IsTouchPointer(eventData))
        {
            return;
        }

        GameObject raycastTarget = eventData.pointerPressRaycast.gameObject != null
            ? eventData.pointerPressRaycast.gameObject
            : eventData.pointerCurrentRaycast.gameObject;
        bool startedOverInteractiveUi = TouchSteeringUiPolicy.StartedOverInteractiveUi(
            transform,
            raycastTarget);

        if (!captureState.TryBegin(
                eventData.pointerId,
                eventData.position,
                IsInteractionAllowed(),
                startedOverInteractiveUi))
        {
            return;
        }

        if (inputReader == null
            || !inputReader.TryBeginTouchSteering(this, eventData.pointerId, eventData.position))
        {
            captureState.Cancel();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsInteractionAllowed())
        {
            CancelActivePointer();
            return;
        }

        if (!IsTouchPointer(eventData)
            || !captureState.TryMove(eventData.pointerId, eventData.position))
        {
            return;
        }

        if (inputReader == null
            || !inputReader.TryUpdateTouchSteering(this, eventData.pointerId, eventData.position))
        {
            CancelActivePointer();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndPointer(eventData);
    }

    public void SetOverlayInteractionAllowed(bool isAllowed)
    {
        overlayInteractionAllowed = isAllowed;
        if (!isAllowed)
        {
            CancelActivePointer();
        }
    }

    public void CancelActivePointer()
    {
        captureState.Cancel();
        inputReader?.CancelTouchSteering(this);
    }

    private void EndPointer(PointerEventData eventData)
    {
        if (!IsTouchPointer(eventData) || !captureState.TryEnd(eventData.pointerId))
        {
            return;
        }

        inputReader?.TryEndTouchSteering(this, eventData.pointerId);
    }

    private void HandleGameplayInputChanged(SquidInkPulseGameplayInputReader nextReader)
    {
        CancelActivePointer();
        inputReader = nextReader;
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

        CancelActivePointer();
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

        CancelActivePointer();
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

    private void HandleSessionStateChanged(
        GameSessionState previousState,
        GameSessionState nextState)
    {
        if (nextState != GameSessionState.Playing)
        {
            CancelActivePointer();
        }
    }

    private void HandleShopStateChanged(
        ShopEventState previousState,
        ShopEventState nextState)
    {
        if (nextState != ShopEventState.Closed)
        {
            CancelActivePointer();
        }
    }

    private void CancelIfUnavailable()
    {
        if (!IsInteractionAllowed())
        {
            CancelActivePointer();
        }
    }

    private bool IsInteractionAllowed()
    {
        return TouchSteeringAvailabilityPolicy.IsAllowed(
            boundSession != null && boundSession.IsPlaying,
            InGameShopManager.BlocksInkPulseActivation,
            overlayInteractionAllowed,
            Time.timeScale > 0f,
            inputReader != null && inputReader.IsEnabled);
    }

    private static bool IsTouchPointer(PointerEventData eventData)
    {
        return eventData is ExtendedPointerEventData extendedEventData
            && extendedEventData.pointerType == UIPointerType.Touch;
    }
}
