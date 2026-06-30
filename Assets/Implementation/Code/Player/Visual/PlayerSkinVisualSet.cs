using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSkinVisualSet : MonoBehaviour
{
    [Header("Visual Roots")]
    [SerializeField] private GameObject movementVisualRoot;
    [SerializeField] private GameObject inkPulseVisualRoot;
    [SerializeField] private GameObject portalVisualRoot;

    [Header("Animators")]
    [SerializeField] private Animator movementAnimator;
    [SerializeField] private Animator inkPulseAnimator;
    [SerializeField] private Animator portalAnimator;

    [Header("Animation States")]
    [SerializeField] private string inkPulseStateName = "InkPulse";
    [SerializeField] private string inkPulseClipName = "InkPulse";
    [SerializeField] private string portalStateName = "Portal";
    [SerializeField] private string portalClipName = "PortalEffect";
    [SerializeField, Min(0.01f)] private float fallbackInkPulseClipLength = 1f;
    [SerializeField, Min(0.01f)] private float fallbackPortalClipLength = 1f;

    public GameObject MovementVisualRoot => movementVisualRoot;
    public GameObject InkPulseVisualRoot => inkPulseVisualRoot;
    public GameObject PortalVisualRoot => portalVisualRoot;
    public Animator MovementAnimator => movementAnimator;
    public Animator InkPulseAnimator => inkPulseAnimator;
    public Animator PortalAnimator => portalAnimator;
    public string InkPulseStateName => inkPulseStateName;
    public string InkPulseClipName => inkPulseClipName;
    public string PortalStateName => portalStateName;
    public string PortalClipName => portalClipName;
    public float FallbackInkPulseClipLength => Mathf.Max(0.01f, fallbackInkPulseClipLength);
    public float FallbackPortalClipLength => Mathf.Max(0.01f, fallbackPortalClipLength);

    public bool IsConfigured => movementVisualRoot != null
        && inkPulseVisualRoot != null
        && portalVisualRoot != null;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void ResolveReferences()
    {
        movementVisualRoot ??= ResolveVisualRoot("SquidVisual", "MovementVisual");
        inkPulseVisualRoot ??= ResolveVisualRoot("InkPulseVisual");
        portalVisualRoot ??= ResolveVisualRoot("PortalVisual");

        movementAnimator ??= ResolveAnimator(movementVisualRoot);
        inkPulseAnimator ??= ResolveAnimator(inkPulseVisualRoot);
        portalAnimator ??= ResolveAnimator(portalVisualRoot);
    }

    private GameObject ResolveVisualRoot(params string[] names)
    {
        if (names == null)
        {
            return null;
        }

        for (int index = 0; index < names.Length; index++)
        {
            Transform directChild = transform.Find(names[index]);
            if (directChild != null)
            {
                return directChild.gameObject;
            }
        }

        Transform[] children = GetComponentsInChildren<Transform>(includeInactive: true);
        for (int childIndex = 0; childIndex < children.Length; childIndex++)
        {
            Transform child = children[childIndex];
            if (child == transform)
            {
                continue;
            }

            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (child.name == names[nameIndex])
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static Animator ResolveAnimator(GameObject visualRoot)
    {
        return visualRoot != null
            ? visualRoot.GetComponent<Animator>() ?? visualRoot.GetComponentInChildren<Animator>(includeInactive: true)
            : null;
    }
}
