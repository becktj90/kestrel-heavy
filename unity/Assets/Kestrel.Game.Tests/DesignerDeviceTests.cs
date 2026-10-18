// Portrait designer on SE, Pro Max, and iPad. Empty KESTREL_REPO_ROOT, Resources fonts only.
// Every tab and guided step exposes its controls. Edits keep the scroll offset. Targets stay tappable.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using Kestrel.Slice.DesignerV3.Stepper;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class DesignerDeviceTests
    {
        struct Device
        {
            public string Name;
            public float W, H;
            public Device(string name, float w, float h) { Name = name; W = w; H = h; }
        }

        static readonly Device[] Devices =
        {
            new Device("SE", 375f, 667f),
            new Device("ProMax", 440f, 956f),
            new Device("iPad", 1032f, 1376f),
            new Device("Phone335", 335f, 700f),
            new Device("WebLandscape", 1280f, 800f),
            new Device("PartsColumn", 307f, 800f),
        };

        static Catalog catalog;
        static HangarData data;
        string prevRoot;
        string away;
        int prevEngineer;

        [OneTimeSetUp]
        public void LoadCatalogBeforeTheEmptyRoot()
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            catalog = Catalog.LoadFromRepo(root);
            data = HangarData.LoadFromRepo(root);
            DesignerLimits.Load(root);
        }

        [SetUp]
        public void EmptyCheckout()
        {
            away = Path.Combine(Path.GetTempPath(), "kh-designer-norepo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            prevRoot = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            StepperCopy.Reset();
            StepperControls.ResetShared();
            prevEngineer = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
        }

        [TearDown]
        public void RestoreCheckout()
        {
            PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prevEngineer);
            StepperCopy.Reset();
            StepperControls.ResetShared();
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prevRoot);
            try { if (!string.IsNullOrEmpty(away) && Directory.Exists(away)) Directory.Delete(away, true); }
            catch (Exception) { }
        }

        [Test]
        public void EmptyRepo_UsesResourcesCopyAndDoesNotWalk()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            string why = null;
            Assert.DoesNotThrow(() => why = StepperCopy.Current.Resolve(StepperCopy.WhyKidId(DesignerStep.Engines)));
            Assert.That(why, Is.Not.Null.And.Not.Empty);
            Assert.DoesNotThrow(() => StepperControls.SharedCampaign());
        }

        [Test]
        public void EveryTab_ShowsItsOptions()
        {
            var session = new HangarSession(catalog, data, Template("workhorse"));
            session.Apply(d => HangarEditor.SetFins(d, true));
            var panel = new DesignerV3Panel(session, new VisualElement(), 1f);
            try
            {
                panel.SetEngineer(true);
                Expect(panel, DesignerV3Tab.Propellant, "field-propellant", "field-mixtureOffsetPct");
                Expect(panel, DesignerV3Tab.Engines, "field-engine", "field-engineCount", "field-cluster", "field-pc", "field-expansionRatio", "field-gimbal", "field-throttle");
                Expect(panel, DesignerV3Tab.Tanks, "field-propMass", "field-pressure");
                Expect(panel, DesignerV3Tab.Stages, "field-addStage", "field-strapOns");
                Expect(panel, DesignerV3Tab.Aero, "field-fins", "field-finCount", "field-finSpan", "field-nose", "field-fairing", "field-fairingDiameter", "field-maxQThrottle");
                Expect(panel, DesignerV3Tab.Structure, "field-material", "field-safetyFactor", "field-avionics");
                Expect(panel, DesignerV3Tab.Guidance, "field-pitchKickAlt", "field-pitchKickDeg", "field-maxQThrottle", "field-gLimit");
                Expect(panel, DesignerV3Tab.Recovery, "field-legs", "field-recoveryMode");
                Expect(panel, DesignerV3Tab.Payload, "field-payload", "field-orbit", "field-payloadMass");
                Expect(panel, DesignerV3Tab.Look, "field-name");
                panel.SetEngineer(false);
                Assert.AreEqual(5, panel.Root.Query<Button>().ToList().FindAll(b => b.name != null && b.name.StartsWith("tab-", StringComparison.Ordinal)).Count);
            }
            finally { panel.Detach(); }
        }

        [Test]
        public void EveryGuidedStep_ShowsItsOptions()
        {
            var session = new HangarSession(catalog, data, Template("workhorse"));
            session.Apply(d =>
            {
                HangarEditor.SetFins(d, true);
                var mission = d.Obj("mission");
                if (mission == null)
                {
                    mission = new JsonObject();
                    JsonMut.Set(d, "mission", mission);
                }
                JsonMut.Set(mission, "requiresLanding", true);
                HangarEditor.SetLegs(d, true);
                return DesignerV3Edits.SetRecoveryMode(d, "rtls");
            });
            var panel = new DesignerV3Panel(session, new VisualElement(), 1f);
            try
            {
                panel.SetEngineer(true);
                panel.Stepper.EnterGuided();
                var seen = new HashSet<DesignerStep>();
                for (int i = 0; i < 16 && seen.Count < StepperCatalog.Count; i++)
                {
                    seen.Add(panel.Stepper.Step);
                    AssertStep(panel, panel.Stepper.Step);
                    // An amber step arms confirm on the first press and moves on the second.
                    if (panel.Stepper.PressForward() || panel.Stepper.PressForward()) continue;
                    var next = panel.Stepper.Root.Q<Button>("stepper-next");
                    if (next != null && next.style.display != DisplayStyle.None)
                        Click(next);
                    else
                        panel.Stepper.PressFix();
                }
                seen.Add(panel.Stepper.Step);
                Assert.AreEqual(StepperCatalog.Count, seen.Count, string.Join(",", seen));
            }
            finally { panel.Detach(); }
        }

        [Test]
        public void Edit_KeepsScroll_TabChangeResetsIt()
        {
            var session = new HangarSession(catalog, data, Template("workhorse"));
            var panel = new DesignerV3Panel(session, new VisualElement(), 1f);
            try
            {
                panel.SetEngineer(true);
                panel.Show(DesignerV3Tab.Engines);
                var scroll = panel.Root.Q<ScrollView>("designer-scroll");
                Assert.IsNotNull(scroll);
                scroll.verticalScroller.highValue = 500f;
                scroll.scrollOffset = new Vector2(0f, 180f);
                session.Apply(d => DesignerKnobs.NudgeMixture(d, 0, 1));
                Assert.AreEqual(180f, scroll.scrollOffset.y, 1f);
                panel.Show(DesignerV3Tab.Tanks);
                Assert.AreEqual(0f, scroll.scrollOffset.y, 1f, "a tab that has not been opened starts at the top");
                panel.Show(DesignerV3Tab.Engines);
                Assert.AreEqual(180f, scroll.scrollOffset.y, 1f, "returning to a tab restores its scroll");
                Assert.IsNotNull(panel.Root.Q("field-engine"));
            }
            finally { panel.Detach(); }
        }

        [Test]
        public void LaunchBackAndHome_StayOnTheDesigner()
        {
            var session = new HangarSession(catalog, data, Template("hopper"));
            var host = new VisualElement { name = "sheet-host" };
            var sheet = new VisualElement { name = "design" };
            var body = new VisualElement { name = "design-body" };
            var launchRow = new VisualElement { name = "launch-row" };
            sheet.Add(body);
            sheet.Add(launchRow);
            host.Add(sheet);
            using (var surface = DeviceSurface.Open(375f, 667f))
            {
                surface.Stage.Add(host);
                surface.Pump(3);
                var panel = DesignerV3Panel.Attach(session, host, 1f);
                Assert.IsNotNull(panel);
                try
                {
                    var toggle = host.Q<Button>(DesignerV3Panel.ToggleName);
                    Assert.IsNotNull(toggle);
                    Click(toggle);
                    Assert.IsTrue(panel.Open, "toggle click opens the designer");
                    Assert.AreEqual("Hangar", toggle.text);
                    Assert.AreEqual(DisplayStyle.None, body.style.display.value);

                    bool launched = false;
                    panel.LaunchRequested += () => launched = true;
                    var launch = panel.Root.Q<Button>("launch-btn");
                    Assert.IsNotNull(launch);
                    Click(launch);
                    if (!launched) Click(launch);
                    Assert.IsTrue(launched);

                    panel.Stepper.EnterGuided();
                    Assert.AreEqual(DesignerStep.Mission, panel.Stepper.Step);
                    Assert.IsFalse(panel.Stepper.PressBack());
                    Assert.IsFalse(panel.Stepper.Root.Q<Button>("stepper-back").enabledSelf);
                    Assert.IsTrue(panel.Stepper.PressForward() || panel.Stepper.PressJump(DesignerStep.Engines));
                    Assert.AreEqual(DesignerStep.Engines, panel.Stepper.Step);
                    Assert.IsTrue(panel.Stepper.PressBack());
                    Assert.AreEqual(DesignerStep.Mission, panel.Stepper.Step);

                    Click(toggle);
                    Assert.IsFalse(panel.Open);
                    Assert.AreEqual("Designer", toggle.text);
                }
                finally { panel.Detach(); }
            }
        }

        [Test]
        public void Devices_KeepTargetsInsideAndUseResourceFonts()
        {
            var fraunces = Resources.Load<Font>("KHFonts/Fraunces-Bold");
            var grotesk = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            var groteskBold = Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold");
            Assert.That(fraunces, Is.Not.Null, "Fraunces-Bold from Resources");
            Assert.That(grotesk, Is.Not.Null, "SpaceGrotesk-Medium from Resources");
            Assert.That(groteskBold, Is.Not.Null, "SpaceGrotesk-Bold from Resources");

            foreach (var device in Devices)
            {
                using (var surface = DeviceSurface.Open(device.W, device.H))
                {
                    var session = new HangarSession(catalog, data, Template("workhorse"));
                    session.Apply(d => HangarEditor.SetFins(d, true));
                    var panel = new DesignerV3Panel(session, new VisualElement(), 1f);
                    try
                    {
                        panel.SetEngineer(true);
                        panel.Show(DesignerV3Tab.Engines);
                        var root = panel.Root;
                        root.style.width = device.W;
                        root.style.height = device.H;
                        surface.Stage.Add(root);
                        panel.SetContentMax(-1f);
                        surface.Pump(8);

                        var goal = root.Q<Label>("step-goal");
                        var launch = root.Q<Button>("launch-btn");
                        Assert.That(goal, Is.Not.Null, device.Name);
                        Assert.That(launch, Is.Not.Null, device.Name);
                        Assert.AreEqual(fraunces, goal.style.unityFontDefinition.value.font, device.Name + " goal font");
                        Assert.AreEqual(groteskBold, launch.style.unityFontDefinition.value.font, device.Name + " launch font");
                        var bodyLabel = root.Q<Label>("field-engineCount");
                        if (bodyLabel == null)
                        {
                            var row = root.Q("field-engineCount");
                            bodyLabel = row != null ? row.Q<Label>() : null;
                        }
                        if (bodyLabel != null)
                            Assert.AreEqual(grotesk, bodyLabel.style.unityFontDefinition.value.font, device.Name + " body font");

                        Inside(surface.Stage, launch, device.Name + " launch");
                        AssertTarget(launch, device.Name + " launch");
                        var tabs = root.Q("tab-bar");
                        Assert.That(tabs, Is.Not.Null, device.Name);
                        Inside(surface.Stage, tabs, device.Name + " tabs");
                        foreach (var button in tabs.Query<Button>().ToList())
                        {
                            Inside(surface.Stage, button, device.Name + " " + button.name);
                            AssertTarget(button, device.Name + " " + button.name);
                        }

                        Walk(root, surface.Stage, device.Name);
                    }
                    finally { panel.Detach(); }
                }
            }
        }

        [Test]
        public void NarrowPortraitAndLandscape_KeepWordsGlyphsAndAContentArea()
        {
            var fonts = new[]
            {
                Resources.Load<Font>("KHFonts/Fraunces-Bold"),
                Resources.Load<Font>("KHFonts/Fraunces-Black"),
                Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium"),
                Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold"),
            };
            foreach (var font in fonts) Assert.That(font, Is.Not.Null);
            foreach (var sample in ShownText.Samples())
                AssertDrawn(fonts, sample, "sample");

            float[] widths = { 335f, 307f, 375f, 1280f };
            float[] heights = { 700f, 800f, 667f, 800f };
            for (int i = 0; i < widths.Length; i++)
            {
                using (var surface = DeviceSurface.Open(widths[i], heights[i]))
                {
                    var design = Template("workhorse");
                    JsonMut.Set(design, "missionId", "m1");
                    var session = new HangarSession(catalog, data, design);
                    var panel = new DesignerV3Panel(session, new VisualElement(), 1f);
                    try
                    {
                        panel.SetEngineer(true);
                        panel.SetTutorial(true);
                        panel.Show(DesignerV3Tab.Engines);
                        var root = panel.Root;
                        root.style.width = widths[i];
                        root.style.height = heights[i];
                        surface.Stage.Add(root);
                        panel.SetContentMax(40f);
                        surface.Pump(6);

                        var scroll = root.Q<ScrollView>("designer-scroll");
                        Assert.That(scroll, Is.Not.Null, widths[i].ToString());
                        Assert.That(scroll.style.minHeight.value.value, Is.GreaterThanOrEqualTo(100f), widths[i] + " content");
                        Assert.IsNull(root.Query<ScrollView>().ToList().Find(s => s.mode == ScrollViewMode.Horizontal), widths[i] + " horizontal scroller");

                        var maxQ = root.Q<Label>("strip-label-q");
                        Assert.That(maxQ, Is.Not.Null);
                        Assert.AreEqual("Max-Q", maxQ.text);
                        var cost = root.Q<Label>("strip-value-cost");
                        Assert.That(cost, Is.Not.Null);
                        Assert.That(cost.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap));
                        StringAssert.DoesNotContain(".00", cost.text);

                        var dec = root.Q<Label>("stepper-dec");
                        Assert.That(dec, Is.Not.Null, widths[i] + " minus");
                        Assert.AreEqual("-", dec.text);
                        Assert.IsNotNull(root.Q("stepper-inc"));

                        panel.Stepper.EnterGuided();
                        if (!panel.Stepper.PressJump(DesignerStep.Engines))
                            panel.Stepper.PressForward();
                        surface.Pump(4);
                        Assert.IsNull(panel.Stepper.Root.Query<ScrollView>().ToList().Find(s => s.mode == ScrollViewMode.Horizontal), widths[i] + " stepper scroller");
                        var guidedDec = panel.Stepper.Root.Q<Label>("stepper-dec");
                        Assert.That(guidedDec, Is.Not.Null, widths[i] + " guided minus");
                        Assert.AreEqual("-", guidedDec.text);

                        var banner = root.Q("tutorial-banner");
                        if (banner != null)
                        {
                            string tour = "";
                            banner.Query<Label>().ForEach(label => tour += " " + label.text);
                            StringAssert.DoesNotContain("Low Earth Orbit", tour);
                        }

                        root.Query<Label>().ForEach(label => { if (Shown(label)) AssertDrawn(fonts, label.text, widths[i] + " " + label.name); });
                        root.Query<Button>().ForEach(button => { if (Shown(button)) AssertDrawn(fonts, button.text, widths[i] + " " + button.name); });
                        panel.Stepper.Root.Query<Label>().ForEach(label => { if (Shown(label)) AssertDrawn(fonts, label.text, widths[i] + " stepper " + label.name); });
                        panel.Stepper.Root.Query<Button>().ForEach(button => { if (Shown(button)) AssertDrawn(fonts, button.text, widths[i] + " stepper " + button.name); });
                    }
                    finally { panel.Detach(); }
                }
            }
        }

        static void AssertDrawn(Font[] fonts, string text, string where)
        {
            if (string.IsNullOrEmpty(text)) return;
            Assert.IsTrue(ShownText.Covered(text), where + " " + text);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r' || c == '\t') continue;
                bool drawn = false;
                for (int f = 0; f < fonts.Length; f++)
                    if (fonts[f] != null && fonts[f].HasCharacter(c)) { drawn = true; break; }
                Assert.IsTrue(drawn, where + " missing '" + c + "' in " + text);
            }
        }

        static void Expect(DesignerV3Panel panel, DesignerV3Tab tab, params string[] names)
        {
            Assert.DoesNotThrow(() => panel.Show(tab), tab.ToString());
            foreach (var name in names)
                Assert.IsNotNull(panel.Root.Q(name), tab + " " + name);
        }

        static void AssertStep(DesignerV3Panel panel, DesignerStep step)
        {
            var root = panel.Stepper.Root;
            switch (step)
            {
                case DesignerStep.Mission:
                    Assert.IsNotNull(root.Q("field-mission"), "mission");
                    Assert.IsNotNull(root.Q("field-payload"));
                    Assert.IsNotNull(root.Q("field-orbit"));
                    break;
                case DesignerStep.Engines:
                    Assert.IsNotNull(root.Q("field-propellant"));
                    Assert.IsNotNull(root.Q("field-engineCount"));
                    Assert.IsNotNull(root.Q("field-cluster"));
                    Assert.IsNotNull(root.Q("field-mixtureOffsetPct"));
                    Assert.IsNotNull(root.Q("field-pc"));
                    Assert.IsNotNull(root.Q("field-expansionRatio"));
                    Assert.IsNotNull(root.Q("field-gimbal"));
                    Assert.IsNotNull(root.Q("field-throttle"));
                    break;
                case DesignerStep.Tanks:
                    Assert.IsNotNull(root.Q("field-propMass"));
                    Assert.IsNotNull(root.Q("field-stages"));
                    Assert.IsNotNull(root.Q("field-safetyFactor"));
                    Assert.IsNotNull(root.Q("field-pressure"));
                    Assert.IsNotNull(root.Q("field-insulation"));
                    Assert.IsNotNull(root.Q("field-material"));
                    Assert.IsNotNull(root.Q("field-avionics"));
                    break;
                case DesignerStep.Aero:
                    Assert.IsNotNull(root.Q("field-nose"));
                    Assert.IsNotNull(root.Q("field-fins"));
                    Assert.IsNotNull(root.Q("field-finCount"));
                    Assert.IsNotNull(root.Q("field-finSpan"));
                    Assert.IsNotNull(root.Q("field-fairing"));
                    Assert.IsNotNull(root.Q("field-fairingDiameter"));
                    Assert.IsNotNull(root.Q("field-maxQThrottle"));
                    Assert.IsNotNull(root.Q("field-gLimit"));
                    break;
                case DesignerStep.Recovery:
                    Assert.IsNotNull(root.Q("field-recoveryMode"));
                    Assert.IsNotNull(root.Q("field-legs"));
                    Assert.IsNotNull(root.Q("field-landingReserve"));
                    break;
                case DesignerStep.Look:
                    Assert.IsNotNull(root.Q("field-paint"));
                    Assert.IsNotNull(root.Q("field-accent"));
                    Assert.IsNotNull(root.Q("field-name"));
                    break;
                default:
                    Assert.IsNotNull(root.Q("stepper-forward"));
                    break;
            }
        }

        static void Walk(VisualElement root, VisualElement stage, string device)
        {
            var scroll = root.Q<ScrollView>("designer-scroll");
            root.Query<VisualElement>().ForEach(el =>
            {
                if (!Shown(el)) return;
                bool hold = el.name == "stepper-dec" || el.name == "stepper-inc";
                bool button = el is Button;
                bool field = el is TextField;
                if (!hold && !button && !field) return;
                float w = el.resolvedStyle.width;
                float h = el.resolvedStyle.height;
                if (w > 1f) Assert.That(w, Is.GreaterThanOrEqualTo(43f), device + " " + NameOf(el) + " width " + w);
                if (h > 1f) Assert.That(h, Is.GreaterThanOrEqualTo(43f), device + " " + NameOf(el) + " height " + h);
                if (InsideHorizontal(el))
                {
                    var scroller = HorizontalScroll(el);
                    float view = scroller != null ? scroller.worldBound.width : stage.worldBound.width;
                    if (el.worldBound.width > 1f && view > 8f)
                        Assert.That(el.worldBound.width, Is.LessThanOrEqualTo(view + 1f), device + " " + NameOf(el) + " wider than its scroller");
                    return;
                }
                if (scroll != null && IsInside(scroll, el)) return;
                if (el.worldBound.width > 1f && el.worldBound.height > 1f)
                    Inside(stage, el, device + " " + NameOf(el));
            });
        }

        static bool IsInside(VisualElement ancestor, VisualElement el)
        {
            for (var p = el; p != null; p = p.parent)
                if (p == ancestor) return true;
            return false;
        }

        static bool InsideHorizontal(VisualElement el)
        {
            for (var p = el.parent; p != null; p = p.parent)
                if (p is ScrollView scroll && scroll.mode == ScrollViewMode.Horizontal) return true;
            return false;
        }

        static ScrollView HorizontalScroll(VisualElement el)
        {
            for (var p = el.parent; p != null; p = p.parent)
                if (p is ScrollView scroll && scroll.mode == ScrollViewMode.Horizontal) return scroll;
            return null;
        }

        static bool Shown(VisualElement el)
        {
            for (var p = el; p != null; p = p.parent)
                if (p.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }

        static void AssertTarget(VisualElement el, string what)
        {
            Assert.That(el.worldBound.width, Is.GreaterThanOrEqualTo(43f), what);
            Assert.That(el.worldBound.height, Is.GreaterThanOrEqualTo(43f), what);
        }

        static void Inside(VisualElement viewport, VisualElement el, string what)
        {
            var view = viewport.worldBound;
            var bound = el.worldBound;
            Assert.That(bound.width, Is.GreaterThan(1f), what);
            Assert.That(bound.height, Is.GreaterThan(1f), what);
            Assert.That(bound.xMin, Is.GreaterThanOrEqualTo(view.xMin - 1f), what + " left");
            Assert.That(bound.yMin, Is.GreaterThanOrEqualTo(view.yMin - 1f), what + " top");
            Assert.That(bound.xMax, Is.LessThanOrEqualTo(view.xMax + 1f), what + " right");
            Assert.That(bound.yMax, Is.LessThanOrEqualTo(view.yMax + 1f), what + " bottom");
        }

        static string NameOf(VisualElement el) => string.IsNullOrEmpty(el.name) ? el.GetType().Name : el.name;

        static void Click(Button button)
        {
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }

        static JsonObject Template(string id)
        {
            var design = DesignMigration.Migrate(data.Template(id).design);
            HangarEditor.Normalize(design);
            return design;
        }

        sealed class DeviceSurface : IDisposable
        {
            EditorWindow window;
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }
            public string How = "none";

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
                    Assert.Fail("panel did not take the device size " + w + "x" + h + " (" + surface.How + ")");
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
                    ApplySettings();
                    UpdatePanels();
                    if (window != null) window.Repaint();
                    Validate(Stage != null ? Stage.panel : null);
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
                    window = ScriptableObject.CreateInstance<DesignerDeviceWindow>();
                    window.position = new Rect(0f, 0f, w, h);
                    window.minSize = new Vector2(w, h);
                    window.maxSize = new Vector2(w, h);
                    window.ShowUtility();
                    var root = window.rootVisualElement;
                    if (root == null) { How = "window-no-root"; return; }
                    Stage = MakeStage(root, w, h);
                    How = "editor-window";
                }
                catch (Exception e) { How = "window-failed: " + e.GetType().Name; }
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
                    go = new GameObject("kh-designer-device");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = settings;
                    ApplySettings();
                    var root = doc.rootVisualElement;
                    if (root == null) { How += "+doc-no-root"; return; }
                    Stage = MakeStage(root, w, h);
                    How += "+uidocument";
                }
                catch (Exception e) { How += "+doc-failed: " + e.GetType().Name; }
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
                if (settings == null) return;
                var method = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) method.Invoke(settings, null);
            }

            static void UpdatePanels()
            {
                var type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                var method = type != null ? type.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                if (method != null && method.GetParameters().Length == 0) method.Invoke(null, null);
            }

            static void Validate(IPanel panel)
            {
                if (panel == null) return;
                var method = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null && method.GetParameters().Length == 0) method.Invoke(panel, null);
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

        sealed class DesignerDeviceWindow : EditorWindow { }
    }
}
