// Words, numbers and layout metrics for the Hangar panel. Pure functions, so EditMode tests pin them.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Slice
{
    public enum CellState { Good, Warn, Bad, Neutral }

    public struct ReadoutCell
    {
        public string label, value, hint;
        public CellState state;
        public ReadoutCell(string label, string value, CellState state, string hint = null) { this.label = label; this.value = value; this.state = state; this.hint = hint; }
    }

    public static class HangarFormat
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Tons(double kg) => kg >= 100000 ? (kg / 1000).ToString("N0", Inv) + " t" : kg >= 10000 ? (kg / 1000).ToString("0", Inv) + " t" : (kg / 1000).ToString("0.0", Inv) + " t";
        public static string Kg(double kg) => kg.ToString("N0", Inv) + " kg";
        public static string Ms(double v) => v.ToString("N0", Inv) + " m/s";
        public static string Metres(double m) => m.ToString("0.0", Inv) + " m";
        public static string Ratio(double x) => x.ToString("0.00", Inv);
        public static string Kc(double kc) => kc.ToString("N0", Inv) + " Kc";
        public static string Kn(double n) => n >= 1e6 ? (n / 1e6).ToString("0.00", Inv) + " MN" : (n / 1000).ToString("N0", Inv) + " kN";
        public static string Pct(double f) => Math.Round(f * 100).ToString(Inv) + "%";

        /// <summary>Propellant step: 1 t under 100 t, 10 t under 1000 t, 50 t above (DESIGNER_SPEC §1).</summary>
        public static double PropStepKg(double kg) => kg < 100000 ? 1000 : kg < 1000000 ? 10000 : 50000;

        /// <summary>Next propellant load for a stepper press, snapped to the step grid.</summary>
        public static double StepProp(double kg, int dir)
        {
            double step = PropStepKg(dir < 0 ? Math.Max(0, kg - 1) : kg);
            double snapped = Math.Round(kg / step) * step;
            double next = snapped + dir * step;
            if (dir > 0 && snapped > kg) next = snapped;
            if (dir < 0 && snapped < kg) next = snapped;
            return Math.Max(step, next);
        }

        /// <summary>
        /// Readout strip. Kid: speed budget, liftoff push, cost. Engineer adds liftoff mass, stability and reliability.
        /// </summary>
        public static List<ReadoutCell> Cells(DataSheet s, bool engineer)
        {
            var o = new List<ReadoutCell>();
            if (s == null)
            {
                o.Add(new ReadoutCell(engineer ? "Δv" : "Speed", "—", CellState.Bad));
                o.Add(new ReadoutCell(engineer ? "TWR" : "Push", "—", CellState.Bad));
                o.Add(new ReadoutCell("Cost", "—", CellState.Neutral));
                return o;
            }
            var speed = s.margin >= 0.05 ? CellState.Good : s.margin >= 0 ? CellState.Warn : CellState.Bad;
            o.Add(engineer
                ? new ReadoutCell("Δv", $"{Ms(s.totalDeltaV_mps)} / {Ms(s.requiredDeltaV_mps)}", speed, $"margin {Pct(s.margin)} · {s.orbitLabel}")
                : new ReadoutCell("Speed", Pct(Math.Max(0, s.totalDeltaV_mps / Math.Max(1, s.requiredDeltaV_mps))), speed, s.ReachesOrbit ? "enough for the trip" : "not enough yet"));
            var push = s.liftoffTwr <= 1 ? CellState.Bad : s.liftoffTwr < 1.25 ? CellState.Warn : CellState.Good;
            o.Add(engineer
                ? new ReadoutCell("TWR", Ratio(s.liftoffTwr), push, Kn(s.liftoffThrust_N))
                : new ReadoutCell("Push", Ratio(s.liftoffTwr) + "×", push, s.liftoffTwr > 1 ? "× its weight" : "too heavy"));
            var cost = s.overBudget ? CellState.Bad : s.nearBudget ? CellState.Warn : CellState.Good;
            o.Add(new ReadoutCell("Cost", Kc(s.cost_Kc), cost, s.budget_Kc > 0 ? "budget " + Kc(s.budget_Kc) : null));
            if (engineer)
            {
                o.Add(new ReadoutCell("Mass", Tons(s.liftoffMass_kg), CellState.Neutral, "dry " + Tons(s.dryMass_kg)));
                var st = s.stability == StabilityBand.Unstable ? CellState.Bad
                    : s.stability == StabilityBand.Twitchy || s.stability == StabilityBand.Overstable ? CellState.Warn : CellState.Good;
                o.Add(new ReadoutCell("Stability", s.stabilityCal.ToString("0.0", Inv) + " cal", st, s.StabilityWord));
                o.Add(new ReadoutCell("Reliability", (s.reliability * 100).ToString("0.0", Inv) + "%", CellState.Neutral));
            }
            return o;
        }

        /// <summary>One-line summary under a part in the stack list ("9 × Gas-generator kerolox · 300 t").</summary>
        public static string Subtitle(Catalog c, JsonObject design, PartRef p)
        {
            switch (p.kind)
            {
                case PartKind.Stage:
                case PartKind.Boosters:
                {
                    var s = HangarParts.Body(design, p);
                    if (s == null) return "";
                    string eng = c.EngineById.TryGetValue(s.Str("engine") ?? "", out var e) ? e.label : s.Str("engine");
                    return $"{(int)s.Num("engineCount")} × {eng} · {Tons(s.Num("propMass"))}";
                }
                case PartKind.Interstage:
                    return Cap(HangarParts.Body(design, p)?.Obj("interstage")?.Str("shape") ?? "straight");
                case PartKind.Fairing:
                {
                    var f = design.Obj("fairing");
                    return $"{Cap(design.Obj("aero")?.Str("nose") ?? "ogive")} · {Metres(f?.Num("diameter") ?? 0)} × {Metres(f?.Num("length") ?? 0)}";
                }
                case PartKind.Payload:
                {
                    var pl = design.Obj("payload");
                    string id = pl?.Str("type");
                    string name = id != null && c.PayloadById.TryGetValue(id, out var x) ? x.displayName ?? x.label : "Cargo";
                    string orbit = pl?.Str("orbit");
                    string where = orbit != null && c.Orbits.TryGetValue(orbit, out var o) ? o.label : orbit;
                    return $"{name} · {Kg(pl?.Num("mass") ?? 0)} → {where}";
                }
            }
            return "";
        }

        public static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).Replace('-', ' ');

        /// <summary>Short engine name for chips: the catalog label without the propellant words.</summary>
        public static string EngineChip(Engine e)
        {
            if (e == null) return "?";
            string l = e.label ?? e.id;
            foreach (var w in new[] { " kerolox", " methalox", " hydrolox", " (vacuum nozzle)", " upper", " motor" })
                l = l.Replace(w, w == " (vacuum nozzle)" ? " vac" : "");
            return l.Trim();
        }
    }

    /// <summary>Panel geometry in points. Phones: a bottom sheet that leaves at least DesignFraming.MinVisibleFraction
    /// of the screen for the rocket. iPad (portrait, wider than 0.6): a 400 pt panel on the right, full height.</summary>
    public static class HangarPanelLayout
    {
        /// <summary>Handle, readout strip, tabs and the Launch row: the parts of the sheet that never scroll.</summary>
        public const float FixedPt = 56f + 56f + 52f + 60f;
        public const float SheetMaxFraction = 0.62f;
        public const float MinContentPt = 120f;

        public static bool SidePanel(float screenWpx, float screenHpx) => DesignFraming.HasSidePanel(screenWpx, screenHpx);

        /// <summary>Scrolling content height on a phone: whatever the 62% sheet leaves after the fixed rows, at least 120 pt.</summary>
        public static float ContentMaxPt(float screenHeightPt, float safeBottomPt)
        {
            if (!(screenHeightPt > 0)) return MinContentPt;
            return Math.Max(MinContentPt, screenHeightPt * SheetMaxFraction - FixedPt - Math.Max(0, safeBottomPt));
        }
    }
}
