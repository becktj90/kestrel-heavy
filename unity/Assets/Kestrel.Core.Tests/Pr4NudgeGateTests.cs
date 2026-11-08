// DECISIONS #80 (fun review PR4): the Engineer pitch nudge opens only once the rocket is clear of the tower.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class Pr4NudgeGateTests
    {
        static AscentSample At(double t, double v) => new AscentSample
        {
            T = t, AltM = 5 * t * t, SpeedMps = v, QPa = 0.6 * v * v, G = 1.5, ThrustN = 8e6, MassKg = 4e5, Thrusting = true, PropFraction = 0.9,
        };

        [Test]
        public void BeforeTheTower_ANudgeIsNotPossible_NotHeldOrQueued()
        {
            var d = AscentDirector.Start(new AscentVehicle(), null, null, true, true, false);
            Assert.IsFalse(d.NudgeOpen, "closed on the pad");
            d.SetNudge(-1);
            d.NudgePitch(+1);
            d.Advance(At(0.5, 5), 0.05);
            d.NudgePitch(-3);
            d.Advance(At(4, AscentDecisions.NudgeGateMps - 0.01), 0.05);
            Assert.IsFalse(d.NudgeOpen);
            Assert.AreEqual(0, d.NudgeDeg, 0, "nothing set before the gate");
            Assert.AreEqual(0, d.Command.PitchOffsetRad, 0, "the flight never sees it");
            Assert.IsFalse(d.Track.Nudged);
            d.Advance(At(4.6, AscentDecisions.NudgeGateMps), 0.05);
            Assert.IsTrue(d.NudgeOpen, "opens at the gate speed");
            Assert.AreEqual(0, d.NudgeDeg, 0, "an earlier press is not replayed when it opens");
            Assert.AreEqual(0, d.Command.PitchOffsetRad, 0);
        }

        [Test]
        public void AfterTheTower_TheNudgeWorks_AndStaysOpenIfTheRocketSlows()
        {
            var d = AscentDirector.Start(new AscentVehicle(), null, null, true, true, false);
            d.Advance(At(5, 120), 0.05);
            d.NudgePitch(+1);
            Assert.AreEqual(1, d.NudgeDeg, 1e-12);
            d.Advance(At(5.1, 60), 0.05);
            Assert.IsTrue(d.NudgeOpen, "latched: a slowdown never takes the row away");
            d.NudgePitch(+1);
            Assert.AreEqual(2, d.NudgeDeg, 1e-12);
            Assert.AreEqual(2 * System.Math.PI / 180.0, d.Command.PitchOffsetRad, 1e-12);
        }

        [Test]
        public void ThePitchCall_WaitsForTheGateToo()
        {
            var d = AscentDirector.Start(new AscentVehicle(), null, null, false, true, false);
            // Above 1500 m and 70 m/s (the call's own arming line) but not yet at the gate speed: a slow heavy lifter (m18).
            d.Advance(new AscentSample { T = 40, AltM = 1600, SpeedMps = 90, Thrusting = true, PropFraction = 0.9 }, 0.05);
            Assert.IsFalse(d.IsOpen(AscentCall.Pitch));
            Assert.IsFalse(d.Choose(AscentCall.Pitch, true));
            Assert.AreEqual(0, d.NudgeDeg, 0);
            d.Advance(new AscentSample { T = 45, AltM = 2000, SpeedMps = 101, Thrusting = true, PropFraction = 0.9 }, 0.05);
            Assert.IsTrue(d.IsOpen(AscentCall.Pitch));
            Assert.IsTrue(d.Choose(AscentCall.Pitch, true));
            // DECISIONS #76: accept confirms the autopilot's pitch program and holds no offset;
            // the -1 deg nudge it used to hold is gone. Manual deviation stays on the Engineer nudge row.
            Assert.AreEqual(0, d.NudgeDeg, 1e-12);
        }

        [Test]
        public void TheGatedNote_FitsTheNotesSheet()
        {
            Assert.LessOrEqual(AscentNotes.NudgeGated.Length, AscentNotes.MaxChars);
            Assert.AreEqual(100, AscentDecisions.NudgeGateMps, 0, "DECISIONS #80");
        }
    }
}
