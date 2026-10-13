// Cost model (E1). Integer minor units (1 Kc = 100,000 minor), stored in long. Each line item
// is rounded once (JS Math.round semantics) and only integers are summed. Port of
// web/js/economy/costModel.js (priceDesign + helpers; formatKc/marginalHints are UI-side).
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Economy
{
    public sealed class CostLine { public string cat, label, group; public long amount; }
    public sealed class Quote
    {
        public long recurring, dev, total; public long? perKg;
        public List<CostLine> lines = new List<CostLine>(), devLines = new List<CostLine>();
        public Dictionary<string, long> byCategory = new Dictionary<string, long>();
    }
    public sealed class CompanyState { public Dictionary<string, int> unitsBuilt = new Dictionary<string, int>(); public HashSet<string> devDone = new HashSet<string>(); }

    public static class CostModel
    {
        public static readonly string[] CATEGORIES = { "engines", "structure", "propellant", "avionics", "recovery", "ops" };
        static long Int(double x) => (long)Math.Max(0, JsMath.Round(x));
        static double N(JsonObject o, string k) => o.Num(k);
        static double Map(JsonObject o, string k, double def) => o.TryGetValue(k ?? "", out var v) && v is double d ? d : def;

        public static long MinorPerKc(Catalog c) => (long)(N(c.Econ.Obj("currency"), "minorPerCredit") * N(c.Econ.Obj("currency"), "creditsPerKc"));

        public static double EngineTrl(Catalog c, string id) { var E = c.Econ.Obj("engine"); return Map(E.Obj("trl"), id, N(E, "defaultTrl")); }
        public static double LearningFactor(double n, double rate) => Math.Pow(Math.Max(1, n), Log2(rate));
        public static double LearningFactor(Catalog c, double n) => LearningFactor(n, N(c.Econ.Obj("engine"), "learningRate"));
        // JS Math.log2. .NET Standard 2.1 has no Math.Log2; Log(x)/Log(2) can differ by 1 ulp, see README tolerance notes.
        static double Log2(double x) => Math.Log(x) / Math.Log(2);

        public static long EngineDevCost(Catalog c, Engine e)
        {
            var E = c.Econ.Obj("engine");
            double F = Math.Max(e.thrustVac, e.thrustSL) / N(E, "refThrustN");
            double cyc = Map(E.Obj("cycleMultiplier"), e.cycle, 1), trl = EngineTrl(c, e.id);
            return Int(N(E, "kDev") * Math.Pow(F, N(E, "devThrustExp")) * cyc * Math.Pow(e.pc / N(E, "refPcBar"), N(E, "devPcExp")) * (1 + N(E, "trlPenaltyPerLevel") * (9 - trl)));
        }

        static double EngineT1(Catalog c, Engine e)
        {
            var E = c.Econ.Obj("engine");
            double F = Math.Max(e.thrustVac, e.thrustSL) / N(E, "refThrustN");
            return N(E, "kUnit") * Math.Pow(F, N(E, "unitThrustExp")) * Map(E.Obj("cycleMultiplier"), e.cycle, 1) * Math.Pow(e.pc / N(E, "refPcBar"), N(E, "unitPcExp"));
        }

        public static long EngineBatchCost(Catalog c, Engine e, int count, int built = 0)
        {
            double t1 = EngineT1(c, e); long sum = 0;
            for (int i = 1; i <= count; i++) sum += Int(t1 * LearningFactor(c, built + i));
            return sum;
        }

        public static double TankArea(Catalog c, StagePart part)
        {
            if (!c.PropellantById.TryGetValue(part.propellant ?? "", out var prop) || part.propMass == 0 || part.diameter == 0) return 0;
            double d = part.diameter, vol = part.propMass / prop.bulkDensity, len = vol / (Math.PI * d * d / 4);
            return Math.PI * d * len + Math.PI * d * d / 2;
        }

        static long StructureCost(Catalog c, Design d, StagePart part, double sf)
        {
            var S = c.Econ.Obj("structure");
            c.PropellantById.TryGetValue(part.propellant ?? "", out var prop);
            double cryo = prop?.storability == "deep-cryogenic" ? 1 + N(S, "deepCryoFactor") : 1;
            double sfk = Math.Pow(Math.Max(1, sf) / N(S, "refSafetyFactor"), N(S, "safetyFactorExp"));
            double m = Mass.StructureMass(c, part, sf) + InterstageAero.MassOf(c, d, part) + part.separationMass;
            return Int((m * Map(S.Obj("materialPerKg"), part.material, 0) + TankArea(c, part) * Map(S.Obj("materialPerM2"), part.material, 0)) * cryo * sfk);
        }

        static long PropellantCost(Catalog c, string propId, double massKg)
        {
            var P = c.Econ.Obj("propellant");
            if (!c.PropellantById.TryGetValue(propId ?? "", out var prop)) return 0;
            return Int(massKg * Map(P.Obj("perKg"), propId, 0) * (1 + prop.boiloffPerHour * N(P, "standbyHours")));
        }

        sealed class Group { public StagePart p; public string key, label; public int n; }

        public static Quote PriceDesign(Catalog c, Design d, CompanyState cs = null)
        {
            var built = new Dictionary<string, int>(cs?.unitsBuilt ?? new Dictionary<string, int>());
            var devDone = new HashSet<string>(cs?.devDone ?? new HashSet<string>());
            double sf = d.safetyFactor;
            var q = new Quote();
            void Add(string cat, string label, long amount, string group) { if (amount > 0) q.lines.Add(new CostLine { cat = cat, label = label, amount = amount, group = group }); }
            var gs = new List<Group>();
            for (int i = 0; i < d.stages.Count; i++) gs.Add(new Group { p = d.stages[i], key = $"stage{i}", label = string.IsNullOrEmpty(d.stages[i].name) ? $"Stage {i + 1}" : d.stages[i].name, n = 1 });
            if (d.StrapOnCount > 0) gs.Add(new Group { p = d.strapOns, key = "strapons", label = $"{d.strapOns.count} strap-ons", n = d.strapOns.count });
            int nStages = d.stages.Count;

            foreach (var g in gs)
            {
                var p = g.p;
                if (c.EngineById.TryGetValue(p.engine ?? "", out var e))
                {
                    int count = p.engineCount * g.n;
                    built.TryGetValue(e.id, out var b);
                    Add("engines", $"{g.label}: {count}× {e.label}", EngineBatchCost(c, e, count, b), g.key);
                    built[e.id] = b + count;
                    if (!devDone.Contains(e.id)) { devDone.Add(e.id); q.devLines.Add(new CostLine { cat = "engines", label = $"Develop {e.label}", amount = EngineDevCost(c, e) }); }
                }
                Add("structure", $"{g.label}: tanks & structure", StructureCost(c, d, p, sf) * g.n, g.key);
                string plabel = c.PropellantById.TryGetValue(p.propellant ?? "", out var pr) ? pr.label : p.propellant;
                Add("propellant", $"{g.label}: {plabel}", PropellantCost(c, p.propellant, p.propMass + p.landingPropMass) * g.n, g.key);
            }
            var mass = Mass.Of(c, d);
            var S = c.Econ.Obj("structure");
            if (mass.fairing != 0)
            {
                double area = Math.PI * d.fairingDiameter * d.fairingLength * 0.75;
                Add("structure", "Fairing", Int(mass.fairing * Map(S.Obj("materialPerKg"), d.fairingMaterial, 0) + area * Map(S.Obj("materialPerM2"), d.fairingMaterial, 0)), "fairing");
            }
            int seps = nStages - 1 + (d.StrapOnCount > 0 ? 1 : 0);
            Add("structure", $"Separation systems ({seps})", seps * (long)N(S, "separationEvent"), "sep");
            var AV = c.Econ.Obj("avionics");
            string tier = d.avionicsTier != null && AV.Obj(d.avionicsTier) != null ? d.avionicsTier : "standard";
            var av = AV.Obj(tier);
            Add("avionics", $"Avionics ({tier})", Int(N(av, "unit") * (1 + N(AV, "perExtraStage") * (nStages - 1))), "avionics");
            if (N(av, "dev") != 0 && !devDone.Contains("avionics:" + tier)) q.devLines.Add(new CostLine { cat = "avionics", label = $"Qualify {tier} avionics", amount = (long)N(av, "dev") });
            var R = c.Econ.Obj("recovery");
            Add("recovery", "Recovery hardware", (long)((d.legs ? N(R, "legs") : 0) + (d.gridFins ? N(R, "gridFins") : 0) + (d.rcs ? N(R, "rcs") : 0)), "recovery");
            var O = c.Econ.Obj("ops");
            double maxD = double.NegativeInfinity;
            foreach (var g in gs) maxD = Math.Max(maxD, g.p.diameter);
            maxD = Math.Max(maxD, d.fairingDiameter);
            Add("ops", "Range fee", (long)N(O, "rangeFee"), "ops");
            Add("ops", "Crew & standby", (long)(N(O, "crewDay") * (N(O, "baseDays") + N(O, "daysPerStage") * nStages)), "ops");
            Add("ops", "Integration", (long)(N(O, "integrationPerStage") * (nStages + d.StrapOnCount)), "ops");
            bool over = maxD > N(O, "oversizeDiameterM");
            Add("ops", over ? "Transport (oversize)" : "Transport", Int(N(O, "transport") * (over ? N(O, "oversizeFactor") : 1)), "ops");

            foreach (var k in CATEGORIES) q.byCategory[k] = 0;
            foreach (var l in q.lines) q.byCategory[l.cat] += l.amount;
            foreach (var l in q.lines) q.recurring += l.amount;
            foreach (var l in q.devLines) q.dev += l.amount;
            q.total = q.recurring + q.dev;
            q.perKg = d.payloadMass > 0 ? (long)JsMath.Round(q.recurring / d.payloadMass) : (long?)null;
            return q;
        }

        /// <summary>
        /// Recovered hardware refunds 70% minus refurbishment. Refurbishment is
        /// (10% + 5% × prior flights) of that hardware. The result is minor units, never negative.
        /// </summary>
        public const double ReuseRefundFraction = 0.70;
        public const double RefurbBase = 0.10;
        public const double RefurbPerFlight = 0.05;

        public static long ReuseRefundMinor(long hardwareMinor, int priorFlights)
        {
            if (hardwareMinor <= 0) return 0;
            int flown = priorFlights < 0 ? 0 : priorFlights;
            double fraction = ReuseRefundFraction - (RefurbBase + RefurbPerFlight * flown);
            if (fraction <= 0) return 0;
            return (long)JsMath.Round(hardwareMinor * fraction);
        }

        /// <summary>
        /// Engines, tanks and recovery hardware that come home. Avionics, propellant and ops stay spent.
        /// Stage groups are "stage0" and, when the boosters fly home too, "strapons".
        /// </summary>
        public static long RecoveredHardwareMinor(Quote quote, bool stage1, bool boosters)
        {
            if (quote == null) return 0;
            long sum = 0;
            foreach (var line in quote.lines)
            {
                if (line.cat != "engines" && line.cat != "structure" && line.cat != "recovery") continue;
                bool stage = stage1 && line.group == "stage0";
                bool strap = boosters && line.group == "strapons";
                bool gear = (stage1 || boosters) && line.group == "recovery";
                if (stage || strap || gear) sum += line.amount;
            }
            return sum;
        }

        /// <summary>"850 Kc", "34.1k Kc", "1.23M Kc".</summary>
        public static string FormatKc(Catalog c, long minor)
        {
            double kc = (double)minor / MinorPerKc(c), a = Math.Abs(kc);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (a >= 1e6) return (kc / 1e6).ToString("N2", ci) + "M Kc";
            if (a >= 1e3) return (kc / 1e3).ToString("N1", ci) + "k Kc";
            return kc.ToString("N0", ci) + " Kc";
        }
    }

    public sealed class StageReliability { public string engine; public int count, tolerance; public double reliability; }

    public static class Reliability
    {
        static double Binom(int n, int k) { double c = 1; for (int i = 1; i <= k; i++) c = c * (n - i + 1) / i; return c; }

        public static int EngineOutTolerance(Catalog c, int n, Engine e)
        {
            var R = c.Econ.Obj("reliability");
            if (e == null || e.cycle == "solid" || n < R.Num("engineOutMinCount")) return 0;
            return (int)Math.Max(1, Math.Floor(n / R.Num("engineOutPerEngines")));
        }

        public static double ClusterReliability(Catalog c, int n, double r, int k)
        {
            if (n <= 0) return 1;
            double p = 1 - r, qb = p * (1 - c.Econ.Obj("reliability").Num("catastrophicFraction"));
            double s = 0;
            for (int j = 0; j <= Math.Min(k, n); j++) s += Binom(n, j) * Math.Pow(qb, j) * Math.Pow(r, n - j);
            return s;
        }

        public static (double reliability, List<StageReliability> stages) Of(Catalog c, Design d)
        {
            var R = c.Econ.Obj("reliability"); var AV = c.Econ.Obj("avionics");
            string tier = d.avionicsTier != null && AV.Obj(d.avionicsTier) != null ? d.avionicsTier : "standard";
            double rel = AV.Obj(tier).Num("reliability");
            var groups = new List<(StagePart p, int n)>();
            foreach (var s in d.stages) groups.Add((s, 1));
            if (d.StrapOnCount > 0) groups.Add((d.strapOns, d.strapOns.count));
            var stages = new List<StageReliability>();
            foreach (var (p, n) in groups)
            {
                if (!c.EngineById.TryGetValue(p.engine ?? "", out var e)) continue;
                int tol = EngineOutTolerance(c, p.engineCount, e);
                double r = Math.Pow(ClusterReliability(c, p.engineCount, e.reliability, tol), n);
                stages.Add(new StageReliability { engine = e.id, count = p.engineCount * n, tolerance = tol, reliability = r });
                rel *= r;
            }
            int seps = d.stages.Count - 1 + (d.StrapOnCount > 0 ? 1 : 0);
            rel *= Math.Pow(R.Num("separationEvent"), seps);
            double sf = d.safetyFactor;
            if (sf < R.Num("lowSafetyFactor")) rel *= R.Num("lowSafetyPenalty"); else if (sf >= R.Num("highSafetyFactor")) rel *= R.Num("highSafetyBonus");
            return (Math.Min(R.Num("cap"), rel), stages);
        }
    }
}
