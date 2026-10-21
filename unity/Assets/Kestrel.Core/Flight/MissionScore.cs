// Pass, stars and the §4 score. Pure: no catalog, no sim.
// score = 1000·pass + 300·max(0, 1 − |margin − 0.05|/0.05) + 200·max(0, (budget − cost)/budget) + landing bonus
// A miss scores 0. The margin term peaks at 5% and is zero outside 0–10%.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class MissionScore
    {
        /// <summary>
        /// The apoapsis a flight is scored on, metres. A ballistic coast scores its drag-aware flown peak
        /// (<see cref="SimResult.coastApexAlt"/>); the vacuum Kepler apoapsis at cutoff ignored the drag on the coast
        /// and read KH-1 at 9.7 km when it peaks at 6.6 km. Orbits, failures and old results keep the Kepler value.
        /// FlightBatch, the designer's reference ascent and the live report all read this.
        /// </summary>
        public static double FlownApoAltM(SimResult r)
        {
            if (r == null) return double.NaN;
            return double.IsNaN(r.coastApexAlt) ? r.orbit.apoAlt : r.coastApexAlt;
        }

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

        /// <summary>
        /// The one target acceptance rule (#313): the batch (<see cref="FlightBatch"/>), the live debrief
        /// (<see cref="FlightDebrief.GoalMet(CampaignMission, bool, double, double, double, string, GoalCheck)"/>) and the
        /// launch predictor all call this. Apogee: the flown peak in the ±5% band. Circular: apoapsis and periapsis in band.
        /// Ellipse: both elements in band, or the parking periapsis in band with the departure burn covered. C3: energy
        /// within 5%, or the parking periapsis in band with the departure burn covered.
        /// </summary>
        public static bool TargetMet(string kind, double apoTargetKm, double periTargetKm, double c3Target,
            double apoM, double periM, double c3, bool departureCovered, out string criterion)
        {
            criterion = null;
            bool apo = AltitudeWithin(apoM, apoTargetKm * 1000);
            bool peri = AltitudeWithin(periM, periTargetKm * 1000);
            switch (kind)
            {
                case "apogee": criterion = "altitude"; return apo;
                case "circular": criterion = "altitude"; return apo && peri;
                case "ellipse":
                    if (apo && peri) { criterion = "elements"; return true; }
                    if (peri && departureCovered) { criterion = "departure"; return true; }
                    return false;
                case "c3":
                    if (WithinFraction(c3, c3Target, AltitudeFraction)) { criterion = "c3"; return true; }
                    if (peri && departureCovered) { criterion = "departure"; return true; }
                    return false;
            }
            return false;
        }

        /// <summary>The target kind of a free-build catalog orbit: a hop, a circle, or an ellipse.</summary>
        public static string FreeOrbitKind(double apoKm, double periKm)
            => periKm < 0 ? "apogee" : Math.Abs(apoKm - periKm) < 1e-9 ? "circular" : "ellipse";

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

        /// <summary>#313 Physics A: finite-burn loss on the departure burn when the departing stage's T/W is at least this.</summary>
        public const double DepartureLossFraction = 0.015;
        /// <summary>T/W (vacuum thrust over weight at departure) at and above which the loss is <see cref="DepartureLossFraction"/>.</summary>
        public const double DepartureLossTw = 0.5;
        /// <summary>Cap on the departure loss for a very low T/W stage.</summary>
        public const double DepartureLossMax = 0.10;

        /// <summary>
        /// #313 Physics A: the impulsive departure burn from a circular parking orbit at <paramref name="parkAltM"/>,
        /// m/s. Ellipse: the target ellipse's perigee speed there minus circular speed. C3: the hyperbolic (or
        /// bound-C3) speed there, √(C3 + 2μ/r), minus circular speed. About 2.46 km/s from 200 km to GTO and about
        /// 3.1 km/s to TLI (C3 −2). NaN for other kinds.
        /// </summary>
        public static double DepartureImpulsiveMps(string kind, double apoKm, double c3Km2PerS2, double parkAltM)
        {
            double r = Gravity.R_EARTH + parkAltM;
            if (!(r > Gravity.R_EARTH * 0.5)) return double.NaN;
            double vc = Math.Sqrt(Gravity.MU_EARTH / r);
            double v;
            if (kind == "ellipse")
            {
                double ra = Gravity.R_EARTH + apoKm * 1000;
                if (!(ra >= r)) return 0;
                v = Math.Sqrt(Gravity.MU_EARTH * (2 / r - 2 / (r + ra)));
            }
            else if (kind == "c3")
            {
                double sq = c3Km2PerS2 * 1e6 + 2 * Gravity.MU_EARTH / r;
                v = sq > 0 ? Math.Sqrt(sq) : 0;
            }
            else return double.NaN;
            return Math.Max(0, v - vc);
        }

        /// <summary>
        /// Finite-burn loss fraction on the departure burn. 1.5% at T/W ≥ 0.5. Below that the loss grows with the burn
        /// arc, as (0.5 / T/W)² × 1.5%, capped at 10% (T/W 0.25 → 6%; T/W ≤ 0.19 → 10%). No thrust → the cap.
        /// </summary>
        public static double DepartureLoss(double tw)
        {
            if (!(tw > 0)) return DepartureLossMax;
            if (tw >= DepartureLossTw) return DepartureLossFraction;
            double f = DepartureLossFraction * (DepartureLossTw / tw) * (DepartureLossTw / tw);
            return f > DepartureLossMax ? DepartureLossMax : f;
        }

        /// <summary>Required departure Δv with the finite-burn loss, m/s (<see cref="DepartureImpulsiveMps"/>).</summary>
        public static double DepartureRequiredMps(string kind, double apoKm, double c3Km2PerS2, double parkAltM, double tw)
            => DepartureImpulsiveMps(kind, apoKm, c3Km2PerS2, parkAltM) * (1 + DepartureLoss(tw));

        /// <summary>Specific energy × 2, m²/s². Negative is bound.</summary>
        public static double C3(double specificEnergy) => 2 * specificEnergy;
    }
}
