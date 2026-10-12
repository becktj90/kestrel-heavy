// H3 gap-to-goal debrief. One sentence, real shortfalls, one fix.
// Margins match data/landing.json and the Haven grade: vyMargin = 1 - |vy| / landingVy.
// A negative margin is quoted as a shortfall in m/s or metres. The sentence never
// rounds a miss down, and never rounds spare margin up.
using System;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public readonly struct LandingDebrief
    {
        public readonly string Kind;
        public readonly double SinkShortfallMs;
        public readonly double OffsetShortfallM;
        public readonly double VyMargin;
        public readonly double DxMargin;
        public readonly string Fix;
        public readonly string Text;

        public LandingDebrief(string kind, double sinkShortfallMs, double offsetShortfallM, double vyMargin, double dxMargin, string fix, string text)
        {
            Kind = kind;
            SinkShortfallMs = sinkShortfallMs;
            OffsetShortfallM = offsetShortfallM;
            VyMargin = vyMargin;
            DxMargin = dxMargin;
            Fix = fix;
            Text = text;
        }
    }

    public static class GapToGoal
    {
        public const string EaseBurn = "ease the landing burn";
        public const string AimPaint = "aim for the paint";
        public const string HoldUpright = "hold the booster upright";

        public static LandingDebrief Landing(string kind, double vy, double dx, double landingVy, double landingTol, bool tipped)
        {
            double limVy = LimitVy(landingVy);
            double limDx = LimitDx(landingTol);
            double vyAbs = Finite(vy);
            double dxAbs = Finite(dx);
            double sink = vyAbs - limVy;
            double offset = dxAbs - limDx;
            double vyMargin = 1 - vyAbs / limVy;
            double dxMargin = 1 - dxAbs / limDx;
            if (kind != "deck")
            {
                return new LandingDebrief(kind, sink, offset, vyMargin, dxMargin, null,
                    "Ungraded. Only a deck touchdown has a catch margin.");
            }

            bool sinkShort = sink > 0;
            bool offsetShort = offset > 0;
            string sinkRoom = F1(FloorTo(limVy - vyAbs, 0.1));
            string offsetRoom = F0(FloorTo(limDx - dxAbs, 1));
            string sinkOver = F1(CeilTo(sink, 0.1));
            string offsetOver = F0(CeilTo(offset, 1));
            string limVyText = F1(limVy);
            string limDxText = F0(limDx);

            if (!sinkShort && !offsetShort && tipped)
            {
                return new LandingDebrief(kind, sink, offset, vyMargin, dxMargin, HoldUpright,
                    "Tipped on the deck. Sink margin " + sinkRoom + " m/s, offset margin " + offsetRoom + " m → " + HoldUpright + ".");
            }
            if (!sinkShort && !offsetShort)
            {
                return new LandingDebrief(kind, sink, offset, vyMargin, dxMargin, null,
                    "Inside the catch. Sink margin " + sinkRoom + " m/s, offset margin " + offsetRoom + " m.");
            }

            bool sinkBinds = vyMargin <= dxMargin;
            string fix = sinkBinds ? EaseBurn : AimPaint;
            string text;
            if (sinkShort && offsetShort)
                text = "Short by " + sinkOver + " m/s sink and " + offsetOver + " m offset (limits " + limVyText + " m/s and " + limDxText + " m) → " + fix + ".";
            else if (sinkShort)
                text = "Short by " + sinkOver + " m/s sink (limit " + limVyText + " m/s) → " + EaseBurn + ".";
            else
                text = "Short by " + offsetOver + " m offset (limit " + limDxText + " m) → " + AimPaint + ".";
            if (tipped) text = text.Substring(0, text.Length - 1) + ". Tipped.";
            return new LandingDebrief(kind, sink, offset, vyMargin, dxMargin, fix, text);
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

        static double Finite(double v) => double.IsNaN(v) || double.IsInfinity(v) ? 0 : Math.Abs(v);

        /// <summary>Smallest step that is still at least the shortfall. Exact multiples stay put.</summary>
        public static double CeilTo(double value, double step)
        {
            if (value <= 0 || step <= 0) return 0;
            return Math.Ceiling(value / step - 1e-9) * step;
        }

        /// <summary>Largest step that does not exceed the spare margin.</summary>
        public static double FloorTo(double value, double step)
        {
            if (value <= 0 || step <= 0) return 0;
            return Math.Floor(value / step + 1e-9) * step;
        }

        static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        static string F0(double v) => v.ToString("0", CultureInfo.InvariantCulture);
    }
}
