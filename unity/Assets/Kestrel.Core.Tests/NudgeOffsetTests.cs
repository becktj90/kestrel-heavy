// #422 (DECISIONS #79): the Engineer pitch nudge is a bounded offset on the program pitch, slewed at the vehicle's
// attitude rate (RecoverySim.PitchRate, 10°/s, #351). It used to be added to PitchCmd every tick on the legacy pitch
// path (Mode 0 and Mode 1 before the catch), so a held 1° became ~120°/s and the rocket looped over off the pad.
using System;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class NudgeOffsetTests
    {
        static AscentSim Sim(string mission, out SimOptions opts)
        {
            var catalog = StarterFixture.Catalog;
            var m = StarterFixture.Campaign.ById(mission);
            var design = Design.FromJson(StarterFixture.Rocket(mission).BuildDesign());
            var setup = MissionFlightSetup.Build(catalog, m, design, MissionFlightSetup.LiveDt, new MissionFlightSite());
            opts = setup.Options;
            return new AscentSim(setup.Stages, opts);
        }

        static AscentSim Sim(string mission) => Sim(mission, out _);

        static void RunTo(AscentSim s, double t) { while (!s.Done && s.T < t) s.Tick(); }

        [Test]
        public void HeldNudge_OnTheLegacyPath_IsAnOffset_NotAPitchRate()
        {
            // KH-1 Starter flies the legacy program: Mode 0 (vertical) until the 60 m/s kick.
            var a = Sim("m1");
            var b = Sim("m1");
            RunTo(a, 0.5); RunTo(b, 0.5);
            b.LivePitchOffsetRad = 1 * Math.PI / 180;
            for (double t = 0.75; t <= 6; t += 0.25)
            {
                RunTo(a, t); RunTo(b, t);
                if (a.Done || b.Done) break;
                double d = (b.PitchCmd - a.PitchCmd) * 180 / Math.PI;
                // The kick catch can land a tick apart, so allow the kick step itself; never a runaway.
                Assert.LessOrEqual(Math.Abs(d), 10.0, "T+" + t + " nudged pitch " + b.PitchCmd * 180 / Math.PI + "° vs " + a.PitchCmd * 180 / Math.PI + "°");
                if (a.Mode == 0 && b.Mode == 0 && t >= 1) Assert.AreEqual(1.0, d, 1e-9, "Mode 0: exactly program + nudge at T+" + t);
            }
        }

        [Test]
        public void TheOffset_IsBoundedAtMaxNudge()
        {
            var s = Sim("m1");
            RunTo(s, 20);
            s.LivePitchOffsetRad = 45 * Math.PI / 180;
            while (!s.Done && s.T < 22) { s.Tick(); Assert.LessOrEqual(Math.Abs(s.NudgeAppliedRad), AscentSim.MaxNudgeRad + 1e-15); }
            Assert.IsFalse(s.Done);
            Assert.AreEqual(AscentSim.MaxNudgeRad, s.NudgeAppliedRad, 1e-12, "a 45° request holds at the 10° bound");
            s.LivePitchOffsetRad = -45 * Math.PI / 180;
            while (!s.Done && s.T < 24.5) { s.Tick(); Assert.LessOrEqual(Math.Abs(s.NudgeAppliedRad), AscentSim.MaxNudgeRad + 1e-15); }
            Assert.IsFalse(s.Done);
            Assert.AreEqual(-AscentSim.MaxNudgeRad, s.NudgeAppliedRad, 1e-12);
        }

        [Test]
        public void TheOffset_SlewsAtTheVehicleAttitudeRate()
        {
            var s = Sim("m1", out var opts);
            RunTo(s, 0.5);
            s.LivePitchOffsetRad = 10 * Math.PI / 180;
            double prev = s.NudgeAppliedRad;
            double t0 = s.T;
            double max = 0;
            while (s.NudgeAppliedRad < AscentSim.MaxNudgeRad - 1e-12 && !s.Done)
            {
                s.Tick();
                max = Math.Max(max, s.NudgeAppliedRad - prev);
                prev = s.NudgeAppliedRad;
            }
            Assert.LessOrEqual(max, RecoverySim.PitchRate * opts.dt + 1e-15, "never faster than 10°/s per tick");
            Assert.AreEqual(1.0, s.T - t0, 0.05, "10° at 10°/s takes about a second");
            // Releasing it slews back to the program and leaves no residue.
            s.LivePitchOffsetRad = 0;
            RunTo(s, s.T + 1.2);
            Assert.AreEqual(0.0, s.NudgeAppliedRad, 0.0);
        }

        [Test]
        public void NoInput_IsBitIdentical_ToAnUntouchedSim()
        {
            foreach (var id in new[] { "m1", "m2", "m3", "m10" })
            {
                var a = Sim(id);
                var b = Sim(id);
                while (!a.Done) { b.LivePitchOffsetRad = 0; a.Tick(); b.Tick(); }
                Assert.IsTrue(b.Done, id);
                for (int i = 0; i < a.Y.Length; i++) Assert.AreEqual(a.Y[i], b.Y[i], 0.0, id + " state " + i);
                Assert.AreEqual(a.Samples.Count, b.Samples.Count, id);
                for (int i = 0; i < a.Samples.Count; i++) Assert.AreEqual(a.Samples[i].pitch, b.Samples[i].pitch, 0.0, id + " sample " + i);
                Assert.AreEqual(0.0, a.NudgeAppliedRad, 0.0);
            }
        }
    }
}
