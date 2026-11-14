// Per-mission personal bests beyond stars (ENGAGEMENT_SPEC §2). Cost and mass are lower-better;
// Δv margin is higher-better; landing offset is lower-better. Honest numbers only; no FOMO.
using System;
using System.Globalization;
using System.Text;

namespace Kestrel.Core.Flight
{
    public readonly struct PbSnapshot
    {
        public readonly double CostKc;
        public readonly double MassKg;
        public readonly double Margin;
        public readonly double LandingOffsetM;
        public readonly bool Passed;

        public PbSnapshot(double costKc, double massKg, double margin, double landingOffsetM, bool passed)
        {
            CostKc = costKc;
            MassKg = massKg;
            Margin = margin;
            LandingOffsetM = landingOffsetM;
            Passed = passed;
        }
    }

    public readonly struct PbDelta
    {
        public readonly bool Cost;
        public readonly bool Mass;
        public readonly bool Margin;
        public readonly bool Landing;
        public readonly string Line;

        public PbDelta(bool cost, bool mass, bool margin, bool landing, string line)
        {
            Cost = cost;
            Mass = mass;
            Margin = margin;
            Landing = landing;
            Line = line;
        }

        public bool Any => Cost || Mass || Margin || Landing;
    }

    public static class PersonalBests
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Apply a passed flight's metrics onto the record. Misses never improve PBs.</summary>
        public static PbDelta Apply(MissionMetrics rec, in PbSnapshot snap)
        {
            if (rec == null || !snap.Passed) return new PbDelta(false, false, false, false, "");
            bool cost = ImproveLower(ref rec.BestCostKc, snap.CostKc);
            bool mass = ImproveLower(ref rec.BestMassKg, snap.MassKg);
            bool margin = ImproveHigher(ref rec.BestMargin, snap.Margin);
            bool land = ImproveLower(ref rec.BestLandingOffsetM, snap.LandingOffsetM);
            if (!cost && !mass && !margin && !land) return new PbDelta(false, false, false, false, "");
            return new PbDelta(cost, mass, margin, land, Line(rec, cost, mass, margin, land));
        }

        public static string Summary(MissionMetrics rec)
        {
            if (rec == null) return "";
            var sb = new StringBuilder();
            if (Finite(rec.BestCostKc)) Append(sb, "cost " + Whole(rec.BestCostKc) + " Kc");
            if (Finite(rec.BestMassKg)) Append(sb, "mass " + Whole(rec.BestMassKg) + " kg");
            if (Finite(rec.BestMargin)) Append(sb, "margin " + Pct(rec.BestMargin));
            if (Finite(rec.BestLandingOffsetM)) Append(sb, "landing " + Whole(rec.BestLandingOffsetM) + " m");
            return sb.Length == 0 ? "" : "PBs · " + sb;
        }

        static string Line(MissionMetrics rec, bool cost, bool mass, bool margin, bool land)
        {
            var sb = new StringBuilder("New personal best");
            int n = 0;
            if (cost) { sb.Append(n == 0 ? ": " : ", "); sb.Append("cost " + Whole(rec.BestCostKc) + " Kc"); n++; }
            if (mass) { sb.Append(n == 0 ? ": " : ", "); sb.Append("mass " + Whole(rec.BestMassKg) + " kg"); n++; }
            if (margin) { sb.Append(n == 0 ? ": " : ", "); sb.Append("margin " + Pct(rec.BestMargin)); n++; }
            if (land) { sb.Append(n == 0 ? ": " : ", "); sb.Append("landing " + Whole(rec.BestLandingOffsetM) + " m"); n++; }
            sb.Append('.');
            return sb.ToString();
        }

        static void Append(StringBuilder sb, string part)
        {
            if (sb.Length > 0) sb.Append(" · ");
            sb.Append(part);
        }

        static bool ImproveLower(ref double best, double next)
        {
            if (!Finite(next) || next < 0) return false;
            if (!Finite(best) || next < best - 1e-9)
            {
                best = next;
                return true;
            }
            return false;
        }

        static bool ImproveHigher(ref double best, double next)
        {
            if (!Finite(next)) return false;
            if (!Finite(best) || next > best + 1e-9)
            {
                best = next;
                return true;
            }
            return false;
        }

        static bool Finite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));

        static string Whole(double n)
        {
            double r = Math.Round(n, MidpointRounding.AwayFromZero);
            if (Math.Abs(r) >= 1e15) return n.ToString("0", Inv);
            return ((long)r).ToString("N0", Inv);
        }

        static string Pct(double fraction)
        {
            double pct = Math.Round(fraction * 100.0, MidpointRounding.AwayFromZero);
            return pct.ToString("0", Inv) + "%";
        }
    }

    /// <summary>Per-mission metric PBs stored beside stars on <see cref="Economy.MissionRecord"/>.</summary>
    public sealed class MissionMetrics
    {
        public double BestCostKc = double.NaN;
        public double BestMassKg = double.NaN;
        public double BestMargin = double.NaN;
        public double BestLandingOffsetM = double.NaN;
    }
}
