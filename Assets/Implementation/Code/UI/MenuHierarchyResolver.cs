using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

public static class MenuHierarchyResolver
{
    public static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName && children[i].TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    public static RectTransform[] FindChildRectTransforms(Transform root, params string[] childNames)
    {
        if (root == null || childNames == null || childNames.Length == 0)
        {
            return new RectTransform[0];
        }

        RectTransform[] results = new RectTransform[childNames.Length];
        int count = 0;
        for (int i = 0; i < childNames.Length; i++)
        {
            RectTransform rectTransform = FindChildComponent<RectTransform>(root, childNames[i]);
            if (rectTransform != null)
            {
                results[count] = rectTransform;
                count++;
            }
        }

        if (count == results.Length)
        {
            return results;
        }

        RectTransform[] compact = new RectTransform[count];
        for (int i = 0; i < count; i++)
        {
            compact[i] = results[i];
        }

        return compact;
    }

    public static TMP_Text FindChildTextByContract(Transform root, params string[] contractNames)
    {
        if (root == null || contractNames == null || contractNames.Length == 0)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].TryGetComponent(out TMP_Text text))
            {
                continue;
            }

            string normalizedChildName = NormalizeContractName(children[i].name);
            for (int contractIndex = 0; contractIndex < contractNames.Length; contractIndex++)
            {
                if (normalizedChildName == NormalizeContractName(contractNames[contractIndex]))
                {
                    return text;
                }
            }
        }

        return null;
    }

    public static Transform FindDirectChildTransform(Transform root, params string[] names)
    {
        if (root == null || names == null || names.Length == 0)
        {
            return null;
        }

        for (int childIndex = 0; childIndex < root.childCount; childIndex++)
        {
            Transform child = root.GetChild(childIndex);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (child.name == names[nameIndex])
                {
                    return child;
                }
            }
        }

        return null;
    }

    public static void RestoreScaleIfCollapsed(Transform target)
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

    private static string NormalizeContractName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        for (int i = 0; i < decomposed.Length; i++)
        {
            char character = decomposed[i];
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
