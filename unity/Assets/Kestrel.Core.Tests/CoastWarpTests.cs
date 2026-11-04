// PR5 (UX #416 item 6): coast time-warp. The policy, and the proof that warp changes only how many fixed steps run per
// frame: a warped flight ends bit-identical to the unwarped pad loop.
using System;
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CoastWarpTests
    {
        static CoastWarpState Clear(double next) => new CoastWarpState { SecondsToNext = next };

        [Test]
        public void FactorFollowsTheClearTimeAhead()
        {
            Assert.That(CoastWarp.Factor(Clear(double.PositiveInfinity)), Is.EqualTo(10));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.FarS)), Is.EqualTo(10));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.FarS - 0.01)), Is.EqualTo(4));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.LeadS)), Is.EqualTo(4));
            Assert.That(CoastWarp.Factor(Clear(CoastWarp.LeadS - 0.01)), Is.EqualTo(1));
            Assert.That(CoastWarp.Factor(Clear(0)), Is.EqualTo(1));
            Assert.That(CoastWarp.Factor(Clear(double.NaN)), Is.EqualTo(1));
        }

        [Test]
        public void AnyBlockerHoldsRealTime()
        {
            var s = Clear(1000); s.OpenCalls = 1; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "open call");
            s = Clear(1000); s.CameraOnBooster = true; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "booster camera");
            s = Clear(1000); s.SlowMo = true; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "slow-mo");
            s = Clear(1000); s.Disabled = true; Assert.That(CoastWarp.Factor(s), Is.EqualTo(1), "player off");
        }

        [Test]
        public void StepCapCoversTenTimesAHitchFrame()
        {
            Assert.That(CoastWarp.StepCap(1), Is.EqualTo(64), "unwarped cap unchanged");
            int need = (int)Math.Ceiling(0.1 * CoastWarp.Fast / MissionFlightSetup.LiveDt);
            Assert.That(CoastWarp.StepCap(CoastWarp.Fast), Is.GreaterThanOrEqualTo(need));
            Assert.That(CoastWarp.StepCap(CoastWarp.Fast), Is.LessThanOrEqualTo(200), "sane per-frame budget");
        }

        [Test]
        public void UntilIsTimeToCloseTheGap()
        {
            Assert.That(CoastWarp.Until(10, 2), Is.EqualTo(5));
            Assert.That(CoastWarp.Until(-1, 2), Is.EqualTo(0));
            Assert.That(CoastWarp.Until(10, -2), Is.EqualTo(double.PositiveInfinity));
            Assert.That(CoastWarp.Until(10, double.NaN), Is.EqualTo(double.PositiveInfinity));
        }

        sealed class Flown
        {
            public SimResult Result;
            public double[] Y;
            public AscentFlightScore Score;
            public int Frames;
            public double MaxFactor;
            public List<string> WarpedEvents = new List<string>();
            public double WallS;
        }

        static (DirectedFlight flight, PlayedChecks checks, double fairingAlt) Build(string missionId)
        {
            var c = StarterFixture.Catalog;
            var m = StarterFixture.Campaign.Missions.Find(x => x.Id == missionId);
            var d = Design.FromJson(StarterFixture.Rocket(m.Id).BuildDesign());
            ulong hash = FlightBatch.DesignHash(d);
            double target = Stack.RequiredDeltaV(c, d.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(m)) target = m.GuidanceAltM;
            var monitor = FailureMonitor.Engages(d) ? FailureMonitor.Create(c, d, target, hash, 1) : null;
            var director = MissionFlightSetup.Director(c, d, monitor?.Events, null, FailureMonitor.StructuralQLimPa(c, d),
                FailureMonitor.GentleTier(d), target, hash, 1, true, false, false, null);
            var setup = MissionFlightSetup.Build(c, m, d, MissionFlightSetup.LiveDt, null);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            return (new DirectedFlight(sim, director, opts), new PlayedChecks(c, d, setup.Stages, opts, sim), opts.fairingJettisonAltM);
        }

        /// <summary>The pre-PR5 FlightDriver loop, verbatim: 1/60 s frames, ×1, 64-step guard.</summary>
        static Flown FlyOld(string id)
        {
            var (flight, checks, _) = Build(id);
            var sim = flight.Sim;
            double clock = 0; int frames = 0;
            while (!sim.Done && frames < 2000000)
            {
                frames++;
                clock += Math.Min(1.0 / 60.0, 0.1);
                int guard = 0;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    flight.Step();
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) break;
                }
            }
            return new Flown { Result = flight.Finish(), Y = (double[])sim.Y.Clone(), Score = flight.Director.Score(), Frames = frames, WallS = frames / 60.0 };
        }

        static Flown FlyWarped(string id, Func<int, double> frame)
        {
            var (flight, checks, fairingAlt) = Build(id);
            var sim = flight.Sim;
            var clock = new WarpedFlightClock(flight, checks, fairingAlt);
            var f = new Flown();
            while (!sim.Done && f.Frames < 2000000)
            {
                double s = frame(f.Frames++);
                f.WallS += s;
                int before = sim.Events.Count, open = flight.Director.OpenCount;
                clock.Advance(s, 1, false);
                if (clock.Factor > f.MaxFactor) f.MaxFactor = clock.Factor;
                if (clock.Factor > 1)
                {
                    for (int i = before; i < sim.Events.Count; i++) f.WarpedEvents.Add(sim.Events[i].type + "@" + sim.Events[i].t.ToString("0.00"));
                    if (flight.Director.OpenCount > open) f.WarpedEvents.Add("call@" + sim.T.ToString("0.00"));
                }
            }
            f.Result = flight.Finish(); f.Y = (double[])sim.Y.Clone(); f.Score = flight.Director.Score();
            return f;
        }

        static double Jitter(int i)
        {
            // A deterministic mix of 144 Hz, 60 Hz, 30 Hz frames and 0.25 s hitches (clamped to 0.1 s).
            switch (i % 7) { case 0: return 1.0 / 144; case 1: return 1.0 / 60; case 2: return 1.0 / 30; case 3: return 0.25; case 4: return 1.0 / 144; case 5: return 1.0 / 90; default: return 1.0 / 60; }
        }

        static void AssertBitIdentical(Flown a, Flown b, string what)
        {
            Assert.That(b.Result.outcome, Is.EqualTo(a.Result.outcome), what + " outcome");
            Assert.That(b.Y.Length, Is.EqualTo(a.Y.Length));
            for (int i = 0; i < a.Y.Length; i++)
                Assert.That(BitConverter.DoubleToInt64Bits(b.Y[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.Y[i])), what + " Y[" + i + "]");
            Assert.That(b.Result.events.Count, Is.EqualTo(a.Result.events.Count), what + " events");
            for (int i = 0; i < a.Result.events.Count; i++)
            {
                var ea = a.Result.events[i]; var eb = b.Result.events[i];
                Assert.That(eb.type, Is.EqualTo(ea.type), what + " event " + i);
                Assert.That(BitConverter.DoubleToInt64Bits(eb.t), Is.EqualTo(BitConverter.DoubleToInt64Bits(ea.t)), what + " event t " + i);
            }
            CompareFields(a.Result, b.Result, what + " result");
            CompareFields(a.Result.orbit, b.Result.orbit, what + " orbit");
            CompareFields(a.Score, b.Score, what + " score");
        }

        static void CompareFields(object a, object b, string what)
        {
            if (a == null || b == null) { Assert.That(b == null, Is.EqualTo(a == null), what); return; }
            foreach (var f in a.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var va = f.GetValue(a); var vb = f.GetValue(b);
                if (va is double da) Assert.That(BitConverter.DoubleToInt64Bits((double)vb), Is.EqualTo(BitConverter.DoubleToInt64Bits(da)), what + "." + f.Name);
                else if (va is int || va is bool || va is string || va is Enum) Assert.That(vb, Is.EqualTo(va), what + "." + f.Name);
            }
        }

        // Two, three and four stage Starter defaults, with a fairing and the full call set.
        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void WarpedFlightIsBitIdentical(string id)
        {
            var old = FlyOld(id);
            var warped = FlyWarped(id, _ => 1.0 / 60);
            var jitter = FlyWarped(id, Jitter);
            Assert.That(warped.MaxFactor, Is.EqualTo(CoastWarp.Fast), "the flight actually warped");
            Assert.That(warped.Frames, Is.LessThan(old.Frames / 2), "warp shortens the flight");
            AssertBitIdentical(old, warped, id + " 60 Hz warped");
            AssertBitIdentical(old, jitter, id + " jittered warped");
        }

        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void EveryEventAndCallLandsAtRealTime(string id)
        {
            var f = FlyWarped(id, _ => 1.0 / 60);
            Assert.That(f.WarpedEvents, Is.Empty, id + ": events or calls inside a warped frame");
        }
    }
}
