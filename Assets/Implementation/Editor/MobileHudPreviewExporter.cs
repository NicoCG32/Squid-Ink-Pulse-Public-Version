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

// Authoring previews only: no Play Mode, scene saves, or device claims.
public static class MobileHudPreviewExporter
{
    private const string OutputFolder = "TestResults/MobileHudPreview";
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Game/ZonaEpipelagica.unity",
        "Assets/Scenes/Game/ZonaAbisopelagica.unity"
    };

    [Serializable]
    private sealed class PreviewReport
    {
        public string scope = "Static Editor authoring preview; not a device or runtime test.";
        public List<FrameReport> frames = new();
    }

    [Serializable]
    private sealed class FrameReport
    {
        public string scene;
        public string format;
        public int width;
        public int height;
        public Rect safeArea;
        public string image;
        public List<ElementReport> elements = new();
        public List<string> layoutFailures = new();
        public List<string> overlapObservations = new();
        public List<string> textOverflowObservations = new();
    }

    [Serializable]
    private sealed class ElementReport
    {
        public string name;
        public Rect pixels;
        public bool insideSafeArea;
    }

    private readonly struct Format
    {
        public Format(string name, int width, int height, int left = 0, int right = 0)
        {
            Name = name;
            Width = width;
            Height = height;
            SafeArea = new Rect(left, 0, width - left - right, height);
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public Rect SafeArea { get; }
    }

    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export HUD Preview Matrix")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            throw new InvalidOperationException("HUD previews require Edit Mode and graphics enabled; omit -nographics.");
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Save your scene changes before exporting HUD previews.");
            }
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        var report = new PreviewReport();
        Directory.CreateDirectory(OutputFolder);
        try
        {
            var formats = new[]
            {
                new Format("16-9", 1920, 1080),
                new Format("tablet-16-10", 2560, 1600),
                new Format("19.5-9", 2340, 1080),
                new Format("20-9", 2400, 1080),
                new Format("20-9-left-cutout", 2400, 1080, left: 108),
                new Format("20-9-right-cutout", 2400, 1080, right: 108),
                new Format("2712x1220-left-cutout", 2712, 1220, left: 122)
            };
            foreach (string path in ScenePaths)
            {
                foreach (Format format in formats)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    report.frames.Add(Capture(scene, format));
                }
            }

            File.WriteAllText(Path.Combine(OutputFolder, "report.json"), JsonUtility.ToJson(report, true));
            WriteGallery(report);
            int failures = report.frames.Sum(frame => frame.layoutFailures.Count);
            int observations = report.frames.Sum(frame => frame.textOverflowObservations.Count);
            int overlaps = report.frames.Sum(frame => frame.overlapObservations.Count);
            Debug.Log($"[MobileHudPreviewExporter] Frames={report.frames.Count}; LayoutFailures={failures}; ContainerOverlapObservations={overlaps}; TextOverflowObservations={observations}; Output={Path.GetFullPath(OutputFolder)}");
        }
        finally
        {
            // Preview changes are discarded, never serialized into scenes or prefabs.
            if (originalSetup.Any(scene => scene.isLoaded && scene.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static FrameReport Capture(Scene scene, Format format)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        GameUIRoot ui = roots.SelectMany(root => root.GetComponentsInChildren<GameUIRoot>(true)).Single();
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .First(candidate => candidate.enabled && candidate.gameObject.activeInHierarchy);
        Canvas hud = ui.HudRoot.GetComponent<Canvas>();
        if (hud == null || !hud.isRootCanvas)
        {
            throw new InvalidOperationException($"{scene.name}: HUD must be a root Canvas.");
        }

        foreach (Canvas canvas in roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
        {
            if (canvas != hud && !canvas.transform.IsChildOf(hud.transform))
            {
                canvas.gameObject.SetActive(false);
            }
        }

        foreach (TouchControlsVisibilityController visibility in hud.GetComponentsInChildren<TouchControlsVisibilityController>(true))
        {
            visibility.ControlsRoot.SetActive(true);
        }

        var texture = new RenderTexture(format.Width, format.Height, 24, RenderTextureFormat.ARGB32);
        Texture2D pixels = null;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            texture.Create();
            camera.targetTexture = texture;
            camera.aspect = (float)format.Width / format.Height;
            hud.renderMode = RenderMode.ScreenSpaceCamera;
            hud.worldCamera = camera;
            hud.planeDistance = camera.nearClipPlane + 1f;
            // Simulate the shared mobile composition policy at the requested size.
            CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.matchWidthOrHeight != 1f)
            {
                throw new InvalidOperationException($"{scene.name}: expected HUD height-match scaling.");
            }
            scaler.enabled = false;
            float match = MobileCanvasScalePolicy.ResolveMatch(true, format.Width, format.Height,
                scaler.referenceResolution, scaler.matchWidthOrHeight);
            hud.scaleFactor = match == 0f ? format.Width / scaler.referenceResolution.x
                : format.Height / scaler.referenceResolution.y;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            SafeAreaAdapter adapter = ui.HudRoot.GetComponentInChildren<SafeAreaAdapter>(true);
            adapter.Apply(format.SafeArea, new Vector2(format.Width, format.Height));
            ui.InkBar.SetFill(1f);
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in hud.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Debug.Log($"[HUDPreview] {scene.name}/{format.Name}: SafeRoot={adapter.Target.name}; anchors={adapter.Target.anchorMin}/{adapter.Target.anchorMax}; bounds={PixelBounds(adapter.Target, camera)}");

            var frame = new FrameReport
            {
                scene = scene.name,
                format = format.Name,
                width = format.Width,
                height = format.Height,
                safeArea = format.SafeArea,
                image = $"{scene.name}-{format.Name}.png"
            };
            TouchGameplayControlsController controls = hud.GetComponentInChildren<TouchGameplayControlsController>(true);
            var elements = new[]
            {
                ui.GadgetSlots.transform as RectTransform,
                ui.InkBar.transform as RectTransform,
                ui.ScoreCounter.transform as RectTransform,
                ui.ShrimpCounter.transform as RectTransform,
                controls.InkPulseButton.transform as RectTransform,
                controls.PauseButton.transform as RectTransform,
                controls.GadgetSlot1Button.transform as RectTransform,
                controls.GadgetSlot2Button.transform as RectTransform
            };
            foreach (RectTransform element in elements)
            {
                Rect bounds = PixelBounds(element, camera);
                bool inside = Contains(format.SafeArea, bounds);
                string name = element.parent != null && element.name == "Button" ? element.parent.name : element.name;
                frame.elements.Add(new ElementReport { name = name, pixels = bounds, insideSafeArea = inside });
                if (!inside) frame.layoutFailures.Add($"{name}: rect extends outside the simulated safe area.");
            }
            for (int i = 0; i < frame.elements.Count; i++)
                for (int j = i + 1; j < frame.elements.Count; j++)
                {
                    Rect first = frame.elements[i].pixels;
                    Rect second = frame.elements[j].pixels;
                    if (Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin) > 1f
                        && Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin) > 1f)
                    {
                        string observation = $"{frame.elements[i].name}/{frame.elements[j].name}: measured rects overlap.";
                        if (i >= 4 && j >= 4) frame.layoutFailures.Add(observation);
                        else frame.overlapObservations.Add(observation + " Inspect visible content; container bounds can include transparent space.");
                    }
                }
            foreach (TMP_Text text in hud.GetComponentsInChildren<TMP_Text>())
            {
                if (text.isTextOverflowing)
                    frame.textOverflowObservations.Add($"{text.transform.parent.name}/{text.name}: text exceeds its rect; inspect overflow mode and visible glyphs before classifying as clipping.");
            }

            RenderTexture.active = texture;
            pixels = new Texture2D(format.Width, format.Height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, format.Width, format.Height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, frame.image), pixels.EncodeToPNG());
            return frame;
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previousActive;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static Rect PixelBounds(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 minimum = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in corners)
        {
            Vector2 point = camera.WorldToScreenPoint(corner);
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1f
        && inner.yMin >= outer.yMin - 1f && inner.xMax <= outer.xMax + 1f && inner.yMax <= outer.yMax + 1f;

    private static void WriteGallery(PreviewReport report)
    {
        var html = new StringBuilder("<!doctype html><html lang='es'><meta charset='utf-8'><title>HUD móvil — matriz Editor</title><style>body{background:#101923;color:#eef4fa;font:16px system-ui;margin:24px}img{width:100%;height:auto}article{max-width:1400px;margin:32px auto}a{color:#84d7ff}p{line-height:1.6}</style><h1>HUD móvil — matriz Editor</h1><p>Escenas reales, snapshot de autoría con barra llena y controles visibles. Sin Play Mode: no certifica animaciones, estados runtime, framing de gameplay ni ergonomía en teléfono. Las capturas usan Canvas sobre cámara y el escalado por altura de producción. Los cutouts se simulan mediante SafeAreaAdapter; las imágenes completas no ocultan la franja excluida.</p><p><a href='report.json'>Medidas en píxeles y hallazgos</a></p>");
        foreach (FrameReport frame in report.frames)
        {
            html.Append($"<article><h2>{frame.scene} · {frame.format}</h2><p>{frame.width}×{frame.height}; safe area {frame.safeArea}; fuera de área segura o botones solapados: {frame.layoutFailures.Count}; observaciones de contenedores: {frame.overlapObservations.Count}; observaciones TMP: {frame.textOverflowObservations.Count}</p><img loading='lazy' src='{frame.image}' alt='{frame.scene} {frame.format}'>");
            foreach (string finding in frame.layoutFailures.Concat(frame.overlapObservations).Concat(frame.textOverflowObservations))
                html.Append($"<p>{System.Net.WebUtility.HtmlEncode(finding)}</p>");
            html.Append("</article>");
        }
        html.Append("</html>");
        File.WriteAllText(Path.Combine(OutputFolder, "index.html"), html.ToString());
    }
}
