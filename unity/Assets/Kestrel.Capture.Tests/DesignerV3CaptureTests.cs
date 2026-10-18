// PlayMode shots of the portrait designer mounted beside the Hangar.
// iPhone SE (375×667 @2x), iPhone 16 Pro Max (440×956 @3x), iPad Pro 13" (1032×1376 @2x).
// Output: $KH_CAP_OUT or <project>/CaptureOut/designer-{se,promax,ipad}.png
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Kestrel.Core.Flight;
using Kestrel.Kit;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    public class DesignerV3CaptureTests
    {
        const int Frames = 60;

        static readonly (string name, int w, int h)[] Devices =
        {
            ("designer-se", 750, 1334),
            ("designer-promax", 1320, 2868),
            ("designer-ipad", 2064, 2752),
        };

#if UNITY_EDITOR
        uint priorWidth, priorHeight;

        [UnitySetUp]
        public IEnumerator RememberViewport()
        {
            UnityEditor.PlayModeWindow.GetRenderingResolution(out priorWidth, out priorHeight);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreViewport()
        {
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
            yield return null;
        }
#endif

        static bool HasLabel(VisualElement root, string title, string value)
        {
            var labels = root.Query<Label>().ToList();
            for (int i = 0; i < labels.Count - 1; i++)
                if (labels[i].text == title && labels[i + 1].text == value) return true;
            return false;
        }

        static string OutDir()
        {
            var d = Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            Directory.CreateDirectory(d);
            return d;
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator DesignerOnSeProMaxAndIpad()
        {
            PadHandoff.Clear();
            GameSession.Clear();
            foreach (var device in Devices)
            {
                var shot = Shot(device.name, device.w, device.h);
                while (shot.MoveNext()) yield return shot.Current;
            }
        }

        static IEnumerator Shot(string name, int w, int h)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)w, (uint)h, "Kestrel " + name);
            for (int i = 0; i < Frames && (Screen.width != w || Screen.height != h); i++) yield return null;
            Assert.AreEqual(w, Screen.width, name + " width");
            Assert.AreEqual(h, Screen.height, name + " height");
#else
            Assert.Ignore("Portrait capture needs the editor play-mode viewport.");
            yield break;
#endif
            CaptureCamera.Clear();
            SceneManager.LoadScene("Design", LoadSceneMode.Single);
            for (int i = 0; i < Frames; i++) yield return null;

            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            Assert.IsNotNull(app, name);
            int prevEngineer = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            try
            {
                app.ShowDesigner(true);
                Assert.IsNotNull(app.Designer, name + " panel");
                app.Designer.SetEngineer(true);
                app.Designer.FinishReference();
                for (int i = 0; i < 5; i++) yield return null;

                var doc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
                Assert.IsNotNull(doc, name);
                var designer = doc.rootVisualElement.Q(DesignerV3Panel.RootName);
                Assert.IsNotNull(designer, name);
                Assert.AreEqual(DisplayStyle.Flex, designer.resolvedStyle.display, name + " visible");
                Assert.AreEqual(DisplayStyle.None, doc.rootVisualElement.Q("design-body").resolvedStyle.display, name + " hangar steps aside");
                Assert.IsNotNull(designer.Q("sticky-strip"), name);
                Assert.IsNotNull(designer.Q("tab-bar"), name);
                foreach (var tab in DesignerV3Tabs.All)
                    Assert.IsNotNull(designer.Q("tab-" + tab), name + " " + tab);
                var toggle = doc.rootVisualElement.Q<Button>(DesignerV3Panel.ToggleName);
                Assert.IsNotNull(toggle, name);
                Assert.AreEqual("Hangar", toggle.text, name);
                Assert.IsTrue(HasLabel(designer, "Chamber pressure", "97 bar"), name + " chamber pressure");
                Assert.IsTrue(HasLabel(designer, "Expansion ratio", "16"), name + " expansion ratio");
                var qText = designer.Q<Label>("strip-value-q")?.text ?? "";
                StringAssert.Contains("kPa", qText, name + " max-Q");
                var costText = designer.Q<Label>("strip-value-cost")?.text ?? "";
                StringAssert.StartsWith("$", costText, name + " cost");
                StringAssert.DoesNotContain(",", costText, name + " cost wraps when unformatted");

                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                var cams = Camera.allCameras.Where(c => c.enabled && c.targetTexture == null).OrderBy(c => c.depth).ToArray();
                foreach (var c in cams) c.targetTexture = rt;
                var panels = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                    .Select(d => d.panelSettings).Where(p => p != null).Distinct().ToArray();
                var oldTargets = panels.Select(p => p.targetTexture).ToArray();
                var oldClear = panels.Select(p => p.clearColor).ToArray();
                foreach (var p in panels) { p.targetTexture = rt; p.clearColor = false; }
                for (int i = 0; i < 3; i++) yield return null;
                foreach (var c in cams) c.targetTexture = null;
                for (int i = 0; i < panels.Length; i++) { panels[i].targetTexture = oldTargets[i]; panels[i].clearColor = oldClear[i]; }

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                rt.Release();
                CaptureCamera.Clear();

                var path = Path.Combine(OutDir(), name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                var px = tex.GetPixels32();
                int distinct = px.Where((_, i) => i % 97 == 0).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
                Debug.Log($"[Capture] {name}: {path} {w}x{h} distinctColours={distinct}");
                Assert.Greater(new FileInfo(path).Length, 1000, name);
                Assert.Greater(distinct, 8, name + " capture is flat colour");
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
            }
            finally
            {
                PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prevEngineer);
            }
        }
    }
}
