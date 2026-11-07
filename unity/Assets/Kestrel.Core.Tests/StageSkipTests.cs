// Skip to next stage (DECISIONS #78): the pump runs the same fixed steps as the pad loop, so a skipped
// flight ends bit-identical to the real-time one; a failure stops the skip where it happens; a call the player
// must decide stops it too, while auto-assist calls resolve inside the pump at their real-time grades.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StageSkipTests
    {
        sealed class Built
        {
            public DirectedFlight Flight;
            public PlayedChecks Checks;
            public AscentSim Sim => Flight.Sim;
        }

        static Built Build(string missionId, bool autoAssist = true, Action<Design> tweak = null)
        {
            var c = StarterFixture.Catalog;
            var m = StarterFixture.Campaign.Missions.Find(x => x.Id == missionId);
            var d = Design.FromJson(StarterFixture.Rocket(m.Id).BuildDesign());
            tweak?.Invoke(d);
            ulong hash = FlightBatch.DesignHash(d);
            double target = Stack.RequiredDeltaV(c, d.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(m)) target = m.GuidanceAltM;
            var monitor = FailureMonitor.Engages(d) ? FailureMonitor.Create(c, d, target, hash, 1) : null;
            var director = MissionFlightSetup.Director(c, d, monitor?.Events, null, FailureMonitor.StructuralQLimPa(c, d),
                FailureMonitor.GentleTier(d), target, hash, 1, autoAssist, false, false, null);
            var setup = MissionFlightSetup.Build(c, m, d, MissionFlightSetup.LiveDt, null);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            return new Built { Flight = new DirectedFlight(sim, director, opts), Checks = new PlayedChecks(c, d, setup.Stages, opts, sim) };
        }

        /// <summary>The pre-skip FlightDriver loop, verbatim: 1/60 s frames, ×1, 64-step guard.</summary>
        static void StepRealTime(Built b, ref double clock)
        {
            var sim = b.Sim;
            clock += 1.0 / 60.0;
            int guard = 0;
            while (sim.T < clock && !sim.Done && guard++ < 64)
            {
                int before = sim.Events.Count;
                b.Flight.Step();
                b.Checks.NoteEvents(sim, before);
                if (b.Checks.StopForBreakup(sim)) break;
            }
        }

        static void AssertSameState(double[] a, double[] b, string what)
        {
            Assert.That(b.Length, Is.EqualTo(a.Length), what + " state length");
            for (int i = 0; i < a.Length; i++)
                Assert.That(BitConverter.DoubleToInt64Bits(b[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(a[i])), what + " Y[" + i + "]");
        }

        static void AssertSameEvents(AscentSim a, AscentSim b, string what)
        {
            Assert.That(b.Events.Count, Is.EqualTo(a.Events.Count), what + " event count");
            for (int i = 0; i < a.Events.Count; i++)
            {
                Assert.That(b.Events[i].type, Is.EqualTo(a.Events[i].type), what + " event " + i);
                Assert.That(BitConverter.DoubleToInt64Bits(b.Events[i].t), Is.EqualTo(BitConverter.DoubleToInt64Bits(a.Events[i].t)), what + " event t " + i);
            }
        }

        [Test]
        public void LabelsNameTheEvents()
        {
            Assert.That(StageSkip.Label(StageSkipTarget.MaxQ), Is.EqualTo("Max-Q"));
            Assert.That(StageSkip.Label(StageSkipTarget.Meco), Is.EqualTo("MECO"));
            Assert.That(StageSkip.Label(StageSkipTarget.Staging), Is.EqualTo("Staging"));
            Assert.That(StageSkip.Label(StageSkipTarget.Seco), Is.EqualTo("SECO"));
            Assert.That(StageSkip.Label(StageSkipTarget.EntryBurn), Is.EqualTo("Entry burn"));
            Assert.That(StageSkip.Label(StageSkipTarget.LandingBurn), Is.EqualTo("Landing burn"));
            Assert.That(StageSkip.Label(StageSkipTarget.None), Is.EqualTo(""));
        }

        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void NextTargetStartsAtMaxQ(string id)
        {
            var b = Build(id);
            Assert.That(StageSkip.NextAscentTarget(b.Sim, b.Checks), Is.EqualTo(StageSkipTarget.MaxQ));
        }

        [TestCase("m3")]
        [TestCase("m6")]
        [TestCase("m10")]
        [TestCase("m19")]
        public void SkippedFlightToFirstEventMatchesRealTime(string id)
        {
            // Real time to Max-Q confirmation, one step at a time: the frame-chunked pad loop records T
            // after a whole frame's stepping (up to a frame past the flip), while the pump stops on the
            // flip step itself. The step sequence is the same either way; compare it step to step.
            var real = Build(id);
            while (!real.Sim.MaxQPassed && !real.Sim.Done)
            {
                int before = real.Sim.Events.Count;
                real.Flight.Step();
                real.Checks.NoteEvents(real.Sim, before);
                if (real.Checks.StopForBreakup(real.Sim)) break;
            }
            Assert.IsTrue(real.Sim.MaxQPassed, id + " reaches Max-Q");
            double realT = real.Sim.T;
            double[] realY = (double[])real.Sim.Y.Clone();

            // The same flight, skipped in per-frame chunks.
            var skipped = Build(id);
            StageSkipRun run = null;
            int frames = 0;
            while (frames++ < 10000)
            {
                run = StageSkip.Advance(skipped.Flight, skipped.Checks, StageSkip.MaxStepsPerFrame);
                if (run.Stop != StageSkipStop.Budget) break;
            }
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Target), id + " stops at the event, not the budget");
            Assert.That(run.Reached, Is.EqualTo(StageSkipTarget.MaxQ), id + " stops at Max-Q");
            Assert.That(BitConverter.DoubleToInt64Bits(skipped.Sim.T), Is.EqualTo(BitConverter.DoubleToInt64Bits(realT)), id + " T");
            AssertSameState(realY, skipped.Sim.Y, id);
            AssertSameEvents(real.Sim, skipped.Sim, id);
            Assert.That(frames, Is.LessThan(20), id + " a skip is a few frames, not a flight");
        }

        [Test]
        public void RepeatedSkipsWalkEveryEventAndMatchRealTime()
        {
            const string id = "m10";
            // Real time to the end.
            var real = Build(id);
            double clock = 0;
            int realFrames = 0;
            while (!real.Sim.Done && realFrames++ < 200000) StepRealTime(real, ref clock);
            Assert.IsTrue(real.Sim.Done, "the real-time flight ends");
            var realResult = real.Flight.Finish();
            Assert.IsNull(realResult.failure, "m10 flies clean in calm air");
            double[] realY = (double[])real.Sim.Y.Clone();

            // The same flight, one skip per event.
            var skipped = Build(id);
            var stops = new System.Collections.Generic.List<StageSkipTarget>();
            int frames = 0;
            while (frames++ < 10000)
            {
                var run = StageSkip.Advance(skipped.Flight, skipped.Checks, StageSkip.MaxStepsPerFrame);
                if (run.Stop == StageSkipStop.Budget) continue;
                if (run.Stop == StageSkipStop.Target) stops.Add(run.Reached);
                else break;
                if (skipped.Sim.Done) break;
            }
            // m10 has 4 stages and its core is the second stage (PlayedChecks.MecoStage 2): the booster
            // stage separates first (staging), then the core burns out (MECO, with its separation), then the
            // last staging, then SECO. A burnout and its separation are one sim step, so each stop names the
            // burnout; the separation rides along.
            Assert.That(stops, Is.EqualTo(new[] { StageSkipTarget.MaxQ, StageSkipTarget.Staging, StageSkipTarget.Meco, StageSkipTarget.Staging, StageSkipTarget.Seco }),
                "one skip per event, in flight order");
            Assert.IsTrue(skipped.Sim.Done, "the skipped flight ends at SECO");
            var skippedResult = skipped.Flight.Finish();
            Assert.That(skippedResult.outcome, Is.EqualTo(realResult.outcome), "outcome");
            Assert.That(BitConverter.DoubleToInt64Bits(skipped.Sim.T), Is.EqualTo(BitConverter.DoubleToInt64Bits(real.Sim.T)), "T");
            AssertSameState(realY, skipped.Sim.Y, "m10 walked");
            AssertSameEvents(real.Sim, skipped.Sim, "m10 walked");
            Assert.That(frames, Is.LessThan(realFrames / 10), "skips take a fraction of the frames");
        }

        [Test]
        public void SkipStopsAtFailure()
        {
            // A starter weakened past its structural limit breaks up at Max-Q in calm air.
            const string id = "m10";
            var real = Build(id, true, d => d.safetyFactor = 0.3);
            double clock = 0;
            double[] failY = null;
            double failT = double.NaN;
            while (!real.Sim.Done)
            {
                double beforeT = real.Sim.T;
                StepRealTime(real, ref clock);
                if (real.Sim.Done && real.Flight.Finish().failure != null)
                {
                    failT = real.Sim.T;
                    failY = (double[])real.Sim.Y.Clone();
                    break;
                }
                Assert.That(real.Sim.T, Is.GreaterThan(beforeT), "the flight advances");
            }
            Assert.IsNotNull(failY, "the weakened starter fails in real time");
            Assert.IsNotNull(real.Flight.Finish().failure, "the failure is recorded");

            var skipped = Build(id, true, d => d.safetyFactor = 0.3);
            StageSkipRun run = null;
            int frames = 0;
            while (frames++ < 10000)
            {
                run = StageSkip.Advance(skipped.Flight, skipped.Checks, StageSkip.MaxStepsPerFrame);
                if (run.Stop != StageSkipStop.Budget) break;
            }
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Failure), "the skip stops at the failure, never past it");
            Assert.That(BitConverter.DoubleToInt64Bits(skipped.Sim.T), Is.EqualTo(BitConverter.DoubleToInt64Bits(failT)), "failure T");
            AssertSameState(failY, skipped.Sim.Y, "failure state");
            Assert.IsNotNull(skipped.Flight.Finish().failure, "the skipped flight records the same failure");
        }



        [Test]
        public void SkipStopsAtCallWhenAutoAssistOff_AndFliesThroughWhenOn()
        {
            const string id = "m10";
            // Real time, no auto-assist: when does the first call open?
            var real = Build(id, false);
            double clock = 0;
            double callT = double.NaN;
            while (!real.Sim.Done && double.IsNaN(callT))
            {
                StepRealTime(real, ref clock);
                if (real.Flight.Director.OpenCount > 0) callT = real.Sim.T;
            }
            Assert.That(double.IsNaN(callT), Is.False, "a call opens on the way up");

            // Skipped, no auto-assist: stops on the exact step the call opens.
            var manual = Build(id, false);
            StageSkipRun run = null;
            int frames = 0;
            while (frames++ < 10000)
            {
                run = StageSkip.Advance(manual.Flight, manual.Checks, StageSkip.MaxStepsPerFrame);
                if (run.Stop != StageSkipStop.Budget) break;
            }
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Call), "the player gets the decision");
            Assert.That(BitConverter.DoubleToInt64Bits(manual.Sim.T), Is.EqualTo(BitConverter.DoubleToInt64Bits(callT)), "call T");
            Assert.That(manual.Flight.Director.OpenCount, Is.GreaterThan(0), "the call is open when the skip stops");

            // Skipped, auto-assist on: the calls resolve inside the pump, the skip reaches Max-Q.
            var auto = Build(id, true);
            frames = 0;
            while (frames++ < 10000)
            {
                run = StageSkip.Advance(auto.Flight, auto.Checks, StageSkip.MaxStepsPerFrame);
                if (run.Stop != StageSkipStop.Budget) break;
            }
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Target), "auto-assist calls never stop a skip");
            Assert.That(run.Reached, Is.EqualTo(StageSkipTarget.MaxQ));
        }

        [Test]
        public void NoTargetOnceTheFlightIsOver()
        {
            var b = Build("m10");
            double clock = 0;
            int frames = 0;
            while (!b.Sim.Done && frames++ < 200000) StepRealTime(b, ref clock);
            Assert.IsTrue(b.Sim.Done);
            Assert.That(StageSkip.NextAscentTarget(b.Sim, b.Checks), Is.EqualTo(StageSkipTarget.None));
            var run = StageSkip.Advance(b.Flight, b.Checks, StageSkip.MaxStepsPerFrame);
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Complete));
            Assert.That(run.Steps, Is.EqualTo(0));
        }

        [Test]
        public void SkipSamplesTelemetryEveryTenthOfASimSecond()
        {
            // Area C, bug 3: the frame sampler runs once per rendered frame, so a mostly-skipped flight starved
            // the debrief log to ~5 bars. The pump's sink gets one sample per 0.1 sim-seconds from the real
            // stepped state — dense because the sim ran, not because frames ran.
            const string id = "m3";
            var skipped = Build(id);
            var samples = new System.Collections.Generic.List<AscentSample>();
            int frames = 0;
            StageSkipRun run = null;
            while (frames++ < 10000)
            {
                run = StageSkip.Advance(skipped.Flight, skipped.Checks, StageSkip.MaxStepsPerFrame, s => samples.Add(s));
                if (run.Stop != StageSkipStop.Budget) break;
            }
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Target), "m3 skips to Max-Q");
            Assert.That(run.Reached, Is.EqualTo(StageSkipTarget.MaxQ));
            Assert.That(frames, Is.LessThan(20), "a skip is a few frames, not a flight");
            Assert.That(samples.Count, Is.GreaterThanOrEqualTo((int)(run.SimSeconds / StageSkip.SampleIntervalS) - 1),
                "one sample per 0.1 sim-seconds of skipped time, not one per frame");
            Assert.That(samples.Count, Is.GreaterThan(frames * 10), "density comes from sim seconds, not frames");
            for (int i = 1; i < samples.Count; i++)
            {
                double gap = samples[i].T - samples[i - 1].T;
                Assert.That(samples[i].T, Is.GreaterThan(samples[i - 1].T), "times climb");
                Assert.That(gap, Is.GreaterThan(0.05).And.LessThan(0.16), "sample " + i + " gap " + gap);
            }

            // The samples are the sim's own state, not fills: step a real-time run to each sample's time and
            // the state there is bit-identical.
            var real = Build(id);
            foreach (var s in samples)
            {
                while (real.Sim.T < s.T && !real.Sim.Done)
                {
                    int before = real.Sim.Events.Count;
                    real.Flight.Step();
                    real.Checks.NoteEvents(real.Sim, before);
                    if (real.Checks.StopForBreakup(real.Sim)) break;
                }
                Assert.That(BitConverter.DoubleToInt64Bits(real.Sim.T), Is.EqualTo(BitConverter.DoubleToInt64Bits(s.T)),
                    "sample T is a real step time");
                var rs = real.Flight.Sample();
                Assert.That(BitConverter.DoubleToInt64Bits(rs.AltM), Is.EqualTo(BitConverter.DoubleToInt64Bits(s.AltM)), "alt at " + s.T);
                Assert.That(BitConverter.DoubleToInt64Bits(rs.SpeedMps), Is.EqualTo(BitConverter.DoubleToInt64Bits(s.SpeedMps)), "speed at " + s.T);
                Assert.That(BitConverter.DoubleToInt64Bits(rs.QPa), Is.EqualTo(BitConverter.DoubleToInt64Bits(s.QPa)), "q at " + s.T);
            }
        }

        [Test]
        public void StepBudgetKeepsTheSkipAcrossFrames()
        {
            var b = Build("m10");
            var run = StageSkip.Advance(b.Flight, b.Checks, 10);
            Assert.That(run.Stop, Is.EqualTo(StageSkipStop.Budget), "ten steps cannot reach Max-Q");
            Assert.That(run.Steps, Is.EqualTo(10));
            Assert.That(run.SimSeconds, Is.GreaterThan(0));
            Assert.IsFalse(b.Sim.Done);
        }
    }
}
