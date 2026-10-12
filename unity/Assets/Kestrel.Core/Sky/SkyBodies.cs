// Sun and star directions. Equatorial positions rotate by the IAU 2000 Earth rotation angle
// (Frames.EraRad, the SOFA era00 split-JD evaluation the rest of the sim uses). No precession,
// matching Frames. Stars come from StarCatalog (Yale BSC5 via NASA HEASARC, public domain).
using System;
using System.Collections.Generic;
using Kestrel.Core.Astro;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Sky
{
    public struct SkyVec
    {
        public double X, Y, Z;
        public SkyVec(double x, double y, double z) { X = x; Y = y; Z = z; }
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public SkyVec Normalized()
        {
            double m = Length;
            return m < 1e-12 ? new SkyVec(0, 1, 0) : new SkyVec(X / m, Y / m, Z / m);
        }
    }

    public struct StarPoint
    {
        public int Id;
        public double RaDeg, DecDeg, VMag, BV, Brightness;
    }

    /// <summary>Default Haven afternoon. Latitude matches Stack.LAUNCH_LATITUDE. Longitude is the fictional coast, not a real site name.</summary>
    public static class SkyClock
    {
        public const double LatitudeDeg = 28.5;
        public const double LongitudeDeg = -80.6;
        public const int Year = 2026, Month = 6, Day = 21;
        /// <summary>16:00 UTC is late morning on this coast, sun well up.</summary>
        public const double HoursUtc = 16.0;
    }

    public static class SkyBodies
    {
        public const double Deg2Rad = Math.PI / 180.0;
        public const double Rad2Deg = 180.0 / Math.PI;
        /// <summary>Player command that forces a night sun. SkyRig reads this in Refresh.</summary>
        public const string NightArg = "-khNight";
        /// <summary>Local civil hour used by -khNight. At Haven on the default date the sun is about −31°.</summary>
        public const double NightHoursLocal = 2.0;
        /// <summary>Visual cap for the shipped star list. The full BSC file stays in data/astro.</summary>
        public const double VisualMaxMag = 6.0;

        /// <summary>Equatorial unit vector (x toward the equinox, z toward the north pole) to Unity (x east, y up, z north).</summary>
        public static SkyVec EquatorialToUnity(double raDeg, double decDeg, double latDeg, double lonDeg, double jd1, double jd2)
        {
            double ra = raDeg * Deg2Rad, dec = decDeg * Deg2Rad;
            double xi = Math.Cos(dec) * Math.Cos(ra);
            double yi = Math.Cos(dec) * Math.Sin(ra);
            double zi = Math.Sin(dec);
            double th = Frames.EraRad(jd1, jd2);
            double c = Math.Cos(th), s = Math.Sin(th);
            double xe = c * xi + s * yi;
            double ye = -s * xi + c * yi;
            double ze = zi;
            double lat = latDeg * Deg2Rad, lon = lonDeg * Deg2Rad;
            double slon = Math.Sin(lon), clon = Math.Cos(lon), slat = Math.Sin(lat), clat = Math.Cos(lat);
            double east = -slon * xe + clon * ye;
            double north = -slat * clon * xe - slat * slon * ye + clat * ze;
            double up = clat * clon * xe + clat * slon * ye + slat * ze;
            return new SkyVec(east, up, north);
        }

        /// <summary>Mean solar local hours to UTC. Local = UTC + longitude/15. Hours may fall outside 0..24; the Julian date rolls the day.</summary>
        public static double UtcFromLocalHours(double hoursLocal, double longitudeDeg)
        {
            if (double.IsNaN(hoursLocal)) hoursLocal = 0;
            double local = hoursLocal % 24.0;
            if (local < 0) local += 24.0;
            return local - longitudeDeg / 15.0;
        }

        public static bool HasNightArg(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
                if (string.Equals(args[i], NightArg, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Display weight for a V magnitude. 1 at V = −1.5, about 0.18 at V = 6.
        /// Linear in magnitude so a mag-6 point is faint but still a point, not a missing texel.
        /// </summary>
        public static double DisplayGain(double vmag)
        {
            if (double.IsNaN(vmag)) vmag = VisualMaxMag;
            double t = SkyColor.Clamp01((vmag + 1.5) / 7.5);
            return 1.0 - 0.82 * t;
        }

        public static double SunElevationRad(double latDeg, double lonDeg, int year, int month, int day, double hoursUtc)
        {
            Frames.JdUt1Parts(year, month, day, hoursUtc, out var jd1, out var jd2);
            double jdUtc = Time.JulianDate(year, month, day, hoursUtc);
            var sun = Ephemeris.Sun(Time.UtcToTT(jdUtc));
            var d = EquatorialToUnity(sun.RaDeg, sun.DecDeg, latDeg, lonDeg, jd1, jd2);
            return Math.Asin(Math.Max(-1, Math.Min(1, d.Y)));
        }

        public static SkyVec SunUnity(double latDeg, double lonDeg, int year, int month, int day, double hoursUtc)
        {
            Frames.JdUt1Parts(year, month, day, hoursUtc, out var jd1, out var jd2);
            double jdUtc = Time.JulianDate(year, month, day, hoursUtc);
            var sun = Ephemeris.Sun(Time.UtcToTT(jdUtc));
            return EquatorialToUnity(sun.RaDeg, sun.DecDeg, latDeg, lonDeg, jd1, jd2).Normalized();
        }

        /// <summary>Equatorial +X, +Y, +Z expressed in Unity world, for the star texture.</summary>
        public static void EquatorialAxes(double latDeg, double lonDeg, double jd1, double jd2, out SkyVec x, out SkyVec y, out SkyVec z)
        {
            x = EquatorialToUnity(0, 0, latDeg, lonDeg, jd1, jd2);
            y = EquatorialToUnity(90, 0, latDeg, lonDeg, jd1, jd2);
            z = EquatorialToUnity(0, 90, latDeg, lonDeg, jd1, jd2);
        }

        /// <summary>IAU galactic north pole, equatorial unit vector. The milky band is the plane perpendicular to this.</summary>
        public static SkyVec GalacticNorthEquatorial()
        {
            double ra = 192.85948 * Deg2Rad, dec = 27.12825 * Deg2Rad;
            return new SkyVec(Math.Cos(dec) * Math.Cos(ra), Math.Cos(dec) * Math.Sin(ra), Math.Sin(dec));
        }

        /// <summary>
        /// Brightest stars from the BSC catalogue, capped by StarCatalog.BudgetForTier.
        /// Brightness is flux relative to the brightest star kept (Sirius ≈ 1).
        /// </summary>
        public static List<StarPoint> LoadStars(string csv, string tier)
        {
            int budget = StarCatalog.BudgetForTier(string.IsNullOrEmpty(tier) ? "medium" : tier);
            var parsed = StarCatalog.Parse(csv ?? "", double.PositiveInfinity, budget);
            var list = new List<StarPoint>(parsed.Count);
            double maxFlux = 1e-12;
            var flux = new double[parsed.Count];
            for (int i = 0; i < parsed.Count; i++)
            {
                flux[i] = Math.Pow(10, -0.4 * parsed[i].VMag);
                if (flux[i] > maxFlux) maxFlux = flux[i];
            }
            for (int i = 0; i < parsed.Count; i++)
            {
                var s = parsed[i];
                list.Add(new StarPoint
                {
                    Id = s.Id, RaDeg = s.RaDeg, DecDeg = s.DecDeg, VMag = s.VMag, BV = s.BV,
                    Brightness = flux[i] / maxFlux,
                });
            }
            return list;
        }

        /// <summary>Rough B−V to linear RGB. Negative is blue, ~0.6 is white, &gt; 1.2 is orange.</summary>
        public static Rgb ColorFromBv(double bv)
        {
            if (double.IsNaN(bv)) bv = 0.6;
            double t = SkyColor.Clamp01((bv + 0.3) / 1.8);
            var cool = new Rgb(0.65, 0.78, 1.0);
            var white = new Rgb(1, 0.98, 0.95);
            var warm = new Rgb(1, 0.72, 0.45);
            return t < 0.45 ? Rgb.Lerp(cool, white, t / 0.45) : Rgb.Lerp(white, warm, (t - 0.45) / 0.55);
        }

        public static void EquirectUv(double raDeg, double decDeg, out double u, out double v)
        {
            double ra = raDeg % 360.0;
            if (ra < 0) ra += 360.0;
            u = ra / 360.0;
            v = SkyColor.Clamp01((decDeg + 90.0) / 180.0);
        }

        public static void EquirectTexel(double raDeg, double decDeg, int width, int height, out int x, out int y)
        {
            EquirectUv(raDeg, decDeg, out var u, out var v);
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            x = (int)Math.Floor(u * width) % width;
            if (x < 0) x += width;
            y = (int)Math.Round(v * (height - 1));
            if (y < 0) y = 0;
            if (y > height - 1) y = height - 1;
        }
    }
}
