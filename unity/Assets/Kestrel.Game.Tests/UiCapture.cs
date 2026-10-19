// Real PNG captures from EditMode device tests, for the PR's CI artifact.
// A test lays its screen out on an offscreen UIDocument (PanelSettings.targetTexture), renders the panel
// into that RenderTexture, and calls Save. Files land in $KH_UI_CAP_OUT, else <project>/CaptureOut/ui,
// which .github/workflows/unity.yml uploads from the EditMode leg as the "ui-captures" artifact.
// Stand-in for the shared helper the #300 worker is adding on feat/home-simple (not pushed when this
// was written); same output root as the PlayMode capture tests (KH_CAP_OUT / CaptureOut).
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public static class UiCapture
    {
        public const string ArtifactName = "ui-captures";
        public const string Folder = "ui";

        public static string OutDir()
        {
            var d = Environment.GetEnvironmentVariable("KH_UI_CAP_OUT");
            if (string.IsNullOrEmpty(d))
            {
                var root = Environment.GetEnvironmentVariable("KH_CAP_OUT");
                if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
                d = Path.Combine(root, Folder);
            }
            Directory.CreateDirectory(d);
            return d;
        }

        /// <summary>Ask UI Toolkit to draw offscreen runtime panels now (EditMode has no player loop to do it).</summary>
        public static void RenderPanels()
        {
            var utility = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
            if (utility == null) return;
            const BindingFlags all = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var name in new[] { "UpdatePanels", "RepaintPanels", "RenderOffscreenPanels" })
            {
                var m = utility.GetMethod(name, all);
                if (m == null) continue;
                var ps = m.GetParameters();
                if (ps.Length == 0) m.Invoke(null, null);
                else if (ps.Length == 1 && ps[0].ParameterType == typeof(bool)) m.Invoke(null, new object[] { false });
            }
        }

        /// <summary>Write the texture as name.png and fail when it is one flat colour (nothing drew).</summary>
        public static string Save(RenderTexture rt, string name)
        {
            Assert.That(rt, Is.Not.Null, name + ": no offscreen texture to capture");
            RenderPanels();
            var prev = RenderTexture.active;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                var px = tex.GetPixels32();
                var seen = new HashSet<int>();
                for (int i = 0; i < px.Length && seen.Count < 16; i += 7)
                    seen.Add(px[i].r << 16 | px[i].g << 8 | px[i].b);
                Assert.That(seen.Count, Is.GreaterThanOrEqualTo(8), name + ": capture is flat; the panel did not render");
                string path = Path.Combine(OutDir(), Slug(name) + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                TestContext.WriteLine("ui capture: " + path);
                return path;
            }
            finally
            {
                RenderTexture.active = prev;
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        public static string Slug(string name)
        {
            var chars = name.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!(char.IsLetterOrDigit(chars[i]) || chars[i] == '-')) chars[i] = '-';
            return new string(chars).Replace("--", "-").Trim('-');
        }
    }
}
