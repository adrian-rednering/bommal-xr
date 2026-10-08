using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-45)]
public sealed class PresentationUIFlowManager : MonoBehaviour
{
    private const string CanvasRootName = "Canvas";

    [Header("Root")]
    [SerializeField] private Transform uiRoot;

    [Header("Panels")]
    [SerializeField] private GameObject simulationPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject feedbackPanel;

    [Header("Simulation UI")]
    [SerializeField] private GameObject instructionObject;
    [SerializeField] private GameObject scriptUncheckedObject;
    [SerializeField] private GameObject scriptInstructionObject;
    [SerializeField] private GameObject scriptCheckedObject;

    [Header("Feedback UI")]
    [SerializeField] private Button againButton;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Image feedbackResultImage;

    [Header("Loading")]
    [SerializeField] private Image loadingFillImage;
    [SerializeField] private Transform loadingSpinner;
    [SerializeField] private float loadingDuration = 1.2f;
    [SerializeField] private float loadingSpinSpeed = 220f;

    [Header("Voice")]
    [SerializeField] private QuestVoiceEvaluationDemo voiceEvaluation;
    [SerializeField] private string targetScriptText = "문화";
    [SerializeField] private float recordingSecondsBeforeAnalyze = 5f;
    [SerializeField] private float recordingStartTimeout = 4f;
    [SerializeField] private int correctScoreThreshold = 70;

    [Header("Demo Flow")]
    [SerializeField] private bool useDemoRecordingLengthFlow = true;
    [SerializeField] private float demoSpeechSecondsBeforeSuccess = 2.8f;
    [SerializeField] private bool forceSuccessOnAttempt = true;
    [SerializeField] private int forcedSuccessAttemptNumber = 2;
    [SerializeField] private string demoSuccessFeedback = "\uBC1C\uD654 \uAE38\uC774\uAC00 \uD655\uC778\uB418\uC5B4 \uCCB4\uD06C \uC644\uB8CC\uB85C \uCC98\uB9AC\uD588\uC2B5\uB2C8\uB2E4.";
    [SerializeField] private string demoRetryFeedback = "\uCCAB \uBC88\uC9F8 \uC2DC\uB3C4\uB294 \uC5F0\uC2B5\uC73C\uB85C \uCC98\uB9AC\uD588\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uD55C \uBC88 \uBC1C\uD654\uD574 \uBCF4\uC138\uC694.";

    [Header("Mic Debug")]
    [SerializeField] private bool showMicDebugOverlay = true;
    [SerializeField] private Text micDebugText;
    [SerializeField] private string micDebugTextObjectName = "MIC_DebugText";
    [SerializeField] private int micDebugMaxChars = 120;

    [Header("Timing")]
    [SerializeField] private float instructionVisibleSeconds = 1.1f;
    [SerializeField] private float instructionFadeSeconds = 0.45f;
    [SerializeField] private float scriptInstructionVisibleSeconds = 2f;
    [SerializeField] private float scriptInstructionFadeSeconds = 0.6f;
    [SerializeField] private float checkedHoldSeconds = 0.7f;

    [Header("Options")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool autoRecord = true;
    [SerializeField] private bool showFeedbackAfterWrong = true;

    private Coroutine flowRoutine;
    private Coroutine recordingRoutine;
    private Coroutine loadingRoutine;
    private bool waitingForVoiceResult;
    private string pendingFeedback;
    private int voiceAttemptCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForPresentationScene()
    {
        if (FindFirstObjectByType<PresentationUIFlowManager>() != null)
        {
            return;
        }

        var simulationPanel = FindInActiveScene("SimulationPanel");
        if (simulationPanel == null)
        {
            return;
        }

        var managerObject = new GameObject("Presentation_UI_FlowManager");
        var manager = managerObject.AddComponent<PresentationUIFlowManager>();
        manager.simulationPanel = simulationPanel.gameObject;
        manager.uiRoot = simulationPanel.parent;
    }

    private void Awake()
    {
        ResolveReferences();
        ResolveMicDebugOverlay();
        BindAgainButton();
        BindVoiceEvaluation();
    }

    private void Start()
    {
        if (autoStart)
        {
            RestartSimulation();
        }
    }

    private void OnDestroy()
    {
        UnbindAgainButton();
        UnbindVoiceEvaluation();
    }

    public void RestartSimulation()
    {
        StopCurrentFlow();
        waitingForVoiceResult = false;
        pendingFeedback = string.Empty;

        ShowOnly(simulationPanel);
        SetVisible(instructionObject, true, 1f);
        SetVisible(scriptUncheckedObject, false, 1f);
        SetVisible(scriptInstructionObject, false, 1f);
        SetVisible(scriptCheckedObject, false, 1f);
        SetFeedbackText(string.Empty);
        SetMicDebugText("MIC raw: waiting");

        flowRoutine = StartCoroutine(RunSimulationIntro());
    }

    public void SimulateCorrect()
    {
        CompleteVoiceResult(true, "테스트 정답 처리");
    }

    public void SimulateWrong()
    {
        CompleteVoiceResult(false, "다시 한 번 천천히 말해 보세요.");
    }

    private IEnumerator RunSimulationIntro()
    {
        yield return new WaitForSeconds(instructionVisibleSeconds);

        SetVisible(scriptUncheckedObject, true, 1f);
        SetVisible(scriptInstructionObject, true, 1f);

        yield return FadeOut(instructionObject, instructionFadeSeconds);
        yield return new WaitForSeconds(scriptInstructionVisibleSeconds);
        yield return FadeOut(scriptInstructionObject, scriptInstructionFadeSeconds);

        if (autoRecord)
        {
            StartVoiceRecording();
        }

        flowRoutine = null;
    }

    private void StartVoiceRecording()
    {
        if (voiceEvaluation == null)
        {
            CompleteVoiceResult(false, "VoiceEvaluationManager가 씬에 없습니다.");
            return;
        }

        waitingForVoiceResult = true;
        voiceEvaluation.SetTargetText(targetScriptText);
        SetMicDebugText("MIC raw: recording...");
        voiceEvaluation.StartRecording();

        if (recordingRoutine != null)
        {
            StopCoroutine(recordingRoutine);
        }

        recordingRoutine = StartCoroutine(StopRecordingAfterDelay());
    }

    private IEnumerator StopRecordingAfterDelay()
    {
        var startDeadline = Time.realtimeSinceStartup + recordingStartTimeout;
        while (
            voiceEvaluation != null
            && !voiceEvaluation.IsRecording
            && waitingForVoiceResult
            && Time.realtimeSinceStartup < startDeadline
        )
        {
            yield return null;
        }

        if (voiceEvaluation == null || !waitingForVoiceResult)
        {
            recordingRoutine = null;
            yield break;
        }

        if (!voiceEvaluation.IsRecording)
        {
            recordingRoutine = null;
            CompleteVoiceResult(false, "마이크 녹음이 시작되지 않았습니다.");
            yield break;
        }

        var waitSeconds = useDemoRecordingLengthFlow
            ? demoSpeechSecondsBeforeSuccess
            : recordingSecondsBeforeAnalyze;
        yield return new WaitForSeconds(Mathf.Max(0.1f, waitSeconds));

        if (useDemoRecordingLengthFlow)
        {
            if (voiceEvaluation != null && voiceEvaluation.IsRecording)
            {
                voiceEvaluation.StopRecordingWithoutAnalyze();
            }

            SetMicDebugText("MIC raw: demo speech length accepted");
            recordingRoutine = null;
            CompleteVoiceResult(false, demoRetryFeedback);
            yield break;
        }

        if (voiceEvaluation != null && voiceEvaluation.IsRecording)
        {
            voiceEvaluation.StopRecordingAndAnalyze();
        }

        recordingRoutine = null;
    }

    private void OnVoiceResponseJson(string responseJson)
    {
        if (!waitingForVoiceResult)
        {
            return;
        }

        VoiceEvaluationResponse response = null;
        try
        {
            response = JsonUtility.FromJson<VoiceEvaluationResponse>(responseJson);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning("PresentationUIFlowManager: voice response parse failed. " + exception.Message);
        }

        SetMicDebugText(FormatSttDebugTranscript(response != null ? response.transcript : null));
        CompleteVoiceResult(IsResponseCorrect(response), BuildFeedback(response));
    }

    private void OnVoiceError(string message)
    {
        if (!waitingForVoiceResult)
        {
            return;
        }

        SetMicDebugText("MIC/STT error: " + TrimForMicDebug(message));
        CompleteVoiceResult(false, message);
    }

    private void CompleteVoiceResult(bool isCorrect, string feedback)
    {
        waitingForVoiceResult = false;
        voiceAttemptCount += 1;
        if (forceSuccessOnAttempt && voiceAttemptCount >= Mathf.Max(1, forcedSuccessAttemptNumber))
        {
            isCorrect = true;
            feedback = demoSuccessFeedback;
        }

        pendingFeedback = string.IsNullOrWhiteSpace(feedback) ? string.Empty : feedback;

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
            loadingRoutine = null;
        }

        flowRoutine = StartCoroutine(CompleteVoiceResultRoutine(isCorrect));
    }

    private IEnumerator CompleteVoiceResultRoutine(bool isCorrect)
    {
        ShowOnly(simulationPanel);
        SetVisible(instructionObject, false, 1f);
        SetVisible(scriptInstructionObject, false, 1f);
        SetVisible(scriptUncheckedObject, !isCorrect, 1f);
        SetVisible(scriptCheckedObject, isCorrect, 1f);

        if (!isCorrect && !showFeedbackAfterWrong)
        {
            yield return new WaitForSeconds(checkedHoldSeconds);
            RestartSimulation();
            yield break;
        }

        yield return new WaitForSeconds(checkedHoldSeconds);
        ShowLoadingPanel();
        yield return RunLoading();
        ShowFeedbackPanel();

        flowRoutine = null;
    }

    private void ShowLoadingPanel()
    {
        ShowOnly(loadingPanel);
        if (loadingFillImage != null)
        {
            loadingFillImage.fillAmount = 0f;
        }
    }

    private IEnumerator RunLoading()
    {
        var elapsed = 0f;
        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, loadingDuration));

            if (loadingFillImage != null)
            {
                loadingFillImage.fillAmount = t;
            }

            if (loadingSpinner != null)
            {
                loadingSpinner.Rotate(0f, 0f, -loadingSpinSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    private void ShowFeedbackPanel()
    {
        ShowOnly(feedbackPanel);
        SetFeedbackText(pendingFeedback);
        if (feedbackResultImage != null)
        {
            feedbackResultImage.enabled = feedbackResultImage.sprite != null;
        }
    }

    private bool IsResponseCorrect(VoiceEvaluationResponse response)
    {
        if (response == null)
        {
            return false;
        }

        if (
            response.requiresRetry
            || string.Equals(response.analysisStatus, "UNDETERMINED", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        if (response.wordResults != null && response.wordResults.Length > 0)
        {
            foreach (var wordResult in response.wordResults)
            {
                if (wordResult == null || string.IsNullOrWhiteSpace(wordResult.status))
                {
                    return false;
                }

                if (!string.Equals(wordResult.status, "CORRECT", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        return response.score != null
            && (
                response.score.overallScore >= correctScoreThreshold
                || response.score.accuracyScore >= correctScoreThreshold
            );
    }

    private static string BuildFeedback(VoiceEvaluationResponse response)
    {
        if (response == null)
        {
            return string.Empty;
        }

        if (response.feedback != null && !string.IsNullOrWhiteSpace(response.feedback.summary))
        {
            return response.feedback.summary;
        }

        if (!string.IsNullOrWhiteSpace(response.retryReason))
        {
            return response.retryReason;
        }

        if (!string.IsNullOrWhiteSpace(response.transcript))
        {
            return "인식 결과: " + response.transcript;
        }

        return string.Empty;
    }

    private void ResolveReferences()
    {
        if (uiRoot == null)
        {
            var canvasRoot = FindInActiveScene(CanvasRootName);
            uiRoot = canvasRoot != null ? canvasRoot : FindInActiveScene("Practice_UI_Root");
        }

        simulationPanel = simulationPanel != null ? simulationPanel : FindChildGameObject("SimulationPanel");
        loadingPanel = loadingPanel != null ? loadingPanel : FindChildGameObject("LoadingPanel", false);
        loadingPanel = loadingPanel != null ? loadingPanel : FindChildGameObject("LodaingPanel", false);
        feedbackPanel = feedbackPanel != null ? feedbackPanel : FindChildGameObject("FeedbackPanel");

        instructionObject = instructionObject != null
            ? instructionObject
            : FindChildGameObject(simulationPanel, "Instruction", false);
        instructionObject = instructionObject != null
            ? instructionObject
            : FindChildGameObject(simulationPanel, "Insturction", false);

        scriptUncheckedObject = scriptUncheckedObject != null
            ? scriptUncheckedObject
            : FindChildGameObject(simulationPanel, "ScriptUnchecked", false);
        scriptInstructionObject = scriptInstructionObject != null
            ? scriptInstructionObject
            : FindChildGameObject(simulationPanel, "ScriptInstruction", false);
        scriptInstructionObject = scriptInstructionObject != null
            ? scriptInstructionObject
            : FindChildGameObject(simulationPanel, "ScriptInsturction", false);
        scriptCheckedObject = scriptCheckedObject != null
            ? scriptCheckedObject
            : FindChildGameObject(simulationPanel, "ScriptChecked", false);

        againButton = againButton != null ? againButton : FindAgainButton();
        feedbackText = feedbackText != null ? feedbackText : FindText(feedbackPanel, "FeedbackText");
        feedbackResultImage = feedbackResultImage != null ? feedbackResultImage : FindFeedbackResultImage();
        loadingFillImage = loadingFillImage != null ? loadingFillImage : FindFilledImage(loadingPanel);
        loadingSpinner = loadingSpinner != null ? loadingSpinner : FindLoadingSpinner(loadingPanel);
        voiceEvaluation = voiceEvaluation != null ? voiceEvaluation : FindFirstObjectByType<QuestVoiceEvaluationDemo>();
    }

    private void BindAgainButton()
    {
        if (againButton == null)
        {
            return;
        }

        againButton.onClick.RemoveListener(RestartSimulation);
        againButton.onClick.AddListener(RestartSimulation);
    }

    private void UnbindAgainButton()
    {
        if (againButton != null)
        {
            againButton.onClick.RemoveListener(RestartSimulation);
        }
    }

    private void BindVoiceEvaluation()
    {
        if (voiceEvaluation == null)
        {
            return;
        }

        if (voiceEvaluation.onResponseJson == null)
        {
            voiceEvaluation.onResponseJson = new UnityEvent<string>();
        }

        if (voiceEvaluation.onError == null)
        {
            voiceEvaluation.onError = new UnityEvent<string>();
        }

        if (voiceEvaluation.onMicInputDebug == null)
        {
            voiceEvaluation.onMicInputDebug = new UnityEvent<string>();
        }

        voiceEvaluation.onResponseJson.RemoveListener(OnVoiceResponseJson);
        voiceEvaluation.onError.RemoveListener(OnVoiceError);
        voiceEvaluation.onMicInputDebug.RemoveListener(OnMicInputDebug);
        voiceEvaluation.onResponseJson.AddListener(OnVoiceResponseJson);
        voiceEvaluation.onError.AddListener(OnVoiceError);
        voiceEvaluation.onMicInputDebug.AddListener(OnMicInputDebug);
    }

    private void UnbindVoiceEvaluation()
    {
        if (voiceEvaluation == null)
        {
            return;
        }

        if (voiceEvaluation.onResponseJson != null)
        {
            voiceEvaluation.onResponseJson.RemoveListener(OnVoiceResponseJson);
        }

        if (voiceEvaluation.onError != null)
        {
            voiceEvaluation.onError.RemoveListener(OnVoiceError);
        }

        if (voiceEvaluation.onMicInputDebug != null)
        {
            voiceEvaluation.onMicInputDebug.RemoveListener(OnMicInputDebug);
        }
    }

    private void ShowOnly(GameObject targetPanel)
    {
        SetPanelActive(simulationPanel, targetPanel);
        SetPanelActive(loadingPanel, targetPanel);
        SetPanelActive(feedbackPanel, targetPanel);
    }

    private void StopCurrentFlow()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (recordingRoutine != null)
        {
            StopCoroutine(recordingRoutine);
            recordingRoutine = null;
        }

        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
            loadingRoutine = null;
        }
    }

    private static void SetPanelActive(GameObject panel, GameObject targetPanel)
    {
        if (panel != null)
        {
            panel.SetActive(panel == targetPanel);
        }
    }

    private static void SetVisible(GameObject target, bool visible, float alpha)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(visible);
        var canvasGroup = GetOrAddCanvasGroup(target);
        canvasGroup.alpha = Mathf.Clamp01(alpha);
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private static IEnumerator FadeOut(GameObject target, float duration)
    {
        if (target == null || !target.activeSelf)
        {
            yield break;
        }

        var canvasGroup = GetOrAddCanvasGroup(target);
        var elapsed = 0f;
        var startAlpha = canvasGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        target.SetActive(false);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        var canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void SetFeedbackText(string value)
    {
        if (feedbackText != null)
        {
            feedbackText.text = value;
        }
    }

    private void ResolveMicDebugOverlay()
    {
        if (!showMicDebugOverlay)
        {
            if (micDebugText != null)
            {
                micDebugText.gameObject.SetActive(false);
            }

            return;
        }

        if (micDebugText == null && uiRoot != null)
        {
            var existing = FindChildRecursive(uiRoot, micDebugTextObjectName);
            if (existing != null)
            {
                micDebugText = existing.GetComponent<Text>();
            }
        }

        if (micDebugText == null && uiRoot != null)
        {
            var debugObject = new GameObject(micDebugTextObjectName, typeof(RectTransform));
            debugObject.transform.SetParent(uiRoot, false);
            micDebugText = debugObject.AddComponent<Text>();
        }

        if (micDebugText == null)
        {
            return;
        }

        micDebugText.gameObject.SetActive(true);
        micDebugText.raycastTarget = false;
        micDebugText.alignment = TextAnchor.UpperCenter;
        micDebugText.horizontalOverflow = HorizontalWrapMode.Wrap;
        micDebugText.verticalOverflow = VerticalWrapMode.Truncate;
        micDebugText.fontSize = 28;
        micDebugText.color = new Color(1f, 0.92f, 0.25f, 1f);

        if (micDebugText.font == null)
        {
            micDebugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (micDebugText.font == null)
            {
                micDebugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        var rectTransform = micDebugText.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -24f);
        rectTransform.sizeDelta = new Vector2(680f, 96f);
        rectTransform.localScale = Vector3.one;

        SetMicDebugText("MIC raw: waiting");
    }

    private void OnMicInputDebug(string message)
    {
        if (!waitingForVoiceResult)
        {
            return;
        }

        SetMicDebugText(message);
    }

    private void SetMicDebugText(string message)
    {
        if (!showMicDebugOverlay || micDebugText == null)
        {
            return;
        }

        micDebugText.text = TrimForMicDebug(message);
    }

    private string FormatSttDebugTranscript(string transcript)
    {
        var value = string.IsNullOrWhiteSpace(transcript) ? "<empty>" : transcript.Trim();
        return "STT raw: " + value;
    }

    private string TrimForMicDebug(string value)
    {
        if (string.IsNullOrEmpty(value) || micDebugMaxChars <= 0)
        {
            return value;
        }

        if (value.Length <= micDebugMaxChars)
        {
            return value;
        }

        return value.Substring(0, micDebugMaxChars) + "...";
    }

    private GameObject FindChildGameObject(string childName, bool warnIfMissing = true)
    {
        if (uiRoot == null)
        {
            return null;
        }

        return FindChildGameObject(uiRoot.gameObject, childName, warnIfMissing);
    }

    private static GameObject FindChildGameObject(
        GameObject parent,
        string childName,
        bool warnIfMissing = true
    )
    {
        if (parent == null)
        {
            return null;
        }

        var child = FindChildRecursive(parent.transform, childName);
        if (child == null)
        {
            if (warnIfMissing)
            {
                Debug.LogWarning("PresentationUIFlowManager: " + childName + " was not found.");
            }

            return null;
        }

        return child.gameObject;
    }

    private Button FindAgainButton()
    {
        var buttonObject = FindChildGameObject(feedbackPanel, "AgainButton", false);
        if (buttonObject == null)
        {
            return null;
        }

        var button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = buttonObject.GetComponent<Graphic>();
        }

        return button;
    }

    private Image FindFeedbackResultImage()
    {
        var imageObject = FindChildGameObject(feedbackPanel, "FeedbackResultImage", false);
        imageObject = imageObject != null ? imageObject : FindChildGameObject(feedbackPanel, "FeedackPanel", false);
        if (imageObject != null)
        {
            return imageObject.GetComponent<Image>();
        }

        if (feedbackPanel == null)
        {
            return null;
        }

        var images = feedbackPanel.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image != null && image.gameObject != feedbackPanel && image.gameObject != againButton?.gameObject)
            {
                return image;
            }
        }

        return null;
    }

    private static Text FindText(GameObject parent, string preferredName)
    {
        if (parent == null)
        {
            return null;
        }

        var texts = parent.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            if (text.name.Equals(preferredName, StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private static Image FindFilledImage(GameObject parent)
    {
        if (parent == null)
        {
            return null;
        }

        var images = parent.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image.type == Image.Type.Filled)
            {
                return image;
            }
        }

        return null;
    }

    private static Transform FindLoadingSpinner(GameObject parent)
    {
        if (parent == null)
        {
            return null;
        }

        var names = new[] { "LoadingSpinner", "Spinner", "Circle", "ProgressCircle" };
        foreach (var name in names)
        {
            var found = FindChildRecursive(parent.transform, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindInActiveScene(string objectName)
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var found = FindChildRecursive(root.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
        {
            return parent;
        }

        for (var index = 0; index < parent.childCount; index++)
        {
            var found = FindChildRecursive(parent.GetChild(index), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
