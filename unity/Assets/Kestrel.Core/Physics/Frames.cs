// WGS-84 geodetic ↔ ECEF, and ECEF ↔ ECI by Earth rotation angle only.
// No polar motion, no precession, no nutation. Julian dates come from Kestrel.Core.Astro.Time.
// J2/J3/J4 acceleration is P2. This type does not compute gravity.
using System;
using Kestrel.Core.Astro;

namespace Kestrel.Core.Physics
{
    public static class Frames
    {
        /// <summary>WGS-84 inverse flattening, 1/f. Defining value, NGA TR8350.2.</summary>
        public const double InverseFlattening = 298.257223563;

        public static double Flattening => 1.0 / InverseFlattening;

        /// <summary>IAU 2000 Earth rotation angle, radians, from JD UT1. UT1 is taken as UTC in 2.0.</summary>
        public static double EraRad(double jdUt1)
        {
            double du = jdUt1 - Time.J2000;
            double turns = 0.7790572732640 + 1.00273781191135448 * du;
            turns -= Math.Floor(turns);
            return turns * 2.0 * Math.PI;
        }

        /// <summary>ERA expressed as sidereal seconds. Not the older GMST polynomial.</summary>
        public static double EraSiderealSeconds(double jdUt1) => EraRad(jdUt1) * 86400.0 / (2.0 * Math.PI);

        public static double JdUt1FromUtc(int year, int month, int day, double hoursUtc)
            => Time.JulianDate(year, month, day, hoursUtc);

        public static void GeodeticToEcef(double latRad, double lonRad, double h, out double x, out double y, out double z)
        {
            double a = Gravity.R_EARTH;
            double e2 = Flattening * (2.0 - Flattening);
            double s = Math.Sin(latRad), c = Math.Cos(latRad);
            double n = a / Math.Sqrt(1.0 - e2 * s * s);
            x = (n + h) * c * Math.Cos(lonRad);
            y = (n + h) * c * Math.Sin(lonRad);
            z = (n * (1.0 - e2) + h) * s;
        }

        public static void EcefToGeodetic(double x, double y, double z, out double latRad, out double lonRad, out double h)
        {
            double a = Gravity.R_EARTH;
            double f = Flattening;
            double b = a * (1.0 - f);
            double e2 = f * (2.0 - f);
            double ep2 = (a * a - b * b) / (b * b);
            lonRad = Math.Atan2(y, x);
            double p = Math.Sqrt(x * x + y * y);
            h = 0;
            if (p < 1e-8)
            {
                latRad = z >= 0 ? Math.PI / 2.0 : -Math.PI / 2.0;
                h = Math.Abs(z) - b;
                return;
            }
            double theta = Math.Atan2(z * a, p * b);
            double st = Math.Sin(theta), ct = Math.Cos(theta);
            latRad = Math.Atan2(z + ep2 * b * st * st * st, p - e2 * a * ct * ct * ct);
            for (int k = 0; k < 2; k++)
            {
                double s = Math.Sin(latRad);
                double n = a / Math.Sqrt(1.0 - e2 * s * s);
                h = p / Math.Cos(latRad) - n;
                latRad = Math.Atan2(z, p * (1.0 - e2 * n / (n + h)));
            }
            double s2 = Math.Sin(latRad);
            double n2 = a / Math.Sqrt(1.0 - e2 * s2 * s2);
            h = p / Math.Cos(latRad) - n2;
        }

        public static void EciToEcef(double jdUt1, double xi, double yi, double zi, double vxi, double vyi, double vzi,
            out double xe, out double ye, out double ze, out double vxe, out double vye, out double vze)
        {
            double th = EraRad(jdUt1);
            double c = Math.Cos(th), s = Math.Sin(th);
            xe = c * xi + s * yi;
            ye = -s * xi + c * yi;
            ze = zi;
            double vxr = c * vxi + s * vyi;
            double vyr = -s * vxi + c * vyi;
            double w = Earth.OMEGA_EARTH;
            vxe = vxr + w * ye;
            vye = vyr - w * xe;
            vze = vzi;
        }

        public static void EcefToEci(double jdUt1, double xe, double ye, double ze, double vxe, double vye, double vze,
            out double xi, out double yi, out double zi, out double vxi, out double vyi, out double vzi)
        {
            double th = EraRad(jdUt1);
            double c = Math.Cos(th), s = Math.Sin(th);
            xi = c * xe - s * ye;
            yi = s * xe + c * ye;
            zi = ze;
            double w = Earth.OMEGA_EARTH;
            double vxr = vxe - w * ye;
            double vyr = vye + w * xe;
            vxi = c * vxr - s * vyr;
            vyi = s * vxr + c * vyr;
            vzi = vze;
        }
    }
}
