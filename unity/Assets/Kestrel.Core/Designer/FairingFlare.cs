// Fairing joint and flare (DECISIONS #71, docs/VEHICLE_ENGINEERING_RULES.md "Fairing joint and flare").
// The fairing base is the top stage's diameter at the joint. fairing.diameter stays the barrel (widest part), and a
// conical shoulder joins the two. The shoulder is carved out of the fairing's own cylinder, so total length, stack
// height, mass, CG and payload fit do not change. Render and advice only: nothing here feeds Cd, mass or flight.
// Twins: web/js/designer/validate.js (fairingShoulder*), RocketMeshBuilder / FairingLayout (Unity), designer/ui.js (SVG).
using System;

namespace Kestrel.Core.Designer
{
    public static class FairingFlare
    {
        /// <summary>The joint radius: the top stage's radius, whatever the barrel diameter is.</summary>
        public static double BaseRadius(double topStageDiameter) => topStageDiameter * 0.5;

        /// <summary>Cylinder length: the fairing length less the nose (nose fraction as Balance.NoseFraction).</summary>
        public static double CylinderLength(double fairingLength, string nose) => fairingLength * (1 - Balance.NoseFraction(nose));

        /// <summary>L_sh = min(dR / tan(shoulder angle), maxFraction x cylinder). Zero when the radii match or there is no cylinder.</summary>
        public static double ShoulderLength(double baseRadius, double barrelRadius, double cylinderLength)
        {
            double dr = Math.Abs(barrelRadius - baseRadius);
            if (dr < 1e-6 || !(cylinderLength > 0)) return 0;
            var lim = DesignerLimits.Active;
            double tan = Math.Tan(lim.fairingShoulderAngleDeg * Math.PI / 180.0);
            return Math.Min(dr / tan, lim.fairingShoulderMaxCylFraction * cylinderLength);
        }

        /// <summary>Half angle of the shoulder in degrees: atan(dR / L_sh). 15 unless the cylinder limit bites.</summary>
        public static double ShoulderAngleDeg(double baseRadius, double barrelRadius, double shoulderLength)
            => shoulderLength > 0 ? Math.Atan(Math.Abs(barrelRadius - baseRadius) / shoulderLength) * 180.0 / Math.PI : 0;

        /// <summary>Radius h above the joint: the cone from the base radius up to the barrel radius.</summary>
        public static double Radius(double baseRadius, double barrelRadius, double shoulderLength, double h)
        {
            if (h <= 0) return baseRadius;
            if (!(shoulderLength > 0) || h >= shoulderLength) return barrelRadius;
            return baseRadius + (barrelRadius - baseRadius) * (h / shoulderLength);
        }

        /// <summary>Barrel diameter over top stage diameter.</summary>
        public static double Ratio(double fairingDiameter, double topStageDiameter) => topStageDiameter > 0 ? fairingDiameter / topStageDiameter : 0;

        /// <summary>The fairing-flare advisory: a flare (barrel wider than the stage) that is over warnRatio, or whose shoulder half angle is over warnAngleDeg.</summary>
        public static bool Extreme(double fairingDiameter, double topStageDiameter, double fairingLength, string nose)
        {
            if (!(topStageDiameter > 0) || !(fairingDiameter > topStageDiameter)) return false;
            var lim = DesignerLimits.Active;
            if (Ratio(fairingDiameter, topStageDiameter) > lim.fairingFlareWarnRatio + 1e-9) return true;
            double rb = BaseRadius(topStageDiameter), rf = fairingDiameter * 0.5;
            double sh = ShoulderLength(rb, rf, CylinderLength(fairingLength, nose));
            return ShoulderAngleDeg(rb, rf, sh) > lim.fairingFlareWarnAngleDeg + 1e-9;
        }

        /// <summary>
        /// Radius the exploded view draws the payload at: the cargo's real diameter, held just inside the barrel so a cargo
        /// that does not fit (Validate blocks that design) does not poke through the skin. With no known cargo it falls back
        /// to the old 0.55 x the barrel radius.
        /// </summary>
        public static double CargoDrawRadius(double cargoDiameter, double barrelRadius)
            => cargoDiameter > 0 ? Math.Min(cargoDiameter * 0.5, barrelRadius * 0.98) : barrelRadius * 0.55;

        /// <summary>The ratio as words for advice text: "half" for 0.5, else a percent.</summary>
        public static string RatioWords(double ratio)
            => Math.Abs(ratio - 0.5) < 1e-9 ? "half" : Math.Round(ratio * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
