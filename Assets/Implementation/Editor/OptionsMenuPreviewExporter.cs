using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OptionsMenuPreviewExporter
{
    private const string Folder = "TestResults/OptionsMenuPreview";
    [Serializable] private sealed class Report { public List<Frame> frames = new(); }
    [Serializable] private sealed class Element { public string name; public Rect bounds; }
    [Serializable] private sealed class Frame
    {
        public string context, format, image;
        public int width, height;
        public Rect safeArea, background;
        public List<Element> elements = new();
        public List<string> failures = new();
    }

    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export Options Preview Matrix")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Edit Mode with graphics required.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes before export.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new Report();
        Directory.CreateDirectory(Folder);
        try
        {
            string[] scenes = { "Assets/Scenes/MainMenu/MainMenu.unity", "Assets/Scenes/ShopMenu/ShopMenu.unity",
                "Assets/Scenes/Game/ZonaEpipelagica.unity", "Assets/Scenes/Game/ZonaAbisopelagica.unity" };
            var formats = new[] { ("16-9",1920,1080,0,0), ("16-9-left",1920,1080,108,0),
                ("16-9-right",1920,1080,0,108), ("20-9-left",2400,1080,108,0),
                ("20-9-right",2400,1080,0,108), ("tablet-16-10",2560,1600,0,0) };
            foreach (string path in scenes)
                foreach (var format in formats)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var frame = new Frame { context=scene.name, format=format.Item1,
                        width=format.Item2, height=format.Item3,
                        safeArea=new Rect(format.Item4,0,format.Item2-format.Item4-format.Item5,format.Item3),
                        image=$"{scene.name}-{format.Item1}.png" };
                    Capture(scene, frame);
                    report.frames.Add(frame);
                }
            File.WriteAllText(Path.Combine(Folder,"report.json"),JsonUtility.ToJson(report,true));
            var html = new StringBuilder("<!doctype html><html lang='es'><meta charset='utf-8'><title>Opciones móviles</title><style>body{background:#101923;color:#eef4fa;font:16px system-ui;margin:24px}article{max-width:1400px;margin:32px auto}img{width:100%}a{color:#84d7ff}</style><h1>Opciones — matriz Editor</h1><p>Prefab real en las cuatro escenas. Escala móvil y cutouts simulados, sin guardar assets ni preferencias. Se comprueban controles/placas y fondo completo. No certifica fade, routing, ergonomía ni una run.</p><a href='report.json'>Medidas</a>");
            foreach (Frame frame in report.frames)
                html.Append($"<article><h2>{frame.context} · {frame.format}</h2><p>Fallos: {frame.failures.Count}</p><img loading='lazy' src='{frame.image}'></article>");
            File.WriteAllText(Path.Combine(Folder,"index.html"),html.Append("</html>").ToString());
            Debug.Log($"[OptionsPreview] Frames={report.frames.Count}; Failures={report.frames.Sum(frame=>frame.failures.Count)}");
        }
        finally
        {
            if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }

    private static void Capture(Scene scene, Frame frame)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        OptionsMenuManager manager = roots.SelectMany(root=>root.GetComponentsInChildren<OptionsMenuManager>(true)).Single();
        var serialized = new SerializedObject(manager);
        GameObject panel = serialized.FindProperty("menuRoot").objectReferenceValue as GameObject;
        GameObject background = serialized.FindProperty("background").objectReferenceValue as GameObject;
        CanvasGroup group = serialized.FindProperty("canvasGroup").objectReferenceValue as CanvasGroup;
        Slider volume = serialized.FindProperty("volumeSlider").objectReferenceValue as Slider;
        Button back = serialized.FindProperty("backButton").objectReferenceValue as Button;
        Canvas owner = panel.GetComponentInParent<Canvas>(true);
        owner.transform.localScale = Vector3.one;
        panel.SetActive(true);
        background.SetActive(true);
        group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true;
        typeof(OptionsMenuManager).GetMethod("ApplyPlatformPresentation", BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(manager,new object[] { true });
        foreach (MainMenu menu in roots.SelectMany(root=>root.GetComponentsInChildren<MainMenu>(true)))
        {
            typeof(MainMenu).GetMethod("ApplyCanvasScaling",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(menu,new object[] { new Vector2(frame.width,frame.height),true });
            typeof(MainMenu).GetMethod("ApplyPlatformPresentation",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(menu,new object[] { true });
        }
        Canvas rootCanvas = owner.rootCanvas;
        foreach (Canvas other in roots.SelectMany(root=>root.GetComponentsInChildren<Canvas>(true)))
        {
            bool inOptions = other == owner || other.transform.IsChildOf(owner.transform);
            bool ancestor = owner.transform.IsChildOf(other.transform);
            if (!inOptions && !ancestor) other.gameObject.SetActive(false);
        }
        Camera camera = roots.SelectMany(root=>root.GetComponentsInChildren<Camera>(true))
            .First(candidate=>candidate.enabled && candidate.gameObject.activeInHierarchy);
        var texture = new RenderTexture(frame.width,frame.height,24);
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            texture.Create(); camera.targetTexture = texture; camera.aspect = (float)frame.width/frame.height;
            rootCanvas.transform.localScale = Vector3.one;
            rootCanvas.renderMode = RenderMode.ScreenSpaceCamera; rootCanvas.worldCamera = camera;
            rootCanvas.planeDistance = camera.nearClipPlane + 1f;
            SafeAreaAdapter[] adapters = panel.GetComponentsInChildren<SafeAreaAdapter>(true);
            foreach (SafeAreaAdapter adapter in adapters)
            {
                CanvasScaler scaler = adapter.GetComponentInParent<CanvasScaler>(true);
                scaler.matchWidthOrHeight = MobileCanvasScalePolicy.ResolveMatch(true,frame.width,frame.height,
                    scaler.referenceResolution,scaler.matchWidthOrHeight);
            }
            foreach (CanvasScaler scaler in rootCanvas.GetComponentsInChildren<CanvasScaler>(true)) scaler.enabled=false;
            CanvasScaler rootScaler = rootCanvas.GetComponent<CanvasScaler>();
            rootCanvas.scaleFactor = Mathf.Pow(frame.width/rootScaler.referenceResolution.x,1f-rootScaler.matchWidthOrHeight)
                * Mathf.Pow(frame.height/rootScaler.referenceResolution.y,rootScaler.matchWidthOrHeight);
            Canvas.ForceUpdateCanvases(); camera.Render();
            typeof(OptionsMenuManager).GetMethod("ApplyPlatformPresentation",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(manager,new object[] { true });
            Canvas.ForceUpdateCanvases();
            foreach (SafeAreaAdapter adapter in adapters) adapter.Apply(frame.safeArea,new Vector2(frame.width,frame.height));
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in panel.GetComponentsInChildren<MobileReferenceContentFitter>(true))
                fitter.Apply((fitter.transform.parent as RectTransform).rect.size,true);
            Canvas.ForceUpdateCanvases(); camera.Render();
            frame.background = PixelBounds(background.transform as RectTransform,camera);
            if (!Contains(frame.background,new Rect(0,0,frame.width,frame.height)))
                frame.failures.Add("Background does not cover the full viewport.");
            var controls = new[] { volume.transform as RectTransform, back.transform as RectTransform,
                volume.transform.Find("Background/Visual") as RectTransform, back.transform.parent.Find("Visual") as RectTransform };
            foreach (RectTransform rect in controls)
            {
                if (rect == null) throw new InvalidOperationException("Missing control/visual reference.");
                Rect bounds = PixelBounds(rect,camera);
                frame.elements.Add(new Element { name=$"{rect.parent.name}/{rect.name}", bounds=bounds });
                if (!Contains(frame.safeArea,bounds)) frame.failures.Add($"{rect.parent.name}/{rect.name} outside safe area.");
            }
            foreach (float value in new[] { 0f,0.5f,1f })
            {
                volume.SetValueWithoutNotify(value);
                Canvas.ForceUpdateCanvases();
                Rect handle = PixelBounds(volume.handleRect,camera);
                if (!Contains(frame.safeArea,handle)) frame.failures.Add($"Volume handle at {value} outside safe area.");
            }
            volume.SetValueWithoutNotify(0.75f);
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture.active=texture;
            pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Folder,frame.image),pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous; camera.targetTexture=null;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static Rect PixelBounds(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2[] points = corners.Select(point=>(Vector2)camera.WorldToScreenPoint(point)).ToArray();
        return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),
            points.Max(point=>point.x),points.Max(point=>point.y));
    }
    private static bool Contains(Rect outer,Rect inner) => inner.xMin >= outer.xMin-1f && inner.yMin >= outer.yMin-1f
        && inner.xMax <= outer.xMax+1f && inner.yMax <= outer.yMax+1f;
}
