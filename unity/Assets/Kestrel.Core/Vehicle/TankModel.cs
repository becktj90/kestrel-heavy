// Tank walls, insulation, boil-off, common bulkhead and fineness (§2.3).
// Mass.cs calls StructureMass only when a stage carries v3 tank fields.
// With no tank object, the structural-fraction shell is unchanged.
using System;
using Kestrel.Core.Data;

namespace Kestrel.Core.Vehicle
{
    public enum FinenessBand { Ok, Warning, Red }

    /// <summary>Flown tank: barrel holds the ullage volume; length includes the sizing domes.</summary>
    public sealed class TankGeometry
    {
        public double radiusM, barrelLengthM, lengthM, diameterM;
    }

    public static class TankModel
    {
        public const double BarToPa = 1e5;
        public const double MPaToPa = 1e6;
        public const double MinGaugeM = 0.001;
        public const double PressureMinBar = 1.5;
        public const double PressureMaxBar = 6;
        public const double Ullage = 0.03;
        public const double CommonBulkheadFactor = 0.92;
        public const double CommonBulkheadHazardBar = 2;
        public const double FinenessWarn = 18;
        public const double FinenessRedLimit = 22;
        public const double FoamBoiloff = 0.4;
        public const double MliBoiloff = 0.15;
        public const double FoamKgPerM2 = 1.2;
        public const double MliKgPerM2 = 2.5;

        /// <summary>Representative Young's modulus (GPa). Matches data/materials.json.</summary>
        public static double ElasticGPa(string materialId)
        {
            switch (materialId)
            {
                case "al-li": return 78;
                case "steel": return 193;
                case "composite": return 70;
                case "al-cu": return 73;
                case "titanium": return 114;
                case "glass-phenolic": return 25;
                default: return 0;
            }
        }

        public static bool PressureInRange(double bar) => bar >= PressureMinBar && bar <= PressureMaxBar;

        public static string NormalizeInsulation(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return "none";
            switch (kind.Trim().ToLowerInvariant())
            {
                case "foam": return "foam";
                case "mli": return "mli";
                default: return "none";
            }
        }

        public static double BoiloffFactor(string insulation)
        {
            switch (NormalizeInsulation(insulation))
            {
                case "foam": return FoamBoiloff;
                case "mli": return MliBoiloff;
                default: return 1;
            }
        }

        public static double InsulationKgPerM2(string insulation)
        {
            switch (NormalizeInsulation(insulation))
            {
                case "foam": return FoamKgPerM2;
                case "mli": return MliKgPerM2;
                default: return 0;
            }
        }

        /// <summary>Hoop wall t = SF·p·r/σ, then the 1 mm minimum gauge. p is bar, σ is MPa, result is metres.</summary>
        public static double WallThicknessM(double safetyFactor, double pressureBar, double radiusM, double yieldMPa)
        {
            if (!(yieldMPa > 0) || !(radiusM > 0) || !(pressureBar > 0) || !(safetyFactor > 0)) return 0;
            double t = safetyFactor * (pressureBar * BarToPa) * radiusM / (yieldMPa * MPaToPa);
            return t > MinGaugeM ? t : MinGaugeM;
        }

        /// <summary>
        /// Cylinder plus two hemispherical domes. Each dome area is 2πr², so the pair is 4πr².
        /// L is the barrel length, not the flown length.
        /// </summary>
        public static double SurfaceAreaM2(double radiusM, double barrelLengthM)
        {
            if (!(radiusM > 0)) return 0;
            double barrel = barrelLengthM > 0 ? barrelLengthM : 0;
            return 2 * Math.PI * radiusM * barrel + 4 * Math.PI * radiusM * radiusM;
        }

        public static double ShellMassKg(double densityKgPerM3, double thicknessM, double radiusM, double barrelLengthM)
        {
            if (!(densityKgPerM3 > 0) || !(thicknessM > 0)) return 0;
            return densityKgPerM3 * thicknessM * SurfaceAreaM2(radiusM, barrelLengthM);
        }

        /// <summary>Thickness implied by a structural-fraction shell spread over the same surface. v2 stages have no hoop pressure.</summary>
        public static double EquivalentWallM(double shellKg, double densityKgPerM3, double radiusM, double barrelLengthM)
        {
            double area = SurfaceAreaM2(radiusM, barrelLengthM);
            if (!(area > 0) || !(densityKgPerM3 > 0) || !(shellKg > 0)) return 0;
            return shellKg / (densityKgPerM3 * area);
        }

        public static TankGeometry Geometry(Catalog c, StagePart part)
        {
            var g = new TankGeometry();
            if (part == null) return g;
            g.diameterM = part.diameter > 0 ? part.diameter : 0;
            g.radiusM = g.diameterM / 2;
            var size = DesignMath.SizeTank(c, part.propellant, part.propMass, part.diameter);
            g.lengthM = size.length;
            if (g.radiusM > 0 && size.volume > 0)
                g.barrelLengthM = size.volume / (Math.PI * g.radiusM * g.radiusM);
            return g;
        }

        public static double FinenessRatio(double lengthM, double diameterM)
            => diameterM > 0 ? lengthM / diameterM : 0;

        /// <summary>L/D &gt; 18 is a buckling warning. L/D &gt; 22 is red. Equality stays in the lower band.</summary>
        public static FinenessBand Fineness(double ratio)
        {
            if (ratio > FinenessRedLimit) return FinenessBand.Red;
            if (ratio > FinenessWarn) return FinenessBand.Warning;
            return FinenessBand.Ok;
        }

        public static FinenessBand Fineness(Catalog c, StagePart part)
        {
            var g = Geometry(c, part);
            return Fineness(FinenessRatio(g.lengthM, g.diameterM));
        }

        /// <summary>Propellant left after h hours. GV4 is this number. Loss is mass minus the return value.</summary>
        public static double RemainingKg(double massKg, double ratePerHour, string insulation, double hours)
        {
            if (!(massKg > 0)) return 0;
            if (!(hours > 0)) return massKg;
            double r = ratePerHour * BoiloffFactor(insulation);
            if (!(r > 0)) return massKg;
            if (r >= 1) return 0;
            return massKg * Math.Pow(1 - r, hours);
        }

        public static double BoilOffLossKg(double massKg, double ratePerHour, string insulation, double hours)
            => massKg - RemainingKg(massKg, ratePerHour, insulation, hours);

        public static double RemainingPropKg(Catalog c, StagePart part, double hours)
        {
            if (part == null) return 0;
            double rate = 0;
            if (c.PropellantById.TryGetValue(part.propellant ?? "", out var prop)) rate = prop.boiloffPerHour;
            string insulation = part.tank != null ? part.tank.InsulationOrNone : "none";
            return RemainingKg(part.propMass, rate, insulation, hours);
        }

        /// <summary>Δp across a common bulkhead above 2 bar is a structural hazard. The bulkhead must be selected.</summary>
        public static bool CommonBulkheadHazard(bool commonBulkhead, double deltaPBar)
            => commonBulkhead && deltaPBar > CommonBulkheadHazardBar;

        public static double InsulationMassKg(Catalog c, StagePart part)
        {
            if (part?.tank == null) return 0;
            double per = InsulationKgPerM2(part.tank.InsulationOrNone);
            if (!(per > 0)) return 0;
            var g = Geometry(c, part);
            return per * SurfaceAreaM2(g.radiusM, g.barrelLengthM);
        }

        public static double HoopShellKg(Catalog c, StagePart part, double safetyFactor)
        {
            if (part?.tank == null || !part.tank.pressureSet) return 0;
            if (!c.MaterialById.TryGetValue(part.material ?? "", out var mat)) return 0;
            var g = Geometry(c, part);
            double t = WallThicknessM(safetyFactor, part.tank.pressureBar, g.radiusM, mat.yieldMPa);
            return ShellMassKg(mat.density, t, g.radiusM, g.barrelLengthM);
        }

        /// <summary>
        /// v3 shell. Pressure replaces the fraction shell with the hoop wall and two domes.
        /// A common bulkhead multiplies that tank mass by 0.92. Insulation is added after, in kg/m².
        /// </summary>
        public static double StructureMass(Catalog c, StagePart part, double safetyFactor, double legacyShellKg)
        {
            if (part?.tank == null || !part.tank.Any) return legacyShellKg;
            double shell = part.tank.pressureSet ? HoopShellKg(c, part, safetyFactor) : legacyShellKg;
            if (part.tank.commonBulkheadSet && part.tank.commonBulkhead) shell *= CommonBulkheadFactor;
            return shell + InsulationMassKg(c, part);
        }
    }
}
