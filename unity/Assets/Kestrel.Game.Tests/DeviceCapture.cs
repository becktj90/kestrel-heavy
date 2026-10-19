// PNG capture of a device-test panel for the CI artifact (no reusable helper existed on main or
// origin/feat/home-simple, so this one follows ScreenCaptureTests' conventions: $KH_CAP_OUT, else
// <project>/CaptureOut, which unity.yml uploads). Order: (1) the UIDocument's render texture after forcing the
// runtime panels to render, (2) the editor window's screen pixels, (3) only when both come back one flat colour
// (batchmode -nographics), the measured worldBound rects painted from each element's resolved colours, saved
// with a "-rects" suffix so nobody mistakes it for a render. Returns the path and the kind.
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

        public static string OutDir()
        {
            string d = System.Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            Directory.CreateDirectory(d);
            return d;
        }

        public static string Save(string fileStem, VisualElement stage, RenderTexture texture, EditorWindow window, out Kind kind)
        {
            int w = Mathf.Max(8, Mathf.RoundToInt(stage.worldBound.width));
            int h = Mathf.Max(8, Mathf.RoundToInt(stage.worldBound.height));
            Texture2D image = null;
            kind = Kind.Render;
            if (texture != null) image = FromTexture(texture);
            if (Flat(image) && window != null)
            {
                Destroy(image);
                image = FromWindow(window, w, h);
                kind = Kind.Window;
            }
            if (Flat(image))
            {
                Destroy(image);
                image = FromRects(stage, w, h);
                kind = Kind.Rects;
            }
            string path = Path.Combine(OutDir(), Safe(fileStem) + (kind == Kind.Rects ? "-rects" : "") + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
            return path;
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
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = was;
                return tex;
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
