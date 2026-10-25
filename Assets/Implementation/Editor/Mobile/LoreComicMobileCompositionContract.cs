using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LoreComicMobileCompositionContract
{
    public static void Validate(LoreComicPresenter presenter,List<string> failures)
    {
        if (presenter==null) { failures.Add("Lore: missing presenter."); return; }
        var serialized=new SerializedObject(presenter);
        GameObject root=serialized.FindProperty("comicRoot").objectReferenceValue as GameObject;
        Image image=serialized.FindProperty("comicImage").objectReferenceValue as Image;
        Button button=serialized.FindProperty("continueButton").objectReferenceValue as Button;
        GameObject buttonRoot=serialized.FindProperty("continueButtonRoot").objectReferenceValue as GameObject;
        if (root==null || image==null || button==null || buttonRoot==null)
        { failures.Add("Lore: incomplete serialized view."); return; }
        RectTransform safe=root.transform.Find("LoreSafeArea") as RectTransform;
        RectTransform content=safe!=null ? safe.Find("LoreContent") as RectTransform : null;
        if (safe==null || content==null) { failures.Add("Lore: missing safe content."); return; }
        if (safe.GetComponent<SafeAreaAdapter>()?.Target!=safe || content.GetComponent<MobileReferenceContentFitter>()==null)
            failures.Add("Lore: invalid safe area/fitter.");
        foreach (RectTransform rect in new[] {safe,content})
            if (rect.anchorMin!=Vector2.zero || rect.anchorMax!=Vector2.one || rect.offsetMin!=Vector2.zero
                || rect.offsetMax!=Vector2.zero || rect.localScale!=Vector3.one)
                failures.Add($"Lore/{rect.name}: preserve authored stretch.");
        if (image.transform.parent!=content || buttonRoot.transform.parent!=content || !image.preserveAspect)
            failures.Add("Lore: protect vignette/continue and preserve sprite aspect.");
        LoreComicMobileLayout layout=content.GetComponent<LoreComicMobileLayout>();
        if (layout==null) failures.Add("Lore: missing mobile geometry.");
        else
        {
            var binding=new SerializedObject(layout);
            if (binding.FindProperty("comicImage").objectReferenceValue!=image
                || binding.FindProperty("continueRoot").objectReferenceValue!=buttonRoot.transform)
                failures.Add("Lore: mobile geometry bound to another view.");
        }
        LoreComicContinueFocus focus=root.GetComponent<LoreComicContinueFocus>();
        if (focus==null || new SerializedObject(focus).FindProperty("button").objectReferenceValue!=button)
            failures.Add("Lore: bind contextual Submit to the real continue button.");
        Transform dimmer=root.transform.Find("Dimmer");
        if (dimmer==null || dimmer.GetComponent<Image>()==null || !dimmer.GetComponent<Image>().raycastTarget
            || dimmer.GetSiblingIndex()!=0) failures.Add("Lore: preserve full-viewport dimmer/input blocker.");
        if (root.GetComponentsInChildren<Canvas>(true).Length!=1 || root.GetComponentsInChildren<GraphicRaycaster>(true).Length!=1
            || root.GetComponentsInChildren<SafeAreaAdapter>(true).Length!=1 || root.GetComponentsInChildren<EventSystem>(true).Length!=0)
            failures.Add("Lore: unexpected UI infrastructure.");
    }
}
