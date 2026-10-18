// Every menu, designer tab, pause sheet and result card at the widths Trevor checks.
// 335, 390 and 430 portrait, and the same phones on their side. KESTREL_REPO_ROOT is an
// empty directory. Fonts and campaign data come from Resources only.
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
using Kestrel.Slice.DesignerV3.Stepper;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class TextFitDeviceSweepTests
    {
        const string Tokens = "Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss";

        struct Frame
        {
            public string Name;
            public float W, H;
            public bool Landscape;
            public Frame(string name, float w, float h)
            {
                Name = name;
                W = w;
                H = h;
                Landscape = w > h;
            }
        }

        static readonly Frame[] Frames =
        {
            new Frame("335 portrait", 335f, 720f),
            new Frame("390 portrait", 390f, 844f),
            new Frame("430 portrait", 430f, 932f),
            new Frame("335 landscape", 720f, 335f),
            new Frame("390 landscape", 844f, 390f),
            new Frame("430 landscape", 932f, 430f),
        };

        static Catalog catalog;
        static HangarData hangar;
        string prevRoot;
        string away;
        int prevEngineer;

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
            finally
            {
                Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev);
            }
        }

        [SetUp]
        public void EmptyCheckout()
        {
            away = Path.Combine(Path.GetTempPath(), "kh-textfit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            prevRoot = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            StepperCopy.Reset();
            MainMenuOverlay.ForceWide = null;
            prevEngineer = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            Assert.That(Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"), Is.Not.Null);
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
        public void HomeMissionsPatchesCodexSettingsBriefingReportAndPauseFit()
        {
            foreach (var frame in Frames)
            {
                MainMenuOverlay.ForceWide = frame.Landscape;
                using (var surface = DeviceSurface.Open(frame.W, frame.H))
                {
                    var home = MountHome(surface.Stage);
                    surface.Pump(8);
                    TextFit.Apply(home);
                    AssertFits(home, frame.Name + " home");
                    var design = home.Q<Button>("home-design");
                    var designLabel = design != null ? design.Q<Label>(className: "kh-mm-design-label") : null;
                    Assert.That(designLabel, Is.Not.Null, frame.Name);
                    Assert.That(designLabel.text, Is.EqualTo(MainMenuCopy.Design.Label));
                    AssertStringInside(designLabel, frame.Name + " DESIGN FIRST");

                    Click(home.Q<Button>("home-settings"));
                    surface.Pump(4);
                    TextFit.Apply(home);
                    var sheet = home.Q("home-settings-sheet");
                    Assert.That(sheet, Is.Not.Null, frame.Name);
                    Assert.That(sheet.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), frame.Name + " settings");
                    AssertFits(sheet, frame.Name + " settings");

                    surface.Stage.Clear();
                    var board = MountBoard(surface.Stage, frame);
                    surface.Pump(6);
                    TextFit.Apply(board);
                    AssertFits(board, frame.Name + " missions");

                    surface.Stage.Clear();
                    var patches = MountInColumn(surface.Stage, PatchCollectionScreen.Build(null, null, null, () => { }));
                    surface.Pump(4);
                    TextFit.Apply(patches);
                    AssertFits(patches, frame.Name + " patches");

                    surface.Stage.Clear();
                    var codex = MountInColumn(surface.Stage, CodexScreen.Build(() => { }));
                    surface.Pump(4);
                    TextFit.Apply(codex);
                    AssertFits(codex, frame.Name + " codex");

                    surface.Stage.Clear();
                    var briefing = MountInColumn(surface.Stage, BriefingScreen.Build(SampleMission(), () => { }, () => { }));
                    surface.Pump(4);
                    TextFit.Apply(briefing);
                    AssertFits(briefing, frame.Name + " briefing");

                    surface.Stage.Clear();
                    var card = ReportView.Build(
                        new FailureEvent { kind = "structural-maxq", t = 26, kid = "The air pushed too hard.", engineer = "Dynamic pressure crossed the wall." },
                        SampleLog(),
                        ReportLayout.Solve(frame.W, frame.H, 0, 0, 0, 0, true),
                        true,
                        null,
                        SampleDebrief(),
                        new ReportActions());
                    surface.Stage.Add(card);
                    surface.Pump(4);
                    TextFit.Apply(card);
                    AssertFits(card, frame.Name + " result");

                    surface.Stage.Clear();
                    var pause = MountPause(surface.Stage);
                    try
                    {
                        surface.Pump(4);
                        TextFit.Apply(pause.Hud);
                        AssertFits(pause.Hud.Q("pause-menu"), frame.Name + " pause");
                    }
                    finally { pause.Dispose(); }
                }
            }
        }

        [Test]
        public void EveryDesignerTabFitsAndTheGuidedFlowStillAdvances()
        {
            var session = new HangarSession(catalog, hangar, Template("workhorse"));
            foreach (var frame in Frames)
            {
                using (var surface = DeviceSurface.Open(frame.W, frame.H))
                {
                    var host = new VisualElement { name = "designer-host" };
                    host.style.position = Position.Absolute;
                    host.style.left = 0f;
                    host.style.top = 0f;
                    host.style.right = 0f;
                    host.style.bottom = 0f;
                    surface.Stage.Add(host);
                    var panel = new DesignerV3Panel(session, host, 1f);
                    try
                    {
                        panel.SetEngineer(true);
                        foreach (var tab in DesignerV3Tabs.All)
                        {
                            panel.Show(tab);
                            surface.Pump(3);
                            TextFit.Apply(panel.Root);
                            AssertFits(panel.Root, frame.Name + " " + tab);
                        }
                        panel.SetEngineer(false);
                        foreach (var tab in DesignerV3Tabs.Simple)
                        {
                            panel.Show(tab);
                            surface.Pump(2);
                            TextFit.Apply(panel.Root);
                            AssertFits(panel.Root, frame.Name + " simple " + tab);
                        }
                    }
                    finally { panel.Detach(); }
                }
            }

            using (var surface = DeviceSurface.Open(335f, 720f))
                WalkGuided(surface, session, "335 portrait");
            using (var surface = DeviceSurface.Open(844f, 390f))
                WalkGuided(surface, session, "390 landscape");
        }

        static void WalkGuided(DeviceSurface surface, HangarSession session, string where)
        {
            var host = new VisualElement { name = "designer-host" };
            host.style.position = Position.Absolute;
            host.style.left = 0f;
            host.style.top = 0f;
            host.style.right = 0f;
            host.style.bottom = 0f;
            surface.Stage.Add(host);
            var panel = new DesignerV3Panel(session, host, 1f);
            try
            {
                panel.SetEngineer(true);
                Assert.That(panel.Stepper, Is.Not.Null, where);
                panel.Stepper.EnterGuided();
                var seen = new HashSet<DesignerStep>();
                for (int i = 0; i < 16 && seen.Count < StepperCatalog.Count; i++)
                {
                    seen.Add(panel.Stepper.Step);
                    surface.Pump(2);
                    TextFit.Apply(panel.Root);
                    AssertFits(panel.Root, where + " step " + panel.Stepper.Step);
                    if (panel.Stepper.PressForward() || panel.Stepper.PressForward()) continue;
                    var next = panel.Stepper.Root.Q<Button>("stepper-next");
                    if (next != null && next.resolvedStyle.display != DisplayStyle.None) Click(next);
                    else panel.Stepper.PressFix();
                }
                seen.Add(panel.Stepper.Step);
                Assert.That(seen.Count, Is.EqualTo(StepperCatalog.Count), where + " " + string.Join(",", seen));
            }
            finally { panel.Detach(); }
        }

        static void AssertFits(VisualElement root, string where)
        {
            Assert.That(root, Is.Not.Null, where);
            var problems = TextFit.Problems(root);
            Assert.That(problems, Is.Empty, where + "\n" + string.Join("\n", problems));
        }

        static void AssertStringInside(TextElement label, string where)
        {
            Assert.That(label, Is.Not.Null, where);
            float w = label.contentRect.width;
            if (float.IsNaN(w) || w < 8f) w = label.layout.width;
            float h = label.contentRect.height;
            if (float.IsNaN(h) || h < 4f) h = label.layout.height;
            Assert.That(w, Is.GreaterThan(8f), where + " width");
            Assert.That(h, Is.GreaterThan(4f), where + " height");
            var need = label.MeasureTextSize(label.text, w, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined);
            Assert.That(need.x, Is.GreaterThan(4f), where + " did not measure");
            Assert.That(need.x, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " need " + need.x.ToString("0") + " box " + w.ToString("0"));
            Assert.That(need.y, Is.LessThanOrEqualTo(h + TextFit.Slack), where + " tall need " + need.y.ToString("0") + " box " + h.ToString("0"));
            var parent = label.parent != null ? label.parent.worldBound : label.worldBound;
            var box = label.worldBound;
            Assert.That(box.xMax, Is.LessThanOrEqualTo(parent.xMax + TextFit.Slack), where + " spills");
            Assert.That(box.yMax, Is.LessThanOrEqualTo(parent.yMax + TextFit.Slack), where + " spills down");
        }

        static VisualElement MountHome(VisualElement stage)
        {
            var shell = Column(stage, out var host);
            MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, true);
            return shell;
        }

        static VisualElement MountBoard(VisualElement stage, Frame frame)
        {
            var shell = Column(stage, out var host);
            var board = MissionBoardScreen.Build(
                ScreenStub.LoadCampaign(), null, null, null,
                _ => { }, _ => { }, () => { }, () => { },
                true, _ => false, frame.W, frame.H, frame.Landscape);
            ScreenStub.FillHost(board);
            host.Add(board);
            return shell;
        }

        static VisualElement MountInColumn(VisualElement stage, VisualElement view)
        {
            var shell = Column(stage, out var host);
            ScreenStub.FillHost(view);
            host.Add(view);
            return shell;
        }

        static VisualElement Column(VisualElement stage, out VisualElement host)
        {
            var shell = new VisualElement { name = "shell", pickingMode = PickingMode.Ignore };
            shell.AddToClassList("kh-root");
            shell.style.position = Position.Absolute;
            shell.style.left = 0f;
            shell.style.right = 0f;
            shell.style.top = 0f;
            shell.style.bottom = 0f;
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Tokens);
            if (sheet != null) shell.styleSheets.Add(sheet);
            stage.Add(shell);
            var safe = new VisualElement { name = "safe", pickingMode = PickingMode.Ignore };
            safe.style.position = Position.Absolute;
            safe.style.left = 0f;
            safe.style.right = 0f;
            safe.style.top = 0f;
            safe.style.bottom = 0f;
            shell.Add(safe);
            host = new VisualElement { name = "home-host", pickingMode = PickingMode.Ignore };
            host.AddToClassList("kh-screen");
            host.AddToClassList("kh-home");
            ScreenStub.FillHost(host);
            safe.Add(host);
            return shell;
        }

        sealed class PauseMount : IDisposable
        {
            public VisualElement Hud;
            public GameObject go;
            public void Dispose()
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static PauseMount MountPause(VisualElement stage)
        {
            var hud = new VisualElement { name = "hud" };
            hud.style.position = Position.Absolute;
            hud.style.left = 0f;
            hud.style.top = 0f;
            hud.style.right = 0f;
            hud.style.bottom = 0f;
            stage.Add(hud);
            var go = new GameObject("kh-pause-fit");
            go.hideFlags = HideFlags.HideAndDontSave;
            var chrome = go.AddComponent<FlightChrome>();
            chrome.Bind(hud, _ => { }, () => { }, () => { });
            chrome.Layout(1f);
            chrome.Show(true);
            return new PauseMount { Hud = hud, go = go };
        }

        static MissionContext SampleMission()
        {
            var campaign = ScreenStub.LoadCampaign();
            if (campaign != null)
            {
                var ctx = MissionContext.FromCampaign(campaign, "m1");
                if (ctx != null) return ctx;
            }
            return MissionContext.Free();
        }

        static TelemetryLog SampleLog()
        {
            var log = new TelemetryLog();
            log.Push(0, 0, 0, 0, 1, 1000, 0, 0, 0);
            log.Push(26, 8000, 400, 35000, 3, 700, 20, 8, 4);
            return log;
        }

        static FlightDebrief SampleDebrief()
        {
            var facts = new FlightFacts { Passed = false, Seed = 1 };
            return FlightDebrief.Compose(null, MissionContext.Free(), facts, NarrationPool.Empty, 100000, null);
        }

        static JsonObject Template(string id)
        {
            var design = DesignMigration.Migrate(hangar.Template(id).design);
            HangarEditor.Normalize(design);
            return design;
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
                    window = ScriptableObject.CreateInstance<TextFitDeviceWindow>();
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
                    go = new GameObject("kh-textfit-device");
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

        sealed class TextFitDeviceWindow : EditorWindow { }
    }
}
