// Play-through on default settings (Simple depth, auto-assist as the fresh design reads it,
// seed 1 as AscentFlight reads it, briefing wind and pad): the board starter flies the live pad loop
// (FlightDriver's frame clock, director, played checks), the trajectory rail is fed the way
// AscentHud feeds it, and the first stage runs through BoosterLanding (what RecoveryShow draws)
// from separation to touchdown, then the post-flight report.
// TestFlight t432: KH-3 never offered a landing because only a design with recovery.mode armed
// one. Every released first stage now gets its landing; an expendable one says why it is lost
// and never counts against the flight. KH-1 is a one-stage hop: no stage separates, so it goes
// straight to the report. KH-4 carries the expendable path (separation, booster-lost line, landing
// attempt, report) and KH-7 the planned Haven landing. KH-3 (Trevor's Tiny Orbit on t432) bent before
// MECO in the live loop before #313 (the director's auto-accepted pitch nudge and max-Q bucket), so its
// restored-strength test only pins "never a frozen screen". KH-3's board starter is now an 8.65° kick
// (calm-air retune); the 8° build it replaced (the t432 rocket) keeps its pinned live numbers. Since #338 (no
// wind-shear ease) the 8° build misses in the seed-1 wind as it does in calm air.
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
    public class FlightToLandingPlaythroughTests
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
            RecoveryLog.Clear();
        }

        [TearDown]
        public void Drop() => RecoveryLog.Clear();

        [Test]
        public void Kh4Starter_ExpendableBooster_SeparatesIntoALandingAttempt_ThenTheReport()
        {
            var run = Play(4);
            TestContext.WriteLine(run.Log());
            Assert.AreEqual(BoosterFate.Expendable, run.Landing.Fate, "KH-4's starter picked no landing");
            Assert.IsTrue(run.SeparatedAt > 0, "the core separated");

            // Landing prompt: the stage that let go is announced as lost, with the reason, at separation.
            Assert.IsTrue(run.Landing.Started, "the landing starts after separation");
            Assert.IsTrue(run.LostCall.Visible, "booster-lost line at separation");
            StringAssert.StartsWith(BoosterLanding.LostTitle, run.LostCall.Line);
            StringAssert.Contains("no landing fuel", run.LostCall.Line);
            Assert.IsTrue(TrajectoryWatch.Plain(run.LostCall.Line), run.LostCall.Line);

            // Landing attempt: flown to the surface at the fixed recovery steps, the rail lit live.
            Assert.IsTrue(run.Landing.Touched, "touchdown reached");
            Assert.IsTrue(run.RailRecoveryBeforeTouchdown, "the rail shows the Haven stop while the booster flies");
            Assert.IsTrue(run.LiveMarksBeforeTouchdown > 1, "phase marks reach the rail before touchdown");
            var verdict = RecoveryLog.Current;
            Assert.IsNotNull(verdict);
            Assert.IsTrue(verdict.Attempted);
            Assert.IsTrue(verdict.Expendable);
            Assert.AreEqual(run.Landing.LostWhy, verdict.LostWhy);
            Assert.IsFalse(RecoveryLog.Active, "the report no longer waits");
            Assert.That(run.Rail.Marks[run.Rail.Marks.Length - 1].Label, Is.EqualTo("Haven"));
            Assert.IsTrue(run.Rail.Marks[run.Rail.Marks.Length - 1].Passed, "the Haven tick lights at touchdown");

            // Report: an expendable booster's loss is never the flight's failure or a landing grade.
            Assert.IsNull(BoosterLanding.ReportGrade(null, verdict));
            Assert.IsNull(BoosterLanding.ReportFailure(null, verdict));
            Assert.IsNotNull(run.Card, "the post-flight card composes");
            if (run.Facts.Failure != null)
                StringAssert.DoesNotStartWith("recovery-", run.Facts.Failure.kind, "the ascent failure, not the booster");
            TestContext.WriteLine(run.Log());
        }

        [Test]
        public void Kh4Starter_LandingIsDeterministic()
        {
            var a = Play(4);
            RecoveryLog.Clear();
            var b = Play(4);
            Assert.AreEqual(a.SeparatedAt, b.SeparatedAt);
            Assert.AreEqual(a.Landing.Verdict.T, b.Landing.Verdict.T);
            Assert.AreEqual(a.Landing.Verdict.Sink, b.Landing.Verdict.Sink);
            Assert.AreEqual(a.Landing.Verdict.MissM, b.Landing.Verdict.MissM);
            Assert.AreEqual(a.Landing.Verdict.Cause, b.Landing.Verdict.Cause);
        }

        [Test]
        public void Kh3RestoredStarter_EitherLandsTheBooster_OrTheReportDoesNotWait()
        {
            // Trevor's Tiny Orbit (t432). Before #313 the live director bent this flight before MECO
            // (auto-accepted -1 deg nudge plus the 0.70 max-Q bucket); under #313 it separates. Either way the player is
            // never left on a frozen screen: a released booster flies to touchdown, or the report shows at once.
            // Pinned on the t432 rocket: the pre-retune 8° build with the old strength restore (1.9).
            var run = Play(3, Restored("m3"), 8.0);
            TestContext.WriteLine(run.Log());
            Assert.AreEqual(BoosterFate.Expendable, run.Landing.Fate);
            Assert.IsFalse(RecoveryLog.Active, "the report is never held");
            Assert.IsNotNull(run.Card);
            if (run.Landing.Started)
            {
                Assert.IsTrue(run.Landing.Touched, "a released booster reaches the surface");
                Assert.IsTrue(run.LostCall.Visible, "and is announced lost, with the reason");
            }
            else
                Assert.IsNotNull(run.AscentFailure, "no separation means the ascent ended first");
        }

        [Test]
        public void Kh3BoardStarter_SeparatesAndReachesOrbit_ThenTheExpendableBoosterLandsBeforeTheReport()
        {
            // #313 flipped this (was Kh3RawStarter_BreaksUpBeforeMeco_ReportDoesNotWaitForABooster). The board starter
            // (8.65° kick, strength 1.45, #343) in the seed-1 briefing wind with no wind-shear ease (#338): no breakup,
            // separation at T+154.3, orbit 200.4 x 199.6 km inside the 200 km band (203.3 x 196.7 km while the ease still
            // flew). Its expendable booster flies its landing attempt to the surface and the report waits for it, then
            // composes with the ascent's pass.
            var run = Play(3);
            TestContext.WriteLine(run.Log());
            Assert.IsNull(run.AscentFailure, "no breakup: " + run.Log());
            Assert.AreEqual("orbit", run.Result.outcome, run.Log());
            Assert.AreEqual(200.4, run.Result.orbit.apoAlt / 1000, 0.5, run.Log());
            Assert.AreEqual(199.6, run.Result.orbit.periAlt / 1000, 0.5, run.Log());
            Assert.AreEqual(BoosterFate.Expendable, run.Landing.Fate);
            Assert.That(run.SeparatedAt, Is.GreaterThan(0), "stage 1 separates");
            Assert.AreEqual(154.3, run.SeparatedAt, 0.5, run.Log());
            Assert.IsTrue(run.Landing.Started, "the released booster starts its landing attempt");
            Assert.IsTrue(run.Landing.Touched, "and reaches the surface");
            Assert.IsTrue(run.LostCall.Visible, "announced lost, with the reason");
            StringAssert.StartsWith(BoosterLanding.LostTitle, run.LostCall.Line);
            var verdict = RecoveryLog.Current;
            Assert.IsNotNull(verdict);
            Assert.IsTrue(verdict.Attempted);
            Assert.IsTrue(verdict.Expendable);
            Assert.IsFalse(RecoveryLog.Active, "the report no longer waits once it touched down");
            Assert.IsNull(BoosterLanding.ReportGrade(null, verdict));
            Assert.IsNull(BoosterLanding.ReportFailure(null, verdict));
            Assert.IsNotNull(run.Card);
            Assert.IsNull(run.Facts.Failure, "an expendable booster's loss is not the flight's failure");
            Assert.IsTrue(run.Facts.Passed, "the ascent lands the 200 km circle");
        }

        [Test]
        public void Kh3EightDegreeBuild_SeparatesAndMissesSuborbital_ThenTheExpendableBoosterLandsBeforeTheReport()
        {
            // The pre-retune 8° build (t432). While the wind-shear ease flew it reached 201.8 x 198.2 km in the seed-1 wind,
            // carried by the ease; in calm air it misses too flat (MissReasonTests, 72.3 km × 328 km). With no ease (#338)
            // the seed-1 wind flight misses the same way: separation at T+154.3, no breakup, suborbital 308.0 x 92.0 km.
            // Its expendable booster still flies its landing attempt and the report waits for it, then shows the miss.
            var run = Play(3, double.NaN, 8.0);
            TestContext.WriteLine(run.Log());
            Assert.IsNull(run.AscentFailure, "no breakup: " + run.Log());
            Assert.AreEqual("suborbital", run.Result.outcome, run.Log());
            Assert.AreEqual(308.0, run.Result.orbit.apoAlt / 1000, 0.5, run.Log());
            Assert.AreEqual(92.0, run.Result.orbit.periAlt / 1000, 0.5, run.Log());
            Assert.AreEqual(BoosterFate.Expendable, run.Landing.Fate);
            Assert.That(run.SeparatedAt, Is.GreaterThan(0), "stage 1 separates");
            Assert.AreEqual(154.3, run.SeparatedAt, 0.5, run.Log());
            Assert.IsTrue(run.Landing.Started, "the released booster starts its landing attempt");
            Assert.IsTrue(run.Landing.Touched, "and reaches the surface");
            Assert.IsTrue(run.LostCall.Visible, "announced lost, with the reason");
            StringAssert.StartsWith(BoosterLanding.LostTitle, run.LostCall.Line);
            var verdict = RecoveryLog.Current;
            Assert.IsNotNull(verdict);
            Assert.IsTrue(verdict.Attempted);
            Assert.IsTrue(verdict.Expendable);
            Assert.IsFalse(RecoveryLog.Active, "the report no longer waits once it touched down");
            Assert.IsNull(BoosterLanding.ReportGrade(null, verdict));
            Assert.IsNull(BoosterLanding.ReportFailure(null, verdict));
            Assert.IsNotNull(run.Card);
            Assert.IsNull(run.Facts.Failure, "an expendable booster's loss is not the flight's failure");
            Assert.IsFalse(run.Facts.Passed, "the ascent misses the 200 km circle");
        }

        [Test]
        public void Kh7Starter_PlannedHavenLanding_IsGradedInTheReport()
        {
            var run = Play(7);
            Assert.AreEqual(BoosterFate.Planned, run.Landing.Fate, "KH-7's starter picks Haven");
            Assert.IsTrue(run.Landing.Started && run.Landing.Touched, "separation to touchdown");
            Assert.IsFalse(run.LostCall.Visible && run.LostCall.Line.StartsWith(BoosterLanding.LostTitle), "a planned landing is not announced lost");
            var verdict = RecoveryLog.Current;
            Assert.IsNotNull(verdict);
            Assert.IsTrue(verdict.Attempted);
            Assert.IsFalse(verdict.Expendable);
            Assert.IsNotNull(verdict.GradeName, "a planned landing is graded");
            Assert.IsTrue(run.RailBurnBeforeTouchdown, "the boostback/entry ticks light while the booster flies");
            Assert.IsTrue(run.Rail.Marks[run.Rail.Marks.Length - 1].Passed, "the Haven tick lights at touchdown");
            if (run.AscentFailure == null)
                Assert.AreEqual(verdict.GradeName, run.Facts.LandingGrade, "the card quotes the flown grade");
            Assert.IsNotNull(run.Card);
            TestContext.WriteLine(run.Log());
        }

        [Test]
        public void Kh1Hop_HasNoBooster_AndGoesStraightToTheReport()
        {
            var run = Play(1);
            TestContext.WriteLine(run.Log());
            Assert.AreEqual(BoosterFate.None, run.Landing.Fate, "a one-stage hop has no stage to let go");
            Assert.IsFalse(RecoveryLog.Active, "the report never waits on a booster that does not exist");
            Assert.IsNull(run.AscentFailure, "the armed hopper flies clean");
            Assert.IsNotNull(run.Card, "the report composes as soon as the hop is over");
            Assert.AreEqual(run.Result.outcome, "suborbital");
            TestContext.WriteLine(run.Log());
        }

        [Test]
        public void EveryReleasedFirstStage_GetsALanding()
        {
            // KH-3..KH-20 starters: every design whose first stage leaves the stack arms a landing.
            var hangarTemplates = hangar.templates;
            for (int index = 1; index <= 20; index++)
            {
                var mission = campaign.Mission(index);
                var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangarTemplates, mission).Design);
                bool releases = BoosterLanding.Releases(catalog, design, out _);
                var landing = BoosterLanding.Arm(catalog, design);
                Assert.AreEqual(releases, landing.Armed, "KH-" + index);
                Assert.AreEqual(releases, RecoveryLog.Active, "KH-" + index + " report waits for touchdown");
                if (landing.Fate == BoosterFate.Expendable)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(landing.LostWhy), "KH-" + index + " says why");
                    Assert.IsTrue(landing.Craft.Recoverable, "KH-" + index + " flies a Haven attempt");
                }
                landing.Release();
            }
        }

        [Test]
        public void Kh7Starter_LiveFlight_EmitsOneFairingEvent_AtTheHeatGate()
        {
            // Graphics (#332): the flown sim emits a real "fairing" event. The live setup takes the release height
            // from the design (fairing.jettisonAltitudeKm), the number the staging show and the director use.
            // Physics (KH-7 follow-up): with the payload's heat limit the event waits for the heat gate (½ρv³ under the
            // limit for 2 s, from the fairing call's arming height up), the same gate the director runs on its samples.
            var run = Play(7);
            double expected = run.Design.fairingJettisonAltitudeKm * 1000;
            Assert.Greater(expected, 0);
            Assert.AreEqual(expected, run.FairingOptM, "MissionFlightSetup passes the design's release height");
            Assert.IsNotNull(run.Result);
            var fairings = run.Result.events.FindAll(e => e.type == "fairing");
            Assert.AreEqual(1, fairings.Count, "exactly one fairing event");
            var f = fairings[0];
            Assert.Greater(run.GateLimit, 0, "the KH-7 payload sets a heat limit");
            Assert.That(f.alt.Value, Is.GreaterThanOrEqualTo(FairingHeatGate.FloorFor(expected)), "not before the fairing call arms");
            Assert.AreEqual(run.GateOpenT, f.t, 1e-9, "on the step the director's heat gate opens");
            Assert.Greater(run.Result.maxQTime, 0);
            Assert.Greater(f.t, run.Result.maxQTime, "after max-Q");
            Assert.IsTrue(Array.Exists(run.Rail.Marks, m => m.Stop == TrajectoryStop.Fairing && m.Passed), "the rail's Fairing tick is lit");
            Assert.IsTrue(run.Landing.Touched, "the stage and recovery chain still runs to touchdown");
            TestContext.WriteLine("fairing T+" + f.t.ToString("0.00") + " alt " + f.alt.Value.ToString("0") + " m (design release " + expected + " m)");
        }

        [Test]
        public void FairingReleaseHeight_ComesFromTheDesign_AndABareStackEmitsNone()
        {
            var bare = new Design { fairingDiameter = 0, fairingLength = 0, fairingJettisonAltitudeKm = 110 };
            Assert.AreEqual(0, MissionFlightSetup.FairingJettisonAltM(bare), "no fairing, no event");
            var low = new Design { fairingDiameter = 3, fairingLength = 8, fairingJettisonAltitudeKm = 95 };
            Assert.AreEqual(95000, MissionFlightSetup.FairingJettisonAltM(low));
            var unset = new Design { fairingDiameter = 3, fairingLength = 8, fairingJettisonAltitudeKm = 0 };
            Assert.AreEqual(MissionFlightSetup.DefaultFairingJettisonAltM, MissionFlightSetup.FairingJettisonAltM(unset));
            Assert.AreEqual(110000, MissionFlightSetup.DefaultFairingJettisonAltM, "Design.cs default jettisonAltitudeKm = 110");
        }

        [Test]
        public void ReportPicksAscentFirst_ThenAGradedLanding_NeverAnExpendableLoss()
        {
            var ascent = new FailureEvent { kind = "overg" };
            var graded = new RecoveryVerdict { Attempted = true, GradeName = "Hard", Failure = new FailureEvent { kind = RecoveryScore.Fast } };
            var lost = new RecoveryVerdict { Attempted = true, Expendable = true, GradeName = "Water", Failure = new FailureEvent { kind = RecoveryScore.Fuel } };
            Assert.AreSame(ascent, BoosterLanding.ReportFailure(ascent, graded));
            Assert.AreSame(graded.Failure, BoosterLanding.ReportFailure(null, graded));
            Assert.IsNull(BoosterLanding.ReportFailure(null, lost));
            Assert.AreEqual("Hard", BoosterLanding.ReportGrade(null, graded));
            Assert.IsNull(BoosterLanding.ReportGrade(null, lost));
            Assert.IsNull(BoosterLanding.ReportGrade(ascent, graded));
        }

        // ---- the live loop, headless ----

        sealed class Run
        {
            public BoosterLanding Landing;
            public Design Design;
            public double FairingOptM, GateLimit, GateOpenT = double.NaN;
            public double SeparatedAt = -1, AscentEndT, TouchdownT;
            public RecoveryCallout LostCall;
            public bool RailRecoveryBeforeTouchdown, RailBurnBeforeTouchdown;
            public int LiveMarksBeforeTouchdown;
            public TrajectoryPlan Rail;
            public FailureEvent AscentFailure;
            public SimResult Result;
            public FlightFacts Facts;
            public FlightDebrief Card;
            public string Mission;
            public readonly List<string> Lines = new List<string>();

            public string Log()
            {
                var marks = new List<string>();
                foreach (var m in Rail.Marks) marks.Add(m.Label + (m.Passed ? "+" : ""));
                var v = RecoveryLog.Current;
                return Mission + " fate " + Landing.Fate + " sep T+" + SeparatedAt.ToString("0.0")
                    + " ascent end T+" + AscentEndT.ToString("0.0") + " " + (AscentFailure != null ? AscentFailure.kind : Result?.outcome)
                    + (v != null ? " | touchdown +" + v.T.ToString("0.0") + " s sink " + v.Sink.ToString("0.0") + " miss " + v.MissM.ToString("0") + " cause " + (v.Cause ?? "none") + " grade " + v.GradeName : "")
                    + " | rail " + Rail.Progress.ToString("0.00") + " [" + string.Join(", ", marks) + "]"
                    + " | apo " + (Facts != null ? Facts.ApoKm.ToString("0.00") : "?") + " km peakG " + (Facts != null ? Facts.PeakG.ToString("0.00") : "?")
                    + (AscentFailure != null ? " | failure " + AscentFailure.kind + "/" + AscentFailure.cause + ": " + AscentFailure.kid : "")
                    + " | card passed " + (Card != null && Card.Passed) + "\n" + string.Join("\n", Lines);
            }
        }

        double Restored(string missionId)
        {
            var file = Kestrel.Core.Json.MiniJson.ParseObject(System.IO.File.ReadAllText(System.IO.Path.Combine(Golden.Root, "data", "board-starters.json")));
            foreach (Kestrel.Core.Json.JsonObject row in file.Arr("missions"))
                if (row.Str("id") == missionId) return row.Num("restoreSafetyFactor");
            Assert.Fail(missionId + " has no strength restore");
            return double.NaN;
        }

        Run Play(int index, double safetyFactor = double.NaN, double pitchKickDeg = double.NaN)
        {
            const double frame = 1.0 / 60.0;
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            if (!double.IsNaN(safetyFactor)) design.safetyFactor = safetyFactor;
            if (!double.IsNaN(pitchKickDeg)) design.guidance.pitchKickDeg = pitchKickDeg;
            ulong hash = FlightBatch.DesignHash(design);
            ulong seed = 1; // AscentFlight.ReadSeed on a design with no seed
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            bool auto = StepperProgress.Read(choice.Design).AutoAssistOn(DesignerDepth.Simple);

            // AscentFlight.Prepare
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            FailureMonitor monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            // #313: the shared director (AscentFlight.Prepare builds it the same way).
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, auto, false, false, card?.Bonuses);
            var vehicle = AscentDecisions.FromDesign(design, catalog, monitor?.Events, card?.Wind, qLim, target, FailureMonitor.GentleTier(design), card?.Bonuses, hash, seed);

            // FlightDriver.Launch
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var guidance = opts.guidance;
            var sim = new AscentSim(setup.Stages, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            // FlightDriver.Update under #313: DirectedFlight.Step applies the director's command before and after each tick.
            var directed = new DirectedFlight(sim, director, opts);
            double omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;

            // SliceApp.PresentAscent: RecoveryShow.Arm
            var run = new Run { Mission = "KH-" + index, Design = design, FairingOptM = opts.fairingJettisonAltM, GateLimit = opts.fairingMaxHeatFlux };
            run.Landing = BoosterLanding.Arm(catalog, design);

            // AscentHud.FeedTrajectory
            var rail = new TrajectoryWatch();
            rail.HasFairing = vehicle.HasFairing;
            rail.Stages = vehicle.Stages > 0 ? vehicle.Stages : 1;
            if (vehicle.JettisonAltM > 0) rail.FairingAltM = vehicle.JettisonAltM;
            int fed = 0;

            double clock = 0, prevDown = 0, prevAlt = 0, attitude = 90;
            bool hasAttitude = false, running = true;
            int frames = 0;
            double down = 0, alt = 0, speed = 0;
            while (frames++ < 200000)
            {
                if (running)
                {
                    clock += frame;
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
                        run.GateOpenT = director.FairingGate.OpenT;
                        run.AscentFailure = run.Result.failure;
                        run.AscentEndT = sim.T;
                    }
                    // FlightDriver.UpdateFrame
                    double x = sim.Y[0], y = sim.Y[1], r = Math.Sqrt(x * x + y * y);
                    double padAngle = Math.PI / 2 + omegaAir * sim.T;
                    down = Gravity.R_EARTH * (Math.Atan2(y, x) - padAngle);
                    alt = r - Gravity.R_EARTH;
                    double vx = sim.Y[2] + omegaAir * y, vy = sim.Y[3] - omegaAir * x;
                    speed = Math.Sqrt(vx * vx + vy * vy);
                    double dx = down - prevDown, dy = alt - prevAlt;
                    if (dx * dx + dy * dy > 1e-6 && alt > 20)
                    {
                        attitude = Math.Atan2(dy, dx) * 180 / Math.PI;
                        hasAttitude = true;
                    }
                    prevDown = down; prevAlt = alt;
                }

                // RecoveryShow.Present
                bool lost = run.Result != null && run.Result.failure != null;
                bool over = lost || (run.Result != null && !running);
                if (run.Landing.Armed && !run.Landing.Started)
                {
                    if (run.Landing.Watch(sim.T, sim.StageIdx, down, alt, speed, attitude, hasAttitude, over))
                    {
                        run.SeparatedAt = sim.T;
                        run.LostCall = run.Landing.Callout(run.Landing.Sample());
                        run.Lines.Add("separation T+" + sim.T.ToString("0.0") + " alt " + (alt / 1000).ToString("0.0") + " km speed " + speed.ToString("0") + " m/s pitch " + attitude.ToString("0.0"));
                    }
                }
                else if (run.Landing.Started && !run.Landing.Touched)
                {
                    var s = run.Landing.Step(frame, RecoveryStick.Assisted);
                    if (!run.Landing.Touched)
                    {
                        if (RecoveryLog.Current == null && RecoveryLog.Marks.Length > run.LiveMarksBeforeTouchdown)
                            run.LiveMarksBeforeTouchdown = RecoveryLog.Marks.Length;
                    }
                    else run.Lines.Add("touchdown at recovery T+" + s.T.ToString("0.0") + " phase " + s.Phase);
                }

                // AscentHud.FeedTrajectory
                rail.Recovery = RecoveryLog.Active || (RecoveryLog.Current != null && RecoveryLog.Current.Attempted);
                while (fed < sim.Events.Count)
                {
                    var ev = sim.Events[fed++];
                    rail.Seen(new TrajectoryEvent { T = ev.t, Type = ev.type, Stage = ev.stage ?? 0, AltM = ev.alt ?? alt, SpeedMps = ev.v ?? speed });
                }
                rail.Sample(sim.T, alt, speed, sim.LastQ);
                var recMarks = RecoveryLog.FlightMarks;
                for (int i = 0; i < recMarks.Length; i++)
                    rail.RecoveryPhase(Word(recMarks[i].Phase), recMarks[i].T, recMarks[i].Alt, recMarks[i].Speed);
                if (run.Landing.Started && !run.Landing.Touched)
                {
                    var plan = rail.At(RecoveryLog.RailClock(sim.T), alt, speed);
                    foreach (var m in plan.Marks)
                    {
                        if (m.Label == "Haven") run.RailRecoveryBeforeTouchdown = true;
                        if ((m.Stop == TrajectoryStop.Entry || m.Stop == TrajectoryStop.Boostback) && m.Passed) run.RailBurnBeforeTouchdown = true;
                    }
                }

                // FailureHost: the card shows when the ascent is over and no booster is still in the air.
                if (!running && (lost || !RecoveryLog.Active) && (!run.Landing.Started || run.Landing.Touched || lost))
                {
                    if (run.Landing.Started && !run.Landing.Touched) continue; // a lost ascent: the booster still finishes for this test
                    break;
                }
            }
            Assert.Less(frames, 200000, "the flight and the landing finish");
            run.Rail = rail.At(RecoveryLog.RailClock(sim.T), alt, speed);
            if (run.Landing.Started && run.Landing.Verdict != null) run.TouchdownT = run.Landing.Verdict.T;

            // PostFlightSource.Facts, then the card.
            var rec = RecoveryLog.Current;
            var facts = new FlightFacts
            {
                Failure = BoosterLanding.ReportFailure(run.AscentFailure, rec),
                LandingGrade = BoosterLanding.ReportGrade(run.AscentFailure, rec),
                Seed = seed,
                DesignId = design.name,
                BudgetKc = mission.BudgetKc,
                GoalKm = mission.ApoKm,
                LandingRequired = mission.LandingRequired || mission.LandBoosters,
                GLimit = double.IsNaN(mission.MaxG) ? 0 : mission.MaxG,
            };
            var result = run.Result;
            facts.PeakG = result.maxG;
            double apo = MissionScore.FlownApoAltM(result);
            facts.ApoKm = apo / 1000.0;
            bool intact = run.AscentFailure == null && result.outcome != "failure" && result.outcome != "crash";
            if (string.IsNullOrEmpty(facts.LandingGrade) && facts.LandingRequired)
                facts.LandingGrade = FlightBatch.PredictLandingGrade(catalog, design, result, mission.LandingMode, false);
            facts.Passed = FlightDebrief.GoalMet(mission, intact, apo, result.orbit.periAlt, result.maxG, facts.LandingGrade);
            run.Facts = facts;
            var ctx = MissionContext.FromMission(campaign, mission, campaign.Seeds[0]);
            run.Card = FlightDebrief.Compose(null, ctx, facts, NarrationPool.Empty, 100000, hangar);
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

    }
}
