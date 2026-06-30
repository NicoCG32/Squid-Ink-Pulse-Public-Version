using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerVisualStateController))]
public sealed class PlayerSkinApplier : MonoBehaviour
{
    private const string SkinMountName = "SkinMount";

    [Header("References")]
    [SerializeField] private PlayerVisualStateController visualStateController;
    [SerializeField] private Transform skinMount;

    [Header("Runtime")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool listenToProfileChanges = true;

    private readonly HashSet<string> missingSkinPrefabPaths = new();
    private GameObject activeSkinInstance;
    private string activeSkinId;
    private string activeSkinResourcePath;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (listenToProfileChanges)
        {
            PersistentPlayerProfile.ProfileChanged += HandleProfileChanged;
        }

        if (applyOnEnable)
        {
            ApplyEquippedSkin();
        }
    }

    private void Start()
    {
        if (applyOnEnable)
        {
            ApplyEquippedSkin();
        }
    }

    private void OnDisable()
    {
        PersistentPlayerProfile.ProfileChanged -= HandleProfileChanged;
    }

    public void ApplyEquippedSkin()
    {
        ResolveReferences();

        UnlockableSkinDefinition equippedSkin = UnlockablesCatalogQuery.GetEquippedSkin();
        string skinId = equippedSkin?.id ?? PlayerSkinIds.Default;
        string resourcePath = equippedSkin?.playerSkinPrefabResourcePath;

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            ClearActiveSkin();
            return;
        }

        string normalizedPath = NormalizeResourcePath(resourcePath);
        if (activeSkinInstance != null
            && string.Equals(activeSkinId, skinId, StringComparison.Ordinal)
            && string.Equals(activeSkinResourcePath, normalizedPath, StringComparison.Ordinal))
        {
            return;
        }

        GameObject skinPrefab = Resources.Load<GameObject>(normalizedPath);
        if (skinPrefab == null)
        {
            if (missingSkinPrefabPaths.Add(normalizedPath))
            {
                Debug.LogWarning($"[PlayerSkinApplier] No se encontro el prefab de skin Resources/{normalizedPath}. Se usara el visual base.", this);
            }

            ClearActiveSkin();
            return;
        }

        ReplaceActiveSkin(skinPrefab, skinId, normalizedPath);
    }

    public void ClearActiveSkin()
    {
        DestroyActiveSkinInstance();
        activeSkinId = null;
        activeSkinResourcePath = null;

        if (visualStateController != null)
        {
            visualStateController.UseDefaultVisualSet();
        }
    }

    private void ReplaceActiveSkin(GameObject skinPrefab, string skinId, string resourcePath)
    {
        DestroyActiveSkinInstance();

        Transform mount = ResolveSkinMount();
        activeSkinInstance = Instantiate(skinPrefab, mount);
        activeSkinInstance.name = skinPrefab.name;
        activeSkinInstance.transform.localPosition = Vector3.zero;
        activeSkinInstance.transform.localRotation = Quaternion.identity;
        activeSkinInstance.transform.localScale = Vector3.one;
        SetLayerRecursively(activeSkinInstance, gameObject.layer);

        PlayerSkinVisualSet visualSet = activeSkinInstance.GetComponentInChildren<PlayerSkinVisualSet>(includeInactive: true);
        if (visualSet == null)
        {
            visualSet = activeSkinInstance.AddComponent<PlayerSkinVisualSet>();
        }

        visualSet.ResolveReferences();
        if (visualStateController == null || !visualStateController.ApplySkinVisualSet(visualSet))
        {
            Debug.LogWarning(
                $"[PlayerSkinApplier] La skin '{skinId}' no contiene un set visual completo. Requiere SquidVisual/MovementVisual, InkPulseVisual y PortalVisual.",
                activeSkinInstance);
            ClearActiveSkin();
            return;
        }

        activeSkinId = skinId;
        activeSkinResourcePath = resourcePath;
    }

    private void DestroyActiveSkinInstance()
    {
        if (activeSkinInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(activeSkinInstance);
        }
        else
        {
            DestroyImmediate(activeSkinInstance);
        }

        activeSkinInstance = null;
    }

    private Transform ResolveSkinMount()
    {
        if (skinMount != null)
        {
            return skinMount;
        }

        Transform existingMount = transform.Find(SkinMountName);
        if (existingMount != null)
        {
            skinMount = existingMount;
            return skinMount;
        }

        GameObject mount = new(SkinMountName);
        mount.layer = gameObject.layer;
        skinMount = mount.transform;
        skinMount.SetParent(transform, false);
        return skinMount;
    }

    private void ResolveReferences()
    {
        if (visualStateController == null)
        {
            visualStateController = GetComponent<PlayerVisualStateController>();
        }

        if (skinMount == null)
        {
            Transform existingMount = transform.Find(SkinMountName);
            if (existingMount != null)
            {
                skinMount = existingMount;
            }
        }
    }

    private void HandleProfileChanged(PlayerProfileSaveData _)
    {
        ApplyEquippedSkin();
    }

    private static string NormalizeResourcePath(string resourcePath)
    {
        string normalizedPath = resourcePath.Trim().Replace('\\', '/');
        return normalizedPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            ? normalizedPath.Substring(0, normalizedPath.Length - 7)
            : normalizedPath;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int index = 0; index < children.Length; index++)
        {
            children[index].gameObject.layer = layer;
        }
    }
}
