using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameUIRoot : MonoBehaviour
{
    [Header("Composition Roots")]
    [SerializeField] private Transform eventSystemRoot;
    [SerializeField] private RectTransform hudRoot;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject gameOverMenuRoot;
    [SerializeField] private GameObject inGameShopMenuRoot;

    [Header("HUD")]
    [SerializeField] private ChargeBar inkBar;
    [SerializeField] private GadgetInventoryHud gadgetSlots;
    [SerializeField] private ShrimpCounterDisplay shrimpCounter;
    [SerializeField] private ScoreCounterDisplay scoreCounter;

    [Header("Managers")]
    [SerializeField] private PauseMenuManager pauseMenuManager;
    [SerializeField] private GameOverMenuManager gameOverMenuManager;
    [SerializeField] private InGameShopManager inGameShopManager;

    public Transform EventSystemRoot => eventSystemRoot;
    public RectTransform HudRoot => hudRoot;
    public GameObject PauseMenuRoot => pauseMenuRoot;
    public GameObject GameOverMenuRoot => gameOverMenuRoot;
    public GameObject InGameShopMenuRoot => inGameShopMenuRoot;
    public ChargeBar InkBar => inkBar;
    public GadgetInventoryHud GadgetSlots => gadgetSlots;
    public ShrimpCounterDisplay ShrimpCounter => shrimpCounter;
    public ScoreCounterDisplay ScoreCounter => scoreCounter;
    public PauseMenuManager PauseMenuManager => pauseMenuManager;
    public GameOverMenuManager GameOverMenuManager => gameOverMenuManager;
    public InGameShopManager InGameShopManager => inGameShopManager;

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        WarnIfMissingReferences();
    }

    private void ResolveReferences()
    {
        eventSystemRoot ??= FindChildTransform("EventSystem");
        hudRoot ??= FindChildComponent<RectTransform>("HUD");
        pauseMenuRoot ??= FindChildGameObject("PauseCanvas");
        gameOverMenuRoot ??= FindChildGameObject("GameOverCanvas");
        inGameShopMenuRoot ??= FindChildGameObject("InGameCanvas");

        inkBar ??= GetComponentInChildren<ChargeBar>(includeInactive: true);
        gadgetSlots ??= GetComponentInChildren<GadgetInventoryHud>(includeInactive: true);
        shrimpCounter ??= GetComponentInChildren<ShrimpCounterDisplay>(includeInactive: true);
        scoreCounter ??= GetComponentInChildren<ScoreCounterDisplay>(includeInactive: true);

        pauseMenuManager ??= GetComponentInChildren<PauseMenuManager>(includeInactive: true);
        gameOverMenuManager ??= GetComponentInChildren<GameOverMenuManager>(includeInactive: true);
        inGameShopManager ??= GetComponentInChildren<InGameShopManager>(includeInactive: true);
    }

    private void WarnIfMissingReferences()
    {
        if (eventSystemRoot == null
            || hudRoot == null
            || pauseMenuRoot == null
            || gameOverMenuRoot == null
            || inGameShopMenuRoot == null
            || inkBar == null
            || gadgetSlots == null
            || shrimpCounter == null
            || scoreCounter == null
            || pauseMenuManager == null
            || gameOverMenuManager == null
            || inGameShopManager == null)
        {
            Debug.LogWarning(
                "[GameUIRoot] Faltan referencias de composicion. Verifica EventSystem, HUD, vistas prefab y managers UI bajo este root.",
                this);
        }
    }

    private Transform FindChildTransform(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private GameObject FindChildGameObject(string childName)
    {
        Transform child = FindChildTransform(childName);
        return child != null ? child.gameObject : null;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = FindChildTransform(childName);
        return child != null && child.TryGetComponent(out T component) ? component : null;
    }
}
