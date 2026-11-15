// "Release the clamps" (DECISIONS #100). At ignition the engines spool up on the pad and a gauge climbs into a green
// zone; the player taps to release the hold-downs. Pure state machine on the count clock (game seconds, T-0 = 0;
// after T-0 the caller keeps advancing the same clock while the count holds at zero).
//
// Honest physics, and the flight never changes:
//  - AscentSim starts at its own t = 0 with the engines at full thrust, as today. The tap only picks WHEN that start
//    happens on the wall clock; the trajectory from the start is the same flight bit for bit (ClampReleaseTests flies it).
//  - Clean: a tap in the green (full thrust) releases now.
//  - Early: before full thrust the sequencer will not release ("thrust not in the gate", the timeline's ign.thrustok
//    beat). It holds the command until full thrust and one recycle beat (EarlyHesitateS), then releases. The gauge reads
//    the live T/W = thrust share x the design's liftoff T/W, so below 1 it says the stack would sit on the pad. No damage.
//  - Late: nobody tapped and the green closed: the sequencer auto-releases, with no timing credit.
//  - Kid / Auto-assist (forgiving): a wider green before T-0 and the auto-release exactly at T-0, today's timing.
//  - Skip count releases at once, no credit (the old path).
using System;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public enum ClampPhase
    {
        /// <summary>Before ignition: no gauge.</summary>
        Idle = 0,
        /// <summary>Engines lit, thrust building.</summary>
        Spooling = 1,
        /// <summary>Full thrust, the release window is open.</summary>
        Green = 2,
        /// <summary>The release is commanded (or held by the sequencer after an early tap); liftoff is due at <see cref="ClampRelease.LiftoffAt"/>.</summary>
        Released = 3,
    }

    public enum ClampOutcome
    {
        None = 0,
        /// <summary>A tap in the green.</summary>
        Clean = 1,
        /// <summary>A tap before full thrust: held to full thrust plus a beat.</summary>
        Early = 2,
        /// <summary>Nobody tapped: auto-release when the green closed (standard window).</summary>
        Late = 3,
        /// <summary>Kid / Auto-assist auto-release at T-0.</summary>
        Auto = 4,
        /// <summary>Skip count.</summary>
        Skipped = 5,
    }

    public sealed class ClampRelease
    {
        public const string ReleaseText = "Release";
        public const string GaugeTitle = "Hold-down";

        readonly ClampTuning tuning;

        public bool Forgiving { get; }
        /// <summary>Design liftoff T/W at full sea-level thrust; NaN when unknown (the readout then shows thrust only).</summary>
        public double LiftoffTwr { get; }
        public double IgnitionT { get; }
        public double GreenStart { get; }
        public double GreenEnd { get; }
        /// <summary>When the sequencer releases on its own if nobody taps (the end of the green).</summary>
        public double AutoAt => GreenEnd;

        public bool Released { get; private set; }
        public double ReleaseT { get; private set; } = double.NaN;
        /// <summary>Count time the sim starts. +Infinity until released.</summary>
        public double LiftoffAt { get; private set; } = double.PositiveInfinity;
        public ClampOutcome Outcome { get; private set; }
        /// <summary>The player's own tap (Clean or Early). Auto, Late and Skip are not.</summary>
        public bool Touched => Outcome == ClampOutcome.Clean || Outcome == ClampOutcome.Early;
        /// <summary>Medal credit: Perfect for a clean tap, Good for an early one, None for every release nobody tapped.</summary>
        public AscentGrade Grade => Outcome == ClampOutcome.Clean ? AscentGrade.Perfect : Outcome == ClampOutcome.Early ? AscentGrade.Good : AscentGrade.None;
        /// <summary>Gauge needle (0..1) frozen at the release.</summary>
        public double ReleaseNeedle { get; private set; } = double.NaN;

        public ClampRelease(ClampTuning tuning, bool forgiving, double liftoffTwr)
        {
            this.tuning = tuning ?? new ClampTuning();
            Forgiving = forgiving;
            LiftoffTwr = liftoffTwr > 0 && !double.IsInfinity(liftoffTwr) ? liftoffTwr : double.NaN;
            var w = forgiving ? this.tuning.Forgiving : this.tuning.Standard;
            double before = Clamp(w != null ? w.GreenBeforeS : 0.6, 0.1, 5);
            double after = Clamp(w != null ? w.GreenAfterS : 0.9, 0, 5);
            GreenStart = -before;
            GreenEnd = after;
            double ign = this.tuning.IgnitionS;
            if (double.IsNaN(ign) || ign > GreenStart - 0.5) ign = GreenStart - 0.5;
            IgnitionT = ign;
        }

        /// <summary>Thrust share 0..1 at count time <paramref name="t"/>: a smooth spool from ignition to full at the green.</summary>
        public double Thrust01(double t)
        {
            if (double.IsNaN(t) || t <= IgnitionT) return 0;
            if (t >= GreenStart) return 1;
            double u = (t - IgnitionT) / (GreenStart - IgnitionT);
            return u * u * (3 - 2 * u);
        }

        /// <summary>Live T/W on the pad (thrust share x the design's liftoff T/W); NaN when the design's is unknown.</summary>
        public double Twr(double t) => double.IsNaN(LiftoffTwr) ? double.NaN : Thrust01(t) * LiftoffTwr;

        public ClampPhase Phase(double t)
        {
            if (Released) return ClampPhase.Released;
            if (double.IsNaN(t) || t < IgnitionT) return ClampPhase.Idle;
            return t < GreenStart ? ClampPhase.Spooling : ClampPhase.Green;
        }

        public bool InGreen(double t) => !double.IsNaN(t) && t >= GreenStart && t <= GreenEnd;

        /// <summary>The gauge needle 0..1: thrust up to the green band, then across the band to its end (the auto-release).</summary>
        public double Needle(double t)
        {
            if (Released && !double.IsNaN(ReleaseNeedle)) return ReleaseNeedle;
            double from = Clamp(tuning.GaugeGreenFrom, 0.1, 0.95);
            if (double.IsNaN(t) || t < GreenStart) return from * Thrust01(t);
            double span = GreenEnd - GreenStart;
            double u = span > 1e-6 ? (t - GreenStart) / span : 1;
            return from + (1 - from) * Clamp(u, 0, 1);
        }

        public double GaugeGreenFrom => Clamp(tuning.GaugeGreenFrom, 0.1, 0.95);

        /// <summary>The player tapped Release at <paramref name="t"/>. False when there is nothing to release yet or already released.</summary>
        public bool Tap(double t)
        {
            if (Released || double.IsNaN(t) || t < IgnitionT) return false;
            if (t < GreenStart)
            {
                Release(t, ClampOutcome.Early);
                LiftoffAt = GreenStart + Clamp(tuning.EarlyHesitateS, 0, 2);
                if (LiftoffAt < t) LiftoffAt = t;
            }
            else Release(t, t <= GreenEnd ? ClampOutcome.Clean : ClampOutcome.Late);
            return true;
        }

        /// <summary>Advance to <paramref name="t"/>: the sequencer auto-releases when the green closes.</summary>
        public void Step(double t)
        {
            if (Released || double.IsNaN(t)) return;
            if (t >= AutoAt) Release(AutoAt, Forgiving ? ClampOutcome.Auto : ClampOutcome.Late);
        }

        /// <summary>Skip count: release now, no credit.</summary>
        public void Skip(double t)
        {
            if (Released) return;
            Release(double.IsNaN(t) ? 0 : t, ClampOutcome.Skipped);
            LiftoffAt = double.NegativeInfinity;
        }

        /// <summary>The sim should start now.</summary>
        public bool LiftoffDue(double t) => Released && !(t < LiftoffAt);

        /// <summary>True while the count has reached T-0 but the release is not due: the count holds at zero.</summary>
        public bool HoldsAtZero(double t) => t >= 0 && !LiftoffDue(t);

        void Release(double t, ClampOutcome outcome)
        {
            ReleaseNeedle = Needle(t);
            Released = true;
            ReleaseT = t;
            Outcome = outcome;
            LiftoffAt = t;
        }

        /// <summary>Presentation only: the stack settles a few centimetres on its posts after an early tap, until liftoff.</summary>
        public double SettleM(double t)
        {
            if (Outcome != ClampOutcome.Early || double.IsNaN(t) || t < ReleaseT || t >= LiftoffAt) return 0;
            double u = Clamp((t - ReleaseT) / 0.25, 0, 1);
            return Clamp(tuning.EarlySettleM, 0, 0.3) * u * u * (3 - 2 * u);
        }

        /// <summary>The one-line state for the card (short: it sits beside the Release button).</summary>
        public string Status(double t)
        {
            var inv = CultureInfo.InvariantCulture;
            switch (Outcome)
            {
                case ClampOutcome.Clean: return "Clean release!";
                case ClampOutcome.Early:
                    if (t < LiftoffAt) return LiftoffTwr * Thrust01(ReleaseT) < 1 ? "Too early: T/W under 1" : "Too early: thrust not full";
                    return "Released";
                case ClampOutcome.Late: return "Late: auto release";
                case ClampOutcome.Auto: return "Auto release";
                case ClampOutcome.Skipped: return "Released";
            }
            var p = Phase(t);
            if (p == ClampPhase.Idle) return "";
            if (p == ClampPhase.Green) return "Full thrust: release!";
            return "Thrust " + Math.Round(Thrust01(t) * 100).ToString("0", inv) + "%";
        }

        /// <summary>"T/W 0.94", always two decimals so the readout does not jump; empty when the design's T/W is unknown.</summary>
        public string TwrText(double t)
        {
            double w = Twr(t);
            if (double.IsNaN(w)) return "";
            return "T/W " + w.ToString("0.00", CultureInfo.InvariantCulture);
        }

        static double Clamp(double v, double lo, double hi)
        {
            if (double.IsNaN(v)) return lo;
            return v < lo ? lo : v > hi ? hi : v;
        }
    }
}
