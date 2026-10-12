// Live sun elevation for anything that should follow the sky without a scene reference.
// Pad lighting (PadLighting.NightFactor) takes degrees and ramps across civil twilight.
// It reads this accessor; the sky does not reference the pad.
using System;

namespace Kestrel.Core.Sky
{
    public static class SkySun
    {
        /// <summary>
        /// Sun elevation in degrees above the horizon. Updated by the sky each frame.
        /// Pad hookup, unchanged here: <c>Night = PadLighting.NightFactor(SkySun.ElevationDeg)</c>.
        /// Before the first sample this is a clear afternoon (42°), so night factor is 0.
        /// </summary>
        public static float ElevationDeg { get; private set; } = 42f;

        /// <summary>Same angle in radians. Asin of the local up-component.</summary>
        public static double ElevationRad => ElevationDeg * (Math.PI / 180.0);

        /// <summary>Publish an elevation in radians (the ephemeris result).</summary>
        public static void Publish(double elevationRad)
        {
            if (double.IsNaN(elevationRad) || double.IsInfinity(elevationRad)) return;
            double deg = elevationRad * (180.0 / Math.PI);
            if (deg > 90) deg = 90;
            if (deg < -90) deg = -90;
            ElevationDeg = (float)deg;
        }

        /// <summary>
        /// 0 in daylight (sun at or above +6°), 1 at night (at or below −6°).
        /// Same civil-twilight ramp as PadLighting.NightFactor(sunElevationDeg).
        /// </summary>
        public static float NightFactor(float sunElevationDeg)
        {
            double x = (6.0 - sunElevationDeg) / 12.0;
            if (double.IsNaN(x)) return 0f;
            if (x < 0) return 0f;
            if (x > 1) return 1f;
            return (float)x;
        }

        public static float NightFactor() => NightFactor(ElevationDeg);
    }
}
