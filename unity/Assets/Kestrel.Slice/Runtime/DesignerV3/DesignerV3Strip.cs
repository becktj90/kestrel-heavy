// Sticky strip cells: Δv vs target, TWR, max-Q gauge, cost/budget, risk chips (§5 / §6).
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Slice;

namespace Kestrel.Slice.DesignerV3
{
    public struct StripCell
    {
        public string id, label, value, hint;
        public CellState state;
        public StripCell(string id, string label, string value, CellState state, string hint = null)
        {
            this.id = id; this.label = label; this.value = value; this.state = state; this.hint = hint;
        }
    }

    public static class DesignerV3Strip
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static List<StripCell> Cells(QuickReadout r, bool engineer)
        {
            var list = new List<StripCell>(6);
            if (r == null)
            {
                list.Add(new StripCell("dv", engineer ? "Δv" : "Speed", "—", CellState.Neutral));
                list.Add(new StripCell("twr", engineer ? "TWR" : "Push", "—", CellState.Neutral));
                list.Add(new StripCell("q", "Max-Q", "—", CellState.Neutral));
                list.Add(new StripCell("cost", "Cost", "—", CellState.Neutral));
                return list;
            }

            double marginPct = r.margin * 100.0;
            string dvVal = engineer
                ? Math.Round(r.totalDeltaV).ToString("N0", Inv) + " / " + Math.Round(r.required).ToString("N0", Inv)
                : Math.Round(marginPct).ToString(Inv) + "%";
            CellState dvState = r.margin >= 0.05 ? CellState.Good : r.margin >= 0 ? CellState.Warn : CellState.Bad;
            list.Add(new StripCell("dv", engineer ? "Δv" : "Speed", dvVal, dvState,
                "Margin " + marginPct.ToString("0.0", Inv) + "% vs " + (r.orbitLabel ?? "target")));

            double twr = Readout.ShownTwr(r.liftoffTWR);
            CellState twrState = twr >= 1.2 ? CellState.Good : twr >= 1.05 ? CellState.Warn : CellState.Bad;
            list.Add(new StripCell("twr", engineer ? "TWR" : "Push", twr.ToString("0.00", Inv), twrState));

            string qVal;
            CellState qState = CellState.Neutral;
            if (r.maxQStale || !r.maxQ_kPa.HasValue) qVal = "Run a sim";
            else
            {
                qVal = r.maxQ_kPa.Value.ToString("0.0", Inv) + " kPa";
                if (r.qLim_kPa.HasValue)
                {
                    double ratio = r.maxQ_kPa.Value / r.qLim_kPa.Value;
                    qState = ratio > 1 ? CellState.Bad : ratio > 0.85 ? CellState.Warn : CellState.Good;
                    qVal += " / " + r.qLim_kPa.Value.ToString("0.0", Inv);
                }
            }
            list.Add(new StripCell("q", "Max-Q", qVal, qState));

            string costVal = StripMoney(r.cost);
            string costHint = r.cost.ToString("N0", Inv);
            CellState costState = CellState.Neutral;
            if (r.budget.HasValue && r.budget.Value > 0)
            {
                string both = costVal + " / " + StripMoney(r.budget.Value);
                costVal = both.Length > 16 ? costVal : both;
                costHint += " / " + r.budget.Value.ToString("N0", Inv);
                costState = r.cost <= r.budget.Value ? CellState.Good : CellState.Bad;
            }
            list.Add(new StripCell("cost", "Cost", costVal, costState, costHint));
            return list;
        }

        /// <summary>Strip cost. 1660781357 → "$1.66B". The exact integer stays on the tooltip.</summary>
        public static string CompactMoney(long amount)
        {
            double v = amount;
            double a = Math.Abs(v);
            string body;
            if (a >= 1e9) body = (v / 1e9).ToString("0.00", Inv) + "B";
            else if (a >= 1e6) body = (v / 1e6).ToString("0.00", Inv) + "M";
            else if (a >= 1e3) body = (v / 1e3).ToString("0.00", Inv) + "k";
            else body = v.ToString("N0", Inv);
            return "$" + body;
        }

        /// <summary>One token, no cents, so a narrow cell does not wrap "$516.30M" into "$516.3" and "0M".</summary>
        public static string StripMoney(long amount)
        {
            double v = amount;
            double a = Math.Abs(v);
            string body;
            if (a >= 1e9) body = (v / 1e9).ToString("0", Inv) + "B";
            else if (a >= 1e6) body = (v / 1e6).ToString("0", Inv) + "M";
            else if (a >= 1e3) body = (v / 1e3).ToString("0", Inv) + "k";
            else body = v.ToString("N0", Inv);
            return "$" + body;
        }

        public static List<FlightRisk> RiskChips(QuickReadout r)
        {
            var outList = new List<FlightRisk>();
            if (r?.risks == null) return outList;
            for (int i = 0; i < r.risks.Count; i++)
            {
                var risk = r.risks[i];
                if (risk == null) continue;
                if (risk.level == "red" || risk.level == "amber") outList.Add(risk);
            }
            return outList;
        }

        public static bool HasRedRisk(QuickReadout r)
        {
            if (r?.risks == null) return false;
            for (int i = 0; i < r.risks.Count; i++)
                if (r.risks[i] != null && r.risks[i].level == "red") return true;
            return false;
        }
    }
}
