using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UiButtonContract
{
    public const string ButtonChildName = "Button";
    public const string VisualChildName = "Visual";
    public const string NormalStateName = "Normal";
    public const string HighlightedStateName = "Destacado";
    public const string PressedStateName = "Presionado";

    public static Button FindButton(Transform root, params string[] contractNames)
    {
        if (root == null || contractNames == null)
        {
            return null;
        }

        foreach (string contractName in contractNames)
        {
            if (string.IsNullOrWhiteSpace(contractName))
            {
                continue;
            }

            Transform contractRoot = FindChildTransform(root, contractName);
            Button button = FindButtonInContractRoot(contractRoot);
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    public static RectTransform FindButtonRootRect(Transform root, params string[] contractNames)
    {
        if (root == null || contractNames == null)
        {
            return null;
        }

        foreach (string contractName in contractNames)
        {
            if (string.IsNullOrWhiteSpace(contractName))
            {
                continue;
            }

            Transform contractRoot = FindChildTransform(root, contractName);
            if (contractRoot != null && contractRoot.TryGetComponent(out RectTransform rectTransform))
            {
                return rectTransform;
            }
        }

        return null;
    }

    public static RectTransform[] FindButtonRootRects(Transform root, params string[] contractNames)
    {
        if (root == null || contractNames == null || contractNames.Length == 0)
        {
            return Array.Empty<RectTransform>();
        }

        List<RectTransform> results = new();
        foreach (string contractName in contractNames)
        {
            RectTransform rectTransform = FindButtonRootRect(root, contractName);
            if (rectTransform != null)
            {
                results.Add(rectTransform);
            }
        }

        return results.ToArray();
    }

    public static bool IsCompliantButton(Button button)
    {
        if (button == null || button.name != ButtonChildName || button.transform.parent == null)
        {
            return false;
        }

        Transform contractRoot = button.transform.parent;
        if (!contractRoot.name.EndsWith("Boton", StringComparison.Ordinal))
        {
            return false;
        }

        Transform visual = contractRoot.Find(VisualChildName);
        return visual != null
            && visual.Find(NormalStateName) != null
            && visual.Find(HighlightedStateName) != null
            && visual.Find(PressedStateName) != null
            && button.GetComponent<ButtonVisualState>() != null;
    }

    public static Button FindButtonInContractRoot(Transform contractRoot)
    {
        if (contractRoot == null)
        {
            return null;
        }

        Transform buttonChild = contractRoot.Find(ButtonChildName);
        if (buttonChild != null && buttonChild.TryGetComponent(out Button childButton))
        {
            return childButton;
        }

        return contractRoot.TryGetComponent(out Button legacyButton) ? legacyButton : null;
    }

    private static Transform FindChildTransform(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }
}
