// Designer v3 steering law (§2.7). Pure: Command(state) returns (pitchRad, throttle) and
// writes nothing. Pitch is the Headless convention, radians from the horizontal, so π/2 is straight up.
// Absent guidance fields reproduce the built-in kick and the gravity-turn catch. The 3 s kick dwell
// in Headless is history, so this function uses the flight-path catch alone. WP-E owns the integrator.
using System;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public struct GuidanceState
    {
        public double alt, vrel, flightPathRad, vr, g, q, mass, thrustN, throttleMin;
        public int stageIndex;
    }

    public sealed class GuidanceProfile
    {
        public const double DefaultKickDeg = 4;
        public const double DefaultKickSpeed = 60;
        /// <summary>Headless holds the kick until the air-relative flight path is within this of the command.</summary>
        public const double CatchRad = 0.002;
        /// <summary>§2.6 nominal q_lim scale. The max-Q bucket is the upper half of this.</summary>
        public const double MaxQReferencePa = 35000;
        public const double MaxQBucketFraction = 0.5;
        /// <summary>Shallow end of the pitch program, radians from horizontal.</summary>
        public const double TurnEndPitchRad = 0.15;
        /// <summary>The pitch program finishes this fraction of the way to the target altitude.</summary>
        public const double TurnoverAltFraction = 0.2;

        public bool useAltitudeKick;
        public double pitchKickAlt;
        public double pitchKickDeg = DefaultKickDeg;
        public double kickSpeed = DefaultKickSpeed;
        public bool turnShapeSet;
        public double turnShape;
        public bool maxQThrottleSet;
        public double maxQThrottle = 100;
        public bool gLimitSet;
        public double gLimit;
        public double targetAlt;

        public static GuidanceProfile Builtin(double targetAlt = 0)
            => new GuidanceProfile { targetAlt = targetAlt };

        public static GuidanceProfile From(Design d, double targetAlt)
        {
            var g = Builtin(targetAlt);
            var spec = d?.guidance;
            if (spec == null) return g;
            if (spec.pitchKickAltSet) { g.useAltitudeKick = true; g.pitchKickAlt = spec.pitchKickAlt; }
            if (spec.pitchKickDegSet) g.pitchKickDeg = spec.pitchKickDeg;
            if (spec.turnShapeSet) { g.turnShapeSet = true; g.turnShape = spec.turnShape; }
            if (spec.maxQThrottleSet) { g.maxQThrottleSet = true; g.maxQThrottle = spec.maxQThrottle; }
            if (spec.gLimitSet) { g.gLimitSet = true; g.gLimit = spec.gLimit; }
            return g;
        }

        /// <summary>§2.7 Command(state) → (pitchRad, throttle).</summary>
        public (double pitchRad, double throttle) Command(GuidanceState state)
        {
            double throttle = ThrottleOf(state);
            double pitch = PitchOf(state, throttle);
            return (pitch, throttle);
        }

        /// <summary>
        /// Closed-loop insertion. Pitch so the radial rate tracks (targetAlt − alt) / 40,
        /// clamped the same way Headless steers an upper stage. aAxial is thrust·throttle / mass.
        /// g is the radial acceleration to cancel (μ/r² − v_t²/r).
        /// </summary>
        public static double PegLitePitch(double alt, double targetAlt, double vr, double g, double aAxial)
        {
            double aT = aAxial > 1e-6 ? aAxial : 1e-6;
            double vrCmd = (targetAlt - alt) / 40.0;
            if (vrCmd > 250) vrCmd = 250;
            else if (vrCmd < -150) vrCmd = -150;
            double sinP = (g + 0.03 * (vrCmd - vr)) / aT;
            if (sinP > 0.95) sinP = 0.95;
            else if (sinP < -0.6) sinP = -0.6;
            return Math.Asin(sinP);
        }

        double PitchOf(GuidanceState s, double throttle)
        {
            if (s.stageIndex > 0)
            {
                double mass = s.mass > 0 ? s.mass : 0;
                double aAxial = mass > 0 ? s.thrustN * throttle / mass : 0;
                return PegLitePitch(s.alt, targetAlt, s.vr, s.g, aAxial);
            }
            double kick = pitchKickDeg * Math.PI / 180.0;
            if (kick < 0) kick = 0;
            double vertical = Math.PI / 2;
            double kicked = vertical - kick;
            bool armed = useAltitudeKick ? s.alt >= pitchKickAlt : s.vrel >= kickSpeed;
            if (!armed) return vertical;
            if (!turnShapeSet)
            {
                if (s.flightPathRad > kicked + CatchRad) return kicked;
                // A biting max-Q cap must not chase a path that is just starting to fall.
                // Snapping all the way back to the kick lofts a rocket that has already turned.
                if (throttle < 1 && HoldKickThroughMaxQ(s.q) && s.flightPathRad > kicked - 0.15)
                    return kicked;
                return s.flightPathRad;
            }
            return Turn(s.alt, kicked);
        }

        double Turn(double alt, double kicked)
        {
            double shape = turnShape;
            if (shape < 0) shape = 0;
            else if (shape > 1) shape = 1;
            double start = useAltitudeKick ? pitchKickAlt : 0;
            double span = targetAlt * TurnoverAltFraction - start;
            if (span < 1) span = 1;
            double u = (alt - start) / span;
            if (u < 0) u = 0;
            else if (u > 1) u = 1;
            // shape 0 stays steep (exponent 3). shape 1 is linear.
            double expo = 1 + 2 * (1 - shape);
            double t = Math.Pow(u, expo);
            return kicked * (1 - t) + TurnEndPitchRad * t;
        }

        /// <summary>
        /// True when the max-Q cap is below full throttle and q is inside the bucket.
        /// Callers hold the kick pitch for that stretch. A 100% cap does not hold.
        /// A g-limit cap does not hold: the gravity turn still has to happen.
        /// </summary>
        public bool HoldKickThroughMaxQ(double q)
        {
            if (!maxQThrottleSet || q < MaxQReferencePa * MaxQBucketFraction) return false;
            double cap = maxQThrottle > 1.5 ? maxQThrottle / 100.0 : maxQThrottle;
            return cap < 1;
        }

        double ThrottleOf(GuidanceState s)
        {
            double throttle = 1;
            if (maxQThrottleSet && s.q >= MaxQReferencePa * MaxQBucketFraction)
            {
                // Designer saves a fraction (0.95) or a percent (95). Above 1.5 it is a percent.
                double cap = maxQThrottle > 1.5 ? maxQThrottle / 100.0 : maxQThrottle;
                if (cap < throttle) throttle = cap;
            }
            if (gLimitSet && gLimit > 0 && s.thrustN > 0 && s.mass > 0)
            {
                double cap = gLimit * Atmosphere.G0 * s.mass / s.thrustN;
                if (cap < throttle) throttle = cap;
            }
            double min = s.throttleMin;
            if (double.IsNaN(min) || min < 0) min = 0;
            else if (min > 1) min = 1;
            if (throttle < min) throttle = min;
            if (throttle > 1) throttle = 1;
            else if (throttle < 0) throttle = 0;
            return throttle;
        }
    }
}
