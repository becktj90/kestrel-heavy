// Starter screen (Phase 1 PR 5): the REAL UI Toolkit tree (StarterPanel over the real catalog, the campaign and
// each mission's campaign winner) at 335x667, 390x844 and 430x932 portrait, iPad portrait 820x1180, and landscape
// web 1280x720, 1366x768 and 1920x1080 (each the panel WebPanelScale gives). Per size and case: every label's
// MeasureTextSize fits its content rect, every element sits inside its parent, no siblings overlap, nothing is
// off-screen or outside the safe area, 44 pt targets, exactly one primary (Launch) bottom-right on web / at the
// bottom on portrait, ◀ Back top-left. One PNG per size (DeviceCapture → $KH_CAP_OUT or unity/CaptureOut).
// The click-through goes by UiIds names only, and the pad design follows every control.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
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
        const string LongReadiness = "Lands on Haven on its last fuel, and both boosters fly home with little to spare.";

        struct Device { public string Name; public float W, H, Top, Bottom; public bool Wide; }

        static IEnumerable<Device> Devices()
        {
            yield return new Device { Name = "335x667", W = 335, H = 667, Top = 20 };
            yield return new Device { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            yield return new Device { Name = "430x932", W = 430, H = 932, Top = 59, Bottom = 34 };
            yield return new Device { Name = "ipad-820x1180", W = 820, H = 1180, Top = 24, Bottom = 20 };
            foreach (var (w, h) in new[] { (1280, 720), (1366, 768), (1920, 1080) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                yield return new Device { Name = "web-" + w + "x" + h, W = Mathf.Round(w / s), H = Mathf.Round(h / s), Wide = true };
            }
        }

        static Catalog catalog;
        static Campaign campaign;

        [OneTimeSetUp]
        public void LoadBases()
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            catalog = Catalog.LoadFromRepo(root);
            campaign = Campaign.Load(root);
            StarterBases.Active = StarterBases.Parse(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            StarterBasesBoot.Loaded = true;
        }

        static StarterRocket Rocket(string id, string key = null)
        {
            var r = new StarterRocket(catalog, campaign.ById(id), StarterBases.For(id));
            if (key != null)
            {
                var p = key.Split('-');
                StarterRocket.TryEngine(p[0], out var e);
                r.Engine = e; r.Stages = int.Parse(p[1]); r.FuelNotch = int.Parse(p[2].Substring(1)); r.Boosters = int.Parse(p[3].Substring(1));
                Assert.AreEqual(key, r.Choice.Key, id + " setting");
            }
            return r;
        }

        static IEnumerable<(string Label, Func<StarterRocket> Make, string Readiness)> Cases()
        {
            yield return ("kh3-boosters-hidden", () => Rocket("m3"), null);
            yield return ("kh5-boosters-shown", () => Rocket("m5"), null);
            yield return ("kh10-boosters-locked", () => Rocket("m10"), null);
            yield return ("kh7-short", () => Rocket("m7", "kerosene-3-f0-b0"), null);
            yield return ("kh6-too-heavy", () => Rocket("m6", "methane-3-f4-b0"), null);
            yield return ("kh19-long-readiness", () => Rocket("m19"), LongReadiness);
        }

        [Test]
        public void EveryDevice_RealTree_TextFits_NoOverlap_InsideSafeArea_Captured()
        {
            var shots = new List<string>();
            foreach (var d in Devices())
                foreach (var c in Cases())
                {
                    string path = Check(d, c.Make(), c.Label, c.Readiness, capture: c.Label == "kh5-boosters-shown");
                    if (path != null) shots.Add(path);
                }
            Assert.AreEqual(7, shots.Count, "one PNG per size");
            foreach (var p in shots)
            {
                Assert.That(File.Exists(p), p);
                Assert.Greater(new FileInfo(p).Length, 200, p + " is a real image");
                TestContext.WriteLine("capture " + p);
            }
        }

        [Test]
        public void ClickThroughByName_ControlsBuildThePadRocket()
        {
            using (var surface = DeviceSurface.Open(390, 844))
            {
                var r = Rocket("m5");
                var panel = new StarterPanel(r, surface.Stage);
                try
                {
                    panel.Layout(390, 844, 47, 34, 0, 0, 1f, false, false);
                    surface.Pump(3);
                    Assert.AreEqual(UiIds.ScreenStarter, panel.Root.name);
                    foreach (var id in UiIds.StarterControls)
                        Assert.That(panel.Root.Q(id), Is.Not.Null, "every UiIds.StarterControls name exists: " + id);
                    Assert.AreEqual(1, panel.Root.Query<Button>(UiIds.StarterLaunch).ToList().Count, "one Launch");

                    string before = Kestrel.Core.Hangar.HangarSession.Snapshot(r.BuildDesign());
                    Click(panel.Root.Q<Button>(UiIds.StarterBoostersOption(2)));
                    Assert.AreEqual(2, r.Boosters);
                    Assert.IsTrue(panel.Root.Q<Button>(UiIds.StarterBoostersOption(2)).ClassListContains("starter-on"));
                    Assert.AreEqual(StarterScreen.BoostersHint, panel.Root.Q<Label>(UiIds.StarterHint).text);
                    Click(panel.Root.Q<Button>(UiIds.StarterFuelNotch(5)));
                    Assert.AreEqual(4, r.FuelNotch);
                    Click(panel.Root.Q<Button>(UiIds.StarterEngineOption(StarterEngine.Methane)));
                    Assert.AreEqual(StarterEngine.Methane, r.Engine);
                    Click(panel.Root.Q<Button>(UiIds.StarterStagesOption(1)));
                    Assert.AreEqual(1, r.Stages);
                    Assert.AreNotEqual(before, Kestrel.Core.Hangar.HangarSession.Snapshot(r.BuildDesign()), "the controls change the built design");
                    Click(panel.Root.Q<Button>(UiIds.StarterReset));
                    Assert.AreEqual(r.Default, r.Choice);
                    Assert.AreEqual(before, Kestrel.Core.Hangar.HangarSession.Snapshot(r.BuildDesign()), "Reset is the campaign winner again");
                    Assert.IsTrue(panel.LaunchReady);
                    Assert.IsTrue(panel.Root.Q<Button>(UiIds.StarterLaunch).enabledSelf);

                    int launched = 0, back = 0;
                    panel.LaunchRequested += () => launched++;
                    panel.BackRequested += () => back++;
                    Click(panel.Root.Q<Button>(UiIds.StarterLaunch));
                    Click(panel.Root.Q<Button>(UiIds.StarterBack));
                    Assert.AreEqual(1, launched); Assert.AreEqual(1, back);

                    // A short rocket still launches; only a hard Validate block greys Launch, with its one plain reason.
                    Click(panel.Root.Q<Button>(UiIds.StarterStagesOption(1)));
                    Click(panel.Root.Q<Button>(UiIds.StarterFuelNotch(1)));
                    Assert.AreEqual(!r.Check().Blocked, panel.Root.Q<Button>(UiIds.StarterLaunch).enabledSelf);
                }
                finally { panel.Detach(); }

                var locked = new StarterPanel(Rocket("m10"), surface.Stage);
                try
                {
                    locked.Layout(390, 844, 47, 34, 0, 0, 1f, false, false);
                    Assert.AreEqual(StarterScreen.BoostersLockedHint, locked.Root.Q<Label>(UiIds.StarterHint).text);
                    Assert.IsFalse(locked.Root.Q<Button>(UiIds.StarterBoostersOption(0)).enabledSelf, "not offered, greyed");
                    Assert.IsTrue(locked.Root.Q<Button>(UiIds.StarterBoostersOption(locked.Rocket.Default.Boosters)).ClassListContains("starter-on"), "the landing pair");
                    Assert.IsTrue(locked.Root.Q(UiIds.StarterBoosters).ClassListContains("starter-locked"));
                }
                finally { locked.Detach(); }

                var kh3 = new StarterPanel(Rocket("m3"), surface.Stage);
                try
                {
                    kh3.Layout(390, 844, 47, 34, 0, 0, 1f, false, false);
                    Assert.AreEqual(DisplayStyle.None, kh3.Root.Q(UiIds.StarterBoosters).style.display.value, "KH-3: the whole row is hidden");
                }
                finally { kh3.Detach(); }
            }
        }

        string Check(Device d, StarterRocket rocket, string label, string readiness, bool capture)
        {
            using (var surface = DeviceSurface.Open(d.W, d.H))
            {
                var panel = new StarterPanel(rocket, surface.Stage);
                try
                {
                    panel.Refresh();
                    if (readiness != null) panel.Root.Q<Label>(UiIds.StarterReadiness).text = readiness;
                    panel.Layout(d.W, d.H, d.Top, d.Bottom, 0, 0, 1f, d.Wide, d.Wide);
                    surface.Pump(3);
                    Settle(surface, panel.Root);
                    string where = d.Name + " " + label;
                    var stage = surface.Stage.worldBound;
                    var safe = new Rect(stage.xMin, stage.yMin + d.Top, stage.width, stage.height - d.Top - d.Bottom);

                    Assert.That(StarterLayout.Problems(panel.Frame, panel.Input), Is.Empty, where + " model");
                    NoSiblingOverlap(panel.Root, where);
                    InsideParents(panel.Root, where);
                    Assert.That(TextFit.Problems(panel.Root), Is.Empty, where);
                    foreach (var t in panel.Root.Query<TextElement>().ToList())
                    {
                        if (!Shown(t)) continue;
                        TextFitsContent(t, where + " " + t.name);
                        // The rocket window may run under the status bar; nothing readable or tappable does.
                        Inside(t.worldBound, safe, where + " " + t.name + " inside the safe area");
                    }
                    var primaries = 0;
                    foreach (var b in panel.Root.Query<Button>().ToList())
                    {
                        if (!Shown(b)) continue;
                        if (b.ClassListContains("starter-primary") || b.name == UiIds.StarterLaunch) primaries++;
                        Assert.GreaterOrEqual(b.worldBound.width, 44f - 0.5f, where + " " + b.name + " width");
                        Assert.GreaterOrEqual(b.worldBound.height, 44f - 0.5f, where + " " + b.name + " height");
                        Inside(b.worldBound, safe, where + " " + b.name + " on screen, inside the safe area");
                    }
                    Assert.AreEqual(1, primaries, where + " exactly one primary button");

                    var launch = panel.Root.Q<Button>(UiIds.StarterLaunch);
                    Assert.AreEqual(DisplayStyle.Flex, launch.resolvedStyle.display, where + " Launch always visible");
                    bool blocked = rocket.Check().Blocked;
                    Assert.AreEqual(!blocked, launch.enabledSelf, where + " Launch is off only on a hard block");
                    if (label == "kh7-short") Assert.IsTrue(launch.enabledSelf, where + " a short rocket still launches");
                    if (label == "kh6-too-heavy")
                    {
                        Assert.IsTrue(blocked, where);
                        Assert.AreEqual(StarterScreen.HeavyBadge, panel.Root.Q<Label>(UiIds.StarterBadge).text, where);
                    }
                    var rocketBox = panel.Root.Q(UiIds.StarterRocket).worldBound;
                    var sheet = panel.Root.Q(UiIds.StarterSheet).worldBound;
                    Assert.That(rocketBox.Overlaps(Shrink(sheet)), Is.False, where + " the rocket window is clear of the sheet");
                    if (d.Wide)
                    {
                        Assert.Greater(launch.worldBound.xMin, stage.xMin + stage.width * 0.5f, where + " Launch bottom-right");
                        Assert.Greater(launch.worldBound.yMin, stage.yMin + stage.height * 0.5f, where + " Launch bottom-right");
                        Assert.LessOrEqual(sheet.width, WebLandscapeType.SideCap + 0.5f, where);
                    }
                    else Assert.Greater(launch.worldBound.yMin, stage.yMin + stage.height * 0.6f, where + " Launch at the bottom");
                    var back = panel.Root.Q<Button>(UiIds.StarterBack).worldBound;
                    Assert.Less(back.xMin, stage.xMin + stage.width * 0.25f, where + " Back top-left");
                    Assert.Less(back.yMin, stage.yMin + stage.height * 0.15f, where + " Back top-left");

                    if (!capture) return null;
                    string path = DeviceCapture.Save("starter-" + d.Name, surface.Stage, surface.Texture, surface.Window, out var kind);
                    TestContext.WriteLine(where + " capture (" + kind + "): " + path);
                    return path;
                }
                finally { panel.Detach(); }
            }
        }

        static bool Shown(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent)
                if (p.resolvedStyle.display == DisplayStyle.None) return false;
            return e.panel != null && e.worldBound.width > 0.5f && e.worldBound.height > 0.5f;
        }

        static void Inside(Rect r, Rect outer, string where)
        {
            Assert.That(r.xMin >= outer.xMin - 0.5f && r.yMin >= outer.yMin - 0.5f && r.xMax <= outer.xMax + 0.5f && r.yMax <= outer.yMax + 0.5f,
                Is.True, where + " " + r + " outside " + outer);
        }

        static void NoSiblingOverlap(VisualElement root, string where)
        {
            var kids = new List<VisualElement>();
            foreach (var c in root.Children())
                if (Shown(c)) kids.Add(c);
            for (int i = 0; i < kids.Count; i++)
            {
                for (int j = i + 1; j < kids.Count; j++)
                    Assert.That(Shrink(kids[i].worldBound).Overlaps(Shrink(kids[j].worldBound)), Is.False,
                        where + ": " + kids[i].name + " " + kids[i].worldBound + " overlaps " + kids[j].name + " " + kids[j].worldBound);
                if (!(kids[i] is TextElement)) NoSiblingOverlap(kids[i], where);
            }
        }

        /// <summary>Every shown element inside its parent (the screen root is the stage-sized layer).</summary>
        static void InsideParents(VisualElement e, string where)
        {
            foreach (var c in e.Children())
            {
                if (!Shown(c)) continue;
                if (e.name != UiIds.ScreenStarter) Inside(c.worldBound, e.worldBound, where + " " + c.name + " inside " + e.name);
                InsideParents(c, where);
            }
        }

        static Rect Shrink(Rect r) => new Rect(r.x + 0.5f, r.y + 0.5f, Mathf.Max(0f, r.width - 1f), Mathf.Max(0f, r.height - 1f));

        /// <summary>MeasureTextSize against the content rect: the one-line need for no-wrap labels, else the longest
        /// word and the wrapped height.</summary>
        static void TextFitsContent(TextElement e, string where)
        {
            if (e == null || string.IsNullOrEmpty(e.text)) return;
            float w = e.contentRect.width;
            if (float.IsNaN(w) || w < 1f) w = e.layout.width;
            float h = e.contentRect.height;
            if (float.IsNaN(h) || h < 1f) h = e.layout.height;
            Assert.Greater(w, 1f, where + " has no room for its text");
            var one = e.MeasureTextSize(e.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
            if (e.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
            {
                Assert.That(one.x, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " need " + one.x.ToString("0") + " box " + w.ToString("0"));
                Assert.That(one.y, Is.LessThanOrEqualTo(h + TextFit.Slack), where + " line " + one.y.ToString("0") + " box " + h.ToString("0"));
                return;
            }
            float longest = 0f;
            foreach (var word in e.text.Split(' '))
                if (word.Length > 0) longest = Mathf.Max(longest, e.MeasureTextSize(word, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x);
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

        /// <summary>Same device surface as WebDesignerOverlapDeviceTests, document first so the capture can read it.</summary>
        sealed class DeviceSurface : IDisposable
        {
            EditorWindow window;
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }
            public RenderTexture Texture => texture;
            public EditorWindow Window => window;

            /// <summary>The UIDocument on a render texture first (it can be read back as a PNG), else an editor window.</summary>
            public static DeviceSurface Open(float w, float h)
            {
                var surface = new DeviceSurface();
                surface.TryDocument(w, h);
                surface.Pump(6);
                if (!surface.Sized(w, h))
                {
                    surface.CloseDocument();
                    surface.TryWindow(w, h);
                    surface.Pump(4);
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

            void CloseDocument()
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
                go = null; settings = null; texture = null; Stage = null;
            }

            public void Dispose()
            {
                CloseWindow();
                CloseDocument();
            }
        }

        sealed class StarterDeviceWindow : EditorWindow { }
    }
}
