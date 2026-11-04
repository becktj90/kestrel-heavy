// #313 (Arcade, TestFlight): Trevor's KH-3 "Tiny Orbit" bent at T+166 (84 kPa, ~33 km) in the live loop on main. The
// live director auto-accepted a held -1 deg pitch nudge and flew a default 0.70 max-Q bucket over the design's own
// throttle. Under #313's shared director the autopilot never takes the nudge, and a max-Q throttle flies only when the
// design sets one (AscentDirector.AutopilotAccept / Refresh). This flies the KH-3 starter, as built and with its one-tap
// strength restore, through the live pad loop on default settings: AscentFlight.Prepare's director (Simple depth,
// auto-assist as the fresh design reads it, seed 1, briefing wind and pad) and FlightDriver's frame clock
// (1/60 s frames, LiveDt physics, DirectedFlight.Step, played checks).
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LivePadKh3Tests
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

        double Restored(string missionId)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == missionId) return row.Num("restoreSafetyFactor");
            Assert.Fail(missionId + " has no strength restore");
            return double.NaN;
        }

        sealed class LiveRun
        {
            public SimResult Result;
            public AscentDirector Director;
            public double SeparatedAt = -1;
            public bool Auto;
            public string Text;
        }

        LiveRun Play(int index, double safetyFactor, double pitchKickDeg = double.NaN)
        {
            const double frame = 1.0 / 60.0;
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            if (!double.IsNaN(safetyFactor)) design.safetyFactor = safetyFactor;
            if (!double.IsNaN(pitchKickDeg)) design.guidance.pitchKickDeg = pitchKickDeg;
            ulong hash = FlightBatch.DesignHash(design);
            const ulong seed = 1; // AscentFlight.ReadSeed on a design with no seed
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            bool auto = StepperProgress.Read(choice.Design).AutoAssistOn(DesignerDepth.Simple);

            // AscentFlight.Prepare
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, auto, false, false, card?.Bonuses);

            // FlightDriver.Launch
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var directed = new DirectedFlight(sim, director, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);

            // FlightDriver.Update
            var run = new LiveRun { Director = director, Auto = auto };
            double clock = 0;
            int frames = 0;
            while (!sim.Done && frames++ < 400000)
            {
                clock += frame;
                int guard = 0;
                bool stop = false;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    directed.Step();
                    for (int i = before; i < sim.Events.Count; i++)
                        if (sim.Events[i].type == "separation" && run.SeparatedAt < 0) run.SeparatedAt = sim.Events[i].t;
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) { stop = true; break; }
                }
                if (stop) break;
            }
            Assert.IsTrue(sim.Done, "the live flight ends");
            run.Result = directed.Finish();
            run.Text = "KH-" + index + " SF " + design.safetyFactor.ToString("0.00") + " auto " + auto + ": " + run.Result.outcome
                + " " + (run.Result.orbit.apoAlt / 1000).ToString("0.0") + " x " + (run.Result.orbit.periAlt / 1000).ToString("0.0") + " km"
                + " sep T+" + run.SeparatedAt.ToString("0.0") + " maxQ " + (run.Result.maxQ / 1000).ToString("0.0") + " kPa"
                + " nudge " + director.NudgeDeg.ToString("0.0") + " deg"
                + (run.Result.failure != null ? " failure " + run.Result.failure.kind + " T+" + run.Result.failure.t.ToString("0.0") : "");
            return run;
        }

        [Test]
        public void Kh3Starter_DefaultSettings_LiveLoop_SeparatesAndReachesOrbit()
        {
            // The starter as the home auto-build hands it over, no edits (calm-air retune: an 8.65° kick, strength 1.45).
            // Seed 1 in the briefing wind with no wind-shear ease (#338): 200.4 x 199.6 km, separation at T+154.3: inside
            // the +/-5% band of KH-3's 200 km circle. (203.3 x 196.7 km while the ease still flew.)
            var mission = campaign.Mission(3);
            var run = Play(3, double.NaN);
            TestContext.WriteLine(run.Text);
            Assert.IsTrue(run.Auto, "default settings: Simple depth, auto-assist on");
            Assert.IsNull(run.Result.failure, run.Text);
            Assert.That(run.SeparatedAt, Is.GreaterThan(0), "stage 1 separates / " + run.Text);
            Assert.AreEqual(154.3, run.SeparatedAt, 0.5, run.Text);
            Assert.AreEqual("orbit", run.Result.outcome, run.Text);
            Assert.AreEqual(200.4, run.Result.orbit.apoAlt / 1000, 0.5, run.Text);
            Assert.AreEqual(199.6, run.Result.orbit.periAlt / 1000, 0.5, run.Text);
            Assert.That(MissionScore.TargetMet(mission.TargetKind, mission.ApoKm, mission.PeriKm, mission.C3Km2PerS2,
                run.Result.orbit.apoAlt, run.Result.orbit.periAlt, MissionScore.C3(run.Result.orbit.energy) / 1e6, false, out _), run.Text);
            // No held nose-down nudge: the autopilot never takes the pitch call (AscentDirector.AutopilotAccept).
            Assert.AreEqual(0.0, run.Director.NudgeDeg, 1e-12, run.Text);
        }

        [Test]
        public void Kh3EightDegreeBuild_DefaultSettings_LiveLoop_SeparatesWithNoBreakup_AndMissesAsInCalmAir()
        {
            // The pre-retune 8° build (t432), no other edits. While the wind-shear ease flew it reached 201.8 x 198.2 km in
            // the seed-1 wind, carried by the ease; calm air misses too flat. With no ease (#338): separation at T+154.3, no
            // breakup, suborbital 308.0 x 92.0 km, outside the band.
            var mission = campaign.Mission(3);
            var run = Play(3, double.NaN, 8.0);
            TestContext.WriteLine(run.Text);
            Assert.IsNull(run.Result.failure, run.Text);
            Assert.AreEqual(154.3, run.SeparatedAt, 0.5, run.Text);
            Assert.AreEqual("suborbital", run.Result.outcome, run.Text);
            Assert.AreEqual(308.0, run.Result.orbit.apoAlt / 1000, 0.5, run.Text);
            Assert.AreEqual(92.0, run.Result.orbit.periAlt / 1000, 0.5, run.Text);
            Assert.IsFalse(MissionScore.TargetMet(mission.TargetKind, mission.ApoKm, mission.PeriKm, mission.C3Km2PerS2,
                run.Result.orbit.apoAlt, run.Result.orbit.periAlt, MissionScore.C3(run.Result.orbit.energy) / 1e6, false, out _), run.Text);
            Assert.AreEqual(0.0, run.Director.NudgeDeg, 1e-12, run.Text);
        }

        [Test]
        public void Kh3EightDegreeBuild_WithItsStrengthRestore_LiveLoop_SeparatesAndReachesOrbit()
        {
            // The pre-retune 8° build with the board's old one-tap strength restore (1.9), the flight that bent at T+166 on
            // main before #313. While the wind-shear ease flew: 223.1 x 176.9 km, outside the band. With no ease (#338):
            // 203.2 x 196.8 km, separation at T+153.4, no breakup, inside the +/-5% band of the 200 km circle. KH-3's board
            // row is now "fly" (8.65° kick), so this pins the old t432 rocket.
            var mission = campaign.Mission(3);
            var run = Play(3, Restored("m3"), 8.0);
            TestContext.WriteLine(run.Text);
            Assert.IsNull(run.Result.failure, run.Text);
            Assert.That(run.SeparatedAt, Is.GreaterThan(0), "stage 1 separates / " + run.Text);
            Assert.AreEqual(153.4, run.SeparatedAt, 0.5, run.Text);
            Assert.AreEqual("orbit", run.Result.outcome, run.Text);
            Assert.AreEqual(203.2, run.Result.orbit.apoAlt / 1000, 0.5, run.Text);
            Assert.AreEqual(196.8, run.Result.orbit.periAlt / 1000, 0.5, run.Text);
            Assert.That(MissionScore.TargetMet(mission.TargetKind, mission.ApoKm, mission.PeriKm, mission.C3Km2PerS2,
                run.Result.orbit.apoAlt, run.Result.orbit.periAlt, MissionScore.C3(run.Result.orbit.energy) / 1e6, false, out _), run.Text);
            Assert.AreEqual(0.0, run.Director.NudgeDeg, 1e-12, run.Text);
        }
    }

    /// <summary>The same live pad loop as <see cref="LivePadKh3Tests"/> for any design: seed 1, auto-assist on (Simple depth),
    /// FlightDriver's 1/60 s frames at LiveDt with played checks. <paramref name="wind"/> false flies calm air on the
    /// reference pad (the batch's conditions); true flies the seed-1 briefing wind and pad.</summary>
    internal static class LivePadLoop
    {
        public static SimResult Fly(Catalog catalog, CampaignMission mission, Design design, bool wind)
        {
            ulong hash = FlightBatch.DesignHash(design);
            const ulong seed = 1;
            var card = MissionBriefing.Load(Golden.Root, mission.Index, hash, seed);
            var weather = wind ? card?.Wind : null;
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, weather, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, MissionFlightSetup.DefaultAutoAssist, false, false, wind ? card?.Bonuses : null);
            var site = new MissionFlightSite { Wind = weather };
            if (wind && card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var directed = new DirectedFlight(sim, director, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            double clock = 0;
            int frames = 0;
            bool stop = false;
            while (!sim.Done && !stop && frames++ < 400000)
            {
                clock += 1.0 / 60.0;
                int guard = 0;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    directed.Step();
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) { stop = true; break; }
                }
            }
            Assert.IsTrue(sim.Done, "the live flight ends");
            return directed.Finish();
        }

        /// <summary>True when an intact live flight lands the mission's band (MissionScore.TargetMet).</summary>
        public static bool InBand(CampaignMission m, SimResult r)
        {
            if (r.failure != null) return false;
            return MissionScore.TargetMet(m.TargetKind, m.ApoKm, m.PeriKm, m.C3Km2PerS2, MissionScore.FlownApoAltM(r), r.orbit.periAlt,
                MissionScore.C3(r.orbit.energy) / 1e6, false, out _);
        }

        /// <summary>
        /// The live report card's verdict on an intact or failed live flight: the same gates as PostFlightSource (Slice)
        /// builds for FlightDebrief.GoalMet. The parking-orbit departure Δv, C3, margin, red risks, reliability, airframe
        /// (gentle, payload, fairing, budget), coast boil-off and the predicted landing grades.
        /// </summary>
        public static bool Passes(Catalog catalog, CampaignMission row, Design design, SimResult result)
        {
            bool intact = result.failure == null && result.outcome != "failure" && result.outcome != "crash";
            double required = row.RequiredDeltaV();
            var extra = new GoalCheck
            {
                IdealDv = result.idealDeltaV,
                DepartureDv = result.departureDv,
                DepartureTW = result.departureTW,
                C3Km2PerS2 = MissionScore.C3(result.orbit.energy) / 1e6,
                Margin = required > 1 ? (result.idealDeltaV - required) / required : 0,
            };
            if (result.risks != null)
            {
                extra.RedKnown = true;
                for (int i = 0; i < result.risks.Count; i++)
                    if (result.risks[i].level == "red") extra.RedRisk = true;
            }
            extra.Reliability = Kestrel.Core.Economy.Reliability.Of(catalog, design).reliability;
            extra.Gentle = FailureMonitor.GentleTier(design);
            extra.PayloadMatch = design.payloadType == row.PayloadId && design.payloadMass == row.MassKg;
            extra.FairingFits = PayloadFit.Check(design, new PayloadSpec { id = row.PayloadId, volume = row.VolumeM3, diameter = row.DiameterM }).fits;
            var price = Kestrel.Core.Economy.CostModel.PriceDesign(catalog, design);
            long per = Kestrel.Core.Economy.CostModel.MinorPerKc(catalog);
            extra.UnderBudget = price.recurring <= Kestrel.Core.Economy.Career.KcToMinor(row.BudgetKc, per > 0 ? per : 1);
            extra.AirframeKnown = true;
            if (row.CoastHours > 0 && design.stages != null && design.stages.Count > 0)
            {
                var upper = design.stages[design.stages.Count - 1];
                extra.BoilOffKg = upper.propMass - TankModel.RemainingPropKg(catalog, upper, row.CoastHours);
            }
            string grade = row.LandingRequired || row.LandBoosters ? FlightBatch.PredictLandingGrade(catalog, design, result, row.LandingMode, false) : null;
            if (row.LandBoosters)
            {
                extra.BoosterKnown = true;
                extra.BoosterGrade = FlightBatch.PredictLandingGrade(catalog, design, result, row.LandingMode, true);
            }
            return FlightDebrief.GoalMet(row, intact, MissionScore.FlownApoAltM(result), result.orbit.periAlt, result.maxG, grade, extra);
        }

        public static string Text(SimResult r) => r.outcome + " " + (MissionScore.FlownApoAltM(r) / 1000).ToString("0.0") + " x "
            + (r.orbit.periAlt / 1000).ToString("0.0") + " km" + (r.failure != null ? " " + r.failure.kind : "");

        /// <summary>
        /// True when the live flight's only miss is KH-13's documented tight landing margin. The kh13-win
        /// starter flies with 28t of landing reserve; the #369 Judge honestly grades its fuel margin "hard"
        /// (&lt;20%), but the design wins — it reaches the 550km SSO and lands on Haven with the #409 guidance.
        /// A "hard" fuel margin is not a miss; "short" (cannot land) is. Used by the board-starter live-pad
        /// gates so m13's honest "hard" does not read as a failure.
        /// </summary>
        public static bool IsM13HardButWins(Catalog catalog, CampaignMission m, Design design, SimResult live)
        {
            if (m?.Id != "m13" || !m.LandingRequired) return false;
            string grade = FlightBatch.PredictLandingGrade(catalog, design, live, m.LandingMode, false);
            return grade == "hard" && InBand(m, live);
        }
    }
}
