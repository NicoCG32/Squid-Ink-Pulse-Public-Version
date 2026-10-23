using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RunMenuMobileCompositionContract
{
    public static void Validate(GameObject root, bool pause, List<string> failures)
    {
        string label = pause ? "Pause" : "GameOver";
        if (root == null) { failures.Add($"{label}: missing menu root."); return; }
        Transform panel = pause ? root.transform : root.transform.Find("Panel");
        RectTransform safe = panel != null ? panel.Find(label + "SafeArea") as RectTransform : null;
        RectTransform content = safe != null ? safe.Find(label + "Content") as RectTransform : null;
        if (safe == null || content == null)
        {
            failures.Add($"{label}: missing safe area/content hierarchy.");
            return;
        }
        SafeAreaAdapter adapter = safe.GetComponent<SafeAreaAdapter>();
        MobileReferenceContentFitter fitter = content.GetComponent<MobileReferenceContentFitter>();
        if (adapter == null || adapter.Target != safe || fitter == null
            || new SerializedObject(fitter).FindProperty("referenceResolution").vector2Value != new Vector2(1920f, 1080f))
            failures.Add($"{label}: invalid safe area/reference fitter.");
        foreach (RectTransform rect in new[] { safe, content })
            if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one
                || rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero
                || rect.localScale != Vector3.one || rect.localRotation != Quaternion.identity)
                failures.Add($"{label}/{rect.name}: authored stretch geometry changed.");
        if (root.GetComponentsInChildren<Canvas>(true).Length != 1
            || root.GetComponentsInChildren<GraphicRaycaster>(true).Length != 1
            || root.GetComponentsInChildren<SafeAreaAdapter>(true).Length != 1
            || root.GetComponentsInChildren<EventSystem>(true).Length != 0)
            failures.Add($"{label}: unexpected UI infrastructure.");
        string[] names = pause ? new[] { "ReanudarBoton", "OpcionesBoton", "MenuBoton", "PauseDecoration" }
            : new[] { "ReintentarBoton", "MenuBoton", "GameOverDecoration", "PuntajeObtenido", "MáximoPuntaje" };
        foreach (string name in names)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1 || !matches[0].IsChildOf(content))
                failures.Add($"{label}/{name}: must belong to safe content exactly once.");
        }
        Image background = pause ? panel.Find("FondoBurbuja")?.GetComponent<Image>() : panel.GetComponent<Image>();
        if (background == null || background.transform.IsChildOf(safe) || !background.raycastTarget
            || (pause && background.transform.GetSiblingIndex() != 0))
            failures.Add($"{label}: keep full-screen blocking background outside safe content.");
    }
}
