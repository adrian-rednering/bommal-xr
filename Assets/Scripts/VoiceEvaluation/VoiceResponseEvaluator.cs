using System;
using UnityEngine;

public static class VoiceResponseEvaluator
{
    public static VoiceEvaluationResponse Parse(string responseJson, string logOwner)
    {
        try
        {
            return JsonUtility.FromJson<VoiceEvaluationResponse>(responseJson);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(logOwner + ": voice response parse failed. " + exception.Message);
            return null;
        }
    }

    // Returns false when the response carries neither word results nor a score,
    // so the caller can fall back to its own check.
    // acceptFuzzyMatches: count words the backend marked as near-matches (accepted) as correct.
    public static bool TryEvaluate(
        VoiceEvaluationResponse response,
        int correctScoreThreshold,
        bool acceptFuzzyMatches,
        out bool isCorrect
    )
    {
        isCorrect = false;

        if (response == null)
        {
            return true;
        }

        if (
            response.requiresRetry
            || string.Equals(response.analysisStatus, "UNDETERMINED", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        if (response.wordResults != null && response.wordResults.Length > 0)
        {
            foreach (var wordResult in response.wordResults)
            {
                if (wordResult == null || string.IsNullOrWhiteSpace(wordResult.status))
                {
                    return true;
                }

                var wordCorrect = (acceptFuzzyMatches && wordResult.accepted)
                    || string.Equals(wordResult.status, "CORRECT", StringComparison.OrdinalIgnoreCase);
                if (!wordCorrect)
                {
                    return true;
                }
            }

            isCorrect = true;
            return true;
        }

        if (response.score != null)
        {
            isCorrect = response.score.overallScore >= correctScoreThreshold
                || response.score.accuracyScore >= correctScoreThreshold;
            return true;
        }

        return false;
    }
}
