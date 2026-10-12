// Drag coefficient vs Mach and drag force. Port of web/js/physics/aero.js.
using System;

namespace Kestrel.Core.Physics
{
    public static class Aero
    {
        public const double M_CRIT = 0.8, M_PEAK = 1.1, PEAK_FACTOR = 2.0;

        public static double CdMach(double mach, double cd0 = 0.3)
        {
            double M = Math.Abs(mach);
            if (M <= M_CRIT) return cd0;
            if (M < M_PEAK)
            {
                double s = (M - M_CRIT) / (M_PEAK - M_CRIT);
                return cd0 * (1 + (PEAK_FACTOR - 1) * s * s * (3 - 2 * s));
            }
            double r = M_PEAK / M;
            return cd0 * (1 + (PEAK_FACTOR - 1) * r * r);
        }

        public static double RefArea(double d) => Math.PI * d * d / 4;
        public static double DynamicPressure(double rho, double v) => 0.5 * rho * v * v;
        public static double DragForce(double rho, double v, double soundSpeed, double cd0, double area)
            => 0.5 * rho * v * v * CdMach(v / soundSpeed, cd0) * area;
    }
}
