// #79: the player flies the final landing burn. Autopilot still flies boostback, entry and the
// descent exactly as before; only the Landing phase takes a player stick. The stick is an input
// into the same sim (RecoveryGuidance.Command, the 10 deg/s pitch rate, the fin/leg/phase gates):
// never a scripted cutscene, so a hand-flown burn can still miss, tip, run dry, or land Perfect,
// and RecoveryScore still grades the touchdown from the same margins (data/landing.json).
//
// Auto-assist is the default and is what a player who never touches the controls gets: with no
// input the director returns the autopilot's own command bit-identically, so a never-burner lands
// exactly as today (the KID retune's rule, carried to Unity, which has no Kid mode of its own).
// Assisted lets the player add throttle above the autopilot and bias the tilt; Manual (Engineer)
// hands the burn over entirely. Grades never change; only the score's landing bonus is multiplied
// for a burn the player actually flew (MissionScore).
using System;

namespace Kestrel.Core.Flight
{
    /// <summary>How much of the landing burn the autopilot still flies.</summary>
    public enum LandingBurnAssist
    {
        /// <summary>Full autopilot, as before #79. No player input is read.</summary>
        Auto = 0,
        /// <summary>Default. The autopilot's throttle is the floor and its tilt the centre: a player who
        /// does nothing lands exactly as on Auto; holding burn and tilting steers from there.</summary>
        Assisted = 1,
        /// <summary>Engineer. The player's throttle and tilt are the command, with no autopilot floor.</summary>
        Manual = 2,
    }

    /// <summary>One frame of player input on the landing burn. Throttle is hold-to-burn (0..1);
    /// tilt is left/right (-1..1), positive toward the east (further downrange).</summary>
    public struct LandingBurnInput
    {
        public double Throttle01;
        public double Tilt01;

        public bool HasInput => Throttle01 > 0 || Tilt01 != 0;

        public static LandingBurnInput None => default;

        public static LandingBurnInput Held(double throttle01, double tilt01)
            => new LandingBurnInput { Throttle01 = throttle01, Tilt01 = tilt01 };
    }

    /// <summary>The landing burn's live numbers, for the HUD: sink, offset and tilt, each against the
    /// limit RecoveryScore grades it on. Offset is against the deck where the swell has moved it, not
    /// the charted centre.</summary>
    public readonly struct LandingBurnReadout
    {
        public readonly double Sink, SinkLimit, Offset, OffsetLimit, TiltDeg, TiltLimitDeg;
        public readonly bool OnDeck;

        public LandingBurnReadout(double sink, double sinkLimit, double offset, double offsetLimit, double tiltDeg, double tiltLimitDeg)
        {
            Sink = sink; SinkLimit = sinkLimit; Offset = offset; OffsetLimit = offsetLimit;
            TiltDeg = tiltDeg; TiltLimitDeg = tiltLimitDeg;
            OnDeck = Math.Abs(offset) <= offsetLimit;
        }

        /// <summary>One line for the burn panel, in the same shape as RecoveryViews.Touchdown.</summary>
        public string Line()
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return "SINK " + Sink.ToString("0.0", inv) + "/" + SinkLimit.ToString("0.0", inv) + " m/s  OFF "
                + Offset.ToString("0", inv) + "/" + OffsetLimit.ToString("0", inv) + " m  TILT "
                + TiltDeg.ToString("0.0", inv) + "/" + TiltLimitDeg.ToString("0", inv) + "°";
        }
    }

    public static class LandingBurnDirector
    {
        /// <summary>How far the player's tilt can bias the autopilot's in Assisted, as a share of the
        /// autopilot's own tilt cap at that altitude. The cap shrinks low down (0.2 rad), so the bias
        /// shrinks with it and the autopilot can always straighten the stage out for the deck: a held
        /// tilt steers the touchdown by tens of metres, never hundreds, and never blocks the final
        /// straighten the way a fixed-radian bias did (m7 swung 210 m on a 0.25 rad bias).</summary>
        public const double TiltAuthorityShare = 0.25;

        /// <summary>The Haven deck's swell when no mission ramp supplies one: the gentlest ramp entry
        /// (KH-1, 6). The web's px and Unity's metres share the scale (CADET landingTol 36 px against
        /// DeckRadiusM 36 m), so the ramp's swellAmp reads as metres unchanged.</summary>
        public const double DefaultSwellAmpM = 6;

        /// <summary>The deck's horizontal offset from its charted centre at body time <paramref name="t"/>:
        /// the same swell as MissionRamp (SwellOmega), so the drawn deck, the live offset readout and the
        /// touchdown miss all read one moving deck. NaN-safe: no swell, no offset.</summary>
        public static double DeckOffsetM(double t, double swellAmpM)
        {
            if (double.IsNaN(t) || double.IsInfinity(t) || double.IsNaN(swellAmpM) || double.IsInfinity(swellAmpM)) return 0;
            return swellAmpM * Math.Sin(t * MissionRamp.SwellOmega);
        }

        /// <summary>The stick for this step of the landing burn. Outside the Landing phase the caller
        /// keeps the autopilot; this only shapes the burn itself. With no input under Assisted the
        /// returned stick is the autopilot's own command, so the flown path is bit-identical to Auto.</summary>
        public static RecoveryStick Stick(in RecoveryBody body, in RecoveryCraft craft, in LandingBurnInput input, LandingBurnAssist assist)
        {
            if (assist == LandingBurnAssist.Auto) return RecoveryStick.Assisted;
            // No input under Assisted is the autopilot itself, not a recomputation of it: the flown
            // path is bit-identical to Auto, and a player who never touches the burn lands as today.
            if (assist == LandingBurnAssist.Assisted && !input.HasInput) return RecoveryStick.Assisted;
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
            double tiltMax = RecoveryGuidance.LandingTiltMax(alt);
            double tiltIn = Clamp01(input.Tilt01);
            double thrIn = Clamp01(input.Throttle01);
            if (assist == LandingBurnAssist.Manual)
            {
                // Tilt01 +1 moves the touchdown east: pitch under vertical thrusts west, so the
                // player's east steer is a pitch under vertical by the tilt.
                return new RecoveryStick
                {
                    Auto = false,
                    PitchRad = Math.PI / 2 - tiltIn * tiltMax,
                    Throttle = thrIn,
                    PlayerInput = input.HasInput,
                };
            }
            var auto = RecoveryGuidance.Command(body, craft, RecoveryStick.Assisted);
            double autoTilt = RecoveryFrames.Wrap(auto.Pitch - Math.PI / 2);
            // Tilt01 is the player's steer: +1 moves the touchdown east (further downrange), −1 west.
            // The autopilot's tilt is the opposite sign (a positive auto tilt thrusts west to kill east
            // drift), so the player's bias subtracts from it.
            double tilt = autoTilt - tiltIn * TiltAuthorityShare * tiltMax;
            if (tilt > tiltMax) tilt = tiltMax;
            else if (tilt < -tiltMax) tilt = -tiltMax;
            double thr = auto.Throttle > thrIn ? auto.Throttle : thrIn;
            return new RecoveryStick
            {
                Auto = false,
                PitchRad = Math.PI / 2 + tilt,
                Throttle = thr,
                PlayerInput = input.HasInput,
            };
        }

        /// <summary>The burn's live numbers from the sample, against the deck where the swell has moved it.</summary>
        public static LandingBurnReadout Readout(in RecoverySample sample, in RecoveryCraft craft, double deckOffsetM)
        {
            double vLeg = craft.VLeg;
            if (!(vLeg > 0)) vLeg = RecoveryModel.LightVLeg;
            return new LandingBurnReadout(sample.Sink, vLeg, sample.MissM - deckOffsetM,
                RecoverySim.DeckRadiusM, sample.TiltDeg, RecoverySim.TiltLimitDeg);
        }

        static double Clamp01(double v)
        {
            if (double.IsNaN(v)) return 0;
            if (v < -1) return -1;
            if (v > 1) return 1;
            return v;
        }
    }
}
