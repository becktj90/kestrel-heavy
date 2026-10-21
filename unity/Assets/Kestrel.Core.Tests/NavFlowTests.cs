// test(nav): the screen routes the shell maps onto scenes (GameSession.Apply, HomeRoutes, ReportRoutes, PadHandoff),
// the designer launch gate a player meets on the way, and the report a crashed rocket still owes.
// Bad rockets may fail and explode; the game must always be navigable.
// Known bugs on main run as KnownBug(...): they are reported as Ignored with the observed result while the bug
// reproduces, and fail loudly once it stops reproducing so the entry in NAV_BUGS.md gets closed. Nothing is fixed here.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class NavFlowTests
    {
        Catalog catalog;
        Campaign campaign;
        HangarData hangar;
        string tmp;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            GameSession.Clear();
            PadHandoff.Clear();
            tmp = null;
        }

        [TearDown]
        public void Clean()
        {
            GameSession.Clear();
            PadHandoff.Clear();
            if (tmp != null && Directory.Exists(tmp)) Directory.Delete(tmp, true);
        }

        /// <summary>
        /// A known bug on main: <paramref name="observed"/> returns null when the route behaves as expected,
        /// else what actually happened. While it reproduces the test is Ignored with that text; once it passes the test fails.
        /// </summary>
        static void KnownBug(string id, string expected, Func<string> observed)
        {
            string got = observed();
            if (got != null) Assert.Ignore("KNOWN BUG " + id + " (NAV_BUGS.md). Expected: " + expected + ". Observed: " + got);
            Assert.Fail("KNOWN BUG " + id + " no longer reproduces. Remove KnownBug and close it in NAV_BUGS.md.");
        }

        static string Where(RouteResult r) => r.Screen + "/" + r.Overlay + " session=" + (r.Session != null ? (r.Session.MissionId ?? "free") : "none");

        static string Src(params string[] path) => File.ReadAllText(Path.Combine(Golden.Root, Path.Combine(path)));

        // ---------------------------------------------------------------- every route lands somewhere

        static IEnumerable<RouteId> AllRoutes() => (RouteId[])Enum.GetValues(typeof(RouteId));

        static readonly string[] Screens = { SessionScreens.Home, SessionScreens.Design, SessionScreens.Flight, SessionScreens.Missions };

        [TestCase("fresh")]
        [TestCase("campaign")]
        [TestCase("free")]
        public void EveryRoute_LandsOnANamedScreen_AndNeverThrows(string start)
        {
            foreach (var id in AllRoutes())
            {
                GameSession.Clear();
                if (start == "campaign") GameSession.Carry(MissionContext.FromCampaign(campaign, "m3"));
                if (start == "free") GameSession.Carry(MissionContext.Free(7));
                RouteResult r = default;
                Assert.DoesNotThrow(() => r = GameSession.Apply(id, campaign, id == RouteId.MissionChosen ? "m1" : null), id + " from " + start);
                CollectionAssert.Contains(Screens, r.Screen, id + " from " + start + " has no screen: a dead end");
            }
        }

        [Test]
        public void EveryScreen_HasAWayOut()
        {
            // Exits per screen or overlay, as the shell wires them (SceneFlow, ScreenStub, NextMission, MenuBack).
            var exits = new Dictionary<string, RouteId[]>
            {
                ["Missions"] = new[] { RouteId.MissionChosen, RouteId.ExitHome },
                ["Missions/Briefing"] = new[] { RouteId.BriefingBuild, RouteId.BriefingBack },
                ["Design"] = new[] { RouteId.DesignerBack, RouteId.ExitHome },
                ["Missions/Report"] = new[] { RouteId.ReportNext, RouteId.ReportRetry, RouteId.ReportMissions, RouteId.ReportHome },
                ["Home"] = new[] { RouteId.HomeLaunch, RouteId.HomeHangar },
            };
            foreach (var kv in exits)
            {
                Assert.IsNotEmpty(kv.Value, kv.Key);
                foreach (var id in kv.Value)
                {
                    GameSession.Carry(MissionContext.FromCampaign(campaign, "m3"));
                    var r = GameSession.Apply(id, campaign, "m1");
                    string to = r.Screen + (r.Overlay != SessionScreens.None ? "/" + r.Overlay : "");
                    Assert.AreNotEqual(kv.Key, to, kv.Key + " " + id + " leaves the screen it is on");
                }
            }
        }

        // ---------------------------------------------------------------- Home

        [TestCase(0, "m1")]
        [TestCase(5, "m6")]
        [TestCase(20, "m20")]
        public void HomeLaunch_BriefsTheNextMission_FreshAndFromASave(int cleared, string mission)
        {
            var r = HomeRoutes.Launch(campaign, cleared, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Missions, r.Screen);
            Assert.AreEqual(SessionScreens.Briefing, r.Overlay);
            Assert.AreEqual(mission, r.Session.MissionId);
            Assert.AreSame(r.Session, GameSession.Current);
        }

        [TestCase(0, "m1")]
        [TestCase(5, "m6")]
        public void HomeDesignFirst_OpensTheDesignerWithTheMission(int cleared, string mission)
        {
            var r = HomeRoutes.DesignFirst(campaign, cleared, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Design, r.Screen);
            Assert.AreEqual(mission, r.Session.MissionId);
            Assert.IsTrue(PadHandoff.TryTake(out var design, out bool fly));
            Assert.IsFalse(fly, "Design first never flies on its own");
            Assert.AreEqual(mission, design.Str("missionId"));
        }

        [Test]
        public void HomeLaunch_NoCampaignData_StillLandsOnMissions()
        {
            var r = HomeRoutes.Launch(null, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Missions, r.Screen);
            var d = HomeRoutes.DesignFirst(null, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Design, d.Screen);
        }

        // ---------------------------------------------------------------- Missions → briefing → designer

        [Test]
        public void MissionsBriefingDesigner_AndBackOutAgain()
        {
            var chosen = GameSession.Apply(RouteId.MissionChosen, campaign, "m2");
            Assert.AreEqual("m2", chosen.Session.MissionId);
            Assert.AreEqual(SessionScreens.Design, GameSession.Apply(RouteId.BriefingBuild).Screen);
            var back = GameSession.Apply(RouteId.DesignerBack);
            Assert.AreEqual(SessionScreens.Missions, back.Screen, "designer Back");
            Assert.AreEqual(SessionScreens.Briefing, back.Overlay, "designer Back reopens the briefing");
            Assert.AreEqual("m2", back.Session.MissionId);
            var leave = GameSession.Apply(RouteId.BriefingBack);
            Assert.AreEqual(SessionScreens.Missions, leave.Screen);
            Assert.AreEqual(SessionScreens.None, leave.Overlay);
            var home = GameSession.Apply(RouteId.ExitHome);
            Assert.AreEqual(SessionScreens.Home, home.Screen);
            Assert.IsNull(GameSession.Current);
        }

        [Test]
        public void UnknownMission_StaysOnTheBoard()
        {
            var r = GameSession.Apply(RouteId.MissionChosen, campaign, "m99");
            Assert.AreEqual(SessionScreens.Missions, r.Screen);
            Assert.AreEqual(SessionScreens.None, r.Overlay);
        }

        [Test]
        public void FreeBuild_DesignerBack_GoesHome()
        {
            GameSession.Apply(RouteId.HomeHangar);
            Assert.IsTrue(GameSession.IsFreeBuild);
            var r = GameSession.Apply(RouteId.DesignerBack);
            Assert.AreEqual(SessionScreens.Home, r.Screen);
            Assert.IsNull(GameSession.Current);
        }

        // ---------------------------------------------------------------- report exits

        [Test]
        public void Report_RetryKeepsMissionAndSeed_MissionsAndHomeLeave()
        {
            var ctx = MissionContext.FromCampaign(campaign, "m3", 4242);
            GameSession.Carry(ctx);
            var retry = ReportRoutes.Again();
            Assert.AreEqual(SessionScreens.Design, retry.Screen, ReportRoutes.AgainLabel);
            Assert.AreEqual("m3", retry.Session.MissionId);
            Assert.AreEqual(4242UL, retry.Session.Seed);
            Assert.AreEqual(SessionScreens.Missions, ReportRoutes.Missions().Screen, ReportRoutes.MissionsLabel);
            var home = ReportRoutes.Home();
            Assert.AreEqual(SessionScreens.Home, home.Screen, ReportRoutes.HomeLabel);
            Assert.IsNull(GameSession.Current);
        }

        [Test]
        public void Report_RetryOnAFreeBuildOrNoSession_StillOpensTheDesigner()
        {
            Assert.AreEqual(SessionScreens.Design, ReportRoutes.Again().Screen, "no session");
            GameSession.Carry(MissionContext.Free(3));
            Assert.AreEqual(SessionScreens.Design, ReportRoutes.Again().Screen, "free build");
        }

        [Test]
        public void Report_NextOnTheLastMission_StaysOnKh20()
        {
            GameSession.Carry(MissionContext.FromCampaign(campaign, "m20"));
            var r = ReportRoutes.Next(campaign);
            Assert.AreEqual(SessionScreens.Missions, r.Screen);
            Assert.AreEqual("m20", r.Session.MissionId);
        }

        [Test]
        public void KnownBug_NAV06_ReportNext_LandsOnTheList_NotTheNextMission()
        {
            KnownBug("NAV-06", "\"" + ReportRoutes.NextLabel + "\" opens KH-4's briefing", () =>
            {
                GameSession.Carry(MissionContext.FromCampaign(campaign, "m3"));
                var r = ReportRoutes.Next(campaign);
                return r.Overlay == SessionScreens.Briefing && r.Session != null && r.Session.MissionId == "m4" ? null : Where(r) + " (the plain Missions list, same screen as the Missions button)";
            });
        }

        // ---------------------------------------------------------------- a crashing rocket still reaches the report

        [TestCase("one-engine", "m4")]
        [TestCase("no-strength", "m4")]
        [TestCase("short-fuel", "m1")]
        public void CrashingRocket_LiveSetup_ReachesTheReport_WithRetryAndDesigner(string mode, string missionId)
        {
            var mission = campaign.ById(missionId);
            var design = Stamped(missionId);
            if (mode == "one-engine") design.stages[0].engineCount = 1;
            if (mode == "no-strength") { design.safetyFactor = 1.0; design.stages[0].engineCount *= 3; }
            if (mode == "short-fuel") design.stages[0].propMass *= 0.05;
            // The live pad step (FlightDriver.PhysicsDt) through the shared setup, not FlightBatch's 0.2 s.
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt);
            var monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(design), campaign.Seeds[0]);
            monitor.Gentle = false;
            setup.Options.monitor = monitor;
            SimResult sim = null;
            Assert.DoesNotThrow(() => sim = Headless.Simulate(setup.Stages, setup.Options), mode);
            bool intact = sim.failure == null && sim.outcome != "failure" && sim.outcome != "crash";
            Assert.IsFalse(intact && mode != "short-fuel", mode + " should not fly: " + sim.outcome);

            var ctx = MissionContext.FromMission(campaign, mission, campaign.Seeds[0]);
            GameSession.Carry(ctx);
            var facts = new FlightFacts { Passed = false, Failure = sim.failure, Seed = ctx.Seed, DesignId = design.name, ApoKm = sim.orbit.apoAlt / 1000.0 };
            FlightDebrief card = null;
            Assert.DoesNotThrow(() => card = FlightDebrief.Compose(null, ctx, facts, null, 1, null), mode);
            Assert.IsNotNull(card, mode + ": the report card");
            Assert.IsFalse(string.IsNullOrEmpty(card.CauseTitle), mode);
            // The exits on that card: one more launch with the same seed, and the designer behind it.
            var again = ReportRoutes.Again();
            Assert.AreEqual(SessionScreens.Design, again.Screen, mode);
            Assert.AreEqual(ctx.Seed, again.Session.Seed, mode);
            Assert.AreEqual(SessionScreens.Missions, GameSession.Apply(RouteId.DesignerBack).Screen, mode + " designer Back");
        }

        // ---------------------------------------------------------------- pause, quit, continue

        [Test]
        public void QuitMidFlight_ThenContinue_ResumesTheMissionDraft()
        {
            tmp = Path.Combine(Path.GetTempPath(), "kh-nav-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var store = new NativeStore(tmp, () => 1000, () => 0.5);
            var library = new DesignLibrary(store);
            Assert.IsFalse(ResumeOffer.Show(library.HasDraft(), library.Count, 0), "fresh install: no CONTINUE");

            var r = HomeRoutes.DesignFirst(campaign, 2, catalog, hangar.templates);
            Assert.IsTrue(PadHandoff.TryTake(out var design, out _));
            library.SaveDraft(design, "Hopper", r.Session.MissionId); // the designer autosaves (DesignLibrary.WatchDraft)
            // Pause → Exit to Home mid-flight is SceneFlow.Go(Home): session and pad handoff cleared.
            Assert.AreEqual(SessionScreens.Home, GameSession.Apply(RouteId.ExitHome).Screen);
            Assert.IsNull(GameSession.Current);

            library = new DesignLibrary(new NativeStore(tmp, () => 2000, () => 0.5));
            Assert.IsTrue(ResumeOffer.Show(library.HasDraft(), library.Count, 0), "existing save: CONTINUE");
            var back = library.LoadDraft(out string mid, out _);
            Assert.IsNotNull(back);
            Assert.AreEqual("m3", mid);
            Assert.AreEqual("m3", back.Str("missionId"));
        }

        // ---------------------------------------------------------------- known bugs on main

        /// <summary>NAV-03 / F4, fixed: Home LAUNCH (and Fly KH-n) brief, then open the designer with the starter. Launch is the designer's.</summary>
        [Test]
        public void NAV03_HomeLaunch_OpensTheDesigner_NotTheCountdown()
        {
            var route = HomeRoutes.Launch(campaign, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Briefing, route.Overlay, Where(route));
            Assert.IsFalse(PadHandoff.FlyOnPad, "the pad is not armed to fly");
            Assert.IsFalse(GameSession.Current.AutoPad, "the briefing reads Build it, not To the pad");
            var built = GameSession.Apply(RouteId.BriefingBuild);
            Assert.AreEqual(SessionScreens.Design, built.Screen, Where(built));
            Assert.IsTrue(PadHandoff.TryTake(out var doc, out bool fly));
            Assert.IsNotNull(doc, "the designer opens with the starter");
            Assert.IsFalse(fly, "Launch from the designer");
            string flow = Src("unity", "Assets", "Kestrel.Game", "Shell", "SceneFlow.cs");
            int start = flow.IndexOf("public void LaunchChosen", StringComparison.Ordinal);
            int end = flow.IndexOf("public void DesignChosen", StringComparison.Ordinal);
            string chosen = flow.Substring(start, end - start);
            Assert.That(chosen, Does.Not.Contain("AutoPad = true"), "Fly KH-n: same as LAUNCH");
            Assert.That(chosen, Does.Not.Contain("PadHandoff.Arm(choice.Design, true"), "Fly KH-n: same as LAUNCH");
        }

        /// <summary>NAV-04 / F4, fixed: the report offers Retry and Back to designer (UiIds report-back-to-designer).</summary>
        [Test]
        public void NAV04_Report_HasBackToDesigner()
        {
            string routes = Src("unity", "Assets", "Kestrel.Slice", "Runtime", "Report", "NextMission.cs");
            int add = routes.IndexOf("public static void AddRoutes", StringComparison.Ordinal);
            int end = routes.IndexOf("public static ReportActions Actions", StringComparison.Ordinal);
            string body = routes.Substring(add, end - add);
            StringAssert.Contains("\"report-retry\"", body);
            StringAssert.Contains("Exit(DesignerId, ReportRoutes.DesignerLabel", body);
            StringAssert.Contains("actions.Designer", body);
            StringAssert.Contains("public const string DesignerId = \"report-back-to-designer\";", routes);
            string slice = Src("unity", "Assets", "Kestrel.Slice", "Runtime", "SliceApp.cs");
            StringAssert.Contains("PostFlightSource.Bind(RestartFlight, ExitToHome, () => { BackToDesign(); ShowDesigner(true); });", slice);
            string host = Src("unity", "Assets", "Kestrel.Game", "Failure", "FailureHost.cs");
            StringAssert.Contains("actions.Designer = () => { leave();", host);

            var ctx = MissionContext.FromCampaign(campaign, "m3");
            GameSession.Carry(ctx);
            ulong seed = ctx.Seed;
            var back = ReportRoutes.BackToDesigner();
            Assert.AreEqual(SessionScreens.Design, back.Screen, Where(back));
            Assert.AreEqual("m3", GameSession.Current.MissionId, "same mission still selected");
            Assert.AreEqual(seed, GameSession.Current.Seed, "same seed");
            Assert.IsFalse(PadHandoff.FlyOnPad, "nothing relaunches");
        }

        [Test]
        public void KnownBug_NAV05_ReportWithoutDebrief_HasOnlyFixIt()
        {
            KnownBug("NAV-05", "a report card built without a debrief still has Retry, Missions and Home", () =>
            {
                string view = Src("unity", "Assets", "Kestrel.Slice", "Runtime", "Report", "ReportView.cs");
                int start = view.IndexOf("return RewardsCard.Build(", StringComparison.Ordinal);
                int end = view.IndexOf("public static VisualElement Icon(", StringComparison.Ordinal);
                string fallback = view.Substring(start, end - start);
                return fallback.Contains("AddRoutes") ? null : "ReportView.Build without a debrief (PostFlightSource.Publish threw) adds only failure-fix and sets no Escape";
            });
        }

        [Test]
        public void KnownBug_NAV07_FromHome_SurvivesTheDesigner_BriefingBackSkipsMissions()
        {
            KnownBug("NAV-07", "once the designer takes the rocket, Back on the reopened briefing returns to Missions", () =>
            {
                HomeRoutes.Launch(campaign, 0, catalog, hangar.templates);
                PadHandoff.TryTake(out _, out _); // the designer takes it (SliceApp StartSession)
                GameSession.Apply(RouteId.DesignerBack); // Missions + briefing
                return PadHandoff.FromHome ? "PadHandoff.FromHome is still true, so SceneFlow.BackFromBriefing routes ExitHome (Home), not Missions" : null;
            });
        }

        [Test]
        public void KnownBug_NAV08_EscapeInFlight_NeverPauses()
        {
            KnownBug("NAV-08", "Escape / hardware back during flight opens the pause menu", () =>
            {
                foreach (var f in Directory.GetFiles(Path.Combine(Golden.Root, "unity", "Assets"), "*.cs", SearchOption.AllDirectories))
                {
                    if (f.Contains("Tests")) continue;
                    if (File.ReadAllText(f).Contains("FlightEscape =")) return null;
                }
                return "MenuBack.FlightEscape is never assigned; AppShell.HandleEscape falls through to whatever back control is visible";
            });
        }

        // ---------------------------------------------------------------- helpers

        Design Stamped(string missionId)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == missionId) return Design.FromJson(JsonMut.CloneObject((JsonObject)row.Arr("designs")[0]));
            throw new InvalidOperationException(missionId);
        }
    }
}
