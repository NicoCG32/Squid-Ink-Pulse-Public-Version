using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[DisallowMultipleComponent]
public sealed class ShopMenuCancelNavigation : MonoBehaviour
{
    [SerializeField] private MainMenu menu;
    [SerializeField] private OptionsMenuManager options;
    [SerializeField] private InputSystemUIInputModule inputModule;
    private InputAction cancelAction;

    private void OnEnable() { BindForPlatform(Application.isMobilePlatform); }
    private void OnDisable()
    {
        if (cancelAction!=null) cancelAction.performed-=HandleCancel;
        cancelAction=null;
    }

    private void BindForPlatform(bool isMobilePlatform)
    {
        if (!isMobilePlatform || cancelAction!=null || inputModule==null) return;
        cancelAction=inputModule.cancel?.action;
        if (cancelAction!=null) cancelAction.performed+=HandleCancel;
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        if (options!=null && options.IsOpen) options.Close();
        else if (menu!=null) menu.VolverAlMenuPrincipal();
    }
}
