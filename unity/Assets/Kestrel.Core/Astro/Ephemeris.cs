// R2/S1 shared astronomy core: deterministic, engine-free sun and Moon positions.
// Sun: Meeus, Astronomical Algorithms ch. 25 (low precision, ~0.01 deg).
// Moon: Meeus ch. 47 main periodic terms (truncated ELP-2000/82), plus nutation in longitude and
// obliquity (ch. 22, main terms). Output is geocentric apparent RA/Dec of date, the frame JPL Horizons
// reports for QUANTITIES=2. Golden vectors: data/astro/horizons-sun-moon.json (<= 0.1 deg, 2000-2100).
// Time argument is Julian Date in TT. Use Time.UtcToTT for wall-clock input.
using System;

namespace Kestrel.Core.Astro
{
    public struct EquatorialPos
    {
        public double RaDeg, DecDeg, DistanceKm;
        public EquatorialPos(double ra, double dec, double dist) { RaDeg = ra; DecDeg = dec; DistanceKm = dist; }
    }

    public static class Time
    {
        public const double J2000 = 2451545.0;
        /// <summary>Julian centuries of TT since J2000.</summary>
        public static double T(double jdTT) => (jdTT - J2000) / 36525.0;

        /// <summary>JD (UTC) from a calendar date-time, proleptic Gregorian (Meeus ch. 7).</summary>
        public static double JulianDate(int y, int mo, int d, double hours)
        {
            if (mo <= 2) { y -= 1; mo += 12; }
            int a = y / 100, b = 2 - a + a / 4;
            return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (mo + 1)) + d + b - 1524.5 + hours / 24.0;
        }

        /// <summary>Delta T = TT - UT in seconds (Espenak and Meeus polynomials, 2005-2150).</summary>
        public static double DeltaT(double year)
        {
            if (year < 2005) { double t = year - 2000; return 63.86 + 0.3345 * t - 0.060374 * t * t + 0.0017275 * t * t * t + 0.000651814 * t * t * t * t + 0.00002373599 * t * t * t * t * t; }
            if (year < 2050) { double t = year - 2000; return 62.92 + 0.32217 * t + 0.005589 * t * t; }
            if (year < 2150) { double u = (year - 1820) / 100; return -20 + 32 * u * u - 0.5628 * (2150 - year); }
            { double u = (year - 1820) / 100; return -20 + 32 * u * u; }
        }

        public static double UtcToTT(double jdUtc) { double year = 2000 + (jdUtc - J2000) / 365.25; return jdUtc + DeltaT(year) / 86400.0; }
    }

    public static class Ephemeris
    {
        const double D2R = Math.PI / 180.0, R2D = 180.0 / Math.PI;
        public const double AU_KM = 149597870.7;

        static double Norm(double deg) { deg %= 360.0; return deg < 0 ? deg + 360.0 : deg; }

        /// <summary>Nutation in longitude and obliquity (deg), Meeus ch. 22 main terms (~0.5").</summary>
        public static void Nutation(double T, out double dPsi, out double dEps, out double epsTrue)
        {
            double om = Norm(125.04452 - 1934.136261 * T) * D2R;
            double L = Norm(280.4665 + 36000.7698 * T) * D2R, Lp = Norm(218.3165 + 481267.8813 * T) * D2R;
            dPsi = (-17.20 * Math.Sin(om) - 1.32 * Math.Sin(2 * L) - 0.23 * Math.Sin(2 * Lp) + 0.21 * Math.Sin(2 * om)) / 3600.0;
            dEps = (9.20 * Math.Cos(om) + 0.57 * Math.Cos(2 * L) + 0.10 * Math.Cos(2 * Lp) - 0.09 * Math.Cos(2 * om)) / 3600.0;
            double eps0 = 23.0 + 26.0 / 60 + 21.448 / 3600 - (46.8150 * T + 0.00059 * T * T - 0.001813 * T * T * T) / 3600.0;
            epsTrue = eps0 + dEps;
        }

        static EquatorialPos FromEcliptic(double lonDeg, double latDeg, double epsDeg, double distKm)
        {
            double l = lonDeg * D2R, b = latDeg * D2R, e = epsDeg * D2R;
            double ra = Math.Atan2(Math.Sin(l) * Math.Cos(e) - Math.Tan(b) * Math.Sin(e), Math.Cos(l));
            double dec = Math.Asin(Math.Sin(b) * Math.Cos(e) + Math.Cos(b) * Math.Sin(e) * Math.Sin(l));
            return new EquatorialPos(Norm(ra * R2D), dec * R2D, distKm);
        }

        /// <summary>Sun apparent ecliptic longitude (deg) and distance (km).</summary>
        public static void SunEcliptic(double jdTT, out double lonDeg, out double distKm)
        {
            double T = Time.T(jdTT);
            double L0 = 280.46646 + 36000.76983 * T + 0.0003032 * T * T;
            double M = (357.52911 + 35999.05029 * T - 0.0001537 * T * T) * D2R;
            double e = 0.016708634 - 0.000042037 * T - 0.0000001267 * T * T;
            double C = (1.914602 - 0.004817 * T - 0.000014 * T * T) * Math.Sin(M) + (0.019993 - 0.000101 * T) * Math.Sin(2 * M) + 0.000289 * Math.Sin(3 * M);
            double trueLon = L0 + C, v = M + C * D2R;
            double R = 1.000001018 * (1 - e * e) / (1 + e * Math.Cos(v));
            Nutation(T, out var dPsi, out _, out _);
            lonDeg = Norm(trueLon - 0.00569 + dPsi);
            distKm = R * AU_KM;
        }

        public static EquatorialPos Sun(double jdTT)
        {
            SunEcliptic(jdTT, out var lon, out var dist);
            Nutation(Time.T(jdTT), out _, out _, out var eps);
            return FromEcliptic(lon, 0, eps, dist);
        }

        // Meeus table 47.A: D, M, M', F, sigma_l (1e-6 deg), sigma_r (1e-3 km)
        static readonly int[,] LR = {
            {0,0,1,0,6288774,-20905355},{2,0,-1,0,1274027,-3699111},{2,0,0,0,658314,-2955968},{0,0,2,0,213618,-569925},
            {0,1,0,0,-185116,48888},{0,0,0,2,-114332,-3149},{2,0,-2,0,58793,246158},{2,-1,-1,0,57066,-152138},
            {2,0,1,0,53322,-170733},{2,-1,0,0,45758,-204586},{0,1,-1,0,-40923,-129620},{1,0,0,0,-34720,108743},
            {0,1,1,0,-30383,104755},{2,0,0,-2,15327,10321},{0,0,1,2,-12528,0},{0,0,1,-2,10980,79661},
            {4,0,-1,0,10675,-34782},{0,0,3,0,10034,-23210},{4,0,-2,0,8548,-21636},{2,1,-1,0,-7888,24208},
            {2,1,0,0,-6766,30824},{1,0,-1,0,-5163,-8379},{1,1,0,0,4987,-16675},{2,-1,1,0,4036,-12831},
            {2,0,2,0,3994,-10445},{4,0,0,0,3861,-11650},{2,0,-3,0,3665,14403},{0,1,-2,0,-2689,-7003},
            {2,0,-1,2,-2602,0},{2,-1,-2,0,2390,10056},{1,0,1,0,-2348,6322},{2,-2,0,0,2236,-9884},
            {0,1,2,0,-2120,5751},{0,2,0,0,-2069,0},{2,-2,-1,0,2048,-4950},{2,0,1,-2,-1773,4130},
            {2,0,0,2,-1595,0},{4,-1,-1,0,1215,-3958},{0,0,2,2,-1110,0},{3,0,-1,0,-892,3258},
            {2,1,1,0,-810,2616},{4,-1,-2,0,759,-1897},{0,2,-1,0,-713,-2117},{2,2,-1,0,-700,2354},
            {2,1,-2,0,691,0},{2,-1,0,-2,596,0},{4,0,1,0,549,-1423},{0,0,4,0,537,-1117},
            {4,-1,0,0,520,-1571},{1,0,-2,0,-487,-1739},{2,1,0,-2,-399,0},{0,0,2,-2,-381,-4421},
            {1,1,1,0,351,0},{3,0,-2,0,-340,0},{4,0,-3,0,330,0},{2,-1,2,0,327,0},
            {0,2,1,0,-323,1165},{1,1,-1,0,299,0},{2,0,3,0,294,0},{2,0,-1,-2,0,8752}};
        // Meeus table 47.B: D, M, M', F, sigma_b (1e-6 deg)
        static readonly int[,] B = {
            {0,0,0,1,5128122},{0,0,1,1,280602},{0,0,1,-1,277693},{2,0,0,-1,173237},{2,0,-1,1,55413},
            {2,0,-1,-1,46271},{2,0,0,1,32573},{0,0,2,1,17198},{2,0,1,-1,9266},{0,0,2,-1,8822},
            {2,-1,0,-1,8216},{2,0,-2,-1,4324},{2,0,1,1,4200},{2,1,0,-1,-3359},{2,-1,-1,1,2463},
            {2,-1,0,1,2211},{2,-1,-1,-1,2065},{0,1,-1,-1,-1870},{4,0,-1,-1,1828},{0,1,0,1,-1794},
            {0,0,0,3,-1749},{0,1,-1,1,-1565},{1,0,0,1,-1491},{0,1,1,1,-1475},{0,1,1,-1,-1410},
            {0,1,0,-1,-1344},{1,0,0,-1,-1335},{0,0,3,1,1107},{4,0,0,-1,1021},{4,0,-1,1,833}};

        /// <summary>Moon apparent geocentric ecliptic longitude/latitude of date (deg) and distance (km).</summary>
        public static void MoonEcliptic(double jdTT, out double lonDeg, out double latDeg, out double distKm)
        {
            double T = Time.T(jdTT), T2 = T * T, T3 = T2 * T, T4 = T3 * T;
            double Lp = Norm(218.3164477 + 481267.88123421 * T - 0.0015786 * T2 + T3 / 538841 - T4 / 65194000);
            double D = Norm(297.8501921 + 445267.1114034 * T - 0.0018819 * T2 + T3 / 545868 - T4 / 113065000) * D2R;
            double M = Norm(357.5291092 + 35999.0502909 * T - 0.0001536 * T2 + T3 / 24490000) * D2R;
            double Mp = Norm(134.9633964 + 477198.8675055 * T + 0.0087414 * T2 + T3 / 69699 - T4 / 14712000) * D2R;
            double F = Norm(93.2720950 + 483202.0175233 * T - 0.0036539 * T2 - T3 / 3526000 + T4 / 863310000) * D2R;
            double A1 = Norm(119.75 + 131.849 * T) * D2R, A2 = Norm(53.09 + 479264.290 * T) * D2R, A3 = Norm(313.45 + 481266.484 * T) * D2R;
            double E = 1 - 0.002516 * T - 0.0000074 * T2;
            double sl = 0, sr = 0, sb = 0;
            for (int i = 0; i < LR.GetLength(0); i++)
            {
                double arg = LR[i, 0] * D + LR[i, 1] * M + LR[i, 2] * Mp + LR[i, 3] * F;
                double ef = Math.Abs(LR[i, 1]) == 1 ? E : Math.Abs(LR[i, 1]) == 2 ? E * E : 1;
                sl += LR[i, 4] * ef * Math.Sin(arg); sr += LR[i, 5] * ef * Math.Cos(arg);
            }
            for (int i = 0; i < B.GetLength(0); i++)
            {
                double arg = B[i, 0] * D + B[i, 1] * M + B[i, 2] * Mp + B[i, 3] * F;
                double ef = Math.Abs(B[i, 1]) == 1 ? E : Math.Abs(B[i, 1]) == 2 ? E * E : 1;
                sb += B[i, 4] * ef * Math.Sin(arg);
            }
            double LpR = Lp * D2R;
            sl += 3958 * Math.Sin(A1) + 1962 * Math.Sin(LpR - F) + 318 * Math.Sin(A2);
            sb += -2235 * Math.Sin(LpR) + 382 * Math.Sin(A3) + 175 * Math.Sin(A1 - F) + 175 * Math.Sin(A1 + F) + 127 * Math.Sin(LpR - Mp) - 115 * Math.Sin(LpR + Mp);
            Nutation(T, out var dPsi, out _, out _);
            lonDeg = Norm(Lp + sl / 1e6 + dPsi);
            latDeg = sb / 1e6;
            distKm = 385000.56 + sr / 1000.0;
        }

        public static EquatorialPos Moon(double jdTT)
        {
            MoonEcliptic(jdTT, out var lon, out var lat, out var dist);
            Nutation(Time.T(jdTT), out _, out _, out var eps);
            return FromEcliptic(lon, lat, eps, dist);
        }

        /// <summary>Moon phase angle elongation-based illuminated fraction (0 new .. 1 full), Meeus ch. 48.</summary>
        public static double MoonIlluminatedFraction(double jdTT)
        {
            var s = Sun(jdTT); var m = Moon(jdTT);
            double psi = AngularSeparationDeg(s.RaDeg, s.DecDeg, m.RaDeg, m.DecDeg) * D2R;
            double i = Math.Atan2(s.DistanceKm * Math.Sin(psi), m.DistanceKm - s.DistanceKm * Math.Cos(psi));
            return (1 + Math.Cos(i)) / 2;
        }

        /// <summary>Great-circle separation in degrees (Vincenty form, stable at all angles).</summary>
        public static double AngularSeparationDeg(double ra1, double dec1, double ra2, double dec2)
        {
            double a1 = ra1 * D2R, d1 = dec1 * D2R, a2 = ra2 * D2R, d2 = dec2 * D2R, da = a2 - a1;
            double x = Math.Cos(d2) * Math.Sin(da), y = Math.Cos(d1) * Math.Sin(d2) - Math.Sin(d1) * Math.Cos(d2) * Math.Cos(da);
            double z = Math.Sin(d1) * Math.Sin(d2) + Math.Cos(d1) * Math.Cos(d2) * Math.Cos(da);
            return Math.Atan2(Math.Sqrt(x * x + y * y), z) * R2D;
        }

        /// <summary>Unit direction in an equatorial-of-date frame (x to equinox, z to north pole).</summary>
        public static void Direction(EquatorialPos p, out double x, out double y, out double z)
        {
            double a = p.RaDeg * D2R, d = p.DecDeg * D2R;
            x = Math.Cos(d) * Math.Cos(a); y = Math.Cos(d) * Math.Sin(a); z = Math.Sin(d);
        }
    }
}
