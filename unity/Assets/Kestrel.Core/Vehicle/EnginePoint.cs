// Resolved engine performance. Absent nozzle, or "default", is the catalog top-level bell.
// A listed nozzles[] entry replaces thrust, Isp, mass and exit diameter. Gimbal stays on the engine.
// v3 engineParams or a mixture offset route through EngineModel. Anything else stays on this table,
// including an existing tune (Stack still applies that on the catalog path).
using Kestrel.Core.Data;

namespace Kestrel.Core.Vehicle
{
    public struct EnginePoint
    {
        public bool known;
        public double thrustSL, thrustVac, ispSL, ispVac, mass, diameter, gimbal;
        public double[] throttle;
        public bool restart;
        public double hazardRate;
        public bool flowSeparation;

        public static EnginePoint Of(Engine e, string nozzleId)
        {
            if (e == null) return new EnginePoint();
            if (!string.IsNullOrEmpty(nozzleId) && nozzleId != "default" && e.nozzles != null)
            {
                for (int i = 0; i < e.nozzles.Count; i++)
                {
                    var n = e.nozzles[i];
                    if (n.id != nozzleId) continue;
                    return new EnginePoint
                    {
                        known = true, thrustSL = n.thrustSL, thrustVac = n.thrustVac, ispSL = n.ispSL, ispVac = n.ispVac,
                        mass = n.mass, diameter = n.exitDiameter, gimbal = e.gimbal,
                        throttle = e.throttle, restart = e.restart,
                    };
                }
            }
            return new EnginePoint
            {
                known = true, thrustSL = e.thrustSL, thrustVac = e.thrustVac, ispSL = e.ispSL, ispVac = e.ispVac,
                mass = e.mass, diameter = e.diameter, gimbal = e.gimbal,
                throttle = e.throttle, restart = e.restart,
            };
        }

        public static EnginePoint Of(Catalog c, StagePart part)
        {
            if (part == null || c == null || !c.EngineById.TryGetValue(part.engine ?? "", out var e)) return new EnginePoint();
            if (!UsesModel(part)) return Of(e, part.nozzle);
            var p = part.engineParams;
            double pc = p != null && p.pcSet ? p.pc : e.pc;
            double eps = p != null && p.expansionRatioSet ? p.expansionRatio : double.NaN;
            double throttleMin = p != null && p.throttleMinSet ? p.throttleMin : double.NaN;
            double gimbalDeg = p != null && p.gimbalDegSet ? p.gimbalDeg : double.NaN;
            var r = EngineModel.Resolve(e, part.nozzle, pc, eps, throttleMin, gimbalDeg, part.MixtureOffsetPctOrZero, part.tune);
            return new EnginePoint
            {
                known = true, thrustSL = r.thrustSL, thrustVac = r.thrustVac, ispSL = r.ispSL, ispVac = r.ispVac,
                mass = r.mass, diameter = r.exitDiameter, gimbal = r.gimbalDeg,
                throttle = new[] { r.throttleMin, r.throttleMax }, restart = e.restart,
                hazardRate = r.hazardRate, flowSeparation = r.flowSeparation,
            };
        }

        public static bool UsesModel(StagePart part)
        {
            if (part == null) return false;
            if (part.mixtureOffsetSet) return true;
            return part.engineParams != null && part.engineParams.Any;
        }

        public static bool NozzleKnown(Engine e, string nozzleId)
        {
            if (e == null) return false;
            if (string.IsNullOrEmpty(nozzleId) || nozzleId == "default") return true;
            if (e.nozzles == null) return false;
            for (int i = 0; i < e.nozzles.Count; i++) if (e.nozzles[i].id == nozzleId) return true;
            return false;
        }
    }
}
