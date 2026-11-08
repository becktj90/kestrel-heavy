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
        /// <summary>The player turned warp off (<see cref="TimeWarpSetting"/> Off).</summary>
        public bool Disabled;
        /// <summary>Narrator Voice: a non-interruptible line (mission control, event-bound host) is playing, or ended
        /// under <see cref="NarrationWarpHold.TailS"/> real seconds ago (<see cref="NarrationWarpHold"/>).</summary>
        public bool NarrationBusy;
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

        /// <summary>Real seconds of warning Sound gets before a drop to ×1 (<see cref="WarpedFlightClock.WarpWillDrop"/>).</summary>
        public const double WillDropLeadRealS = 0.5;
        /// <summary>Narrator Voice: real seconds of quiet an interruptible line needs after it ends, before the next call.</summary>
        public const double LineTailS = 1.5;

        /// <summary>A blocker that holds ×1 whatever the clear time ahead.</summary>
        public static bool Blocked(in CoastWarpState s)
            => s.Disabled || s.SlowMo || s.CameraOnBooster || s.OpenCalls > 0 || s.NarrationBusy;

        public static double Factor(in CoastWarpState s)
        {
            if (Blocked(s)) return Off;
            double next = s.SecondsToNext;
            if (double.IsNaN(next) || next < LeadS) return Off;
            return next < FarS ? Slow : Fast;
        }

        public static int StepCap(double factor) => factor > Off ? WarpStepCap : BaseStepCap;

        /// <summary>The HUD chip: "⏩ ×4" or "⏩ ×10". Empty at ×1.</summary>
        public static string ChipText(double factor) => factor > Off ? "⏩ ×" + ((int)factor).ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

        /// <summary>The chip's accessible text: "Time warp, 10 times". Empty at ×1.</summary>
        public static string AccessibleText(double factor) => factor > Off ? "Time warp, " + ((int)factor).ToString(System.Globalization.CultureInfo.InvariantCulture) + " times" : "";

        /// <summary>
        /// The factor the clock returns to once nothing blocks it, for a lead of <paramref name="simSecondsToNext"/>:
        /// ×1 under <see cref="LeadS"/>, ×4 to <see cref="FarS"/>, ×10 beyond (as <see cref="Factor"/> unblocked).
        /// </summary>
        public static double ResumeFactor(double simSecondsToNext)
        {
            if (double.IsNaN(simSecondsToNext) || simSecondsToNext < LeadS) return Off;
            return simSecondsToNext < FarS ? Slow : Fast;
        }

        /// <summary>
        /// Narrator Voice: an interruptible line of <paramref name="clipSec"/> may start only when it and
        /// <see cref="LineTailS"/> end before the next call, counted in wall seconds at the factor the clock will run at:
        /// the larger of the current <paramref name="factor"/> and, when warp is on, <see cref="ResumeFactor"/>. A brief ×1
        /// (the narration tail, the booster camera, an open call) must not let a long line into a gap warp then shrinks
        /// (Narrator Voice on #436). An unknown or zero lead fits nothing.
        /// </summary>
        public static bool LineFits(double clipSec, double simSecondsToNext, double factor, bool warpEnabled = true)
        {
            if (double.IsNaN(simSecondsToNext)) return false;
            if (double.IsPositiveInfinity(simSecondsToNext)) return true;
            if (!(factor >= Off)) factor = Off;
            if (warpEnabled) factor = Math.Max(factor, ResumeFactor(simSecondsToNext));
            return clipSec + LineTailS <= simSecondsToNext / factor;
        }

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
        /// <summary>Drops to ×1 that came with no lead (an unforeseen event or a hard blocker): WarpWillDrop fired with 0.</summary>
        public int ForcedDrops { get; private set; }

        /// <summary>Sound: the rate changed (from, to, sim time). Fires only on a real change, at the start of the frame
        /// that runs at the new rate.</summary>
        public event Action<int, int, double> WarpChanged;
        /// <summary>
        /// Sound: a drop to ×1 is coming in about <c>realSecondsUntil</c> wall seconds (≤ <see cref="CoastWarp.WillDropLeadRealS"/>).
        /// Always fires before the matching WarpChanged(x→1). When the drop is unforeseen (an event the estimate missed,
        /// a call, the booster camera, a narration hold, the player's switch) it fires with 0 in the same frame, just before.
        /// </summary>
        public event Action<int, float> WarpWillDrop;
        bool warned;

        /// <param name="fairingJettisonAltM">SimOptions.fairingJettisonAltM (0: no fairing event).</param>
        public WarpedFlightClock(DirectedFlight flight, PlayedChecks checks, double fairingJettisonAltM)
        {
            this.flight = flight ?? throw new ArgumentNullException(nameof(flight));
            this.checks = checks;
            fairingArmAltM = fairingJettisonAltM > 0 ? FairingHeatGate.FloorFor(fairingJettisonAltM) : double.NaN;
        }

        /// <summary>One rendered frame. Returns the physics steps run.</summary>
        public int Advance(double frameS, double presentationScale, bool cameraOnBooster, bool narrationBusy = false)
        {
            var sim = flight.Sim;
            MecoThisFrame = false;
            Measure(sim);
            var dir = flight.Director;
            double prev = Factor;
            Factor = CoastWarp.Factor(new CoastWarpState
            {
                OpenCalls = dir != null ? dir.OpenCount : 0,
                CameraOnBooster = cameraOnBooster,
                SlowMo = presentationScale != 1,
                Disabled = Disabled,
                NarrationBusy = narrationBusy,
                SecondsToNext = SecondsToNext,
            });
            Announce(prev, Factor, sim.T);
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

        // Events only: nothing here feeds the clock or the steps.
        void Announce(double prev, double now, double simT)
        {
            if (now != prev)
            {
                if (now == CoastWarp.Off && !warned)
                {
                    ForcedDrops++;
                    WarpWillDrop?.Invoke((int)prev, 0f);
                }
                warned = false;
                WarpChanged?.Invoke((int)prev, (int)now, simT);
            }
            if (now > CoastWarp.Off && !warned)
            {
                // The ×1 band starts LeadS sim seconds before the next call or event.
                double real = (SecondsToNext - CoastWarp.LeadS) / now;
                if (real <= CoastWarp.WillDropLeadRealS)
                {
                    warned = true;
                    WarpWillDrop?.Invoke((int)now, (float)Math.Max(0, real));
                }
            }
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

namespace Kestrel.Core.Flight
{
    /// <summary>The player's time-warp setting (pref <see cref="Pref"/>), saved like kh.audio.commentary. The Settings row
    /// comes from UX after #436.</summary>
    public enum TimeWarpMode
    {
        Auto = 0,
        Off = 1,
    }

    public static class TimeWarpSetting
    {
        public const string Pref = "kh.flight.timewarp";

        public static TimeWarpMode FromInt(int value) => value == 1 ? TimeWarpMode.Off : TimeWarpMode.Auto;

        /// <summary>The saved mode. No pref is Auto.</summary>
        public static TimeWarpMode Resolve(bool hasPref, int pref) => hasPref ? FromInt(pref) : TimeWarpMode.Auto;

        public static string Label(TimeWarpMode mode) => mode == TimeWarpMode.Off ? "Time warp off" : "Time warp auto";
    }

    /// <summary>
    /// Narrator Voice: warp holds ×1 while a non-interruptible line plays (mission control, event-bound host lines) and for
    /// <see cref="TailS"/> real seconds after it ends. Interruptible lines (history, color) may play under warp. Clocked in
    /// real seconds by the caller (NarrationPlayer, unscaled time).
    /// </summary>
    public sealed class NarrationWarpHold
    {
        public const double TailS = 0.5;
        bool playing;
        double until = double.NegativeInfinity;

        public void Note(bool nonInterruptiblePlaying, double nowRealS)
        {
            playing = nonInterruptiblePlaying;
            if (playing) until = nowRealS + TailS;
        }

        public bool Holds(double nowRealS) => playing || nowRealS < until;

        public void Reset() { playing = false; until = double.NegativeInfinity; }
    }
}
