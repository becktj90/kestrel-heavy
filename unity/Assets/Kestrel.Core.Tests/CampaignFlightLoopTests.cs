// KH-1..KH-20 from the mission board. The board fork is what Home flies.
// KH-1 and KH-2 are armed in HomeAutoBuild and win on every campaign seed.
// KH-3..KH-20 start from data/board-starters.json. Some already win. Some win
// after Auto Fix it. Some win after setting the safety factor back. The KH-3
// starter still misses, so a failed flight does not clear the campaign.
// data/golden/campaign-winners.json holds one passing rocket for every mission.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CampaignFlightLoopTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            GameSession.Clear();
            PadHandoff.Clear();
        }

        [TearDown]
        public void DropSession()
        {
            GameSession.Clear();
            PadHandoff.Clear();
        }

        [Test]
        public void BoardStarters_LoadFromThePackWhenTheCheckoutIsGone()
        {
            string prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            string away = Path.Combine(Path.GetTempPath(), "kh-nostarters-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            string json = File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json"));
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            HomeAutoBuild.BoardStarterPack = () => json;
            try
            {
                Assert.IsNull(Catalog.RepoRootOrNull());
                var design = HomeAutoBuild.BoardStarter("m3");
                Assert.IsNotNull(design);
                Assert.AreEqual("h-n5-k8-23500-2500", design.Str("name"));
                Assert.IsNull(HomeAutoBuild.BoardStarter("m1"), "KH-1 stays on the armed hopper");
            }
            finally
            {
                HomeAutoBuild.BoardStarterPack = null;
                Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev);
                if (Directory.Exists(away)) Directory.Delete(away, true);
            }
        }

        [Test]
        public void EveryMission_StartsFromTheBoard_AndFliesTheStarter()
        {
            Assert.AreEqual(20, campaign.Missions.Count);
            var lines = new List<string>();
            for (int index = 1; index <= 20; index++)
            {
                var mission = campaign.Mission(index);
                Assert.IsNotNull(mission, "KH-" + index);
                Assert.AreEqual(mission.Id, HomeRoutes.NextMission(campaign, index - 1).Id);
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                Assert.IsNotNull(choice, mission.Id);
                Assert.IsNotNull(choice.Design, mission.Id);
                var design = Design.FromJson(choice.Design);
                var ctx = MissionContext.FromMission(campaign, mission, campaign.Seeds[0]);
                ctx.VehicleLine = choice.VehicleLine;
                ctx.ReachLine = choice.ReachLine;
                ctx.AutoPad = choice.LaunchEnabled;
                GameSession.Carry(ctx);
                PadHandoff.Arm(choice.Design, choice.LaunchEnabled, fromHome: false);

                Assert.DoesNotThrow(() => Guide(catalog, design, mission), mission.Id + " guided");
                var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
                Assert.AreNotEqual("error", flight.outcome, mission.Id + " " + flight.failure);
                bool met = FlightDebrief.GoalMet(mission, flight.failure == null && flight.outcome != "failure" && flight.outcome != "crash",
                    flight.apoKm * 1000, flight.periKm * 1000, flight.maxG, flight.landingGrade, FlightDebrief.FromFlight(flight));
                Assert.AreEqual(flight.passed, met, mission.Id + " card gate");

                bool armed = index <= 2;
                if (armed)
                {
                    Assert.IsTrue(choice.LaunchEnabled, mission.Id);
                    foreach (var seed in campaign.Seeds)
                    {
                        var seeded = FlightBatch.Fly(catalog, mission, design, seed);
                        Assert.IsTrue(seeded.passed, mission.Id + " seed " + seed + " " + (seeded.failure ?? "miss")
                            + " apo " + seeded.apoKm.ToString("0.00"));
                    }
                }
                else if (index == 3)
                {
                    Assert.IsFalse(flight.passed, "KH-3 starter still misses, so a failed flight does not clear the campaign");
                }
                lines.Add("KH-" + index + " " + choice.TemplateId
                    + (flight.passed ? " pass" : " fail " + (flight.failure ?? "short"))
                    + " apo " + flight.apoKm.ToString("0.0")
                    + " q " + flight.qPeakKPa.ToString("0.0"));
            }
            TestContext.WriteLine(string.Join("\n", lines));
        }

        [Test]
        public void WinsPersistUnlocksMedalsAndPatches_AMissDoesNot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-loop-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                long minor = 100000;
                var store = CareerStore.Load(new NativeStore(dir, () => 1000, () => 0.25), campaign, minor);
                for (int index = 1; index <= 3; index++)
                {
                    var mission = campaign.Mission(index);
                    var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                    var design = Design.FromJson(choice.Design);
                    var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
                    var ctx = MissionContext.FromMission(campaign, mission, campaign.Seeds[0]);
                    GameSession.Carry(ctx);
                    int cleared = store.Career.Cleared;
                    var card = FlightDebrief.Commit(store, ctx, Facts(mission, flight, choice.TemplateId), NarrationPool.Empty, minor, hangar);
                    Assert.AreEqual(flight.passed, card.Passed, mission.Id);
                    Assert.IsTrue(card.WroteCareer, mission.Id);
                    var again = CareerStore.Load(new NativeStore(dir, () => 1000, () => 0.25), campaign, minor);
                    var record = again.Mission(mission.Id);
                    Assert.IsNotNull(record, mission.Id);
                    Assert.GreaterOrEqual(record.Attempts, 1, mission.Id);
                    Assert.AreEqual(card.Stars, record.BestStars, mission.Id);
                    if (flight.passed)
                    {
                        Assert.AreEqual(index, again.Career.Cleared, mission.Id);
                        Assert.GreaterOrEqual(record.BestStars, 1, mission.Id);
                        foreach (var id in card.Cosmetics)
                            Assert.IsTrue(again.Career.HasUnlock(id), id);
                    }
                    else
                    {
                        Assert.AreEqual(cleared, again.Career.Cleared, mission.Id + " stays locked");
                        var next = GameSession.Apply(RouteId.ReportNext, campaign);
                        Assert.AreEqual("m" + (index + 1), next.Session.MissionId, "Next still moves the session");
                        Assert.AreEqual(cleared, again.Career.Cleared);
                    }
                    store = again;
                }

                GameSession.Carry(MissionContext.FromMission(campaign, campaign.Mission(1)));
                Assert.AreEqual(SessionScreens.Home, GameSession.Apply(RouteId.ReportHome).Screen);
                GameSession.Carry(MissionContext.FromMission(campaign, campaign.Mission(1)));
                Assert.AreEqual(SessionScreens.Missions, GameSession.Apply(RouteId.ReportMissions).Screen);
                GameSession.Carry(MissionContext.FromMission(campaign, campaign.Mission(1)));
                Assert.AreEqual(SessionScreens.Design, GameSession.Apply(RouteId.ReportRetry).Screen);
                Assert.AreEqual("m1", GameSession.Current.MissionId, "Retry keeps the mission");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void CampaignWinners_FlyPassOnEverySeed()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            int seen = 0;
            foreach (JsonObject row in file.Arr("missions"))
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission, row.Str("id"));
                var design = Design.FromJson((JsonObject)row.Arr("designs")[0]);
                Assert.AreEqual(mission.PayloadId, design.payloadType, mission.Id);
                Assert.AreEqual(mission.MassKg, design.payloadMass, mission.Id);
                Assert.AreEqual("campaign", design.tier, mission.Id);
                foreach (var seed in campaign.Seeds)
                {
                    var flight = FlightBatch.Fly(catalog, mission, design, seed);
                    Assert.IsTrue(flight.passed, mission.Id + " seed " + seed + " " + (flight.failure ?? "miss")
                        + " apo " + flight.apoKm.ToString("0.0") + " peri " + flight.periKm.ToString("0.0"));
                }
                seen++;
            }
            Assert.AreEqual(20, seen);
        }

        [Test]
        public void BoardStarter_FixItOrOneStrengthEdit_WinsEveryMission()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            int seen = 0;
            foreach (JsonObject row in file.Arr("missions"))
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission, row.Str("id"));
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                Assert.IsNotNull(choice, mission.Id);
                var design = Design.FromJson(choice.Design);
                Assert.AreEqual(mission.MassKg, design.payloadMass, mission.Id);
                string mode = row.Str("mode");
                if (mode == "fly")
                {
                    AssertSeeds(mission, design);
                }
                else if (mode == "fix")
                {
                    var raw = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
                    Assert.IsFalse(raw.passed, mission.Id + " starter is the near miss");
                    var json = JsonMut.CloneObject(choice.Design);
                    Readout.Reference(catalog, json);
                    var report = StepperModel.Evaluate(catalog, json);
                    StepCheck check = null;
                    for (int i = 0; i < report.Checks.Length; i++)
                    {
                        if (report.Checks[i] != null && !string.IsNullOrEmpty(report.Checks[i].Code))
                        {
                            check = report.Checks[i];
                            break;
                        }
                    }
                    Assert.IsNotNull(check, mission.Id + " has a warning for Fix it");
                    StepperFix.Apply(catalog, json, check);
                    AssertSeeds(mission, Design.FromJson(json));
                }
                else if (mode == "strength")
                {
                    var raw = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
                    Assert.IsFalse(raw.passed, mission.Id + " starter is the near miss");
                    design.safetyFactor = row.Num("restoreSafetyFactor");
                    AssertSeeds(mission, design);
                }
                else Assert.Fail(mission.Id + " mode " + mode);
                seen++;
            }
            Assert.AreEqual(18, seen, "KH-3 through KH-20");
        }

        void AssertSeeds(CampaignMission mission, Design design)
        {
            foreach (var seed in campaign.Seeds)
            {
                var flight = FlightBatch.Fly(catalog, mission, design, seed);
                Assert.IsTrue(flight.passed, mission.Id + " seed " + seed + " " + (flight.failure ?? "miss")
                    + " apo " + flight.apoKm.ToString("0.0") + " peri " + flight.periKm.ToString("0.0")
                    + " q " + flight.qPeakKPa.ToString("0.0"));
            }
        }

        [Test]
        public void CampaignViableDesigns_WinM1ThroughM10()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-viable.json")));
            int seen = 0;
            foreach (JsonObject row in file.Arr("missions"))
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission);
                var design = Design.FromJson((JsonObject)row.Arr("designs")[0]);
                foreach (var seed in campaign.Seeds)
                {
                    var flight = FlightBatch.Fly(catalog, mission, design, seed);
                    Assert.IsTrue(flight.passed, mission.Id + " seed " + seed + " " + (flight.failure ?? "miss")
                        + " apo " + flight.apoKm.ToString("0.0") + " peri " + flight.periKm.ToString("0.0"));
                }
                seen++;
            }
            Assert.AreEqual(10, seen, "viable file covers KH-1..KH-10");
        }

        [Test]
        public void StageSeparation_UsesTheSpentStageNumber()
        {
            var mission = campaign.Mission(4);
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-viable.json")));
            JsonObject doc = null;
            foreach (JsonObject row in file.Arr("missions"))
            {
                if (row.Str("id") != "m4") continue;
                doc = (JsonObject)row.Arr("designs")[0];
            }
            Assert.IsNotNull(doc);
            var design = Design.FromJson(doc);
            var input = Stack.SimInputs(catalog, design, 0.2, 0);
            input.opts.targetAlt = mission.GuidanceAltM;
            input.opts.monitor = FailureMonitor.Create(catalog, design, mission.GuidanceAltM, 1, 1);
            var sim = Headless.Simulate(input.stages, input.opts);
            int seps = 0;
            for (int i = 0; i < sim.events.Count; i++)
            {
                var sep = sim.events[i];
                if (sep.type != "separation" || !sep.stage.HasValue) continue;
                seps++;
                bool burned = false;
                for (int j = 0; j < i; j++)
                {
                    var burn = sim.events[j];
                    if (burn.type == "burnout" && burn.stage == sep.stage) burned = true;
                }
                Assert.IsTrue(burned, "separation " + sep.stage + " follows that stage's burnout");
                bool lit = false;
                for (int j = i; j < sim.events.Count; j++)
                {
                    var ign = sim.events[j];
                    if (ign.type == "ignition" && ign.stage == sep.stage + 1) lit = true;
                }
                Assert.IsTrue(lit, "ignition is the next stage after " + sep.stage);
            }
            Assert.Greater(seps, 0, "the workhorse stages");
        }

        [Test]
        public void Haven_SuccessTipAndMiss()
        {
            var craft = new RecoveryCraft { Legs = true, LegsKind = "light", Mode = "barge" };
            var deck = RecoveryScore.Judge(Sample(1, 0, 0, 1, 20), craft, null);
            Assert.IsNull(deck.Cause);
            Assert.That(deck.GradeName, Is.EqualTo("Perfect").Or.EqualTo("Good"));
            Assert.That(deck.Medal, Is.EqualTo("gold").Or.EqualTo("silver"));

            var tip = RecoveryScore.Judge(Sample(1, 20, 0, 1, 20), craft, null);
            Assert.AreEqual(RecoveryScore.Tilt, tip.Cause);
            Assert.AreEqual("bronze", tip.Medal);

            var miss = RecoveryScore.Judge(Sample(1, 0, RecoverySim.DeckRadiusM + 10, 1, 20), craft, null);
            Assert.AreEqual(RecoveryScore.Miss, miss.Cause);
            Assert.IsNull(miss.Medal);
        }

        [Test]
        public void Count_AbortSkipPauseAndRetry()
        {
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, campaign.Mission(1)).Design);
            var sequence = LaunchSequence.Open(TimelineGenerator.Generate(catalog, design, Pad.For(catalog, design), new Mission { mode = "standard" }, 1));
            var handoff = new LiftoffHandoff();
            handoff.Enter(sequence);
            Assert.IsTrue(handoff.Counting);
            handoff.Tick(LiftoffHandoff.MaxStep, () => true);
            Assert.IsTrue(handoff.Counting, "one frame does not skip the count");
            handoff.Finish();
            Assert.IsTrue(handoff.Closed);
            bool aborted = false;
            handoff.Tick(1, () => { aborted = true; return true; });
            Assert.IsFalse(aborted, "abort closes the count");

            var retry = new LiftoffHandoff();
            retry.Enter(LaunchSequence.Open(TimelineGenerator.Generate(catalog, design, Pad.For(catalog, design), new Mission { mode = "standard" }, 1)));
            bool skipped = false;
            retry.Skip(() => { skipped = true; return true; });
            Assert.IsTrue(skipped);
            Assert.IsTrue(retry.Ascending, "skip count lifts off");

            var input = Stack.SimInputs(catalog, design, 0.2, 0);
            input.opts.monitor = FailureMonitor.Create(catalog, design, campaign.Mission(1).GuidanceAltM, 1, 1);
            var sim = new AscentSim(input.stages, input.opts);
            sim.Tick();
            double pausedAt = sim.T;
            Assert.AreEqual(pausedAt, sim.T, "pause does not advance the sim");
            sim.Tick();
            Assert.Greater(sim.T, pausedAt, "resume continues the same flight");

            var restarted = new AscentSim(input.stages, input.opts);
            Assert.AreEqual(0, restarted.T, "retry mid-flight starts at T+0");
            restarted.Tick();
            Assert.Greater(restarted.T, 0);
        }

        static FlightFacts Facts(CampaignMission mission, CampaignFlight flight, string designId)
        {
            return new FlightFacts
            {
                Passed = flight.passed,
                EconomyKnown = true,
                Margin = flight.margin,
                CostKc = flight.costKc,
                BudgetKc = mission.BudgetKc,
                LandingRequired = mission.LandingRequired || mission.LandBoosters,
                LandingGrade = flight.landingGrade,
                PeakG = flight.maxG,
                GLimit = double.IsNaN(mission.MaxG) ? 0 : mission.MaxG,
                CostMinor = flight.costMinor,
                Seed = flight.seed,
                DesignId = designId,
                ArticleKey = "core",
            };
        }

        static void Guide(Catalog catalog, Design design, CampaignMission mission)
        {
            var input = Stack.SimInputs(catalog, design, 0.2, 0);
            input.opts.targetAlt = mission.GuidanceAltM;
            if (input.opts.guidance != null) input.opts.guidance.targetAlt = mission.GuidanceAltM;
            var monitor = FailureMonitor.Create(catalog, design, mission.GuidanceAltM, 1, 1);
            input.opts.monitor = monitor;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var vehicle = AscentDecisions.FromDesign(design, catalog, monitor.Events, null, qLim, mission.GuidanceAltM, false, null, 1, 1);
            var director = AscentDirector.Start(vehicle, monitor.Events, null, true, false, true);
            var sim = new AscentSim(input.stages, input.opts);
            int n = 0;
            while (!sim.Done && n++ < 4000)
            {
                var cmd = director.Command;
                double scale = cmd.ThrottleScale;
                if (double.IsNaN(scale) || scale <= 0) scale = 1;
                else if (scale > 1) scale = 1;
                sim.LiveThrottleScale = scale;
                sim.Tick();
                if (monitor.Event != null)
                {
                    sim.BreakUp(null);
                    break;
                }
                director.Advance(new AscentSample
                {
                    T = sim.T,
                    AltM = sim.LastQ >= 0 ? Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]) - Gravity.R_EARTH : 0,
                    QPa = sim.LastQ,
                    MassKg = sim.Y[4],
                    ThrustN = sim.LastThrust,
                    PropFraction = double.IsNaN(sim.PropellantFraction) ? 1 : sim.PropellantFraction,
                    StageIndex = sim.StageIdx,
                    Thrusting = sim.Thrusting,
                }, 0.2);
            }
        }

        static RecoverySample Sample(double sink, double tilt, double miss, double legs, double prop)
        {
            return new RecoverySample
            {
                Sink = sink,
                TiltDeg = tilt,
                MissM = miss,
                Legs01 = legs,
                Prop = prop,
                T = 90,
            };
        }
    }
}
