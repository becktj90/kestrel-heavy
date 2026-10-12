// One interstage length for mass, drag and CP: 0.6 × the larger stage diameter.
// That is the mesh rule RocketMeshBuilder already draws (1.2 × max radius).
// Mass is areal density × frustum lateral area. An explicit interstageMass is the Engineer override.
// Flare and shoulder add a flat +0.02 Cd. Straight adds none. See docs/PHYSICS.md.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;

namespace Kestrel.Core.Vehicle
{
    public static class InterstageAero
    {
        public static double Length(double dLower, double dUpper)
            => DesignerLimits.Active.interstageLengthFactor * Math.Max(dLower, dUpper);

        public static double LateralArea(double dLower, double dUpper)
        {
            double L = Length(dLower, dUpper);
            double r1 = dLower / 2, r2 = dUpper / 2;
            double slant = Math.Sqrt(L * L + (r1 - r2) * (r1 - r2));
            return Math.PI * (r1 + r2) * slant;
        }

        public static double DerivedMass(Catalog c, string material, double dLower, double dUpper)
            => DesignerLimits.Active.Areal(material) * LateralArea(dLower, dUpper);

        /// <summary>
        /// Kilograms added to this stage's dry mass. An explicit interstageMass wins, including 0.
        /// An absent key on a version 1 design is 0, matching JS. The frustum is derived only at
        /// version 2 or higher, and only on the upper stage of a joint whose lower stage did not
        /// already carry an override.
        /// </summary>
        public static double MassKg(Catalog c, Design d, int i)
        {
            var s = d.stages[i];
            if (s.interstageSpecified) return s.interstageMass;
            if (d == null || d.version < 2) return 0;
            if (i <= 0) return 0;
            if (d.stages[i - 1].interstageSpecified) return 0;
            return DerivedMass(c, s.material, d.stages[i - 1].diameter, s.diameter);
        }

        public static double MassOf(Catalog c, Design d, StagePart part)
        {
            if (d == null || part == null) return 0;
            int i = d.stages.IndexOf(part);
            if (i >= 0) return MassKg(c, d, i);
            return part.interstageSpecified ? part.interstageMass : 0;
        }

        /// <summary>Flat labelled drag. +0.02 per flare or shoulder. Absent shape is straight.</summary>
        public static double DragCd(Design d)
        {
            if (d?.stages == null) return 0;
            double cd = 0, step = DesignerLimits.Active.interstageDragCd;
            for (int i = 1; i < d.stages.Count; i++)
            {
                var shape = d.stages[i].interstageShape;
                if (shape == "flare" || shape == "shoulder") cd += step;
            }
            return cd;
        }

        /// <summary>
        /// Barrowman conical transition. CNa uses dRef. xFromForward is the CP distance aft of the
        /// forward end of the frustum (0 when the term is skipped). Positive CNa is a base flare
        /// (wider aft) and moves vehicle CP aft. Negative CNa is a shoulder and is destabilizing.
        /// </summary>
        public static void Conical(double dAft, double dForward, double length, double dRef, out double cna, out double xFromForward)
        {
            cna = 0;
            xFromForward = 0;
            if (!(dRef > 0) || !(dAft > 0) || !(dForward > 0)) return;
            cna = 2 * (Sq(dAft / dRef) - Sq(dForward / dRef));
            if (cna == 0 || !(length > 0)) return;
            double ratio = dForward / dAft;
            double denom = 1 - ratio * ratio;
            if (denom != 0) xFromForward = (length / 3) * (1 + (1 - ratio) / denom);
        }

        static double Sq(double x) => x * x;
    }
}
