// Haven touchdown grades. Thresholds are data/landing.json.
// Margin is how much of the catch limit is left. A negative margin is a miss, named Salvage on a deck.
using System;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public sealed class LandingTune
    {
        public readonly double PadRatLandingVy, PerfectMin, GoodMin;

        public static LandingTune Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "landing.json"))));

        public static LandingTune FromJson(JsonObject o)
            => new LandingTune(o.Num("padRatLandingVy"), o.Num("perfectMin"), o.Num("goodMin"));

        LandingTune(double padRatLandingVy, double perfectMin, double goodMin)
        {
            PadRatLandingVy = padRatLandingVy; PerfectMin = perfectMin; GoodMin = goodMin;
        }

        public static double LimitVy(double landingVy)
        {
            if (double.IsNaN(landingVy) || double.IsInfinity(landingVy) || landingVy == 0) landingVy = 0.1;
            return Math.Max(0.1, landingVy);
        }

        public static double LimitDx(double landingTol)
        {
            if (double.IsNaN(landingTol) || double.IsInfinity(landingTol) || landingTol == 0) landingTol = 1;
            return Math.Max(1, landingTol);
        }

        public TouchdownMargins Margins(double vy, double dx, double landingVy, double landingTol)
        {
            double limVy = LimitVy(landingVy), limDx = LimitDx(landingTol);
            double vyAbs = double.IsNaN(vy) || double.IsInfinity(vy) ? 0 : Math.Abs(vy);
            double dxAbs = double.IsNaN(dx) || double.IsInfinity(dx) ? 0 : Math.Abs(dx);
            double vyMargin = 1 - vyAbs / limVy;
            double dxMargin = 1 - dxAbs / limDx;
            return new TouchdownMargins(vyMargin, dxMargin, Math.Min(vyMargin, dxMargin));
        }

        /// <summary>
        /// Perfect, Good, Hard, or Salvage on a deck. Water and any other kind are ungraded (null).
        /// The margins stay on the result so a debrief can quote the shortfall without rounding it up.
        /// </summary>
        public TouchdownGrade Grade(string kind, double vy, double dx, double landingVy, double landingTol, bool tipped)
        {
            var m = Margins(vy, dx, landingVy, landingTol);
            if (kind != "deck") return new TouchdownGrade(null, m);
            string name;
            if (tipped || m.VyMargin < 0 || m.DxMargin < 0) name = "Salvage";
            else if (m.Margin >= PerfectMin) name = "Perfect";
            else if (m.Margin >= GoodMin) name = "Good";
            else name = "Hard";
            return new TouchdownGrade(name, m);
        }
    }

    public readonly struct TouchdownMargins
    {
        public readonly double VyMargin, DxMargin, Margin;
        public TouchdownMargins(double vyMargin, double dxMargin, double margin)
        {
            VyMargin = vyMargin; DxMargin = dxMargin; Margin = margin;
        }
    }

    public readonly struct TouchdownGrade
    {
        public readonly string Name;
        public readonly TouchdownMargins Margins;
        public TouchdownGrade(string name, TouchdownMargins margins) { Name = name; Margins = margins; }
    }
}
