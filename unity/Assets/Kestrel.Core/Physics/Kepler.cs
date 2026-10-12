// Classical orbital elements ↔ ECI state, and two-body analytic coast.
// Elliptic orbits only (e < 1). No J2 here; see OnRails.
// Element conventions follow Vallado, Fundamentals of Astrodynamics and Applications, ch. 2.
using System;

namespace Kestrel.Core.Physics
{
    public struct ClassicalElements
    {
        public double a, e, i, raan, argp, m;
    }

    public static class Kepler
    {
        /// <summary>Solve Kepler's equation M = E − e sin E for the eccentric anomaly.</summary>
        public static double EccentricAnomaly(double meanAnom, double e)
        {
            double M = WrapPi(meanAnom);
            double E = e < 0.8 ? M : Math.PI;
            for (int k = 0; k < 12; k++)
            {
                double f = E - e * Math.Sin(E) - M;
                double fp = 1.0 - e * Math.Cos(E);
                double d = f / fp;
                E -= d;
                if (Math.Abs(d) < 1e-14) break;
            }
            return E;
        }

        public static double TrueAnomaly(double E, double e)
        {
            double s = Math.Sqrt(1.0 + e) * Math.Sin(E / 2.0);
            double c = Math.Sqrt(1.0 - e) * Math.Cos(E / 2.0);
            return 2.0 * Math.Atan2(s, c);
        }

        public static ClassicalElements FromState(double rx, double ry, double rz, double vx, double vy, double vz,
            double mu = Gravity.MU_EARTH)
        {
            double r = Math.Sqrt(rx * rx + ry * ry + rz * rz);
            double v2 = vx * vx + vy * vy + vz * vz;
            double rv = rx * vx + ry * vy + rz * vz;
            double hx = ry * vz - rz * vy, hy = rz * vx - rx * vz, hz = rx * vy - ry * vx;
            double h = Math.Sqrt(hx * hx + hy * hy + hz * hz);
            double energy = v2 / 2.0 - mu / r;
            double a = -mu / (2.0 * energy);
            double ex = ((v2 - mu / r) * rx - rv * vx) / mu;
            double ey = ((v2 - mu / r) * ry - rv * vy) / mu;
            double ez = ((v2 - mu / r) * rz - rv * vz) / mu;
            double e = Math.Sqrt(ex * ex + ey * ey + ez * ez);
            double i = h > 0 ? Math.Acos(Math.Max(-1, Math.Min(1, hz / h))) : 0;
            // Line of nodes n = ẑ × h = (−hy, hx, 0)
            double nx = -hy, ny = hx;
            double nxy = Math.Sqrt(nx * nx + ny * ny);
            double raan = nxy < 1e-12 ? 0 : Math.Atan2(ny, nx);
            if (raan < 0) raan += 2.0 * Math.PI;

            double argp;
            double nu;
            if (e < 1e-12)
            {
                argp = 0;
                if (nxy < 1e-12)
                {
                    // Circular equatorial: true longitude from +x
                    nu = Math.Atan2(ry, rx);
                    if (nu < 0) nu += 2.0 * Math.PI;
                }
                else
                {
                    // Circular inclined: argument of latitude u = atan2( r · (ĥ × n̂), r · n̂ )
                    double cosU = (nx * rx + ny * ry) / (nxy * r);
                    double hxnX = -hz * ny, hxnY = hz * nx, hxnZ = hx * ny - hy * nx;
                    double sinU = (rx * hxnX + ry * hxnY + rz * hxnZ) / (h * r);
                    nu = Math.Atan2(sinU, cosU);
                    if (nu < 0) nu += 2.0 * Math.PI;
                }
            }
            else if (nxy < 1e-12)
            {
                // Elliptic equatorial: longitude of periapsis
                argp = Math.Atan2(ey, ex);
                if (argp < 0) argp += 2.0 * Math.PI;
                double cosNu = (ex * rx + ey * ry + ez * rz) / (e * r);
                nu = Math.Acos(Math.Max(-1, Math.Min(1, cosNu)));
                if (rv < 0) nu = 2.0 * Math.PI - nu;
            }
            else
            {
                // Elliptic inclined: ω = atan2( ê · (ĥ × n̂), ê · n̂ )
                double hxnX = hy * 0 - hz * ny, hxnY = hz * nx - hx * 0, hxnZ = hx * ny - hy * nx;
                double sinW = (ex * hxnX + ey * hxnY + ez * hxnZ) / h;
                double cosW = ex * nx + ey * ny;
                argp = Math.Atan2(sinW, cosW);
                if (argp < 0) argp += 2.0 * Math.PI;
                double cosNu = (ex * rx + ey * ry + ez * rz) / (e * r);
                nu = Math.Acos(Math.Max(-1, Math.Min(1, cosNu)));
                if (rv < 0) nu = 2.0 * Math.PI - nu;
            }

            double E = 2.0 * Math.Atan(Math.Sqrt(Math.Max(0, (1 - e) / (1 + e))) * Math.Tan(nu / 2.0));
            double m = WrapTwoPi(E - e * Math.Sin(E));
            return new ClassicalElements { a = a, e = e, i = i, raan = raan, argp = argp, m = m };
        }

        public static void ToState(ClassicalElements el, out double rx, out double ry, out double rz,
            out double vx, out double vy, out double vz, double mu = Gravity.MU_EARTH)
        {
            double E = EccentricAnomaly(el.m, el.e);
            double nu = TrueAnomaly(E, el.e);
            double p = el.a * (1.0 - el.e * el.e);
            double r = p / (1.0 + el.e * Math.Cos(nu));
            double x = r * Math.Cos(nu), y = r * Math.Sin(nu);
            double vxP = -Math.Sqrt(mu / p) * Math.Sin(nu);
            double vyP = Math.Sqrt(mu / p) * (el.e + Math.Cos(nu));
            RotPqwToEci(el.raan, el.i, el.argp, x, y, 0, out rx, out ry, out rz);
            RotPqwToEci(el.raan, el.i, el.argp, vxP, vyP, 0, out vx, out vy, out vz);
        }

        /// <summary>Two-body analytic coast by Δt seconds. a, e, i, Ω, ω fixed; M advances by n·Δt.</summary>
        public static ClassicalElements Propagate(ClassicalElements el, double dt, double mu = Gravity.MU_EARTH)
        {
            double n = Math.Sqrt(mu / (el.a * el.a * el.a));
            var o = el;
            o.m = WrapTwoPi(el.m + n * dt);
            return o;
        }

        public static void PropagateState(double rx0, double ry0, double rz0, double vx0, double vy0, double vz0,
            double dt, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz,
            double mu = Gravity.MU_EARTH)
        {
            var el = FromState(rx0, ry0, rz0, vx0, vy0, vz0, mu);
            ToState(Propagate(el, dt, mu), out rx, out ry, out rz, out vx, out vy, out vz, mu);
        }

        static void RotPqwToEci(double raan, double i, double argp, double x, double y, double z,
            out double X, out double Y, out double Z)
        {
            double cO = Math.Cos(raan), sO = Math.Sin(raan);
            double ci = Math.Cos(i), si = Math.Sin(i);
            double cw = Math.Cos(argp), sw = Math.Sin(argp);
            double r11 = cO * cw - sO * sw * ci;
            double r12 = -cO * sw - sO * cw * ci;
            double r13 = sO * si;
            double r21 = sO * cw + cO * sw * ci;
            double r22 = -sO * sw + cO * cw * ci;
            double r23 = -cO * si;
            double r31 = sw * si;
            double r32 = cw * si;
            X = r11 * x + r12 * y + r13 * z;
            Y = r21 * x + r22 * y + r23 * z;
            Z = r31 * x + r32 * y + ci * z;
        }

        static double WrapPi(double a)
        {
            a = WrapTwoPi(a);
            if (a > Math.PI) a -= 2.0 * Math.PI;
            return a;
        }

        static double WrapTwoPi(double a)
        {
            a %= 2.0 * Math.PI;
            if (a < 0) a += 2.0 * Math.PI;
            return a;
        }
    }
}
