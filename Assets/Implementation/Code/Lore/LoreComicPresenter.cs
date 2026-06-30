using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum LoreComicEvent
{
    None = 0,
    GameStart = 10,
    PortalEpipelagicToAbyssopelagic = 20,
    PortalAbyssopelagicToEpipelagic = 30,
    Defeat = 40,
    ScoreMilestone = 50,
    ShopInGameFirst = 60,
    ShopInGameLastPurchased = 70,
    ShopInGameLastNoPurchase = 80
}

public enum LoreComicZone
{
    Unknown = 0,
    Epipelagic = 10,
    Abyssopelagic = 20,
    Tutorial = 30
}

public readonly struct LoreComicRequest
{
    public LoreComicRequest(LoreComicEvent comicEvent, LoreComicZone zone, string fromSceneName = null, string toSceneName = null)
    {
        ComicEvent = comicEvent;
        Zone = zone;
        FromSceneName = fromSceneName;
        ToSceneName = toSceneName;
    }

    public LoreComicEvent ComicEvent { get; }
    public LoreComicZone Zone { get; }
    public string FromSceneName { get; }
    public string ToSceneName { get; }
}

[Serializable]
public sealed class LoreComicEntry
{
    public LoreComicEvent comicEvent = LoreComicEvent.GameStart;
    public LoreComicZone zone = LoreComicZone.Unknown;
    public Sprite[] sprites = Array.Empty<Sprite>();
    [Min(0f)] public float displaySeconds = 3f;
    public bool waitForContinue;
    public bool showContinueButton;

    public bool Matches(LoreComicRequest request)
    {
        if (comicEvent != request.ComicEvent)
        {
            return false;
        }

        return zone == LoreComicZone.Unknown || zone == request.Zone;
    }
}

[DisallowMultipleComponent]
public sealed class LoreComicPresenter : MonoBehaviour
{
    private const int MinimumComicSortingOrder = 5000;

    [Header("References")]
    [SerializeField] private GameObject comicRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image comicImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject continueButtonRoot;

    [Header("Catalog")]
    [SerializeField] private LoreComicEntry[] entries = Array.Empty<LoreComicEntry>();

    [Header("Defaults")]
    [SerializeField, Min(0f)] private float defaultDisplaySeconds = 3f;
    [SerializeField, Min(0f)] private float defaultStartDisplaySeconds = 0f;
    [SerializeField] private bool defaultStartWaitsForContinue = true;
    [SerializeField] private bool defaultStartShowsContinueButton = true;
    [SerializeField] private bool pauseTimeWhileShowing = true;
    [SerializeField] private bool allowExternalContinueWithoutButton;
    [SerializeField] private bool hideOnAwake = true;

    private Coroutine activeRoutine;
    private Action activeCompletion;
    private bool continueRequested;
    private bool pauseApplied;
    private float timeScaleBeforePresentation = 1f;

    private void Awake()
    {
        ResolveReferences();
        NormalizeRenderableScale();
        WireContinueButton();

        if (hideOnAwake)
        {
            HideImmediate();
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        WireContinueButton();
    }

    private void OnDisable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Continue);
        }

        RestoreTimeScaleIfNeeded();
    }

    public void Continue()
    {
        continueRequested = true;
    }

    public bool TryPlay(LoreComicRequest request, Action onCompleted)
    {
        ResolveReferences();
        NormalizeRenderableScale();

        if (comicRoot == null)
        {
            Debug.LogWarning("[LoreComicPresenter] Falta ComicRoot. El comic se omitira.", this);
            return false;
        }

        LoreComicEntry entry = FindEntry(request);
        if (!HasDisplayableSprite(entry))
        {
            return false;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        activeCompletion = onCompleted;
        activeRoutine = StartCoroutine(RunPresentation(request, entry));
        return true;
    }

    public static IEnumerator PlayGameStartIfAvailable()
    {
        LoreComicRequest request = new LoreComicRequest(LoreComicEvent.GameStart, LoreComicZone.Unknown);
        yield return PlayIfAvailable(request);
    }

    public static IEnumerator PlayPortalTransitionIfAvailable(string destinationSceneName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        LoreComicZone fromZone = LoreComicZoneResolver.ResolveFromScene(activeScene);
        LoreComicZone toZone = LoreComicZoneResolver.ResolveFromSceneName(destinationSceneName);
        LoreComicEvent comicEvent = LoreComicZoneResolver.ResolvePortalEvent(fromZone, toZone);
        if (comicEvent == LoreComicEvent.None)
        {
            yield break;
        }

        string fromSceneName = !string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.path : activeScene.name;
        LoreComicRequest request = new LoreComicRequest(comicEvent, toZone, fromSceneName, destinationSceneName);
        yield return PlayIfAvailable(request);
    }

    public static IEnumerator PlayDefeatIfAvailable()
    {
        LoreComicZone activeZone = LoreComicZoneResolver.ResolveFromScene(SceneManager.GetActiveScene());
        LoreComicRequest request = new LoreComicRequest(LoreComicEvent.Defeat, activeZone);
        yield return PlayIfAvailable(request);
    }

    public static IEnumerator PlayInGameShopFirstIfAvailable()
    {
        LoreComicZone activeZone = LoreComicZoneResolver.ResolveFromScene(SceneManager.GetActiveScene());
        LoreComicRequest request = new LoreComicRequest(LoreComicEvent.ShopInGameFirst, activeZone);
        yield return PlayIfAvailable(request);
    }

    public static IEnumerator PlayInGameShopLastIfAvailable(bool purchased)
    {
        LoreComicZone activeZone = LoreComicZoneResolver.ResolveFromScene(SceneManager.GetActiveScene());
        LoreComicEvent comicEvent = purchased
            ? LoreComicEvent.ShopInGameLastPurchased
            : LoreComicEvent.ShopInGameLastNoPurchase;
        LoreComicRequest request = new LoreComicRequest(comicEvent, activeZone);
        yield return PlayIfAvailable(request);
    }

    public static IEnumerator PlayInGameShopLastPurchasedIfAvailable()
    {
        LoreComicZone activeZone = LoreComicZoneResolver.ResolveFromScene(SceneManager.GetActiveScene());
        LoreComicRequest request = new LoreComicRequest(LoreComicEvent.ShopInGameLastPurchased, activeZone);
        yield return PlayIfAvailable(request);
    }

    public static IEnumerator PlayInGameShopLastNoPurchaseIfAvailable()
    {
        LoreComicZone activeZone = LoreComicZoneResolver.ResolveFromScene(SceneManager.GetActiveScene());
        LoreComicRequest request = new LoreComicRequest(LoreComicEvent.ShopInGameLastNoPurchase, activeZone);
        yield return PlayIfAvailable(request);
    }

    private static IEnumerator PlayIfAvailable(LoreComicRequest request)
    {
        LoreComicPresenter presenter = FindActivePresenter();
        if (presenter == null)
        {
            yield break;
        }

        bool completed = false;
        if (!presenter.TryPlay(request, () => completed = true))
        {
            yield break;
        }

        while (!completed)
        {
            yield return null;
        }
    }

    private static LoreComicPresenter FindActivePresenter()
    {
        LoreComicPresenter[] presenters = Resources.FindObjectsOfTypeAll<LoreComicPresenter>();
        for (int i = 0; i < presenters.Length; i++)
        {
            LoreComicPresenter presenter = presenters[i];
            if (presenter == null || !presenter.isActiveAndEnabled)
            {
                continue;
            }

            Scene scene = presenter.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                return presenter;
            }
        }

        return null;
    }

    private IEnumerator RunPresentation(LoreComicRequest request, LoreComicEntry entry)
    {
        continueRequested = false;

        Sprite selectedSprite = SelectSprite(entry);
        float displaySeconds = ResolveDisplaySeconds(entry, request);
        bool waitForContinue = ResolveWaitForContinue(entry, request);
        bool showContinue = ResolveShowContinueButton(entry, request);
        bool canWaitForContinue = waitForContinue && (continueButton != null || allowExternalContinueWithoutButton);

        if (waitForContinue && !canWaitForContinue)
        {
            Debug.LogWarning("[LoreComicPresenter] El comic espera confirmacion, pero no hay ContinueButton asignado. Se cerrara al terminar la duracion.", this);
        }

        ApplyTimeScaleIfNeeded();
        ApplySprite(selectedSprite);
        SetVisible(true, showContinue && canWaitForContinue);

        if (displaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
        }

        if (canWaitForContinue)
        {
            SetContinueVisible(showContinue);
            while (!continueRequested)
            {
                yield return null;
            }
        }

        HideImmediate();
        RestoreTimeScaleIfNeeded();

        activeRoutine = null;

        Action completion = activeCompletion;
        activeCompletion = null;
        completion?.Invoke();
    }

    private LoreComicEntry FindEntry(LoreComicRequest request)
    {
        if (entries == null)
        {
            return null;
        }

        LoreComicEntry fallback = null;
        for (int i = 0; i < entries.Length; i++)
        {
            LoreComicEntry entry = entries[i];
            if (entry == null || entry.comicEvent != request.ComicEvent)
            {
                continue;
            }

            if (entry.zone == request.Zone)
            {
                return entry;
            }

            if (entry.zone == LoreComicZone.Unknown)
            {
                fallback = entry;
            }
        }

        return fallback;
    }

    private Sprite SelectSprite(LoreComicEntry entry)
    {
        if (entry?.sprites == null || entry.sprites.Length == 0)
        {
            return null;
        }

        int availableCount = 0;
        for (int i = 0; i < entry.sprites.Length; i++)
        {
            if (entry.sprites[i] != null)
            {
                availableCount++;
            }
        }

        if (availableCount == 0)
        {
            return null;
        }

        int selectedIndex = UnityEngine.Random.Range(0, availableCount);
        for (int i = 0; i < entry.sprites.Length; i++)
        {
            Sprite sprite = entry.sprites[i];
            if (sprite == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return sprite;
            }

            selectedIndex--;
        }

        return null;
    }

    private static bool HasDisplayableSprite(LoreComicEntry entry)
    {
        if (entry?.sprites == null)
        {
            return false;
        }

        for (int i = 0; i < entry.sprites.Length; i++)
        {
            if (entry.sprites[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private float ResolveDisplaySeconds(LoreComicEntry entry, LoreComicRequest request)
    {
        if (entry != null)
        {
            return Mathf.Max(0f, entry.displaySeconds);
        }

        return request.ComicEvent == LoreComicEvent.GameStart
            ? Mathf.Max(0f, defaultStartDisplaySeconds)
            : Mathf.Max(0f, defaultDisplaySeconds);
    }

    private bool ResolveWaitForContinue(LoreComicEntry entry, LoreComicRequest request)
    {
        if (entry != null)
        {
            return entry.waitForContinue;
        }

        return request.ComicEvent == LoreComicEvent.GameStart && defaultStartWaitsForContinue;
    }

    private bool ResolveShowContinueButton(LoreComicEntry entry, LoreComicRequest request)
    {
        if (entry != null)
        {
            return entry.showContinueButton;
        }

        return request.ComicEvent == LoreComicEvent.GameStart && defaultStartShowsContinueButton;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (comicImage == null)
        {
            return;
        }

        comicImage.sprite = sprite;
        comicImage.enabled = sprite != null;
    }

    private void ApplyTimeScaleIfNeeded()
    {
        if (!pauseTimeWhileShowing || pauseApplied)
        {
            return;
        }

        timeScaleBeforePresentation = Time.timeScale;
        Time.timeScale = 0f;
        pauseApplied = true;
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!pauseApplied)
        {
            return;
        }

        pauseApplied = false;
        Time.timeScale = timeScaleBeforePresentation;
    }

    private void SetVisible(bool visible, bool showContinue)
    {
        if (visible)
        {
            NormalizeRenderableScale();
        }

        if (comicRoot != null && visible && !comicRoot.activeSelf)
        {
            comicRoot.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        SetContinueVisible(showContinue);

        if (comicRoot != null && !visible && comicRoot != gameObject)
        {
            comicRoot.SetActive(false);
        }
    }

    private void SetContinueVisible(bool visible)
    {
        if (continueButtonRoot != null)
        {
            continueButtonRoot.SetActive(visible);
        }

        if (continueButton != null)
        {
            continueButton.interactable = visible;
        }
    }

    private void HideImmediate()
    {
        SetVisible(false, false);
        continueRequested = false;
    }

    private void NormalizeRenderableScale()
    {
        Transform rootTransform = comicRoot != null ? comicRoot.transform : transform;
        Canvas ownerCanvas = rootTransform != null
            ? rootTransform.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);

        RestoreScaleIfCollapsed(transform);
        RestoreScaleIfCollapsed(ownerCanvas != null ? ownerCanvas.transform : null);
        RestoreScaleIfCollapsed(rootTransform);
        NormalizeCanvasLayer(ownerCanvas);
    }

    private static void NormalizeCanvasLayer(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, MinimumComicSortingOrder);
    }

    private static void RestoreScaleIfCollapsed(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 localScale = target.localScale;
        if (Mathf.Approximately(localScale.x, 0f)
            || Mathf.Approximately(localScale.y, 0f)
            || Mathf.Approximately(localScale.z, 0f))
        {
            target.localScale = Vector3.one;
        }
    }

    private void ResolveReferences()
    {
        if (comicRoot == null)
        {
            Transform comicTransform = FindChildTransform(transform, "Comic", "LoreComic", "ComicCanvas");
            if (comicTransform != null)
            {
                comicRoot = comicTransform.gameObject;
            }
            else if (TryGetComponent(out Canvas _) || TryGetComponent(out CanvasGroup _) || TryGetComponent(out Image _))
            {
                comicRoot = gameObject;
            }
        }

        if (comicRoot == null)
        {
            return;
        }

        canvasGroup ??= comicRoot.GetComponent<CanvasGroup>();
        comicImage ??= FindChildComponent<Image>(comicRoot.transform, "Vineta", "Vignette", "ComicImage", "ComicPanel");
        continueButton ??= UiButtonContract.FindButton(comicRoot.transform, "ContinuarBoton", "PlayBoton", "JugarBoton", "ContinueButton");
        continueButtonRoot ??= continueButton != null ? continueButton.gameObject : null;
    }

    private void WireContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.onClick.RemoveListener(Continue);
        continueButton.onClick.AddListener(Continue);
    }

    private Transform FindChildTransform(Transform root, params string[] names)
    {
        if (root == null || names == null || names.Length == 0)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (children[i].name == names[nameIndex])
                {
                    return children[i];
                }
            }
        }

        return null;
    }

    private T FindChildComponent<T>(Transform root, params string[] names) where T : Component
    {
        Transform child = FindChildTransform(root, names);
        return child != null && child.TryGetComponent(out T component) ? component : null;
    }
}

public static class LoreComicZoneResolver
{
    public static LoreComicZone ResolveFromScene(Scene scene)
    {
        if (!string.IsNullOrWhiteSpace(scene.path))
        {
            return ResolveFromSceneName(scene.path);
        }

        return ResolveFromSceneName(scene.name);
    }

    public static LoreComicZone ResolveFromSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return LoreComicZone.Unknown;
        }

        string normalized = sceneName.Replace("\\", "/").ToLowerInvariant();
        if (normalized.Contains("abisopelagica"))
        {
            return LoreComicZone.Abyssopelagic;
        }

        if (normalized.Contains("epipelagica"))
        {
            return LoreComicZone.Epipelagic;
        }

        if (normalized.Contains("tutorial"))
        {
            return LoreComicZone.Tutorial;
        }

        return LoreComicZone.Unknown;
    }

    public static LoreComicEvent ResolvePortalEvent(LoreComicZone fromZone, LoreComicZone toZone)
    {
        if (fromZone == LoreComicZone.Epipelagic && toZone == LoreComicZone.Abyssopelagic)
        {
            return LoreComicEvent.PortalEpipelagicToAbyssopelagic;
        }

        if (fromZone == LoreComicZone.Abyssopelagic && toZone == LoreComicZone.Epipelagic)
        {
            return LoreComicEvent.PortalAbyssopelagicToEpipelagic;
        }

        return LoreComicEvent.None;
    }
}
