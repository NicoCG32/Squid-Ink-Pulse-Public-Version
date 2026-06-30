using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerSkinAssetBuilder
{
    private const string SourceRoot = "Assets/Content/Animations/Characters/BabySquid";
    private const string SkinPrefabFolder = "Assets/Content/Prefabs/Player/Resources/PlayerSkins";
    private const float FrameRate = 60f;

    private static readonly SkinAssetDefinition[] SkinDefinitions =
    {
        new(
            "default",
            "Default",
            "PlayerSkins/Default",
            $"{SourceRoot}/default/Movement",
            $"{SourceRoot}/default/InkPulse",
            $"{SourceRoot}/default/PortalEffect",
            createSourceFolderIfMissing: true),
        new("Chile", "Chile", "PlayerSkins/Chile"),
        new("Formal", "Formal", "PlayerSkins/Formal"),
        new("Huaso", "Huaso", "PlayerSkins/Huaso"),
        new("Marley", "Marley", "PlayerSkins/Marley"),
        new("Nemo", "Nemo", "PlayerSkins/Nemo"),
        new("Rock", "Rock", "PlayerSkins/Rock"),
        new("Sonic", "Sonic", "PlayerSkins/Sonic"),
        new("Travis", "Travis", "PlayerSkins/Travis")
    };

    [MenuItem("Tools/Squid/Player/Build Skin Prefabs")]
    public static void BuildSkinPrefabs()
    {
        EnsureAssetFolder(SkinPrefabFolder);

        int builtCount = 0;
        foreach (SkinAssetDefinition definition in SkinDefinitions)
        {
            if (BuildSkinPrefab(definition))
            {
                builtCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PlayerSkinAssetBuilder] Built {builtCount} player skin prefabs.");
    }

    private static bool BuildSkinPrefab(SkinAssetDefinition definition)
    {
        string skinSourceFolder = $"{SourceRoot}/{definition.SourceFolder}";
        if (!AssetDatabase.IsValidFolder(skinSourceFolder))
        {
            if (definition.CreateSourceFolderIfMissing)
            {
                EnsureAssetFolder(skinSourceFolder);
            }
            else
            {
                Debug.LogWarning($"[PlayerSkinAssetBuilder] Missing skin source folder: {skinSourceFolder}");
                return false;
            }
        }

        string movementFrameFolder = string.IsNullOrWhiteSpace(definition.MovementFrameFolder)
            ? ResolveFrameFolder(skinSourceFolder)
            : definition.MovementFrameFolder;
        Sprite[] movementFrames = LoadOrderedSprites(movementFrameFolder);
        if (movementFrames.Length == 0)
        {
            Debug.LogWarning($"[PlayerSkinAssetBuilder] Skin '{definition.PrefabName}' has no movement sprite frames in {movementFrameFolder}.");
            return false;
        }

        Sprite[] inkPulseFrames = LoadFramesOrFallback(
            definition.InkPulseFrameFolder,
            movementFrames,
            definition.PrefabName,
            "InkPulse");
        Sprite[] portalFrames = LoadFramesOrFallback(
            definition.PortalFrameFolder,
            movementFrames,
            definition.PrefabName,
            "PortalEffect");

        string generatedFolder = $"{skinSourceFolder}/Generated";
        EnsureAssetFolder(generatedFolder);

        AnimationClip movementClip = CreateSpriteClip($"{generatedFolder}/Movement.anim", "Movement", movementFrames, loop: true);
        AnimationClip inkPulseClip = CreateSpriteClip($"{generatedFolder}/InkPulse.anim", "InkPulse", inkPulseFrames, loop: false);
        AnimationClip portalClip = CreateSpriteClip($"{generatedFolder}/PortalEffect.anim", "PortalEffect", portalFrames, loop: false);

        AnimatorController movementController = CreateSingleStateController(
            $"{generatedFolder}/Movement.controller",
            "Movement",
            movementClip);
        AnimatorController inkPulseController = CreateSingleStateController(
            $"{generatedFolder}/InkPulse.controller",
            "InkPulse",
            inkPulseClip);
        AnimatorController portalController = CreateSingleStateController(
            $"{generatedFolder}/Portal.controller",
            "Portal",
            portalClip);

        using SkinPrefabScope prefabScope = new(definition.PrefabName);
        Transform movementVisual = CreateVisualRoot(
            prefabScope.Root.transform,
            "SquidVisual",
            movementFrames[0],
            movementController,
            new Vector3(-0.04f, 0f, 0f),
            new Vector3(0.15918185f, 0.16442001f, 1f),
            sortingOrder: 0);
        Transform inkPulseVisual = CreateVisualRoot(
            prefabScope.Root.transform,
            "InkPulseVisual",
            inkPulseFrames[0],
            inkPulseController,
            new Vector3(-0.0447f, -0.8343f, 0f),
            new Vector3(0.268331f, 0.30347988f, 1f),
            sortingOrder: 1);
        Transform portalVisual = CreateVisualRoot(
            prefabScope.Root.transform,
            "PortalVisual",
            portalFrames[0],
            portalController,
            new Vector3(-0.043f, -0.0097f, 0f),
            new Vector3(0.25222033f, 0.2580058f, 1f),
            sortingOrder: 0);

        ConfigureVisualSet(
            prefabScope.Root.AddComponent<PlayerSkinVisualSet>(),
            movementVisual,
            inkPulseVisual,
            portalVisual,
            GetClipLength(inkPulseFrames),
            GetClipLength(portalFrames));

        string prefabPath = $"{SkinPrefabFolder}/{definition.PrefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(prefabScope.Root, prefabPath);
        Debug.Log($"[PlayerSkinAssetBuilder] Built skin prefab Resources/{definition.ResourcePath} from {movementFrameFolder}.");
        return true;
    }

    private static Sprite[] LoadFramesOrFallback(
        string frameFolder,
        Sprite[] fallbackFrames,
        string prefabName,
        string visualName)
    {
        if (string.IsNullOrWhiteSpace(frameFolder))
        {
            return fallbackFrames;
        }

        Sprite[] frames = LoadOrderedSprites(frameFolder);
        if (frames.Length > 0)
        {
            return frames;
        }

        Debug.LogWarning($"[PlayerSkinAssetBuilder] Skin '{prefabName}' has no {visualName} sprite frames in {frameFolder}; using movement frames.");
        return fallbackFrames;
    }

    private static string ResolveFrameFolder(string skinSourceFolder)
    {
        string movementFolder = $"{skinSourceFolder}/Movement";
        if (AssetDatabase.IsValidFolder(movementFolder) && HasPngFrames(movementFolder))
        {
            return movementFolder;
        }

        return skinSourceFolder;
    }

    private static bool HasPngFrames(string assetFolder)
    {
        string fullPath = Path.GetFullPath(assetFolder);
        return Directory.Exists(fullPath)
            && Directory.GetFiles(fullPath, "*.png", SearchOption.TopDirectoryOnly).Length > 0;
    }

    private static Sprite[] LoadOrderedSprites(string frameFolder)
    {
        string fullFrameFolder = Path.GetFullPath(frameFolder);
        if (!Directory.Exists(fullFrameFolder))
        {
            return Array.Empty<Sprite>();
        }

        return Directory.GetFiles(fullFrameFolder, "*.png", SearchOption.TopDirectoryOnly)
            .Select(NormalizeAssetPath)
            .OrderBy(path => path, FramePathComparer.Instance)
            .Select(LoadSprite)
            .Where(sprite => sprite != null)
            .ToArray();
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        if (AssetImporter.GetAtPath(assetPath) is TextureImporter textureImporter)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.SaveAndReimport();
        }

        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static AnimationClip CreateSpriteClip(string clipPath, string clipName, Sprite[] frames, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.name = clipName;
        clip.frameRate = FrameRate;
        clip.ClearCurves();

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int index = 0; index < frames.Length; index++)
        {
            keyframes[index] = new ObjectReferenceKeyframe
            {
                time = index / FrameRate,
                value = frames[index]
            };
        }

        EditorCurveBinding spriteBinding = new()
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateSingleStateController(
        string controllerPath,
        string stateName,
        AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            stateMachine.RemoveState(childState.state);
        }

        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = clip;
        state.writeDefaultValues = true;
        stateMachine.defaultState = state;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Transform CreateVisualRoot(
        Transform parent,
        string name,
        Sprite initialSprite,
        AnimatorController controller,
        Vector3 localPosition,
        Vector3 localScale,
        int sortingOrder)
    {
        GameObject visualRoot = new(name);
        visualRoot.layer = parent.gameObject.layer;
        Transform visualTransform = visualRoot.transform;
        visualTransform.SetParent(parent, false);
        visualTransform.localPosition = localPosition;
        visualTransform.localRotation = Quaternion.identity;
        visualTransform.localScale = localScale;

        SpriteRenderer spriteRenderer = visualRoot.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = initialSprite;
        spriteRenderer.sortingOrder = sortingOrder;

        Animator animator = visualRoot.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        return visualTransform;
    }

    private static void ConfigureVisualSet(
        PlayerSkinVisualSet visualSet,
        Transform movementVisual,
        Transform inkPulseVisual,
        Transform portalVisual,
        float inkPulseFallbackClipLength,
        float portalFallbackClipLength)
    {
        SerializedObject serializedVisualSet = new(visualSet);
        serializedVisualSet.FindProperty("movementVisualRoot").objectReferenceValue = movementVisual.gameObject;
        serializedVisualSet.FindProperty("inkPulseVisualRoot").objectReferenceValue = inkPulseVisual.gameObject;
        serializedVisualSet.FindProperty("portalVisualRoot").objectReferenceValue = portalVisual.gameObject;
        serializedVisualSet.FindProperty("movementAnimator").objectReferenceValue = movementVisual.GetComponent<Animator>();
        serializedVisualSet.FindProperty("inkPulseAnimator").objectReferenceValue = inkPulseVisual.GetComponent<Animator>();
        serializedVisualSet.FindProperty("portalAnimator").objectReferenceValue = portalVisual.GetComponent<Animator>();
        serializedVisualSet.FindProperty("inkPulseStateName").stringValue = "InkPulse";
        serializedVisualSet.FindProperty("inkPulseClipName").stringValue = "InkPulse";
        serializedVisualSet.FindProperty("portalStateName").stringValue = "Portal";
        serializedVisualSet.FindProperty("portalClipName").stringValue = "PortalEffect";
        serializedVisualSet.FindProperty("fallbackInkPulseClipLength").floatValue = inkPulseFallbackClipLength;
        serializedVisualSet.FindProperty("fallbackPortalClipLength").floatValue = portalFallbackClipLength;
        serializedVisualSet.ApplyModifiedPropertiesWithoutUndo();
    }

    private static float GetClipLength(Sprite[] frames)
    {
        return Mathf.Max(0.01f, frames.Length / FrameRate);
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string normalizedPath = folderPath.Replace('\\', '/').Trim('/');
        string[] parts = normalizedPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        string currentPath = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string nextPath = $"{currentPath}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[index]);
            }

            currentPath = nextPath;
        }
    }

    private static string NormalizeAssetPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string projectPath = Path.GetFullPath(".");
        string relativePath = fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath.Substring(projectPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            : path;
        return relativePath.Replace('\\', '/');
    }

    private readonly struct SkinAssetDefinition
    {
        public SkinAssetDefinition(
            string sourceFolder,
            string prefabName,
            string resourcePath,
            string movementFrameFolder = "",
            string inkPulseFrameFolder = "",
            string portalFrameFolder = "",
            bool createSourceFolderIfMissing = false)
        {
            SourceFolder = sourceFolder;
            PrefabName = prefabName;
            ResourcePath = resourcePath;
            MovementFrameFolder = movementFrameFolder;
            InkPulseFrameFolder = inkPulseFrameFolder;
            PortalFrameFolder = portalFrameFolder;
            CreateSourceFolderIfMissing = createSourceFolderIfMissing;
        }

        public string SourceFolder { get; }
        public string PrefabName { get; }
        public string ResourcePath { get; }
        public string MovementFrameFolder { get; }
        public string InkPulseFrameFolder { get; }
        public string PortalFrameFolder { get; }
        public bool CreateSourceFolderIfMissing { get; }
    }

    private sealed class FramePathComparer : IComparer<string>
    {
        public static readonly FramePathComparer Instance = new();

        public int Compare(string left, string right)
        {
            string leftName = Path.GetFileNameWithoutExtension(left);
            string rightName = Path.GetFileNameWithoutExtension(right);
            if (int.TryParse(leftName, NumberStyles.Integer, CultureInfo.InvariantCulture, out int leftNumber)
                && int.TryParse(rightName, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rightNumber))
            {
                return leftNumber.CompareTo(rightNumber);
            }

            return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
        }
    }

    private sealed class SkinPrefabScope : IDisposable
    {
        public SkinPrefabScope(string prefabName)
        {
            Root = new GameObject(prefabName);
            int playerLayer = LayerMask.NameToLayer("Player");
            Root.layer = playerLayer >= 0 ? playerLayer : 0;
        }

        public GameObject Root { get; }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Root);
        }
    }
}
