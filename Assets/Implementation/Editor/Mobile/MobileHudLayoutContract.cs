using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Compara contenedores de layout. Los efectos, sprites y presenters internos
// pueden especializarse por zona sin definir una geometria HUD diferente.
public static class MobileHudLayoutContract
{
    private static readonly string[] ContainerNames =
    {
        "GadgetSlots", "InkBar", "Score", "ShrimpCounter", "TouchControlsOwner"
    };

    public static void Validate(
        Transform canonicalSafeArea,
        Transform candidateSafeArea,
        string context,
        List<string> failures)
    {
        if (canonicalSafeArea == null || candidateSafeArea == null)
        {
            failures.Add($"{context}: falta la raiz segura para comparar el HUD comun.");
            return;
        }

        ValidateSafeArea(canonicalSafeArea, "HUD canonico", failures);
        ValidateSafeArea(candidateSafeArea, context, failures);

        foreach (string name in ContainerNames)
        {
            RectTransform expected = canonicalSafeArea.Find(name) as RectTransform;
            RectTransform actual = candidateSafeArea.Find(name) as RectTransform;
            if (expected == null || actual == null)
            {
                failures.Add($"{context}/{name}: falta el contenedor RectTransform del HUD comun.");
                continue;
            }

            Compare(expected.anchorMin, actual.anchorMin, context, name, "anchorMin", failures);
            Compare(expected.anchorMax, actual.anchorMax, context, name, "anchorMax", failures);
            Compare(expected.anchoredPosition, actual.anchoredPosition, context, name, "anchoredPosition", failures);
            Compare(expected.sizeDelta, actual.sizeDelta, context, name, "sizeDelta", failures);
            Compare(expected.pivot, actual.pivot, context, name, "pivot", failures);
            if ((expected.localScale - actual.localScale).sqrMagnitude > 0.000001f)
            {
                failures.Add($"{context}/{name}: localScale diverge del HUD canonico.");
            }

            if (Quaternion.Angle(expected.localRotation, actual.localRotation) > 0.01f)
            {
                failures.Add($"{context}/{name}: localRotation diverge del HUD canonico.");
            }

            string expectedSource = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(expected.gameObject);
            string actualSource = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actual.gameObject);
            if (string.IsNullOrEmpty(expectedSource) || expectedSource != actualSource)
            {
                failures.Add($"{context}/{name}: debe reutilizar el prefab HUD canonico {expectedSource}.");
            }
        }
    }

    private static void ValidateSafeArea(Transform root, string context, List<string> failures)
    {
        if (root is not RectTransform rect)
        {
            failures.Add($"{context}/SafeAreaRoot: falta RectTransform.");
            return;
        }

        // Se valida la geometria de autoria antes de aplicar Screen.safeArea.
        Compare(Vector2.zero, rect.anchorMin, context, "SafeAreaRoot", "anchorMin", failures);
        Compare(Vector2.one, rect.anchorMax, context, "SafeAreaRoot", "anchorMax", failures);
        Compare(Vector2.zero, rect.offsetMin, context, "SafeAreaRoot", "offsetMin", failures);
        Compare(Vector2.zero, rect.offsetMax, context, "SafeAreaRoot", "offsetMax", failures);
        if ((rect.localScale - Vector3.one).sqrMagnitude > 0.000001f)
        {
            failures.Add($"{context}/SafeAreaRoot: localScale debe ser (1, 1, 1).");
        }

        if (Quaternion.Angle(rect.localRotation, Quaternion.identity) > 0.01f)
        {
            failures.Add($"{context}/SafeAreaRoot: localRotation debe ser identidad.");
        }
    }

    private static void Compare(
        Vector2 expected, Vector2 actual, string context, string name, string property,
        List<string> failures)
    {
        if ((expected - actual).sqrMagnitude > 0.000001f)
        {
            failures.Add($"{context}/{name}: {property} diverge del HUD canonico; esperado {expected}, actual {actual}.");
        }
    }
}
