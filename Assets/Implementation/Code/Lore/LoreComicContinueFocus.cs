using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LoreComicContinueFocus : MonoBehaviour
{
    [SerializeField] private Button button;
    private EventSystem owner;
    private GameObject previousSelection;
    private GameObject ownedSelection;
    private Navigation previousNavigation;
    private bool ownsFocus;

    public void SetPresented(bool visible, bool showContinue)
    {
        if (!visible) { Release(); return; }
        if (EventSystem.current == null || EventSystem.current.alreadySelecting) return;
        GameObject target = showContinue && button != null && button.gameObject.activeInHierarchy
            ? button.gameObject : null;
        if (ownsFocus)
        {
            if (owner == EventSystem.current && owner.currentSelectedGameObject == ownedSelection)
            {
                owner.SetSelectedGameObject(target);
                ownedSelection = target;
            }
            return;
        }
        owner = EventSystem.current;
        previousSelection = owner.currentSelectedGameObject;
        if (button != null)
        {
            previousNavigation = button.navigation;
            Navigation navigation = previousNavigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
        ownsFocus = true;
        ownedSelection = target;
        owner.SetSelectedGameObject(target);
    }

    private void OnDisable() => Release();

    private void Release()
    {
        if (!ownsFocus) return;
        ownsFocus = false;
        if (button != null) button.navigation = previousNavigation;
        if (owner != null && owner.currentSelectedGameObject == ownedSelection)
        {
            bool canRestore = previousSelection != null && previousSelection.activeInHierarchy;
            Selectable selectable = canRestore ? previousSelection.GetComponent<Selectable>() : null;
            if (selectable != null && !selectable.IsInteractable()) canRestore = false;
            owner.SetSelectedGameObject(canRestore ? previousSelection : null);
        }
        owner = null;
        previousSelection = null;
        ownedSelection = null;
    }
}
