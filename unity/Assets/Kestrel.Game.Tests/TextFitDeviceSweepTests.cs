// Every menu, designer tab, pause sheet and result card at the widths Trevor checks.
// 335, 390 and 430 portrait, and the same phones on their side. KESTREL_REPO_ROOT is an
// empty directory. Fonts and campaign data come from Resources only.
using Kestrel.Core.Ui;
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
    public partial class TextFitDeviceSweepTests
    {
        const string ExperienceKey = Kestrel.Core.Features.ExperienceSettings.CornerCharacterPref;
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
                    Settle(surface, home);
                    AssertFits(home, frame.Name + " home");
                    var design = home.Q<Button>("home-design");
                    var designLabel = design != null ? design.Q<Label>(className: "kh-mm-design-label") : null;
                    Assert.That(designLabel, Is.Not.Null, frame.Name);
                    Assert.That(designLabel.text, Is.EqualTo(MainMenuCopy.Design.Label));
                    AssertStringInside(designLabel, frame.Name + " DESIGN FIRST");

                    Click(home.Q<Button>("home-settings"));
                    surface.Pump(4);
                    Settle(surface, home);
                    var sheet = home.Q("screen-settings");
                    Assert.That(sheet, Is.Not.Null, frame.Name);
                    Assert.That(sheet.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), frame.Name + " settings");
                    AssertFits(sheet, frame.Name + " settings");

                    surface.Stage.Clear();
                    var board = MountBoard(surface.Stage, frame);
                    surface.Pump(6);
                    Settle(surface, board);
                    AssertFits(board, frame.Name + " missions");

                    surface.Stage.Clear();
                    var patches = MountInColumn(surface.Stage, PatchCollectionScreen.Build(null, null, null, () => { }));
                    surface.Pump(4);
                    Settle(surface, patches);
                    AssertFits(patches, frame.Name + " patches");

                    surface.Stage.Clear();
                    var codex = MountInColumn(surface.Stage, CodexScreen.Build(() => { }));
                    surface.Pump(4);
                    Settle(surface, codex);
                    AssertFits(codex, frame.Name + " codex");

                    surface.Stage.Clear();
                    var briefing = MountInColumn(surface.Stage, BriefingScreen.Build(SampleMission(), () => { }, () => { }));
                    surface.Pump(4);
                    Settle(surface, briefing);
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
                    Settle(surface, card);
                    AssertFits(card, frame.Name + " result");

                    surface.Stage.Clear();
                    var pause = MountPause(surface.Stage);
                    try
                    {
                        surface.Pump(4);
                        Settle(surface, pause.Hud);
                        AssertFits(pause.Hud.Q(UiIds.ScreenPause), frame.Name + " pause");
                    }
                    finally { pause.Dispose(); }
                }
            }
        }

        /// <summary>#416 item 3: the pass card (MISSION COMPLETE, stars, "+2.0M Kc", green Next mission) fits at 335 and at
        /// the landscape web panel (WebPanelScale: 1280x720 points).</summary>
        [Test]
        public void PassCardFitsAt335AndWebLandscape()
        {
            foreach (var frame in new[] { new Frame("335 portrait", 335f, 720f), new Frame("web landscape", 1280f, 720f) })
            {
                using (var surface = DeviceSurface.Open(frame.W, frame.H))
                {
                    var card = ReportView.Build(null, SampleLog(), ReportLayout.Solve(frame.W, frame.H, 0, 0, 0, 0, true), true, null,
                        ReportTests.PassDebrief(), new ReportActions());
                    surface.Stage.Add(card);
                    ReportCardChrome.Present(surface.Stage, card, 1f, 0f, 0f);
                    surface.Pump(6);
                    Settle(surface, card);
                    AssertFits(card, frame.Name + " pass card");
                    foreach (var id in new[] { Kestrel.Core.Ui.UiIds.ReportHero, Kestrel.Core.Ui.UiIds.ReportKcHeadline })
                        AssertStringInside(card.Q<Label>(id), frame.Name + " " + id);
                    var next = card.Q<Button>(Kestrel.Core.Ui.UiIds.ReportNext);
                    Assert.That(next.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), frame.Name);
                    Assert.That(next.worldBound.yMax, Is.LessThanOrEqualTo(surface.Stage.worldBound.yMax + 0.5f), frame.Name + " Next mission on screen");
                    Assert.That(card.Q<Button>(Kestrel.Core.Ui.UiIds.ReportFix), Is.Null, frame.Name);
                }
            }
        }

        /// <summary>Playthrough 2026-11-11 strings at 335 and the web landscape panel: the failed card with the close
        /// break-up numbers ("25.1 kPa" against "25.0 kPa") and Next relabelled "Missions" (KH-20 has no next mission, so
        /// Next lands on the list whatever the save holds), and the KH-3 / KH-14 briefings with "Don't come up short on speed".</summary>
        [Test]
        public void PlaythroughStringsFitAt335AndWebLandscape()
        {
            var campaign = ScreenStub.LoadCampaign();
            Assert.That(campaign, Is.Not.Null, "Resources/KestrelData/campaign is synced");
            var raw = MiniJson.ParseObject(Resources.Load<TextAsset>("KestrelData/campaign").text);
            var frames = new List<Frame>(Frames) { new Frame("web landscape", 1280f, 720f) };
            var saved = GameSession.Current;
            try
            {
                foreach (var frame in frames)
                {
                    MainMenuOverlay.ForceWide = frame.Landscape;
                    using (var surface = DeviceSurface.Open(frame.W, frame.H))
                    {
                        foreach (var id in new[] { "m3", "m14" })
                        {
                            var ctx = MissionContext.FromCampaign(campaign, id);
                            Assert.That(ctx, Is.Not.Null, id);
                            ctx.BonusGoals.Clear();
                            foreach (var o in raw.Arr("missions"))
                                if (o is JsonObject m && m.Str("id") == id)
                                    foreach (var b in m.Obj("briefing").Arr("bonuses"))
                                        ctx.BonusGoals.Add(((JsonObject)b).Str("label"));
                            Assert.That(ctx.BonusGoals, Does.Contain("Don't come up short on speed"), id);
                            surface.Stage.Clear();
                            var briefing = MountInColumn(surface.Stage, BriefingScreen.Build(ctx, () => { }, () => { }));
                            surface.Pump(4);
                            Settle(surface, briefing);
                            AssertFits(briefing, frame.Name + " " + id + " briefing");
                        }

                        GameSession.Carry(MissionContext.FromCampaign(campaign, "m20"));
                        var hit = new FailureHit { on = true, kind = FailureKind.StructuralMaxQ, cause = 0, value = 25056.6, limit = 25000, engine = -1, stage = 1 };
                        var ev = FailureModel.Explain(hit, 4.84, 25056.6);
                        var facts = new FlightFacts { Failure = ev, Passed = false, Seed = 1 };
                        var debrief = FlightDebrief.Compose(null, GameSession.Current, facts, NarrationPool.Empty, 100000, null);
                        Assert.That(debrief.Numbers, Is.EqualTo("Air pushed 25.1 kPa. The walls held 25.0 kPa."));
                        surface.Stage.Clear();
                        var card = ReportView.Build(ev, SampleLog(), ReportLayout.Solve(frame.W, frame.H, 0, 0, 0, 0, true), true, null, debrief, new ReportActions());
                        surface.Stage.Add(card);
                        ReportCardChrome.Present(surface.Stage, card, 1f, 0f, 0f);
                        surface.Pump(6);
                        Settle(surface, card);
                        AssertFits(card, frame.Name + " failed card, Next to the list");
                        var next = card.Q<Button>(UiIds.ReportNext);
                        Assert.That(next.text, Is.EqualTo(ReportRoutes.NextLockedLabel), frame.Name);
                        AssertStringInside(next, frame.Name + " Next relabelled");
                        AssertStringInside(card.Q<Label>("failure-numbers"), frame.Name + " break-up numbers");
                    }
                }
            }
            finally
            {
                GameSession.Carry(saved);
                MainMenuOverlay.ForceWide = null;
            }
        }

        /// <summary>Playthrough 2026-11-11, the new card strings at 335 and the web landscape panel: the departure pass
        /// card ("Parked in a 197 × 203 km orbit with 4176 m/s left…", KH-18's real winner numbers, the widest) with its
        /// narration, the engine-out engineer line, and the Starter-screen lost-booster tip.</summary>
        [Test]
        public void PlaythroughCardLinesFitAt335AndWebLandscape()
        {
            var campaign = ScreenStub.LoadCampaign();
            Assert.That(campaign, Is.Not.Null, "Resources/KestrelData/campaign is synced");
            var row = campaign.ById("m18");
            var facts = new FlightFacts { Passed = true, ApoKm = 203, Seed = 1 };
            // Parked: the sim is still in LEO. Passing the target C3 here would mark ReachedDirectly and drop Parked
            // (DepartureCardTests uses a far-short C3 for the same reason).
            facts.FillDeparture(row, 203000, 197000, new GoalCheck { DepartureDv = 4176, DepartureTW = 0.9, C3Km2PerS2 = -60 });
            Assert.IsTrue(facts.Parked, "KH-18 departs from parking");
            string numbers = FlightDebrief.NumberLine(null, facts, true);
            StringAssert.StartsWith("Parked in a 197 × 203 km orbit with 4176 m/s left.", numbers);
            var pass = ReportTests.PassDebrief();
            pass.Numbers = numbers;
            pass.NarratorKid = FlightDebrief.Fill("She parked {park} with {dvleft} m/s still in the tanks. {stars} on the board.", null, facts, null);
            pass.NarratorEng = FlightDebrief.Fill("Pass. Parked {park}. Δv left {dvleft} m/s against {dvneed} m/s for the {burn} burn, which is not flown.", null, facts, null);

            var hit = new FailureHit { on = true, kind = FailureKind.EngineOut, cause = 0, value = 1, limit = 27, engine = 2, stage = 0 };
            var engineOut = FailureModel.Explain(hit, 63.4, 0);
            StringAssert.StartsWith("1 of 27 engines quit at T+63.4 s.", engineOut.engineer);

            BoosterLanding.Why(new RecoveryCraft { DryKg = 20000, ReserveKg = 0, Legs = false, Mode = "none" }, true, out var tip, out _);
            var lost = ReportTests.PassDebrief();
            lost.Passed = false;
            lost.KcHeadline = "";
            lost.Kid = tip;

            foreach (var frame in new[] { new Frame("335 portrait", 335f, 720f), new Frame("web landscape", 1280f, 720f) })
            {
                foreach (var (name, ev, debrief, label) in new[]
                {
                    ("departure pass card", (FailureEvent)null, pass, "failure-numbers"),
                    ("engine-out card", engineOut, lost, "failure-engineer"),
                    ("lost-booster tip", (FailureEvent)null, lost, "failure-kid"),
                })
                {
                    using (var surface = DeviceSurface.Open(frame.W, frame.H))
                    {
                        var card = ReportView.Build(ev, SampleLog(), ReportLayout.Solve(frame.W, frame.H, 0, 0, 0, 0, true), true, null, debrief, new ReportActions());
                        surface.Stage.Add(card);
                        ReportCardChrome.Present(surface.Stage, card, 1f, 0f, 0f);
                        surface.Pump(6);
                        Settle(surface, card);
                        AssertFits(card, frame.Name + " " + name);
                        AssertStringInside(card.Q<Label>(label), frame.Name + " " + name + " " + label);
                    }
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
                            Settle(surface, panel.Root);
                            AssertFits(panel.Root, frame.Name + " " + tab);
                        }
                        panel.SetEngineer(false);
                        foreach (var tab in DesignerV3Tabs.Simple)
                        {
                            panel.Show(tab);
                            surface.Pump(2);
                            Settle(surface, panel.Root);
                            AssertFits(panel.Root, frame.Name + " simple " + tab);
                        }
                    }
                    finally { panel.Detach(); }
                }
            }

            ArmRecovery(session);
            using (var surface = DeviceSurface.Open(335f, 720f))
                WalkGuided(surface, session, "335 portrait");
            using (var surface = DeviceSurface.Open(844f, 390f))
                WalkGuided(surface, session, "390 landscape");
        }

        static void ArmRecovery(HangarSession session)
        {
            // Recovery is off the guided path until the design has to land. Same setup as the device stepper test.
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
        }

        static void WalkGuided(DeviceSurface surface, HangarSession session, string where)
        {
            session.Apply(d =>
            {
                StepperProgress.FreshGuided().Write(d);
                return new Edit("guided");
            });
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
                    Settle(surface, panel.Root);
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

        /// <summary>Settings > Show corner character: the label and its note fit, on and off, with a 44 pt row, at every frame.</summary>
        [Test]
        public void SettingsCornerCharacterRowAndNoteFitInPortraitAndLandscape()
        {
            const string key = ExperienceKey;
            bool had = PlayerPrefs.HasKey(key);
            int prev = PlayerPrefs.GetInt(key, 0);
            try
            {
                foreach (bool on in new[] { false, true })
                    foreach (var frame in Frames)
                    {
                        PlayerPrefs.SetInt(key, on ? 1 : 0);
                        MainMenuOverlay.ForceWide = frame.Landscape;
                        string where = frame.Name + (on ? " on" : " off");
                        using (var surface = DeviceSurface.Open(frame.W, frame.H))
                        {
                            var home = MountHome(surface.Stage);
                            surface.Pump(8);
                            Click(home.Q<Button>("home-settings"));
                            surface.Pump(4);
                            Settle(surface, home);
                            var row = home.Q<Button>(UiIds.SettingsCornerCharacter);
                            var note = home.Q<Label>("settings-corner-character-note");
                            Assert.That(row, Is.Not.Null, where);
                            Assert.That(note, Is.Not.Null, where);
                            Assert.That(row.text, Is.EqualTo("Show corner character"), where);
                            Assert.That(note.text, Is.EqualTo("A little helper in the corner. Off by default."), where);
                            Assert.That(row.Q<Label>(row.name + "-state").text, Is.EqualTo(on ? "On" : "Off"), where);
                            Assert.That(row.layout.height, Is.GreaterThanOrEqualTo(43.5f), where + " touch target");
                            var sheet = home.Q("screen-settings");
                            AssertFits(sheet, where + " settings");
                            // Short landscape sheets scroll: both must be reachable inside the list's viewport.
                            var list = home.Q<ScrollView>("settings-list");
                            Assert.That(list, Is.Not.Null, where);
                            foreach (var el in new VisualElement[] { row, note })
                            {
                                list.ScrollTo(el);
                                surface.Pump(2);
                                AssertStringInside((TextElement)el, where + " " + el.name);
                                var view = list.worldBound;
                                Assert.That(el.worldBound.yMin, Is.GreaterThanOrEqualTo(view.yMin - TextFit.Slack - 0.5f), where + " " + el.name + " above the list");
                                Assert.That(el.worldBound.yMax, Is.LessThanOrEqualTo(view.yMax + TextFit.Slack + 0.5f), where + " " + el.name + " below the list");
                                Assert.That(el.worldBound.xMax, Is.LessThanOrEqualTo(sheet.worldBound.xMax + TextFit.Slack), where + " " + el.name + " past the sheet");
                            }
                        }
                    }
            }
            finally
            {
                MainMenuOverlay.ForceWide = null;
                if (had) PlayerPrefs.SetInt(key, prev); else PlayerPrefs.DeleteKey(key);
            }
        }

        /// <summary>#416 item 8: with the web scroll chrome on, every list loses its grey scroller, the mission card's
        /// text takes the lane back, and the board still fits at 335 portrait and the 1280x720 web landscape panel.</summary>
        [Test]
        public void WebScrollChromeBoardFitsAt335AndWebLandscape()
        {
            var frames = new[] { new Frame("335 portrait", 335f, 720f), new Frame("web landscape", 1280f, 720f) };
            WebScrollChrome.Force = true;
            try
            {
                foreach (var frame in frames)
                {
                    MainMenuOverlay.ForceWide = frame.Landscape;
                    using (var surface = DeviceSurface.Open(frame.W, frame.H))
                    {
                        var shell = MountBoard(surface.Stage, frame);
                        surface.Pump(6);
                        WebScrollChrome.Sweep(shell);
                        Settle(surface, shell);
                        AssertFits(shell, frame.Name + " board (web scroll)");
                        int lists = 0;
                        shell.Query<ScrollView>().ForEach(sv =>
                        {
                            lists++;
                            Assert.That(sv.ClassListContains(WebScrollChrome.DressedClass), Is.True, frame.Name + " " + sv.name);
                            Assert.That(sv.verticalScroller.resolvedStyle.width, Is.LessThanOrEqualTo(0.5f), frame.Name + " " + sv.name + " scroller");
                        });
                        Assert.That(lists, Is.GreaterThan(0), frame.Name);
                        var card = shell.Q<ScrollView>("mission-card-scroll");
                        var name = shell.Q<Label>("mission-name");
                        Assert.That(card, Is.Not.Null, frame.Name);
                        Assert.That(name, Is.Not.Null, frame.Name);
                        AssertStringInside(name, frame.Name + " mission title");
                        Assert.That(name.worldBound.xMax, Is.LessThanOrEqualTo(card.worldBound.xMax + TextFit.Slack), frame.Name + " title under the lane");
                    }
                }
            }
            finally
            {
                WebScrollChrome.Force = null;
                MainMenuOverlay.ForceWide = null;
            }
        }

        /// <summary>Settings > Commentary and Time warp: label, the longest state chip ("Calls only") and the helper
        /// lines fit with a 44 pt row at 335 px, the phone frames, and the web landscape sizes.</summary>
        [Test]
        public void SettingsCommentaryAndTimeWarpRowsFitAt335AndWebLandscape()
        {
            string[] keys = { Kestrel.Core.Flight.Narration.Commentary.Pref, Kestrel.Core.Features.ExperienceSettings.LegacyNarrationMutePref, Kestrel.Core.Flight.TimeWarpSetting.Pref };
            var had = new bool[keys.Length];
            var prev = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) { had[i] = PlayerPrefs.HasKey(keys[i]); prev[i] = PlayerPrefs.GetInt(keys[i], 0); }
            var frames = new System.Collections.Generic.List<Frame>(Frames)
            {
                new Frame("web 1280x720", 1280f, 720f),
                new Frame("web 1366x768", 1366f, 768f),
                new Frame("web 1920x1080", 1920f, 1080f),
            };
            // Each pass sets both rows to their widest chip: Calls only + Auto, then Off + Off.
            var passes = new[] { (c: 1, w: 0, cs: "Calls only", ws: "Auto"), (c: 2, w: 1, cs: "Off", ws: "Off"), (c: 0, w: 0, cs: "Full", ws: "Auto") };
            try
            {
                foreach (var pass in passes)
                    foreach (var frame in frames)
                    {
                        PlayerPrefs.SetInt(keys[0], pass.c);
                        PlayerPrefs.SetInt(keys[1], pass.c == 2 ? 1 : 0);
                        PlayerPrefs.SetInt(keys[2], pass.w);
                        MainMenuOverlay.ForceWide = frame.Landscape;
                        string where = frame.Name + " " + pass.cs + "/" + pass.ws;
                        using (var surface = DeviceSurface.Open(frame.W, frame.H))
                        {
                            var home = MountHome(surface.Stage);
                            surface.Pump(8);
                            Click(home.Q<Button>("home-settings"));
                            surface.Pump(4);
                            Settle(surface, home);
                            var sheet = home.Q("screen-settings");
                            AssertFits(sheet, where + " settings");
                            var list = home.Q<ScrollView>("settings-list");
                            Assert.That(list, Is.Not.Null, where);
                            var c = home.Q<Button>(UiIds.SettingsCommentary);
                            var w = home.Q<Button>(UiIds.SettingsTimeWarp);
                            Assert.That(c, Is.Not.Null, where);
                            Assert.That(w, Is.Not.Null, where);
                            var cState = c.Q<Label>(c.name + "-state");
                            var wState = w.Q<Label>(w.name + "-state");
                            Assert.That(cState.text, Is.EqualTo(pass.cs), where);
                            Assert.That(wState.text, Is.EqualTo(pass.ws), where);
                            Assert.That(c.layout.height, Is.GreaterThanOrEqualTo(43.5f), where + " touch target");
                            Assert.That(w.layout.height, Is.GreaterThanOrEqualTo(43.5f), where + " touch target");
                            var els = new VisualElement[] { c, cState, home.Q<Label>("settings-commentary-note"), w, wState, home.Q<Label>("settings-time-warp-note") };
                            foreach (var el in els)
                            {
                                Assert.That(el, Is.Not.Null, where);
                                list.ScrollTo(el);
                                surface.Pump(2);
                                AssertStringInside((TextElement)el, where + " " + el.name);
                                var view = list.worldBound;
                                Assert.That(el.worldBound.yMin, Is.GreaterThanOrEqualTo(view.yMin - TextFit.Slack - 0.5f), where + " " + el.name + " above the list");
                                Assert.That(el.worldBound.yMax, Is.LessThanOrEqualTo(view.yMax + TextFit.Slack + 0.5f), where + " " + el.name + " below the list");
                                Assert.That(el.worldBound.xMax, Is.LessThanOrEqualTo(sheet.worldBound.xMax + TextFit.Slack), where + " " + el.name + " past the sheet");
                            }
                            // The row's label never runs under its state chip.
                            foreach (var row in new[] { c, w })
                            {
                                var chip = row.Q<Label>(row.name + "-state");
                                float labelRight = row.worldBound.xMin + row.resolvedStyle.paddingLeft + row.MeasureTextSize(row.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                                Assert.That(labelRight, Is.LessThanOrEqualTo(chip.worldBound.xMin + TextFit.Slack), where + " " + row.name + " label under the chip");
                            }
                        }
                    }
            }
            finally
            {
                MainMenuOverlay.ForceWide = null;
                for (int i = 0; i < keys.Length; i++)
                    if (had[i]) PlayerPrefs.SetInt(keys[i], prev[i]); else PlayerPrefs.DeleteKey(keys[i]);
            }
        }

        static void Settle(DeviceSurface surface, VisualElement root)
        {
            TextFit.Apply(root);
            surface.Pump(3);
            TextFit.Apply(root);
            surface.Pump(2);
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
            var hud = new VisualElement { name = UiIds.ScreenFlight };
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
