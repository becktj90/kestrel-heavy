// t466 (Arcade, build 204, KH-3): after SECO the upper stage was in orbit, but the flight never ended. The report
// waited for the booster (FailureHost holds while RecoveryLog.Active) and the rail's marker waited under Boostback for
// an expendable booster with no landing fuel that never boosts back: KH-3's board starter reached orbit at T+517.0 and
// its booster touched down at recovery T+536.8 (flight T+691.1), 174 s of dead air. Now, once the ascent has ended
// without a failure, the booster finishes its landing at once on Auto-assist (BoosterLanding.Conclude, same fixed
// steps, same verdict) and SECO / Orbit is the rail's final stop (TrajectoryWatch.AscentOver).
// Time-sliced like the phone: FlightDriver's 1/60 s frames, the director, the played checks, RecoveryShow.Present
// (Conclude, then Step) before AscentHud.Present (FeedTrajectory), and FailureHost's report gate.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class RailInsertionEndStopTests
    {
        /// <summary>Wall-clock seconds from the end of the ascent to the report and the rail's final stop.</summary>
        public const double ReportWithinS = 2.0;
        const double Frame = 1.0 / 60.0;

        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            RecoveryLog.Clear();
        }

        [TearDown]
        public void Drop() => RecoveryLog.Clear();

        public static IEnumerable<int> Starters()
        {
            for (int i = 1; i <= 20; i++) yield return i;
        }

        sealed class Run
        {
            public string Mission;
            public SimResult Result;
            public BoosterLanding Landing;
            public double AscentEndClock = double.NaN, ReportClock = double.NaN, RailDoneClock = double.NaN;
            public TrajectoryPlan Rail;
            public RecoveryVerdict Verdict;

            public string Log()
            {
                var marks = new List<string>();
                if (Rail != null) foreach (var m in Rail.Marks) marks.Add(m.Label + (m.Passed ? "+" : m.Upcoming ? "(next)" : ""));
                return Mission + " " + (Result != null ? Result.outcome + (Result.failure != null ? " failure " + Result.failure.kind : "") : "?")
                    + " fate " + (Landing != null ? Landing.Fate.ToString() : "?")
                    + " ascent end " + AscentEndClock.ToString("0.00") + " report " + ReportClock.ToString("0.00")
                    + " rail done " + RailDoneClock.ToString("0.00")
                    + " | rail " + (Rail != null ? Rail.Progress.ToString("0.000") : "?") + " [" + string.Join(", ", marks) + "]";
            }
        }

        /// <param name="conclude">RecoveryShow's call at the end of the ascent. False flies the booster out live (main before t466).</param>
        Run Play(int index, bool conclude, double pitchKickDeg = double.NaN)
        {
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            if (!double.IsNaN(pitchKickDeg)) design.guidance.pitchKickDeg = pitchKickDeg;
            ulong hash = FlightBatch.DesignHash(design);
            const ulong seed = 1;
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            bool auto = StepperProgress.Read(choice.Design).AutoAssistOn(DesignerDepth.Simple);

            // AscentFlight.Prepare
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, auto, false, false, card?.Bonuses);
            var vehicle = AscentDecisions.FromDesign(design, catalog, monitor?.Events, card?.Wind, qLim, target, FailureMonitor.GentleTier(design), card?.Bonuses, hash, seed);

            // FlightDriver.Launch
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            var directed = new DirectedFlight(sim, director, opts);
            double omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;

            // RecoveryShow.Arm
            var run = new Run { Mission = "KH-" + index };
            run.Landing = BoosterLanding.Arm(catalog, design);

            // AscentHud.FeedTrajectory
            var rail = new TrajectoryWatch();
            rail.HasFairing = vehicle.HasFairing;
            rail.Stages = vehicle.Stages > 0 ? vehicle.Stages : 1;
            rail.StrapOns = vehicle.StrapOns > 0;
            rail.RecoveryMode = string.IsNullOrEmpty(vehicle.RecoveryMode) ? null : vehicle.RecoveryMode;
            if (vehicle.JettisonAltM > 0) rail.FairingAltM = vehicle.JettisonAltM;
            int fed = 0;

            double clock = 0, wall = 0, prevDown = 0, prevAlt = 0, attitude = 90, down = 0, alt = 0, speed = 0;
            bool hasAttitude = false, running = true;
            for (int frames = 0; frames < 400000; frames++)
            {
                wall += Frame;
                // FlightDriver.Update
                if (running)
                {
                    clock += Frame;
                    int guard = 0;
                    while (sim.T < clock && !sim.Done && guard++ < 64)
                    {
                        int before = sim.Events.Count;
                        directed.Step();
                        checks.NoteEvents(sim, before);
                        if (checks.StopForBreakup(sim)) break;
                    }
                    if (sim.Done)
                    {
                        running = false;
                        run.Result = directed.Finish();
                        run.AscentEndClock = wall;
                    }
                    double x = sim.Y[0], y = sim.Y[1], r = Math.Sqrt(x * x + y * y);
                    down = Gravity.R_EARTH * (Math.Atan2(y, x) - (Math.PI / 2 + omegaAir * sim.T));
                    alt = r - Gravity.R_EARTH;
                    double vx = sim.Y[2] + omegaAir * y, vy = sim.Y[3] - omegaAir * x;
                    speed = Math.Sqrt(vx * vx + vy * vy);
                    double dx = down - prevDown, dy = alt - prevAlt;
                    if (dx * dx + dy * dy > 1e-6 && alt > 20) { attitude = Math.Atan2(dy, dx) * 180 / Math.PI; hasAttitude = true; }
                    prevDown = down; prevAlt = alt;
                }
                bool lost = run.Result != null && run.Result.failure != null;
                bool over = run.Result != null && !running;

                // RecoveryShow.Present
                if (run.Landing.Armed && !run.Landing.Started)
                    run.Landing.Watch(sim.T, sim.StageIdx, down, alt, speed, attitude, hasAttitude, over || lost);
                else if (run.Landing.Started)
                {
                    if (conclude) run.Landing.Conclude(over, lost);
                    run.Landing.Step(Frame, RecoveryStick.Assisted);
                }

                // AscentHud.FeedTrajectory
                rail.Recovery = RecoveryLog.Active || (RecoveryLog.Current != null && RecoveryLog.Current.Attempted);
                while (fed < sim.Events.Count)
                {
                    var ev = sim.Events[fed++];
                    rail.Seen(new TrajectoryEvent { T = ev.t, Type = ev.type, Stage = ev.stage ?? 0, AltM = ev.alt ?? alt, SpeedMps = ev.v ?? speed });
                }
                rail.Burn(sim.StageIdx + 1, sim.PropellantFraction);
                rail.Sample(sim.T, alt, speed, sim.LastQ);
                if (conclude && over && !lost) rail.AscentOver(sim.T);
                var marks = RecoveryLog.FlightMarks;
                for (int i = 0; i < marks.Length; i++)
                    rail.RecoveryPhase(Word(marks[i].Phase), marks[i].T, marks[i].Alt, marks[i].Speed);
                var plan = rail.At(RecoveryLog.RailClock(sim.T), alt, speed);
                if (over && double.IsNaN(run.RailDoneClock) && plan.Upcoming < 0 && plan.Progress >= 1f) run.RailDoneClock = wall;

                // FailureHost.Update: a failure shows at once; otherwise the card waits for the ascent and any booster.
                if (over && (lost || !RecoveryLog.Active))
                {
                    run.ReportClock = wall;
                    run.Rail = plan;
                    break;
                }
            }
            run.Verdict = RecoveryLog.Current;
            return run;
        }

        static string Word(RecoveryPhase phase)
        {
            switch (phase)
            {
                case RecoveryPhase.Boostback: return "boostback";
                case RecoveryPhase.Entry: return "entry";
                case RecoveryPhase.Landing:
                case RecoveryPhase.Down: return "landing";
                default: return "";
            }
        }

        [TestCaseSource(nameof(Starters))]
        public void EveryStarter_RailReachesItsFinalStop_AndTheReportOpens_WithinNSecondsOfTheAscentEnding(int index)
        {
            var run = Play(index, true);
            TestContext.WriteLine(run.Log());
            Assert.IsNotNull(run.Result, "the ascent ends / " + run.Log());
            Assert.IsFalse(double.IsNaN(run.ReportClock), "the report opens / " + run.Log());
            Assert.LessOrEqual(run.ReportClock - run.AscentEndClock, ReportWithinS, "report after the ascent ended / " + run.Log());
            Assert.IsFalse(RecoveryLog.Active, "no booster holds the report / " + run.Log());
            if (run.Landing.Started) Assert.IsTrue(run.Landing.Touched, "the released booster's result joins the report / " + run.Log());
            if (run.Result.failure != null) return; // the failure card shows at once; the rail is not the end of that flight
            Assert.IsFalse(double.IsNaN(run.RailDoneClock), "the rail reaches its final stop / " + run.Log());
            Assert.LessOrEqual(run.RailDoneClock - run.AscentEndClock, ReportWithinS, "rail final stop after the ascent ended / " + run.Log());
            Assert.AreEqual(-1, run.Rail.Upcoming, run.Log());
            Assert.AreEqual(1f, run.Rail.Progress, run.Log());
            foreach (var m in run.Rail.Marks) Assert.IsTrue(m.Passed, m.Label + " is flown / " + run.Log());
            int seco = Array.FindIndex(run.Rail.Marks, m => m.Stop == TrajectoryStop.Seco);
            if (seco >= 0 && run.Result.outcome == "orbit")
                Assert.AreEqual("Orbit", run.Rail.Marks[seco].Label, "insertion is the Orbit stop / " + run.Log());
        }

        [Test]
        public void Kh3BoardStarter_OrbitThenReport_NoLongerWaitsMinutesForTheExpendableBooster()
        {
            // Main before t466: orbit at T+517.0, then ~174 s with the marker under Boostback until the booster touched down.
            var before = Play(3, false);
            TestContext.WriteLine("before: " + before.Log());
            Assert.AreEqual("orbit", before.Result.outcome, before.Log());
            Assert.AreEqual(BoosterFate.Expendable, before.Landing.Fate);
            Assert.Greater(before.ReportClock - before.AscentEndClock, 60, "the old wait this pins / " + before.Log());
            RecoveryLog.Clear();

            var after = Play(3, true);
            TestContext.WriteLine("after: " + after.Log());
            Assert.AreEqual("orbit", after.Result.outcome, after.Log());
            Assert.LessOrEqual(after.ReportClock - after.AscentEndClock, ReportWithinS, after.Log());
            Assert.AreEqual("Orbit", Array.Find(after.Rail.Marks, m => m.Stop == TrajectoryStop.Seco).Label, after.Log());
            Assert.AreEqual(-1, after.Rail.Upcoming, after.Log());
            Assert.IsNull(Array.Find(after.Rail.Marks, m => m.Stop == TrajectoryStop.Boostback).Label,
                "no Boostback stop for a booster that never boosted back / " + after.Log());
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(12)]
        public void Concluded_BoosterVerdict_IsBitIdentical_ToFlyingItOutLive(int index)
        {
            // Physics (one plant, same steps): finishing the landing at insertion runs the same fixed recovery steps on
            // Auto-assist, so the published verdict equals the one the live frames reach, bit for bit. These starters'
            // boosters were still in the air at insertion on main (seed 1, live frames): KH-3 174 s, KH-4 331 s and KH-12
            // 360 s (expendable). KH-13 (277 s, a planned Haven landing) left this list with t-booster-land: its rebuilt
            // starter separates early and lands on Haven well before insertion.
            var live = Play(index, false);
            var liveV = live.Verdict;
            RecoveryLog.Clear();
            var settled = Play(index, true);
            var setV = settled.Verdict;
            TestContext.WriteLine("live: " + live.Log() + "\nsettled: " + settled.Log());
            Assert.IsTrue(live.Landing.Started, live.Log());
            Assert.Greater(live.ReportClock - live.AscentEndClock, 1.0, "the booster was still flying at insertion, so it settled / " + live.Log());
            Assert.IsNotNull(liveV, live.Log());
            Assert.IsNotNull(setV, settled.Log());
            Assert.AreEqual(liveV.T, setV.T, 0.0, "T");
            Assert.AreEqual(liveV.Sink, setV.Sink, 0.0, "sink");
            Assert.AreEqual(liveV.TiltDeg, setV.TiltDeg, 0.0, "tilt");
            Assert.AreEqual(liveV.MissM, setV.MissM, 0.0, "miss");
            Assert.AreEqual(liveV.PropLeft, setV.PropLeft, 0.0, "prop left");
            Assert.AreEqual(liveV.Cause, setV.Cause, "cause");
            Assert.AreEqual(liveV.GradeName, setV.GradeName, "grade");
            Assert.AreEqual(liveV.Medal, setV.Medal, "medal");
            Assert.AreEqual(liveV.Expendable, setV.Expendable, "expendable");
            Assert.AreEqual(liveV.Failure == null, setV.Failure == null, "failure");
            Assert.AreEqual(liveV.Marks.Length, setV.Marks.Length, "marks");
            for (int i = 0; i < liveV.Marks.Length; i++)
            {
                Assert.AreEqual(liveV.Marks[i].Phase, setV.Marks[i].Phase, "mark " + i);
                Assert.AreEqual(liveV.Marks[i].T, setV.Marks[i].T, 0.0, "mark " + i + " T");
            }
        }

        [Test]
        public void Rail_AfterInsertion_EndsOnOrbit_InsteadOfWaitingUnderBoostback()
        {
            // The rail alone, KH-3's shape: two stages, a fairing, a released expendable booster (no planned mode).
            TrajectoryWatch Fly()
            {
                var w = new TrajectoryWatch { HasFairing = true, Stages = 2, Recovery = true, RecoveryMode = null };
                w.Seen(new TrajectoryEvent { T = 0.6, Type = "liftoff" });
                w.Seen(new TrajectoryEvent { T = 70, Type = "maxq", AltM = 12000, SpeedMps = 500 });
                w.Seen(new TrajectoryEvent { T = 154.3, Type = "burnout", Stage = 1, AltM = 89400, SpeedMps = 4428 });
                w.Seen(new TrajectoryEvent { T = 170.4, Type = "fairing", AltM = 110000, SpeedMps = 4500 });
                w.Seen(new TrajectoryEvent { T = 517.0, Type = "cutoff", Stage = 2, AltM = 199600, SpeedMps = 7363 });
                w.Seen(new TrajectoryEvent { T = 517.0, Type = "orbit", AltM = 199600, SpeedMps = 7363 });
                return w;
            }
            var held = Fly().At(530, 199600, 7363);
            Assert.AreEqual(TrajectoryStop.Boostback, held.Marks[held.Upcoming].Stop, "main before t466: waits for the booster");

            var w2 = Fly();
            w2.AscentOver(517.0);
            var plan = w2.At(517.0, 199600, 7363);
            Assert.AreEqual(-1, plan.Upcoming);
            Assert.AreEqual(1f, plan.Progress);
            var last = plan.Marks[plan.Marks.Length - 1];
            Assert.AreEqual(TrajectoryStop.Seco, last.Stop);
            Assert.AreEqual("Orbit", last.Label);
            Assert.IsTrue(last.Passed);

            // A booster stop already flown keeps its place; one not flown by insertion is dropped.
            var w3 = Fly();
            w3.RecoveryPhase("boostback", 160, 95000, 4300);
            w3.AscentOver(517.0);
            var kept = w3.At(517.0, 199600, 7363);
            Assert.AreEqual(-1, kept.Upcoming);
            Assert.IsTrue(Array.Exists(kept.Marks, m => m.Stop == TrajectoryStop.Boostback && m.Passed));
            Assert.IsFalse(Array.Exists(kept.Marks, m => m.Stop == TrajectoryStop.Entry || m.Stop == TrajectoryStop.Landing));

            // Before the ascent is over nothing is dropped.
            var w4 = Fly();
            w4.AscentOver(600);
            Assert.AreEqual(TrajectoryStop.Boostback, w4.At(530, 199600, 7363).Marks[w4.At(530, 199600, 7363).Upcoming].Stop);
        }
    }
}
