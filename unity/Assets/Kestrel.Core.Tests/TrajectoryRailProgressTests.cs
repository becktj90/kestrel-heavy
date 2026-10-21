// The left progress rail through a real ascent (TestFlight t432, #333): at T+116 (S1, 33 km) and T+192 (S2, 57 km)
// the marker sat parked between two stops and Fairing never lit. The rail is fed exactly as AscentHud.FeedTrajectory
// feeds it: the sim's events, then Sample() every frame, then At().
using System;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TrajectoryRailProgressTests
    {
        static void Fly(bool simFairingEvent, Action<double, double, TrajectoryPlan> each, out TrajectoryWatch watch)
        {
            var sim = new AscentSim(Headless.MediumLifter(), new SimOptions { payload = 5000, targetAlt = 200e3, fairingJettisonAltM = simFairingEvent ? 110000 : 0 });
            watch = new TrajectoryWatch { HasFairing = true, Stages = 2, FairingAltM = 110000 };
            int fed = 0;
            for (int i = 0; i < 400000 && !sim.Done; i++)
            {
                sim.Tick();
                for (; fed < sim.Events.Count; fed++)
                {
                    var ev = sim.Events[fed];
                    watch.Seen(new TrajectoryEvent { T = ev.t, Type = ev.type, Stage = ev.stage ?? 0, AltM = ev.alt ?? 0, SpeedMps = ev.v ?? 0 });
                }
                double alt = Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]) - Gravity.R_EARTH;
                watch.Sample(sim.T, alt, Math.Sqrt(sim.Y[2] * sim.Y[2] + sim.Y[3] * sim.Y[3]), sim.LastQ);
                if (i % 20 == 0) each(sim.T, alt, watch.At(sim.T, alt, Math.Sqrt(sim.Y[2] * sim.Y[2] + sim.Y[3] * sim.Y[3])));
            }
            for (; fed < sim.Events.Count; fed++)
            {
                var ev = sim.Events[fed];
                watch.Seen(new TrajectoryEvent { T = ev.t, Type = ev.type, Stage = ev.stage ?? 0, AltM = ev.alt ?? 0, SpeedMps = ev.v ?? 0 });
            }
            double endAlt = Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]) - Gravity.R_EARTH;
            each(sim.T + 0.05, endAlt, watch.At(sim.T + 0.05, endAlt, 0));
        }

        [TestCase(true, TestName = "RailMovesAndLightsEveryStop(sim fairing event)")]
        [TestCase(false, TestName = "RailMovesAndLightsEveryStop(no sim fairing event, the flown AscentSim)")]
        public void RailMovesForwardAndEveryMilestoneLights(bool simFairingEvent)
        {
            float prev = 0f; double prevT = 0, lastMoveT = 0;
            double longestStall = 0;
            int lit = 0;
            TrajectoryPlan final = null;
            Fly(simFairingEvent, (t, alt, plan) =>
            {
                Assert.That(plan.Progress, Is.GreaterThanOrEqualTo(prev - 1e-6f), "never backwards at T+" + t.ToString("0.0"));
                int passed = 0;
                foreach (var m in plan.Marks) if (m.Passed) passed++;
                Assert.That(passed, Is.GreaterThanOrEqualTo(lit), "a lit stop stays lit");
                lit = passed;
                if (t > 1 && plan.Upcoming >= 0)
                {
                    if (plan.Progress > prev + 1e-5f) lastMoveT = t;
                    longestStall = Math.Max(longestStall, t - lastMoveT);
                    // The marker never reaches an unlit stop.
                    Assert.That(plan.Progress, Is.LessThan(plan.Marks[plan.Upcoming].Along), "short of the next stop at T+" + t.ToString("0.0"));
                }
                prev = plan.Progress; prevT = t; final = plan;
            }, out var watch);
            // 202/t432: parked for > 60 s at the 0.82 clamp. It visibly creeps every few seconds now.
            Assert.That(longestStall, Is.LessThan(5.0), "the marker kept moving");
            Assert.That(final, Is.Not.Null);
            foreach (var m in final.Marks)
                Assert.That(m.Passed, Is.True, m.Label + " lit by the end of the ascent; final T+" + prevT.ToString("0") + " " + string.Join(",", Array.ConvertAll(final.Marks, k => k.Label + (k.Passed ? "*" : ""))));
            Assert.That(final.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void TheT432MomentsAreAheadOfTheOldClamp()
        {
            // T+116, S1: Max-Q passed at ~T+60; MECO not yet. T+192, S2: MECO passed at ~T+150, fairing not yet (57 km).
            var w = new TrajectoryWatch { HasFairing = true, Stages = 2, FairingAltM = 110000 };
            w.Seen(new TrajectoryEvent { T = 0.5, Type = "liftoff", AltM = 1 });
            w.Seen(new TrajectoryEvent { T = 60, Type = "maxq", AltM = 11000 });
            var a = w.At(116, 33000, 1450);
            var b = w.At(190, 33000 + 1, 1450);
            Assert.That(b.Progress, Is.GreaterThan(a.Progress), "S1 still burning: the marker still climbs");
            w.Seen(new TrajectoryEvent { T = 150, Type = "burnout", Stage = 1, AltM = 45000 });
            var c = w.At(192, 56900, 4370);
            var d = w.At(230, 75000, 5200);
            Assert.That(d.Progress, Is.GreaterThan(c.Progress));
            Assert.That(TrajectoryWatch.Ease(10), Is.LessThan(TrajectoryWatch.EaseCap));
            Assert.That(TrajectoryWatch.Ease(0.6), Is.EqualTo(0.6).Within(1e-12));
            // The fairing call resolving (AscentDirector.FairingT, fed as a "fairing" event) lights Fairing.
            w.Seen(new TrajectoryEvent { T = 231, Type = "fairing", AltM = 98000 });
            var e = w.At(232, 99000, 5300);
            Assert.That(Array.Find(e.Marks, m => m.Stop == TrajectoryStop.Fairing).Passed, Is.True);
        }
    }
}
