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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Static authoring previews of the real tutorial pages, never a scene rewrite.
public static class MainMenuTutorialPreviewExporter
{
    private const string ScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string OutputFolder = "TestResults/MainMenuTutorialPreview";

    [Serializable]
    private sealed class Report
    {
        public string scope = "Static Editor preview with simulated mobile scaling/safe area. Glyph bounds use emitted TMP character quads, not text RectTransforms or opaque-pixel coverage. This does not certify runtime routing, readability or device behavior.";
        public List<Frame> frames = new();
    }

    [Serializable]
    private sealed class Frame
    {
        public string format, image, page;
        public int width, height, pageNumber;
        public Rect safeArea, nextButtonBounds;
        public bool nextButtonInsideSafeArea;
        public List<TextBounds> texts = new();
        public List<string> failures = new();
    }

    [Serializable]
    private sealed class TextBounds
    {
        public string name;
        public Rect glyphBounds;
        public int generatedCharacters, visibleGlyphs;
        public bool glyphsInsideSafeArea, textOverflowing;
        public List<OutsideGlyph> outsideGlyphs = new();
    }

    [Serializable]
    private sealed class OutsideGlyph
    {
        public int characterIndex, unicode;
        public Rect bounds;
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

    [MenuItem("Tools/Squid Ink Pulse/Mobile/Export Main Menu Tutorial Preview Matrix")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Tutorial previews require Edit Mode with graphics; omit -nographics.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save scene changes before exporting tutorial previews.");

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new Report();
        Directory.CreateDirectory(OutputFolder);
        try
        {
            var formats = new[]
            {
                new Format("16-9", 1920, 1080),
                new Format("16-9-left-cutout", 1920, 1080, left: 108),
                new Format("16-9-right-cutout", 1920, 1080, right: 108),
                new Format("20-9-left-cutout", 2400, 1080, left: 108),
                new Format("20-9-right-cutout", 2400, 1080, right: 108),
                new Format("tablet-16-10", 2560, 1600)
            };
            foreach (Format format in formats)
                for (int page = 0; page < 3; page++)
                {
                    Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    report.frames.Add(Capture(scene, format, page));
                }

            File.WriteAllText(Path.Combine(OutputFolder, "report.json"), JsonUtility.ToJson(report, true));
            WriteGallery(report);
            Debug.Log($"[MainMenuTutorialPreview] Frames={report.frames.Count}; Glyphs={report.frames.Sum(frame => frame.texts.Sum(text => text.visibleGlyphs))}; OutsideGlyphs={report.frames.Sum(frame => frame.texts.Sum(text => text.outsideGlyphs.Count))}; Failures={report.frames.Sum(frame => frame.failures.Count)}; Output={Path.GetFullPath(OutputFolder)}");
        }
        finally
        {
            // Opening/restoring scenes discards preview-only activation/scaling changes.
            if (setup.Any(scene => scene.isLoaded && scene.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static Frame Capture(Scene scene, Format format, int pageIndex)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        MainMenu menu = roots.SelectMany(root => root.GetComponentsInChildren<MainMenu>(true)).Single();
        var serialized = new SerializedObject(menu);
        SerializedProperty pages = serialized.FindProperty("tutorialComicPages");
        if (pages.arraySize != 3)
            throw new InvalidOperationException("The main menu tutorial must have its three authored pages.");
        GameObject tutorial = serialized.FindProperty("tutorialComicsPanel").objectReferenceValue as GameObject;
        GameObject page = pages.GetArrayElementAtIndex(pageIndex).FindPropertyRelative("root").objectReferenceValue as GameObject;
        Button next = serialized.FindProperty("tutorialNextButton").objectReferenceValue as Button;
        if (tutorial == null || page == null || next == null)
            throw new InvalidOperationException("Tutorial panel, page and Next references must be assigned.");
        Canvas canvas = tutorial.GetComponent<Canvas>();
        if (canvas == null || !canvas.isRootCanvas)
            throw new InvalidOperationException("The tutorial must own its root Canvas.");

        // Use the existing presenter and page references; no duplicate UI or text.
        typeof(MainMenu).GetMethod("ApplyPlatformPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(menu, new object[] { true });
        menu.AbrirTutorial();
        for (int i = 0; i < pageIndex; i++)
            menu.AvanzarTutorial();
        if (!page.activeInHierarchy)
            throw new InvalidOperationException($"The existing presenter did not show page {pageIndex + 1}.");

        foreach (Canvas other in roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
            if (other != canvas && !other.transform.IsChildOf(canvas.transform))
                other.gameObject.SetActive(false);
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .Single(candidate => candidate.enabled && candidate.gameObject.activeInHierarchy);

        var texture = new RenderTexture(format.Width, format.Height, 24, RenderTextureFormat.ARGB32);
        Texture2D pixels = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        var frame = new Frame
        {
            format = format.Name, width = format.Width, height = format.Height,
            safeArea = format.SafeArea, page = page.name, pageNumber = pageIndex + 1,
            image = $"{format.Name}-page-{pageIndex + 1}.png"
        };
        try
        {
            texture.Create();
            camera.targetTexture = texture;
            camera.aspect = (float)format.Width / format.Height;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 1f;

            // CanvasScaler does not tick with arbitrary RenderTexture sizes in Edit Mode.
            // Apply the same pure policy, disable updates, then set the root factor explicitly.
            foreach (CanvasScaler scaler in tutorial.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.matchWidthOrHeight = MobileCanvasScalePolicy.ResolveMatch(true,
                    format.Width, format.Height, scaler.referenceResolution, scaler.matchWidthOrHeight);
                scaler.enabled = false;
            }
            CanvasScaler rootScaler = canvas.GetComponent<CanvasScaler>();
            if (rootScaler == null || rootScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || rootScaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                throw new InvalidOperationException("The tutorial requires a reference-resolution CanvasScaler.");
            canvas.scaleFactor = rootScaler.matchWidthOrHeight == 0f
                ? format.Width / rootScaler.referenceResolution.x
                : format.Height / rootScaler.referenceResolution.y;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            foreach (SafeAreaAdapter adapter in tutorial.GetComponentsInChildren<SafeAreaAdapter>(true))
                adapter.Apply(format.SafeArea, new Vector2(format.Width, format.Height));
            Canvas.ForceUpdateCanvases();
            foreach (MobileReferenceContentFitter fitter in tutorial.GetComponentsInChildren<MobileReferenceContentFitter>(true))
                if (fitter.transform.parent is RectTransform available)
                    fitter.Apply(available.rect.size, true);

            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(false))
                text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            camera.Render();

            foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(false))
            {
                TextBounds measurement = MeasureGlyphs(text, camera, format.SafeArea);
                frame.texts.Add(measurement);
                if (measurement.visibleGlyphs == 0 && !string.IsNullOrWhiteSpace(text.text))
                    frame.failures.Add($"{measurement.name}: nonempty text emitted no visible glyph quads.");
                if (!measurement.glyphsInsideSafeArea)
                    frame.failures.Add($"{measurement.name}: {measurement.outsideGlyphs.Count} glyph quads extend outside safe area.");
            }
            if (frame.texts.Count == 0)
                frame.failures.Add("The selected tutorial page has no active TMP text to measure.");
            frame.nextButtonBounds = PixelBounds(next.transform as RectTransform, camera);
            frame.nextButtonInsideSafeArea = Contains(format.SafeArea, frame.nextButtonBounds);
            if (!next.gameObject.activeInHierarchy || !next.interactable)
                frame.failures.Add("Next must be active and interactable on every tutorial page.");
            if (!frame.nextButtonInsideSafeArea)
                frame.failures.Add("Next hit area extends outside safe area.");

            RenderTexture.active = texture;
            pixels = new Texture2D(format.Width, format.Height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, format.Width, format.Height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, frame.image), pixels.EncodeToPNG());
            return frame;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static TextBounds MeasureGlyphs(TMP_Text text, Camera camera, Rect safeArea)
    {
        var result = new TextBounds
        {
            name = $"{text.transform.parent.name}/{text.name}",
            generatedCharacters = text.textInfo.characterCount,
            glyphsInsideSafeArea = true,
            textOverflowing = text.isTextOverflowing
        };
        Vector2 minimum = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = text.textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;
            Vector3[] vertices = text.textInfo.meshInfo[character.materialReferenceIndex].vertices;
            Vector2 glyphMin = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 glyphMax = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 point = camera.WorldToScreenPoint(text.transform.TransformPoint(vertices[character.vertexIndex + corner]));
                glyphMin = Vector2.Min(glyphMin, point);
                glyphMax = Vector2.Max(glyphMax, point);
            }
            Rect bounds = Rect.MinMaxRect(glyphMin.x, glyphMin.y, glyphMax.x, glyphMax.y);
            result.visibleGlyphs++;
            minimum = Vector2.Min(minimum, glyphMin);
            maximum = Vector2.Max(maximum, glyphMax);
            if (!Contains(safeArea, bounds))
            {
                result.glyphsInsideSafeArea = false;
                result.outsideGlyphs.Add(new OutsideGlyph { characterIndex = i, unicode = character.character, bounds = bounds });
            }
        }
        result.glyphBounds = result.visibleGlyphs > 0
            ? Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y) : Rect.zero;
        return result;
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

    private static void WriteGallery(Report report)
    {
        var html = new StringBuilder("<!doctype html><html lang='es'><meta charset='utf-8'><title>Tutorial móvil — matriz Editor</title><style>body{background:#101923;color:#eef4fa;font:16px system-ui;margin:24px}img{width:100%;height:auto}article{max-width:1400px;margin:32px auto}a{color:#84d7ff}p{line-height:1.6}</style><h1>Tutorial móvil — matriz Editor</h1><p>Páginas reales de Cómo Jugar, mediante su presenter y referencias serializadas. Escala móvil y cutouts simulados, sin guardar escenas. Se miden los quads de glifos TMP emitidos y el hit area de Next; los quads pueden incluir padding SDF. Los PNG no ocultan el área excluida. No certifica routing touch, legibilidad ni comportamiento físico.</p><p><a href='report.json'>Medidas y glifos fuera de área segura</a></p>");
        foreach (Frame frame in report.frames)
        {
            html.Append($"<article><h2>{frame.format} · página {frame.pageNumber}</h2><p>{frame.width}×{frame.height}; safe area {frame.safeArea}; glifos: {frame.texts.Sum(text => text.visibleGlyphs)}; glifos fuera: {frame.texts.Sum(text => text.outsideGlyphs.Count)}; fallos: {frame.failures.Count}</p><img loading='lazy' src='{frame.image}' alt='Tutorial {frame.format} página {frame.pageNumber}'>");
            foreach (string failure in frame.failures)
                html.Append($"<p>{System.Net.WebUtility.HtmlEncode(failure)}</p>");
            html.Append("</article>");
        }
        html.Append("</html>");
        File.WriteAllText(Path.Combine(OutputFolder, "index.html"), html.ToString());
    }
}
