using UnityEngine;
using UnityEngine.UI;

// On-screen text used to watch mic/STT state on the headset, where the console is not visible.
public static class DebugOverlayText
{
    public static Text Resolve(Text existing, Transform root, string objectName, bool show, bool ignoreCase)
    {
        if (!show)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return existing;
        }

        var text = existing;
        if (text == null && root != null)
        {
            var found = SceneObjectUtility.FindChildRecursive(root, objectName, ignoreCase);
            if (found != null)
            {
                text = found.GetComponent<Text>();
            }
        }

        if (text == null && root != null)
        {
            var debugObject = new GameObject(objectName, typeof(RectTransform));
            debugObject.transform.SetParent(root, false);
            text = debugObject.AddComponent<Text>();
        }

        if (text == null)
        {
            return null;
        }

        text.gameObject.SetActive(true);
        text.raycastTarget = false;
        text.alignment = TextAnchor.UpperCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.fontSize = 28;
        text.color = new Color(1f, 0.92f, 0.25f, 1f);

        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        var rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -24f);
        rectTransform.sizeDelta = new Vector2(680f, 96f);
        rectTransform.localScale = Vector3.one;

        return text;
    }

    public static string FormatTranscript(string transcript)
    {
        var value = string.IsNullOrWhiteSpace(transcript) ? "<empty>" : transcript.Trim();
        return "STT raw: " + value;
    }

    public static string Trim(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || maxChars <= 0 || value.Length <= maxChars)
        {
            return value;
        }

        return value.Substring(0, maxChars) + "...";
    }
}
