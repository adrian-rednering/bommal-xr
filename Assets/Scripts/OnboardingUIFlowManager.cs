using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class OnboardingUIFlowManager : MonoBehaviour
{
    private const string PracticeUiRootName = "Practice_UI_Root";

    [Header("Root")]
    [SerializeField] private Transform onboardingUiRoot;
    [SerializeField] private Transform practiceUiRoot;
    [SerializeField] private PracticeUIFlowManager practiceFlowManager;

    [Header("Onboarding Objects")]
    [SerializeField] private GameObject onboardingPanel;
    [SerializeField] private GameObject namespaceButtonObject;
    [SerializeField] private GameObject namePlaceholderObject;
    [SerializeField] private GameObject namespaceCompleteObject;
    [SerializeField] private GameObject startButtonObject;
    [SerializeField] private Button namespaceButton;
    [SerializeField] private Button namePlaceholderButton;
    [SerializeField] private Button startButton;

    [Header("Name Input")]
    [SerializeField] private InputField nameInputField;
    [SerializeField] private Text nameInputText;
    [SerializeField] private Text confirmedNameText;
    [SerializeField] private string defaultName = "\uB3C4\uC6B0";
    [SerializeField] private int maxNameLength = 8;
    [SerializeField] private bool hidePlaceholderWhileEditing = true;
    [SerializeField] private bool autoFocusKeyboard = true;
    [SerializeField] private int keyboardOpenRetryCount = 3;
    [SerializeField] private float keyboardOpenRetryDelay = 0.25f;

    [Header("Fallback UI")]
    [SerializeField] private bool createFallbackInputIfMissing = true;
    [SerializeField] private bool createFallbackStartButtonIfMissing = true;

    private TouchScreenKeyboard keyboard;
    private Coroutine keyboardOpenCoroutine;
    private bool isEditingName;
    private bool nameConfirmed;
    private string currentName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForOnboardingScene()
    {
        if (FindFirstObjectByType<OnboardingUIFlowManager>() != null)
        {
            return;
        }

        var root = FindInActiveSceneByNames(
            "Onboarding_UI_Root",
            "Onboarding UI Root",
            "ONboarding_UI_Root",
            "ONboarding UI Root"
        );
        if (root == null)
        {
            return;
        }

        var managerObject = new GameObject("Onboarding_UI_FlowManager");
        var manager = managerObject.AddComponent<OnboardingUIFlowManager>();
        manager.onboardingUiRoot = root;
    }

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        BindInputField();
        ShowOnboarding();
    }

    private void Start()
    {
        ShowOnboarding();
    }

    private void Update()
    {
        UpdateKeyboardState();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        UnbindInputField();
    }

    public void ShowOnboarding()
    {
        currentName = string.Empty;
        isEditingName = false;
        nameConfirmed = false;

        SetActive(onboardingUiRoot, true);
        SetActive(practiceUiRoot, false);
        SetActive(onboardingPanel, true);
        SetActive(namespaceCompleteObject, false);
        SetActive(startButtonObject, false);
        SetActive(namePlaceholderObject, true);

        if (nameInputField != null)
        {
            nameInputField.text = string.Empty;
            nameInputField.gameObject.SetActive(false);
        }

        SetConfirmedNameText(defaultName);
    }

    public void BeginNameInput()
    {
        if (nameConfirmed)
        {
            return;
        }

        isEditingName = true;
        StopKeyboardOpenCoroutine();
        SetActive(namePlaceholderObject, !hidePlaceholderWhileEditing);

        if (nameInputField == null)
        {
            return;
        }

        nameInputField.gameObject.SetActive(true);
        nameInputField.interactable = true;
        nameInputField.text = currentName;
        FocusNameInputField();

        if (autoFocusKeyboard)
        {
            keyboardOpenCoroutine = StartCoroutine(OpenKeyboardWithRetries());
        }
    }

    public void ConfirmNameInput()
    {
        var value = nameInputField != null ? nameInputField.text : currentName;
        currentName = SanitizeName(value);
        if (string.IsNullOrWhiteSpace(currentName))
        {
            currentName = defaultName;
        }

        isEditingName = false;
        nameConfirmed = true;

        if (nameInputField != null)
        {
            nameInputField.DeactivateInputField();
            nameInputField.gameObject.SetActive(false);
        }

        keyboard = null;
        StopKeyboardOpenCoroutine();
        SetActive(namePlaceholderObject, false);
        SetActive(namespaceCompleteObject, true);
        SetActive(startButtonObject, true);
        SetConfirmedNameText(currentName);
    }

    public void StartPractice()
    {
        SetActive(onboardingUiRoot, false);
        SetActive(practiceUiRoot, true);

        if (practiceFlowManager == null)
        {
            practiceFlowManager = FindFirstObjectByType<PracticeUIFlowManager>();
        }

        if (practiceFlowManager != null)
        {
            practiceFlowManager.RestartFlow();
        }
    }

    private void ResolveReferences()
    {
        onboardingUiRoot = onboardingUiRoot != null
            ? onboardingUiRoot
            : FindInActiveSceneByNames(
                "Onboarding_UI_Root",
                "Onboarding UI Root",
                "ONboarding_UI_Root",
                "ONboarding UI Root"
            );

        if (onboardingUiRoot == null)
        {
            Debug.LogWarning("OnboardingUIFlowManager: onboarding root was not found.");
            return;
        }

        practiceUiRoot = practiceUiRoot != null
            ? practiceUiRoot
            : FindInActiveSceneByNames(PracticeUiRootName);
        practiceFlowManager = practiceFlowManager != null
            ? practiceFlowManager
            : FindFirstObjectByType<PracticeUIFlowManager>();
        onboardingPanel = onboardingPanel != null
            ? onboardingPanel
            : FindChildGameObject(
                onboardingUiRoot,
                "OnboardingPanel",
                "Onboarding_Panel",
                "StartInstructionPanel"
            );

        namespaceButtonObject = namespaceButtonObject != null
            ? namespaceButtonObject
            : FindChildGameObject(
                onboardingUiRoot,
                "Namespace",
                "NamespaceButton",
                "NameSpace",
                "NameButton",
                "NameInput",
                "NameInputButton"
            );
        if (namespaceButtonObject == null && createFallbackInputIfMissing)
        {
            namespaceButtonObject = CreateFallbackNamespaceButtonObject();
        }

        namePlaceholderObject = namePlaceholderObject != null
            ? namePlaceholderObject
            : FindChildGameObject(
                onboardingUiRoot,
                "[name]",
                "[Name]",
                "NamePlaceholder",
                "NamespacePlaceholder"
            );
        if (namePlaceholderObject == null && namespaceButtonObject != null && createFallbackInputIfMissing)
        {
            namePlaceholderObject = CreateNamePlaceholder(namespaceButtonObject.transform);
        }

        namespaceCompleteObject = namespaceCompleteObject != null
            ? namespaceCompleteObject
            : FindChildGameObject(
                onboardingUiRoot,
                "NamespaceComplete",
                "NameSpaceComplete",
                "NamespaceCompelete",
                "NameSpaceCompelete",
                "Namespace_Complete",
                "NameComplete",
                "NameConfirmed"
            );
        if (namespaceCompleteObject == null && createFallbackInputIfMissing)
        {
            namespaceCompleteObject = CreateFallbackNamespaceCompleteObject();
        }

        startButtonObject = startButtonObject != null
            ? startButtonObject
            : FindChildGameObject(
                onboardingUiRoot,
                "StartButton",
                "Start Button",
                "OnboardingStartButton",
                "Start"
            );

        namespaceButton = namespaceButton != null
            ? namespaceButton
            : GetOrAddButton(namespaceButtonObject);
        namePlaceholderButton = namePlaceholderButton != null
            ? namePlaceholderButton
            : GetOrAddButton(namePlaceholderObject);
        startButton = startButton != null ? startButton : GetOrAddButton(startButtonObject);

        nameInputField = nameInputField != null
            ? nameInputField
            : onboardingUiRoot.GetComponentInChildren<InputField>(true);

        if (nameInputField == null && createFallbackInputIfMissing)
        {
            nameInputField = CreateNameInputField();
        }

        if (startButtonObject == null && createFallbackStartButtonIfMissing)
        {
            startButton = CreateFallbackStartButton();
            startButtonObject = startButton.gameObject;
        }

        if (nameInputField != null)
        {
            nameInputText = nameInputText != null ? nameInputText : nameInputField.textComponent;
        }

        confirmedNameText = confirmedNameText != null
            ? confirmedNameText
            : FindText(namespaceCompleteObject, "ConfirmedNameText", "NameText", "UserNameText");
    }

    private void BindButtons()
    {
        if (namespaceButton != null)
        {
            namespaceButton.onClick.RemoveListener(BeginNameInput);
            namespaceButton.onClick.AddListener(BeginNameInput);
        }

        if (namePlaceholderButton != null)
        {
            namePlaceholderButton.onClick.RemoveListener(BeginNameInput);
            namePlaceholderButton.onClick.AddListener(BeginNameInput);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartPractice);
            startButton.onClick.AddListener(StartPractice);
        }
    }

    private void BindInputField()
    {
        if (nameInputField == null)
        {
            return;
        }

        nameInputField.characterLimit = maxNameLength;
        nameInputField.onEndEdit.RemoveListener(OnNameInputEndEdit);
        nameInputField.onEndEdit.AddListener(OnNameInputEndEdit);
    }

    private void UnbindButtons()
    {
        if (namespaceButton != null)
        {
            namespaceButton.onClick.RemoveListener(BeginNameInput);
        }

        if (namePlaceholderButton != null)
        {
            namePlaceholderButton.onClick.RemoveListener(BeginNameInput);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartPractice);
        }
    }

    private void UnbindInputField()
    {
        if (nameInputField != null)
        {
            nameInputField.onEndEdit.RemoveListener(OnNameInputEndEdit);
        }
    }

    private void OnNameInputEndEdit(string value)
    {
        currentName = SanitizeName(value);

        if (isEditingName && keyboardOpenCoroutine != null)
        {
            return;
        }

        if (ShouldWaitForKeyboardDone())
        {
            return;
        }

        if (isEditingName && autoFocusKeyboard && keyboard == null && Application.isMobilePlatform)
        {
            return;
        }

        ConfirmNameInput();
    }

    private void UpdateKeyboardState()
    {
        if (!isEditingName || keyboard == null)
        {
            return;
        }

        if (nameInputField != null && nameInputField.text != keyboard.text)
        {
            nameInputField.text = keyboard.text;
        }

        if (keyboard.status == TouchScreenKeyboard.Status.Done)
        {
            ConfirmNameInput();
        }
        else if (keyboard.status == TouchScreenKeyboard.Status.Canceled)
        {
            CancelNameInput();
        }
    }

    private bool ShouldWaitForKeyboardDone()
    {
        return keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible;
    }

    private IEnumerator OpenKeyboardWithRetries()
    {
        yield return null;

        for (var attempt = 0; attempt <= keyboardOpenRetryCount; attempt++)
        {
            FocusNameInputField();
            OpenTouchKeyboard();

            if (keyboard != null)
            {
                keyboardOpenCoroutine = null;
                yield break;
            }

            if (keyboardOpenRetryDelay > 0.0f)
            {
                yield return new WaitForSecondsRealtime(keyboardOpenRetryDelay);
            }
            else
            {
                yield return null;
            }
        }

        Debug.LogWarning("OnboardingUIFlowManager: TouchScreenKeyboard.Open returned null.");
        keyboardOpenCoroutine = null;
    }

    private void FocusNameInputField()
    {
        if (nameInputField == null)
        {
            return;
        }

        nameInputField.ActivateInputField();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(nameInputField.gameObject);
        }
    }

    private void OpenTouchKeyboard()
    {
        TouchScreenKeyboard.hideInput = false;
        keyboard = TouchScreenKeyboard.Open(
            currentName,
            TouchScreenKeyboardType.Default,
            false,
            false,
            false,
            false,
            "\uC774\uB984 \uC785\uB825",
            maxNameLength
        );
    }

    private void StopKeyboardOpenCoroutine()
    {
        if (keyboardOpenCoroutine == null)
        {
            return;
        }

        StopCoroutine(keyboardOpenCoroutine);
        keyboardOpenCoroutine = null;
    }

    private void CancelNameInput()
    {
        isEditingName = false;
        keyboard = null;
        StopKeyboardOpenCoroutine();

        if (nameInputField != null)
        {
            nameInputField.DeactivateInputField();
            nameInputField.gameObject.SetActive(false);
        }

        SetActive(namePlaceholderObject, true);
    }

    private InputField CreateNameInputField()
    {
        var parent = namespaceButtonObject != null
            ? namespaceButtonObject.transform
            : (onboardingPanel != null ? onboardingPanel.transform : onboardingUiRoot);
        if (parent == null)
        {
            return null;
        }

        var inputObject = new GameObject("Runtime_NameInputField", typeof(RectTransform));
        inputObject.transform.SetParent(parent, false);

        var inputRect = inputObject.GetComponent<RectTransform>();
        ApplyNameEntryRect(inputRect, parent);
        inputRect.localScale = Vector3.one;

        var image = inputObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        image.raycastTarget = true;

        var inputField = inputObject.AddComponent<InputField>();
        inputField.targetGraphic = image;
        inputField.transition = Selectable.Transition.None;
        inputField.characterLimit = maxNameLength;

        var textObject = new GameObject("Runtime_NameText", typeof(RectTransform));
        textObject.transform.SetParent(inputObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        var text = textObject.AddComponent<Text>();
        text.font = GetBuiltInFont();
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        inputField.textComponent = text;
        nameInputText = text;
        inputObject.SetActive(false);
        return inputField;
    }

    private GameObject CreateFallbackNamespaceButtonObject()
    {
        var parent = onboardingPanel != null ? onboardingPanel.transform : onboardingUiRoot;
        if (parent == null)
        {
            return null;
        }

        var target = new GameObject("Runtime_NamespaceButton", typeof(RectTransform));
        target.transform.SetParent(parent, false);

        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(235f, 80f);
        rect.sizeDelta = new Vector2(260f, 96f);
        rect.localScale = Vector3.one;

        var image = target.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        image.raycastTarget = true;

        var button = target.AddComponent<Button>();
        button.targetGraphic = image;
        namespaceButton = button;
        return target;
    }

    private GameObject CreateNamePlaceholder(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        var textObject = new GameObject("Runtime_NamePlaceholder", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        ApplyNameEntryRect(rect, parent);

        var text = textObject.AddComponent<Text>();
        text.font = GetBuiltInFont();
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.88f, 1f, 0.15f, 1f);
        text.text = "[" + defaultName + "]";
        text.raycastTarget = false;
        return textObject;
    }

    private static void ApplyNameEntryRect(RectTransform rect, Transform parent)
    {
        if (rect == null)
        {
            return;
        }

        if (IsNameSpaceSprite(parent))
        {
            rect.anchorMin = new Vector2(0.56f, 0.12f);
            rect.anchorMax = new Vector2(0.97f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private GameObject CreateFallbackNamespaceCompleteObject()
    {
        var parent = onboardingPanel != null ? onboardingPanel.transform : onboardingUiRoot;
        if (parent == null)
        {
            return null;
        }

        var completeObject = new GameObject("Runtime_NamespaceComplete", typeof(RectTransform));
        completeObject.transform.SetParent(parent, false);

        var rect = completeObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(235f, 80f);
        rect.sizeDelta = new Vector2(260f, 96f);
        rect.localScale = Vector3.one;

        var image = completeObject.AddComponent<Image>();
        image.color = new Color(0.92f, 1f, 0.35f, 0.92f);
        image.raycastTarget = false;

        confirmedNameText = CreateConfirmedNameText(completeObject.transform);
        completeObject.SetActive(false);
        return completeObject;
    }

    private Button CreateFallbackStartButton()
    {
        var parent = onboardingPanel != null ? onboardingPanel.transform : onboardingUiRoot;
        var buttonObject = new GameObject("Runtime_StartButton", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -300f);
        rect.sizeDelta = new Vector2(240f, 88f);
        rect.localScale = Vector3.one;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.68f, 0.78f, 0.18f, 1f);
        image.raycastTarget = true;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var textObject = new GameObject("Runtime_StartText", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.AddComponent<Text>();
        text.font = GetBuiltInFont();
        text.fontSize = 34;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "\uC2DC\uC791\uD558\uAE30";

        return button;
    }

    private static Button GetOrAddButton(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        var button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.AddComponent<Button>();
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = target.GetComponent<Graphic>();
        }

        return button;
    }

    private void SetConfirmedNameText(string value)
    {
        if (confirmedNameText == null && namespaceCompleteObject != null)
        {
            confirmedNameText = CreateConfirmedNameText(namespaceCompleteObject.transform);
        }

        if (confirmedNameText != null)
        {
            confirmedNameText.text = "[" + value + "]\uB2D8";
        }
    }

    private Text CreateConfirmedNameText(Transform parent)
    {
        var textObject = new GameObject("Runtime_ConfirmedNameText", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = textObject.AddComponent<Text>();
        text.font = GetBuiltInFont();
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        return text;
    }

    private string SanitizeName(string value)
    {
        var sanitized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (maxNameLength > 0 && sanitized.Length > maxNameLength)
        {
            sanitized = sanitized.Substring(0, maxNameLength);
        }

        return sanitized;
    }

    private static void SetActive(Transform target, bool active)
    {
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static Text FindText(GameObject parent, params string[] names)
    {
        if (parent == null)
        {
            return null;
        }

        var texts = parent.GetComponentsInChildren<Text>(true);
        foreach (var name in names)
        {
            foreach (var text in texts)
            {
                if (NamesMatch(text.name, name))
                {
                    return text;
                }
            }
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private static GameObject FindChildGameObject(Transform root, params string[] names)
    {
        var child = FindChildRecursive(root, names);
        return child != null ? child.gameObject : null;
    }

    private static Transform FindInActiveSceneByNames(params string[] names)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var found = FindChildRecursive(root.transform, names);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, params string[] names)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (NamesMatch(parent.name, name) || SpriteNameMatches(parent, name))
            {
                return parent;
            }
        }

        for (var index = 0; index < parent.childCount; index++)
        {
            var found = FindChildRecursive(parent.GetChild(index), names);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool SpriteNameMatches(Transform target, string expected)
    {
        var image = target.GetComponent<Image>();
        if (image == null || image.sprite == null)
        {
            return false;
        }

        return NamesMatch(image.sprite.name, expected);
    }

    private static bool IsNameSpaceSprite(Transform target)
    {
        var image = target != null ? target.GetComponent<Image>() : null;
        return image != null
            && image.sprite != null
            && NamesMatch(image.sprite.name, "NameSpace");
    }

    private static bool NamesMatch(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var normalizedActual = NormalizeName(actual);
        var normalizedExpected = NormalizeName(expected);
        return normalizedActual == normalizedExpected
            || normalizedActual.StartsWith(normalizedExpected + "(", StringComparison.Ordinal);
    }

    private static string NormalizeName(string value)
    {
        return value
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    private static Font GetBuiltInFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
