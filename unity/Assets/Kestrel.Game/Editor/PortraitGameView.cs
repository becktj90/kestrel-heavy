// Fixed portrait Game view resolutions for true-size screenshots (no stretched landscape view).
//
// Menu:  Kestrel/Game View/<device>   and   Kestrel/Game View/Capture Screenshot
// CLI:   Unity -projectPath unity -executeMethod Kestrel.Game.Editor.PortraitGameView.AddAllSizes
//        Unity -projectPath unity -executeMethod Kestrel.Game.Editor.PortraitGameView.SelectFromCommandLine -khSize iPhone
//
// Unity has no public API for Game view sizes, so this uses the long-standing internal
// GameViewSizes / GameViewSize / GameView reflection approach. Every reflective step is guarded:
// if the internals change, it logs a warning and falls back (the capture helper still renders at the
// requested pixel size via an offscreen camera render).
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Editor
{
    public static class PortraitGameView
    {
        public readonly struct Device
        {
            public readonly string Key, Label; public readonly int W, H;
            public Device(string key, string label, int w, int h) { Key = key; Label = label; W = w; H = h; }
        }

        public static readonly Device[] Devices =
        {
            new Device("iPhone",   "KH iPhone 1290x2796",     1290, 2796),
            new Device("ProMax",   "KH Pro Max 1320x2868",    1320, 2868),
            new Device("iPad13",   "KH iPad 13in 2064x2752",  2064, 2752),
            new Device("iPhoneSE", "KH iPhone SE 750x1334",   750,  1334),
        };

        static Device? current;

        [MenuItem("Kestrel/Game View/Add Portrait Sizes")] public static void AddAllSizes() { foreach (var d in Devices) FindOrAdd(d); }
        [MenuItem("Kestrel/Game View/iPhone 1290x2796")]   public static void SelectIPhone()   => Select("iPhone");
        [MenuItem("Kestrel/Game View/Pro Max 1320x2868")]  public static void SelectProMax()   => Select("ProMax");
        [MenuItem("Kestrel/Game View/iPad 13in 2064x2752")] public static void SelectIPad13()  => Select("iPad13");
        [MenuItem("Kestrel/Game View/iPhone SE 750x1334")] public static void SelectIPhoneSE() => Select("iPhoneSE");

        /// <summary>CLI: -khSize iPhone|ProMax|iPad13|iPhoneSE (default iPhone).</summary>
        public static void SelectFromCommandLine() => Select(Arg("-khSize") ?? "iPhone");

        /// <summary>Adds (if missing) and selects the size. Returns false if the reflection path failed.</summary>
        public static bool Select(string key)
        {
            var d = Devices.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (d.Key == null) { Debug.LogError($"[Kestrel] unknown Game view size '{key}'. Use: {string.Join(", ", Devices.Select(x => x.Key))}"); return false; }
            current = d;
            try
            {
                int idx = FindOrAdd(d);
                if (idx < 0) return Fallback(d, "size not registered");
                var gvType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gv = EditorWindow.GetWindow(gvType, false, "Game", false);
                var sel = gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sel != null) sel.Invoke(gv, new object[] { idx, null });
                else
                {
                    var prop = gvType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop == null) return Fallback(d, "GameView selection API not found");
                    prop.SetValue(gv, idx);
                }
                gv.Repaint();
                Debug.Log($"[Kestrel] Game view set to {d.Label}");
                return true;
            }
            catch (Exception e) { return Fallback(d, e.GetBaseException().Message); }
        }

        static bool Fallback(Device d, string why)
        {
            Debug.LogWarning($"[Kestrel] Could not set Game view to {d.Label} via reflection ({why}). " +
                             "Add it manually (Game view > resolution > +), or use Capture, which renders offscreen at the exact size.");
            return false;
        }

        /// <summary>Index of the size in the current platform group, adding it if missing; -1 on failure.</summary>
        static int FindOrAdd(Device d)
        {
            try
            {
                var asm = typeof(EditorWindow).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singleType.GetProperty("instance").GetValue(null);
                var groupType = sizesType.GetProperty("currentGroupType").GetValue(instance);
                var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
                var gType = group.GetType();
                var texts = (string[])gType.GetMethod("GetDisplayTexts").Invoke(group, null);
                for (int i = 0; i < texts.Length; i++) if (texts[i].StartsWith(d.Label)) return i;

                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new[] { Enum.Parse(sizeKind, "FixedResolution"), d.W, d.H, d.Label });
                gType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                sizesType.GetMethod("SaveToHDD")?.Invoke(instance, null);
                return (int)gType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }
            catch (Exception e) { Debug.LogWarning($"[Kestrel] GameViewSizes reflection failed: {e.GetBaseException().Message}"); return -1; }
        }

        // ---- Capture ----

        [MenuItem("Kestrel/Game View/Capture Screenshot")]
        public static void CaptureMenu() => Capture(null);

        /// <summary>CLI: -khSize iPhone -khOut path.png. Play mode uses ScreenCapture (includes UI Toolkit overlay).</summary>
        public static void CaptureFromCommandLine() { Select(Arg("-khSize") ?? "iPhone"); Capture(Arg("-khOut")); }

        public static string Capture(string path)
        {
            var d = current ?? Devices[0];
            path ??= Path.Combine("Screenshots", $"{DateTime.Now:yyyyMMdd-HHmmss}-{d.Key}-{d.W}x{d.H}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            if (Application.isPlaying)
            {
                // Game view is at the selected fixed size, so this captures at true pixel size (written end of frame).
                ScreenCapture.CaptureScreenshot(path, 1);
                Debug.Log($"[Kestrel] ScreenCapture queued -> {path} ({d.W}x{d.H})");
                return path;
            }
            // Edit mode fallback: render Camera.main offscreen at the exact size (no UI Toolkit overlay).
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Kestrel] No Camera.main to capture; enter Play mode or add a camera."); return null; }
            var rt = new RenderTexture(d.W, d.H, 24, RenderTextureFormat.ARGB32);
            var prevT = cam.targetTexture; var prevA = RenderTexture.active;
            try
            {
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(d.W, d.H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, d.W, d.H), 0, 0); tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log($"[Kestrel] Offscreen capture -> {path} ({d.W}x{d.H})");
                return path;
            }
            finally { cam.targetTexture = prevT; RenderTexture.active = prevA; UnityEngine.Object.DestroyImmediate(rt); }
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
