// Designer v3 recovery and separation checks (§2.4, §2.8).
// Predicates only. LandingGrade still scores a flown touchdown. FailureModel (WP-E) consumes these.
using System;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public static class RecoveryModel
    {
        public const double LightLegFraction = 0.06;
        public const double HeavyLegFraction = 0.09;
        public const double LightVLeg = 2;
        public const double HeavyVLeg = 4;
        public const double RtlsFactor = 2;
        public const double BargeFactor = 0.3;
        public const double SeparationWindowS = 2;
        public const double SeparationBaseMps = 0.5;
        public const double SeparationPerQkPa = 0.02;
        /// <summary>§2.7 RCS adds authority above 60 km. Four 100 N jets.</summary>
        public const double RcsAltitudeM = 60000;
        public const double RcsThrustN = 400;

        /// <summary>Absent or "light" is the v2 +6% dry mass. "heavy" is +9%.</summary>
        public static double LegDryFraction(string kind) => kind == "heavy" ? HeavyLegFraction : LightLegFraction;

        public static double VLeg(string kind) => kind == "heavy" ? HeavyVLeg : LightVLeg;

        /// <summary>§2.8 touchdown v_z &gt; v_leg collapses the legs.</summary>
        public static bool LegCollapse(double sinkRate, double vLeg) => sinkRate > vLeg;

        public static bool LegCollapse(double sinkRate, string kind) => LegCollapse(sinkRate, VLeg(kind));

        /// <summary>§2.8 Δv_land ≈ v_term + g·t_burn·(1 − 1/TWR_land).</summary>
        public static double LandingDeltaV(double vTerm, double g, double tBurn, double twrLand)
        {
            if (!(twrLand > 0) || double.IsNaN(twrLand) || double.IsInfinity(twrLand)) return double.PositiveInfinity;
            return vTerm + g * tBurn * (1 - 1 / twrLand);
        }

        /// <summary>RTLS ≈ 2·v_h(MECO). Barge ("Haven") ≈ 0.3·v_h(MECO). Expendable is 0.</summary>
        public static double BoostbackDeltaV(string mode, double horizontalMeco)
        {
            double v = Math.Abs(horizontalMeco);
            if (mode == "rtls") return RtlsFactor * v;
            if (mode == "barge") return BargeFactor * v;
            return 0;
        }

        public static double ReserveDeltaV(double ispSeconds, double dryMass, double reserveMass)
        {
            if (!(ispSeconds > 0) || !(dryMass > 0) || !(reserveMass > 0)) return 0;
            return ispSeconds * Atmosphere.G0 * Math.Log((dryMass + reserveMass) / dryMass);
        }

        /// <summary>A reserve that cannot pay the landing Δv is a hard landing.</summary>
        public static bool ReserveShort(double dvAvailable, double dvNeed) => dvAvailable < dvNeed;

        /// <summary>§2.4 v_min = 0.5 + 0.02·q_kPa.</summary>
        public static double MinRelSpeed(double qKPa)
            => SeparationBaseMps + SeparationPerQkPa * (qKPa > 0 ? qKPa : 0);

        /// <summary>Two-body Δv from a separation impulse.</summary>
        public static double SeparationRelSpeed(double sepImpulse, double massAft, double massForward)
        {
            if (!(sepImpulse > 0) || !(massAft > 0) || !(massForward > 0)) return 0;
            return sepImpulse * (1 / massAft + 1 / massForward);
        }

        /// <summary>Collision when v_rel stays under v_min within 2 s.</summary>
        public static bool SeparationCollision(double vRel, double qKPa, double secondsSinceSep)
            => secondsSinceSep >= 0 && secondsSinceSep <= SeparationWindowS && vRel < MinRelSpeed(qKPa);

        /// <summary>§2.4 hot staging without a vented interstage overpressures the bay.</summary>
        public static bool HotStagingOverpressure(string sepMode, string interstageShape)
            => sepMode == "hot" && interstageShape != "hot-staging";

        /// <summary>1 means separate at burnout. A set boosterSepPct is that fraction of the burn.</summary>
        public static double BoosterSepFraction(Design d)
        {
            if (d == null || !d.boosterSepSet) return 1;
            double p = d.boosterSepPct;
            if (p < 0) return 0;
            if (p > 100) return 1;
            return p / 100.0;
        }

        public static bool RcsActive(bool rcs, double altitudeM) => rcs && altitudeM > RcsAltitudeM;
    }
}
