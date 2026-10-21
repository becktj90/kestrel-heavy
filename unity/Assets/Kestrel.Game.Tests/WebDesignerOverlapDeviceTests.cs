// Web designer overlap audit (beckify.com 50ae4f3, Trevor's HP laptop), with real UI Toolkit rects.
// Landscape web: the guided stepper in the WebDesignerLayout parts column at 1280x720, 1366x768 and
// 1920x1080 (all a 1280x720 panel after WebPanelScale), and the free designer's launch row in the
// readout column. Portrait: the stepper at 335. Guided steps 1-3 with a warning showing. No sibling
// rects intersect in the stepper blocks, no label overflows its box (TextFit.Problems), and Fix it,
// Back, Engineer and All parts keep their words. Home: the credits chip clears KESTREL, stats and LAUNCH.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using Kestrel.Slice.DesignerV3.Stepper;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class WebDesignerOverlapDeviceTests
    {
        const string Tokens = "Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss";
        const float PanelPad = 12f, Handle = 56f, PanelBottom = 8f;

        static Catalog catalog;
        static HangarData hangar;
        string prevRoot;
        string away;
        int prevEngineer;

        struct Column { public string Name; public float SurfaceW, SurfaceH; public HudBox Box; public WebDesignerFrame Frame; }

        static IEnumerable<Column> StepperColumns()
        {
            foreach (var (w, h) in new[] { (1280, 720), (1366, 768), (1920, 1080) })
            {
                Panel(w, h, out float pw, out float ph);
                var f = WebDesignerLayout.Place(pw, ph, 0, 0, 0, 0);
                yield return new Column
                {
                    Name = w + "x" + h, SurfaceW = pw, SurfaceH = ph, Frame = f,
                    Box = new HudBox { X = f.Parts.X + PanelPad, Y = f.Parts.Y + Handle, W = f.Parts.W - 2f * PanelPad, H = f.Parts.H - Handle - PanelBottom },
                };
            }
            yield return new Column
            {
                Name = "335 portrait", SurfaceW = 335f, SurfaceH = 720f,
                Box = new HudBox { X = 8f, Y = Handle, W = 335f - 16f, H = 720f - Handle - 2f * PanelBottom },
            };
        }

        static void Panel(int w, int h, out float pw, out float ph)
        {
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
            pw = Mathf.Round(w / s);
            ph = Mathf.Round(h / s);
        }

        [OneTimeSetUp]
        public void LoadCatalogBeforeTheEmptyRoot()
        {
            string prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", null);
            try
            {
                string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
                catalog = Catalog.LoadFromRepo(root);
                hangar = HangarData.LoadFromRepo(root);
                DesignerLimits.Load(root);
            }
            finally { Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev); }
        }

        [SetUp]
        public void EmptyCheckout()
        {
            away = Path.Combine(Path.GetTempPath(), "kh-overlap-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            prevRoot = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            StepperCopy.Reset();
            MainMenuOverlay.ForceWide = null;
            prevEngineer = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
        }

        [TearDown]
        public void RestoreCheckout()
        {
            PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prevEngineer);
            MainMenuOverlay.ForceWide = null;
            StepperCopy.Reset();
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prevRoot);
            try { if (!string.IsNullOrEmpty(away) && Directory.Exists(away)) Directory.Delete(away, true); }
            catch (Exception) { }
        }

        [Test]
        public void GuidedStepsOneToThreeWithAWarningNeverOverlap()
        {
            foreach (var col in StepperColumns())
            {
                foreach (var step in new[] { DesignerStep.Mission, DesignerStep.Engines, DesignerStep.Tanks })
                {
                    var session = new HangarSession(catalog, hangar, WarningDesign(step));
                    using (var surface = DeviceSurface.Open(col.SurfaceW, col.SurfaceH))
                    {
                        var host = Host(surface.Stage, col.Box);
                        var panel = new DesignerV3Panel(session, host, 1f);
                        try
                        {
                            panel.SetEngineer(false);
                            bool wide = col.Frame != null;
                            panel.SetLandscape(wide);
                            if (wide) panel.SetWebColumn(col.Box.W, col.Box.H);
                            panel.Stepper.EnterGuided();
                            surface.Pump(3);
                            Settle(surface, panel.Root);
                            string where = col.Name + " step " + panel.Stepper.Step + " (" + panel.Stepper.Gate + ")";
                            var root = panel.Stepper.Root;
                            AssertStepper(root, col.Box, where);

                            // The confirm card ("This step still has a warning") must not land on Back.
                            var next = root.Q<Button>("stepper-next");
                            if (next != null && next.resolvedStyle.display != DisplayStyle.None)
                            {
                                Click(next);
                                surface.Pump(3);
                                Settle(surface, panel.Stepper.Root);
                                AssertStepper(panel.Stepper.Root, col.Box, where + " +confirm");
                            }
                        }
                        finally { panel.Detach(); }
                    }
                }
            }
        }

        [Test]
        public void FreeDesignerLaunchRowWrapsByWordsInTheReadoutColumn()
        {
            foreach (var col in StepperColumns())
            {
                if (col.Frame == null) continue;
                var session = new HangarSession(catalog, hangar, WarningDesign(DesignerStep.Tanks));
                using (var surface = DeviceSurface.Open(col.SurfaceW, col.SurfaceH))
                {
                    var right = col.Frame.RightPanel;
                    var host = Host(surface.Stage, new HudBox { X = right.X + 12f, Y = right.Y + 12f, W = right.W - 24f, H = right.H - 24f });
                    var panel = new DesignerV3Panel(session, host, 1f);
                    try
                    {
                        panel.SetEngineer(false);
                        panel.SetLandscape(true);
                        // WarningDesign saves a fresh guided progress, so the panel opens in Guided view with
                        // the launch row hidden (0 x 0). Measure the free designer this test is about.
                        panel.Stepper.ShowAllParts();
                        surface.Pump(3);
                        Settle(surface, panel.Root);
                        Assert.That(panel.Stepper.IsGuided, Is.False, col.Name + " free designer");
                        var row = panel.Root.Q("designer-launch");
                        Assert.That(row, Is.Not.Null, col.Name);
                        Assert.That(row.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), col.Name + " launch row shown");
                        Assert.That(row.worldBound.width, Is.GreaterThan(100f), col.Name + " launch row laid out " + row.worldBound);
                        NoSiblingOverlap(row, col.Name + " launch row");
                        var reason = row.Q<Label>("launch-gate-reason");
                        if (reason != null && !string.IsNullOrEmpty(reason.text))
                        {
                            Assert.That(reason.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), col.Name + " reason shown");
                            Assert.That(reason.worldBound.width, Is.GreaterThan(row.worldBound.width * 0.8f), col.Name + " the reason has its own row");
                            AssertWords(reason, col.Name + " reason");
                        }
                        foreach (var b in row.Query<Button>().ToList()) AssertWords(b, col.Name + " " + b.name);
                        Assert.That(TextFit.Problems(row), Is.Empty, col.Name);
                    }
                    finally { panel.Detach(); }
                }
            }
        }

        [Test]
        public void TheIssuePillFitsItsBandWithALongMissionTitle()
        {
            foreach (var col in StepperColumns())
            {
                if (col.Frame == null) continue;
                using (var surface = DeviceSurface.Open(col.SurfaceW, col.SurfaceH))
                {
                    var f = col.Frame;
                    var pill = new Label("12 things to fix") { name = "issue-chip" };
                    Pin(pill, f.Issue);
                    pill.style.fontSize = 15f;
                    pill.style.paddingLeft = 12f; pill.style.paddingRight = 12f;
                    pill.style.whiteSpace = WhiteSpace.NoWrap;
                    var panel = new VisualElement { name = "design" };
                    Pin(panel, f.Parts);
                    var title = new Button { name = "design-toggle", text = "▼  Kestrel Heavy demo mission to a very high polar orbit •" };
                    title.style.whiteSpace = WhiteSpace.Normal;
                    title.style.fontSize = 16f;
                    title.style.minHeight = 44f;
                    panel.Add(title);
                    surface.Stage.Add(panel);
                    surface.Stage.Add(pill);
                    surface.Pump(4);
                    Assert.That(pill.worldBound.Overlaps(title.worldBound), Is.False, col.Name + " pill on the title");
                    Assert.That(pill.worldBound.yMax, Is.LessThanOrEqualTo(panel.worldBound.yMin + 0.5f), col.Name + " pill above the panel");
                    AssertWords(pill, col.Name + " pill");
                    Assert.That(title.worldBound.xMax, Is.LessThanOrEqualTo(panel.worldBound.xMax + TextFit.Slack), col.Name + " title wraps inside");
                }
            }
        }

        [Test]
        public void HomeCreditsClearTheTitleStatsAndLaunch()
        {
            foreach (var (name, w, h, landscape) in new[] { ("390 portrait", 390f, 844f, false), ("335 portrait", 335f, 720f, false), ("375x667 SE portrait", 375f, 667f, false), ("1366x768", 1280f, 720f, true) })
            {
                MainMenuOverlay.ForceWide = landscape;
                using (var surface = DeviceSurface.Open(w, h))
                {
                    var shell = HomeColumn(surface.Stage, out var host);
                    var overlay = MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, true);
                    HomeResumeBar.Mount(overlay, 99999, true, () => { });
                    surface.Pump(8);
                    TextFit.Apply(shell);
                    surface.Pump(4);
                    var bar = overlay.Q(HomeResumeBar.BarName);
                    Assert.That(bar, Is.Not.Null, name);
                    foreach (var id in new[] { "mm-eyebrow", "mm-kestrel", "mm-heavy", "mm-stats", "home-primary", "home-design" })
                    {
                        var e = shell.Q(id);
                        if (e == null || e.resolvedStyle.display == DisplayStyle.None || e.worldBound.width < 1f) continue;
                        Assert.That(bar.worldBound.Overlaps(e.worldBound), Is.False, name + " credits on " + id + " " + bar.worldBound + " " + e.worldBound);
                    }
                    var chip = bar.Q<Label>(HomeResumeBar.ChipName);
                    if (chip != null) AssertWords(chip, name + " credits");
                }
            }
        }

        // ---- helpers ------------------------------------------------------------------------------

        /// <summary>A design whose step shows a warning: the Hopper case (thin margin, short of fuel).</summary>
        static JsonObject WarningDesign(DesignerStep step)
        {
            var design = DesignMigration.Migrate(hangar.Template("workhorse").design);
            HangarEditor.Normalize(design);
            var stage = HangarParts.Body(design, PartRef.Stage(0));
            double prop = stage != null ? stage.Num("propMass") : 0;
            if (prop > 0) HangarEditor.SetPropMass(design, PartRef.Stage(0), prop * 0.6);
            var progress = StepperProgress.FreshGuided();
            for (var s = DesignerStep.Mission; s <= step; s++) progress.Visited.Add(s);
            progress.Current = step;
            progress.Write(design);
            return design;
        }

        static VisualElement Host(VisualElement stage, HudBox box)
        {
            var host = new VisualElement { name = "designer-host" };
            Pin(host, box);
            host.style.flexDirection = FlexDirection.Column;
            stage.Add(host);
            return host;
        }

        static void Pin(VisualElement e, HudBox b)
        {
            e.style.position = Position.Absolute;
            e.style.left = b.X;
            e.style.top = b.Y;
            e.style.width = b.W;
            e.style.height = b.H;
        }

        static readonly string[] Blocks =
        {
            "stepper-root", "stepper-header", "stepper-tabs", "stepper-depth", "stepper-progress", "stepper-nav",
            "stepper-check", "stepper-confirm",
        };

        static void AssertStepper(VisualElement root, HudBox column, string where)
        {
            Assert.That(root, Is.Not.Null, where);
            Assert.That(root.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), where + " guided");
            foreach (var name in Blocks)
            {
                var block = name == "stepper-root" ? root : root.Q(name);
                if (block == null) continue;
                NoSiblingOverlap(block, where + " " + name);
            }
            var title = root.Q("stepper-title");
            var tabs = root.Q("stepper-tabs");
            var count = root.Q("stepper-count");
            if (title != null && tabs != null)
                Assert.That(title.worldBound.yMax, Is.LessThanOrEqualTo(tabs.worldBound.yMin + 0.5f), where + " title over the tabs");
            if (count != null && title != null)
                Assert.That(count.worldBound.yMax, Is.LessThanOrEqualTo(title.worldBound.yMin + 0.5f), where + " count over the title");
            var progress = root.Q("stepper-progress-scroll");
            if (tabs != null && progress != null)
                Assert.That(tabs.worldBound.yMax, Is.LessThanOrEqualTo(progress.worldBound.yMin + 0.5f), where + " tabs over the tiles");
            var check = root.Q("stepper-check");
            if (check != null && progress != null && check.parent == root)
                Assert.That(progress.worldBound.yMax, Is.LessThanOrEqualTo(check.worldBound.yMin + 0.5f), where + " tiles over the check");
            foreach (var id in new[] { "stepper-count", "stepper-simple", "stepper-engineer", "stepper-all-parts", "stepper-back", "stepper-forward", "stepper-next", "stepper-stay", "stepper-go" })
            {
                var e = root.Q<TextElement>(id);
                if (e == null || e.resolvedStyle.display == DisplayStyle.None) continue;
                AssertWords(e, where + " " + id);
            }
            foreach (var b in root.Query<Button>(className: "stepper-seg").ToList()) AssertWords(b, where + " " + b.name);
            var nav = root.Q("stepper-nav");
            float bottom = root.worldBound.yMin + column.H;
            if (nav != null)
                Assert.That(nav.worldBound.yMax, Is.LessThanOrEqualTo(bottom + TextFit.Slack), where + " nav inside the column");
            var problems = TextFit.Problems(root);
            Assert.That(problems, Is.Empty, where + "\n" + string.Join("\n", problems));
        }

        static void NoSiblingOverlap(VisualElement parent, string where)
        {
            var kids = new List<VisualElement>();
            foreach (var c in parent.Children())
            {
                if (c.resolvedStyle.display == DisplayStyle.None || c.style.position.value == Position.Absolute) continue;
                var r = c.worldBound;
                if (r.width < 1f || r.height < 1f) continue;
                kids.Add(c);
            }
            for (int i = 0; i < kids.Count; i++)
                for (int j = i + 1; j < kids.Count; j++)
                {
                    var a = Shrink(kids[i].worldBound);
                    var b = Shrink(kids[j].worldBound);
                    Assert.That(a.Overlaps(b), Is.False, where + ": " + Name(kids[i]) + " " + kids[i].worldBound + " overlaps " + Name(kids[j]) + " " + kids[j].worldBound);
                }
        }

        static Rect Shrink(Rect r) => new Rect(r.x + 0.5f, r.y + 0.5f, Mathf.Max(0f, r.width - 1f), Mathf.Max(0f, r.height - 1f));

        static string Name(VisualElement e) => string.IsNullOrEmpty(e.name) ? e.GetType().Name : e.name;

        /// <summary>The label's text fits its content box on as many lines as it has room for, without a split word.</summary>
        static void AssertWords(TextElement e, string where)
        {
            if (e == null || string.IsNullOrEmpty(e.text)) return;
            float w = e.contentRect.width;
            if (float.IsNaN(w) || w < 1f) w = e.layout.width;
            float h = e.contentRect.height;
            if (float.IsNaN(h) || h < 1f) h = e.layout.height;
            var one = e.MeasureTextSize(e.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
            if (e.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
            {
                Assert.That(one.x, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " need " + one.x.ToString("0") + " box " + w.ToString("0"));
                return;
            }
            // Wrapping copy: the longest word fits the box, and the wrapped text fits its height.
            float longest = 0f;
            foreach (var word in e.text.Split(' '))
            {
                if (word.Length == 0) continue;
                longest = Mathf.Max(longest, e.MeasureTextSize(word, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x);
            }
            Assert.That(longest, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " a word is wider than its box (" + longest.ToString("0") + " > " + w.ToString("0") + ")");
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

        static VisualElement HomeColumn(VisualElement stage, out VisualElement host)
        {
            var shell = new VisualElement { name = "shell", pickingMode = PickingMode.Ignore };
            shell.AddToClassList("kh-root");
            shell.style.position = Position.Absolute;
            shell.style.left = 0f; shell.style.right = 0f; shell.style.top = 0f; shell.style.bottom = 0f;
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Tokens);
            if (sheet != null) shell.styleSheets.Add(sheet);
            stage.Add(shell);
            var safe = new VisualElement { name = "safe", pickingMode = PickingMode.Ignore };
            safe.style.position = Position.Absolute;
            safe.style.left = 0f; safe.style.right = 0f; safe.style.top = 0f; safe.style.bottom = 0f;
            shell.Add(safe);
            host = new VisualElement { name = "home-host", pickingMode = PickingMode.Ignore };
            host.AddToClassList("kh-screen");
            host.AddToClassList("kh-home");
            ScreenStub.FillHost(host);
            safe.Add(host);
            return shell;
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
                if (!surface.Sized(w, h))
                    Assert.Fail("panel did not take the device size " + w + "x" + h);
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
                    window = ScriptableObject.CreateInstance<WebOverlapDeviceWindow>();
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
                    go = new GameObject("kh-web-overlap-device");
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
                if (tree != null)
                {
                    tree.style.width = w;
                    tree.style.height = h;
                }
                panelRoot.style.width = w;
                panelRoot.style.height = h;
                var stage = new VisualElement { name = "stage", pickingMode = PickingMode.Ignore };
                stage.style.position = Position.Absolute;
                stage.style.left = 0f;
                stage.style.top = 0f;
                stage.style.width = w;
                stage.style.height = h;
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
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        sealed class WebOverlapDeviceWindow : EditorWindow { }
    }
}
