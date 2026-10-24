using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class InGameShopMobileCompositionContract
{
    public static void Validate(GameObject root,List<string> failures)
    {
        if (root==null) { failures.Add("InGameShop: missing root."); return; }
        RectTransform safe=root.transform.Find("InGameShopSafeArea") as RectTransform;
        RectTransform content=safe!=null ? safe.Find("InGameShopContent") as RectTransform : null;
        InGameShopMobilePresentation presentation=root.GetComponent<InGameShopMobilePresentation>();
        if (safe==null || content==null || presentation==null) { failures.Add("InGameShop: missing safe/mobile layout."); return; }
        if (safe.GetComponent<SafeAreaAdapter>()?.Target!=safe || content.GetComponent<MobileReferenceContentFitter>()==null)
            failures.Add("InGameShop: invalid safe area/fitter.");
        foreach (RectTransform rect in new[] { safe,content })
            if (rect.anchorMin!=Vector2.zero || rect.anchorMax!=Vector2.one || rect.offsetMin!=Vector2.zero
                || rect.offsetMax!=Vector2.zero || rect.localScale!=Vector3.one || rect.localRotation!=Quaternion.identity)
                failures.Add($"InGameShop/{rect.name}: preserve authored stretch.");
        Transform background=root.transform.Find("FondoBurbuja");
        if (background==null || !background.GetComponent<Image>().raycastTarget || background.GetSiblingIndex()!=0)
            failures.Add("InGameShop: missing full-viewport input blocker.");
        foreach (string name in new[] { "ComprarBoton","Gadget","ShopDecoration","ShrimpDecorator","SinSaldo","TiempoMovil","CompraMovil" })
        {
            Transform[] matches=root.GetComponentsInChildren<Transform>(true).Where(item=>item.name==name).ToArray();
            if (matches.Length!=1 || !matches[0].IsChildOf(content)) failures.Add($"InGameShop/{name}: must belong to safe content exactly once.");
        }
        Transform buy=content.Find("ComprarBoton");
        if (buy==null || buy.Find("SinSaldo")==null) failures.Add("InGameShop: SinSaldo must be independent of button visual state.");
        var serialized=new SerializedObject(presentation);
        SerializedProperty hints=serialized.FindProperty("keyboardHints");
        if (hints.arraySize!=3) failures.Add("InGameShop: wire all three keyboard hint states.");
        var unique=new HashSet<GameObject>();
        for (int i=0;i<hints.arraySize;i++)
        {
            GameObject hint=hints.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (hint==null || hint.name!="B" || !hint.transform.IsChildOf(buy) || !unique.Add(hint)) failures.Add("InGameShop: invalid keyboard hint.");
        }
        foreach (string field in new[] { "mobileBuyLabel","mobileTimerRoot" })
        {
            GameObject mobile=serialized.FindProperty(field).objectReferenceValue as GameObject;
            if (mobile==null || mobile.activeSelf || !mobile.transform.IsChildOf(content)) failures.Add($"InGameShop/{field}: keep hidden in authored desktop layout.");
        }
        if (root.GetComponentsInChildren<Canvas>(true).Length!=1 || root.GetComponentsInChildren<GraphicRaycaster>(true).Length!=1
            || root.GetComponentsInChildren<SafeAreaAdapter>(true).Length!=1 || root.GetComponentsInChildren<EventSystem>(true).Length!=0)
            failures.Add("InGameShop: unexpected UI infrastructure.");
    }
}
