using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopProductInfoSetupUtility
{
    private const string ShopMenuScenePath = "Assets/Scenes/ShopMenu/ShopMenu.unity";
    private const string ProductInfoBlockName = "ProductInfoBlock";
    private const string NameTextName = "NombreProducto";
    private const string DescriptionTextName = "DescripcionProducto";
    private const string PriceTextName = "PrecioProducto";
    private const int UiLayer = 5;

    [MenuItem("Tools/Squid/Shop/Setup Product Info Block")]
    public static void SetupProductInfoBlock()
    {
        Scene scene = EditorSceneManager.OpenScene(ShopMenuScenePath, OpenSceneMode.Single);
        OutOfGameShopManager manager = FindSceneComponent<OutOfGameShopManager>(scene);
        if (manager == null)
        {
            throw new InvalidOperationException("[ShopProductInfoSetupUtility] ShopMenu requiere exactamente un OutOfGameShopManager.");
        }

        Transform panel = manager.transform.Find("Panel");
        if (panel == null)
        {
            throw new InvalidOperationException("[ShopProductInfoSetupUtility] No se encontro Canvas/Panel en ShopMenu.");
        }

        RectTransform block = EnsureProductInfoBlock(panel);
        TMP_Text nameText = EnsureText(block, NameTextName, TextAlignmentOptions.Top, new Vector2(0f, 0.64f), new Vector2(1f, 1f));
        TMP_Text descriptionText = EnsureText(block, DescriptionTextName, TextAlignmentOptions.Center, new Vector2(0f, 0.22f), new Vector2(1f, 0.64f));
        TMP_Text priceText = EnsureText(block, PriceTextName, TextAlignmentOptions.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0.22f));

        SerializedObject serializedManager = new(manager);
        serializedManager.FindProperty("selectedItemNameText").objectReferenceValue = nameText;
        serializedManager.FindProperty("selectedItemDescriptionText").objectReferenceValue = descriptionText;
        serializedManager.FindProperty("selectedItemPriceText").objectReferenceValue = priceText;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopProductInfoSetupUtility] ProductInfoBlock and Inspector references serialized. No existing ShopMenu art was modified.");
    }

    private static RectTransform EnsureProductInfoBlock(Transform panel)
    {
        Transform existing = panel.Find(ProductInfoBlockName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject blockObject = new GameObject(ProductInfoBlockName, typeof(RectTransform));
        blockObject.layer = UiLayer;
        RectTransform block = blockObject.GetComponent<RectTransform>();
        block.SetParent(panel, false);
        block.anchorMin = new Vector2(0.5f, 0.5f);
        block.anchorMax = new Vector2(0.5f, 0.5f);
        block.pivot = new Vector2(0.5f, 0.5f);
        block.anchoredPosition = Vector2.zero;
        block.sizeDelta = new Vector2(520f, 230f);
        return block;
    }

    private static TMP_Text EnsureText(
        RectTransform parent,
        string textName,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        Transform existing = parent.Find(textName);
        TextMeshProUGUI text;
        if (existing != null)
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                throw new InvalidOperationException($"[ShopProductInfoSetupUtility] {ProductInfoBlockName}/{textName} debe tener TextMeshProUGUI.");
            }
        }
        else
        {
            GameObject textObject = new GameObject(textName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = UiLayer;
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.raycastTarget = false;
            text.text = string.Empty;
        }

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = new Vector2(12f, 6f);
        rectTransform.offsetMax = new Vector2(-12f, -6f);
        text.alignment = alignment;
        return text;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(includeInactive: true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
