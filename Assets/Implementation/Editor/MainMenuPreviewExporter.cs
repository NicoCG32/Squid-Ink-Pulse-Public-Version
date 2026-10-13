using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuPreviewExporter
{
    private const string Folder = "TestResults/MainMenuPreview";
    [Serializable] private sealed class Report { public List<Frame> frames = new(); }
    [Serializable] private sealed class Frame
    {
        public string name;
        public int width, height;
        public Rect safeArea;
        public List<Element> elements = new();
        public List<string> failures = new();
    }
    [Serializable] private sealed class Element { public string name; public Rect bounds; }

    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export Main Menu Preview Matrix")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Edit Mode and graphics required.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes before export.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new Report();
        Directory.CreateDirectory(Folder);
        try
        {
            var formats = new[] { ("16-9",1920,1080,0,0), ("16-9-left",1920,1080,108,0),
                ("20-9-left",2400,1080,108,0), ("20-9-right",2400,1080,0,108),
                ("tablet-16-10",2560,1600,0,0), ("tablet-inset",2560,1600,80,0) };
            foreach (var format in formats)
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu/MainMenu.unity", OpenSceneMode.Single);
                MainMenu menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenu>(true)).Single();
                new SerializedObject(menu).FindProperty("tutorialComicsPanel").objectReferenceValue
                    .AsGameObject().SetActive(false);
                OptionsMenuManager options = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OptionsMenuManager>(true)).Single();
                options.gameObject.SetActive(false);
                Canvas canvas = menu.GetComponent<Canvas>();
                Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
                var frame = new Frame { name=format.Item1, width=format.Item2, height=format.Item3,
                    safeArea=new Rect(format.Item4,0,format.Item2-format.Item4-format.Item5,format.Item3) };
                var texture = new RenderTexture(frame.width,frame.height,24);
                Texture2D pixels = null;
                RenderTexture previous = RenderTexture.active;
                try
                {
                    texture.Create();
                    camera.targetTexture = texture;
                    camera.aspect = (float)frame.width/frame.height;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = camera.nearClipPlane + 1f;
                    CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                    float match = MobileCanvasScalePolicy.ResolveMatch(true,frame.width,frame.height,
                        scaler.referenceResolution,scaler.matchWidthOrHeight);
                    scaler.enabled = false;
                    canvas.scaleFactor = match == 0f ? frame.width/scaler.referenceResolution.x
                        : frame.height/scaler.referenceResolution.y;
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    foreach (SafeAreaAdapter adapter in menu.GetComponentsInChildren<SafeAreaAdapter>(true))
                        adapter.Apply(frame.safeArea,new Vector2(frame.width,frame.height));
                    typeof(MainMenu).GetMethod("ApplyPlatformPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(menu, new object[] { true });
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    IEnumerable<RectTransform> controls = menu.GetComponentsInChildren<Button>(false)
                        .Where(button => button.interactable && button.GetComponentInParent<OptionsMenuManager>() == null)
                        .Select(button => button.transform as RectTransform);
                    IEnumerable<RectTransform> foreground = menu.GetComponentsInChildren<RectTransform>(false)
                        .Where(rect => rect.name == "Logo" || rect.name == "ShrimpCounter");
                    foreach (RectTransform rect in controls.Concat(foreground))
                    {
                        Vector3[] corners = new Vector3[4];
                        rect.GetWorldCorners(corners);
                        Vector2[] points = corners.Select(point => (Vector2)camera.WorldToScreenPoint(point)).ToArray();
                        Rect bounds = Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),
                            points.Max(point=>point.x),points.Max(point=>point.y));
                        string label = $"{rect.parent.name}/{rect.name}";
                        frame.elements.Add(new Element { name=label, bounds=bounds });
                        if (bounds.xMin < frame.safeArea.xMin-1f || bounds.yMin < frame.safeArea.yMin-1f
                            || bounds.xMax > frame.safeArea.xMax+1f || bounds.yMax > frame.safeArea.yMax+1f)
                            frame.failures.Add($"{label}: {bounds}");
                    }
                    RenderTexture.active = texture;
                    pixels = new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.Combine(Folder,frame.name+".png"),pixels.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                    texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
                }
                report.frames.Add(frame);
            }
            File.WriteAllText(Path.Combine(Folder,"report.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"[MainMenuPreview] Frames={report.frames.Count}; Failures={report.frames.Sum(frame=>frame.failures.Count)}");
        }
        finally
        {
            if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }

    private static GameObject AsGameObject(this UnityEngine.Object value) => value as GameObject;
}
