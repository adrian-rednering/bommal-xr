using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneObjectUtility
{
    public static Transform FindInActiveScene(string objectName, bool ignoreCase = false)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var found = FindChildRecursive(root.transform, objectName, ignoreCase);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public static Transform FindChildRecursive(Transform parent, string childName, bool ignoreCase = false)
    {
        if (parent == null)
        {
            return null;
        }

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(parent.name, childName, comparison))
        {
            return parent;
        }

        for (var index = 0; index < parent.childCount; index++)
        {
            var found = FindChildRecursive(parent.GetChild(index), childName, ignoreCase);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    // Activates the panel only when it is the target, so a group of panels shows exactly one.
    public static void SetPanelActive(GameObject panel, GameObject targetPanel)
    {
        if (panel != null)
        {
            panel.SetActive(panel == targetPanel);
        }
    }
}
