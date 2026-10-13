// Stage allowables, max-Q limit and margin of safety (§2.6).
// Failure events that consume these numbers belong to WP-E.
using System;
using Kestrel.Core.Data;

namespace Kestrel.Core.Vehicle
{
    /// <summary>Geometry the allowable formulas read. Wall is metres, yield is MPa, pressure is bar.</summary>
    public sealed class StageSection
    {
        public double radiusM, wallM, yieldMPa, safetyFactor, pressureBar;
    }

    public sealed class Allowables
    {
        public double fAllowN, qLimPa;
    }

    /// <summary>Load case for Margin. aAxial is m/s² so m·a is newtons. q is pascals.</summary>
    public sealed class LoadState
    {
        public double fAllowN, massAboveKg, aAxial, qPa, areaM2, cd, cnAlpha, alphaRad, xCpM, xCgM, radiusM;
    }

    public static class StructureModel
    {
        public const double G0 = 9.80665;
        public const double QRefKPa = 35;
        public const double SfRef = 1.4;
        public const double YieldRefMPa = 500;
        public const double BreakupG = 8;
        public const string MaxQFailureKind = "structural-maxq";

        /// <summary>F_allow = σ·2πr·t/SF + p·πr². σ and p are converted from MPa and bar.</summary>
        public static double FAllowN(StageSection stage)
        {
            if (stage == null) return 0;
            double wall = stage.yieldMPa * TankModel.MPaToPa * 2 * Math.PI * stage.radiusM * stage.wallM;
            double axial = stage.safetyFactor > 0 ? wall / stage.safetyFactor : 0;
            double pressure = stage.pressureBar * TankModel.BarToPa * Math.PI * stage.radiusM * stage.radiusM;
            if (!(axial > 0)) axial = 0;
            if (!(pressure > 0)) pressure = 0;
            return axial + pressure;
        }

        /// <summary>q_lim = 35 kPa · (SF/1.4) · (σ_y/500). Result is pascals. σ_y is MPa.</summary>
        public static double QLimPa(double safetyFactor, double yieldMPa)
        {
            if (!(safetyFactor > 0) || !(yieldMPa > 0)) return 0;
            return QRefKPa * 1000.0 * (safetyFactor / SfRef) * (yieldMPa / YieldRefMPa);
        }

        public static Allowables Allowables(StageSection stage)
        {
            var a = new Allowables();
            if (stage == null) return a;
            a.fAllowN = FAllowN(stage);
            a.qLimPa = QLimPa(stage.safetyFactor, stage.yieldMPa);
            return a;
        }

        /// <summary>Stage allowables. Hoop wall when pressure is set; otherwise the fraction shell spread over the tank surface. q_lim uses the stage material.</summary>
        public static Allowables Allowables(Catalog c, StagePart stage, double safetyFactor)
            => Allowables(Section(c, stage, safetyFactor));

        /// <summary>The fairing limit uses the fairing material, not the stage wall.</summary>
        public static double FairingQLimPa(Catalog c, Design design)
        {
            if (design == null || c == null) return 0;
            if (!c.MaterialById.TryGetValue(design.fairingMaterial ?? "", out var mat)) return 0;
            return QLimPa(design.safetyFactor, mat.yieldMPa);
        }

        public static StageSection Section(Catalog c, StagePart stage, double safetyFactor)
        {
            var s = new StageSection { safetyFactor = safetyFactor };
            if (stage == null) return s;
            s.radiusM = stage.diameter > 0 ? stage.diameter / 2 : 0;
            if (c != null && c.MaterialById.TryGetValue(stage.material ?? "", out var mat))
                s.yieldMPa = mat.yieldMPa;
            bool pressure = stage.tank != null && stage.tank.pressureSet && stage.tank.pressureBar > 0;
            if (pressure)
            {
                s.pressureBar = stage.tank.pressureBar;
                s.wallM = TankModel.WallThicknessM(safetyFactor, s.pressureBar, s.radiusM, s.yieldMPa);
            }
            else if (c != null && c.MaterialById.TryGetValue(stage.material ?? "", out var shellMat))
            {
                var g = TankModel.Geometry(c, stage);
                double legacy = Mass.StructuralFractionMass(c, stage, safetyFactor);
                s.wallM = TankModel.EquivalentWallM(legacy, shellMat.density, g.radiusM, g.barrelLengthM);
            }
            return s;
        }

        /// <summary>M_b = q·S·CN_α·α·|x_cp − x_cg|.</summary>
        public static double BendingMomentNm(LoadState state)
        {
            if (state == null) return 0;
            return state.qPa * state.areaM2 * state.cnAlpha * state.alphaRad * Math.Abs(state.xCpM - state.xCgM);
        }

        /// <summary>Thin-wall equivalent axial force. σ = M·r/I and I = πr³t, so F_eq = 2M/r.</summary>
        public static double EquivalentBendingN(LoadState state)
        {
            if (state == null || !(state.radiusM > 0)) return 0;
            return Math.Abs(2 * BendingMomentNm(state) / state.radiusM);
        }

        /// <summary>F = m_above·a_axial + q·S·Cd + bending.</summary>
        public static double LoadN(LoadState state)
        {
            if (state == null) return 0;
            return state.massAboveKg * state.aAxial + state.qPa * state.areaM2 * state.cd + EquivalentBendingN(state);
        }

        /// <summary>MS = F_allow/F − 1. No compressive load is an infinite margin. MS &lt; 0 is structural failure.</summary>
        public static double Margin(LoadState state)
        {
            double f = LoadN(state);
            if (!(f > 0)) return double.PositiveInfinity;
            return state.fAllowN / f - 1;
        }

        public static bool StructuralFailure(double margin) => margin < 0;

        public static bool ExceedsQLim(double qPa, double qLimPa) => qPa > qLimPa;

        /// <summary>a is m/s². The limit is in g. Damage and breakup are strict: equality holds.</summary>
        public static bool OverG(double accelMps2, double limitG) => limitG > 0 && accelMps2 > limitG * G0;

        public static bool PayloadDamaged(double accelMps2, double payloadMaxG) => OverG(accelMps2, payloadMaxG);

        public static bool VehicleBreakup(double accelMps2) => OverG(accelMps2, BreakupG);
    }
}
