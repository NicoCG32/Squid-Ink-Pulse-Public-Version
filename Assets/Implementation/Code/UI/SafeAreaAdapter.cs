using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaAdapter : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private bool hasApplied;

    public RectTransform Target => target;

    private void Awake()
    {
        ResolveTarget();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Rect safeArea = Screen.safeArea;
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        if (!hasApplied || safeArea != lastSafeArea || screenSize != lastScreenSize)
        {
            Apply(safeArea, screenSize);
        }
    }

    public void Refresh()
    {
        Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
    }

    public void Apply(Rect safeArea, Vector2 screenSize)
    {
        ResolveTarget();
        if (target == null)
        {
            return;
        }

        SafeAreaAnchors anchors = SafeAreaAnchorPolicy.Resolve(safeArea, screenSize);
        target.anchorMin = anchors.Minimum;
        target.anchorMax = anchors.Maximum;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(
            Mathf.RoundToInt(screenSize.x),
            Mathf.RoundToInt(screenSize.y));
        hasApplied = true;
    }

    private void ResolveTarget()
    {
        target ??= GetComponent<RectTransform>();
    }
}
