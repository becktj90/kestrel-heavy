// Device stand-in: KESTREL_REPO_ROOT is an empty directory, and the card fonts
// come from Resources. The board still starts every KH mission, and the HUD
// comes back after the result card.
using Kestrel.Core.Ui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class CampaignFlightLoopDeviceTests
    {
        string _prev;
        string _away;

        [SetUp]
        public void EmptyRoot()
        {
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            _away = Path.Combine(Path.GetTempPath(), "kh-empty-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
        }

        [TearDown]
        public void RestoreRoot()
        {
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _prev);
            if (Directory.Exists(_away)) Directory.Delete(_away, true);
        }

        [Test]
        public void ResourcesFonts_AndTheBoardStartsEveryMission()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            var title = Resources.Load<Font>("KHFonts/Fraunces-Bold");
            var body = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            var heavy = Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold");
            Assert.That(title, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(heavy, Is.Not.Null);
            Assert.That(ReportCardChrome.TitleFace(), Is.SameAs(title));
            Assert.That(ReportCardChrome.BodyFace(), Is.SameAs(body));
            Assert.That(ReportCardChrome.ButtonFace(), Is.SameAs(heavy));

            var campaign = Campaign.Load(FindRepo());
            var career = Career.Start(campaign, 100000);
            career.Cleared = 20;
            var launched = new List<string>();
            using (var surface = PanelHost.Open(390f, 844f))
            {
                var board = MissionBoardView.Create(
                    campaign, career,
                    new Dictionary<string, MissionRecord>(),
                    new Dictionary<string, PayloadLabel>(),
                    launched.Add, _ => { }, () => { }, () => { },
                    true, _ => true, 390f, 844f, false);
                Assert.That(board, Is.Not.Null);
                surface.Stage.Add(board);
                surface.Pump(4);
                for (int i = 1; i <= 20; i++)
                {
                    string id = "m" + i;
                    var node = board.Q<Button>("mission-node-" + id);
                    Assert.That(node, Is.Not.Null, id);
                    Click(node);
                    Click(board.Q<Button>("mission-primary"));
                    Assert.That(launched, Does.Contain(id), "board start " + id);
                }
            }
        }

        [Test]
        public void ResultCardButtonsWork_AndTheHudReturns()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            using (var surface = PanelHost.Open(390f, 844f))
            {
            var stage = surface.Stage;
            var hud = new VisualElement { name = UiIds.ScreenFlight, pickingMode = PickingMode.Ignore };
            hud.Add(new Label("T+ 12.0") { name = "h-t" });
            hud.Add(new Label("ALT 4.0 km") { name = "h-alt" });
            hud.Add(new Label("Q 40") { name = "h-q" });
            hud.Add(new Label("On the count") { name = "h-verdict" });
            var skip = new Button { name = UiIds.FlightSkipCount, text = "Skip count" };
            hud.Add(skip);
            var nudge = new VisualElement { name = "ascent-nudge" };
            nudge.Add(new Button { name = "nudge-plus", text = "+" });
            hud.Add(nudge);
            stage.Add(hud);
            stage.Add(new VisualElement { name = "guide-pop", pickingMode = PickingMode.Ignore });

            int home = 0, missions = 0, retry = 0, next = 0;
            var card = ReportView.Build(
                new FailureEvent { kind = "structural-maxq", t = 20, kid = "The air pushed too hard.", engineer = "q crossed the wall." },
                new TelemetryLog(),
                ReportLayout.Solve(390, 844, 20, 0, 0, 0, true),
                true,
                null,
                FlightDebrief.Compose(null, MissionContext.Free(), new FlightFacts { Passed = false, Seed = 1 }, NarrationPool.Empty, 100000, null),
                new ReportActions
                {
                    Home = () => home++,
                    Missions = () => missions++,
                    Again = () => retry++,
                    Next = () => next++,
                });
            stage.Add(card);
            ReportCardChrome.RetireFlight(stage);
            Assert.That(hud.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(skip.enabledSelf, Is.False);
            Assert.That(stage.Q<Label>("h-t").text, Is.EqualTo(""));
            Assert.That(stage.Q<Label>("h-alt").text, Is.EqualTo(""));

            Click(card.Q<Button>("report-home"));
            Click(card.Q<Button>("report-missions"));
            Click(card.Q<Button>("report-retry"));
            Click(card.Q<Button>("report-next"));
            Assert.That(home, Is.EqualTo(1));
            Assert.That(missions, Is.EqualTo(1));
            Assert.That(retry, Is.EqualTo(1));
            Assert.That(next, Is.EqualTo(1));

            ReportCardChrome.Dismiss(stage);
            ReportCardChrome.RestoreFlight(stage);
            Assert.That(stage.Q(ReportView.RootName), Is.Null, "retry removes the card");
            Assert.That(hud.style.display.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(hud.ClassListContains("hidden"), Is.False);
            Assert.That(skip.enabledSelf, Is.True);
            Assert.That(skip.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(stage.Q("guide-pop").pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(hud.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(stage.Q("ascent-nudge").style.display.keyword, Is.EqualTo(StyleKeyword.Null));
            }
        }

        [Test]
        public void ResultStoryStaysAboveTheButtons_OnPhoneAndWeb()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            Assert.That(Resources.Load<Font>("KHFonts/Fraunces-Bold"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"), Is.Not.Null);
            Assert.That(ReportCardChrome.UnitsPerPoint(1280f, 800f, 96f, 1280f), Is.EqualTo(2f).Within(0.01f));
            Assert.That(1280f / (800f / 2f), Is.EqualTo(3.2f).Within(0.01f), "the swapped landscape scale this guards");
            var sizes = new[]
            {
                new[] { 335f, 700f, 96f },
                new[] { 375f, 667f, 326f },
                new[] { 440f, 956f, 458f },
                new[] { 1032f, 1376f, 264f },
                new[] { 1280f, 800f, 96f },
            };
            for (int i = 0; i < sizes.Length; i++)
            {
                float w = sizes[i][0], h = sizes[i][1], dpi = sizes[i][2];
                float upp = ReportCardChrome.UnitsPerPoint(w, h, dpi, w);
                using (var surface = PanelHost.Open(w, h))
                {
                    var debrief = new FlightDebrief
                    {
                        CauseTitle = "Short of the mark",
                        Numbers = "Reached 22 km. The goal was 10 km.",
                        StarsLine = "No stars this flight.",
                        MedalsLine = "Timing Bronze",
                        Kid = "It climbed past the hop.",
                        Engineer = "Apogee 22 km. The band was 10 km.",
                    };
                    var card = ReportView.Build(
                        null,
                        new TelemetryLog(),
                        ReportLayout.Solve(w, h, 20f, 0f, 0f, 0f, true),
                        true,
                        null,
                        debrief,
                        new ReportActions());
                    surface.Stage.Add(card);
                    ReportCardChrome.Present(surface.Stage, card, upp, 20f, 0f);
                    surface.Pump(6);
                    string where = w.ToString("0") + "x" + h.ToString("0");
                    AssertStory(card, "failure-cause", where);
                    AssertStory(card, "failure-numbers", where);
                    AssertStory(card, "report-stars", where);
                    var numbers = card.Q<Label>("failure-numbers");
                    StringAssert.Contains("22", numbers.text);
                    StringAssert.Contains("10", numbers.text);
                    StringAssert.Contains("km", numbers.text);
                    var actions = card.Q("report-actions");
                    Assert.That(actions, Is.Not.Null, where);
                    Assert.That(actions.worldBound.height, Is.GreaterThan(8f), where + " buttons");
                    Assert.That(numbers.worldBound.yMin, Is.LessThan(actions.worldBound.yMin), where + " numbers start above the buttons");
                    Assert.That(numbers.worldBound.center.y, Is.LessThan(actions.worldBound.yMin), where + " numbers sit above the buttons");
                }
            }
        }

        [Test]
        public void LandscapeReadoutsLeaveTheFlightAndTheResultCard()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            using (var surface = PanelHost.Open(1280f, 800f))
            {
                var ui = new VisualElement { name = "root" };
                ui.style.width = 1280f;
                ui.style.height = 800f;
                var design = new VisualElement { name = "design" };
                design.Add(new VisualElement { name = "sticky-strip" });
                design.Add(new Label("Reading 0.000, limit 0.000, at T+0.00 s.") { name = "risk-chips" });
                design.Add(new VisualElement { name = "launch-row" });
                design.Add(new Label { name = "launch-reason" });
                design.Add(new VisualElement { name = "designer-launch" });
                var hud = new VisualElement { name = UiIds.ScreenFlight };
                hud.AddToClassList("hidden");
                ui.Add(design);
                ui.Add(hud);
                surface.Stage.Add(ui);
                var frame = WebDesignerLayout.Place(1280f, 800f, 0f, 0f, 0f, 0f);
                Assert.IsTrue(frame.Wide);
                try
                {
                    WebDesignChrome.Apply(ui, frame);
                    var host = ui.Q("web-readouts");
                    Assert.That(host, Is.Not.Null);
                    Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                    design.AddToClassList("hidden");
                    WebDesignChrome.Apply(ui, frame);
                    Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.None));

                    design.RemoveFromClassList("hidden");
                    hud.RemoveFromClassList("hidden");
                    WebDesignChrome.Apply(ui, frame);
                    Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.None));

                    hud.AddToClassList("hidden");
                    ui.Add(new VisualElement { name = ReportView.RootName });
                    WebDesignChrome.Apply(ui, frame);
                    Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.None));
                }
                finally
                {
                    WebDesignChrome.Clear(ui);
                }
            }
        }

        [Test]
        public void EmptyCheckoutStillAimsAnApogeeHop()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            GameSession.Clear();
            try
            {
                GameSession.Carry(new MissionContext
                {
                    MissionId = "m-missing",
                    Mode = MissionMode.Campaign,
                    Target = new MissionTarget { Kind = "apogee", ApoKm = 10 },
                });
                var missing = AscentFlight.CurrentMission();
                Assert.That(missing, Is.Not.Null);
                Assert.IsTrue(MissionAim.IsApogeeHop(missing));
                Assert.AreEqual(1000, missing.GuidanceAltM, 0.01);

                var campaign = ScreenStub.LoadCampaign();
                var row = campaign != null ? campaign.ById("m1") : null;
                if (row != null)
                {
                    GameSession.Carry(MissionContext.FromMission(campaign, row));
                    var hop = AscentFlight.CurrentMission();
                    Assert.AreEqual("m1", hop.Id);
                    Assert.AreEqual("apogee", hop.TargetKind);
                    Assert.AreEqual(1000, hop.GuidanceAltM, 0.01);
                }
                var opts = new SimOptions { targetAlt = 200000, guidance = GuidanceProfile.Builtin(200000) };
                Assert.IsTrue(MissionAim.Apply(opts, missing));
                Assert.AreEqual(1000, opts.targetAlt, 0.01);
                Assert.AreEqual(1000, opts.guidance.targetAlt, 0.01);
            }
            finally
            {
                GameSession.Clear();
            }
        }

        static void AssertStory(VisualElement card, string name, string where)
        {
            var label = card.Q<Label>(name);
            Assert.That(label, Is.Not.Null, where + " " + name);
            Assert.That(string.IsNullOrEmpty(label.text), Is.False, where + " " + name);
            Assert.That(label.worldBound.height, Is.GreaterThan(8f), where + " " + name);
        }

        static string FindRepo()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "data", "campaign.json"))) return dir.FullName;
                dir = dir.Parent;
            }
            return "/workspace";
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

        /// <summary>
        /// Same batch-mode panel as HomeDeviceLayoutTests. NavigationSubmitEvent is a no-op
        /// until the button is attached to a sized panel.
        /// </summary>
        sealed class PanelHost : IDisposable
        {
            EditorWindow _window;
            GameObject _go;
            PanelSettings _settings;
            RenderTexture _rt;
            public VisualElement Stage { get; private set; }

            public static PanelHost Open(float w, float h)
            {
                var host = new PanelHost();
                host.TryWindow(w, h);
                host.Pump(4);
                if (!host.Sized(w, h))
                {
                    host.CloseWindow();
                    host.TryDocument(w, h);
                    host.Pump(6);
                }
                Assert.That(host.Stage, Is.Not.Null);
                Assert.That(host.Stage.panel, Is.Not.Null, "button events need a panel");
                Assert.That(host.Sized(w, h), Is.True, "panel did not take the device size");
                return host;
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
                    ApplySettings();
                    UpdatePanels();
                    if (_window != null) _window.Repaint();
                    Validate(Stage != null ? Stage.panel : null);
                }
            }

            bool Sized(float w, float h)
            {
                if (Stage == null || Stage.panel == null) return false;
                var b = Stage.worldBound;
                if (b.width < 8f || b.height < 8f) return false;
                float sx = b.width / w, sy = b.height / h;
                return sx >= 0.8f && sy >= 0.8f && sx <= 3.5f && sy <= 3.5f && Math.Abs(sx - sy) <= 0.15f;
            }

            void TryWindow(float w, float h)
            {
                try
                {
                    _window = ScriptableObject.CreateInstance<FlightLoopWindow>();
                    _window.position = new Rect(0f, 0f, w, h);
                    _window.minSize = new Vector2(w, h);
                    _window.maxSize = new Vector2(w, h);
                    _window.ShowUtility();
                    var root = _window.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { Stage = null; }
            }

            void TryDocument(float w, float h)
            {
                try
                {
                    _rt = new RenderTexture(Mathf.Max(8, (int)w), Mathf.Max(8, (int)h), 0, RenderTextureFormat.ARGB32);
                    _rt.Create();
                    _settings = ScriptableObject.CreateInstance<PanelSettings>();
                    _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    _settings.scale = 1f;
                    _settings.targetTexture = _rt;
                    var theme = Resources.Load<PanelSettings>("UI/KH_ShellPanelSettings");
                    if (theme != null && theme.themeStyleSheet != null) _settings.themeStyleSheet = theme.themeStyleSheet;
                    _go = new GameObject("kh-flight-loop");
                    _go.hideFlags = HideFlags.HideAndDontSave;
                    var doc = _go.AddComponent<UIDocument>();
                    doc.panelSettings = _settings;
                    ApplySettings();
                    var root = doc.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { Stage = null; }
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

            void ApplySettings()
            {
                if (_settings == null) return;
                var m = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) m.Invoke(_settings, null);
            }

            static void UpdatePanels()
            {
                var t = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                var m = t != null ? t.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                if (m != null && m.GetParameters().Length == 0) m.Invoke(null, null);
            }

            static void Validate(IPanel panel)
            {
                if (panel == null) return;
                var m = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && m.GetParameters().Length == 0) m.Invoke(panel, null);
            }

            void CloseWindow()
            {
                if (_window == null) return;
                _window.Close();
                _window = null;
                Stage = null;
            }

            public void Dispose()
            {
                CloseWindow();
                if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
                if (_settings != null) UnityEngine.Object.DestroyImmediate(_settings);
                if (_rt != null)
                {
                    _rt.Release();
                    UnityEngine.Object.DestroyImmediate(_rt);
                }
            }
        }

        sealed class FlightLoopWindow : EditorWindow { }
    }
}
