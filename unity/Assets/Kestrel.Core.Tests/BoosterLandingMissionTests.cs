// t-booster-land (owner, iPhone: "I still haven't been able to land the booster"). Every landing mission's board
// starter, flown the way the live pad flies it (FlightDriver's frame clock and ground-frame hand-over, the shared
// director on Auto-assist, the briefing wind and site for the seed, BoosterLanding from separation to touchdown on
// the assisted stick), reaches its target AND lands its first stage Good or Perfect. Before the fix only KH-7 landed:
// the recovery law was tuned on inertial burnout states and crashed every steep ground-frame hand-over at
// 470-700 m/s with 30-85 t of landing fuel still aboard, while FlightBatch's Δv formula graded the same flights
// good/perfect. FlightBatch now flies the same landing (BoosterLanding.FlyPlanned), so the judge agrees.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class BoosterLandingMissionTests
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

        // KH-6 RTLS, KH-7/13/15 Haven, KH-10/19 RTLS with strap-ons. Seed 1 is the live default; 2 and 5 fly other
        // briefing winds (seed 2 lofts KH-13's hand-over to 77°).
        [TestCase(6, 1UL)] [TestCase(6, 2UL)] [TestCase(6, 5UL)]
        [TestCase(7, 1UL)] [TestCase(7, 2UL)] [TestCase(7, 5UL)]
        [TestCase(10, 1UL)] [TestCase(10, 2UL)] [TestCase(10, 5UL)]
        [TestCase(13, 1UL)] [TestCase(13, 2UL)] [TestCase(13, 5UL)]
        [TestCase(15, 1UL)] [TestCase(15, 2UL)] [TestCase(15, 5UL)]
        [TestCase(19, 1UL)] [TestCase(19, 2UL)] [TestCase(19, 5UL)]
        public void LandingStarter_LiveFlight_ReachesTheTarget_AndLandsTheBooster(int index, ulong seed)
        {
            var run = Play(index, seed);
            string log = run.Log();
            TestContext.WriteLine(log);
            var mission = campaign.Mission(index);
            Assert.IsTrue(mission.LandingRequired, log);
            Assert.IsNull(run.Result.failure, log);
            Assert.IsTrue(run.Reached, "the ascent reaches the mission target: " + log);
            Assert.AreEqual(BoosterFate.Planned, run.Landing.Fate, log);
            Assert.IsTrue(run.Landing.Started && run.Landing.Touched, "separation to touchdown: " + log);

            // The reserve is held out of the ascent: the stage separates with its whole landing fuel.
            Assert.AreEqual(run.Design.stages[0].landingPropMass, run.Landing.Craft.ReserveKg, 1e-6, log);

            var v = run.Landing.Verdict;
            Assert.IsNull(v.Cause, log);
            Assert.IsTrue(MissionScore.IsGood(v.GradeName), log);
            Assert.LessOrEqual(Math.Abs(v.MissM), RecoverySim.DeckRadiusM, log);
            Assert.Greater(v.PropLeft, 0, log);
            Assert.IsTrue(MissionScore.IsGood(BoosterLanding.ReportGrade(null, v)), "the card quotes a passing grade");

            // The rail's recovery stops are flown: an entry burn always, and a boostback on a pad return.
            Assert.IsTrue(Flew(v, RecoveryPhase.Entry), "entry burn: " + log);
            if (mission.LandingMode == "rtls") Assert.IsTrue(Flew(v, RecoveryPhase.Boostback), "boostback: " + log);
            Assert.IsTrue(Flew(v, RecoveryPhase.Landing), "landing burn: " + log);
        }

        [TestCase(6)] [TestCase(7)] [TestCase(10)] [TestCase(13)] [TestCase(15)] [TestCase(19)]
        public void LandingStarter_Batch_FliesTheSameLanding_AndPasses(int index)
        {
            // FlightBatch (the judge) flies the booster out with BoosterLanding.FlyPlanned from the ground-frame hand-over.
            var mission = campaign.Mission(index);
            var json = HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design;
            var flight = FlightBatch.Fly(catalog, mission, Design.FromJson(json), campaign.Seeds[0]);
            string why = "KH-" + index + " " + flight.outcome + "/" + flight.failure + " land=" + flight.landingGrade + " cause=" + flight.landingCause
                + " miss=" + flight.landingMissM.ToString("0") + " sink=" + flight.landingSink.ToString("0.0") + " sep=" + flight.separationSpeedMps.ToString("0")
                + " m/s @ " + flight.separationAltKm.ToString("0.0") + " km booster=" + flight.boosterGrade;
            TestContext.WriteLine(why);
            Assert.IsTrue(flight.passed, why);
            Assert.IsTrue(MissionScore.IsGood(flight.landingGrade), why);
            Assert.IsNull(flight.landingCause, why);
            Assert.That(flight.separationSpeedMps, Is.GreaterThan(500).And.LessThan(2500), why);
        }

        [Test]
        public void ExpendableBooster_Batch_IsShort_AndTheRailPlansNoBoostback()
        {
            // An expendable KH-7 build: no landing is graded, so a required landing fails in the batch as it does live.
            var mission = campaign.Mission(7);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            design.recoveryMode = "expendable";
            var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
            Assert.AreEqual("short", flight.landingGrade);
            Assert.IsFalse(flight.passed);
            // The released stage still flies its Haven attempt (TryAnyway: barge), so the rail plans Entry and Haven only.
            var vehicle = AscentDecisions.FromDesign(design, catalog, null, null, 0, 200000, false, null, 1, 1);
            Assert.AreEqual("barge", vehicle.RecoveryMode);
            var rail = new TrajectoryWatch { RecoveryMode = vehicle.RecoveryMode };
            Assert.IsFalse(rail.FliesBoostback());
        }

        [Test]
        public void LeglessBooster_FlownTouchdownDoesNotCount()
        {
            // Review (PR #367): RecoveryScore.Judge reads a legless craft as legs-down, so a soft legless touchdown graded Good.
            // The mission needs legs before the flown grade counts, in the batch and on the live card.
            var mission = campaign.Mission(7);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            design.legs = false;
            var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
            Assert.AreEqual("short", flight.landingGrade, "flown " + flight.landingCause + " sink " + flight.landingSink.ToString("0.0"));
            Assert.IsFalse(flight.passed);
            Assert.AreEqual("short", BoosterLanding.MissionGrade(mission, design, "perfect"));
            design.legs = true;
            Assert.AreEqual("perfect", BoosterLanding.MissionGrade(mission, design, "perfect"));
        }

        [TestCase(6, "barge")]
        [TestCase(7, "rtls")]
        public void WrongSite_DoesNotSatisfyTheMissionsLanding(int index, string flownMode)
        {
            // Review (PR #367): the flown landing follows design.recoveryMode, so a Haven catch could have satisfied KH-6's
            // required pad return. Only the mission's own site counts.
            var mission = campaign.Mission(index);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            Assert.AreNotEqual(flownMode, mission.LandingMode);
            design.recoveryMode = flownMode;
            var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
            Assert.AreEqual("short", flight.landingGrade, "flown " + flight.landingCause);
            Assert.IsFalse(flight.passed);
            Assert.AreEqual("short", BoosterLanding.MissionGrade(mission, design, "good"));
            design.recoveryMode = mission.LandingMode;
            Assert.AreEqual("good", BoosterLanding.MissionGrade(mission, design, "good"));
            Assert.AreEqual("good", BoosterLanding.MissionGrade(campaign.Mission(4), design, "good"), "a mission with no landing leaves the grade alone");
        }

        [Test]
        public void HandOver_IsFlightDriversGroundFrame()
        {
            // A state 40 km up, 10 km east of a pad that has rotated for 100 s: FlightDriver.UpdateFrame's numbers.
            double omega = Earth.SiteRotationSpeed(28.5) / Gravity.R_EARTH, t = 100;
            double r = Gravity.R_EARTH + 40000, ang = Math.PI / 2 + omega * t + 10000 / Gravity.R_EARTH;
            double x = r * Math.Cos(ang), y = r * Math.Sin(ang);
            // Ground-relative 600 m/s east and 800 m/s up, plus the air's co-rotation.
            double ex = -Math.Sin(ang), ey = Math.Cos(ang), ux = Math.Cos(ang), uy = Math.Sin(ang);
            double vx = 600 * ex + 800 * ux - omega * y, vy = 600 * ey + 800 * uy + omega * x;
            BoosterLanding.HandOver(new[] { x, y, vx, vy }, t, omega, out double down, out double alt, out double speed, out double pitch);
            Assert.AreEqual(10000, down, 1e-3);
            Assert.AreEqual(40000, alt, 1e-3);
            Assert.AreEqual(1000, speed, 1e-6);
            Assert.AreEqual(Math.Atan2(800, 600 * Gravity.R_EARTH / r) * 180 / Math.PI, pitch, 1e-9);
        }

        [Test]
        public void Predict_IsPure_AndTheBargeSitsOnTheSlowedPrediction()
        {
            var craft = new RecoveryCraft
            {
                DryKg = 20000, ReserveKg = 40000, ThrustVacN = 5600000, ThrustSlN = 5070000, IspSec = 311,
                DiameterM = 3.7, Cd = 0.5, GridFins = true, Legs = true, LegsKind = "light", Mode = "barge",
            };
            var body = RecoverySim.Begin(craft, 5000, 42000, 450, 780);
            RecoverySim.Predict(body, craft, out double a1, out double t1);
            RecoverySim.Predict(body, craft, out double a2, out double t2);
            Assert.AreEqual(a1, a2, 0);
            Assert.AreEqual(t1, t2, 0);
            Assert.AreEqual(a1, body.PredArc, 0, "Begin takes the first prediction");
            Assert.Greater(body.BargeArc, 5000, "Haven is downrange");
            Assert.Less(body.BargeArc, a1, "the 0.3 east cut lands short of the unburned fall");
        }

        static bool Flew(RecoveryVerdict v, RecoveryPhase phase)
        {
            var marks = v.Marks;
            for (int i = 0; i < marks.Length; i++) if (marks[i].Phase == phase) return true;
            return false;
        }

        sealed class Run
        {
            public string Mission;
            public Design Design;
            public BoosterLanding Landing;
            public SimResult Result;
            public bool Reached;
            public string Criterion;
            public double SeparatedAt = -1, SepSpeed, SepAlt;

            public string Log()
            {
                var v = Landing != null ? Landing.Verdict : null;
                return Mission + " " + (Result != null ? Result.outcome + " apo " + (Result.orbit.apoAlt / 1000).ToString("0") + " peri " + (Result.orbit.periAlt / 1000).ToString("0") : "?")
                    + " reached " + Reached + " (" + Criterion + ") | sep T+" + SeparatedAt.ToString("0.0") + " " + SepSpeed.ToString("0") + " m/s " + (SepAlt / 1000).ToString("0.0") + " km"
                    + (v != null ? " | " + (v.Cause ?? "clean") + " " + v.GradeName + " sink " + v.Sink.ToString("0.0") + " miss " + v.MissM.ToString("0") + " prop " + v.PropLeft.ToString("0") : " | no touchdown");
            }
        }

        // FlightToLandingPlaythroughTests.Play without the rail and report: AscentFlight.Prepare, FlightDriver.Launch and
        // UpdateFrame at 60 fps, RecoveryShow.Present (Watch, then Step on the assisted stick).
        Run Play(int index, ulong seed)
        {
            const double frame = 1.0 / 60.0;
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            ulong hash = FlightBatch.DesignHash(design);
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            bool auto = StepperProgress.Read(choice.Design).AutoAssistOn(DesignerDepth.Simple);
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            FailureMonitor monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, auto, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            var directed = new DirectedFlight(sim, director, opts);
            double omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;

            var run = new Run { Mission = "KH-" + index + " s" + seed, Design = design };
            run.Landing = BoosterLanding.Arm(catalog, design);
            double clock = 0, prevDown = 0, prevAlt = 0, attitude = 90, down = 0, alt = 0, speed = 0;
            bool hasAttitude = false, running = true;
            int frames = 0;
            while (frames++ < 400000)
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
                    }
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
                bool lost = run.Result != null && run.Result.failure != null;
                bool over = lost || (run.Result != null && !running);
                if (run.Landing.Armed && !run.Landing.Started)
                {
                    if (run.Landing.Watch(sim.T, sim.StageIdx, down, alt, speed, attitude, hasAttitude, over))
                    {
                        run.SeparatedAt = sim.T;
                        run.SepSpeed = speed;
                        run.SepAlt = alt;
                    }
                }
                else if (run.Landing.Started && !run.Landing.Touched)
                    run.Landing.Step(frame, RecoveryStick.Assisted);
                if (!running && (!run.Landing.Started || run.Landing.Touched)) break;
            }
            Assert.Less(frames, 400000, "the flight and the landing finish");

            var res = run.Result;
            double depReq = MissionScore.DepartureRequiredMps(mission.TargetKind, mission.ApoKm, mission.C3Km2PerS2, mission.GuidanceAltM, res.departureTW);
            bool dv = mission.DepartsFromParking && res.departureDv + 1e-6 >= depReq;
            run.Reached = MissionScore.TargetMet(mission.TargetKind, mission.ApoKm, mission.PeriKm, mission.C3Km2PerS2,
                MissionScore.FlownApoAltM(res), res.orbit.periAlt, MissionScore.C3(res.orbit.energy) / 1e6, dv, out run.Criterion);
            return run;
        }
    }
}
