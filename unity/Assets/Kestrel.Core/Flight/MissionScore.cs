// Pass, stars and the §4 score. Pure: no catalog, no sim.
// score = 1000·pass + 300·max(0, 1 − |margin − 0.05|/0.05) + 200·max(0, (budget − cost)/budget) + landing bonus
// A miss scores 0. The margin term peaks at 5% and is zero outside 0–10%.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class MissionScore
    {
        public const double PassPoints = 1000;
        public const double MarginWeight = 300;
        public const double MarginPeak = 0.05;
        public const double CostWeight = 200;
        public const double LandingPerfect = 200;
        public const double LandingGood = 100;
        public const double StarMarginMin = 0.02;
        public const double StarMarginMax = 0.10;
        public const double ThreeStarGFraction = 0.80;
        public const double AltitudeFraction = 0.05;
        public const double InclinationDeg = 0.5;
        /// <summary>Same loss pad Stack.RequiredDeltaV adds on an orbital target.</summary>
        public const double OrbitalLossMps = 1500;
        /// <summary>Same pad Stack.RequiredDeltaV adds on a suborbital apogee.</summary>
        public const double ApogeeLossMps = 1000;

        /// <summary>
        /// §4 score. Pass is 0 or 1. Margin is (achieved − required) / required.
        /// Landing bonus is 200 for a perfect catch, 100 for a good catch, otherwise 0.
        /// A failed flight scores 0: the margin and cost terms describe a success.
        /// </summary>
        public static double Points(bool pass, double margin, double cost, double budget, string landingGrade)
        {
            if (!pass) return 0;
            double marginTerm = MarginWeight * Math.Max(0, 1 - Math.Abs(margin - MarginPeak) / MarginPeak);
            double costTerm = budget > 0 ? CostWeight * Math.Max(0, (budget - cost) / budget) : 0;
            return PassPoints + marginTerm + costTerm + LandingBonus(landingGrade);
        }

        public static double LandingBonus(string landingGrade)
        {
            if (IsPerfect(landingGrade)) return LandingPerfect;
            if (IsGood(landingGrade)) return LandingGood;
            return 0;
        }

        /// <summary>
        /// One star for a pass. Two when the margin is 2–10% and the cost is inside the budget.
        /// Three when those hold and, if a landing is required, the catch is perfect; otherwise peak g is at most 80% of the limit.
        /// </summary>
        public static int Stars(bool pass, double margin, double cost, double budget, bool landingRequired, bool perfectLanding, double peakG, double gLimit)
        {
            if (!pass) return 0;
            bool two = margin >= StarMarginMin && margin <= StarMarginMax && cost <= budget;
            if (!two) return 1;
            bool three = landingRequired ? perfectLanding : gLimit > 0 && peakG <= ThreeStarGFraction * gLimit;
            return three ? 3 : 2;
        }

        public static bool IsPerfect(string grade) => grade == "perfect" || grade == "Perfect";
        public static bool IsGood(string grade) => IsPerfect(grade) || grade == "good" || grade == "Good";

        public static bool WithinFraction(double actual, double target, double fraction)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || double.IsNaN(target) || double.IsInfinity(target)) return false;
            double scale = Math.Abs(target);
            if (scale == 0) return Math.Abs(actual) <= fraction;
            return Math.Abs(actual - target) <= fraction * scale;
        }

        public static bool AltitudeWithin(double flownM, double targetM)
            => WithinFraction(flownM, targetM, AltitudeFraction);

        public static bool InclinationWithin(double flownDeg, double targetDeg)
            => WithinFraction(flownDeg, targetDeg, InclinationDeg / Math.Max(Math.Abs(targetDeg), 1e-9))
               || Math.Abs(flownDeg - targetDeg) <= InclinationDeg;

        /// <summary>
        /// Δv the mission asks for, matching Stack.RequiredDeltaV where the catalog has the same orbit.
        /// Apogee uses √(2 g h) + 1000. Orbits use v at periapsis minus site rotation plus 1500 m/s.
        /// C3 targets use the speed at periapsis for that specific energy.
        /// </summary>
        public static double RequiredDeltaV(string kind, double apoKm, double periKm, double c3Km2PerS2, double latitudeDeg = 28.5)
        {
            if (kind == "apogee")
            {
                double h = apoKm * 1000;
                if (!(h > 0)) return 0;
                return Math.Sqrt(2 * Atmosphere.G0 * h) + ApogeeLossMps;
            }
            double peri = periKm * 1000;
            double rp = Gravity.R_EARTH + peri;
            if (!(rp > 0)) return 0;
            double v;
            if (kind == "c3")
            {
                double c3 = c3Km2PerS2 * 1e6;
                double specific = 2 * Gravity.MU_EARTH / rp + c3;
                v = specific > 0 ? Math.Sqrt(specific) : 0;
            }
            else
            {
                double ra = Gravity.R_EARTH + apoKm * 1000;
                if (!(ra > 0)) return 0;
                v = Math.Sqrt(Gravity.MU_EARTH * (2 / rp - 2 / (rp + ra)));
            }
            return v - Earth.SiteRotationSpeed(latitudeDeg) + OrbitalLossMps;
        }

        /// <summary>Specific energy × 2, m²/s². Negative is bound.</summary>
        public static double C3(double specificEnergy) => 2 * specificEnergy;
    }
}
