// Instant designer readout. Port of quickReadout() in web/js/designer/readout.js,
// plus the v2 fields (masses, sea-level Δv, CG/CP, cached max-Q).
// Quick() does not integrate. Max-Q is filled only when ReferenceAscent already cached this design.
// simReadout() is simulate(); data/golden/simulate.json and catalog-flight cover it.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public sealed class StageReadout
    {
        public string label, role;
        public int designIndex = -1;
        public double m0, twr, twrVac;
        /// <summary>Δv (vac). Vacuum-Isp rocket equation for this serial phase. Not a loss model.</summary>
        public double deltaV;
        /// <summary>Serial-stage dry mass, including the landing reserve held as dry.</summary>
        public double dryMass_kg;
        /// <summary>Usable propellant burned in this serial phase.</summary>
        public double propMass_kg;
        /// <summary>
        /// Δv (SL, pad stage). ispSL held for the whole burn: a sea-level bound, not a loss model.
        /// Null except the phase that lights on the pad.
        /// </summary>
        public double? deltaVSL_mps;
    }

    public sealed class QuickReadout
    {
        public VehicleMass mass;
        public List<StageReadout> stages = new List<StageReadout>();
        public double liftoffTWR, totalDeltaV, margin, required, targetAlt, liftoffMass;
        public long cost;
        public double reliability;
        public string orbitLabel;
        /// <summary>
        /// Liftoff CG, metres from the vehicle base, positive up. Full tanks, including landing reserve.
        /// TODO: a CG that follows the burn, and the stability at max-Q (about 60% of the propellant burned).
        /// </summary>
        public double cg_m;
        /// <summary>CP at the liftoff shape, same datum. Barrowman-style nose, body transitions and fins.</summary>
        public double cp_m;
        /// <summary>
        /// Liftoff stability, (liftoff CG − CP) / reference diameter. Positive means the CG is above the CP.
        /// TODO: the same margin at max-Q, once the CG follows the burn.
        /// </summary>
        public double stabilityCal;
        /// <summary>
        /// Peak dynamic pressure of the powered flight, from the cached reference ascent. Null while stale.
        /// A dense descent reopens the early stop so a lofted fall under thrust is included.
        /// Unpowered re-entry is not: the integrator stops when thrust ends.
        /// </summary>
        public double? maxQ_kPa;
        /// <summary>Geometric altitude at that peak. Null while stale.</summary>
        public double? maxQAlt_m;
        /// <summary>True until the debounced reference ascent for this design hash has finished.</summary>
        public bool maxQStale;
    }

    public static class Readout
    {
        const double G0 = 9.80665;

        /// <summary>
        /// Instant path. O(stages) mass and rocket equation, one CG/CP pass, cost and reliability.
        /// Does not run Headless. Budget: p95 &lt; 0.5 ms (ReadoutBench).
        /// </summary>
        public static QuickReadout Quick(Catalog c, JsonObject design)
        {
            var d = Design.FromJson(design);
            var mass = Mass.Of(c, d);
            var serial = Stack.SerialStages(c, d);
            var sims = serial.stages.ConvertAll(s => s.ToSim());
            var dv = Headless.StackDeltaV(sims, serial.payload);
            var stages = new List<StageReadout>();
            for (int i = 0; i < serial.stages.Count; i++)
            {
                var s = serial.stages[i];
                double above = serial.payload;
                for (int j = i + 1; j < serial.stages.Count; j++) above += serial.stages[j].propMass + serial.stages[j].dryMass;
                double m0 = above + s.propMass + s.dryMass;
                double thrust = i == 0 ? s.thrustSL : s.thrustVac;
                // Only the first serial phase lights on the pad (stage 1, or core+strap-ons).
                // The core continuation after booster burnout is not a pad lighting.
                double? dvSl = null;
                if (i == 0) dvSl = RocketEq.DeltaV(s.ispSL, m0, m0 - s.propMass);
                stages.Add(new StageReadout
                {
                    label = s.label, role = s.role, designIndex = s.index, m0 = m0,
                    twr = thrust / (m0 * G0), twrVac = s.thrustVac / (m0 * G0), deltaV = dv.perStage[i],
                    dryMass_kg = s.dryMass, propMass_kg = s.propMass, deltaVSL_mps = dvSl,
                });
            }
            var req = Stack.RequiredDeltaV(c, d.payloadOrbit);
            var price = CostModel.PriceDesign(c, d);
            var rel = Reliability.Of(c, d);
            var bal = Balance.Of(c, d);
            var readout = new QuickReadout
            {
                mass = mass, stages = stages,
                liftoffTWR = stages.Count > 0 ? stages[0].twr : 0,
                liftoffMass = stages.Count > 0 ? stages[0].m0 : mass.liftoffMass,
                totalDeltaV = dv.total, required = req.required, targetAlt = req.targetAlt,
                margin = (dv.total - req.required) / req.required,
                cost = price.recurring, reliability = rel.reliability, orbitLabel = req.orbit.label,
                cg_m = bal.cg_m, cp_m = bal.cp_m, stabilityCal = bal.stabilityCal,
                maxQStale = true,
            };
            if (ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(c, design), out double qPa, out double alt))
            {
                readout.maxQ_kPa = qPa / 1000.0;
                readout.maxQAlt_m = alt;
                readout.maxQStale = false;
            }
            return readout;
        }

        /// <summary>
        /// Reference ascent for max-Q. Runs the Headless path once per design hash and caches it.
        /// Subsequent Quick() calls for the same hash return maxQStale false.
        /// Cost: one Headless ascent at dt 0.2 s. Stops once max-Q is sealed, and continues if a dense descent clears that stop. Not for the edit frame. Budget ≤ 30 ms.
        /// </summary>
        public static QuickReadout Reference(Catalog c, JsonObject design) => ReferenceAscent.Run(c, design);
    }
}
