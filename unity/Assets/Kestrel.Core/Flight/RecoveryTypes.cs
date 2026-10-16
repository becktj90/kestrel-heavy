// Player-facing booster recovery after stage separation. Pad-fixed state, fixed steps,
// no wall clock. Expendable designs do not enter this phase.
using System;

namespace Kestrel.Core.Flight
{
    public enum RecoveryPhase
    {
        Idle = 0,
        Boostback = 1,
        Coast = 2,
        Entry = 3,
        Descent = 4,
        Landing = 5,
        Down = 6,
    }

    /// <summary>First-stage numbers the recovery burn is allowed to use. Reserve is propellant, not dry mass.</summary>
    public struct RecoveryCraft
    {
        public double DryKg, ReserveKg, ThrustVacN, ThrustSlN, IspSec, DiameterM, Cd;
        public bool GridFins, Legs;
        public string LegsKind, Mode;

        public double VLeg => RecoveryModel.VLeg(Legs ? LegsKind : "light");
        public bool Recoverable => Mode == "barge" || Mode == "rtls";
    }

    /// <summary>Simple leaves Auto set. Engineer supplies throttle and an absolute pitch.</summary>
    public struct RecoveryStick
    {
        public bool Auto;
        public double Throttle, PitchRad;

        public static RecoveryStick Assisted => new RecoveryStick { Auto = true };
    }

    public struct RecoverySample
    {
        public double T, Alt, Speed, Mass, Prop, Pitch, Throttle, Q;
        public double Sink, TiltDeg, MissM, East, Radial, Arc;
        public RecoveryPhase Phase;
        public double Fins, Legs01;
        public bool FuelEmpty;
    }

    public struct RecoveryCommand
    {
        public double Pitch, Throttle;
        public bool Fins, Legs;
        public RecoveryPhase Phase;
    }

    /// <summary>One flown recovery. Failure is null when the booster meets the deck limits.</summary>
    public sealed class RecoveryVerdict
    {
        public bool Attempted;
        public string Cause, GradeName, Medal;
        public double Sink, TiltDeg, MissM, T, PropLeft;
        public FailureEvent Failure;
        public RecoverySample End;
        public RecoveryMark[] Marks = Array.Empty<RecoveryMark>();
    }

    public struct RecoveryMark
    {
        public RecoveryPhase Phase;
        public double T, Alt, Speed, Sink, TiltDeg, MissM, Prop;
    }
}
