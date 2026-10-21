// #313: one flight model for prediction and play. The batch, the designer practice flight, the launch timeline and
// the live pad all step the same AscentDirector through DirectedFlight (MissionFlightSetup.Director/Fly). Kestrel
// Physics' rulings: the design's built-in max-Q throttle (GuidanceProfile) is the only max-Q law, so the bare sim, the
// batch, the designer and the pad are identical; the engines cut off at the target's specific orbital energy, located
// inside the step; the fuel-imbalance penalty throttles only the imbalanced stage, until it separates or shuts down.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SharedDirectorTests
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
        }

        /// <summary>The live pad: FlightDriver's setup at its 1/120 s step, the batch's monitor and the shared director.</summary>
        SimResult Live(CampaignMission mission, Design design, ulong seed, double dt = MissionFlightSetup.LiveDt)
        {
            var setup = MissionFlightSetup.Build(catalog, mission, design, dt);
            var monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(design), seed);
            monitor.Gentle = false;
            setup.Options.monitor = monitor;
            var director = MissionFlightSetup.Director(catalog, setup, design, FlightBatch.DesignHash(design), seed);
            return MissionFlightSetup.Fly(setup, director);
        }

        IEnumerable<(CampaignMission mission, Design design)> Winners()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                yield return (campaign.ById(row.Str("id")), Design.FromJson((JsonObject)row.Arr("designs")[0]));
        }

        Design Viable(string missionId, string id)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-viable.json")));
            foreach (JsonObject row in file.Arr("missions"))
            {
                if (row.Str("id") != missionId) continue;
                foreach (JsonObject d in row.Arr("designs"))
                    if (d.Str("id") == id) return Design.FromJson(d);
            }
            Assert.Fail(missionId + " " + id + " missing");
            return null;
        }

        /// <summary>KH-1 and KH-2 fly the armed hopper. KH-3..KH-20 fly the board starter after its one documented edit.</summary>
        IEnumerable<(CampaignMission mission, Design design, string mode)> Starters()
        {
            for (int i = 1; i <= 2; i++)
            {
                var m = campaign.Mission(i);
                yield return (m, Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design), "armed");
            }
            var board = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            foreach (JsonObject row in board.Arr("missions"))
            {
                var m = campaign.ById(row.Str("id"));
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, m);
                string mode = row.Str("mode");
                if (mode == "strength")
                {
                    var d = Design.FromJson(choice.Design);
                    d.safetyFactor = row.Num("restoreSafetyFactor");
                    yield return (m, d, mode);
                }
                else if (mode == "fix")
                {
                    var json = JsonMut.CloneObject(choice.Design);
                    Readout.Reference(catalog, json);
                    var report = StepperModel.Evaluate(catalog, json);
                    StepCheck check = null;
                    for (int c = 0; c < report.Checks.Length && check == null; c++)
                        if (report.Checks[c] != null && !string.IsNullOrEmpty(report.Checks[c].Code)) check = report.Checks[c];
                    Assert.IsNotNull(check, m.Id + " has a warning for Fix it");
                    StepperFix.Apply(catalog, json, check);
                    yield return (m, Design.FromJson(json), mode);
                }
                else yield return (m, Design.FromJson(choice.Design), mode);
            }
        }

        [Test]
        public void ThePredictionPlayer_IsTheDefaultPlayer()
        {
            var progress = new StepperProgress();
            Assert.AreEqual(progress.AutoAssistOn(DesignerDepth.Simple), MissionFlightSetup.DefaultAutoAssist);
        }

        [Test]
        public void DefaultPlayer_EveryGoldenWinnerAndStarter_PassesOnEverySeed()
        {
            int winners = 0, starters = 0;
            var misses = new List<string>();
            foreach (var (mission, design) in Winners())
            {
                foreach (var seed in campaign.Seeds)
                {
                    var f = FlightBatch.Fly(catalog, mission, design, seed);
                    if (!f.passed) misses.Add("winner " + mission.Id + " seed " + seed + " " + (f.failure ?? "miss") + " apo " + f.apoKm.ToString("0.0") + " peri " + f.periKm.ToString("0.0"));
                }
                winners++;
            }
            foreach (var (mission, design, mode) in Starters())
            {
                foreach (var seed in campaign.Seeds)
                {
                    var f = FlightBatch.Fly(catalog, mission, design, seed);
                    if (!f.passed) misses.Add("starter(" + mode + ") " + mission.Id + " seed " + seed + " " + (f.failure ?? "miss") + " apo " + f.apoKm.ToString("0.0") + " peri " + f.periKm.ToString("0.0"));
                }
                starters++;
            }
            Assert.AreEqual(20, winners);
            Assert.AreEqual(20, starters);
            Assert.IsEmpty(misses, string.Join("\n", misses));
        }

        [Test]
        public void HopHeadroom_KeepsEverySeedClearOfTheLowEdge()
        {
            // Physics: the KH-1 starter peaks at 9.7 km or more; m2b and m2f at least 2 km over the 95 km edge.
            var m1 = campaign.Mission(1);
            var m2 = campaign.Mission(2);
            var starter = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m1).Design);
            Assert.AreEqual(515, starter.stages[0].propMass, 1e-9);
            foreach (var seed in campaign.Seeds)
            {
                var f = FlightBatch.Fly(catalog, m1, starter, seed);
                Assert.IsTrue(f.passed, "KH-1 starter seed " + seed);
                Assert.That(f.apoKm, Is.GreaterThanOrEqualTo(9.7), "KH-1 starter seed " + seed);
                var b = FlightBatch.Fly(catalog, m1, Viable("m1", "m1b-705"), seed);
                Assert.IsTrue(b.passed, "m1b seed " + seed);
                Assert.That(b.apoKm, Is.GreaterThanOrEqualTo(9.7), "m1b seed " + seed);
                foreach (var id in new[] { "m2b-2600", "m2f-1600" })
                {
                    var g = FlightBatch.Fly(catalog, m2, Viable("m2", id), seed);
                    Assert.IsTrue(g.passed, id + " seed " + seed);
                    Assert.That(g.apoKm, Is.GreaterThanOrEqualTo(97.0), id + " seed " + seed);
                }
            }
        }

        [Test]
        public void BatchAndLivePad_Agree_HopsOnThePeak()
        {
            var hops = new List<(CampaignMission, Design, string)>();
            for (int i = 1; i <= 2; i++)
            {
                var m = campaign.Mission(i);
                hops.Add((m, Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design), m.Id + " starter"));
            }
            foreach (var (m, d) in Winners())
                if (MissionAim.IsApogeeHop(m)) hops.Add((m, d, m.Id + " winner " + d.id));
            hops.Add((campaign.Mission(1), Viable("m1", "m1b-705"), "m1b-705"));
            hops.Add((campaign.Mission(1), Viable("m1", "m1p-360"), "m1p-360"));
            hops.Add((campaign.Mission(2), Viable("m2", "m2b-2600"), "m2b-2600"));
            hops.Add((campaign.Mission(2), Viable("m2", "m2f-1600"), "m2f-1600"));
            Assert.That(hops.Count, Is.GreaterThanOrEqualTo(8));
            foreach (var (m, d, label) in hops)
            {
                var batch = FlightBatch.Fly(catalog, m, d, campaign.Seeds[0]);
                var live = Live(m, d, campaign.Seeds[0]);
                double livePeak = MissionScore.FlownApoAltM(live) / 1000;
                Assert.That(Math.Abs(batch.apoKm - livePeak) / livePeak, Is.LessThanOrEqualTo(MissionFlightSetup.HopPeakTolerance),
                    label + " batch " + batch.apoKm.ToString("0.000") + " live " + livePeak.ToString("0.000"));
                Assert.AreEqual(batch.passed, FlightDebrief.GoalMet(m, live.failure == null && live.outcome != "failure", MissionScore.FlownApoAltM(live), live.orbit.periAlt, live.maxG, null), label + " verdict");
            }
        }

        [Test]
        public void BatchAndLivePad_Agree_OrbitsOnApoapsisPeriapsisAndSemiMajorAxis()
        {
            // Every orbital winner and board starter, seed 1: the 0.2 s batch against the 1/120 s pad.
            int orbits = 0;
            var misses = new List<string>();
            var flights = new List<(CampaignMission, Design, string)>();
            foreach (var (m, d) in Winners()) if (!MissionAim.IsApogeeHop(m)) flights.Add((m, d, "winner"));
            foreach (var (m, d, mode) in Starters()) if (!MissionAim.IsApogeeHop(m)) flights.Add((m, d, "starter(" + mode + ")"));
            foreach (var (m, d, label) in flights)
            {
                var batch = Live(m, d, campaign.Seeds[0], MissionFlightSetup.BatchDt);
                var live = Live(m, d, campaign.Seeds[0]);
                Assert.AreEqual(batch.outcome, live.outcome, label + " " + m.Id);
                double apo = Math.Abs(batch.orbit.apoAlt - live.orbit.apoAlt) / live.orbit.apoAlt;
                double peri = Math.Abs(batch.orbit.periAlt - live.orbit.periAlt);
                double sma = Math.Abs(batch.orbit.a - live.orbit.a) / live.orbit.a;
                if (apo > MissionFlightSetup.OrbitApoTolerance || peri > MissionFlightSetup.OrbitPeriToleranceM || sma > MissionFlightSetup.OrbitSmaTolerance)
                    misses.Add(label + " " + m.Id + " apo " + (apo * 100).ToString("0.000") + "% peri " + peri.ToString("0") + " m sma " + (sma * 100).ToString("0.0000") + "%");
                orbits++;
            }
            Assert.AreEqual(36, orbits);
            Assert.IsEmpty(misses, string.Join("\n", misses));
        }

        [Test]
        public void SharedDirector_IsDeterministic()
        {
            foreach (var id in new[] { 1, 2, 6 })
            {
                var m = campaign.Mission(id);
                Design d = null;
                foreach (var (wm, wd) in Winners()) if (wm.Id == m.Id) d = wd;
                foreach (var dt in new[] { MissionFlightSetup.BatchDt, MissionFlightSetup.LiveDt })
                {
                    var a = Live(m, d, campaign.Seeds[0], dt);
                    var b = Live(m, d, campaign.Seeds[0], dt);
                    Assert.AreEqual(a.outcome, b.outcome);
                    Assert.AreEqual(BitConverter.DoubleToInt64Bits(MissionScore.FlownApoAltM(a)), BitConverter.DoubleToInt64Bits(MissionScore.FlownApoAltM(b)), m.Id);
                    Assert.AreEqual(BitConverter.DoubleToInt64Bits(a.orbit.periAlt), BitConverter.DoubleToInt64Bits(b.orbit.periAlt), m.Id);
                    Assert.AreEqual(BitConverter.DoubleToInt64Bits(a.maxG), BitConverter.DoubleToInt64Bits(b.maxG), m.Id);
                }
                var f1 = FlightBatch.Fly(catalog, m, d, campaign.Seeds[0]);
                var f2 = FlightBatch.Fly(catalog, m, d, campaign.Seeds[0]);
                Assert.AreEqual(BitConverter.DoubleToInt64Bits(f1.apoKm), BitConverter.DoubleToInt64Bits(f2.apoKm));
                Assert.AreEqual(BitConverter.DoubleToInt64Bits(f1.periKm), BitConverter.DoubleToInt64Bits(f2.periKm));
            }
        }

        [Test]
        public void BatchDesignerAndTimeline_FlyTheSameDirectedFlight()
        {
            // The designer practice flight and the launch timeline fly ReferenceAscent.Flight: the predict monitor's
            // schedule (hash 1, seed 1) and the shared director. Flying the same inputs by hand gives the same peak.
            var m = campaign.Mission(1);
            var json = HomeAutoBuild.Choose(catalog, hangar.templates, m).Design;
            var design = Design.FromJson(json);
            var designer = ReferenceAscent.Simulate(catalog, json);
            var input = ReferenceAscent.Inputs(catalog, json);
            var setup = new MissionFlightInputs { Stages = input.stages, Options = input.opts, TargetAltM = m.GuidanceAltM };
            var hand = MissionFlightSetup.Fly(setup, MissionFlightSetup.Director(catalog, setup, design, ReferenceAscent.PredictHash, ReferenceAscent.PredictSeed));
            Assert.AreEqual(MissionScore.FlownApoAltM(hand), MissionScore.FlownApoAltM(designer));
            // Physics ruling 4: the director calls the design's own max-Q law, so the bare sim flies the same peak.
            var bare = Headless.Simulate(ReferenceAscent.Inputs(catalog, json).stages, ReferenceAscent.Inputs(catalog, json).opts);
            Assert.AreEqual(MissionScore.FlownApoAltM(bare), MissionScore.FlownApoAltM(designer), "the default player flies the design's own max-Q law");
            var timeline = LaunchPredictor.Predict(catalog, design);
            Assert.AreEqual(Math.Floor(MissionScore.FlownApoAltM(designer) / 100) / 10, timeline.ApoKm, 1e-9);
            var batch = FlightBatch.Fly(catalog, m, design, campaign.Seeds[0]);
            Assert.That(Math.Abs(batch.apoKm * 1000 - MissionScore.FlownApoAltM(designer)) / MissionScore.FlownApoAltM(designer), Is.LessThanOrEqualTo(MissionFlightSetup.HopPeakTolerance));
        }

        [Test]
        public void TheEnginesCutOffAtTheTargetEnergy_LocatedInsideTheStep()
        {
            // Physics ruling 1 and 2: ε = −μ/(2a), a = R + (h_apo + h_peri)/2, located by bisection; no perigee test.
            var m = campaign.Mission(6);
            Design d = null;
            foreach (var (wm, wd) in Winners()) if (wm.Id == m.Id) d = wd;
            double target = SimOptions.EnergyFor(m.ApoKm * 1000, m.PeriKm * 1000);
            Assert.AreEqual(target, MissionFlightSetup.TargetEnergy(m), 0);
            foreach (var dt in new[] { MissionFlightSetup.BatchDt, MissionFlightSetup.LiveDt })
            {
                var setup = MissionFlightSetup.Build(catalog, m, d, dt);
                Assert.IsTrue(setup.Options.energyCutoff);
                Assert.AreEqual(target, setup.Options.cutoffEnergy, 0);
                var r = MissionFlightSetup.Fly(setup, null);
                Assert.AreEqual("orbit", r.outcome);
                CutoffT(r);
                Assert.AreEqual(target, r.orbit.energy, Math.Abs(target) * 1e-9, "dt " + dt);
                Assert.AreEqual(Gravity.R_EARTH + 0.5 * (m.ApoKm + m.PeriKm) * 1000, r.orbit.a, 1.0, "dt " + dt);
            }
            // Ellipse: the mean of apoapsis and periapsis. C3: half the C3. Apogee hops never cut off on energy.
            var gto = campaign.Mission(7);
            Assert.AreEqual(SimOptions.EnergyFor(gto.ApoKm * 1000, gto.PeriKm * 1000), MissionFlightSetup.TargetEnergy(gto), 0);
            var c3 = campaign.Mission(9);
            Assert.AreEqual(0.5 * c3.C3Km2PerS2 * 1e6, MissionFlightSetup.TargetEnergy(c3), 0);
            Assert.IsTrue(double.IsNaN(MissionFlightSetup.TargetEnergy(campaign.Mission(1))));
            Assert.IsFalse(new SimOptions().energyCutoff, "raw SimOptions keep the JS simulate() parity path");
        }

        [Test]
        public void TheDesignMaxQThrottle_IsTheOnlyMaxQLaw_ForEveryHop()
        {
            // Physics ruling 4: bare sim, batch and live fly the same peak for the hop starters and winners.
            for (int i = 1; i <= 2; i++)
            {
                var m = campaign.Mission(i);
                var d = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);
                foreach (var seed in campaign.Seeds)
                {
                    var setup = MissionFlightSetup.Build(catalog, m, d, MissionFlightSetup.BatchDt);
                    setup.Options.monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(d), seed);
                    setup.Options.monitor.Gentle = false;
                    var bare = Headless.Simulate(setup.Stages, setup.Options);
                    var batch = FlightBatch.Fly(catalog, m, d, seed);
                    Assert.AreEqual(MissionScore.FlownApoAltM(bare) / 1000.0, batch.apoKm, 1e-12, m.Id + " seed " + seed);
                }
            }
        }

        [Test]
        public void TheFuelImbalancePenalty_ThrottlesOnlyItsStage_UntilItSeparates()
        {
            // Physics ruling 3. A stage-0 imbalance resolved without crossfeed: 0.97 on stage 0, nothing on stage 1.
            var vehicle = new AscentVehicle { Stages = 2, FuelEvent = true, QLimPa = 35000, ThrottleMin = 0.4 };
            var director = AscentDirector.Start(vehicle, null, null, true, false, false);
            var s = new AscentSample { T = 20, AltM = 5000, SpeedMps = 300, QPa = 1000, Thrusting = true, PropFraction = 0.8, StageIndex = 0 };
            for (int i = 0; i < 80 && !director.IsDone(AscentCall.FuelBalance); i++)
            {
                s.T += 0.5;
                director.Advance(s, 0.5);
            }
            Assert.IsTrue(director.IsDone(AscentCall.FuelBalance));
            Assert.AreEqual(AscentDecisions.FuelPenalty, director.Command.ThrottleScale, 1e-12, "the imbalanced stage");
            s.StageIndex = 1; s.PropFraction = 1; s.T += 0.5;
            director.Advance(s, 0.5);
            Assert.AreEqual(1, director.Command.ThrottleScale, 1e-12, "the next stage burns at full throttle");
            s.StageIndex = 0; s.Thrusting = false; s.T += 0.5;
            director.Advance(s, 0.5);
            Assert.AreEqual(1, director.Command.ThrottleScale, 1e-12, "shut down");
        }

        static double CutoffT(SimResult r)
        {
            foreach (var e in r.events) if (e.type == "cutoff") return e.t;
            Assert.Fail("no cutoff event");
            return double.NaN;
        }
    }
}
