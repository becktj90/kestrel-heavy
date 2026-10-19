// Starter screen (Phase 1 PR 5) with real UI Toolkit rects, like WebDesignerOverlapDeviceTests:
// 335x667, 390x844 and 430x932 portrait, iPad portrait 820x1180, and landscape web 1280x720, 1366x768
// and 1920x1080 (each a 1280x720 panel after WebPanelScale). Boosters hidden (KH-3), shown (KH-5) and
// locked (KH-10), a long readiness line and the "Checking" state. No sibling rects intersect, every label
// fits its box (TextFit.Problems), whole words never split, and every tappable control is 44 pt.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class StarterDesignerDeviceTests
    {
        const string LongReadiness = "Ready. Lands on Haven on its last fuel, and both boosters fly home with little to spare.";

        struct Device { public string Name; public float W, H, Top, Bottom; public bool Wide; }

        static IEnumerable<Device> Devices()
        {
            yield return new Device { Name = "335x667", W = 335, H = 667 };
            yield return new Device { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            yield return new Device { Name = "430x932", W = 430, H = 932, Top = 59, Bottom = 34 };
            yield return new Device { Name = "iPad 820x1180", W = 820, H = 1180, Top = 24, Bottom = 20 };
            foreach (var (w, h) in new[] { (1280, 720), (1366, 768), (1920, 1080) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                yield return new Device { Name = "web " + w + "x" + h, W = Mathf.Round(w / s), H = Mathf.Round(h / s), Wide = true };
            }
        }

        static StarterTable table;

        [OneTimeSetUp]
        public void LoadTable()
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            table = StarterTable.LoadPlaceholder(root);
        }

        [Test]
        public void EveryDeviceAndMission_NoOverlap_TextFits_44ptTargets()
        {
            foreach (var d in Devices())
                foreach (var (mission, label, blocked) in new[] { ("m3", "KH-3 boosters hidden", false), ("m5", "KH-5 boosters shown", false), ("m10", "KH-10 boosters locked", false), ("m7", "KH-7 checking", true) })
                    Check(d, new StarterRocket(table, mission), label, blocked, null);
        }

        [Test]
        public void LongReadinessLine_WrapsInsideTheSheet()
        {
            foreach (var d in Devices())
                Check(d, new StarterRocket(table, "m19"), "KH-19 long readiness", false, LongReadiness);
        }

        [Test]
        public void KnobsRebuildTheScreen_AndLockedBoostersStayPut()
        {
            using (var surface = DeviceSurface.Open(390, 844))
            {
                var r = new StarterRocket(table, "m5");
                var panel = new StarterPanel(r, surface.Stage);
                try
                {
                    panel.Layout(390, 844, 47, 34, 0, 0, 1f, false, false);
                    surface.Pump(3);
                    Click(panel.Root.Q<Button>("starter-boosters-2"));
                    Assert.AreEqual(4, r.Boosters, "KH-5 offers 0/2/4");
                    Assert.IsTrue(panel.Root.Q<Button>("starter-boosters-2").ClassListContains("starter-on"));
                    Assert.AreEqual(StarterScreen.BoostersHint, panel.Root.Q<Label>("starter-hint").text);
                    Click(panel.Root.Q<Button>("starter-fuel-4"));
                    Assert.AreEqual(4, r.FuelNotch);
                    Click(panel.Root.Q<Button>("starter-reset"));
                    Assert.AreEqual(r.Default, r.Choice);
                    Assert.IsTrue(panel.LaunchReady);
                    Assert.IsTrue(panel.Root.Q<Button>("starter-launch").enabledSelf);
                }
                finally { panel.Detach(); }

                var locked = new StarterRocket(table, "m10");
                var lp = new StarterPanel(locked, surface.Stage);
                try
                {
                    lp.Layout(390, 844, 47, 34, 0, 0, 1f, false, false);
                    Assert.AreEqual(StarterScreen.BoostersLockedHint, lp.Root.Q<Label>("starter-hint").text);
                    Assert.IsFalse(lp.Root.Q<Button>("starter-boosters-0").enabledSelf, "not offered, greyed");
                    Assert.IsTrue(lp.Root.Q<Button>("starter-boosters-1").ClassListContains("starter-on"), "the landing pair");
                    Assert.IsTrue(lp.Root.Q("starter-row-boosters").ClassListContains("starter-locked"));
                    var kh3 = new StarterPanel(new StarterRocket(table, "m3"), surface.Stage);
                    kh3.Layout(390, 844, 47, 34, 0, 0, 1f, false, false);
                    Assert.AreEqual(DisplayStyle.None, kh3.Root.Q("starter-row-boosters").style.display.value, "KH-3: the whole row is hidden");
                    kh3.Detach();
                }
                finally { lp.Detach(); }
            }
        }

        void Check(Device d, StarterRocket rocket, string label, bool blocked, string readiness)
        {
            using (var surface = DeviceSurface.Open(d.W, d.H))
            {
                var panel = new StarterPanel(rocket, surface.Stage);
                try
                {
                    panel.HostBlocked = () => blocked;
                    panel.Refresh();
                    if (readiness != null)
                    {
                        // The copy comes from flown results (PR 4); force a long one through the same split.
                        panel.Root.Q<Label>("starter-readiness").text = readiness.Substring(StarterScreen.ReadyWord.Length).Trim();
                    }
                    panel.Layout(d.W, d.H, d.Top, d.Bottom, 0, 0, 1f, d.Wide, d.Wide);
                    surface.Pump(3);
                    Settle(surface, panel.Root);
                    string where = d.Name + " " + label;
                    Assert.That(StarterLayout.Problems(panel.Frame, panel.Input), Is.Empty, where + " model");
                    NoSiblingOverlap(panel.Root, where);
                    Assert.That(TextFit.Problems(panel.Root), Is.Empty, where);
                    foreach (var t in panel.Root.Query<TextElement>().ToList())
                        if (t.resolvedStyle.display != DisplayStyle.None && t.panel != null) AssertWords(t, where + " " + t.name);
                    foreach (var b in panel.Root.Query<Button>().ToList())
                    {
                        if (b.resolvedStyle.display == DisplayStyle.None) continue;
                        Assert.GreaterOrEqual(b.worldBound.width, 44f - 0.5f, where + " " + b.name + " width");
                        Assert.GreaterOrEqual(b.worldBound.height, 44f - 0.5f, where + " " + b.name + " height");
                        Assert.That(surface.Stage.worldBound.Contains(b.worldBound.center), Is.True, where + " " + b.name + " on screen");
                    }
                    var launch = panel.Root.Q<Button>("starter-launch");
                    Assert.AreEqual(DisplayStyle.Flex, launch.resolvedStyle.display, where + " Launch always visible");
                    Assert.AreEqual(!blocked, launch.enabledSelf, where + " Launch follows CanLaunch and the gate");
                    var badge = panel.Root.Q<Label>("starter-badge");
                    if (blocked)
                    {
                        Assert.AreEqual(StarterScreen.Checking, panel.Root.Q<Label>("starter-readiness").text, where);
                        Assert.AreEqual(DisplayStyle.None, badge.resolvedStyle.display, where + " no green word without a result");
                    }
                    var rocketBox = panel.Root.Q("starter-rocket").worldBound;
                    var sheet = panel.Root.Q("starter-sheet").worldBound;
                    Assert.That(rocketBox.Overlaps(Shrink(sheet)), Is.False, where + " the rocket window is clear of the sheet");
                    if (d.Wide)
                    {
                        Assert.Greater(launch.worldBound.xMin, d.W * 0.5f, where + " Launch bottom-right");
                        Assert.LessOrEqual(sheet.width, WebLandscapeType.SideCap + 0.5f, where);
                    }
                    var back = panel.Root.Q<Button>("starter-back").worldBound;
                    Assert.Less(back.xMin, d.W * 0.25f, where + " Back top-left");
                    Assert.Less(back.yMin, d.H * 0.15f, where + " Back top-left");
                }
                finally { panel.Detach(); }
            }
        }

        static void NoSiblingOverlap(VisualElement root, string where)
        {
            var kids = new List<VisualElement>();
            foreach (var c in root.Children())
                if (c.resolvedStyle.display != DisplayStyle.None && c.worldBound.width > 0.5f && c.worldBound.height > 0.5f) kids.Add(c);
            for (int i = 0; i < kids.Count; i++)
            {
                for (int j = i + 1; j < kids.Count; j++)
                    Assert.That(Shrink(kids[i].worldBound).Overlaps(Shrink(kids[j].worldBound)), Is.False,
                        where + ": " + kids[i].name + " " + kids[i].worldBound + " overlaps " + kids[j].name + " " + kids[j].worldBound);
                if (!(kids[i] is TextElement)) NoSiblingOverlap(kids[i], where);
            }
        }

        static Rect Shrink(Rect r) => new Rect(r.x + 0.5f, r.y + 0.5f, Mathf.Max(0f, r.width - 1f), Mathf.Max(0f, r.height - 1f));

        static void AssertWords(TextElement e, string where)
        {
            if (e == null || string.IsNullOrEmpty(e.text)) return;
            float w = e.contentRect.width;
            if (float.IsNaN(w) || w < 1f) w = e.layout.width;
            float h = e.contentRect.height;
            if (float.IsNaN(h) || h < 1f) h = e.layout.height;
            if (w < 1f) return;
            var one = e.MeasureTextSize(e.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
            if (e.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
            {
                Assert.That(one.x, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " need " + one.x.ToString("0") + " box " + w.ToString("0"));
                return;
            }
            float longest = 0f;
            foreach (var word in e.text.Split(' '))
            {
                if (word.Length == 0) continue;
                longest = Mathf.Max(longest, e.MeasureTextSize(word, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x);
            }
            Assert.That(longest, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " a word is wider than its box");
            var need = e.MeasureTextSize(e.text, w, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined);
            Assert.That(need.y, Is.LessThanOrEqualTo(h + TextFit.Slack), where + " tall need " + need.y.ToString("0") + " box " + h.ToString("0"));
        }

        static void Settle(DeviceSurface surface, VisualElement root)
        {
            TextFit.Apply(root);
            surface.Pump(3);
            TextFit.Apply(root);
            surface.Pump(2);
        }

        static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }

        /// <summary>Same device surface as WebDesignerOverlapDeviceTests: an editor window, else a UIDocument on a render texture.</summary>
        sealed class DeviceSurface : IDisposable
        {
            EditorWindow window;
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }

            public static DeviceSurface Open(float w, float h)
            {
                var surface = new DeviceSurface();
                surface.TryWindow(w, h);
                surface.Pump(4);
                if (!surface.Sized(w, h))
                {
                    surface.CloseWindow();
                    surface.TryDocument(w, h);
                    surface.Pump(6);
                }
                if (!surface.Sized(w, h)) Assert.Fail("panel did not take the device size " + w + "x" + h);
                return surface;
            }

            public void Pump(int passes)
            {
                for (int i = 0; i < passes; i++)
                {
                    if (Stage != null && Stage.panel != null)
                    {
                        var tree = Stage.panel.visualTree;
                        if (tree != null && Stage.resolvedStyle.width > 2f)
                        {
                            tree.style.width = Stage.resolvedStyle.width;
                            tree.style.height = Stage.resolvedStyle.height;
                        }
                    }
                    if (settings != null)
                    {
                        var apply = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (apply != null) apply.Invoke(settings, null);
                    }
                    var utility = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                    var update = utility != null ? utility.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                    if (update != null && update.GetParameters().Length == 0) update.Invoke(null, null);
                    if (window != null) window.Repaint();
                    var panel = Stage != null ? Stage.panel : null;
                    if (panel != null)
                    {
                        var validate = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (validate != null && validate.GetParameters().Length == 0) validate.Invoke(panel, null);
                    }
                }
            }

            bool Sized(float w, float h)
            {
                if (Stage == null || Stage.panel == null) return false;
                var bound = Stage.worldBound;
                if (bound.width < 8f || bound.height < 8f) return false;
                float sx = bound.width / w, sy = bound.height / h;
                return sx >= 0.8f && sy >= 0.8f && sx <= 3.5f && sy <= 3.5f && Math.Abs(sx - sy) <= 0.15f;
            }

            void TryWindow(float w, float h)
            {
                try
                {
                    window = ScriptableObject.CreateInstance<StarterDeviceWindow>();
                    window.position = new Rect(0f, 0f, w, h);
                    window.minSize = new Vector2(w, h);
                    window.maxSize = new Vector2(w, h);
                    window.ShowUtility();
                    var root = window.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { }
            }

            void TryDocument(float w, float h)
            {
                try
                {
                    texture = new RenderTexture(Mathf.Max(8, (int)w), Mathf.Max(8, (int)h), 0, RenderTextureFormat.ARGB32);
                    texture.Create();
                    settings = ScriptableObject.CreateInstance<PanelSettings>();
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = 1f;
                    settings.targetTexture = texture;
                    go = new GameObject("kh-starter-device");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = settings;
                    var root = doc.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { }
            }

            static VisualElement MakeStage(VisualElement panelRoot, float w, float h)
            {
                var tree = panelRoot.panel != null ? panelRoot.panel.visualTree : null;
                if (tree != null) { tree.style.width = w; tree.style.height = h; }
                panelRoot.style.width = w;
                panelRoot.style.height = h;
                var stage = new VisualElement { name = "stage", pickingMode = PickingMode.Ignore };
                stage.style.position = Position.Absolute;
                stage.style.left = 0f; stage.style.top = 0f;
                stage.style.width = w; stage.style.height = h;
                stage.style.overflow = Overflow.Hidden;
                panelRoot.Add(stage);
                return stage;
            }

            void CloseWindow()
            {
                if (window == null) return;
                window.Close();
                window = null;
                Stage = null;
            }

            public void Dispose()
            {
                CloseWindow();
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

        sealed class StarterDeviceWindow : EditorWindow { }
    }
}
