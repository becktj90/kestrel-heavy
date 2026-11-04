// PR5 (UX #416 item 6): coast time-warp. Long burns and coasts with nothing to decide play faster than real time.
// Warp only changes how many fixed 1/120 s physics steps run per rendered frame: the step, the sim, the director and the
// played checks are the same code in the same order, so a warped flight ends bit-identical to an unwarped one
// (CoastWarpTests.WarpedFlightIsBitIdentical). Warp drops to ×1 a few sim seconds before every call and every
// predictable event (burnout, separation, fairing, cutoff, Kármán line), and stops the frame on the step any
// unpredicted event or call lands (a random engine failure), so the player sees each one at ×1.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    /// <summary>What the warp policy reads each frame.</summary>
    public struct CoastWarpState
    {
        /// <summary>Calls open on the HUD (AscentDirector.OpenCount).</summary>
        public int OpenCalls;
        /// <summary>The camera follows the returning booster (RecoveryShow.SteerCamera).</summary>
        public bool CameraOnBooster;
        /// <summary>A cinematic slow-mo is on (presentation scale not 1).</summary>
        public bool SlowMo;
        /// <summary>The player turned warp off.</summary>
        public bool Disabled;
        /// <summary>Sim seconds to the next call or predictable event. 0 when one is due now or the estimate is unknown.</summary>
        public double SecondsToNext;
    }

    public static class CoastWarp
    {
        public const double Off = 1, Slow = 4, Fast = 10;
        /// <summary>Sim seconds of clear flight ahead needed to warp at all. Inside this the flight runs at ×1.</summary>
        public const double LeadS = 5;
        /// <summary>Sim seconds of clear flight ahead needed for ×10; between <see cref="LeadS"/> and this, ×4.</summary>
        public const double FarS = 30;
        /// <summary>The unwarped per-frame step cap (the old FlightDriver guard).</summary>
        public const int BaseStepCap = 64;
        /// <summary>
        /// Warped per-frame cap: ×10 over the 0.1 s hitch clamp is 1 sim second, 120 steps at 1/120 s. 160 keeps headroom;
        /// a step costs microseconds, so this stays well inside a frame.
        /// </summary>
        public const int WarpStepCap = 160;

        public static double Factor(in CoastWarpState s)
        {
            if (s.Disabled || s.SlowMo || s.CameraOnBooster || s.OpenCalls > 0) return Off;
            double next = s.SecondsToNext;
            if (double.IsNaN(next) || next < LeadS) return Off;
            return next < FarS ? Slow : Fast;
        }

        public static int StepCap(double factor) => factor > Off ? WarpStepCap : BaseStepCap;

        /// <summary>Sim seconds until a value moving at <paramref name="rate"/> per second covers <paramref name="gap"/>.
        /// 0 when already there; +Infinity when it is not moving toward it or either value is unknown.</summary>
        public static double Until(double gap, double rate)
        {
            if (double.IsNaN(gap) || double.IsNaN(rate)) return double.PositiveInfinity;
            if (gap <= 0) return 0;
            return rate > 0 ? gap / rate : double.PositiveInfinity;
        }
    }

    /// <summary>
    /// The live flight's presentation clock with coast warp: advances by the frame's wall time × presentation scale ×
    /// warp factor, and runs the fixed physics steps (DirectedFlight.Step, then the PlayedChecks, as the pad always has)
    /// until the sim catches up. FlightDriver owns one per flight; tests drive it with any frame pattern.
    /// </summary>
    public sealed class WarpedFlightClock
    {
        readonly DirectedFlight flight;
        readonly PlayedChecks checks;
        readonly double fairingArmAltM;
        double lastT = double.NaN, lastProp = double.NaN, lastAlt = double.NaN, lastMargin = double.NaN;
        int lastStage = -1;
        double propRate = double.NaN, climb = double.NaN, marginRate = double.NaN;
        bool fairingSeen, karmanSeen;
        int seenEvents;

        public double Clock { get; private set; }
        /// <summary>The factor the last <see cref="Advance"/> ran at.</summary>
        public double Factor { get; private set; } = CoastWarp.Off;
        /// <summary>Sim seconds to the next call or predictable event, as of the last frame.</summary>
        public double SecondsToNext { get; private set; }
        /// <summary>True on the frame the core burned out (PlayedChecks.NoteEvents).</summary>
        public bool MecoThisFrame { get; private set; }
        public bool Disabled { get; set; }

        /// <param name="fairingJettisonAltM">SimOptions.fairingJettisonAltM (0: no fairing event).</param>
        public WarpedFlightClock(DirectedFlight flight, PlayedChecks checks, double fairingJettisonAltM)
        {
            this.flight = flight ?? throw new ArgumentNullException(nameof(flight));
            this.checks = checks;
            fairingArmAltM = fairingJettisonAltM > 0 ? FairingHeatGate.FloorFor(fairingJettisonAltM) : double.NaN;
        }

        /// <summary>One rendered frame. Returns the physics steps run.</summary>
        public int Advance(double frameS, double presentationScale, bool cameraOnBooster)
        {
            var sim = flight.Sim;
            MecoThisFrame = false;
            Measure(sim);
            var dir = flight.Director;
            Factor = CoastWarp.Factor(new CoastWarpState
            {
                OpenCalls = dir != null ? dir.OpenCount : 0,
                CameraOnBooster = cameraOnBooster,
                SlowMo = presentationScale != 1,
                Disabled = Disabled,
                SecondsToNext = SecondsToNext,
            });
            Clock += new PresentationTimeScale(presentationScale).ToSimSeconds(Math.Min(frameS, 0.1)) * Factor;
            int cap = CoastWarp.StepCap(Factor), steps = 0;
            bool warped = Factor > CoastWarp.Off;
            while (sim.T < Clock && !sim.Done && steps < cap)
            {
                int before = sim.Events.Count;
                int open = dir != null ? dir.OpenCount : 0;
                flight.Step();
                steps++;
                if (checks != null && checks.NoteEvents(sim, before)) MecoThisFrame = true;
                if (checks != null && checks.StopForBreakup(sim)) break;
                // Backstop: an event or call the estimate missed ends the warped frame on its own step.
                if (warped && (sim.Events.Count != before || (dir != null && dir.OpenCount > open))) { Clock = sim.T; break; }
            }
            // A warped frame never banks a debt of sim time to replay later.
            if (warped && Clock > sim.T) Clock = sim.T;
            return steps;
        }

        void Measure(AscentSim sim)
        {
            var ev = sim.Events;
            for (; seenEvents < ev.Count; seenEvents++)
            {
                if (ev[seenEvents].type == "fairing") fairingSeen = true;
            }
            double x = sim.Y[0], y = sim.Y[1];
            double r = Math.Sqrt(x * x + y * y);
            double alt = r - Gravity.R_EARTH;
            if (alt >= UpperAtmosphere.KarmanM) karmanSeen = true;
            double prop = sim.PropellantFraction, margin = sim.CutoffEnergyMargin;
            int stage = sim.StageIdx;
            double dt = sim.T - lastT;
            if (dt > 1e-9)
            {
                propRate = stage == lastStage && !double.IsNaN(prop) && !double.IsNaN(lastProp) ? (lastProp - prop) / dt : double.NaN;
                climb = (alt - lastAlt) / dt;
                marginRate = stage == lastStage && !double.IsNaN(margin) && !double.IsNaN(lastMargin) ? (margin - lastMargin) / dt : double.NaN;
                lastT = sim.T; lastProp = prop; lastAlt = alt; lastMargin = margin; lastStage = stage;
            }
            else if (double.IsNaN(lastT)) { lastT = sim.T; lastProp = prop; lastAlt = alt; lastMargin = margin; lastStage = stage; }

            double next = double.PositiveInfinity;
            if (sim.Thrusting)
            {
                // No rate yet on this stage: no estimate, no warp.
                if (double.IsNaN(propRate)) next = 0;
                else next = Math.Min(next, CoastWarp.Until(prop, propRate));
                if (!double.IsNaN(margin)) next = Math.Min(next, double.IsNaN(marginRate) ? 0 : CoastWarp.Until(-margin, marginRate));
            }
            if (!fairingSeen && !double.IsNaN(fairingArmAltM)) next = Math.Min(next, CoastWarp.Until(fairingArmAltM - alt, climb));
            if (!karmanSeen) next = Math.Min(next, CoastWarp.Until(UpperAtmosphere.KarmanM - alt, climb));
            var dir = flight.Director;
            if (dir != null) next = Math.Min(next, dir.SecondsToNextArm(propRate, climb));
            SecondsToNext = next;
        }
    }
}
