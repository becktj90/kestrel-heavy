// Mass model (kg). Port of web/js/vehicle/mass.js.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;

namespace Kestrel.Core.Vehicle
{
    public sealed class StageMass { public string name; public double dry, prop, wet, usableProp, landingProp; }
    public sealed class StrapOnMass { public int count; public double dry, prop, wet, eachDry, eachProp; }
    public sealed class MassRatio { public double m0, mf, massRatio, propFraction; }
    public sealed class VehicleMass
    {
        public List<StageMass> stages; public StrapOnMass strapOns; public double payload, fairing, liftoffMass, dryMass; public MassRatio[] ratios;
    }

    public static class Mass
    {
        const double REF_SAFETY = 1.4, SOLID_CASE_FRACTION = 0.09, LEG_FRACTION = 0.06, GRIDFIN_MASS_PER_M2 = 120, RCS_MASS = 250;
        static readonly Dictionary<string, double> AVIONICS_MASS = new Dictionary<string, double> { ["basic"] = 60, ["standard"] = 150, ["redundant"] = 300 };
        static readonly Dictionary<string, double> FAIRING_AREAL = new Dictionary<string, double> { ["composite"] = 14, ["al-li"] = 20, ["steel"] = 45, ["al-cu"] = 22, ["titanium"] = 18, ["glass-phenolic"] = 16 };

        public static double StructureMass(Catalog c, StagePart part, double safetyFactor = REF_SAFETY)
        {
            if (!c.PropellantById.TryGetValue(part.propellant ?? "", out var prop) || !c.MaterialById.TryGetValue(part.material ?? "", out var mat)) return 0;
            double sfScale = safetyFactor / REF_SAFETY;
            if (prop.storability == "solid") return part.propMass * SOLID_CASE_FRACTION * (mat.structuralFraction / 0.045) * sfScale;
            double volScale = Math.Sqrt(1030 / prop.bulkDensity);
            return part.propMass * mat.structuralFraction * volScale * sfScale;
        }

        public static double EnginesMass(Catalog c, StagePart part)
        {
            var p = EnginePoint.Of(c, part);
            return p.known ? p.mass * part.engineCount : 0;
        }

        public static double FairingMass(Design d)
        {
            double r = d.fairingDiameter / 2;
            double area = 2 * Math.PI * r * d.fairingLength * 0.85;
            return area * (FAIRING_AREAL.TryGetValue(d.fairingMaterial ?? "", out var k) ? k : 20);
        }

        public static List<StageMass> StageMasses(Catalog c, Design d)
        {
            double sf = d.safetyFactor;
            var list = new List<StageMass>();
            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i];
                double dry = StructureMass(c, s, sf) + EnginesMass(c, s) + InterstageAero.MassKg(c, d, i) + s.separationMass;
                if (i == d.stages.Count - 1) dry += AVIONICS_MASS.TryGetValue(d.avionicsTier ?? "", out var av) ? av : 150;
                if (i == 0)
                {
                    if (d.legs) dry *= 1 + LEG_FRACTION;
                    if (d.gridFins) dry += GRIDFIN_MASS_PER_M2 * Math.PI * Math.Pow(s.diameter / 2, 2);
                    if (d.rcs) dry += RCS_MASS;
                }
                double prop = s.propMass, landingProp = Math.Min(s.landingPropMass, prop);
                list.Add(new StageMass { name = s.name, dry = dry, prop = prop, wet = dry + prop, usableProp = prop - landingProp, landingProp = landingProp });
            }
            return list;
        }

        public static StrapOnMass StrapOnMasses(Catalog c, Design d)
        {
            var so = d.strapOns;
            if (so == null || so.count == 0) return new StrapOnMass();
            double dryEach = StructureMass(c, so, d.safetyFactor) + EnginesMass(c, so) + 80;
            return new StrapOnMass { count = so.count, dry = dryEach * so.count, prop = so.propMass * so.count, wet = (dryEach + so.propMass) * so.count, eachDry = dryEach, eachProp = so.propMass };
        }

        public static VehicleMass Of(Catalog c, Design d)
        {
            var stages = StageMasses(c, d);
            var straps = StrapOnMasses(c, d);
            double payload = d.payloadMass, fairing = FairingMass(d);
            double above = payload + fairing;
            var ratios = new MassRatio[stages.Count];
            for (int i = stages.Count - 1; i >= 0; i--)
            {
                double m0 = above + stages[i].wet, mf = m0 - stages[i].usableProp;
                ratios[i] = new MassRatio { m0 = m0, mf = mf, massRatio = m0 / mf, propFraction = stages[i].usableProp / m0 };
                above = m0;
            }
            double dry = 0; foreach (var s in stages) dry += s.dry; // JS reduce order, then + straps.dry
            return new VehicleMass { stages = stages, strapOns = straps, payload = payload, fairing = fairing, liftoffMass = above + straps.wet, dryMass = dry + straps.dry, ratios = ratios };
        }
    }
}
