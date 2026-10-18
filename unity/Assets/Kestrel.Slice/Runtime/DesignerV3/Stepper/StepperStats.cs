// Live numbers for one step. Keys come from StepperCatalog.StatKeys, values from Readout.Quick.
// Max-Q stays "—" until the debounced reference ascent fills it.
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;

namespace Kestrel.Slice.DesignerV3.Stepper
{
    public struct StepStat
    {
        public string Key, Label, Value, Hint;
        public CellState State;
        public StepStat(string key, string label, string value, CellState state, string hint = null)
        {
            Key = key; Label = label; Value = value; State = state; Hint = hint;
        }
    }

    public static class StepperStats
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static List<StepStat> Build(DesignerStep step, QuickReadout readout, JsonObject design, Catalog catalog)
        {
            var def = StepperCatalog.Get(step);
            var list = new List<StepStat>(def.StatKeys.Length);
            for (int i = 0; i < def.StatKeys.Length; i++)
                list.Add(Cell(def.StatKeys[i], readout, design, catalog));
            return list;
        }

        public static StepStat Cell(string key, QuickReadout r, JsonObject design, Catalog catalog)
        {
            switch (key)
            {
                case "liftoffTWR":
                    if (r == null || double.IsNaN(r.liftoffTWR)) return Dash("liftoffTWR", "TWR");
                    double shown = Readout.ShownTwr(r.liftoffTWR);
                    var twr = shown >= StepperChecks.PassTwr ? CellState.Good
                        : shown >= StepperChecks.BlockTwr ? CellState.Warn : CellState.Bad;
                    return new StepStat("liftoffTWR", "TWR", shown.ToString("0.00", Inv), twr, "Liftoff thrust over weight");
                case "engineCount":
                    return new StepStat("engineCount", "Engines", ((int)(Stage0(design)?.Num("engineCount") ?? 0)).ToString(Inv), CellState.Neutral);
                case "mixtureOffsetPct":
                    double mix = Stage0(design)?.Num("mixtureOffsetPct") ?? 0;
                    var mixState = System.Math.Abs(mix) > 6 ? CellState.Warn : CellState.Good;
                    return new StepStat("mixtureOffsetPct", "Mix", Signed(mix, "0.0") + "%", mixState, "Mixture offset");
                case "payloadMass":
                    return new StepStat("payloadMass", "Cargo", r == null ? "—" : HangarFormat.Kg(design?.Obj("payload")?.Num("mass") ?? 0), CellState.Neutral);
                case "orbit":
                    string orbit = r?.orbitLabel;
                    if (string.IsNullOrEmpty(orbit)) orbit = design?.Obj("payload")?.Str("orbit");
                    return new StepStat("orbit", "Orbit", string.IsNullOrEmpty(orbit) ? "—" : orbit, CellState.Neutral);
                case "cost":
                    if (r == null) return Dash("cost", "Cost");
                    string cost = DesignerV3Strip.StripMoney(r.cost);
                    var costState = CellState.Neutral;
                    if (r.budget.HasValue && r.budget.Value > 0)
                    {
                        string both = cost + " / " + DesignerV3Strip.StripMoney(r.budget.Value);
                        cost = both.Length > 16 ? cost : both;
                        costState = r.cost <= r.budget.Value ? CellState.Good : CellState.Bad;
                    }
                    return new StepStat("cost", "Cost", cost, costState);
                case "margin":
                    if (r == null || double.IsNaN(r.margin)) return Dash("margin", "Margin");
                    var marginState = r.margin >= 0.05 ? CellState.Good : r.margin >= 0 ? CellState.Warn : CellState.Bad;
                    return new StepStat("margin", "Margin", Signed(r.margin * 100.0, "0.0") + "%", marginState, "Δv margin versus the target");
                case "totalDeltaV":
                    if (r == null || double.IsNaN(r.totalDeltaV)) return Dash("totalDeltaV", "Δv");
                    string dv = r.totalDeltaV >= 1000
                        ? (r.totalDeltaV / 1000.0).ToString("0.00", Inv) + " km/s"
                        : r.totalDeltaV.ToString("0", Inv) + " m/s";
                    return new StepStat("totalDeltaV", "Δv", dv, r.margin >= 0 ? CellState.Good : CellState.Bad);
                case "maxQFraction":
                case "maxQ":
                    return MaxQ(key, r);
                case "safetyFactor":
                    return new StepStat("safetyFactor", "Strength", (design?.Num("safetyFactor", 1.4) ?? 1.4).ToString("0.00", Inv), CellState.Neutral, "Safety factor");
                case "stability":
                    if (r == null) return Dash("stability", "Stable");
                    double stab = r.minStability ?? r.stabilityCal;
                    var stabState = stab < 0 ? CellState.Bad : CellState.Good;
                    return new StepStat("stability", "Stable", stab.ToString("0.00", Inv) + " cal", stabState);
                case "car":
                    if (r == null || !r.minCar.HasValue) return new StepStat("car", "CAR", "—", CellState.Neutral, "Control margin. Fills in after the reference ascent.");
                    var carState = r.minCar.Value < 1 ? CellState.Warn : CellState.Good;
                    return new StepStat("car", "CAR", r.minCar.Value.ToString("0.00", Inv), carState);
                case "fairingFit":
                    if (r == null || !r.fairingFits.HasValue) return new StepStat("fairingFit", "Fit", "—", CellState.Neutral);
                    return new StepStat("fairingFit", "Fit", r.fairingFits.Value ? "Fits" : "No fit", r.fairingFits.Value ? CellState.Good : CellState.Bad);
                case "landingReserve":
                    if (r == null) return Dash("landingReserve", "Reserve");
                    return new StepStat("landingReserve", "Reserve", HangarFormat.Ms(r.landingReserve_mps), CellState.Neutral, "Landing fuel, as Δv");
                case "landingNeed":
                    if (r == null || !r.landingNeed_mps.HasValue) return new StepStat("landingNeed", "Need", "—", CellState.Neutral, "Return burn. Fills in after the reference ascent.");
                    return new StepStat("landingNeed", "Need", HangarFormat.Ms(r.landingNeed_mps.Value), CellState.Neutral);
                case "recoveryMode":
                    return new StepStat("recoveryMode", "Landing", ModeLabel(StepperChecks.RecoveryMode(design, null)), CellState.Neutral);
                case "payloadCost":
                    return PayloadCost(catalog, design);
                case "name":
                    string name = design?.Obj("livery")?.Str("name");
                    if (string.IsNullOrEmpty(name)) name = design?.Str("name");
                    return new StepStat("name", "Name", string.IsNullOrEmpty(name) ? "—" : name, CellState.Neutral);
                case "reliability":
                    if (r == null || double.IsNaN(r.reliability)) return Dash("reliability", "Reliab.");
                    return new StepStat("reliability", "Reliab.", r.reliability.ToString("0.00", Inv), r.reliability >= 0.9 ? CellState.Good : CellState.Warn);
                case "peakG":
                    if (r == null || !r.peakG.HasValue) return new StepStat("peakG", "g", "—", CellState.Neutral, "Peak g. Fills in after the reference ascent.");
                    var gState = r.gLimit > 0 && r.peakG.Value > r.gLimit ? CellState.Bad : CellState.Good;
                    return new StepStat("peakG", "g", r.peakG.Value.ToString("0.0", Inv), gState);
                default:
                    return Dash(key, key);
            }
        }

        static StepStat MaxQ(string key, QuickReadout r)
        {
            if (r == null || r.maxQStale || !r.maxQ_kPa.HasValue)
                return new StepStat(key, "Max-Q", "—", CellState.Neutral, "Fills in after the reference ascent.");
            string val = r.maxQ_kPa.Value.ToString("0", Inv);
            var state = CellState.Neutral;
            if (r.qLim_kPa.HasValue && r.qLim_kPa.Value > 0)
            {
                val += "/" + r.qLim_kPa.Value.ToString("0", Inv);
                double ratio = r.maxQ_kPa.Value / r.qLim_kPa.Value;
                state = ratio > 1 ? CellState.Bad : ratio > StepperChecks.MaxQAmberFraction ? CellState.Warn : CellState.Good;
            }
            return new StepStat(key, "Max-Q", val + " kPa", state);
        }

        static StepStat PayloadCost(Catalog catalog, JsonObject design)
        {
            if (catalog == null || design == null) return Dash("payloadCost", "Cargo");
            try
            {
                var parsed = Design.FromJson(design);
                var trade = RecoveryTradeoff.Measure(catalog, parsed);
                string val = trade.PayloadCostKg.ToString("0", Inv) + " kg";
                var state = trade.PayloadCostKg > 1 ? CellState.Warn : CellState.Neutral;
                return new StepStat("payloadCost", "Cargo", val, state, trade.Kid);
            }
            catch (System.Exception)
            {
                return Dash("payloadCost", "Cargo");
            }
        }

        static StepStat Dash(string key, string label) => new StepStat(key, label, "—", CellState.Neutral);

        static JsonObject Stage0(JsonObject design) => design?.Arr("stages") != null && design.Arr("stages").Count > 0 ? design.Arr("stages")[0] as JsonObject : null;

        static string Signed(double value, string format)
        {
            string n = value.ToString(format, Inv);
            return value > 0 ? "+" + n : n;
        }

        static string ModeLabel(string mode)
        {
            switch (mode)
            {
                case "rtls": return "Pad";
                case "barge": return "Barge";
                default: return "Expend";
            }
        }
    }
}
