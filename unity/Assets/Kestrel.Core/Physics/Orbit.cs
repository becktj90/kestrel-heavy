// State vector -> orbital elements. Port of web/js/physics/orbit.js.
using System;

namespace Kestrel.Core.Physics
{
    public struct OrbitElements
    {
        public double a, e, i, energy, h, periapsis, apoapsis, periAlt, apoAlt, period;
    }

    public static class Orbit
    {
        public static OrbitElements Elements(double rx, double ry, double rz, double vx, double vy, double vz, double mu = Gravity.MU_EARTH)
        {
            double rm = Math.Sqrt(rx * rx + ry * ry + rz * rz);
            double v2 = vx * vx + vy * vy + vz * vz;
            double hx = ry * vz - rz * vy, hy = rz * vx - rx * vz, hz = rx * vy - ry * vx;
            double h = Math.Sqrt(hx * hx + hy * hy + hz * hz);
            double energy = v2 / 2 - mu / rm;
            double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (mu * mu)));
            double a = energy == 0 ? double.PositiveInfinity : -mu / (2 * energy);
            double peri = h * h / (mu * (1 + e));
            double apo = e < 1 ? a * (1 + e) : double.PositiveInfinity;
            var o = new OrbitElements
            {
                a = a, e = e, i = h > 0 ? Math.Acos(Math.Max(-1, Math.Min(1, hz / h))) : 0,
                energy = energy, h = h, periapsis = peri, apoapsis = apo,
                periAlt = peri - Gravity.R_EARTH, apoAlt = apo - Gravity.R_EARTH,
                period = e < 1 ? 2 * Math.PI * Math.Sqrt(a * a * a / mu) : double.PositiveInfinity,
            };
            return o;
        }

        public static OrbitElements Planar(double x, double y, double vx, double vy, double mu = Gravity.MU_EARTH)
            => Elements(x, y, 0, vx, vy, 0, mu);

        public static double CircularVelocity(double alt, double mu = Gravity.MU_EARTH, double radius = Gravity.R_EARTH)
            => Math.Sqrt(mu / (radius + alt));
    }
}
