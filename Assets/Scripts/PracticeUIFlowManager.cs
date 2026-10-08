using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class PracticeUIFlowManager : MonoBehaviour
{
    private const string PracticeUiRootName = "Practice_UI_Root";

    [Header("Root")]
    [SerializeField] private Transform practiceUiRoot;

    [Header("Panels")]
    [SerializeField] private GameObject startInstructionPanel;
    [SerializeField] private GameObject listeningPanel;
    [SerializeField] private GameObject wrongPanel;
    [SerializeField] private GameObject tryPanel;
    [SerializeField] private GameObject tryPanel2;
    [SerializeField] private GameObject correctPanel;
    [SerializeField] private GameObject preparationPanel;

    [Header("Choice")]
    [SerializeField] private string choiceButtonPrefix = "Button";
    [SerializeField] private string cultureButtonName = "Button1";
    [SerializeField] private string expectedAnswer = "문화";
    [SerializeField] private float floatingOptionAmplitude = 18f;
    [SerializeField] private float floatingOptionHorizontalAmplitude = 7f;
    [SerializeField] private float floatingOptionSpeed = 1.35f;
    [SerializeField] private Transform cultureAttachedObject;
    [SerializeField] private Vector3 cultureAttachedLocalOffset = new Vector3(0f, 0f, -0.1f);

    [Header("Panel Animations")]
    [SerializeField] private Animator startInstructionAnimator;
    [SerializeField] private bool usePrefabOptionAnimation = true;
    [SerializeField] private GameObject listeningMouthObject;
    [SerializeField] private Animator listeningMouthAnimator;
    [SerializeField] private string listeningMouthObjectName = "mouth_comics_xreal";
    [SerializeField] private bool hideListeningMouthUntilVoice = true;
    [SerializeField] private GameObject tryMouthObject;
    [SerializeField] private Animator tryMouthAnimator;
    [SerializeField] private GameObject tryMouthObject2;
    [SerializeField] private Animator tryMouthAnimator2;
    [SerializeField] private string tryMouthObjectName = "mouth_culture_xreal";
    [SerializeField] private bool waitForTryMouthBeforeMic = true;
    [SerializeField] private float tryMouthMicEnableFallbackDelay = 1.6f;

    [Header("Listening")]
    [SerializeField] private GameObject listeningMicIdleObject;
    [SerializeField] private GameObject listeningMicActiveObject;
    [SerializeField] private Button listeningMicButton;
    [SerializeField] private Image listeningMicIcon;
    [SerializeField] private Sprite micIdleSprite;
    [SerializeField] private Sprite micRecognizedSprite;
    [SerializeField] private bool startRecordingFromMicButton = true;
    [SerializeField] private bool keepMic2VisibleWhileRecording = true;

    [Header("Try Recording")]
    [SerializeField] private GameObject tryMicObject;
    [SerializeField] private Button tryMicButton;
    [SerializeField] private GameObject tryMicObject2;
    [SerializeField] private Button tryMicButton2;

    [Header("Correct Popup")]
    [SerializeField] private GameObject correctPopupUi;
    [SerializeField] private float correctPopupDuration = 0.4f;
    [SerializeField] private float correctPopupStartScale = 0.18f;

    [Header("Preparation")]
    [SerializeField] private Button startPresentationButton;
    [SerializeField] private string nextSceneName = "2. PresentaionRoom";
    [SerializeField] private float correctToPreparationDelay = 1.6f;

    [Header("Voice")]
    [SerializeField] private QuestVoiceEvaluationDemo voiceEvaluation;
    [SerializeField] private float recordingSecondsBeforeAnalyze = 1.6f;
    [SerializeField] private float recordingStartTimeout = 4f;
    [SerializeField] private int correctScoreThreshold = 70;

    [Header("STT Debug")]
    [SerializeField] private bool showSttDebugOverlay = true;
    [SerializeField] private Text sttDebugText;
    [SerializeField] private string sttDebugTextObjectName = "STT_DebugText";
    [SerializeField] private int sttDebugMaxChars = 120;

    [Header("Timing")]
    [SerializeField] private float wrongPanelDuration = 1.4f;

    [Header("Options")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool autoRecordOnChoice = true;
    [SerializeField] private bool showWrongPanelOnVoiceError = true;
    [SerializeField] private bool forceCorrectOnFinalPracticeAttempt = true;
    [SerializeField] private int finalPracticeAttemptNumber = 3;

    private readonly List<ChoiceBinding> choiceBindings = new List<ChoiceBinding>();
    private Coroutine autoStopRecordingRoutine;
    private Coroutine panelRoutine;
    private Coroutine correctPopupRoutine;
    private Coroutine tryMouthRoutine;
    private Vector3 correctPopupDefaultScale = Vector3.one;
    private bool correctPopupScaleCaptured;
    private bool waitingForVoiceResult;
    private bool listeningMouthPlayedForCurrentAttempt;
    private string selectedExpectedAnswer;
    private int completedVoiceAttemptCount;
    private int wrongAttemptCount;
    private GameObject activeTryPanel;
    private Animator activeTryMouthAnimator;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForPracticeScene()
    {
        if (FindFirstObjectByType<PracticeUIFlowManager>() != null)
        {
            return;
        }

        var root = SceneObjectUtility.FindInActiveScene(PracticeUiRootName);
        if (root == null)
        {
            return;
        }

        var managerObject = new GameObject("Practice_UI_FlowManager");
        var manager = managerObject.AddComponent<PracticeUIFlowManager>();
        manager.practiceUiRoot = root;
    }

    private void Awake()
    {
        ResolveReferences();
        ResolveSttDebugOverlay();
        BindListeningMicButton();
        BindTryMicButton();
        BindChoiceButtons();
        BindPreparationButton();
        BindVoiceEvaluation();
        CaptureCorrectPopupScale();
    }

    private void Start()
    {
        if (autoStart)
        {
            RestartFlow();
        }
    }

    private void Update()
    {
        UpdateFloatingOptions();
        UpdateCultureAttachedObject();
    }

    private void OnDestroy()
    {
        UnbindChoiceButtons();
        UnbindListeningMicButton();
        UnbindTryMicButton();
        UnbindPreparationButton();
        UnbindVoiceEvaluation();
    }

    public void RestartFlow()
    {
        StopCurrentFlow();
        waitingForVoiceResult = false;
        completedVoiceAttemptCount = 0;
        wrongAttemptCount = 0;
        activeTryPanel = null;
        activeTryMouthAnimator = null;
        selectedExpectedAnswer = expectedAnswer;
        listeningMouthPlayedForCurrentAttempt = false;
        ResetChoicePositions();
        SetListeningRecognized(false);
        SetTryMicButtonInteractable(false);
        HideMouthAnimations();
        HideCorrectPopup();
        SetSttDebugText("STT raw: waiting");
        ShowStartInstructionPanel();
    }

    public void ShowStartInstructionPanel()
    {
        listeningMouthPlayedForCurrentAttempt = false;
        HideMouthAnimations();
        ShowOnly(startInstructionPanel);
        ResetChoicePositions();
        RestartAnimator(startInstructionAnimator);
    }

    public void ShowListeningPanel()
    {
        StopCurrentFlow();
        listeningMouthPlayedForCurrentAttempt = false;
        SetListeningRecognized(false);
        SetTryMicButtonInteractable(false);
        ShowOnly(listeningPanel);
    }

    public void ShowWrongPanel()
    {
        StopCurrentFlow();
        SetTryMicButtonInteractable(false);
        ShowOnly(wrongPanel);
        panelRoutine = StartCoroutine(ShowTryPanelAfterDelay());
    }

    public void ShowTryPanel()
    {
        ShowTryPanelVariant(tryPanel);
    }

    public void ShowTryPanel2()
    {
        ShowTryPanelVariant(tryPanel2 != null ? tryPanel2 : tryPanel);
    }

    private void ShowTryPanelVariant(GameObject targetTryPanel)
    {
        StopCurrentFlow();
        waitingForVoiceResult = true;
        listeningMouthPlayedForCurrentAttempt = false;
        activeTryPanel = targetTryPanel != null ? targetTryPanel : tryPanel;

        if (voiceEvaluation != null)
        {
            voiceEvaluation.SetTargetText(selectedExpectedAnswer);
        }

        SetListeningRecognized(false);
        ShowOnly(activeTryPanel);
        PlayTryMouthExample(activeTryPanel);
    }

    public void ShowCorrectPanel()
    {
        StopCurrentFlow();
        SetTryMicButtonInteractable(false);
        ShowOnly(correctPanel);
        PlayCorrectPopup();
        panelRoutine = StartCoroutine(ShowPreparationAfterCorrectDelay());
    }

    public void ShowPreparationPanel()
    {
        StopCurrentFlow();
        ShowOnly(preparationPanel);
    }

    public void LoadNextScene()
    {
        var targetScene = ResolveNextSceneName();
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("PracticeUIFlowManager: next scene is not registered in Build Settings.");
            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    public void SelectCulture()
    {
        SelectChoice(expectedAnswer);
    }

    public void SelectChoice(string answer)
    {
        StopCurrentFlow();

        selectedExpectedAnswer = string.IsNullOrWhiteSpace(answer) ? expectedAnswer : answer;
        waitingForVoiceResult = true;
        listeningMouthPlayedForCurrentAttempt = false;
        SetListeningRecognized(false);
        ShowOnly(listeningPanel);

        if (voiceEvaluation != null)
        {
            voiceEvaluation.SetTargetText(selectedExpectedAnswer);
        }

        if (startRecordingFromMicButton)
        {
            SetListeningMicButtonInteractable(true);
        }
        else if (autoRecordOnChoice && voiceEvaluation != null)
        {
            StartVoiceRecordingFromCurrentPanel();
        }
    }

    public void StartRecordingFromMicButton()
    {
        if (!waitingForVoiceResult)
        {
            return;
        }

        StartVoiceRecordingFromCurrentPanel();
    }

    public void NotifySpeechRecognized(string transcript)
    {
        SetListeningRecognized(true);

        if (!waitingForVoiceResult)
        {
            return;
        }

        waitingForVoiceResult = false;
        NotifyAnswerResult(IsTranscriptCorrect(transcript, selectedExpectedAnswer));
    }

    public void NotifyAnswerResult(bool isCorrect)
    {
        completedVoiceAttemptCount += 1;
        if (
            forceCorrectOnFinalPracticeAttempt
            && completedVoiceAttemptCount >= Mathf.Max(1, finalPracticeAttemptNumber)
        )
        {
            isCorrect = true;
        }

        if (isCorrect)
        {
            ShowCorrectPanel();
        }
        else
        {
            wrongAttemptCount += 1;
            ShowWrongPanel();
        }
    }

    public void SimulateCorrect()
    {
        waitingForVoiceResult = false;
        NotifyAnswerResult(true);
    }

    public void SimulateWrong()
    {
        waitingForVoiceResult = false;
        NotifyAnswerResult(false);
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
            autoStopRecordingRoutine = null;
            yield break;
        }

        if (!voiceEvaluation.IsRecording)
        {
            autoStopRecordingRoutine = null;
            if (showWrongPanelOnVoiceError)
            {
                waitingForVoiceResult = false;
                NotifyAnswerResult(false);
            }

            yield break;
        }

        SetListeningRecognized(true);
        yield return new WaitForSeconds(recordingSecondsBeforeAnalyze);

        if (voiceEvaluation != null && voiceEvaluation.IsRecording)
        {
            voiceEvaluation.StopRecordingAndAnalyze();
        }

        autoStopRecordingRoutine = null;
    }

    private IEnumerator ShowTryPanelAfterDelay()
    {
        yield return new WaitForSeconds(wrongPanelDuration);
        panelRoutine = null;
        if (wrongAttemptCount >= 2 && tryPanel2 != null)
        {
            ShowTryPanel2();
        }
        else
        {
            ShowTryPanel();
        }
    }

    private IEnumerator ShowPreparationAfterCorrectDelay()
    {
        yield return new WaitForSeconds(correctToPreparationDelay);
        ShowOnly(preparationPanel);
        panelRoutine = null;
    }

    private void ResolveReferences()
    {
        var sceneRoot = SceneObjectUtility.FindInActiveScene(PracticeUiRootName);
        if (practiceUiRoot == null || practiceUiRoot.name != PracticeUiRootName)
        {
            practiceUiRoot = sceneRoot;
        }

        if (practiceUiRoot == null)
        {
            Debug.LogWarning("PracticeUIFlowManager: Practice_UI_Root was not found.");
            return;
        }

        if (!practiceUiRoot.gameObject.activeSelf)
        {
            practiceUiRoot.gameObject.SetActive(true);
        }

        startInstructionPanel = startInstructionPanel != null ? startInstructionPanel : FindChildGameObject("StartInstructionPanel");
        listeningPanel = listeningPanel != null ? listeningPanel : FindChildGameObject("ListeningPanel");
        wrongPanel = wrongPanel != null ? wrongPanel : FindChildGameObject("WrongPanel");
        tryPanel = tryPanel != null ? tryPanel : FindChildGameObject("TryPanel", false);
        tryPanel = tryPanel != null ? tryPanel : FindChildGameObject("CorrectPanel (1)", false);
        tryPanel2 = tryPanel2 != null ? tryPanel2 : FindChildGameObject("TryPanel2", false);
        correctPanel = correctPanel != null ? correctPanel : FindChildGameObject("CorrectPanel");
        preparationPanel = preparationPanel != null ? preparationPanel : FindChildGameObject("PreperationPanel", false);
        preparationPanel = preparationPanel != null ? preparationPanel : FindChildGameObject("PreparationPanel", false);

        correctPopupUi = correctPopupUi != null ? correctPopupUi : FindChildGameObject("Correct_PopupUI", false);
        correctPopupUi = correctPopupUi != null ? correctPopupUi : FindDirectChildGameObject(correctPanel, "3");
        startPresentationButton = startPresentationButton != null ? startPresentationButton : FindStartPresentationButton();

        ResolveAnimationReferences();
        ResolveMicSprites();

        voiceEvaluation = voiceEvaluation != null ? voiceEvaluation : FindFirstObjectByType<QuestVoiceEvaluationDemo>();
    }

    private void ResolveSttDebugOverlay()
    {
        sttDebugText = DebugOverlayText.Resolve(
            sttDebugText,
            practiceUiRoot,
            sttDebugTextObjectName,
            showSttDebugOverlay,
            false
        );
        SetSttDebugText("STT raw: waiting");
    }

    private void ResolveAnimationReferences()
    {
        if (startInstructionPanel != null && startInstructionAnimator == null)
        {
            startInstructionAnimator = FindAnimatorInChildren(startInstructionPanel);
        }

        if (listeningPanel != null && listeningMouthObject == null)
        {
            listeningMouthObject = FindChildGameObject(listeningPanel, listeningMouthObjectName);
        }

        if (listeningMouthObject != null && listeningMouthAnimator == null)
        {
            listeningMouthAnimator = FindAnimatorInChildren(listeningMouthObject);
        }

        if (tryPanel != null && tryMouthObject == null)
        {
            tryMouthObject = FindChildGameObject(tryPanel, tryMouthObjectName);
        }

        if (tryMouthObject != null && tryMouthAnimator == null)
        {
            tryMouthAnimator = FindAnimatorInChildren(tryMouthObject);
        }

        if (tryPanel2 != null && tryMouthObject2 == null)
        {
            tryMouthObject2 = FindChildGameObject(tryPanel2, tryMouthObjectName);
        }

        if (tryMouthObject2 == null && tryPanel2 != null)
        {
            var animator = FindAnimatorInChildren(tryPanel2);
            tryMouthObject2 = animator != null ? animator.gameObject : null;
        }

        if (tryMouthObject2 != null && tryMouthAnimator2 == null)
        {
            tryMouthAnimator2 = FindAnimatorInChildren(tryMouthObject2);
        }
    }

    private void StartVoiceRecordingFromCurrentPanel()
    {
        if (voiceEvaluation == null)
        {
            OnVoiceError("VoiceEvaluationManager was not found.");
            return;
        }

        SetListeningMicButtonInteractable(false);
        SetTryMicButtonInteractable(false);
        SetSttDebugText("STT raw: recording...");
        voiceEvaluation.StartRecording();

        if (autoStopRecordingRoutine != null)
        {
            StopCoroutine(autoStopRecordingRoutine);
        }

        autoStopRecordingRoutine = StartCoroutine(StopRecordingAfterDelay());
    }

    private void ResolveMicSprites()
    {
        if (listeningPanel == null)
        {
            return;
        }

        listeningMicIdleObject = listeningMicIdleObject != null
            ? listeningMicIdleObject
            : FindDirectChildGameObject(listeningPanel, "Mic2");
        listeningMicActiveObject = listeningMicActiveObject != null
            ? listeningMicActiveObject
            : FindDirectChildGameObject(listeningPanel, "Mic1");

        if (listeningMicIdleObject == null)
        {
            listeningMicIdleObject = FindGameObjectBySpriteName(listeningPanel.transform, "Mic2");
        }

        if (listeningMicActiveObject == null)
        {
            listeningMicActiveObject = FindGameObjectBySpriteName(listeningPanel.transform, "Mic1");
        }

        if (listeningMicIcon == null)
        {
            listeningMicIcon = FindImageBySpriteName(listeningPanel.transform, "Mic1");
        }

        if (listeningMicIcon == null)
        {
            listeningMicIcon = FindImageByObjectName(listeningPanel.transform, "Mic");
        }

        if (listeningMicIcon == null && listeningPanel.transform.childCount > 0)
        {
            var lastChild = listeningPanel.transform.GetChild(listeningPanel.transform.childCount - 1);
            listeningMicIcon = lastChild.GetComponent<Image>();
        }

        if (micIdleSprite == null && listeningMicIcon != null)
        {
            var idleImage = listeningMicIdleObject != null ? listeningMicIdleObject.GetComponent<Image>() : null;
            micIdleSprite = idleImage != null ? idleImage.sprite : listeningMicIcon.sprite;
        }

        if (micRecognizedSprite == null && practiceUiRoot != null)
        {
            var activeImage = listeningMicActiveObject != null ? listeningMicActiveObject.GetComponent<Image>() : null;
            micRecognizedSprite = activeImage != null ? activeImage.sprite : null;
        }

        if (listeningMicButton == null && listeningMicIdleObject != null)
        {
            listeningMicButton = GetOrAddButton(listeningMicIdleObject);
        }

        ResolveTryMicButton();
    }

    private void ResolveTryMicButton()
    {
        ResolveTryMicButtonForPanel(tryPanel, ref tryMicObject, ref tryMicButton);
        ResolveTryMicButtonForPanel(tryPanel2, ref tryMicObject2, ref tryMicButton2);
    }

    private void ResolveTryMicButtonForPanel(GameObject panel, ref GameObject micObject, ref Button micButton)
    {
        if (panel == null)
        {
            return;
        }

        micObject = micObject != null
            ? micObject
            : FindDirectChildGameObject(panel, "Mic2");

        if (micObject == null)
        {
            micObject = FindGameObjectBySpriteName(panel.transform, "Mic2");
        }

        if (micButton == null && micObject != null)
        {
            micButton = GetOrAddButton(micObject);
        }
    }

    private void BindListeningMicButton()
    {
        if (listeningMicButton == null)
        {
            return;
        }

        listeningMicButton.onClick.RemoveListener(StartRecordingFromMicButton);
        listeningMicButton.onClick.AddListener(StartRecordingFromMicButton);
        SetListeningMicButtonInteractable(false);
    }

    private void BindTryMicButton()
    {
        if (tryMicButton == null)
        {
            if (tryMicButton2 == null)
            {
                return;
            }
        }

        BindTryMicButton(tryMicButton);
        BindTryMicButton(tryMicButton2);
        SetTryMicButtonInteractable(false);
    }

    private void BindTryMicButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(StartRecordingFromMicButton);
        button.onClick.AddListener(StartRecordingFromMicButton);
    }

    private void BindChoiceButtons()
    {
        UnbindChoiceButtons();

        if (startInstructionPanel == null)
        {
            return;
        }

        var candidates = new List<Transform>();
        CollectChoiceButtonTransforms(startInstructionPanel.transform, candidates);

        for (var index = 0; index < candidates.Count; index++)
        {
            var child = candidates[index];
            var button = GetOrAddButton(child.gameObject);
            if (button == null)
            {
                continue;
            }

            var expectedForButton = child.name.Equals(cultureButtonName, StringComparison.OrdinalIgnoreCase)
                ? expectedAnswer
                : child.name;
            var rectTransform = child as RectTransform;
            var phase = index * 0.93f;
            var useManualFloating = child.parent == startInstructionPanel.transform;
            UnityAction action = () => SelectChoice(expectedForButton);

            button.onClick.AddListener(action);
            choiceBindings.Add(new ChoiceBinding(
                button,
                action,
                rectTransform,
                expectedForButton,
                phase,
                useManualFloating
            ));
        }
    }

    private void BindPreparationButton()
    {
        if (startPresentationButton == null)
        {
            return;
        }

        startPresentationButton.onClick.RemoveListener(LoadNextScene);
        startPresentationButton.onClick.AddListener(LoadNextScene);
    }

    private void BindVoiceEvaluation()
    {
        if (voiceEvaluation == null)
        {
            return;
        }

        voiceEvaluation.onResponseJson.RemoveListener(OnVoiceResponseJson);
        voiceEvaluation.onTranscript.RemoveListener(OnVoiceTranscript);
        voiceEvaluation.onError.RemoveListener(OnVoiceError);
        voiceEvaluation.onMicInputDebug.RemoveListener(OnMicInputDebug);

        voiceEvaluation.onResponseJson.AddListener(OnVoiceResponseJson);
        voiceEvaluation.onTranscript.AddListener(OnVoiceTranscript);
        voiceEvaluation.onError.AddListener(OnVoiceError);
        voiceEvaluation.onMicInputDebug.AddListener(OnMicInputDebug);
    }

    private void UnbindChoiceButtons()
    {
        foreach (var binding in choiceBindings)
        {
            if (binding.Button != null)
            {
                binding.Button.onClick.RemoveListener(binding.Action);
            }
        }

        choiceBindings.Clear();
    }

    private void UnbindListeningMicButton()
    {
        if (listeningMicButton != null)
        {
            listeningMicButton.onClick.RemoveListener(StartRecordingFromMicButton);
        }
    }

    private void UnbindTryMicButton()
    {
        UnbindTryMicButton(tryMicButton);
        UnbindTryMicButton(tryMicButton2);
    }

    private void UnbindTryMicButton(Button button)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(StartRecordingFromMicButton);
        }
    }

    private void UnbindPreparationButton()
    {
        if (startPresentationButton != null)
        {
            startPresentationButton.onClick.RemoveListener(LoadNextScene);
        }
    }

    private void UnbindVoiceEvaluation()
    {
        if (voiceEvaluation == null)
        {
            return;
        }

        voiceEvaluation.onResponseJson.RemoveListener(OnVoiceResponseJson);
        voiceEvaluation.onTranscript.RemoveListener(OnVoiceTranscript);
        voiceEvaluation.onError.RemoveListener(OnVoiceError);
        voiceEvaluation.onMicInputDebug.RemoveListener(OnMicInputDebug);
    }

    private void OnVoiceResponseJson(string responseJson)
    {
        if (!waitingForVoiceResult)
        {
            return;
        }

        var response = VoiceResponseEvaluator.Parse(responseJson, nameof(PracticeUIFlowManager));

        SetListeningRecognized(true);
        SetSttDebugText(DebugOverlayText.FormatTranscript(response != null ? response.transcript : null));
        waitingForVoiceResult = false;
        NotifyAnswerResult(IsResponseCorrect(response));
    }

    private void OnVoiceTranscript(string transcript)
    {
        SetSttDebugText(DebugOverlayText.FormatTranscript(transcript));
        NotifySpeechRecognized(transcript);
    }

    private void OnVoiceError(string message)
    {
        Debug.LogWarning("PracticeUIFlowManager: voice evaluation error. " + message);
        SetSttDebugText("STT error: " + DebugOverlayText.Trim(message, sttDebugMaxChars));

        if (!waitingForVoiceResult || !showWrongPanelOnVoiceError)
        {
            return;
        }

        waitingForVoiceResult = false;
        NotifyAnswerResult(false);
    }

    private void OnMicInputDebug(string message)
    {
        if (!waitingForVoiceResult)
        {
            return;
        }

        SetSttDebugText(message);
    }

    private void SetSttDebugText(string message)
    {
        if (!showSttDebugOverlay || sttDebugText == null)
        {
            return;
        }

        sttDebugText.text = DebugOverlayText.Trim(message, sttDebugMaxChars);
    }

    private bool IsResponseCorrect(VoiceEvaluationResponse response)
    {
        return VoiceResponseEvaluator.TryEvaluate(response, correctScoreThreshold, true, out var isCorrect)
            ? isCorrect
            : IsTranscriptCorrect(response.transcript, selectedExpectedAnswer);
    }

    private bool IsTranscriptCorrect(string transcript, string expected)
    {
        var normalizedTranscript = NormalizeForCompare(transcript);
        var normalizedExpected = NormalizeForCompare(expected);

        if (string.IsNullOrEmpty(normalizedExpected))
        {
            return !string.IsNullOrEmpty(normalizedTranscript);
        }

        return normalizedTranscript == normalizedExpected
            || normalizedTranscript.Contains(normalizedExpected);
    }

    private void SetListeningRecognized(bool recognized)
    {
        if (recognized)
        {
            PlayListeningMouthAnimation();
        }
        else
        {
            listeningMouthPlayedForCurrentAttempt = false;
            SetMouthObjectActive(listeningMouthObject, !hideListeningMouthUntilVoice);
        }

        var usesObjectSwap = listeningMicIdleObject != null || listeningMicActiveObject != null;
        var showMic2 = !recognized || keepMic2VisibleWhileRecording;
        var showMic1 = recognized && !keepMic2VisibleWhileRecording;

        if (listeningMicIdleObject != null)
        {
            listeningMicIdleObject.SetActive(showMic2);
        }

        if (listeningMicActiveObject != null)
        {
            listeningMicActiveObject.SetActive(showMic1);
        }

        SetListeningMicButtonInteractable(
            startRecordingFromMicButton && waitingForVoiceResult && !recognized
        );

        if (recognized)
        {
            SetTryMicButtonInteractable(false);
        }

        if (usesObjectSwap)
        {
            return;
        }

        if (listeningMicIcon == null)
        {
            return;
        }

        if (!listeningMicIcon.gameObject.activeSelf)
        {
            listeningMicIcon.gameObject.SetActive(true);
        }

        var sprite = recognized ? micRecognizedSprite : micIdleSprite;
        if (sprite != null)
        {
            listeningMicIcon.sprite = sprite;
        }
    }

    private void SetListeningMicButtonInteractable(bool interactable)
    {
        if (listeningMicButton != null)
        {
            listeningMicButton.interactable = interactable;
        }
    }

    private void SetTryMicButtonInteractable(bool interactable)
    {
        if (tryMicObject != null)
        {
            tryMicObject.SetActive(true);
        }

        if (tryMicButton != null)
        {
            tryMicButton.interactable = interactable;
        }

        if (tryMicObject2 != null)
        {
            tryMicObject2.SetActive(true);
        }

        if (tryMicButton2 != null)
        {
            tryMicButton2.interactable = interactable;
        }
    }

    private void PlayListeningMouthAnimation()
    {
        if (listeningMouthPlayedForCurrentAttempt)
        {
            return;
        }

        listeningMouthPlayedForCurrentAttempt = true;
        SetMouthObjectActive(listeningMouthObject, true);
        RestartAnimator(listeningMouthAnimator);
    }

    private void PlayTryMouthExample(GameObject targetTryPanel)
    {
        SetMouthObjectActive(listeningMouthObject, !hideListeningMouthUntilVoice);
        var useSecondTryPanel = targetTryPanel != null && targetTryPanel == tryPanel2;
        var targetMouthObject = useSecondTryPanel && tryMouthObject2 != null ? tryMouthObject2 : tryMouthObject;
        activeTryMouthAnimator = useSecondTryPanel && tryMouthAnimator2 != null ? tryMouthAnimator2 : tryMouthAnimator;

        SetMouthObjectActive(tryMouthObject, !useSecondTryPanel && tryMouthObject != null);
        SetMouthObjectActive(tryMouthObject2, useSecondTryPanel && tryMouthObject2 != null);
        SetMouthObjectActive(targetMouthObject, true);
        RestartAnimator(activeTryMouthAnimator);

        if (!waitForTryMouthBeforeMic)
        {
            SetTryMicButtonInteractable(true);
            return;
        }

        SetTryMicButtonInteractable(false);
        tryMouthRoutine = StartCoroutine(EnableTryMicAfterMouthExample());
    }

    private IEnumerator EnableTryMicAfterMouthExample()
    {
        var delay = GetAnimatorClipLength(activeTryMouthAnimator, tryMouthMicEnableFallbackDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (activeTryPanel != null && activeTryPanel.activeInHierarchy && waitingForVoiceResult)
        {
            SetTryMicButtonInteractable(true);
        }

        tryMouthRoutine = null;
    }

    private void HideMouthAnimations()
    {
        SetMouthObjectActive(listeningMouthObject, !hideListeningMouthUntilVoice);
        SetMouthObjectActive(tryMouthObject, false);
        SetMouthObjectActive(tryMouthObject2, false);
    }

    private static void SetMouthObjectActive(GameObject mouthObject, bool active)
    {
        if (mouthObject != null)
        {
            mouthObject.SetActive(active);
        }
    }

    private static void RestartAnimator(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);

        if (animator.layerCount <= 0)
        {
            return;
        }

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != 0)
        {
            animator.Play(state.shortNameHash, 0, 0f);
            animator.Update(0f);
        }
    }

    private static float GetAnimatorClipLength(Animator animator, float fallbackDelay)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0 && clips[0] != null)
            {
                return Mathf.Max(0f, clips[0].length);
            }
        }

        return Mathf.Max(0f, fallbackDelay);
    }

    private void PlayCorrectPopup()
    {
        if (correctPopupUi == null)
        {
            return;
        }

        if (correctPopupRoutine != null)
        {
            StopCoroutine(correctPopupRoutine);
        }

        correctPopupRoutine = StartCoroutine(PlayCorrectPopupRoutine());
    }

    private IEnumerator PlayCorrectPopupRoutine()
    {
        CaptureCorrectPopupScale();

        var popupTransform = correctPopupUi.transform;
        var canvasGroup = correctPopupUi.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = correctPopupUi.AddComponent<CanvasGroup>();
        }

        correctPopupUi.SetActive(true);
        popupTransform.localScale = correctPopupDefaultScale * correctPopupStartScale;
        canvasGroup.alpha = 0f;

        var elapsed = 0f;
        while (elapsed < correctPopupDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / correctPopupDuration);
            var eased = EaseOutBack(t);

            popupTransform.localScale = Vector3.LerpUnclamped(
                correctPopupDefaultScale * correctPopupStartScale,
                correctPopupDefaultScale,
                eased
            );
            canvasGroup.alpha = Mathf.Clamp01(t * 1.6f);
            yield return null;
        }

        popupTransform.localScale = correctPopupDefaultScale;
        canvasGroup.alpha = 1f;
        correctPopupRoutine = null;
    }

    private void HideCorrectPopup()
    {
        if (correctPopupUi == null)
        {
            return;
        }

        CaptureCorrectPopupScale();
        correctPopupUi.transform.localScale = correctPopupDefaultScale;
        correctPopupUi.SetActive(false);
    }

    private void CaptureCorrectPopupScale()
    {
        if (correctPopupScaleCaptured || correctPopupUi == null)
        {
            return;
        }

        correctPopupDefaultScale = correctPopupUi.transform.localScale;
        correctPopupScaleCaptured = true;
    }

    private void UpdateFloatingOptions()
    {
        if (usePrefabOptionAnimation)
        {
            return;
        }

        if (startInstructionPanel == null || !startInstructionPanel.activeInHierarchy)
        {
            return;
        }

        var time = Time.time * floatingOptionSpeed;
        foreach (var binding in choiceBindings)
        {
            if (binding.RectTransform == null || !binding.UseManualFloating)
            {
                continue;
            }

            var offset = new Vector2(
                Mathf.Cos(time * 0.77f + binding.Phase) * floatingOptionHorizontalAmplitude,
                Mathf.Sin(time + binding.Phase) * floatingOptionAmplitude
            );
            binding.RectTransform.anchoredPosition = binding.BaseAnchoredPosition + offset;
        }
    }

    private void UpdateCultureAttachedObject()
    {
        if (cultureAttachedObject == null || startInstructionPanel == null || !startInstructionPanel.activeInHierarchy)
        {
            return;
        }

        var cultureBinding = choiceBindings.Find(binding => binding.ExpectedAnswer == expectedAnswer);
        if (cultureBinding == null || cultureBinding.RectTransform == null)
        {
            return;
        }

        cultureAttachedObject.position = cultureBinding.RectTransform.TransformPoint(cultureAttachedLocalOffset);
    }

    private void ResetChoicePositions()
    {
        foreach (var binding in choiceBindings)
        {
            if (binding.RectTransform != null)
            {
                binding.BaseAnchoredPosition = binding.RectTransform.anchoredPosition;
            }
        }
    }

    private void ShowOnly(GameObject targetPanel)
    {
        SceneObjectUtility.SetPanelActive(startInstructionPanel, targetPanel);
        SceneObjectUtility.SetPanelActive(listeningPanel, targetPanel);
        SceneObjectUtility.SetPanelActive(wrongPanel, targetPanel);
        SceneObjectUtility.SetPanelActive(tryPanel, targetPanel);
        SceneObjectUtility.SetPanelActive(tryPanel2, targetPanel);
        SceneObjectUtility.SetPanelActive(correctPanel, targetPanel);
        SceneObjectUtility.SetPanelActive(preparationPanel, targetPanel);
    }

    private void StopCurrentFlow()
    {
        if (autoStopRecordingRoutine != null)
        {
            StopCoroutine(autoStopRecordingRoutine);
            autoStopRecordingRoutine = null;
        }

        if (panelRoutine != null)
        {
            StopCoroutine(panelRoutine);
            panelRoutine = null;
        }

        if (correctPopupRoutine != null)
        {
            StopCoroutine(correctPopupRoutine);
            correctPopupRoutine = null;
        }

        if (tryMouthRoutine != null)
        {
            StopCoroutine(tryMouthRoutine);
            tryMouthRoutine = null;
        }
    }

    private GameObject FindChildGameObject(string childName, bool warnIfMissing = true)
    {
        var child = SceneObjectUtility.FindChildRecursive(practiceUiRoot, childName);
        if (child == null)
        {
            if (warnIfMissing)
            {
                Debug.LogWarning("PracticeUIFlowManager: " + childName + " was not found.");
            }

            return null;
        }

        return child.gameObject;
    }

    private static GameObject FindDirectChildGameObject(GameObject parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (var index = 0; index < parent.transform.childCount; index++)
        {
            var child = parent.transform.GetChild(index);
            if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Button GetOrAddButton(GameObject target)
    {
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

    private static GameObject FindChildGameObject(GameObject parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        var child = SceneObjectUtility.FindChildRecursive(parent.transform, childName);
        return child != null ? child.gameObject : null;
    }

    private static Animator FindAnimatorInChildren(GameObject parent)
    {
        if (parent == null)
        {
            return null;
        }

        var ownAnimator = parent.GetComponent<Animator>();
        if (ownAnimator != null)
        {
            return ownAnimator;
        }

        var animators = parent.GetComponentsInChildren<Animator>(true);
        foreach (var animator in animators)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                return animator;
            }
        }

        return animators.Length > 0 ? animators[0] : null;
    }

    private void CollectChoiceButtonTransforms(Transform parent, List<Transform> results)
    {
        if (parent == null)
        {
            return;
        }

        for (var index = 0; index < parent.childCount; index++)
        {
            var child = parent.GetChild(index);
            if (child.name.StartsWith(choiceButtonPrefix, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(child);
            }

            CollectChoiceButtonTransforms(child, results);
        }
    }

    private Button FindStartPresentationButton()
    {
        if (preparationPanel == null)
        {
            return null;
        }

        var startButtonTransform = SceneObjectUtility.FindChildRecursive(preparationPanel.transform, "StartButton");
        if (startButtonTransform == null)
        {
            return null;
        }

        return GetOrAddButton(startButtonTransform.gameObject);
    }

    private string ResolveNextSceneName()
    {
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            return nextSceneName;
        }

        if (Application.CanStreamedLevelBeLoaded("2. PresentationRoom"))
        {
            return "2. PresentationRoom";
        }

        if (Application.CanStreamedLevelBeLoaded("2. PresentaionRoom"))
        {
            return "2. PresentaionRoom";
        }

        var nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextBuildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            return SceneUtility.GetScenePathByBuildIndex(nextBuildIndex);
        }

        return null;
    }

    private static Image FindImageByObjectName(Transform root, string namePart)
    {
        if (root == null)
        {
            return null;
        }

        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return image;
            }
        }

        return null;
    }

    private static Image FindImageBySpriteName(Transform root, string spriteName)
    {
        if (root == null)
        {
            return null;
        }

        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image.sprite != null && image.sprite.name.Equals(spriteName, StringComparison.OrdinalIgnoreCase))
            {
                return image;
            }
        }

        return null;
    }

    private static GameObject FindGameObjectBySpriteName(Transform root, string spriteName)
    {
        var image = FindImageBySpriteName(root, spriteName);
        return image != null ? image.gameObject : null;
    }

    private static string NormalizeForCompare(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static float EaseOutBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        var t = value - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    private sealed class ChoiceBinding
    {
        public ChoiceBinding(
            Button button,
            UnityAction action,
            RectTransform rectTransform,
            string expectedAnswer,
            float phase,
            bool useManualFloating
        )
        {
            Button = button;
            Action = action;
            RectTransform = rectTransform;
            ExpectedAnswer = expectedAnswer;
            Phase = phase;
            UseManualFloating = useManualFloating;
            BaseAnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        }

        public Button Button { get; }
        public UnityAction Action { get; }
        public RectTransform RectTransform { get; }
        public string ExpectedAnswer { get; }
        public float Phase { get; }
        public bool UseManualFloating { get; }
        public Vector2 BaseAnchoredPosition { get; set; }
    }
}
