// Point-mass gravity and Earth rotation. Port of web/js/physics/{gravity,earth}.js.
using System;

namespace Kestrel.Core.Physics
{
    public static class Gravity
    {
        public const double MU_EARTH = 3.986004418e14, R_EARTH = 6378137;
        public static double Accel(double r, double mu = MU_EARTH) => mu / (r * r);
        public static void Vec(double x, double y, double z, double[] o, double mu = MU_EARTH)
        {
            double r2 = x * x + y * y + z * z, r = Math.Sqrt(r2), k = -mu / (r2 * r);
            o[0] = k * x; o[1] = k * y; o[2] = k * z;
        }
    }

    public static class Earth
    {
        public const double OMEGA_EARTH = 7.2921159e-5;
        public static double SiteRotationSpeed(double latDeg, double radius = Gravity.R_EARTH)
            => OMEGA_EARTH * radius * Math.Cos(latDeg * Math.PI / 180);
        public static double MinInclination(double latDeg) => Math.Abs(latDeg);
    }
}
