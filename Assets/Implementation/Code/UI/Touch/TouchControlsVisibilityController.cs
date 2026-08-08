using UnityEngine;

[DisallowMultipleComponent]
public sealed class TouchControlsVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject controlsRoot;
    [SerializeField] private bool showInEditor;

    public GameObject ControlsRoot => controlsRoot;
    public bool ShowInEditor => showInEditor;

    private void OnEnable()
    {
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (controlsRoot == null)
        {
            Debug.LogWarning(
                "[TouchControlsVisibilityController] Falta asignar el root hijo de controles touch.",
                this);
            return;
        }

        if (controlsRoot == gameObject || !controlsRoot.transform.IsChildOf(transform))
        {
            Debug.LogError(
                "[TouchControlsVisibilityController] El root de controles touch debe ser un descendiente distinto del owner.",
                this);
            return;
        }

        bool shouldShow = TouchControlsVisibilityPolicy.ShouldShow(
            Application.platform,
            showInEditor);
        controlsRoot.SetActive(shouldShow);
    }
}
