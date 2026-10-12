// The Hangar's live data sheet: mass, thrust, TWR, Δv per stage and total, cost against the budget,
// and the CG/CP stability readout. Every number comes from the existing readout (Readout.Quick,
// Balance, Stack, CostModel); this class only arranges it into rows and adds the budget and the
// stability band. Nothing here integrates a trajectory, so it is safe on the edit frame.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Hangar
{
    /// <summary>One burn phase as the readout serialises it (strap-ons light with the core as one phase).</summary>
    public sealed class PhaseRow
    {
        public string label, role;
        /// <summary>Design stage index, or -1 for the core+strap-ons phase.</summary>
        public int stageIndex = -1;
        public double startMass_kg, dryMass_kg, propMass_kg;
        /// <summary>Sea-level thrust for the phase that lights on the pad, vacuum thrust otherwise.</summary>
        public double thrust_N;
        public double twr, twrVac, deltaV_mps;
        public double? deltaVSL_mps;
    }

    /// <summary>One physical part as drawn: what the stack list and the part card show.</summary>
    public sealed class PartRow
    {
        public PartRef part;
        public string label, engineId, engineLabel, nozzle, propellant, material;
        public int engineCount;
        public double diameter_m, tankLength_m, dryMass_kg, propMass_kg, landingProp_kg;
        public double thrustSL_N, thrustVac_N, ispSL_s, ispVac_s;
    }

    public enum StabilityBand
    {
        /// <summary>1–2 calibres (or above, up to the overstable line).</summary>
        Stable,
        /// <summary>0–1 cal: flies, but twitchy.</summary>
        Twitchy,
        /// <summary>CP above CG on a rocket that needs fins to fly straight.</summary>
        Unstable,
        /// <summary>Above the overstable line: weathercocks in wind.</summary>
        Overstable,
        /// <summary>No fins, gimballed pad engines: the engines hold it straight, the fin rule does not apply.</summary>
        EngineSteered,
    }

    public sealed class DataSheet
    {
        public readonly List<PhaseRow> phases = new List<PhaseRow>();
        public readonly List<PartRow> parts = new List<PartRow>();
        public double liftoffMass_kg, dryMass_kg, payload_kg, fairing_kg;
        public double liftoffThrust_N, liftoffTwr;
        public double totalDeltaV_mps, requiredDeltaV_mps, margin;
        public string orbitLabel;
        public long costMinor;
        public double cost_Kc, budget_Kc;
        public bool overBudget, nearBudget;
        public double reliability;
        public double cg_m, cp_m, stabilityCal, length_m, refDiameter_m;
        public StabilityBand stability;
        public double? maxQ_kPa;
        public bool maxQStale;
        /// <summary>The readout the sheet was built from (Validate uses the same instance).</summary>
        public QuickReadout readout;

        public bool ReachesOrbit => margin >= 0;

        /// <summary>Kid word for the stability band.</summary>
        public string StabilityWord
        {
            get
            {
                switch (stability)
                {
                    case StabilityBand.Stable: return "Steady";
                    case StabilityBand.Twitchy: return "A bit wobbly";
                    case StabilityBand.Unstable: return "Would flip over";
                    case StabilityBand.Overstable: return "Too stiff";
                    default: return "Engines keep it straight";
                }
            }
        }

        public static DataSheet Build(Catalog c, HangarData h, JsonObject design, QuickReadout r = null)
        {
            r ??= Readout.Quick(c, design);
            var d = Design.FromJson(design);
            var bal = Balance.Of(c, d);
            var lim = DesignerLimits.Active;
            var sheet = new DataSheet
            {
                readout = r,
                liftoffMass_kg = r.liftoffMass, dryMass_kg = r.mass.dryMass, payload_kg = r.mass.payload, fairing_kg = r.mass.fairing,
                liftoffTwr = r.liftoffTWR, totalDeltaV_mps = r.totalDeltaV, requiredDeltaV_mps = r.required, margin = r.margin,
                orbitLabel = r.orbitLabel, costMinor = r.cost, reliability = r.reliability,
                cg_m = r.cg_m, cp_m = r.cp_m, stabilityCal = r.stabilityCal, length_m = bal.length_m, refDiameter_m = bal.refDiameter_m,
                maxQ_kPa = r.maxQ_kPa, maxQStale = r.maxQStale,
            };
            var serial = Stack.SerialStages(c, d).stages;
            for (int i = 0; i < r.stages.Count; i++)
            {
                var s = r.stages[i];
                double thrust = i < serial.Count ? (i == 0 ? serial[i].thrustSL : serial[i].thrustVac) : 0;
                sheet.phases.Add(new PhaseRow
                {
                    label = s.label, role = s.role, stageIndex = s.designIndex, startMass_kg = s.m0, dryMass_kg = s.dryMass_kg,
                    propMass_kg = s.propMass_kg, thrust_N = thrust, twr = s.twr, twrVac = s.twrVac, deltaV_mps = s.deltaV, deltaVSL_mps = s.deltaVSL_mps,
                });
            }
            sheet.liftoffThrust_N = sheet.phases.Count > 0 ? sheet.phases[0].thrust_N : 0;

            for (int i = d.stages.Count - 1; i >= 0; i--)
                sheet.parts.Add(Part(c, d, d.stages[i], PartRef.Stage(i), HangarParts.StageLabel(design, i), r.mass.stages[i].dry, r.mass.stages[i].landingProp));
            if (d.StrapOnCount > 0)
                sheet.parts.Add(Part(c, d, d.strapOns, PartRef.Boosters, $"Boosters ×{d.StrapOnCount}", r.mass.strapOns.eachDry, 0));

            double perKc = CostModel.MinorPerKc(c);
            sheet.cost_Kc = perKc > 0 ? r.cost / (double)perKc : 0;
            sheet.budget_Kc = h != null ? h.budgets.For(design) : 0;
            if (sheet.budget_Kc > 0)
            {
                sheet.overBudget = sheet.cost_Kc > sheet.budget_Kc;
                sheet.nearBudget = !sheet.overBudget && sheet.cost_Kc > sheet.budget_Kc * h.budgets.nearFraction;
            }
            sheet.stability = Band(c, d, r.stabilityCal, lim);
            return sheet;
        }

        static PartRow Part(Catalog c, Design d, StagePart s, PartRef p, string label, double dry, double landing)
        {
            c.EngineById.TryGetValue(s.engine ?? "", out var e);
            var pt = EnginePoint.Of(e, s.nozzle);
            // Same tune scaling as Stack.SerialStages, so the part row agrees with the phase rows.
            double lim = DesignerLimits.Active.tuneLimitPct;
            double K(double pct) => 1 + Math.Max(-lim, Math.Min(lim, double.IsNaN(pct) ? 0 : pct)) / 100;
            double kt = s.tune != null ? K(s.tune.thrustPct) : 1, ki = s.tune != null ? K(s.tune.ispPct) : 1;
            pt.thrustSL *= kt; pt.thrustVac *= kt; pt.ispSL *= ki; pt.ispVac *= ki;
            return new PartRow
            {
                part = p, label = label, engineId = s.engine, engineLabel = e?.label ?? s.engine, nozzle = s.nozzle ?? "default",
                propellant = s.propellant, material = s.material, engineCount = s.engineCount,
                diameter_m = s.diameter, tankLength_m = DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length,
                dryMass_kg = dry, propMass_kg = s.propMass, landingProp_kg = landing,
                thrustSL_N = pt.thrustSL * s.engineCount, thrustVac_N = pt.thrustVac * s.engineCount, ispSL_s = pt.ispSL, ispVac_s = pt.ispVac,
            };
        }

        /// <summary>Same rule DesignRules.Stability applies: the 1–2 cal band is for finned or unguided rockets.</summary>
        public static StabilityBand Band(Catalog c, Design d, double cal, DesignerLimits lim)
        {
            bool steered = DesignRules.PadSteered(c, d);
            bool band = d.fins || !steered;
            if (band && cal > lim.overstableAbove) return StabilityBand.Overstable;
            if (!band) return cal >= lim.stabilityAimMin ? StabilityBand.Stable : StabilityBand.EngineSteered;
            if (cal < 0) return StabilityBand.Unstable;
            if (cal < lim.stabilityAimMin) return StabilityBand.Twitchy;
            return StabilityBand.Stable;
        }
    }
}
