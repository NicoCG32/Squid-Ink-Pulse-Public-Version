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

public static class LoreComicPreviewExporter
{
    private const string Folder="TestResults/LoreComicPreview";
    [Serializable] private sealed class Report { public List<Frame> frames=new(); public List<string> omitted=new(); }
    [Serializable] private sealed class Frame
    {
        public string scene, comicEvent, sprite, format, image;
        public int width,height;
        public Rect safeArea,background,comic,button;
        public bool showContinue;
        public List<string> failures=new();
    }
    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export Lore Comic Preview Matrix")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)
            throw new InvalidOperationException("Edit Mode with graphics required.");
        for (int i=0;i<SceneManager.sceneCount;i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes before export.");
        SceneSetup[] setup=EditorSceneManager.GetSceneManagerSetup();
        var report=new Report(); Directory.CreateDirectory(Folder);
        try
        {
            var formats=new[] { ("16-9",1920,1080,0,0), ("16-9-left",1920,1080,108,0),
                ("16-9-right",1920,1080,0,108), ("19-5-9-left",2340,1080,108,0),
                ("19-5-9-right",2340,1080,0,108), ("20-9-left",2400,1080,108,0),
                ("20-9-right",2400,1080,0,108), ("tablet-16-10",2560,1600,0,0) };
            foreach (string zone in new[] { "MainMenu","ZonaEpipelagica","ZonaAbisopelagica" })
            {
                string path=zone=="MainMenu" ? "Assets/Scenes/MainMenu/MainMenu.unity" : $"Assets/Scenes/Game/{zone}.unity";
                Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var presenter=FindPresenter(scene);
                LoreComicEntry[] entries=Ref<LoreComicEntry[]>(presenter,"entries");
                var requests=zone=="MainMenu" ? new[] {new LoreComicRequest(LoreComicEvent.GameStart,LoreComicZone.Unknown)}
                    : new[] {new LoreComicRequest(zone=="ZonaEpipelagica" ? LoreComicEvent.PortalEpipelagicToAbyssopelagic : LoreComicEvent.PortalAbyssopelagicToEpipelagic,
                            zone=="ZonaEpipelagica" ? LoreComicZone.Abyssopelagic : LoreComicZone.Epipelagic),
                        new LoreComicRequest(LoreComicEvent.Defeat,zone=="ZonaEpipelagica" ? LoreComicZone.Epipelagic : LoreComicZone.Abyssopelagic),
                        new LoreComicRequest(LoreComicEvent.ShopInGameFirst,zone=="ZonaEpipelagica" ? LoreComicZone.Epipelagic : LoreComicZone.Abyssopelagic),
                        new LoreComicRequest(LoreComicEvent.ShopInGameLastPurchased,zone=="ZonaEpipelagica" ? LoreComicZone.Epipelagic : LoreComicZone.Abyssopelagic),
                        new LoreComicRequest(LoreComicEvent.ShopInGameLastNoPurchase,zone=="ZonaEpipelagica" ? LoreComicZone.Epipelagic : LoreComicZone.Abyssopelagic)};
                foreach (LoreComicRequest request in requests)
                {
                    LoreComicEntry entry=LoreComicEntrySelector.FindEntry(entries,request);
                    if (!LoreComicEntrySelector.HasDisplayableSprite(entry))
                    { report.omitted.Add($"{zone}/{request.ComicEvent}: no configured sprite."); continue; }
                    foreach (int index in Enumerable.Range(0,entry.sprites.Length).Where(i=>entry.sprites[i]!=null))
                        foreach (var format in formats)
                        {
                            scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                            presenter=FindPresenter(scene);
                            Sprite sprite=Ref<LoreComicEntry[]>(presenter,"entries").First(item=>item.comicEvent==entry.comicEvent && item.zone==entry.zone).sprites[index];
                            var frame=new Frame {scene=zone,comicEvent=request.ComicEvent.ToString(),sprite=AssetDatabase.GetAssetPath(sprite),
                                width=format.Item2,height=format.Item3,format=format.Item1,
                                safeArea=new Rect(format.Item4,0,format.Item2-format.Item4-format.Item5,format.Item3),
                                showContinue=LoreComicEntrySelector.ResolveShowContinueButton(entry,request,true),
                                image=$"{zone}-{(int)request.ComicEvent}-{index}-{format.Item1}.png"};
                            Capture(scene,presenter,sprite,frame); report.frames.Add(frame);
                        }
                }
            }
            File.WriteAllText(Path.Combine(Folder,"report.json"),JsonUtility.ToJson(report,true));
            var html=new StringBuilder("<!doctype html><html lang='es'><meta charset='utf-8'><title>Cómics narrativos</title><style>body{background:#101923;color:#eef4fa;font:16px system-ui;margin:24px}article{max-width:1400px;margin:32px auto}img{width:100%}a{color:#84d7ff}</style><h1>Cómics narrativos — matriz Editor</h1><p>Sprites configurados de inicio, portal, tienda y derrota en sus escenas reales. Safe area simulada; se conservan entradas temporizadas y confirmación explícita. Sin guardar escenas ni perfil. No certifica routing ni ergonomía física.</p><a href='report.json'>Medidas</a>");
            foreach (Frame frame in report.frames) html.Append($"<article><h2>{frame.scene}/{frame.comicEvent}/{frame.format}</h2><p>{frame.failures.Count} fallos.</p><img loading='lazy' src='{frame.image}'></article>");
            File.WriteAllText(Path.Combine(Folder,"index.html"),html.Append("</html>").ToString());
            Debug.Log($"[LoreComicPreview] Frames={report.frames.Count}; Omitted={report.omitted.Count}; Failures={report.frames.Sum(frame=>frame.failures.Count)}");
        }
        finally
        {
            if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
    private static void Capture(Scene scene,LoreComicPresenter presenter,Sprite sprite,Frame frame)
    {
        GameObject[] roots=scene.GetRootGameObjects();
        GameObject menu=Ref<GameObject>(presenter,"comicRoot");
        Canvas canvas=menu.GetComponent<Canvas>();
        foreach (Canvas other in roots.SelectMany(root=>root.GetComponentsInChildren<Canvas>(true))) other.enabled=other==canvas;
        var view=new LoreComicView(menu,Ref<CanvasGroup>(presenter,"canvasGroup"),Ref<Image>(presenter,"comicImage"),
            Ref<Button>(presenter,"continueButton"),Ref<GameObject>(presenter,"continueButtonRoot"),presenter.transform,5000);
        view.Show(sprite,frame.showContinue);
        Camera camera=roots.SelectMany(root=>root.GetComponentsInChildren<Camera>(true)).First(item=>item.enabled && item.gameObject.activeInHierarchy);
        CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
        scaler.matchWidthOrHeight=MobileCanvasScalePolicy.ResolveMatch(true,frame.width,frame.height,scaler.referenceResolution,scaler.matchWidthOrHeight);
        scaler.enabled=false;
        var texture=new RenderTexture(frame.width,frame.height,24); Texture2D pixels=null; RenderTexture previous=RenderTexture.active;
        try
        {
            texture.Create(); camera.targetTexture=texture; camera.aspect=(float)frame.width/frame.height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=camera.nearClipPlane+1f;
            canvas.scaleFactor=Mathf.Pow(frame.width/scaler.referenceResolution.x,1f-scaler.matchWidthOrHeight)*Mathf.Pow(frame.height/scaler.referenceResolution.y,scaler.matchWidthOrHeight);
            Canvas.ForceUpdateCanvases(); camera.Render();
            SafeAreaAdapter[] safe=menu.GetComponentsInChildren<SafeAreaAdapter>(true);
            if (safe.Length!=1) frame.failures.Add("Missing unique safe area.");
            foreach (SafeAreaAdapter adapter in safe) adapter.Apply(frame.safeArea,new Vector2(frame.width,frame.height));
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in menu.GetComponentsInChildren<MobileReferenceContentFitter>(true)) fitter.Apply((fitter.transform.parent as RectTransform).rect.size,true);
            Canvas.ForceUpdateCanvases();
            var layout=menu.GetComponentInChildren<LoreComicMobileLayout>(true);
            if (layout==null) frame.failures.Add("Missing mobile comic layout."); else layout.Apply(true);
            Canvas.ForceUpdateCanvases(); camera.Render();
            frame.background=Bounds(menu.transform.Find("Dimmer") as RectTransform,camera);
            if (!Contains(frame.background,new Rect(0,0,frame.width,frame.height))) frame.failures.Add("Dimmer does not cover viewport.");
            Image image=Ref<Image>(presenter,"comicImage");
            frame.comic=Bounds(image.rectTransform,camera);
            if (!Contains(frame.safeArea,frame.comic)) frame.failures.Add("Comic outside safe area.");
            if (!image.preserveAspect) frame.failures.Add("Comic must preserve sprite aspect ratio.");
            if (frame.showContinue)
            {
                frame.button=Bounds(Ref<GameObject>(presenter,"continueButtonRoot").transform as RectTransform,camera);
                if (!Contains(frame.safeArea,frame.button)) frame.failures.Add("Continue outside safe area.");
                if (frame.comic.Overlaps(frame.button)) frame.failures.Add("Comic overlaps continue button.");
            }
            RenderTexture.active=texture; pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(Folder,frame.image),pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous; camera.targetTexture=null;
            if (pixels!=null) UnityEngine.Object.DestroyImmediate(pixels);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    private static LoreComicPresenter FindPresenter(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root=>root.GetComponentsInChildren<LoreComicPresenter>(true)).Single();
    private static T Ref<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static Rect Bounds(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4]; rect.GetWorldCorners(corners); var points=corners.Select(point=>camera.WorldToScreenPoint(point)).ToArray();
        return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));
    }
    private static bool Contains(Rect outer,Rect inner) => inner.xMin>=outer.xMin-1f && inner.yMin>=outer.yMin-1f && inner.xMax<=outer.xMax+1f && inner.yMax<=outer.yMax+1f;
}
