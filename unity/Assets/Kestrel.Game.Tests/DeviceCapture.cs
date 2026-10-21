// PNG capture of a UiDeviceSurface (#300's shared EditMode device panel) for the CI artifact. UiDeviceSurface
// has no capture of its own, so this reads its window / render texture by field name instead of editing #300's
// file. Output: $KH_CAP_OUT/<sub>, else <project>/CaptureOut/<sub> (unity.yml uploads CaptureOut/starter as
// device-captures). Order: (1) the editor window's screen pixels, (2) the UIDocument's render texture after
// forcing the runtime panels to render, (3) only when both are unavailable or one flat colour (batchmode
// -nographics, no GPU), the measured worldBound rects, saved with a "-rects" suffix so nobody mistakes it for a
// render. Never logs an error or throws: a GPU-less runner gets warnings and the rects file.
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public static class DeviceCapture
    {
        public enum Kind { Render, Window, Rects }

        public static string OutDir(string sub)
        {
            string d = System.Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            if (!string.IsNullOrEmpty(sub)) d = Path.Combine(d, sub);
            Directory.CreateDirectory(d);
            return d;
        }

        static bool HasGpu => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        static T Field<T>(UiDeviceSurface surface, string name) where T : class
        {
            var f = typeof(UiDeviceSurface).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return f != null ? f.GetValue(surface) as T : null;
        }

        /// <summary>The PNG path, or null (with a warning) if nothing could be written.</summary>
        public static string Save(string sub, string fileStem, UiDeviceSurface surface, out Kind kind)
        {
            kind = Kind.Rects;
            Texture2D image = null;
            try
            {
                var stage = surface.Stage;
                int w = Mathf.Max(8, Mathf.RoundToInt(stage.worldBound.width));
                int h = Mathf.Max(8, Mathf.RoundToInt(stage.worldBound.height));
                if (HasGpu)
                {
                    var window = Field<EditorWindow>(surface, "window");
                    if (window != null && !Application.isBatchMode) { image = FromWindow(window, w, h); kind = Kind.Window; }
                    var texture = Field<RenderTexture>(surface, "texture");
                    if (Flat(image) && texture != null && texture.IsCreated()) { Destroy(image); image = FromTexture(texture); kind = Kind.Render; }
                }
                if (Flat(image)) { Destroy(image); image = FromRects(stage, w, h); kind = Kind.Rects; }
                string path = Path.Combine(OutDir(sub), Safe(fileStem) + (kind == Kind.Rects ? "-rects" : "") + ".png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DeviceCapture] " + fileStem + ": " + e.GetType().Name + " " + e.Message);
                return null;
            }
            finally { Destroy(image); }
        }

        static Texture2D FromTexture(RenderTexture rt)
        {
            try
            {
                var utility = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                foreach (var m in new[] { "RenderOffscreenPanels", "RepaintOffscreenPanels" })
                {
                    var render = utility?.GetMethod(m, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (render != null && render.GetParameters().Length == 0) { render.Invoke(null, null); break; }
                }
                var was = RenderTexture.active;
                RenderTexture.active = rt;
                try
                {
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    return tex;
                }
                finally { RenderTexture.active = was; }
            }
            catch (Exception) { return null; }
        }

        static Texture2D FromWindow(EditorWindow window, int w, int h)
        {
            try
            {
                window.Repaint();
                // UnityEditorInternal.InternalEditorUtility.ReadScreenPixel, by name (not in the stubcheck stubs).
                var util = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.InternalEditorUtility");
                var read = util?.GetMethod("ReadScreenPixel", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Vector2), typeof(int), typeof(int) }, null);
                var px = read != null ? read.Invoke(null, new object[] { window.position.position, w, h }) as Color[] : null;
                if (px == null || px.Length != w * h) return null;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels(px);
                tex.Apply();
                return tex;
            }
            catch (Exception) { return null; }
        }

        /// <summary>The laid-out tree as filled rects (background colour, else a dim outline) with text boxes in the text colour.</summary>
        static Texture2D FromRects(VisualElement stage, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var fill = new Color[w * h];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0.04f, 0.05f, 0.08f, 1f);
            tex.SetPixels(fill);
            var o = stage.worldBound.position;
            Paint(tex, stage, o, h);
            tex.Apply();
            return tex;
        }

        static void Paint(Texture2D tex, VisualElement e, Vector2 o, int h)
        {
            if (e.resolvedStyle.display == DisplayStyle.None) return;
            var r = e.worldBound;
            r.position -= o;
            var bg = e.resolvedStyle.backgroundColor;
            if (bg.a > 0.01f) Box(tex, r, bg, true, h);
            else if (e is Button || e.childCount > 0) Box(tex, r, new Color(0.35f, 0.4f, 0.5f, 1f), false, h);
            if (e is TextElement t && !string.IsNullOrEmpty(t.text))
            {
                var c = e.contentRect;
                var cr = new Rect(r.x + c.x, r.y + c.y, c.width, c.height);
                var col = new Color(0.92f, 0.94f, 1f, 0.6f);
                Box(tex, cr, col, true, h);
            }
            foreach (var k in e.Children()) Paint(tex, k, o, h);
        }

        static void Box(Texture2D tex, Rect r, Color c, bool filled, int h)
        {
            int x0 = Mathf.Clamp(Mathf.RoundToInt(r.xMin), 0, tex.width - 1), x1 = Mathf.Clamp(Mathf.RoundToInt(r.xMax) - 1, 0, tex.width - 1);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(r.yMin), 0, h - 1), y1 = Mathf.Clamp(Mathf.RoundToInt(r.yMax) - 1, 0, h - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!filled && x != x0 && x != x1 && y != y0 && y != y1) continue;
                    int ty = h - 1 - y; // texture rows run bottom-up
                    var under = tex.GetPixel(x, ty);
                    tex.SetPixel(x, ty, Color.Lerp(under, new Color(c.r, c.g, c.b, 1f), c.a));
                }
        }

        static bool Flat(Texture2D t)
        {
            if (t == null) return true;
            var px = t.GetPixels32();
            if (px.Length == 0) return true;
            var a = px[0];
            for (int i = 1; i < px.Length; i += 7)
                if (px[i].r != a.r || px[i].g != a.g || px[i].b != a.b) return false;
            return true;
        }

        static string Safe(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }

        static void Destroy(UnityEngine.Object o) { if (o != null) UnityEngine.Object.DestroyImmediate(o); }
    }
}
