using System;
using UnityEditor;
using UnityEngine;

public static class PlayerSkinPrefabSetupUtility
{
    private const string BabySquidPrefabPath = "Assets/Content/Prefabs/Player/BabySquid.prefab";
    private const string SkinMountName = "SkinMount";

    [MenuItem("Tools/Squid/Player/Setup Skin Mount")]
    public static void SetupSkinMount()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(BabySquidPrefabPath);
        try
        {
            PlayerVisualStateController visualStateController = prefabRoot.GetComponent<PlayerVisualStateController>();
            if (visualStateController == null)
            {
                throw new InvalidOperationException("[PlayerSkinPrefabSetupUtility] BabySquid requiere PlayerVisualStateController.");
            }

            Transform skinMount = EnsureSkinMount(prefabRoot.transform);
            PlayerSkinApplier skinApplier = prefabRoot.GetComponent<PlayerSkinApplier>();
            if (skinApplier == null)
            {
                skinApplier = prefabRoot.AddComponent<PlayerSkinApplier>();
            }

            SerializedObject serializedApplier = new(skinApplier);
            serializedApplier.FindProperty("visualStateController").objectReferenceValue = visualStateController;
            serializedApplier.FindProperty("skinMount").objectReferenceValue = skinMount;
            serializedApplier.FindProperty("applyOnEnable").boolValue = true;
            serializedApplier.FindProperty("listenToProfileChanges").boolValue = true;
            serializedApplier.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(skinMount.gameObject);
            EditorUtility.SetDirty(skinApplier);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, BabySquidPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerSkinPrefabSetupUtility] SkinMount and PlayerSkinApplier configured on BabySquid.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Transform EnsureSkinMount(Transform playerRoot)
    {
        Transform existing = playerRoot.Find(SkinMountName);
        if (existing != null)
        {
            return existing;
        }

        GameObject mount = new(SkinMountName);
        mount.layer = playerRoot.gameObject.layer;
        Transform mountTransform = mount.transform;
        mountTransform.SetParent(playerRoot, false);
        mountTransform.localPosition = Vector3.zero;
        mountTransform.localRotation = Quaternion.identity;
        mountTransform.localScale = Vector3.one;
        return mountTransform;
    }
}
