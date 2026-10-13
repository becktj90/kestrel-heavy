// Designer v3 drag build-up (§2.5). Cd0 is the sum of a nose table, a fairing shoulder,
// fin area, stowed grid fins, strap-ons and catalog interstage drag. Cd(Mach) stays Aero.CdMach.
// Stack.BaseCd calls this only when the design carries v3 aero fields. Without those fields
// the historical hard-coded terms stay in force so the goldens do not move.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Physics
{
    public struct FinGeometry
    {
        public int count;
        public double span, rootChord, tipChord, leSweep;
    }

    /// <summary>Term-by-term §2.5 build-up. total = nose + fairing + fins + grid + strap + interstage.</summary>
    public struct CdBreakdown
    {
        public double nose, fairing, fins, grid, strap, interstage, total;
    }

    public static class AeroModel
    {
        /// <summary>Flat-plate Cd of fin planform area, referenced up to the vehicle area. Engineering fit, same family as Cd(Mach).</summary>
        public const double FinCdPlate = 0.02;
        /// <summary>§2.5 stowed grid fins. The ascent Cd0 uses this. Deployed descent uses <see cref="GridDeployedCd"/>.</summary>
        public const double GridStowedCd = 0.01;
        /// <summary>Historical grid-fin increment. Descent control deploys the lattices; the ascent build-up stays stowed.</summary>
        public const double GridDeployedCd = 0.03;
        public const double StrapOnCd = 0.03;
        /// <summary>Hammerhead shoulder: fairing wider than the stage under it. The v2 stack already charges this.</summary>
        public const double FairingShoulderPerRatio = 0.08;
        public const double UnknownNoseCd = 0.32;

        // Tip/root and leading-edge sweep for the named planforms. The catalogs store span, root chord and count.
        const double TrapezoidTipFraction = 0.6;
        const double ClippedDeltaTipFraction = 0.25;
        const double SweptTipFraction = 0.5;

        static readonly object Gate = new object();
        static AeroTables tables;

        /// <summary>
        /// v3 aero is opt-in. Version 3 alone is not enough: the v2→v3 migration is an identity on every number.
        /// Guidance, legs and recovery are not aero fields.
        /// </summary>
        public static bool HasV3Aero(Design d)
        {
            if (d == null) return false;
            if (!string.IsNullOrEmpty(d.finPlanform) || d.finRootChordSet || d.gridFinDeployAltSet) return true;
            if (!string.IsNullOrEmpty(d.nose) && !DesignMath.NoseShapes.ContainsKey(d.nose)) return true;
            var stages = d.stages;
            if (stages == null) return false;
            for (int i = 0; i < stages.Count; i++)
            {
                var shape = stages[i].interstageShape;
                if (shape == "truss" || shape == "hot-staging") return true;
            }
            return false;
        }

        public static bool IsGridPlanform(string planform)
            => planform == "square-lattice" || planform == "diamond-lattice";

        /// <summary>Fixed-fin geometry the player set (planform or root chord). Grid lattices are not a Barrowman surface.</summary>
        public static bool HasV3FinGeometry(Design d)
        {
            if (d == null || !d.fins || IsGridPlanform(d.finPlanform)) return false;
            return !string.IsNullOrEmpty(d.finPlanform) || d.finRootChordSet;
        }

        public static double NoseCd(string nose) => Tables().Nose(nose);

        /// <summary>Heating factor relative to the ogive. §2.5: each nose sets Cd0, a CP offset and a heating factor. Thermal failure is WP-E.</summary>
        public static double NoseHeatingFactor(string nose)
        {
            double ogive = NoseCd("ogive");
            return ogive > 0 ? NoseCd(nose) / ogive : 1;
        }

        public static CdBreakdown Breakdown(Design d)
        {
            var t = Tables();
            double dRef = d != null ? Stack.ReferenceDiameter(d) : 0;
            var b = new CdBreakdown
            {
                nose = d == null ? 0 : t.Nose(d.nose),
                fairing = FairingShoulder(d),
                fins = FinCd(d, dRef),
                grid = GridContribution(d, false),
                strap = d == null ? 0 : StrapOnCd * Math.Max(0, d.StrapOnCount),
                interstage = d == null ? 0 : InterstageAero.CatalogDragCd(d, t.InterstageDrag),
            };
            b.total = b.nose + b.fairing + b.fins + b.grid + b.strap + b.interstage;
            return b;
        }

        public static double Cd0(Design d) => Breakdown(d).total;

        /// <summary>§2.5: the build-up, then Cd(Mach) via Aero.CdMach.</summary>
        public static double Cd(Design d, double mach) => Aero.CdMach(mach, Cd0(d));

        public static double GridContribution(Design d, bool deployed)
        {
            if (d == null) return 0;
            if (!d.gridFins && !IsGridPlanform(d.finPlanform)) return 0;
            return deployed ? GridDeployedCd : GridStowedCd;
        }

        /// <summary>
        /// Barrowman fin CN_α per radian, including the body-interference factor.
        /// Square unswept fins (tip = root, no sweep) take the mid-chord length as the span,
        /// which is the expression Balance already uses for v2 fins.
        /// </summary>
        public static double FinCnAlpha(int count, double span, double rootChord, double tipChord, double leSweep, double dRef, double bodyRadius)
        {
            if (count <= 0 || !(span > 0) || !(dRef > 0)) return 0;
            double sum = rootChord + tipChord;
            if (!(sum > 0)) return 0;
            double mid = leSweep + (tipChord - rootChord) / 2;
            double lf = mid == 0 ? span : Math.Sqrt(span * span + mid * mid);
            double cna1 = (4.0 * count * (span / dRef) * (span / dRef)) / (1 + Math.Sqrt(1 + (2 * lf / sum) * (2 * lf / sum)));
            double kfb = 1 + bodyRadius / (span + bodyRadius);
            return kfb * cna1;
        }

        public static bool TryFixedFin(Design d, out FinGeometry geom)
        {
            geom = default;
            if (!HasV3FinGeometry(d)) return false;
            var row = Tables().Fin(d.finPlanform);
            int count = d.finCountSet ? d.finCount : row != null ? row.count : d.finCount;
            if (count < 3) count = 3;
            if (count > 8) count = 8;
            double span = d.finSpanSet ? d.finSpanM : row != null && row.span > 0 ? row.span : d.finSpanM;
            double root = d.finRootChordSet ? d.finRootChord : row != null && row.rootChord > 0 ? row.rootChord : span;
            if (!(span > 0) || !(root > 0)) return false;
            Planform(d.finPlanform, span, root, out double tip, out double sweep);
            geom = new FinGeometry { count = count, span = span, rootChord = root, tipChord = tip, leSweep = sweep };
            return true;
        }

        /// <summary>§2.5 fin mass = catalog areal density × the planform area the player actually drew.</summary>
        public static double FinMassKg(Design d)
        {
            if (!TryFixedFin(d, out var geom)) return 0;
            var row = Tables().Fin(d.finPlanform);
            if (row == null || !(row.mass > 0) || !(row.rootChord > 0) || !(row.span > 0) || row.count <= 0) return 0;
            Planform(row.planform, row.span, row.rootChord, out double catTip, out _);
            double catArea = row.count * row.span * (row.rootChord + catTip) / 2;
            if (!(catArea > 0)) return 0;
            double area = geom.count * geom.span * (geom.rootChord + geom.tipChord) / 2;
            return row.mass / catArea * area;
        }

        public static void Planform(string planform, double span, double rootChord, out double tipChord, out double leSweep)
        {
            if (planform == "trapezoid")
            {
                tipChord = TrapezoidTipFraction * rootChord;
                leSweep = 0;
                return;
            }
            if (planform == "clipped-delta")
            {
                tipChord = ClippedDeltaTipFraction * rootChord;
                leSweep = rootChord - tipChord;
                return;
            }
            if (planform == "swept")
            {
                tipChord = SweptTipFraction * rootChord;
                leSweep = span;
                return;
            }
            tipChord = rootChord;
            leSweep = 0;
        }

        static double FairingShoulder(Design d)
        {
            if (d?.stages == null || d.stages.Count == 0) return 0;
            double top = d.stages[d.stages.Count - 1].diameter;
            if (!(top > 0) || !(d.fairingDiameter > top)) return 0;
            return FairingShoulderPerRatio * (d.fairingDiameter / top - 1);
        }

        static double FinCd(Design d, double dRef)
        {
            if (d == null || !d.fins || IsGridPlanform(d.finPlanform) || !(dRef > 0)) return 0;
            double area;
            if (TryFixedFin(d, out var geom))
                area = geom.count * geom.span * (geom.rootChord + geom.tipChord) / 2;
            else
            {
                if (!(d.finSpanM > 0) || d.finCount <= 0) return 0;
                double chord = d.finRootChordSet ? d.finRootChord : d.finSpanM;
                area = d.finCount * d.finSpanM * chord;
            }
            double sRef = Math.PI * dRef * dRef / 4;
            if (!(sRef > 0)) return 0;
            return FinCdPlate * area / sRef;
        }

        static AeroTables Tables()
        {
            if (tables != null) return tables;
            lock (Gate)
            {
                if (tables != null) return tables;
                tables = AeroTables.Load(RepoRoot());
            }
            return tables;
        }

        static string RepoRoot()
        {
            var env = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            string start = !string.IsNullOrEmpty(env) ? env : Directory.GetCurrentDirectory();
            try { return Catalog.FindRepoRoot(start); }
            catch (DirectoryNotFoundException) { return Catalog.FindRepoRoot(AppContext.BaseDirectory); }
        }

        sealed class FinRow
        {
            public string planform;
            public int count;
            public double span, rootChord, mass;
        }

        sealed class AeroTables
        {
            readonly Dictionary<string, double> noseExtra = new Dictionary<string, double>();
            readonly Dictionary<string, double> interstage = new Dictionary<string, double>();
            readonly Dictionary<string, FinRow> fins = new Dictionary<string, FinRow>();

            public static AeroTables Load(string root)
            {
                var t = new AeroTables();
                string Read(string name) => File.ReadAllText(Path.Combine(root, "data", name));
                foreach (JsonObject o in MiniJson.ParseObject(Read("fairings.json")).Arr("fairings"))
                {
                    string shape = o.Str("shape");
                    if (string.IsNullOrEmpty(shape) || DesignMath.NoseShapes.ContainsKey(shape) || t.noseExtra.ContainsKey(shape)) continue;
                    t.noseExtra[shape] = o.Num("cd");
                }
                foreach (JsonObject o in MiniJson.ParseObject(Read("interstages.json")).Arr("interstages"))
                {
                    string shape = o.Str("shape");
                    if (!string.IsNullOrEmpty(shape) && !t.interstage.ContainsKey(shape)) t.interstage[shape] = o.Num("dragDeltaCd");
                }
                foreach (JsonObject o in MiniJson.ParseObject(Read("fins.json")).Arr("fins"))
                {
                    string plan = o.Str("planform");
                    if (string.IsNullOrEmpty(plan) || t.fins.ContainsKey(plan)) continue;
                    t.fins[plan] = new FinRow
                    {
                        planform = plan,
                        count = (int)o.Num("count"),
                        span = o.Num("span"),
                        rootChord = o.Num("rootChord"),
                        mass = o.Num("mass"),
                    };
                }
                return t;
            }

            public double Nose(string nose)
            {
                if (string.IsNullOrEmpty(nose)) nose = "ogive";
                if (DesignMath.NoseShapes.TryGetValue(nose, out var table)) return table;
                if (noseExtra.TryGetValue(nose, out var extra)) return extra;
                return UnknownNoseCd;
            }

            public double? InterstageDrag(string shape)
                => shape != null && interstage.TryGetValue(shape, out var cd) ? cd : (double?)null;

            public FinRow Fin(string planform)
                => planform != null && fins.TryGetValue(planform, out var row) ? row : null;
        }
    }
}
