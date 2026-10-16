// Event → visual state for flight staging. Sim time only: the same events and the same t
// always produce the same plume, pose and callout. Flight Thrum is not spoken from here.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightStagingTests
    {
        [Test]
        public void PlanReleasesBoostersThenTheCoreAndKeepsTheLastStage()
        {
            var serial = new List<SerialStage>
            {
                new SerialStage { role = "strapons+core", label = "Core + 2 strap-ons" },
                new SerialStage { role = "core", index = 0, label = "Stage 1" },
                new SerialStage { role = "core", index = 1, label = "Stage 2" },
            };
            var bodies = FlightStagingPlan.Bodies(serial, 2);
            Assert.AreEqual(4, bodies.Count);
            Assert.AreEqual("booster", bodies[0].Role);
            Assert.AreEqual(0, bodies[0].Serial);
            Assert.AreEqual(0, bodies[0].Index);
            Assert.IsTrue(bodies[0].Releases);
            Assert.AreEqual(1, bodies[1].Index);
            Assert.AreEqual("stage", bodies[2].Role);
            Assert.AreEqual(1, bodies[2].Serial);
            Assert.AreEqual(0, bodies[2].Index);
            Assert.IsTrue(bodies[2].Releases);
            Assert.AreEqual(2, bodies[3].Serial);
            Assert.IsFalse(bodies[3].Releases);
        }

        [Test]
        public void SameTimestampClusterShowsMecoThenSepThenIgnition()
        {
            var events = Cluster(80, 42000, 1860, 1);
            var a = Play(events, false, 0);
            var b = Play(events, false, 0);

            var early = Sample(a, events, 80.05, 42000, 1860, 12000, 1);
            Assert.AreEqual(StagingKind.Meco, early.Call.Kind);
            Assert.AreEqual("MECO", early.Call.Title);
            Assert.That(early.Call.Line, Does.Contain("T+80.0"));
            Assert.That(early.Call.Line, Does.Contain("42.0km"));
            Assert.That(early.Call.Line, Does.Contain("1.86km/s"));
            Assert.AreEqual(42000, early.Call.AltM, 1e-6);
            Assert.AreEqual(1860, early.Call.SpeedMps, 1e-6);
            Assert.That(early.Plume.Scale, Is.LessThan(0.95));
            Assert.That(early.Plume.Heat, Is.GreaterThan(0.05));
            Assert.IsTrue(early.Plume.Lit);
            Assert.IsFalse(a.Released(0, 80.05));

            var sepT = 80 + FlightStaging.SepDelaySec + 0.05;
            var mid = Sample(a, events, sepT, 43000, 1900, 8000, 1);
            Assert.AreEqual(StagingKind.StageSep, mid.Call.Kind);
            Assert.AreEqual("STAGE SEP", mid.Call.Title);
            Assert.IsTrue(a.Released(0, sepT));
            var pose = a.Pose("stage", 0, 0, sepT + 1.0);
            Assert.IsTrue(pose.Released);
            Assert.That(pose.Y, Is.LessThan(-1));
            Assert.That(pose.Pitch, Is.GreaterThan(10));

            var ignT = 80 + FlightStaging.IgniteDelaySec + 0.05;
            var late = Sample(a, events, ignT, 45000, 2000, 4000, 1);
            Assert.AreEqual(StagingKind.Ignition, late.Call.Kind);
            Assert.AreEqual("ses1", late.Call.NarrationId);
            Assert.That(late.Plume.Flash, Is.GreaterThan(0.5));
            Assert.That(late.Plume.Scale, Is.GreaterThan(0.05));
            Assert.AreEqual(1, late.Plume.Serial);

            var again = Sample(b, events, ignT, 45000, 2000, 4000, 1);
            Assert.AreEqual(late.Plume.Scale, again.Plume.Scale, 1e-12);
            Assert.AreEqual(late.Plume.Flash, again.Plume.Flash, 1e-12);
            Assert.AreEqual(pose.X, b.Pose("stage", 0, 0, sepT + 1.0).X, 1e-12);
            Assert.AreEqual(pose.Pitch, b.Pose("stage", 0, 0, sepT + 1.0).Pitch, 1e-12);
        }

        [Test]
        public void CalloutKeepsTheEventNumbersWhileTheSimMovesOn()
        {
            var events = Cluster(80, 42000, 1860, 1);
            var staging = Play(events, false, 0);
            Sample(staging, events, 80.2, 99000, 4000, 1000, 1);
            Assert.IsTrue(staging.TryCallout(80.2, out var call));
            Assert.AreEqual(80, call.T, 1e-9);
            Assert.AreEqual(42000, call.AltM, 1e-6);
            Assert.AreEqual(1860, call.SpeedMps, 1e-6);
            Assert.That(call.Line, Does.Not.Contain("99.0km"));
        }

        [Test]
        public void BoostersFallApartAndTheCoreStaysUntilItsOwnSeparation()
        {
            var events = new List<SimEvent>();
            events.AddRange(Cluster(50, 28000, 1400, 1));
            events.AddRange(Cluster(120, 70000, 2400, 2));
            var staging = Play(events, false, 2);
            double fell = 50 + FlightStaging.SepDelaySec + 1;
            Sample(staging, events, fell, 30000, 1500, 2000, 1);
            Assert.IsTrue(staging.Released(0, fell));
            Assert.IsFalse(staging.Released(1, fell));
            var left = staging.Pose("booster", 0, 0, fell);
            var right = staging.Pose("booster", 1, 0, fell);
            Assert.That(left.X, Is.GreaterThan(1));
            Assert.That(right.X, Is.LessThan(-1));
            Assert.That(left.Y, Is.LessThan(0));
            Assert.AreEqual(left.Y, right.Y, 1e-9);
            Assert.IsFalse(staging.Pose("stage", 0, 1, fell).Released);

            double core = 120 + FlightStaging.SepDelaySec + 0.5;
            Sample(staging, events, core, 72000, 2500, 100, 1);
            Assert.IsTrue(staging.Released(1, core));
            var stage = staging.Pose("stage", 0, 1, core);
            Assert.IsTrue(stage.Released);
            Assert.That(stage.Y, Is.LessThan(0));
            var later = staging.Pose("stage", 0, 1, core + 2);
            Assert.That(later.Y, Is.LessThan(stage.Y));
        }

        [Test]
        public void FinalCutoffCollapsesThePlumeAndDoesNotSeparate()
        {
            var events = new List<SimEvent>
            {
                new SimEvent { t = 200, type = "cutoff", stage = 2, alt = 180000, v = 7600 },
            };
            var staging = Play(events, false, 0);
            var dying = Sample(staging, events, 200.1, 180000, 7600, 0, 0);
            Assert.AreEqual(StagingKind.Meco, dying.Call.Kind);
            Assert.That(dying.Plume.Scale, Is.LessThan(0.9));
            Assert.That(dying.Plume.Heat, Is.GreaterThan(0));
            var outT = 200 + FlightStaging.CollapseSec + FlightStaging.AfterglowSec + 0.05;
            var done = Sample(staging, events, outT, 181000, 7600, 0, 0);
            Assert.AreEqual(0, done.Plume.Scale, 1e-9);
            Assert.IsFalse(done.Plume.Lit);
            Assert.IsFalse(staging.Released(1, outT));
            double afterCall = 200 + FlightStaging.CalloutSec + 0.05;
            Sample(staging, events, afterCall, 181000, 7600, 0, 0);
            Assert.IsFalse(staging.TryCallout(afterCall, out _));

            var sink = Drain(staging);
            Assert.IsFalse(sink.Spoke);
            Assert.AreEqual("", sink.Spoken);
            CollectionAssert.Contains(sink.Offered, "meco");
            CollectionAssert.DoesNotContain(sink.Offered, "sep");
            CollectionAssert.DoesNotContain(sink.Offered, "ses1");
        }

        [Test]
        public void MediumLifterSimSeparatesTheFirstStageAndRelights()
        {
            var result = Headless.Simulate(Headless.MediumLifter(), new SimOptions { dt = 0.25, tMax = 500, cutoff = true });
            SimEvent burn = null, sep = null, ign = null;
            for (int i = 0; i < result.events.Count; i++)
            {
                var e = result.events[i];
                if (burn == null && e.type == "burnout") burn = e;
                if (sep == null && e.type == "separation") sep = e;
                if (burn != null && ign == null && e.type == "ignition" && e.t > 1) ign = e;
            }
            Assert.IsNotNull(burn, "sim burnout");
            Assert.IsNotNull(sep, "sim separation");
            Assert.IsNotNull(ign, "sim next ignition");
            Assert.AreEqual(burn.stage, sep.stage, "separation.stage matches the spent burnout stage");
            Assert.AreEqual(1, sep.stage.Value);
            int spent = sep.stage.Value - 1;

            var staging = Play(result.events, false, 0);
            double alt = burn.alt ?? 0;
            double spd = burn.v ?? 0;
            Sample(staging, result.events, burn.t + 0.05, alt, spd, 1000, 0);
            Assert.IsFalse(staging.Released(spent, burn.t + 0.05));
            Assert.That(staging.Plume(burn.t + 0.1, 1).Heat, Is.GreaterThan(0));
            Sample(staging, result.events, sep.t + FlightStaging.SepDelaySec + 0.05, alt, spd, 100, 1);
            Assert.IsTrue(staging.Released(spent, sep.t + FlightStaging.SepDelaySec + 0.05));
            var pose = staging.Pose("stage", 0, spent, sep.t + FlightStaging.SepDelaySec + 1);
            Assert.That(pose.Y, Is.LessThan(-1));
            var lit = staging.Plume(ign.t + FlightStaging.IgniteDelaySec + FlightStaging.SpoolSec, 1);
            Assert.That(lit.Scale, Is.GreaterThan(0.8));
            Assert.AreEqual(ign.stage.Value - 1, lit.Serial);
            Assert.IsFalse(staging.Released(spent + 1, result.time));
        }

        [Test]
        public void FairingWaitsForMaxQThenAltitudeAndPeels()
        {
            var staging = new FlightStaging();
            staging.Reset(true, 110000, 0, false);
            var none = new List<SimEvent>();
            staging.Advance(none, 10, 120000, 1000, 5000, 1);
            staging.Advance(none, 20, 130000, 1500, 20000, 1);
            Assert.IsFalse(staging.FairingAt(20).Separated, "still climbing through max-Q");
            staging.Advance(none, 40, 150000, 2200, 15000, 1);
            var peel = staging.FairingAt(40);
            Assert.IsTrue(peel.Separated);
            Assert.AreEqual(0, peel.YawDeg, 1e-9);
            var open = staging.FairingAt(40 + FlightStaging.FairingSec);
            Assert.AreEqual(FlightStaging.FairingPeelDeg, open.YawDeg, 1e-6);
            Assert.AreEqual(FlightStaging.FairingOutM, open.OutM, 1e-6);
            Assert.AreEqual(FlightStaging.FairingDropM, open.DropM, 1e-6);
            Assert.IsTrue(staging.TryCallout(40.1, out var call));
            Assert.AreEqual(StagingKind.FairingSep, call.Kind);
            Assert.AreEqual("FAIRING SEP", call.Title);
            Assert.AreEqual("fairing", call.NarrationId);
            Assert.AreEqual(150000, call.AltM, 1e-6);

            var sink = Drain(staging);
            Assert.IsFalse(sink.Spoke);
            CollectionAssert.Contains(sink.Offered, "fairing");
            Assert.AreEqual(1, sink.Offered.Count, "the condition offers once");
            staging.Advance(none, 50, 160000, 2300, 1000, 1);
            Assert.IsFalse(staging.TakeCue(out _));
        }

        [Test]
        public void FairingEventFiresEvenBeforeTheAltitudeCondition()
        {
            var events = new List<SimEvent>
            {
                new SimEvent { t = 5, type = "fairing", alt = 1000, v = 250 },
            };
            var staging = Play(events, true, 0);
            Sample(staging, events, 5, 1000, 250, 40000, 1);
            Assert.IsTrue(staging.FairingAt(5).Separated);
            Assert.IsTrue(staging.TryCallout(5.1, out var call));
            Assert.AreEqual("FAIRING SEP", call.Title);
            Assert.That(call.Line, Does.Contain("T+5.0"));
            Assert.That(call.Line, Does.Contain("1.0km"));
            Assert.That(call.Line, Does.Contain("250m/s"));
        }

        [Test]
        public void LiftoffIgnitionIsNotACallout()
        {
            var events = new List<SimEvent> { new SimEvent { t = 0, type = "ignition", stage = 1 } };
            var staging = Play(events, false, 0);
            Sample(staging, events, 1, 100, 40, 100000, 1);
            Assert.IsFalse(staging.TryCallout(1, out _));
            Assert.AreEqual(0, staging.CueCount);
            var plume = staging.Plume(1, 0.8);
            Assert.AreEqual(0.8, plume.Scale, 1e-9);
            Assert.AreEqual(0, plume.Flash, 1e-9);
        }

        [Test]
        public void ReducedMotionFallsWithoutASpin()
        {
            var events = Cluster(10, 1000, 300, 1);
            var staging = Play(events, false, 2);
            staging.Reset(false, 0, 2, true);
            double t = 10 + FlightStaging.SepDelaySec + 1;
            Sample(staging, events, t, 1000, 300, 1000, 1);
            var pose = staging.Pose("booster", 0, 0, t);
            Assert.IsTrue(pose.Released);
            Assert.That(pose.Y, Is.LessThan(-1));
            Assert.AreEqual(0, pose.Pitch, 1e-9);
            Assert.AreEqual(0, pose.Yaw, 1e-9);
            Assert.AreEqual(0, pose.Roll, 1e-9);
            Assert.AreEqual(0, staging.Plume(10 + FlightStaging.IgniteDelaySec + 0.1, 1).Flash, 1e-9);
        }

        [Test]
        public void RepeatingTheSameTimeDoesNotOfferTwice()
        {
            var events = Cluster(30, 10000, 800, 1);
            var staging = Play(events, false, 0);
            Sample(staging, events, 30.1, 10000, 800, 1000, 1);
            var sink = Drain(staging);
            Assert.AreEqual(3, sink.Offered.Count);
            Sample(staging, events, 30.1, 10000, 800, 1000, 1);
            Assert.IsFalse(staging.TakeCue(out _));
            Assert.IsFalse(sink.Spoke);
        }

        [Test]
        public void CalloutFitsThePortraitGapAboveTheRocket()
        {
            string line = FlightStaging.Format(FlightStaging.Title(StagingKind.FairingSep), 999.9, 999900, 9990);
            float width = FlightHudLayout.TextWidth(line, FlightHudLayout.LabelFontPt);
            float content = 375f - 2f * FlightHudLayout.SideMarginPt;
            Assert.That(width, Is.LessThanOrEqualTo(content), line);
            Assert.That(line.Length, Is.LessThan(48));

            const float worstClockPt = 56f;
            foreach (var d in FlightHudLayout.PortraitDevices())
            {
                float screenTop = d.SafeTop + worstClockPt + FlightStaging.CalloutTopInsideHud();
                float screenBottom = screenTop + FlightStaging.CalloutPt;
                Assert.That(screenBottom, Is.LessThan(d.H * FlightHudLayout.StackTop - 4f), d.Name + " " + d.SafeTop);
                var frame = FlightHudLayout.Place(d, FlightHudMode.Cruise);
                Assert.That(screenBottom, Is.LessThan(frame.Dock.Y), d.Name);
                Assert.That(d.SafeTop + worstClockPt + FlightHudLayout.GapPt + FlightHudLayout.TelemetryPt, Is.LessThan(screenTop), d.Name);
            }
        }

        static List<SimEvent> Cluster(double t, double alt, double speed, int spentStage)
        {
            return new List<SimEvent>
            {
                new SimEvent { t = t, type = "burnout", stage = spentStage, alt = alt, v = speed },
                new SimEvent { t = t, type = "separation", stage = spentStage, alt = alt, v = speed },
                new SimEvent { t = t, type = "ignition", stage = spentStage + 1, alt = alt, v = speed },
            };
        }

        static FlightStaging Play(IList<SimEvent> events, bool fairing, int boosters)
        {
            var staging = new FlightStaging();
            staging.Reset(fairing, 110000, boosters, false);
            return staging;
        }

        struct Sampled
        {
            public PlumeRead Plume;
            public StagingCallout Call;
        }

        static Sampled Sample(FlightStaging staging, IList<SimEvent> events, double t, double alt, double speed, double q, double thrust)
        {
            staging.Advance(events, t, alt, speed, q, thrust);
            var read = staging.Plume(t, thrust);
            staging.TryCallout(t, out var call);
            return new Sampled { Plume = read, Call = call };
        }

        static SilentFlightNarration Drain(FlightStaging staging)
        {
            var sink = new SilentFlightNarration();
            while (staging.TakeCue(out var cue)) sink.Offer(cue);
            return sink;
        }
    }
}
