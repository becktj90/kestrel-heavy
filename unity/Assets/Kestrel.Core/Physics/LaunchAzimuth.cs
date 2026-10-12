// Non-rotating spherical launch azimuth. sin(Az) = cos(i) / cos(φ).
// Az is degrees from north toward east. The southbound track is 180 − Az and is not returned.
// A WGS-84 site in ECI waits on Frames; this is the closed form only.
using System;

namespace Kestrel.Core.Physics
{
    public static class LaunchAzimuth
    {
        public static double FromNorthDeg(double inclinationDeg, double latitudeDeg)
        {
            double s = Math.Cos(inclinationDeg * Math.PI / 180) / Math.Cos(latitudeDeg * Math.PI / 180);
            if (s > 1 || s < -1) return double.NaN;
            return Math.Asin(s) * 180 / Math.PI;
        }
    }
}
