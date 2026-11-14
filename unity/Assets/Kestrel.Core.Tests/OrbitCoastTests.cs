// The climax coast (OrbitCoast): after a clean final cutoff the live flight plays the real coast, propagated by the same
// AscentSim, for 3-5 wall seconds with an "Orbit!" or "Apogee!" callout before the result card.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class OrbitCoastTests
    {
        Catalog catalog;
        Campaign campaign;
        HangarData hangar;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            HomeAutoBuild.InstallMissionOrbits(catalog, campaign);
        }

        (AscentSim sim, SimResult result) FlyStarter(string id, double dt)
        {
            var m = campaign.ById(id);
            var d = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);
            var setup = MissionFlightSetup.Build(catalog, m, d, dt);
            var sim = new AscentSim(setup.Stages, setup.Options);
            while (sim.Tick()) { }
            return (sim, sim.Finish());
        }

        static double Alt(CoastState s) => Math.Sqrt(s.x * s.x + s.y * s.y) - Gravity.R_EARTH;

        [Test]
        public void FirstHop_CoastsToTheFlownApex_WithAnApogeeCallout()
        {
            var (sim, r) = FlyStarter("m1", MissionFlightSetup.LiveDt);
            Assert.AreEqual("suborbital", r.outcome);
            double t0 = sim.T, x0 = sim.Y[0];
            var coast = OrbitCoast.Plan(sim, r);
            Assert.IsNotNull(coast, "a clean hop cutoff has a coast to play");
            Console.WriteLine($"KH-1 coast: cutoff T+{coast.StartT:0.0} -> apex {coast.EndAltM:0} m in {coast.SpanSeconds:0.0} s sim, {coast.WallSeconds} s wall, {coast.Samples} samples");
            Assert.IsFalse(coast.IsOrbit);
            Assert.AreEqual(OrbitCoast.CalloutApogee, coast.Callout);
            Assert.AreEqual(t0, sim.T, "planning the coast never moves the sim");
            Assert.AreEqual(x0, sim.Y[0]);
            Assert.AreEqual(r.time, coast.StartT, 1e-9, "the coast starts at the cutoff state");
            Assert.AreEqual(r.coastApexAlt, coast.EndAltM, Math.Max(5, 0.002 * r.coastApexAlt), "ends at the drag-aware flown apex the score uses");
            Assert.That(coast.SpanSeconds, Is.GreaterThan(OrbitCoast.MinSpanSeconds));
            Assert.That(coast.WallSeconds, Is.InRange(OrbitCoast.MinWallSeconds, OrbitCoast.MaxWallSeconds));
            StringAssert.Contains("km", coast.Detail);
            Monotone(coast, rising: true);
        }

        [Test]
        public void TinyOrbit_CoastsAlongTheNewOrbit_WithAnOrbitCallout()
        {
            var (sim, r) = FlyStarter("m3", MissionFlightSetup.BatchDt);
            Assert.AreEqual("orbit", r.outcome);
            var coast = OrbitCoast.Plan(sim, r);
            Assert.IsNotNull(coast);
            Assert.IsTrue(coast.IsOrbit);
            Console.WriteLine($"KH-3 coast: cutoff T+{coast.StartT:0.0} span {coast.SpanSeconds:0} s ({coast.Warp:0}x), ends {coast.EndAltM / 1000:0.0} km; {coast.Detail}");
            Assert.AreEqual(OrbitCoast.CalloutOrbit, coast.Callout);
            Assert.That(coast.SpanSeconds, Is.GreaterThanOrEqualTo(OrbitCoast.MinOrbitSpanSeconds), "an orbit coast covers real ground");
            Assert.That(coast.SpanSeconds, Is.LessThanOrEqualTo(r.orbit.period / 2 + 1));
            Assert.That(coast.Warp, Is.GreaterThan(20), "it is a time-warp, not real time");
            // Propagated, not drawn: every sample stays on the cutoff orbit (energy within 0.1%, never under perigee).
            for (int i = 0; i <= 40; i++)
            {
                var s = coast.At(coast.WallSeconds * i / 40.0);
                var o = Orbit.Planar(s.x, s.y, s.vx, s.vy);
                Assert.AreEqual(r.orbit.energy, o.energy, Math.Abs(r.orbit.energy) * 1e-3, "energy at sample " + i);
                Assert.That(Alt(s), Is.GreaterThan(r.orbit.periAlt - 1000), "above perigee at sample " + i);
            }
        }

        [Test]
        public void Progress_EasesFromCutoffToTheEnd_AndFinishes()
        {
            var (sim, r) = FlyStarter("m1", MissionFlightSetup.LiveDt);
            var coast = OrbitCoast.Plan(sim, r, 9);
            Assert.AreEqual(OrbitCoast.MaxWallSeconds, coast.WallSeconds, "wall time is clamped to 3-5 s");
            Assert.AreEqual(OrbitCoast.MinWallSeconds, OrbitCoast.Plan(sim, r, 0.5).WallSeconds);
            Assert.AreEqual(0, coast.Progress(0), 1e-12);
            Assert.AreEqual(1, coast.Progress(coast.WallSeconds), 1e-12);
            Assert.AreEqual(coast.StartT, coast.At(0).t, 1e-9);
            Assert.AreEqual(coast.End.t, coast.At(coast.WallSeconds + 1).t, 1e-9);
            Assert.IsFalse(coast.Finished(coast.WallSeconds - 0.01));
            Assert.IsTrue(coast.Finished(coast.WallSeconds));
            double prev = -1;
            for (int i = 0; i <= 50; i++) { double p = coast.Progress(coast.WallSeconds * i / 50.0); Assert.That(p, Is.GreaterThanOrEqualTo(prev)); prev = p; }
        }

        [Test]
        public void NoCoast_ForAFailure_ACrash_OrNothing()
        {
            var (sim, r) = FlyStarter("m1", MissionFlightSetup.LiveDt);
            Assert.IsNull(OrbitCoast.Plan(null, r));
            Assert.IsNull(OrbitCoast.Plan(sim, null));
            var failed = sim.Finish();
            failed.failure = new FailureEvent { kind = "structural-maxq", t = 20 };
            Assert.IsNull(OrbitCoast.Plan(sim, failed), "a failure goes straight to its own show");
            var crash = sim.Finish();
            crash.outcome = "crash";
            Assert.IsNull(OrbitCoast.Plan(sim, crash));
        }

        [Test]
        public void CoastTrack_StopsAtTheApex_AndLeavesTheSimAlone()
        {
            var (sim, r) = FlyStarter("m1", MissionFlightSetup.LiveDt);
            var track = sim.CoastTrack(OrbitCoast.HopMaxSpanSeconds, 0.5, true);
            Assert.That(track.Count, Is.GreaterThan(2));
            var apex = track[track.Count - 1];
            Assert.AreEqual(r.coastApexAlt, Alt(apex), Math.Max(5, 0.002 * r.coastApexAlt));
            var before = track[track.Count - 2];
            Assert.That(apex.vx * apex.x + apex.vy * apex.y, Is.LessThanOrEqualTo(0), "the last sample is past the top");
            Assert.That(before.vx * before.x + before.vy * before.y, Is.GreaterThan(0), "the one before it still climbs");
            Assert.IsTrue(sim.Done, "still finished");
            Assert.AreEqual(r.time, sim.T, "the sim clock did not move");
        }

        static void Monotone(OrbitCoast coast, bool rising)
        {
            double prev = double.NegativeInfinity;
            for (int i = 0; i <= 40; i++)
            {
                double a = Alt(coast.At(coast.WallSeconds * i / 40.0));
                if (rising) Assert.That(a, Is.GreaterThanOrEqualTo(prev - 0.5), "altitude climbs to the apex, sample " + i);
                prev = a;
            }
        }

        /// <summary>
        /// #431 guaranteed exit: the coast's wall clock follows real time, so the climax ends after its 3-5 s at any frame
        /// rate down to 2 fps, and within ceil(5 / 0.5) = 10 frames below that. At 0.1 s per frame (the ascent's hitch
        /// clamp) a 2.5 fps CI player still had most of the coast to play 15 s later.
        /// </summary>
        [Test]
        public void TheCoastClockFollowsRealTime_AndEndsInBoundedFrames_AtAnyFrameRate()
        {
            foreach (double fps in new[] { 120.0, 60.0, 30.0, 10.0, 4.0, 2.5, 2.0 })
            {
                double dt = 1.0 / fps, wall = 0, real = 0;
                int frames = 0;
                while (wall < OrbitCoast.MaxWallSeconds && frames < 100000) { wall = OrbitCoast.Advance(wall, dt, dt); real += dt; frames++; }
                Assert.That(real, Is.LessThanOrEqualTo(OrbitCoast.MaxWallSeconds + dt + 1e-9), fps + " fps: real seconds to finish");
            }
            foreach (double slow in new[] { 0.75, 2.0, 30.0, double.PositiveInfinity })
            {
                double wall = 0; int frames = 0;
                while (wall < OrbitCoast.MaxWallSeconds && frames < 1000) { wall = OrbitCoast.Advance(wall, slow, slow); frames++; }
                Assert.That(frames, Is.LessThanOrEqualTo((int)Math.Ceiling(OrbitCoast.MaxWallSeconds / OrbitCoast.MaxStepSeconds)) ,
                    slow + " s frames: the coast still ends in bounded frames");
            }
            // Paused game time (timeScale 0) still advances on the unscaled clock; a hitch is capped; NaN or negative counts as zero.
            Assert.AreEqual(1.0 / 60, OrbitCoast.Advance(0, 0, 1.0 / 60), 1e-12);
            Assert.AreEqual(1.0 / 30, OrbitCoast.Advance(0, 1.0 / 60, 1.0 / 30), 1e-12, "a slow frame counts in full, not clamped to the scaled step");
            Assert.AreEqual(OrbitCoast.MaxStepSeconds, OrbitCoast.Advance(0, 3, 3), 1e-12);
            Assert.AreEqual(0.0, OrbitCoast.Advance(0, double.NaN, -1), 1e-12);
        }

        /// <summary>
        /// A 7 s hitch used to satisfy EnsureReportReady's MaxWallSeconds+2 check in one callback, while the coast
        /// clock could add only MaxStepSeconds. PlayMode then saw Coasting false before CoastWall reached 1.5.
        /// </summary>
        [Test]
        public void WatchdogCredit_AHitchCountsAsOneCappedStep()
        {
            double mark = double.NaN;
            double credit = OrbitCoast.WatchdogCredit(mark, 10, out mark);
            Assert.AreEqual(0, credit, 1e-12, "the first sample only arms the clock");
            Assert.AreEqual(10, mark, 1e-12);

            credit = OrbitCoast.WatchdogCredit(mark, 17, out mark);
            Assert.AreEqual(OrbitCoast.MaxStepSeconds, credit, 1e-12, "a 7 s hitch is one capped step, not the whole stall budget");
            Assert.AreEqual(17, mark, 1e-12);

            double again = mark;
            Assert.AreEqual(0, OrbitCoast.WatchdogCredit(mark, 17, out again), 1e-12, "FailureHost and SliceApp share one timestamp");
            Assert.AreEqual(17, again, 1e-12);
            Assert.AreEqual(0, OrbitCoast.WatchdogCredit(mark, double.NaN, out double kept), 1e-12);
            Assert.AreEqual(mark, kept, 1e-12);
            Assert.AreEqual(0, OrbitCoast.WatchdogCredit(20, 19, out double back), 1e-12, "a backwards clock does not add credit");
            Assert.AreEqual(19, back, 1e-12);
        }

        [Test]
        public void WatchdogCredit_SmoothFramesTrackRealTime_HitchesTakeTheStepCap()
        {
            double mark = double.NaN;
            double credit = OrbitCoast.WatchdogCredit(mark, 0, out mark);
            double t = 0;
            for (int i = 0; i < 70; i++)
            {
                t += 0.1;
                credit += OrbitCoast.WatchdogCredit(mark, t, out mark);
            }
            Assert.AreEqual(7.0, credit, 1e-9, "10 fps is under the step cap, so the budget still matches real time");

            mark = double.NaN;
            credit = OrbitCoast.WatchdogCredit(mark, 0, out mark);
            t = 0;
            int frames = 0;
            while (credit < OrbitCoast.MaxWallSeconds + 2 && frames < 100)
            {
                t += 7;
                credit += OrbitCoast.WatchdogCredit(mark, t, out mark);
                frames++;
            }
            Assert.AreEqual((int)Math.Ceiling((OrbitCoast.MaxWallSeconds + 2) / OrbitCoast.MaxStepSeconds), frames,
                "hitch frames reach the stall budget in capped steps, not on the first 7 s gap");
        }
    }
}
