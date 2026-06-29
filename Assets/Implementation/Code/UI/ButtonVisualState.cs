using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ButtonVisualState : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Visual States")]
    [SerializeField] private GameObject normalState;
    [SerializeField] private GameObject highlightedState;
    [SerializeField] private GameObject pressedState;
    [SerializeField] private bool usePressedStateWhenSelected;

    [Header("Pressed SFX")]
    [SerializeField] private AudioSource pressedAudioSource;
    [SerializeField] private AudioClip pressedSfx;
    [SerializeField, Range(0f, 1f)] private float pressedSfxVolume = 1f;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor;
    [SerializeField] private ButtonVisualStateKind editorPreviewState = ButtonVisualStateKind.Normal;

    private bool isPointerInside;
    private bool isPointerDown;
    private bool isSelected;
    private bool wasInteractable = true;
    private ButtonVisualStateKind currentState = ButtonVisualStateKind.Normal;
    private Coroutine submitReleaseRoutine;

    public Button Button => button;
    public GameObject NormalState => normalState;
    public GameObject HighlightedState => highlightedState;
    public GameObject PressedState => pressedState;
    public AudioSource PressedAudioSource => pressedAudioSource;
    public AudioClip PressedSfx => pressedSfx;

    public void SetUsePressedStateWhenSelected(bool value)
    {
        if (usePressedStateWhenSelected == value)
        {
            return;
        }

        usePressedStateWhenSelected = value;
        UpdateVisualState();
    }

    private void Reset()
    {
        ResolveReferences();
        ApplyState(ButtonVisualStateKind.Normal);
    }

    private void Awake()
    {
        ResolveReferences();
        wasInteractable = CanInteract();
        ApplyState(ButtonVisualStateKind.Normal);
    }

    private void OnEnable()
    {
        UpdateVisualState();
    }

    private void Update()
    {
        bool isInteractable = CanInteract();
        if (isInteractable == wasInteractable)
        {
            return;
        }

        wasInteractable = isInteractable;
        if (!isInteractable)
        {
            isPointerDown = false;
            isPointerInside = false;
            isSelected = false;
        }

        UpdateVisualState();
    }

    private void OnDisable()
    {
        if (submitReleaseRoutine != null)
        {
            StopCoroutine(submitReleaseRoutine);
            submitReleaseRoutine = null;
        }

        isPointerDown = false;
        isPointerInside = false;
        isSelected = false;
        ApplyState(ButtonVisualStateKind.Normal);
    }

    private void OnValidate()
    {
        ResolveReferences();
        if (Application.isPlaying)
        {
            return;
        }

        ApplyState(previewInEditor ? editorPreviewState : ButtonVisualStateKind.Normal);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        isPointerInside = true;
        UpdateVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPointerDown = false;
        UpdateVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        isPointerDown = true;
        ApplyState(ButtonVisualStateKind.Presionado);
        PlayPressedSfx();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        UpdateVisualState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        isSelected = true;
        UpdateVisualState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        isPointerDown = false;
        UpdateVisualState();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        ApplyState(ButtonVisualStateKind.Presionado);
        PlayPressedSfx();

        if (submitReleaseRoutine != null)
        {
            StopCoroutine(submitReleaseRoutine);
        }

        submitReleaseRoutine = StartCoroutine(ReleaseSubmitVisualState());
    }

    private void UpdateVisualState()
    {
        if (!CanInteract())
        {
            ApplyState(ButtonVisualStateKind.Normal);
            return;
        }

        if (isPointerDown)
        {
            ApplyState(ButtonVisualStateKind.Presionado);
            return;
        }

        if (isSelected && usePressedStateWhenSelected)
        {
            ApplyState(ButtonVisualStateKind.Presionado);
            return;
        }

        ApplyState(isPointerInside || isSelected
            ? ButtonVisualStateKind.Destacado
            : ButtonVisualStateKind.Normal);
    }

    private void ApplyState(ButtonVisualStateKind state)
    {
        currentState = state;
        SetActive(normalState, state == ButtonVisualStateKind.Normal);
        SetActive(highlightedState, state == ButtonVisualStateKind.Destacado);
        SetActive(pressedState, state == ButtonVisualStateKind.Presionado);
    }

    private void PlayPressedSfx()
    {
        if (pressedAudioSource == null)
        {
            return;
        }

        if (pressedSfx != null)
        {
            pressedAudioSource.PlayOneShot(pressedSfx, pressedSfxVolume);
            return;
        }

        if (pressedAudioSource.clip != null)
        {
            pressedAudioSource.Play();
        }
    }

    private IEnumerator ReleaseSubmitVisualState()
    {
        yield return new WaitForSecondsRealtime(0.08f);
        submitReleaseRoutine = null;
        UpdateVisualState();
    }

    private bool CanInteract()
    {
        return button != null && button.isActiveAndEnabled && button.IsInteractable();
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        Transform contractRoot = transform.parent;
        Transform visualRoot = contractRoot != null ? contractRoot.Find(UiButtonContract.VisualChildName) : null;
        if (visualRoot == null)
        {
            return;
        }

        normalState ??= ResolveState(visualRoot, UiButtonContract.NormalStateName);
        highlightedState ??= ResolveState(visualRoot, UiButtonContract.HighlightedStateName);
        pressedState ??= ResolveState(visualRoot, UiButtonContract.PressedStateName);
    }

    private static GameObject ResolveState(Transform visualRoot, string stateName)
    {
        Transform state = visualRoot.Find(stateName);
        return state != null ? state.gameObject : null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}

public enum ButtonVisualStateKind
{
    Normal,
    Destacado,
    Presionado
}
