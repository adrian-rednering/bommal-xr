using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public enum VoiceEvaluationBackendKind
{
    StandardUnityContract,
    LegendaryVowels,
}

public sealed class QuestVoiceEvaluationDemo : MonoBehaviour
{
    private const string StandardApiPrefixPath = "/api/v1";
    private const string StandardAnalyzePath = "/api/v1/analyze";
    private const string LegendaryPrefixPath = "/pronunciation";
    private const string LegendaryApiPrefixPath = "/pronunciation/api/v1";
    private const string LegendaryAnalyzePath = "/pronunciation/api/v1/analyze";

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "http://192.168.219.155:9000";
    [SerializeField] private VoiceEvaluationBackendKind backendKind = VoiceEvaluationBackendKind.LegendaryVowels;
    [SerializeField] private string analyzePathOverride;
    [SerializeField] private int requestTimeoutSeconds = 15;

    [Header("Analysis Request")]
    [SerializeField] private string mode = "education";
    [SerializeField] private string targetText = "\uBB38\uD654";
    [SerializeField] private string sessionId = "quest-session-001";
    [SerializeField] private string clientVersion = "unity-quest-demo";

    [Header("Recording")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxRecordingSeconds = 10;

    [Header("Mic Debug")]
    [SerializeField] private bool emitMicInputDebug = true;
    [SerializeField] private float micDebugIntervalSeconds = 0.2f;
    [SerializeField] private int micDebugSampleWindow = 1024;

    [Header("Debug Output")]
    [SerializeField] private bool logDebugOutput = true;
    [SerializeField] private bool logResponseJson = true;
    [SerializeField] private int responseJsonLogMaxChars = 4000;
    [SerializeField] private bool mirrorDebugToSushiSite = true;
    [SerializeField] private string sushiDebugBaseUrl = "http://192.168.219.155:9000";
    [SerializeField] private string sushiDebugPath = "/unity/voice-debug";
    [SerializeField] private int sushiDebugTimeoutSeconds = 3;
    [SerializeField] private string lastResponseJson;
    [SerializeField] private string lastError;
    [SerializeField] private string lastTranscript;
    [SerializeField] private string lastMicDebugStatus;

    public UnityEvent<string> onResponseJson = new UnityEvent<string>();
    public UnityEvent<string> onError = new UnityEvent<string>();
    public UnityEvent<string> onTranscript = new UnityEvent<string>();
    public UnityEvent<string> onMicInputDebug = new UnityEvent<string>();

    private AudioClip recordingClip;
    private string microphoneDevice;
    private bool isRecording;
    private int attemptCounter;
    private Coroutine micDebugRoutine;
    private float recordingStartedAt;

    public bool IsRecording => isRecording;
    public string LastResponseJson => lastResponseJson;
    public string LastError => lastError;
    public string LastTranscript => lastTranscript;
    public string LastMicDebugStatus => lastMicDebugStatus;

    public string TargetText
    {
        get => NormalizeKnownText(targetText);
        set => targetText = NormalizeKnownText(value);
    }

    public void SetTargetText(string value)
    {
        targetText = NormalizeKnownText(value);
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            LogDebug("StartRecording ignored because recording is already active.");
            return;
        }

        LogDebug("StartRecording requested.");
        EmitMicDebug("MIC raw: requesting permission");
        StartCoroutine(StartRecordingAfterPermission());
    }

    public void StopRecordingAndAnalyze()
    {
        if (!isRecording || recordingClip == null)
        {
            StopMicDebugRoutine();
            ReportError("No active recording.");
            return;
        }

        int samplePosition = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;
        StopMicDebugRoutine();
        LogDebug($"StopRecording requested. samplePosition={samplePosition}");

        if (samplePosition <= 0)
        {
            EmitMicDebug("MIC raw: empty recording");
            ReportError("Recording was empty.");
            return;
        }

        byte[] wavBytes = WavEncoder.Encode(recordingClip, samplePosition);
        attemptCounter += 1;
        string attemptId = $"attempt-{attemptCounter:000}";
        EmitMicDebug($"MIC raw: captured samples={samplePosition} wavBytes={wavBytes.Length}");
        LogDebug($"Recording encoded. wavBytes={wavBytes.Length} attempt={attemptId}");
        MirrorVoiceDebugInput(attemptId, samplePosition, wavBytes.Length);
        StartCoroutine(AnalyzeWav(wavBytes, attemptId));
    }

    public void StopRecordingWithoutAnalyze()
    {
        if (!isRecording)
        {
            StopMicDebugRoutine();
            return;
        }

        int samplePosition = recordingClip != null ? Microphone.GetPosition(microphoneDevice) : 0;
        Microphone.End(microphoneDevice);
        isRecording = false;
        StopMicDebugRoutine();
        EmitMicDebug($"MIC raw: demo captured samples={samplePosition}");
        LogDebug($"Demo stop without analyze. samplePosition={samplePosition}");
    }

    private IEnumerator StartRecordingAfterPermission()
    {
        yield return RequestMicrophonePermission();

        if (!HasMicrophonePermission())
        {
            EmitMicDebug("MIC raw: permission denied");
            ReportError("Microphone permission was denied.");
            yield break;
        }

        microphoneDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        LogDebug(
            $"Microphone permission ok. devices={Microphone.devices.Length} selected={microphoneDevice ?? "<default>"}"
        );
        recordingClip = Microphone.Start(
            microphoneDevice,
            false,
            maxRecordingSeconds,
            sampleRate
        );

        if (recordingClip == null)
        {
            ReportError("Microphone recording could not start.");
            yield break;
        }

        lastError = string.Empty;
        isRecording = true;
        recordingStartedAt = Time.realtimeSinceStartup;
        StartMicDebugRoutine();
        EmitMicDebug($"MIC raw: started device={microphoneDevice ?? "<default>"} sampleRate={sampleRate}");
        LogDebug($"Microphone recording started. sampleRate={sampleRate} maxSeconds={maxRecordingSeconds}");
    }

    private void OnDestroy()
    {
        StopMicDebugRoutine();
    }

    private IEnumerator RequestMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (
                !Permission.HasUserAuthorizedPermission(Permission.Microphone)
                && Time.realtimeSinceStartup < timeoutAt
            )
            {
                yield return null;
            }
        }
#else
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#endif
    }

    private static bool HasMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
    }

    private IEnumerator AnalyzeWav(byte[] wavBytes, string attemptId)
    {
        if (!TryBuildAnalyzeUrl(out string analyzeUrl))
        {
            ReportError("Backend Base Url is empty.");
            yield break;
        }

        string requestId = $"{sessionId}-{attemptId}";
        string normalizedTargetText = TargetText;
        LogDebug(
            $"POST analyze start. url={analyzeUrl} requestId={requestId} mode={mode} target={normalizedTargetText} wavBytes={wavBytes.Length}"
        );
        var form = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection(
                "audio",
                wavBytes,
                "quest-recording.wav",
                "audio/wav"
            ),
            new MultipartFormDataSection("mode", mode),
            new MultipartFormDataSection("target_text", normalizedTargetText),
            new MultipartFormDataSection("session_id", sessionId),
            new MultipartFormDataSection("attempt_id", attemptId),
            new MultipartFormDataSection("client_version", clientVersion),
        };

        using (UnityWebRequest request = UnityWebRequest.Post(analyzeUrl, form))
        {
            request.timeout = requestTimeoutSeconds;
            request.SetRequestHeader("x-request-id", requestId);

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (Exception exception)
            {
                ReportError($"UnityWebRequest could not start: {exception.GetType().Name}: {exception.Message}");
                yield break;
            }

            yield return operation;

            string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            LogDebug(
                $"POST analyze finished. responseCode={request.responseCode} result={request.result} bodyLength={body.Length}"
            );
            if (request.result != UnityWebRequest.Result.Success)
            {
                ReportError($"{request.responseCode}: {request.error}\n{body}");
                yield break;
            }

            string unityResponseJson = BuildUnityCompatibleResponseJson(body);
            lastResponseJson = unityResponseJson;
            LogResponseJson(unityResponseJson);
            onResponseJson?.Invoke(unityResponseJson);

            VoiceEvaluationResponse response = null;
            try
            {
                response = JsonUtility.FromJson<VoiceEvaluationResponse>(unityResponseJson);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning("QuestVoiceEvaluationDemo: response parse failed. " + exception.Message);
            }

            lastTranscript = response != null ? response.transcript : string.Empty;
            LogDebug("STT raw transcript=" + (string.IsNullOrWhiteSpace(lastTranscript) ? "<empty>" : lastTranscript));
            MirrorVoiceDebugOutput(attemptId, body, unityResponseJson, lastTranscript);
            onTranscript?.Invoke(lastTranscript);
        }
    }

    private bool TryBuildAnalyzeUrl(out string analyzeUrl)
    {
        analyzeUrl = string.Empty;
        string baseUrl = backendBaseUrl != null ? backendBaseUrl.Trim() : string.Empty;
        string analyzePath = GetAnalyzePath();
        string apiPrefixPath = GetApiPrefixPath();

        if (string.IsNullOrEmpty(baseUrl))
        {
            return false;
        }

        baseUrl = baseUrl.TrimEnd('/');
        if (baseUrl.EndsWith(analyzePath, StringComparison.OrdinalIgnoreCase))
        {
            analyzeUrl = baseUrl;
        }
        else if (baseUrl.EndsWith(apiPrefixPath, StringComparison.OrdinalIgnoreCase))
        {
            analyzeUrl = baseUrl + "/analyze";
        }
        else if (
            backendKind == VoiceEvaluationBackendKind.LegendaryVowels
            && baseUrl.EndsWith(LegendaryPrefixPath, StringComparison.OrdinalIgnoreCase)
        )
        {
            analyzeUrl = baseUrl + StandardAnalyzePath;
        }
        else
        {
            analyzeUrl = baseUrl + analyzePath;
        }

        return true;
    }

    private string GetAnalyzePath()
    {
        if (!string.IsNullOrWhiteSpace(analyzePathOverride))
        {
            return EnsureLeadingSlash(analyzePathOverride.Trim());
        }

        return backendKind == VoiceEvaluationBackendKind.LegendaryVowels
            ? LegendaryAnalyzePath
            : StandardAnalyzePath;
    }

    private string GetApiPrefixPath()
    {
        if (!string.IsNullOrWhiteSpace(analyzePathOverride))
        {
            string path = EnsureLeadingSlash(analyzePathOverride.Trim()).TrimEnd('/');
            return path.EndsWith("/analyze", StringComparison.OrdinalIgnoreCase)
                ? path.Substring(0, path.Length - "/analyze".Length)
                : path;
        }

        return backendKind == VoiceEvaluationBackendKind.LegendaryVowels
            ? LegendaryApiPrefixPath
            : StandardApiPrefixPath;
    }

    private static string EnsureLeadingSlash(string value)
    {
        return value.StartsWith("/", StringComparison.Ordinal) ? value : "/" + value;
    }

    private string BuildUnityCompatibleResponseJson(string responseJson)
    {
        if (backendKind != VoiceEvaluationBackendKind.LegendaryVowels)
        {
            return responseJson;
        }

        try
        {
            var legendaryResponse = JsonUtility.FromJson<LegendaryVoiceEvaluationResponse>(responseJson);
            if (legendaryResponse == null || string.IsNullOrWhiteSpace(legendaryResponse.requestId))
            {
                return responseJson;
            }

            return JsonUtility.ToJson(ToUnityContractResponse(legendaryResponse));
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning("QuestVoiceEvaluationDemo: Legendary response adapter failed. " + exception.Message);
            return responseJson;
        }
    }

    private static VoiceEvaluationResponse ToUnityContractResponse(LegendaryVoiceEvaluationResponse source)
    {
        return new VoiceEvaluationResponse
        {
            apiVersion = string.IsNullOrWhiteSpace(source.apiVersion) ? "v1" : source.apiVersion,
            requestId = source.requestId,
            sessionId = source.sessionId,
            attemptId = source.attemptId,
            mode = source.mode,
            analysisStatus = source.analysisStatus,
            requiresRetry = source.requiresRetry,
            retryReason = source.retryReason,
            targetText = source.targetText,
            transcript = source.transcript,
            confidenceNote = source.confidenceNote,
            stt = source.stt,
            wordResults = source.wordResults ?? Array.Empty<WordComparisonResult>(),
            metrics = source.metrics,
            score = ToUnityRuleScore(source.score),
            feedback = ToUnityFeedback(source.feedback),
        };
    }

    private static RuleScore ToUnityRuleScore(LegendaryEvaluationScore source)
    {
        if (source == null)
        {
            return null;
        }

        return new RuleScore
        {
            overallScore = Mathf.RoundToInt(source.overallScore),
            accuracyScore = Mathf.RoundToInt(source.textMatchScore > 0f ? source.textMatchScore : source.accuracyScore),
            deliveryScore = Mathf.RoundToInt(source.deliveryScore),
            contentScore = Mathf.RoundToInt(source.contentScore),
            taskScore = Mathf.RoundToInt(source.taskScore),
        };
    }

    private static Feedback ToUnityFeedback(LegendaryFeedback source)
    {
        if (source == null)
        {
            return null;
        }

        return new Feedback
        {
            status = source.status,
            summary = source.summary,
            strengths = source.strengths ?? Array.Empty<string>(),
            practiceItems = ToUnityPracticeItems(source.practiceItems),
            nextAction = source.nextAction,
        };
    }

    private static string[] ToUnityPracticeItems(LegendaryPracticeItem[] source)
    {
        if (source == null || source.Length == 0)
        {
            return Array.Empty<string>();
        }

        var items = new List<string>();
        foreach (var item in source)
        {
            if (item == null)
            {
                continue;
            }

            string text = !string.IsNullOrWhiteSpace(item.tip)
                ? item.tip
                : item.expected;
            if (!string.IsNullOrWhiteSpace(text))
            {
                items.Add(text);
            }
        }

        return items.ToArray();
    }

    private void MirrorVoiceDebugInput(string attemptId, int sampleFrames, int wavBytes)
    {
        if (!mirrorDebugToSushiSite)
        {
            return;
        }

        var payload = new VoiceDebugRelayPayload
        {
            stage = "input",
            sessionId = sessionId,
            attemptId = attemptId,
            mode = mode,
            targetText = TargetText,
            clientVersion = clientVersion,
            backendUrl = backendBaseUrl,
            backendKind = backendKind.ToString(),
            sampleRate = sampleRate,
            sampleFrames = sampleFrames,
            wavBytes = wavBytes,
            micStatus = lastMicDebugStatus,
            unityRealtime = Time.realtimeSinceStartup,
        };
        StartCoroutine(PostVoiceDebugPayload(payload));
    }

    private void MirrorVoiceDebugOutput(
        string attemptId,
        string rawResponseJson,
        string responseJson,
        string transcript
    )
    {
        if (!mirrorDebugToSushiSite)
        {
            return;
        }

        var payload = new VoiceDebugRelayPayload
        {
            stage = "output",
            sessionId = sessionId,
            attemptId = attemptId,
            mode = mode,
            targetText = TargetText,
            clientVersion = clientVersion,
            backendUrl = backendBaseUrl,
            backendKind = backendKind.ToString(),
            transcript = transcript,
            rawResponseJson = rawResponseJson,
            responseJson = responseJson,
            unityRealtime = Time.realtimeSinceStartup,
        };
        StartCoroutine(PostVoiceDebugPayload(payload));
    }

    private void MirrorVoiceDebugError(string message)
    {
        if (!mirrorDebugToSushiSite)
        {
            return;
        }

        var payload = new VoiceDebugRelayPayload
        {
            stage = "error",
            sessionId = sessionId,
            attemptId = attemptCounter > 0 ? $"attempt-{attemptCounter:000}" : string.Empty,
            mode = mode,
            targetText = TargetText,
            clientVersion = clientVersion,
            backendUrl = backendBaseUrl,
            backendKind = backendKind.ToString(),
            error = message,
            micStatus = lastMicDebugStatus,
            unityRealtime = Time.realtimeSinceStartup,
        };
        StartCoroutine(PostVoiceDebugPayload(payload));
    }

    private IEnumerator PostVoiceDebugPayload(VoiceDebugRelayPayload payload)
    {
        if (!TryBuildSushiDebugUrl(out string url))
        {
            yield break;
        }

        string json = JsonUtility.ToJson(payload);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Max(1, sushiDebugTimeoutSeconds);
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogDebug($"Sushi debug mirror failed. url={url} result={request.result} error={request.error}");
            }
        }
    }

    private bool TryBuildSushiDebugUrl(out string url)
    {
        url = string.Empty;
        string baseUrl = sushiDebugBaseUrl != null ? sushiDebugBaseUrl.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return false;
        }

        string path = string.IsNullOrWhiteSpace(sushiDebugPath)
            ? "/unity/voice-debug"
            : EnsureLeadingSlash(sushiDebugPath.Trim());
        url = baseUrl.TrimEnd('/') + path;
        return true;
    }

    private static string NormalizeKnownText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value == "\u81FE\uBA85\uC195" ? "\uBB38\uD654" : value;
    }

    [Serializable]
    private sealed class VoiceDebugRelayPayload
    {
        public string stage;
        public string sessionId;
        public string attemptId;
        public string mode;
        public string targetText;
        public string clientVersion;
        public string backendUrl;
        public string backendKind;
        public int sampleRate;
        public int sampleFrames;
        public int wavBytes;
        public string micStatus;
        public string transcript;
        public string error;
        public string rawResponseJson;
        public string responseJson;
        public float unityRealtime;
    }

    private void ReportError(string message)
    {
        lastError = message;
        MirrorVoiceDebugError(message);
        onError?.Invoke(message);
        Debug.LogError("QuestVoiceEvaluationDemo: " + message);
    }

    private void LogDebug(string message)
    {
        if (!logDebugOutput)
        {
            return;
        }

        Debug.Log("QuestVoiceEvaluationDemo: " + message);
    }

    private void LogResponseJson(string responseJson)
    {
        if (!logResponseJson)
        {
            return;
        }

        LogDebug("STT response JSON=" + TrimForResponseJsonLog(responseJson));
    }

    private string TrimForResponseJsonLog(string responseJson)
    {
        if (string.IsNullOrEmpty(responseJson) || responseJsonLogMaxChars <= 0)
        {
            return responseJson;
        }

        if (responseJson.Length <= responseJsonLogMaxChars)
        {
            return responseJson;
        }

        return responseJson.Substring(0, responseJsonLogMaxChars) + "...";
    }

    private void StartMicDebugRoutine()
    {
        StopMicDebugRoutine();

        if (!emitMicInputDebug)
        {
            return;
        }

        micDebugRoutine = StartCoroutine(EmitMicInputDebugLoop());
    }

    private void StopMicDebugRoutine()
    {
        if (micDebugRoutine == null)
        {
            return;
        }

        StopCoroutine(micDebugRoutine);
        micDebugRoutine = null;
    }

    private IEnumerator EmitMicInputDebugLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.05f, micDebugIntervalSeconds));

        while (isRecording && recordingClip != null)
        {
            EmitMicDebug(BuildMicInputDebugStatus());
            yield return wait;
        }
    }

    private string BuildMicInputDebugStatus()
    {
        int samplePosition = Microphone.GetPosition(microphoneDevice);
        int channels = Mathf.Max(1, recordingClip.channels);
        int windowFrames = Mathf.Clamp(micDebugSampleWindow, 1, Mathf.Max(1, samplePosition));
        int startFrame = Mathf.Max(0, samplePosition - windowFrames);
        int frameCount = samplePosition - startFrame;
        float rms = 0f;
        float peak = 0f;

        if (frameCount > 0)
        {
            var samples = new float[frameCount * channels];
            recordingClip.GetData(samples, startFrame);

            double sumSquares = 0d;
            for (var index = 0; index < samples.Length; index++)
            {
                float value = samples[index];
                float abs = Mathf.Abs(value);
                if (abs > peak)
                {
                    peak = abs;
                }

                sumSquares += value * value;
            }

            rms = Mathf.Sqrt((float)(sumSquares / samples.Length));
        }

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - recordingStartedAt);
        return $"MIC raw: pos={samplePosition} elapsed={elapsed:0.0}s rms={rms:0.0000} peak={peak:0.0000}";
    }

    private void EmitMicDebug(string message)
    {
        if (!emitMicInputDebug)
        {
            return;
        }

        lastMicDebugStatus = message;
        onMicInputDebug?.Invoke(message);
        LogDebug(message);
    }
}

public static class WavEncoder
{
    private const short PcmBitDepth = 16;

    public static byte[] Encode(AudioClip clip, int sampleFrames)
    {
        int channels = clip.channels;
        int clampedFrames = Mathf.Clamp(sampleFrames, 0, clip.samples);
        var samples = new float[clampedFrames * channels];
        clip.GetData(samples, 0);

        byte[] pcmBytes = ConvertToPcm16(samples);
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            int byteRate = clip.frequency * channels * PcmBitDepth / 8;
            short blockAlign = (short)(channels * PcmBitDepth / 8);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmBytes.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(clip.frequency);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(PcmBitDepth);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmBytes.Length);
            writer.Write(pcmBytes);

            return stream.ToArray();
        }
    }

    private static byte[] ConvertToPcm16(IReadOnlyList<float> samples)
    {
        var bytes = new byte[samples.Count * 2];

        for (int index = 0; index < samples.Count; index++)
        {
            float clamped = Mathf.Clamp(samples[index], -1f, 1f);
            short value = (short)Mathf.RoundToInt(clamped * short.MaxValue);
            bytes[index * 2] = (byte)(value & 0xff);
            bytes[index * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return bytes;
    }
}

[Serializable]
public sealed class VoiceEvaluationResponse
{
    public string apiVersion;
    public string requestId;
    public string sessionId;
    public string attemptId;
    public string mode;
    public string analysisStatus;
    public bool requiresRetry;
    public string retryReason;
    public string targetText;
    public string transcript;
    public string confidenceNote;
    public SttSummary stt;
    public WordComparisonResult[] wordResults;
    public AudioMetrics metrics;
    public RuleScore score;
    public Feedback feedback;
}

[Serializable]
public sealed class SttSummary
{
    public string provider;
    public string model;
    public bool verificationUsed;
    public bool verificationAgreement;
}

[Serializable]
public sealed class WordComparisonResult
{
    public int targetIndex;
    public int transcriptIndex;
    public string expected;
    public string recognized;
    public string status;
    public bool accepted;
    public string acceptanceReason;
    public float similarity;
    public SyllableComparisonResult[] syllableResults;
}

[Serializable]
public sealed class SyllableComparisonResult
{
    public int targetIndex;
    public int transcriptIndex;
    public string expected;
    public string recognized;
    public string status;
}

[Serializable]
public sealed class AudioMetrics
{
    public float durationSec;
    public float speechDurationSec;
    public float silenceDurationSec;
    public float silenceRatio;
    public int longPauseCount;
    public float averageRms;
    public float peakRms;
    public float speakingRateCpm;
    public int fillerCount;
}

[Serializable]
public sealed class RuleScore
{
    public int overallScore;
    public int accuracyScore;
    public int deliveryScore;
    public int contentScore;
    public int taskScore;
}

[Serializable]
public sealed class Feedback
{
    public string status;
    public string summary;
    public string[] strengths;
    public string[] practiceItems;
    public string nextAction;
}

[Serializable]
public sealed class LegendaryVoiceEvaluationResponse
{
    public string apiVersion;
    public string requestId;
    public string sessionId;
    public string attemptId;
    public string mode;
    public string analysisStatus;
    public bool requiresRetry;
    public string retryReason;
    public bool needsRepractice;
    public string targetText;
    public string transcript;
    public string confidenceNote;
    public SttSummary stt;
    public WordComparisonResult[] wordResults;
    public AudioMetrics metrics;
    public LegendaryEvaluationScore score;
    public LegendaryFeedback feedback;
}

[Serializable]
public sealed class LegendaryEvaluationScore
{
    public float overallScore;
    public float textMatchScore;
    public float timingScore;
    public float pauseScore;
    public float fluencyScore;
    public float deliveryScore;
    public float accuracyScore;
    public float contentScore;
    public float taskScore;
    public string scoreBasis;
    public int matchedSyllableCount;
    public int totalTargetSyllableCount;
}

[Serializable]
public sealed class LegendaryFeedback
{
    public string status;
    public string summary;
    public string[] strengths;
    public LegendaryPracticeItem[] practiceItems;
    public string nextAction;
}

[Serializable]
public sealed class LegendaryPracticeItem
{
    public string expected;
    public string recognized;
    public string practiceResourceId;
    public string articulationTipId;
    public string tip;
}
