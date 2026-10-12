// Instant designer readout. Port of quickReadout() in web/js/designer/readout.js.
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
        public double m0, twr, twrVac, deltaV;
    }

    public sealed class QuickReadout
    {
        public VehicleMass mass;
        public List<StageReadout> stages = new List<StageReadout>();
        public double liftoffTWR, totalDeltaV, margin, required, targetAlt, liftoffMass;
        public long cost;
        public double reliability;
        public string orbitLabel;
    }

    public static class Readout
    {
        const double G0 = 9.80665;

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
                stages.Add(new StageReadout
                {
                    label = s.label, role = s.role, designIndex = s.index, m0 = m0,
                    twr = thrust / (m0 * G0), twrVac = s.thrustVac / (m0 * G0), deltaV = dv.perStage[i],
                });
            }
            var req = Stack.RequiredDeltaV(c, d.payloadOrbit);
            var price = CostModel.PriceDesign(c, d);
            var rel = Reliability.Of(c, d);
            return new QuickReadout
            {
                mass = mass, stages = stages,
                liftoffTWR = stages.Count > 0 ? stages[0].twr : 0,
                liftoffMass = stages.Count > 0 ? stages[0].m0 : mass.liftoffMass,
                totalDeltaV = dv.total, required = req.required, targetAlt = req.targetAlt,
                margin = (dv.total - req.required) / req.required,
                cost = price.recurring, reliability = rel.reliability, orbitLabel = req.orbit.label,
            };
        }
    }
}
