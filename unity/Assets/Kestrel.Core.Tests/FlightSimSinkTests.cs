// Flight-sim hooks are observers. Expectations come from the sink-free result, the
// published event list, and the fixed pad and jettison numbers — not from the hook log.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public partial class FlightSimSinkTests
    {
        const double FairingM = 110000;
        const double Dt = 0.2;

        [Test]
        public void ClockScalesWallTimeAndKeepsThePhysicsStep()
        {
            const double step = 0.05;
            var slow = new PresentationTimeScale(0.4);
            var clock = new FixedStepAccumulator(step);
            int slowSteps = 0;
            for (int i = 0; i < 25; i++) slowSteps += clock.Advance(0.5, slow);
            Assert.AreEqual(step, clock.Step, 0, "0.4x must not scale the physics dt");
            Assert.AreEqual(100, slowSteps, "25 frames of 0.5 s at 0.4x is 5 s, 100 steps of 0.05 s");

            var realtime = new FixedStepAccumulator(step);
            int realSteps = 0;
            for (int i = 0; i < 10; i++) realSteps += realtime.Advance(0.5, PresentationTimeScale.Realtime);
            Assert.AreEqual(100, realSteps);

            var oneFrame = new FixedStepAccumulator(step);
            Assert.AreEqual(100, oneFrame.Advance(5, PresentationTimeScale.Realtime));
            Assert.AreEqual(step, oneFrame.Step, 0);

            var fine = new FixedStepAccumulator(step);
            int fineSteps = 0;
            for (int i = 0; i < 300; i++) fineSteps += fine.Advance(1.0 / 60.0, PresentationTimeScale.Realtime);
            Assert.AreEqual(100, fineSteps, "5 s of 60 Hz frames is the same 100 steps");
        }

        [Test]
        public void AscentHooksFireOnceInOrderWithTheSimPayloads()
        {
            var bare = Headless.Simulate(Headless.MediumLifter(), Opt(false));
            var quiet = Headless.Simulate(Headless.MediumLifter(), Opt(true));
            var log = new HookLog();
            var flown = Headless.Simulate(Headless.MediumLifter(), Opt(true), log);

            Same(quiet, flown);
            Assert.AreEqual(bare.outcome, flown.outcome);
            Assert.AreEqual(bare.time, flown.time, 0);
            Assert.AreEqual(bare.maxQ, flown.maxQ, 0);
            Assert.AreEqual(bare.maxQTime, flown.maxQTime, 0);
            Assert.AreEqual(bare.x, flown.x, 0);
            Assert.AreEqual(bare.y, flown.y, 0);
            Assert.AreEqual(bare.vx, flown.vx, 0);
            Assert.AreEqual(bare.vy, flown.vy, 0);
            Assert.AreEqual(bare.m, flown.m, 0);
            Assert.AreEqual(Count(bare, "fairing"), 0, "the default flight does not grow an event");
            Assert.AreEqual(Count(bare, "maxq"), 0, "max-Q is a hook, not a new golden event");
            Assert.AreEqual(1, Count(flown, "fairing"));
            Assert.AreEqual(bare.events.Count + 1, flown.events.Count);

            CollectionAssert.AreEqual(new[] { "ignition", "liftoff", "maxq", "sep", "ignition", "fairing" }, log.Names());
            Assert.AreEqual(2, log.IgnitionStages[1]);

            Assert.AreEqual(2, log.IgnitionCount);
            Assert.AreEqual(1, log.IgnitionStages[0]);
            Assert.AreEqual(0, log.IgnitionTimes[0], 0);
            var ign = Find(flown, "ignition", 1);
            Assert.AreEqual(ign.stage.Value, log.IgnitionStages[1]);
            Assert.AreEqual(ign.t, log.IgnitionTimes[1], 0);

            var lift = Find(bare, "liftoff", 0);
            Assert.AreEqual(1, log.LiftoffCount);
            Assert.AreEqual(lift.t, log.LiftoffT, 0);
            Assert.AreEqual(0, log.LiftoffPad.X, 0);
            Assert.AreEqual(Gravity.R_EARTH, log.LiftoffPad.Y, 0);

            Assert.AreEqual(1, log.MaxQCount);
            Assert.AreEqual(bare.maxQ, log.MaxQQ, 0);
            Assert.AreEqual(bare.maxQTime, log.MaxQT, 0);
            Assert.Greater(log.MaxQQ, 15000, "the medium lifter's peak is the real max-Q, above the old presenter arm");
            Assert.Less(log.MaxQT, Find(bare, "separation", 0).t);

            var sep = Find(bare, "separation", 0);
            var burn = Find(bare, "burnout", 0);
            Assert.AreEqual(1, log.SepCount);
            Assert.AreEqual(sep.stage.Value, log.SepStage);
            Assert.AreEqual(1, log.SepStage);
            double sepR = Math.Sqrt(log.SepPos.X * log.SepPos.X + log.SepPos.Y * log.SepPos.Y);
            Assert.AreEqual(Gravity.R_EARTH + sep.alt.Value, sepR, 1e-6);
            Assert.AreEqual(burn.v.Value, Math.Sqrt(log.SepVel.X * log.SepVel.X + log.SepVel.Y * log.SepVel.Y), 1e-6);
            Assert.AreEqual(sep.t, burn.t, 0);

            var fair = Find(flown, "fairing", 0);
            Assert.AreEqual(1, log.FairingCount);
            Assert.AreEqual(fair.t, log.FairingT, 0);
            Assert.GreaterOrEqual(fair.t, flown.maxQTime);
            Assert.GreaterOrEqual(fair.alt.Value, FairingM);
            double fairR = Math.Sqrt(log.FairingPos.X * log.FairingPos.X + log.FairingPos.Y * log.FairingPos.Y);
            Assert.AreEqual(Gravity.R_EARTH + fair.alt.Value, fairR, 1e-4);
            Assert.AreEqual(fair.v.Value, Math.Sqrt(log.FairingVel.X * log.FairingVel.X + log.FairingVel.Y * log.FairingVel.Y), 1e-6);
            Assert.AreEqual(0, log.FailureCount);
            Assert.AreEqual(0, log.ImpactCount);
        }

        [Test]
        public void ASinkDoesNotChangeTheResult()
        {
            var opts = Opt(true);
            var none = Headless.Simulate(Headless.MediumLifter(), opts);
            var silent = Headless.Simulate(Headless.MediumLifter(), opts, new SilentSink());
            var loud = new HookLog();
            var recorded = Headless.Simulate(Headless.MediumLifter(), opts, loud);
            Same(none, silent);
            Same(none, recorded);
            Assert.AreEqual(1, loud.MaxQCount);
            Assert.AreEqual(none.maxQ, loud.MaxQQ, 0);
        }

        [Test]
        public void FrameDeltaAndTimeScaleLeaveTheFlightBitIdentical()
        {
            var runs = new List<SimResult>();
            var prints = new List<string>();
            foreach (var schedule in new[]
            {
                (wall: Dt, scale: 1.0),
                (wall: Dt / 4, scale: 1.0),
                (wall: Dt / 0.4, scale: 0.4),
                (wall: 1.0 / 60.0, scale: 0.4),
            })
            {
                var log = new HookLog();
                var flown = FlyClock(schedule.wall, schedule.scale, log);
                runs.Add(flown);
                prints.Add(log.Fingerprint());
                Assert.AreEqual(Dt, new FixedStepAccumulator(Dt).Step, 0);
            }

            for (int i = 1; i < runs.Count; i++)
            {
                Same(runs[0], runs[i]);
                Assert.AreEqual(prints[0], prints[i], "schedule " + i);
            }
            Assert.That(prints[0], Does.Contain("maxq:"));
            Assert.That(prints[0], Does.Contain("fairing:"));
            Assert.Greater(runs[0].time, 50);
        }

        [Test]
        public void FailureEngineAndImpactFireOnce()
        {
            var engine = new HookLog();
            var sim = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = Dt, tMax = 30 }) { Sink = engine };
            while (!sim.Done && sim.T < 2) sim.Tick();
            double breakT = sim.T;
            sim.BreakUp(new FailureEvent { t = breakT, kind = "engine-out", cause = "engine-out", engineIndex = 3, stage = 1 });
            var engineResult = sim.Finish();
            Assert.AreEqual(1, engine.FailureCount);
            Assert.AreEqual(1, engine.EngineCount);
            Assert.AreEqual(0, engine.ImpactCount);
            Assert.AreEqual("engine-out", engine.FailKind);
            Assert.AreEqual("engine-out", engine.FailCause);
            Assert.AreEqual(3, engine.FailureEngine);
            Assert.AreEqual(3, engine.EngineIndex);
            Assert.AreEqual(FlightEngineMode.Sputter, engine.EngineMode);
            Assert.AreEqual(engineResult.failure.kind, engine.FailKind);
            Assert.AreEqual(engineResult.failure.engineIndex, engine.EngineIndex);
            var failEv = Find(engineResult, "failure", 0);
            Assert.AreEqual(failEv.t, engine.FailT, 0);
            double failR = Math.Sqrt(engine.FailPos.X * engine.FailPos.X + engine.FailPos.Y * engine.FailPos.Y);
            Assert.AreEqual(Gravity.R_EARTH + failEv.alt.Value, failR, 1e-4);
            Assert.AreEqual(failEv.v.Value, Math.Sqrt(engine.FailVel.X * engine.FailVel.X + engine.FailVel.Y * engine.FailVel.Y), 1e-6);
            int failAt = engine.Names().IndexOf("failure");
            int engAt = engine.Names().IndexOf("engine");
            Assert.Greater(failAt, engine.Names().IndexOf("liftoff"));
            Assert.AreEqual(failAt + 1, engAt);

            var burst = new HookLog();
            var over = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = Dt, tMax = 30 }) { Sink = burst };
            over.Tick();
            over.BreakUp(new FailureEvent { t = over.T, kind = "engine-overpressure", cause = "pc-cap", engineIndex = 0, stage = 1 });
            over.Finish();
            Assert.AreEqual(1, burst.EngineCount);
            Assert.AreEqual(0, burst.EngineIndex);
            Assert.AreEqual(FlightEngineMode.Overpressure, burst.EngineMode);
            Assert.AreEqual("engine-overpressure", burst.FailKind);
            Assert.AreEqual("pc-cap", burst.FailCause);

            var mix = new HookLog();
            var mixed = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = Dt, tMax = 30 }) { Sink = mix };
            mixed.Tick();
            mixed.BreakUp(new FailureEvent { t = mixed.T, kind = "bad-mixture", cause = "mixture", engineIndex = 1, stage = 1 });
            mixed.Finish();
            Assert.AreEqual(1, mix.EngineCount);
            Assert.AreEqual(FlightEngineMode.Sputter, mix.EngineMode);
            Assert.AreEqual(1, mix.EngineIndex);

            var bent = new HookLog();
            var structure = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = Dt, tMax = 30 }) { Sink = bent };
            structure.Tick();
            structure.BreakUp(new FailureEvent { t = structure.T, kind = "structural-maxq", cause = "q", engineIndex = -1, stage = 1 });
            structure.Finish();
            Assert.AreEqual(1, bent.FailureCount);
            Assert.AreEqual(0, bent.EngineCount);
            Assert.AreEqual("structural-maxq", bent.FailKind);
            Assert.AreEqual("q", bent.FailCause);
            Assert.AreEqual(-1, bent.FailureEngine);

            var impact = FlyWeakScout();
            Assert.AreEqual(1, impact.Log.FailureCount);
            Assert.AreEqual(0, impact.Log.EngineCount);
            Assert.AreEqual(1, impact.Log.ImpactCount);
            Assert.AreEqual("structural-maxq", impact.Log.FailKind);
            Assert.AreEqual("q", impact.Log.FailCause);
            Assert.Less(impact.Log.Names().IndexOf("failure"), impact.Log.Names().IndexOf("impact"));
            var hit = Find(impact.Result, "impact", 0);
            Assert.AreEqual(hit.t, impact.Result.time, 0);
            double r = JsMath.Hypot(impact.Result.x, impact.Result.y);
            double radial = r > 0 ? -(impact.Result.vx * impact.Result.x + impact.Result.vy * impact.Result.y) / r : 0;
            double expectSpeed = radial > 0 ? radial : JsMath.Hypot(impact.Result.vx, impact.Result.vy);
            Assert.AreEqual(expectSpeed, impact.Log.ImpactSpeed, 0);
            Assert.AreEqual(impact.Result.x, impact.Log.ImpactPos.X, 0);
            Assert.AreEqual(impact.Result.y, impact.Log.ImpactPos.Y, 0);
            Assert.Greater(impact.Log.ImpactSpeed, 0);
        }

        [Test]
        public void RecoveryHooksFollowThePhaseLogAndTheTouchdown()
        {
            var deck = new HookLog();
            var landed = RecoverySim.Fly(Craft(9000, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted, deck);
            var deckBare = RecoverySim.Fly(Craft(9000, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted);
            Assert.AreEqual(deckBare.End.Sink, landed.End.Sink, 0);
            Assert.AreEqual(deckBare.End.TiltDeg, landed.End.TiltDeg, 0);
            Assert.AreEqual(deckBare.MissM, landed.MissM, 0);
            Assert.AreEqual(deckBare.Cause, landed.Cause);
            Assert.IsNull(landed.Cause, Dump(landed));
            AssertPhases(deck, landed);
            Assert.Less(deck.Phases.IndexOf(RecoveryPhase.Boostback), deck.Phases.IndexOf(RecoveryPhase.Landing));
            Assert.Less(deck.Phases.IndexOf(RecoveryPhase.Landing), deck.Phases.IndexOf(RecoveryPhase.Down));
            Assert.AreEqual(1, deck.TouchCount);
            Assert.AreEqual(0, deck.CrashCount);
            Assert.AreEqual(landed.End.Sink, deck.TouchVz, 0);
            Assert.AreEqual(landed.End.TiltDeg, deck.TouchTilt, 0);
            Assert.AreEqual(FlightSurface.Deck, deck.TouchSurface);
            Assert.LessOrEqual(Math.Abs(landed.End.MissM), RecoverySim.DeckRadiusM);
            Assert.Greater(deck.Names().LastIndexOf("touchdown"), deck.Names().LastIndexOf("phase"));

            var sea = new HookLog();
            var splashed = RecoverySim.Fly(Craft(0, "barge"), 4500, 20000, 150, 25, RecoveryStick.Assisted, sea);
            Assert.AreEqual(RecoveryScore.Fuel, splashed.Cause);
            Assert.Greater(Math.Abs(splashed.End.MissM), RecoverySim.DeckRadiusM);
            Assert.AreEqual(1, sea.TouchCount);
            Assert.AreEqual(splashed.End.Sink, sea.TouchVz, 0);
            Assert.AreEqual(splashed.End.TiltDeg, sea.TouchTilt, 0);
            Assert.AreEqual(FlightSurface.Sea, sea.TouchSurface);
            string boom = RecoveryViews.Boom(splashed.Cause, splashed.End.Sink, Craft(0, "barge").VLeg);
            Assert.AreEqual("structural-maxq", boom);
            Assert.AreEqual(1, sea.CrashCount);
            Assert.AreEqual(boom, sea.CrashKind);
            double energy = 0.5 * splashed.End.Mass * splashed.End.Speed * splashed.End.Speed;
            Assert.AreEqual(energy, sea.CrashEnergy, 0);
            RecoveryFrames.Axes(sea.CrashPos.X, sea.CrashPos.Y, out _, out double alt, out double arc, out _, out _, out _, out _);
            Assert.AreEqual(splashed.End.Alt, alt, 0);
            Assert.AreEqual(splashed.End.Arc, arc, 0);
            Assert.AreEqual(splashed.End.Speed, Math.Sqrt(sea.CrashVel.X * sea.CrashVel.X + sea.CrashVel.Y * sea.CrashVel.Y), 0);
            Assert.Greater(sea.Names().IndexOf("crash"), sea.Names().IndexOf("touchdown"));

            var ground = new HookLog();
            var missed = RecoverySim.Fly(Craft(0, "rtls"), 80000, 40000, 900, -80, RecoveryStick.Assisted, ground);
            Assert.Greater(Math.Abs(missed.End.MissM), RecoverySim.DeckRadiusM);
            Assert.AreEqual(1, ground.TouchCount);
            Assert.AreEqual(FlightSurface.Ground, ground.TouchSurface);
            Assert.AreEqual(missed.End.Sink, ground.TouchVz, 0);
            Assert.AreEqual(missed.End.TiltDeg, ground.TouchTilt, 0);
        }

        static SimOptions Opt(bool fairing)
        {
            return new SimOptions { dt = Dt, tMax = 800, fairingJettisonAltM = fairing ? FairingM : 0 };
        }

        static SimResult FlyClock(double wall, double scale, HookLog log)
        {
            var opts = Opt(true);
            var sim = new AscentSim(Headless.MediumLifter(), opts) { Sink = log };
            var clock = new FixedStepAccumulator(opts.dt);
            var presentation = new PresentationTimeScale(scale);
            int guard = 0, stall = 0;
            while (!sim.Done && guard++ < 500000)
            {
                int n = clock.Advance(wall, presentation);
                if (n == 0)
                {
                    Assert.Less(++stall, 100000, "the accumulator stalled");
                    continue;
                }
                stall = 0;
                Assert.AreEqual(opts.dt, clock.Step, 0);
                for (int i = 0; i < n && !sim.Done; i++) sim.Tick();
            }
            Assert.IsTrue(sim.Done, "scale " + scale.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(Dt, clock.Step, 0);
            return sim.Finish();
        }

        static ImpactRun FlyWeakScout()
        {
            var cat = Golden.Catalog;
            var design = cat.Preset("scout").Clone();
            design.version = 3;
            design.tier = "kid";
            design.safetyFactor = 1.1;
            for (int i = 0; i < design.stages.Count; i++) design.stages[i].material = "al-li";
            var input = Stack.SimInputs(cat, design, 0.2);
            input.opts.tMax = 2000;
            var log = new HookLog();
            var sim = new AscentSim(input.stages, input.opts) { Sink = log };
            int guard = 0;
            while (!sim.Done && guard++ < 20000) sim.Tick();
            Assert.IsTrue(sim.Done, "t=" + sim.T.ToString("0.0", CultureInfo.InvariantCulture));
            return new ImpactRun { Log = log, Result = sim.Finish() };
        }

        static RecoveryCraft Craft(double reserveKg, string mode)
        {
            return new RecoveryCraft
            {
                DryKg = 18000,
                ReserveKg = reserveKg,
                ThrustVacN = 520000,
                ThrustSlN = 470000,
                IspSec = 311,
                DiameterM = 3.7,
                Cd = 0.52,
                GridFins = true,
                Legs = true,
                LegsKind = "heavy",
                Mode = mode,
            };
        }

        static void AssertPhases(HookLog log, RecoveryVerdict verdict)
        {
            Assert.AreEqual(verdict.Marks.Length, log.PhaseCount);
            for (int i = 0; i < verdict.Marks.Length; i++)
            {
                Assert.AreEqual(verdict.Marks[i].Phase, log.Phases[i]);
                Assert.AreEqual(verdict.Marks[i].T, log.PhaseT[i], 0);
                if (i > 0) Assert.AreNotEqual(log.Phases[i - 1], log.Phases[i]);
            }
            Assert.AreEqual(RecoveryPhase.Coast, log.Phases[0]);
            Assert.AreEqual(RecoveryPhase.Down, log.Phases[log.Phases.Count - 1]);
        }

        static void Same(SimResult a, SimResult b)
        {
            Assert.AreEqual(a.outcome, b.outcome);
            Assert.AreEqual(a.time, b.time, 0);
            Assert.AreEqual(a.maxQ, b.maxQ, 0);
            Assert.AreEqual(a.maxQTime, b.maxQTime, 0);
            Assert.AreEqual(a.maxQAlt, b.maxQAlt, 0);
            Assert.AreEqual(a.maxG, b.maxG, 0);
            Assert.AreEqual(a.lossGravity, b.lossGravity, 0);
            Assert.AreEqual(a.lossDrag, b.lossDrag, 0);
            Assert.AreEqual(a.lossSteering, b.lossSteering, 0);
            Assert.AreEqual(a.finalMass, b.finalMass, 0);
            Assert.AreEqual(a.x, b.x, 0);
            Assert.AreEqual(a.y, b.y, 0);
            Assert.AreEqual(a.vx, b.vx, 0);
            Assert.AreEqual(a.vy, b.vy, 0);
            Assert.AreEqual(a.m, b.m, 0);
            Assert.AreEqual(a.orbit.a, b.orbit.a, 0);
            Assert.AreEqual(a.orbit.e, b.orbit.e, 0);
            Assert.AreEqual(a.orbit.periAlt, b.orbit.periAlt, 0);
            Assert.AreEqual(a.orbit.apoAlt, b.orbit.apoAlt, 0);
            Assert.AreEqual(a.events.Count, b.events.Count, string.Join(", ", b.events));
            for (int i = 0; i < a.events.Count; i++)
            {
                Assert.AreEqual(a.events[i].type, b.events[i].type, "event " + i);
                Assert.AreEqual(a.events[i].stage, b.events[i].stage, "event " + i);
                Assert.AreEqual(a.events[i].t, b.events[i].t, 0, "event " + i);
                Assert.AreEqual(a.events[i].alt, b.events[i].alt, "event " + i);
                Assert.AreEqual(a.events[i].v, b.events[i].v, "event " + i);
            }
        }

        static int Count(SimResult r, string type)
        {
            int n = 0;
            for (int i = 0; i < r.events.Count; i++) if (r.events[i].type == type) n++;
            return n;
        }

        static SimEvent Find(SimResult r, string type, int skip)
        {
            for (int i = 0; i < r.events.Count; i++)
            {
                if (r.events[i].type != type) continue;
                if (skip == 0) return r.events[i];
                skip--;
            }
            Assert.Fail("missing " + type);
            return null;
        }

        static string Dump(RecoveryVerdict v)
        {
            return (v.Cause ?? "clean") + " sink=" + v.End.Sink.ToString("0.00", CultureInfo.InvariantCulture)
                + " miss=" + v.MissM.ToString("0", CultureInfo.InvariantCulture);
        }

        struct ImpactRun
        {
            public HookLog Log;
            public SimResult Result;
        }

        sealed class SilentSink : IFlightSimSink
        {
            public void OnIgnition(int stage, double t) { }
            public void OnLiftoff(double t, FlightVec padPos) { }
            public void OnMaxQ(double q, double t) { }
            public void OnStageSeparation(int stage, FlightVec pos, FlightVec vel) { }
            public void OnFairingSeparation(double t, FlightVec pos, FlightVec vel) { }
            public void OnRecoveryPhase(RecoveryPhase phase, double t) { }
            public void OnTouchdown(double vz, double tilt, FlightSurface surface) { }
            public void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy) { }
            public void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t) { }
            public void OnEngineFailure(int engineIndex, string mode) { }
            public void OnImpact(double speed, FlightVec pos) { }
        }

        sealed class HookLog : IFlightSimSink
        {
            public readonly List<string> Order = new List<string>();
            public readonly List<int> IgnitionStages = new List<int>();
            public readonly List<double> IgnitionTimes = new List<double>();
            public readonly List<RecoveryPhase> Phases = new List<RecoveryPhase>();
            public readonly List<double> PhaseT = new List<double>();
            public int IgnitionCount, LiftoffCount, MaxQCount, SepCount, FairingCount;
            public int PhaseCount, TouchCount, CrashCount, FailureCount, EngineCount, ImpactCount;
            public int SepStage, EngineIndex, FailureEngine;
            public double LiftoffT, MaxQQ, MaxQT, FairingT, FailT, ImpactSpeed, TouchVz, TouchTilt, CrashEnergy;
            public FlightVec LiftoffPad, SepPos, SepVel, FairingPos, FairingVel, FailPos, FailVel, ImpactPos, CrashPos, CrashVel;
            public string FailKind, FailCause, EngineMode, CrashKind;
            public FlightSurface TouchSurface;

            public List<string> Names()
            {
                var names = new List<string>(Order.Count);
                for (int i = 0; i < Order.Count; i++)
                {
                    string line = Order[i];
                    int cut = line.IndexOf(':');
                    names.Add(cut < 0 ? line : line.Substring(0, cut));
                }
                return names;
            }

            public string Fingerprint()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < Order.Count; i++) sb.Append(Order[i]).Append('\n');
                return sb.ToString();
            }

            static string R(double x) => x.ToString("R", CultureInfo.InvariantCulture);

            public void OnIgnition(int stage, double t)
            {
                IgnitionCount++;
                IgnitionStages.Add(stage);
                IgnitionTimes.Add(t);
                Order.Add("ignition:" + stage.ToString(CultureInfo.InvariantCulture) + ":" + R(t));
            }

            public void OnLiftoff(double t, FlightVec padPos)
            {
                LiftoffCount++;
                LiftoffT = t;
                LiftoffPad = padPos;
                Order.Add("liftoff:" + R(t) + ":" + R(padPos.X) + ":" + R(padPos.Y));
            }

            public void OnMaxQ(double q, double t)
            {
                MaxQCount++;
                MaxQQ = q;
                MaxQT = t;
                Order.Add("maxq:" + R(q) + ":" + R(t));
            }

            public void OnStageSeparation(int stage, FlightVec pos, FlightVec vel)
            {
                SepCount++;
                SepStage = stage;
                SepPos = pos;
                SepVel = vel;
                Order.Add("sep:" + stage.ToString(CultureInfo.InvariantCulture) + ":" + R(pos.X) + ":" + R(pos.Y) + ":" + R(vel.X) + ":" + R(vel.Y));
            }

            public void OnFairingSeparation(double t, FlightVec pos, FlightVec vel)
            {
                FairingCount++;
                FairingT = t;
                FairingPos = pos;
                FairingVel = vel;
                Order.Add("fairing:" + R(t) + ":" + R(pos.X) + ":" + R(pos.Y) + ":" + R(vel.X) + ":" + R(vel.Y));
            }

            public void OnRecoveryPhase(RecoveryPhase phase, double t)
            {
                PhaseCount++;
                Phases.Add(phase);
                PhaseT.Add(t);
                Order.Add("phase:" + phase + ":" + R(t));
            }

            public void OnTouchdown(double vz, double tilt, FlightSurface surface)
            {
                TouchCount++;
                TouchVz = vz;
                TouchTilt = tilt;
                TouchSurface = surface;
                Order.Add("touchdown:" + R(vz) + ":" + R(tilt) + ":" + surface);
            }

            public void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy)
            {
                CrashCount++;
                CrashKind = kind;
                CrashPos = pos;
                CrashVel = vel;
                CrashEnergy = energy;
                Order.Add("crash:" + kind + ":" + R(pos.X) + ":" + R(pos.Y) + ":" + R(vel.X) + ":" + R(vel.Y) + ":" + R(energy));
            }

            public void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t)
            {
                FailureCount++;
                FailKind = kind;
                FailCause = cause;
                FailureEngine = engineIndex;
                FailPos = pos;
                FailVel = vel;
                FailT = t;
                Order.Add("failure:" + kind + ":" + cause + ":" + engineIndex.ToString(CultureInfo.InvariantCulture) + ":" + R(t));
            }

            public void OnEngineFailure(int engineIndex, string mode)
            {
                EngineCount++;
                EngineIndex = engineIndex;
                EngineMode = mode;
                Order.Add("engine:" + engineIndex.ToString(CultureInfo.InvariantCulture) + ":" + mode);
            }

            public void OnImpact(double speed, FlightVec pos)
            {
                ImpactCount++;
                ImpactSpeed = speed;
                ImpactPos = pos;
                Order.Add("impact:" + R(speed) + ":" + R(pos.X) + ":" + R(pos.Y));
            }
        }
    }
}
