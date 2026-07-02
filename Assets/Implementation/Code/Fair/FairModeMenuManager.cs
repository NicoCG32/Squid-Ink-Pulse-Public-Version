using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FairModeMenuManager : MonoBehaviour
{
    private static FairModeMenuManager instance;

    private Canvas canvas;
    private TMP_InputField nicknameInput;
    private TMP_InputField recoveryCodeInput;
    private TMP_InputField serverUrlInput;
    private TMP_Text statusText;
    private Button recoverButton;
    private Button createButton;
    private Button continueButton;
    private Button exitButton;
    private CanvasGroup canvasGroup;
    private bool isBusy;

    public static FairModeMenuManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject menuObject = new("FairModeMenu");
        instance = menuObject.AddComponent<FairModeMenuManager>();
        DontDestroyOnLoad(menuObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
        Hide();
    }

    public void ShowInitialLogin(bool serverVerified = true)
    {
        if (FairParticipantSession.HasActiveSession)
        {
            Hide();
            return;
        }

        BuildUi();
        SetStatus(serverVerified
            ? "Ingresa tu nick y codigo, o crea un jugador nuevo."
            : "No se pudo verificar el servidor. Revisa la URL antes de entrar.");
        SetBusy(false);
        continueButton.gameObject.SetActive(false);
        recoveryCodeInput.gameObject.SetActive(true);
        Show();
    }

    private void BuildUi()
    {
        if (canvas != null)
        {
            return;
        }

        EnsureEventSystem();

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        GameObject dimmer = CreateUiObject("Dimmer", transform);
        RectTransform dimmerRect = dimmer.AddComponent<RectTransform>();
        dimmerRect.anchorMin = Vector2.zero;
        dimmerRect.anchorMax = Vector2.one;
        dimmerRect.offsetMin = Vector2.zero;
        dimmerRect.offsetMax = Vector2.zero;
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject panel = CreateUiObject("Panel", transform);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 620f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.09f, 0.12f, 0.96f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(42, 42, 38, 38);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateText("Title", panel.transform, "Feria Squid Ink", 36, FontStyles.Bold);
        SetElementHeight(title.gameObject, 54f);

        statusText = CreateText("Status", panel.transform, string.Empty, 19, FontStyles.Normal);
        statusText.alignment = TextAlignmentOptions.Center;
        SetElementHeight(statusText.gameObject, 72f);

        nicknameInput = CreateInput(panel.transform, "Nick", "Nick");
        recoveryCodeInput = CreateInput(panel.transform, "Codigo", "Codigo de recuperacion");
        serverUrlInput = CreateInput(panel.transform, "Servidor", "Servidor");
        serverUrlInput.text = FairModeSettings.ServerBaseUrl;

        GameObject buttonRow = CreateUiObject("Buttons", panel.transform);
        HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 12f;
        buttonLayout.childControlWidth = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandHeight = true;
        SetElementHeight(buttonRow, 64f);

        recoverButton = CreateButton(buttonRow.transform, "Entrar", RecoverExistingParticipant);
        createButton = CreateButton(buttonRow.transform, "Nuevo jugador", CreateNewParticipant);
        exitButton = CreateButton(buttonRow.transform, "Salir", QuitApplication);
        continueButton = CreateButton(panel.transform, "Continuar", Hide);
        SetElementHeight(continueButton.gameObject, 64f);
        continueButton.gameObject.SetActive(false);
    }

    private void RecoverExistingParticipant()
    {
        if (isBusy)
        {
            return;
        }

        string nickname = nicknameInput.text.Trim();
        string recoveryCode = recoveryCodeInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(recoveryCode))
        {
            SetStatus("Nick y codigo son obligatorios.");
            return;
        }

        FairModeSettings.SaveServerBaseUrl(serverUrlInput.text);
        StartCoroutine(RecoverRoutine(nickname, recoveryCode));
    }

    private void CreateNewParticipant()
    {
        if (isBusy)
        {
            return;
        }

        string nickname = nicknameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            SetStatus("Escribe un nick para crear jugador.");
            return;
        }

        FairModeSettings.SaveServerBaseUrl(serverUrlInput.text);
        StartCoroutine(CreateRoutine(nickname));
    }

    private IEnumerator RecoverRoutine(string nickname, string recoveryCode)
    {
        SetBusy(true);
        SetStatus("Conectando con servidor...");
        FairParticipantSession session = FairParticipantSession.Instance;
        yield return session.RecoverParticipant(nickname, recoveryCode, result =>
        {
            if (result.Success)
            {
                SetStatus($"Bienvenido, {result.Value.nickname}.");
                StartCoroutine(HideAfterDelay());
            }
            else
            {
                SetStatus(FormatError(result));
                SetBusy(false);
            }
        });
    }

    private IEnumerator CreateRoutine(string nickname)
    {
        SetBusy(true);
        SetStatus("Creando jugador...");
        FairParticipantSession session = FairParticipantSession.Instance;
        yield return session.CreateNewParticipant(nickname, result =>
        {
            if (result.Success)
            {
                string code = result.Value.recoveryCode;
                recoveryCodeInput.text = code;
                SetStatus($"Jugador creado. Codigo: {code}. Guardalo para volver.");
                continueButton.gameObject.SetActive(true);
                SetBusy(false);
            }
            else
            {
                SetStatus(FormatError(result));
                SetBusy(false);
            }
        });
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        Hide();
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        recoverButton.interactable = !busy;
        createButton.interactable = !busy;
        exitButton.interactable = !busy;
        nicknameInput.interactable = !busy;
        recoveryCodeInput.interactable = !busy;
        serverUrlInput.interactable = !busy;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private static string FormatError<T>(FairApiResult<T> result)
    {
        if (result.ErrorCode == "active_session")
        {
            return "Este jugador ya esta activo en otro PC.";
        }

        if (result.ErrorCode == "participant_not_found")
        {
            return "Nick o codigo no encontrado.";
        }

        return string.IsNullOrWhiteSpace(result.Message)
            ? $"Error de servidor: {result.ErrorCode}"
            : result.Message;
    }

    private static TMP_Text CreateText( string name, Transform parent, string text, int size, FontStyles style)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static TMP_InputField CreateInput(Transform parent, string name, string placeholder)
    {
        GameObject root = CreateUiObject($"{name}Input", parent);
        Image background = root.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.95f);
        SetElementHeight(root, 58f);

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        TMP_Text text = CreateText("Text", root.transform, string.Empty, 23, FontStyles.Normal);
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        StretchWithPadding(text.rectTransform, 18f, 10f);

        TMP_Text placeholderText = CreateText("Placeholder", root.transform, placeholder, 21, FontStyles.Italic);
        placeholderText.color = new Color(0f, 0f, 0f, 0.45f);
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
        StretchWithPadding(placeholderText.rectTransform, 18f, 10f);

        input.textComponent = text;
        input.placeholder = placeholderText;
        input.targetGraphic = background;
        return input;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject root = CreateUiObject(label.Replace(" ", string.Empty), parent);
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.15f, 0.48f, 0.78f, 1f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        TMP_Text text = CreateText("Text", root.transform, label, 22, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 22f;
        StretchWithPadding(text.rectTransform, 8f, 4f);
        return button;
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void StretchWithPadding(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
    }

    private static void SetElementHeight(GameObject target, float height)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.minHeight = height;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject target = new(name);
        target.transform.SetParent(parent, false);
        return target;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
        DontDestroyOnLoad(eventSystem);
    }
}
