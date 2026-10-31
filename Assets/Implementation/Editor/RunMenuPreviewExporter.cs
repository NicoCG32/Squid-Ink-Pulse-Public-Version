using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RunMenuPreviewExporter
{
    private const string Folder = "TestResults/RunMenuPreview";
    [Serializable] private sealed class Report { public List<Frame> frames = new(); }
    [Serializable] private sealed class Element { public string name; public Rect bounds; }
    [Serializable] private sealed class Frame
    {
        public string scene, menu, format, image;
        public int width, height, glyphsChecked;
        public Rect safeArea, background;
        public List<Element> elements = new();
        public List<string> failures = new();
    }

    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export Run Menu Preview Matrix")]
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
            var formats = new[] { ("16-9",1920,1080,0,0), ("16-9-left",1920,1080,108,0),
                ("16-9-right",1920,1080,0,108), ("19-5-9-left",2340,1080,108,0),
                ("19-5-9-right",2340,1080,0,108), ("20-9-left",2400,1080,108,0),
                ("20-9-right",2400,1080,0,108), ("tablet-16-10",2560,1600,0,0) };
            foreach (string zone in new[] { "ZonaEpipelagica", "ZonaAbisopelagica" })
                foreach (bool pause in new[] { true, false })
                    foreach (var format in formats)
                    {
                        Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/Game/{zone}.unity", OpenSceneMode.Single);
                        var frame = new Frame { scene=zone, menu=pause ? "Pause" : "GameOver", format=format.Item1,
                            width=format.Item2, height=format.Item3,
                            safeArea=new Rect(format.Item4,0,format.Item2-format.Item4-format.Item5,format.Item3),
                            image=$"{zone}-{(pause ? "pause" : "gameover")}-{format.Item1}.png" };
                        Capture(scene, pause, frame);
                        report.frames.Add(frame);
                    }
            File.WriteAllText(Path.Combine(Folder,"report.json"),JsonUtility.ToJson(report,true));
            var html = new StringBuilder("<!doctype html><html lang='es'><meta charset='utf-8'><title>Pausa y Game Over</title><style>body{background:#101923;color:#eef4fa;font:16px system-ui;margin:24px}article{max-width:1400px;margin:32px auto}img{width:100%}a{color:#84d7ff}</style><h1>Pausa y Game Over — matriz Editor</h1><p>Instancias reales de ambas zonas, reposo final sin animación. Safe area simulada, escala móvil y puntajes de muestra; sin guardar escenas ni perfil. No certifica routing, ergonomía ni una run.</p><a href='report.json'>Medidas</a>");
            foreach (Frame frame in report.frames)
                html.Append($"<article><h2>{frame.scene} / {frame.menu} / {frame.format}</h2><p>{frame.elements.Count} rectángulos, {frame.glyphsChecked} glifos; {frame.failures.Count} fallos.</p><img loading='lazy' src='{frame.image}'></article>");
            File.WriteAllText(Path.Combine(Folder,"index.html"),html.Append("</html>").ToString());
            Debug.Log($"[RunMenuPreview] Frames={report.frames.Count}; Failures={report.frames.Sum(frame=>frame.failures.Count)}");
        }
        finally
        {
            if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }

    private static void Capture(Scene scene, bool pause, Frame frame)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        GameUIRoot ui = roots.SelectMany(root=>root.GetComponentsInChildren<GameUIRoot>(true)).Single();
        GameObject menu = pause ? ui.PauseMenuRoot : ui.GameOverMenuRoot;
        Canvas canvas = menu.GetComponent<Canvas>();
        foreach (Canvas other in roots.SelectMany(root=>root.GetComponentsInChildren<Canvas>(true)))
            if (other != canvas && !other.transform.IsChildOf(canvas.transform)) other.gameObject.SetActive(false);
        menu.SetActive(true); menu.transform.localScale=Vector3.one;
        CanvasGroup group = menu.GetComponent<CanvasGroup>();
        group.alpha=1f; group.interactable=true; group.blocksRaycasts=true;
        Camera camera = roots.SelectMany(root=>root.GetComponentsInChildren<Camera>(true))
            .First(candidate=>candidate.enabled && candidate.gameObject.activeInHierarchy);
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.matchWidthOrHeight=MobileCanvasScalePolicy.ResolveMatch(true,frame.width,frame.height,
            scaler.referenceResolution,scaler.matchWidthOrHeight);
        scaler.enabled=false;
        var texture = new RenderTexture(frame.width,frame.height,24);
        Texture2D pixels=null;
        RenderTexture previous=RenderTexture.active;
        try
        {
            texture.Create(); camera.targetTexture=texture; camera.aspect=(float)frame.width/frame.height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera;
            canvas.planeDistance=camera.nearClipPlane+1f;
            canvas.scaleFactor=Mathf.Pow(frame.width/scaler.referenceResolution.x,1f-scaler.matchWidthOrHeight)
                * Mathf.Pow(frame.height/scaler.referenceResolution.y,scaler.matchWidthOrHeight);
            Canvas.ForceUpdateCanvases(); camera.Render();
            foreach (SafeAreaAdapter adapter in menu.GetComponentsInChildren<SafeAreaAdapter>(true))
                adapter.Apply(frame.safeArea,new Vector2(frame.width,frame.height));
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in menu.GetComponentsInChildren<MobileReferenceContentFitter>(true))
                fitter.Apply((fitter.transform.parent as RectTransform).rect.size,true);
            foreach (TMP_Text text in menu.GetComponentsInChildren<TMP_Text>(true))
                text.text=GameOverScoreText.Format(text.name=="PuntajeObtenido" ? "Puntaje: " : "Best: ",long.MaxValue);
            Canvas.ForceUpdateCanvases(); camera.Render();
            RectTransform background = (pause ? menu.transform.Find("FondoBurbuja") : menu.transform.Find("Panel")) as RectTransform;
            frame.background=PixelBounds(background,camera);
            if (!Contains(frame.background,new Rect(0,0,frame.width,frame.height))) frame.failures.Add("Background does not cover viewport.");
            var measured = menu.GetComponentsInChildren<Button>(true).Select(button=>button.transform as RectTransform)
                .Concat(menu.GetComponentsInChildren<TMP_Text>(true).Select(text=>text.transform as RectTransform));
            foreach (RectTransform rect in measured)
            {
                Rect bounds=PixelBounds(rect,camera);
                frame.elements.Add(new Element { name=rect.name=="Button" ? rect.parent.name : rect.name,bounds=bounds });
                if (!Contains(frame.safeArea,bounds)) frame.failures.Add($"{rect.parent.name}/{rect.name} outside safe area.");
            }
            foreach (TMP_Text text in menu.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate();
                foreach (TMP_CharacterInfo glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(glyph=>glyph.isVisible))
                {
                    frame.glyphsChecked++;
                    Vector3 a=camera.WorldToScreenPoint(text.transform.TransformPoint(glyph.bottomLeft));
                    Vector3 b=camera.WorldToScreenPoint(text.transform.TransformPoint(glyph.topRight));
                    if (!Contains(frame.safeArea,Rect.MinMaxRect(a.x,a.y,b.x,b.y))) frame.failures.Add($"{text.name}: glyph outside safe area.");
                }
            }
            RenderTexture.active=texture;
            pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Folder,frame.image),pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous; camera.targetTexture=null;
            if (pixels!=null) UnityEngine.Object.DestroyImmediate(pixels);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static Rect PixelBounds(RectTransform rect, Camera camera)
    {
        var corners=new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2[] points=corners.Select(point=>(Vector2)camera.WorldToScreenPoint(point)).ToArray();
        return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));
    }
    private static bool Contains(Rect outer,Rect inner) => inner.xMin>=outer.xMin-1f && inner.yMin>=outer.yMin-1f
        && inner.xMax<=outer.xMax+1f && inner.yMax<=outer.yMax+1f;
}
