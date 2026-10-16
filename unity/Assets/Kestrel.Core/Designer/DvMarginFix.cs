// Fix it for a negative Δv margin. Adds propellant, widens tanks, adds a stage, or swaps in a
// higher-Isp engine until margin is at least zero, without going over the design budget or the
// tank and stage limits. One call is one design; the session records it as a single undo step.
// When the destination cannot be reached, the reason names the limit and what to change.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public sealed class DvFixResult
    {
        public bool Solved;
        public bool Changed;
        public string Reason = "";
        public JsonObject Design;
        public readonly List<string> Changes = new List<string>();
        public double Margin = double.NaN;
        public double MarginMps = double.NaN;
        public double RequiredMps = double.NaN;
        public double DeltaV;
    }

    public static class DvMarginFix
    {
        static DesignerLimits Lim => DesignerLimits.Active;

        public static DvFixResult Solve(Catalog catalog, JsonObject design)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (design == null) throw new ArgumentNullException(nameof(design));
            var work = new Worker(catalog, design);
            return work.Run();
        }

        sealed class Worker
        {
            readonly Catalog catalog;
            readonly JsonObject original;
            readonly JsonObject d;
            readonly double budget;
            readonly string orbitId;
            readonly List<string> changes = new List<string>();
            bool hitBudget, hitMass;

            public Worker(Catalog catalog, JsonObject design)
            {
                this.catalog = catalog;
                original = design;
                d = JsonMut.CloneObject(design);
                budget = BudgetOf(design);
                orbitId = design.Obj("payload")?.Str("orbit");
            }

            public DvFixResult Run()
            {
                if (string.IsNullOrEmpty(orbitId) || !catalog.Orbits.ContainsKey(orbitId))
                    return Fail(UnknownOrbit(orbitId));
                var start = Measure(d);
                if (!start.Ok)
                    return Fail(Explain(double.NaN, "The speed for this trip could not be worked out.", false, false));
                if (Reached(start))
                    return Done(start, false);

                bool skipGrow = false, skipThrust = false, skipIsp = false, skipStage = false;
                for (int iter = 0; iter < 80; iter++)
                {
                    var now = Measure(d);
                    if (Reached(now) && PackOk(d)) break;
                    bool moved = false;
                    if (!skipGrow && now.Ok && now.Twr > 1.2)
                    {
                        moved = TryGrow();
                        skipGrow = !moved;
                    }
                    if (!moved && !skipThrust && (!now.Ok || now.Twr < 1.35))
                    {
                        moved = TryThrust();
                        skipThrust = !moved;
                    }
                    if (!moved && !skipGrow)
                    {
                        moved = TryGrow();
                        skipGrow = !moved;
                    }
                    if (!moved && !skipIsp)
                    {
                        moved = TryIsp();
                        skipIsp = !moved;
                    }
                    if (!moved && !skipStage)
                    {
                        moved = TryAddStage();
                        skipStage = !moved;
                    }
                    if (!moved) break;
                    skipGrow = skipThrust = skipIsp = skipStage = false;
                }

                var end = Measure(d);
                if (Reached(end) && PackOk(d) && Afford(end))
                    return Done(end, changes.Count > 0);
                double shortfall = start.Ok ? Math.Max(0, start.Required - start.Dv) : double.NaN;
                bool mass = hitMass || AllTanksFull();
                return Fail(Explain(shortfall, null, hitBudget, mass));
            }

            DvFixResult Done(Metrics m, bool changed)
            {
                var result = new DvFixResult
                {
                    Solved = true,
                    Changed = changed,
                    Design = changed ? JsonMut.CloneObject(d) : JsonMut.CloneObject(original),
                    Margin = m.Margin,
                    MarginMps = m.Dv - m.Required,
                    RequiredMps = m.Required,
                    DeltaV = m.Dv,
                };
                result.Changes.AddRange(changes);
                return result;
            }

            DvFixResult Fail(string reason)
            {
                var result = new DvFixResult
                {
                    Solved = false,
                    Changed = false,
                    Reason = reason,
                    Design = JsonMut.CloneObject(original),
                };
                var start = Measure(original);
                if (start.Ok)
                {
                    result.Margin = start.Margin;
                    result.MarginMps = start.Dv - start.Required;
                    result.RequiredMps = start.Required;
                    result.DeltaV = start.Dv;
                }
                return result;
            }

            bool TryGrow()
            {
                var stages = d.Arr("stages");
                if (stages == null || stages.Count == 0) return false;
                bool room = false;
                for (int i = stages.Count - 1; i >= 0; i--)
                {
                    var s = stages[i] as JsonObject;
                    if (s == null) continue;
                    double cur = s.Num("propMass");
                    if (cur >= Lim.propMassMax - 1) continue;
                    room = true;
                    double big = Math.Round(Math.Min(Lim.propMassMax, Math.Max(cur + 800, cur * 1.2)));
                    double small = Math.Round(Math.Min(Lim.propMassMax, cur + 800));
                    int index = i;
                    if (big > cur + 0.5 && Commit(() => HangarEditor.SetPropMass(d, PartRef.Stage(index), big), false, "Added propellant"))
                        return true;
                    if (small > cur + 0.5 && small < big - 0.5 && Commit(() => HangarEditor.SetPropMass(d, PartRef.Stage(index), small), false, "Added propellant"))
                        return true;
                }
                if (!room) hitMass = true;
                return false;
            }

            bool TryThrust()
            {
                var s0 = Stage(0);
                if (s0 == null) return false;
                int count = (int)s0.Num("engineCount");
                if (count >= Lim.engineCountMax && s0.Num("diameter") >= Lim.diameterMax - 0.05) return false;
                int next = Math.Min(Lim.engineCountMax, count + 1);
                if (next == count) return false;
                return Commit(() =>
                {
                    HangarEditor.SetEngineCount(d, PartRef.Stage(0), next);
                    WidenToFit(0);
                    FixStackWidths();
                }, true, "Added first-stage engines");
            }

            bool TryIsp()
            {
                var stages = d.Arr("stages");
                if (stages == null) return false;
                for (int i = stages.Count - 1; i >= 0; i--)
                {
                    var options = Candidates(i, 6);
                    for (int k = 0; k < options.Count; k++)
                    {
                        var opt = options[k];
                        int index = i;
                        if (Commit(() =>
                        {
                            HangarEditor.SetEngine(catalog, d, PartRef.Stage(index), opt.Id);
                            HangarEditor.SetNozzle(catalog, d, PartRef.Stage(index), opt.Nozzle);
                            if (index > 0) HangarEditor.SetEngineCount(d, PartRef.Stage(index), 1);
                            WidenToFit(index);
                            FixStackWidths();
                        }, false, "Swapped to a higher-Isp engine"))
                            return true;
                    }
                }
                return false;
            }

            bool TryAddStage()
            {
                var stages = d.Arr("stages");
                if (stages == null || stages.Count >= Lim.stagesMax)
                {
                    hitMass = true;
                    return false;
                }
                double[] loads = { 80000, 40000, 20000, 10000 };
                for (int i = 0; i < loads.Length; i++)
                {
                    double load = loads[i];
                    if (Commit(() => FuelNewStage(load), false, "Added a stage")) return true;
                }
                return false;
            }

            void FuelNewStage(double load)
            {
                int at = d.Arr("stages").Count;
                if (!HangarEditor.AddStage(catalog, d, at).changed)
                    throw new InvalidOperationException("no stage");
                int idx = d.Arr("stages").Count - 1;
                AssignBest(idx);
                HangarEditor.SetPropMass(d, PartRef.Stage(idx), load);
                WidenToFit(idx);
                int guard = 0;
                while (guard++ < 12)
                {
                    var twr = Measure(d);
                    if (twr.Ok && twr.Twr > Lim.minTwr) break;
                    var s0 = Stage(0);
                    if (s0 == null) break;
                    int count = (int)s0.Num("engineCount");
                    if (count >= Lim.engineCountMax) break;
                    HangarEditor.SetEngineCount(d, PartRef.Stage(0), count + 1);
                    WidenToFit(0);
                    FixStackWidths();
                }
                FixStackWidths();
            }

            bool Commit(Action mutate, bool allowThrust, string note)
            {
                var snap = JsonMut.CloneObject(d);
                var before = Measure(d);
                try
                {
                    mutate();
                    HangarEditor.Normalize(d);
                    MatchFairing();
                }
                catch
                {
                    CopyInto(d, snap);
                    return false;
                }
                var now = Measure(d);
                if (!Accept(before, now, allowThrust))
                {
                    CopyInto(d, snap);
                    return false;
                }
                if (!changes.Contains(note)) changes.Add(note);
                return true;
            }

            bool Accept(Metrics before, Metrics now, bool allowThrust)
            {
                if (!now.Ok || double.IsNaN(now.Margin) || double.IsInfinity(now.Margin)) return false;
                if (!SamePayload(original, d)) return false;
                if (!Afford(now))
                {
                    hitBudget = true;
                    return false;
                }
                if (!(now.Twr > Lim.minTwr)) return false;
                if (!PackOk(d)) return false;
                if (now.Margin > before.Margin + 1e-5) return true;
                if (allowThrust && now.Twr > before.Twr + 0.02 && now.Margin > before.Margin - 0.04) return true;
                return false;
            }

            struct Metrics
            {
                public bool Ok;
                public double Margin, Dv, Required, Twr, CostKc;
            }

            Metrics Measure(JsonObject design)
            {
                try
                {
                    var q = Readout.Quick(catalog, design);
                    double per = CostModel.MinorPerKc(catalog);
                    double cost = per > 0 ? q.cost / (double)per : 0;
                    return new Metrics
                    {
                        Ok = true,
                        Margin = q.margin,
                        Dv = q.totalDeltaV,
                        Required = q.required,
                        Twr = q.liftoffTWR,
                        CostKc = cost,
                    };
                }
                catch (Exception)
                {
                    return new Metrics { Margin = double.NaN, Twr = 0, CostKc = double.PositiveInfinity };
                }
            }

            bool Reached(Metrics m) => m.Ok && m.Twr > Lim.minTwr && m.Margin >= 0 && !double.IsNaN(m.Margin) && !double.IsInfinity(m.Margin);

            bool Afford(Metrics m) => double.IsPositiveInfinity(budget) || (!(m.CostKc > budget + 0.5));

            bool PackOk(JsonObject design) => EnginesFit(design) && TopHeavyOk(design) && ShapeOk(design);

            bool ShapeOk(JsonObject design)
            {
                try { return Validate.Shape(catalog, design).ok; }
                catch (Exception) { return false; }
            }

            bool EnginesFit(JsonObject design)
            {
                var stages = design.Arr("stages");
                if (stages == null || stages.Count == 0) return false;
                for (int i = 0; i < stages.Count; i++)
                {
                    var s = stages[i] as JsonObject;
                    if (s == null || !catalog.EngineById.TryGetValue(s.Str("engine") ?? "", out var e)) return false;
                    var ep = EnginePoint.Of(e, s.Str("nozzle"));
                    if (!(ep.diameter > 0)) return false;
                    if (s.Num("engineCount") * ep.diameter * ep.diameter > Lim.pack * s.Num("diameter") * s.Num("diameter") + 1e-4)
                        return false;
                }
                return true;
            }

            bool TopHeavyOk(JsonObject design)
            {
                var stages = design.Arr("stages");
                if (stages == null) return false;
                for (int i = 1; i < stages.Count; i++)
                {
                    var up = stages[i] as JsonObject;
                    var below = stages[i - 1] as JsonObject;
                    if (up == null || below == null) return false;
                    if (up.Num("diameter") > below.Num("diameter") * 1.6 + 1e-6) return false;
                }
                return true;
            }

            bool AllTanksFull()
            {
                var stages = d.Arr("stages");
                if (stages == null || stages.Count == 0) return false;
                if (stages.Count < Lim.stagesMax) return false;
                for (int i = 0; i < stages.Count; i++)
                {
                    var s = stages[i] as JsonObject;
                    if (s != null && s.Num("propMass") < Lim.propMassMax - 1) return false;
                }
                return true;
            }

            void AssignBest(int index)
            {
                var options = Candidates(index, 1);
                if (options.Count == 0) return;
                var opt = options[0];
                HangarEditor.SetEngine(catalog, d, PartRef.Stage(index), opt.Id);
                HangarEditor.SetNozzle(catalog, d, PartRef.Stage(index), opt.Nozzle);
                if (index > 0) HangarEditor.SetEngineCount(d, PartRef.Stage(index), 1);
                WidenToFit(index);
            }

            sealed class Pick { public string Id, Nozzle; public double Isp; }

            List<Pick> Candidates(int index, int take)
            {
                var s = Stage(index);
                var list = new List<Pick>();
                if (s == null) return list;
                double current = IspOf(s);
                int count = index == 0 ? Math.Max(1, (int)s.Num("engineCount")) : 1;
                double below = index > 0 ? Stage(index - 1).Num("diameter") : Lim.diameterMax;
                foreach (var e in catalog.Engines)
                {
                    Consider(list, e, "default", index, count, below, current);
                    if (e.nozzles == null) continue;
                    for (int n = 0; n < e.nozzles.Count; n++)
                        Consider(list, e, e.nozzles[n].id, index, count, below, current);
                }
                list.Sort((a, b) => b.Isp.CompareTo(a.Isp));
                if (list.Count > take) list.RemoveRange(take, list.Count - take);
                return list;
            }

            void Consider(List<Pick> list, Engine e, string nozzle, int index, int count, double belowDia, double currentIsp)
            {
                if (e == null) return;
                if (index == 0 && !(e.thrustSL > 0) && nozzle == "default") return;
                var ep = EnginePoint.Of(e, nozzle);
                if (index == 0 && !(ep.thrustSL > 0)) return;
                if (!(ep.ispVac > currentIsp + 2) || !(ep.diameter > 0)) return;
                double need = Math.Sqrt(count * ep.diameter * ep.diameter / Lim.pack) + 0.05;
                if (need > Lim.diameterMax) return;
                if (index > 0 && need > belowDia * 1.6 + 1e-6 && need / 1.6 > Lim.diameterMax) return;
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Id == e.id && list[i].Nozzle == nozzle) return;
                list.Add(new Pick { Id = e.id, Nozzle = string.IsNullOrEmpty(nozzle) ? "default" : nozzle, Isp = ep.ispVac });
            }

            void WidenToFit(int index)
            {
                var s = Stage(index);
                if (s == null || !catalog.EngineById.TryGetValue(s.Str("engine") ?? "", out var e)) return;
                var ep = EnginePoint.Of(e, s.Str("nozzle"));
                if (!(ep.diameter > 0)) return;
                double need = Math.Sqrt(s.Num("engineCount") * ep.diameter * ep.diameter / Lim.pack) + 0.05;
                need = Math.Ceiling(need * 10.0) / 10.0;
                if (need < Lim.diameterMin) need = Lim.diameterMin;
                if (need > Lim.diameterMax) need = Lim.diameterMax;
                if (s.Num("diameter") + 1e-6 < need)
                    HangarEditor.SetDiameter(d, PartRef.Stage(index), need);
            }

            void FixStackWidths()
            {
                var stages = d.Arr("stages");
                if (stages == null) return;
                for (int i = 1; i < stages.Count; i++)
                {
                    var up = stages[i] as JsonObject;
                    var below = stages[i - 1] as JsonObject;
                    if (up == null || below == null) continue;
                    if (up.Num("diameter") <= below.Num("diameter") * 1.6 + 1e-6) continue;
                    double needBelow = up.Num("diameter") / 1.6;
                    if (needBelow <= Lim.diameterMax)
                        HangarEditor.SetDiameter(d, PartRef.Stage(i - 1), needBelow);
                    else
                    {
                        double cap = Math.Floor(below.Num("diameter") * 1.6 * 10.0) / 10.0;
                        HangarEditor.SetDiameter(d, PartRef.Stage(i), Math.Max(Lim.diameterMin, cap));
                    }
                }
            }

            void MatchFairing()
            {
                var stages = d.Arr("stages");
                if (stages == null || stages.Count == 0) return;
                var top = stages[stages.Count - 1] as JsonObject;
                var fairing = d.Obj("fairing");
                if (top == null || fairing == null) return;
                if (fairing.Num("diameter") + 1e-9 < top.Num("diameter"))
                    JsonMut.Set(fairing, "diameter", top.Num("diameter"));
            }

            JsonObject Stage(int index)
            {
                var stages = d.Arr("stages");
                if (stages == null || index < 0 || index >= stages.Count) return null;
                return stages[index] as JsonObject;
            }

            double IspOf(JsonObject s)
            {
                if (s == null || !catalog.EngineById.TryGetValue(s.Str("engine") ?? "", out var e)) return 0;
                return EnginePoint.Of(e, s.Str("nozzle")).ispVac;
            }

            static bool SamePayload(JsonObject a, JsonObject b)
            {
                var pa = a?.Obj("payload");
                var pb = b?.Obj("payload");
                if (pa == null || pb == null) return false;
                if (pa.Str("orbit") != pb.Str("orbit")) return false;
                return Math.Abs(pa.Num("mass") - pb.Num("mass")) < 0.51;
            }

            static double BudgetOf(JsonObject design)
            {
                if (design != null && design.ContainsKey("budgetKc") && design["budgetKc"] is double b && b > 0 && !double.IsNaN(b) && !double.IsInfinity(b))
                    return b;
                return double.PositiveInfinity;
            }

            static void CopyInto(JsonObject dst, JsonObject src)
            {
                dst.Clear();
                dst.Order.Clear();
                foreach (var k in src.Order) dst.Add(k, JsonMut.Clone(src[k]));
            }

            string OrbitLabel()
            {
                if (!string.IsNullOrEmpty(orbitId) && catalog.Orbits.TryGetValue(orbitId, out var o) && !string.IsNullOrEmpty(o.label))
                    return o.label;
                return string.IsNullOrEmpty(orbitId) ? "this destination" : orbitId;
            }

            string UnknownOrbit(string id)
            {
                string shown = string.IsNullOrEmpty(id) ? "" : id;
                return "payload.orbit \"" + shown + "\" is not a known orbit, so this trip's speed cannot be checked. Lighten the cargo, raise the budget, or pick a different mission.";
            }

            string Explain(double shortfallMps, string why, bool budgetBound, bool massBound)
            {
                var inv = CultureInfo.InvariantCulture;
                string where = OrbitLabel();
                if (string.IsNullOrEmpty(why))
                {
                    if (budgetBound && !double.IsPositiveInfinity(budget))
                        why = "Adding the propellant, tank, stage, or higher-Isp engine that would close the gap goes past the " + budget.ToString("0", inv) + " Kc budget.";
                    else if (massBound)
                        why = "The tanks are at " + Lim.propMassMax.ToString("0", inv) + " kg and the rocket already has " + Lim.stagesMax.ToString(inv) + " stages, and the highest-Isp engines that fit are still short.";
                    else
                        why = "No propellant load, tank size, stage, or higher-Isp engine that this rocket can use closes the gap.";
                }
                string gap = double.IsNaN(shortfallMps) ? "Some speed" : Math.Round(shortfallMps).ToString("0", inv) + " m/s";
                return "Still " + gap + " short of " + where + ". " + why + " Lighten the cargo, raise the budget, or pick a different mission.";
            }
        }
    }
}
