using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Explicit local diagnostics only. No resource loading, profile changes or gameplay input.
public sealed class AndroidTextureAudit : MonoBehaviour
{
    private string session;
    private string previewName;
    private int previewWidth, previewHeight, sequence;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity?.Call<AndroidJavaObject>("getIntent");
            if (intent == null || !intent.Call<bool>("getBooleanExtra", "sip-texture-audit", false)) return;
            var audit = new GameObject("AndroidTextureAudit").AddComponent<AndroidTextureAudit>();
            audit.previewName = intent.Call<string>("getStringExtra", "sip-texture-preview");
            audit.previewWidth = intent.Call<int>("getIntExtra", "sip-texture-width", 0);
            audit.previewHeight = intent.Call<int>("getIntExtra", "sip-texture-height", 0);
            DontDestroyOnLoad(audit.gameObject);
        }
        catch (AndroidJavaException) { Debug.LogWarning("[AndroidTextureAudit] Optional intent unavailable; audit disabled."); }
#endif
    }

    private void Start()
    {
        session = DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Schedule(SceneManager.GetActiveScene());
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Schedule(scene);

    private void Schedule(Scene scene)
    {
        if (scene != SceneManager.GetActiveScene()) return;
        StopAllCoroutines();
        StartCoroutine(CaptureAfterDelay(scene));
    }

    private IEnumerator CaptureAfterDelay(Scene scene)
    {
        yield return new WaitForSecondsRealtime(2);
        if (!scene.isLoaded || scene != SceneManager.GetActiveScene()) yield break;
        try { Capture(scene); }
        catch (Exception error)
        {
            Debug.LogWarning($"[AndroidTextureAudit] Capture failed: {error.GetType().Name}; audit disabled.");
            Destroy(gameObject);
        }
    }

    private void Capture(Scene scene)
    {
        var clock = Stopwatch.StartNew();
        // This enumerates already-loaded objects, including internal/disabled ones; never call every frame.
        Texture2D[] loaded = Resources.FindObjectsOfTypeAll<Texture2D>();
        var rows = loaded.Where(texture => texture != null).Select(texture => new TextureRow
        {
            instanceId = texture.GetInstanceID(), name = texture.name,
            width = texture.width, height = texture.height, mipCount = texture.mipmapCount,
            format = texture.format.ToString(), graphicsFormat = texture.graphicsFormat.ToString(),
            readable = texture.isReadable, runtimeMemoryBytes = Profiler.GetRuntimeMemorySizeLong(texture)
        }).OrderByDescending(row => row.runtimeMemoryBytes).ThenBy(row => row.name, StringComparer.Ordinal).ToArray();
        clock.Stop();
        string folder = Path.Combine(Application.persistentDataPath, "Diagnostics", "TextureAudit");
        Directory.CreateDirectory(folder);
        string basename = session + "-" + (++sequence).ToString("D3");
        var result = new Snapshot
        {
            capturedUtc = DateTime.UtcNow.ToString("o"), buildGuid = Application.buildGUID,
            unity = Application.unityVersion, version = Application.version, development = Debug.isDebugBuild,
            scene = scene.name, realtimeSinceStartupSeconds = Time.realtimeSinceStartupAsDouble,
            gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
            graphicsVersion = SystemInfo.graphicsDeviceVersion,
            astc4x4Supported = SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4),
            etc2Supported = SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8),
            reportedNativeTextureBytes = rows.Sum(row => row.runtimeMemoryBytes),
            unavailableMemoryRows = rows.Count(row => row.runtimeMemoryBytes == 0),
            enumerationMilliseconds = clock.Elapsed.TotalMilliseconds, textures = rows
        };
        if (!string.IsNullOrEmpty(previewName))
        {
            var matches = loaded.Where(texture => texture != null && texture.name == previewName &&
                (previewWidth <= 0 || texture.width == previewWidth) &&
                (previewHeight <= 0 || texture.height == previewHeight)).ToArray();
            if (matches.Length == 1)
            {
                result.previewFile = basename + "-preview.png";
                WritePreview(matches[0], Path.Combine(folder, result.previewFile));
            }
        }
        File.WriteAllText(Path.Combine(folder, basename + ".json"), JsonUtility.ToJson(result, true), new UTF8Encoding(false));
        Debug.Log($"[AndroidTextureAudit] Scene={scene.name}; Textures={rows.Length}; EnumerationMs={result.enumerationMilliseconds:F2}; File={basename}.json; Folder={folder}");
    }

    private static void WritePreview(Texture2D source, string path)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Texture2D copy = null;
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply(false);
            File.WriteAllBytes(path, copy.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (copy != null) Destroy(copy);
        }
    }

    [Serializable]
    private sealed class Snapshot
    {
        public string capturedUtc, buildGuid, unity, version, scene, gpu, graphicsApi, graphicsVersion, previewFile;
        public bool development, astc4x4Supported, etc2Supported;
        public long reportedNativeTextureBytes;
        public int unavailableMemoryRows;
        public double realtimeSinceStartupSeconds, enumerationMilliseconds;
        public string limitations = "Loaded Texture2D inventory, not proof of physical GPU residency. Native memory returns zero when unavailable; treat those rows as unknown, not zero allocation. Enumeration and optional GPU preview disturb timing: exclude capture frames from benchmarks. No forced asset loading/unloading, gameplay changes, per-frame CPU/GPU attribution or progress data.";
        public TextureRow[] textures;
    }

    [Serializable]
    private sealed class TextureRow
    {
        public int instanceId, width, height, mipCount;
        public string name, format, graphicsFormat;
        public bool readable;
        public long runtimeMemoryBytes;
    }
}
