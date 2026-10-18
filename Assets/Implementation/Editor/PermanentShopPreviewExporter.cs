using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class PermanentShopPreviewExporter
{
    private const string Folder = "TestResults/PermanentShopPreview";
    [Serializable] private sealed class Report { public List<Frame> frames = new(); }
    [Serializable] private sealed class Element { public string name; public Rect bounds; }
    [Serializable] private sealed class Frame
    {
        public string format, image;
        public int width, height, selectionsChecked, glyphsChecked;
        public Rect safeArea, background;
        public List<Element> controls = new();
        public List<string> failures = new();
    }

    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export Permanent Shop Preview Matrix")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Edit Mode with graphics required.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(item => item.isLoaded && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty))
            throw new InvalidOperationException("Save scenes before export.");
        Directory.CreateDirectory(Folder);
        var report = new Report();
        try
        {
            var formats = new[] { ("16-9",1920,1080,0,0), ("16-9-left",1920,1080,108,0),
                ("16-9-right",1920,1080,0,108), ("19-5-9-left",2340,1080,108,0),
                ("19-5-9-right",2340,1080,0,108), ("20-9-left",2400,1080,108,0),
                ("20-9-right",2400,1080,0,108), ("tablet-16-10",2560,1600,0,0) };
            foreach (var format in formats)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/ShopMenu/ShopMenu.unity", OpenSceneMode.Single);
                var frame = new Frame { format=format.Item1, width=format.Item2, height=format.Item3,
                    safeArea=new Rect(format.Item4,0,format.Item2-format.Item4-format.Item5,format.Item3),
                    image=format.Item1+".png" };
                Capture(frame);
                report.frames.Add(frame);
            }
            File.WriteAllText(Path.Combine(Folder,"report.json"),JsonUtility.ToJson(report,true));
            var html = new StringBuilder("<!doctype html><html lang='es'><meta charset='utf-8'><title>Tienda móvil</title><style>body{background:#101923;color:#eef4fa;font:16px system-ui;margin:24px}article{max-width:1400px;margin:32px auto}img{width:100%}a{color:#84d7ff}</style><h1>Tienda permanente — matriz Editor</h1><p>Escena real, safe area simulada y textos del catálogo semilla. No modifica el perfil ni certifica compras o touch físico.</p><a href='report.json'>Medidas</a>");
            foreach (Frame frame in report.frames)
                html.Append($"<article><h2>{frame.format}</h2><p>Fallos: {frame.failures.Count}; selecciones: {frame.selectionsChecked}</p><img src='{frame.image}'></article>");
            File.WriteAllText(Path.Combine(Folder,"index.html"),html.Append("</html>").ToString());
            Debug.Log($"[PermanentShopPreview] Frames={report.frames.Count}; Controls={report.frames.Sum(f=>f.controls.Count)}; Selections={report.frames.Sum(f=>f.selectionsChecked)}; Glyphs={report.frames.Sum(f=>f.glyphsChecked)}; Failures={report.frames.Sum(f=>f.failures.Count)}");
        }
        finally
        {
            if (setup.Any(item=>item.isLoaded && item.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }

    private static void Capture(Frame frame)
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        OutOfGameShopManager shop = roots.SelectMany(root=>root.GetComponentsInChildren<OutOfGameShopManager>(true)).Single();
        Canvas canvas = shop.GetComponent<Canvas>();
        canvas.transform.localScale=Vector3.one;
        foreach (OptionsMenuManager options in roots.SelectMany(root=>root.GetComponentsInChildren<OptionsMenuManager>(true)))
            options.GetComponentInParent<Canvas>(true).gameObject.SetActive(false);
        var serialized = new SerializedObject(shop);
        typeof(OutOfGameShopManager).GetMethod("ApplyPlatformPresentation",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(shop,new object[] { true });
        TMP_Text[] texts = new[] { "selectedItemNameText","selectedItemDescriptionText","selectedItemPriceText","selectedItemStateText" }
            .Select(field=>serialized.FindProperty(field).objectReferenceValue as TMP_Text).Where(text=>text!=null).ToArray();
        Camera camera = roots.SelectMany(root=>root.GetComponentsInChildren<Camera>(true)).First(item=>item.enabled);
        var texture = new RenderTexture(frame.width,frame.height,24);
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            texture.Create(); camera.targetTexture=texture; camera.aspect=(float)frame.width/frame.height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera;
            canvas.planeDistance=camera.nearClipPlane+1f;
            CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
            scaler.matchWidthOrHeight=MobileCanvasScalePolicy.ResolveMatch(true,frame.width,frame.height,
                scaler.referenceResolution,scaler.matchWidthOrHeight);
            scaler.enabled=false;
            canvas.scaleFactor=Mathf.Pow(frame.width/scaler.referenceResolution.x,1f-scaler.matchWidthOrHeight)
                *Mathf.Pow(frame.height/scaler.referenceResolution.y,scaler.matchWidthOrHeight);
            Canvas.ForceUpdateCanvases(); camera.Render();
            foreach (SafeAreaAdapter adapter in canvas.GetComponentsInChildren<SafeAreaAdapter>(true))
                adapter.Apply(frame.safeArea,new Vector2(frame.width,frame.height));
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in canvas.GetComponentsInChildren<MobileReferenceContentFitter>(true))
                fitter.Apply((fitter.transform.parent as RectTransform).rect.size,true);
            // The cover component is optional so the same exporter captures the baseline.
            MobileBackgroundCover cover=canvas.GetComponentInChildren<MobileBackgroundCover>(true);
            if (cover!=null) cover.Apply(((RectTransform)cover.transform.parent).rect.size,true);
            Canvas.ForceUpdateCanvases(); camera.Render();
            Transform background=canvas.GetComponentsInChildren<Transform>(true).Single(item=>item.name=="Fondo");
            frame.background=Bounds(background as RectTransform,camera);
            if (!Contains(frame.background,new Rect(0,0,frame.width,frame.height))) frame.failures.Add("Background does not cover viewport.");
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                Rect bounds=Bounds(button.transform as RectTransform,camera);
                string name=button.transform.parent.name;
                frame.controls.Add(new Element { name=name,bounds=bounds });
                if (!Contains(frame.safeArea,bounds)) frame.failures.Add(name+" outside safe area.");
                if (Mathf.Min(bounds.width,bounds.height)<48f) frame.failures.Add(name+" hit area below 48 preview pixels.");
            }
            UnlockablesCatalogSaveData catalog=JsonUtility.FromJson<UnlockablesCatalogSaveData>(
                File.ReadAllText(Path.Combine(PersistentDbPaths.EditorSeedDirectory,PersistentDbPaths.UnlockablesCatalogFileName)));
            catalog.Normalize();
            var presenter=new PermanentShopSlotPresenter(shop);
            var upgradeVisuals=(PermanentShopSlotVisual[])typeof(OutOfGameShopManager).GetField("upgradeSlotVisuals",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(shop);
            var skinVisuals=(PermanentShopSlotVisual[])typeof(OutOfGameShopManager).GetField("skinSlotVisuals",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(shop);
            UnlockableSkinDefinition[] visibleSkins=catalog.skins.Where(skin=>!string.IsNullOrWhiteSpace(skin.shopSpriteResourcePath)).ToArray();
            for (int i=0;i<4;i++)
            {
                presenter.PresentUpgradeSlot(upgradeVisuals[i],catalog.permanentUpgrades[i],false);
                presenter.PresentSkinSlot(skinVisuals[i],visibleSkins[i],false,false);
            }
            (serialized.FindProperty("upgradeLevelIndicatorRoot").objectReferenceValue as GameObject)?.SetActive(false);
            var selections = catalog.permanentUpgrades.Select(upgrade=>PermanentShopSelectionPresenter.ForUpgrade(upgrade,0,true,upgrade.basePrice,amount=>ShrimpCounterDisplay.FormatShrimpAmount(amount)))
                .Concat(catalog.skins.Where(skin=>!string.IsNullOrWhiteSpace(skin.shopSpriteResourcePath))
                    .Select(skin=>PermanentShopSelectionPresenter.ForSkin(skin,false,false,true,amount=>ShrimpCounterDisplay.FormatShrimpAmount(amount))));
            foreach (PermanentShopSelectionPresentation selection in selections)
            {
                string[] copy={ selection.DisplayName,selection.Description,selection.Price,selection.State };
                for (int i=0;i<texts.Length;i++)
                {
                    texts[i].text=copy[i]; texts[i].ForceMeshUpdate();
                    if (texts[i].isTextOverflowing) frame.failures.Add(selection.DisplayName+"/"+texts[i].name+" overflows.");
                    foreach (var character in texts[i].textInfo.characterInfo.Take(texts[i].textInfo.characterCount).Where(c=>c.isVisible))
                    {
                        frame.glyphsChecked++;
                        foreach (Vector3 point in new[] { character.bottomLeft,character.topLeft,character.topRight,character.bottomRight })
                        {
                            Vector2 pixel=camera.WorldToScreenPoint(texts[i].transform.TransformPoint(point));
                            if (!Contains(frame.safeArea,new Rect(pixel,Vector2.zero))) { frame.failures.Add(selection.DisplayName+"/"+texts[i].name+" glyph outside safe area."); break; }
                        }
                    }
                }
                frame.selectionsChecked++;
            }
            camera.Render(); RenderTexture.active=texture;
            pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Folder,frame.image),pixels.EncodeToPNG());
            if (frame.format=="16-9") File.WriteAllLines(Path.Combine(Folder,"hierarchy.txt"),canvas.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.parent==canvas.transform || t.parent?.name=="Panel" || t.name=="ShrimpCounter").Select(t=>$"{t.parent?.name}/{t.name}"));
        }
        finally
        {
            RenderTexture.active=previous; camera.targetTexture=null;
            if (pixels!=null) UnityEngine.Object.DestroyImmediate(pixels);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static Rect Bounds(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2[] points=corners.Select(point=>(Vector2)camera.WorldToScreenPoint(point)).ToArray();
        return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
    }
    private static bool Contains(Rect outer,Rect inner) => inner.xMin>=outer.xMin-1f && inner.yMin>=outer.yMin-1f
        && inner.xMax<=outer.xMax+1f && inner.yMax<=outer.yMax+1f;
}
