// Starter screen (Phase 1 PR 5): the REAL UI Toolkit tree (StarterPanel over the real catalog, the campaign and
// each mission's campaign winner) on #300's UiDeviceSurface at 335x667, 390x844 and 430x932 portrait, iPad portrait
// 820x1180, and landscape web 1280x720, 1366x768 and 1920x1080 (each the panel WebPanelScale gives). Per size and
// case, #300's UiTreeChecks (MeasureTextSize text inside its content rect and its parent, no overlapping leaves,
// nothing off-screen or outside the safe area) plus the Starter rules: 44 pt targets, exactly one primary (Launch)
// bottom-right on web / at the bottom on portrait, ◀ Back top-left. One PNG per size (DeviceCapture →
// CaptureOut/starter). The click-through goes by UiIds names only; the too-heavy case stays fixable on screen.
// Batchmode-safe: no LogError, no throw outside asserts; surfaces disposed per case, globals restored in TearDown.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class StarterDesignerDeviceTests
    {
        const string LongReadiness = "Lands on Haven on its last fuel, and both boosters fly home with little to spare.";
        /// <summary>A twr-low setting one fuel notch from flying (same as Kestrel.Core.Tests.StarterScreenTests.TooHeavyKey).</summary>
        const string TooHeavyKey = "kerosene-3-f4-b0";
        const string CaptureSub = "starter";

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
        /// <summary>Honest Ready: a setting the launch predictor breaks up at max-Q (KH-3, two kerosene stages, full fuel)
        /// and one that keeps too little landing fuel (KH-13 methane, full fuel). Same as Kestrel.Core.Tests.StarterHonestReadyTests.</summary>
        const string BreaksUpKey = "kerosene-2-f4-b0", CantLandKey = "methane-2-f4-b0";
        IReadOnlyDictionary<string, Kestrel.Core.Json.JsonObject> basesBefore;
        StarterReadinessCopy copyBefore;
        bool loadedBefore;
        readonly List<IDisposable> open = new List<IDisposable>();
        readonly List<StarterPanel> panels = new List<StarterPanel>();

        [OneTimeSetUp]
        public void LoadBases()
        {
            basesBefore = StarterBases.Active;
            loadedBefore = StarterBasesBoot.Loaded;
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            catalog = Catalog.LoadFromRepo(root);
            campaign = Campaign.Load(root);
            StarterBases.Active = StarterBases.Parse(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            StarterBasesBoot.Loaded = true;
            copyBefore = StarterReadinessCopy.Active;
            StarterReadinessCopy.Active = StarterReadinessCopy.Load(root);
        }

        [TearDown]
        public void CloseEverything()
        {
            foreach (var p in panels) { try { p.Detach(); } catch (Exception e) { Debug.LogWarning("[StarterDevice] detach: " + e.Message); } }
            panels.Clear();
            foreach (var d in open) { try { d.Dispose(); } catch (Exception e) { Debug.LogWarning("[StarterDevice] dispose: " + e.Message); } }
            open.Clear();
        }

        [OneTimeTearDown]
        public void RestoreBases()
        {
            StarterBases.Active = basesBefore;
            StarterBasesBoot.Loaded = loadedBefore;
            StarterReadinessCopy.Active = copyBefore;
        }

        UiDeviceSurface Surface(float w, float h)
        {
            var s = UiDeviceSurface.Open(w, h);
            open.Add(s);
            return s;
        }

        StarterPanel Panel(StarterRocket r, UiDeviceSurface s)
        {
            var p = new StarterPanel(r, s.Stage);
            panels.Add(p);
            return p;
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
            yield return ("kh6-too-heavy", () => Rocket("m6", TooHeavyKey), null);
            yield return ("kh19-long-readiness", () => Rocket("m19"), LongReadiness);
            yield return ("kh3-breaks-up", () => Rocket("m3", BreaksUpKey), null);
            yield return ("kh13-cant-land", () => Rocket("m13", CantLandKey), null);
            yield return ("kh5-checking", () => Rocket("m5"), null);
        }

        [Test]
        public void EveryDevice_RealTree_TextFits_NoOverlap_InsideSafeArea_Captured()
        {
            var shots = new List<string>();
            foreach (var d in Devices())
                foreach (var c in Cases())
                {
                    string path = Check(d, c.Make(), c.Label, c.Readiness, capture: c.Label == "kh5-boosters-shown" || c.Label == "kh3-breaks-up");
                    if (path != null) shots.Add(path);
                }
            Assert.AreEqual(14, shots.Count, "one PNG per size, plus the Honest Ready badge per size");
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
            using (var surface = UiDeviceSurface.Open(390, 844))
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

        /// <summary>Arcade's condition for the twr-low block: at 335x667 the plain message is on screen and fits, Launch
        /// is off, every control is still live, and one notch less fuel (tapped there) clears it.</summary>
        [Test]
        public void TooHeavy_At335_MessageFits_LaunchOff_ControlsLive_LessFuelClearsIt()
        {
            var surface = Surface(335, 667);
            var r = Rocket("m6", TooHeavyKey);
            var panel = Panel(r, surface);
            panel.Layout(335, 667, 20, 0, 0, 0, 1f, false, false);
            surface.Pump(3);
            Settle(surface, panel.Root);
            Assert.IsTrue(r.Check().Blocked, "KH-6 " + TooHeavyKey + " is twr-low");

            var line = panel.Root.Q<Label>(UiIds.StarterReadiness);
            var badge = panel.Root.Q<Label>(UiIds.StarterBadge);
            Assert.AreEqual("Too heavy to leave the pad. Try less fuel.", line.text);
            Assert.AreEqual(StarterScreen.HeavyBadge, badge.text);
            Assert.IsTrue(UiTreeChecks.Visible(line), "the message is on screen");
            Assert.IsTrue(UiTreeChecks.Visible(badge), "the badge is on screen");
            var stage = surface.Stage.worldBound;
            var safe = new Rect(stage.xMin, stage.yMin + 20, stage.width, stage.height - 20);
            var problems = UiTreeChecks.Problems(panel.Root, stage, safe, "335x667 too heavy");
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems));
            Assert.That(TextFit.Problems(panel.Root), Is.Empty, "335x667 too heavy text-fit");

            var launch = panel.Root.Q<Button>(UiIds.StarterLaunch);
            Assert.IsFalse(launch.enabledSelf, "Launch is off on the hard block");
            int launched = 0;
            panel.LaunchRequested += () => launched++;
            Click(launch);
            Assert.AreEqual(0, launched, "a blocked Launch does nothing");

            foreach (var id in UiIds.StarterControls)
            {
                if (id == UiIds.StarterLaunch) continue;
                var c = panel.Root.Q(id);
                Assert.That(c, Is.Not.Null, id);
                if (!UiTreeChecks.Visible(c)) continue;   // a row the mission hides
                Assert.IsTrue(c.enabledInHierarchy, id + " stays live while blocked");
            }

            Click(panel.Root.Q<Button>(UiIds.StarterFuelNotch(4)));
            Assert.AreEqual("kerosene-3-f3-b0", r.Choice.Key);
            Assert.IsFalse(r.Check().Blocked, "one notch less fuel lifts off");
            Assert.IsTrue(launch.enabledSelf, "Launch comes back");
            Assert.AreNotEqual(StarterScreen.HeavyBadge, badge.text);
            Click(launch);
            Assert.AreEqual(1, launched);
        }

        string Check(Device d, StarterRocket rocket, string label, string readiness, bool capture)
        {
            CloseEverything();   // one surface open at a time
            var surface = Surface(d.W, d.H);
            var panel = Panel(rocket, surface);
            if (label == "kh5-checking") panel.Refresh();   // not flown yet: the "Test-flying it" line
            else panel.FinishPrediction();
            if (readiness != null) panel.Root.Q<Label>(UiIds.StarterReadiness).text = readiness;
            panel.Layout(d.W, d.H, d.Top, d.Bottom, 0, 0, 1f, d.Wide, d.Wide);
            surface.Pump(3);
            Settle(surface, panel.Root);
            string where = d.Name + " " + label;
            var stage = surface.Stage.worldBound;
            var safe = new Rect(stage.xMin, stage.yMin + d.Top, stage.width, stage.height - d.Top - d.Bottom);

            Assert.That(StarterLayout.Problems(panel.Frame, panel.Input), Is.Empty, where + " model");
            var problems = UiTreeChecks.Problems(panel.Root, stage, safe, where);
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems));
            Assert.That(TextFit.Problems(panel.Root), Is.Empty, where);

            int primaries = 0;
            foreach (var b in panel.Root.Query<Button>().ToList())
            {
                if (!UiTreeChecks.Visible(b)) continue;
                if (b.name == UiIds.StarterLaunch) primaries++;
                Assert.GreaterOrEqual(b.worldBound.width, 44f - 0.5f, where + " " + b.name + " width");
                Assert.GreaterOrEqual(b.worldBound.height, 44f - 0.5f, where + " " + b.name + " height");
            }
            Assert.AreEqual(1, primaries, where + " exactly one primary button");

            var launch = panel.Root.Q<Button>(UiIds.StarterLaunch);
            Assert.IsTrue(UiTreeChecks.Visible(launch), where + " Launch always visible");
            bool blocked = rocket.Check().Blocked;
            Assert.AreEqual(!blocked, launch.enabledSelf, where + " Launch is off only on a hard block");
            if (label == "kh7-short") Assert.IsTrue(launch.enabledSelf, where + " a short rocket still launches");
            if (label == "kh6-too-heavy")
            {
                Assert.IsTrue(blocked, where);
                Assert.AreEqual(StarterScreen.HeavyBadge, panel.Root.Q<Label>(UiIds.StarterBadge).text, where);
                Assert.IsTrue(UiTreeChecks.Visible(panel.Root.Q<Label>(UiIds.StarterReadiness)), where + " the reason is on screen");
            }
            var badgeLabel = panel.Root.Q<Label>(UiIds.StarterBadge);
            var lineLabel = panel.Root.Q<Label>(UiIds.StarterReadiness);
            if (label == "kh3-breaks-up" || label == "kh13-cant-land")
            {
                // #369 closed: kh13-cant-land (m13 methane-2-f4-b0) reads the Landing cause again.
                string cause = label == "kh3-breaks-up" ? StarterVerdict.MaxQ : StarterVerdict.Landing;
                Assert.AreEqual(cause, rocket.Verdict().Cause, where);
                Assert.AreEqual(StarterReadinessCopy.Current.Badge(cause), badgeLabel.text, where + " the Honest Ready badge");
                Assert.AreNotEqual(StarterScreen.ReadyBadge, badgeLabel.text, where);
                Assert.AreNotEqual(StarterScreen.TightBadge, badgeLabel.text, where);
                StringAssert.StartsWith(StarterReadinessCopy.Current.Causes[cause].Kid, lineLabel.text, where);
                Assert.IsTrue(UiTreeChecks.Visible(badgeLabel), where + " the badge is on screen");
                Assert.IsTrue(UiTreeChecks.Visible(lineLabel), where + " the reason is on screen");
                Assert.IsTrue(launch.enabledSelf, where + " a bad but buildable rocket still launches");
            }
            if (label == "kh5-checking")
            {
                Assert.AreEqual(StarterReadinessCopy.Current.Checking, lineLabel.text, where);
                Assert.AreEqual("", badgeLabel.text, where + " no Ready while checking");
                Assert.IsTrue(launch.enabledSelf, where);
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
            string path = DeviceCapture.Save(CaptureSub, (label == "kh3-breaks-up" ? "starter-breaks-up-" : "starter-") + d.Name, surface, out var kind);
            TestContext.WriteLine(where + " capture (" + kind + "): " + (path ?? "none"));
            return path;
        }

        static Rect Shrink(Rect r) => new Rect(r.x + 0.5f, r.y + 0.5f, Mathf.Max(0f, r.width - 1f), Mathf.Max(0f, r.height - 1f));

        static void Settle(UiDeviceSurface surface, VisualElement root)
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
    }
}
