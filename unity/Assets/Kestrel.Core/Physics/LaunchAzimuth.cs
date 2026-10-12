// Launch azimuth from north toward east, degrees.
// Inertial: sin Az = cos i / cos φ on a non-rotating sphere. Northerly is arcsin; southerly is 180° − that.
// Rotating: ground heading of a horizontal inertial velocity after the eastward site speed ω r cos φ is removed.
// |cos i| > |cos φ| throws. The result is never NaN.
using System;

namespace Kestrel.Core.Physics
{
    public static class LaunchAzimuth
    {
        public static double InertialNortherlyDeg(double inclinationDeg, double latitudeDeg)
            => Math.Asin(InertialSine(inclinationDeg, latitudeDeg)) * 180.0 / Math.PI;

        public static double InertialSoutherlyDeg(double inclinationDeg, double latitudeDeg)
        {
            double north = InertialNortherlyDeg(inclinationDeg, latitudeDeg);
            double south = 180.0 - north;
            if (south >= 360.0) south -= 360.0;
            if (south < 0) south += 360.0;
            return south;
        }

        /// <summary>
        /// Ground heading, degrees, for inertial speed <paramref name="inertialSpeed"/> at radius <paramref name="radius"/>.
        /// The inertial direction is the northerly branch.
        /// </summary>
        public static double RotatingNortherlyDeg(double inclinationDeg, double latitudeDeg, double inertialSpeed, double radius)
            => RotatingDeg(InertialNortherlyDeg(inclinationDeg, latitudeDeg), latitudeDeg, inertialSpeed, radius);

        /// <summary>Ground heading for the southerly inertial branch.</summary>
        public static double RotatingSoutherlyDeg(double inclinationDeg, double latitudeDeg, double inertialSpeed, double radius)
            => RotatingDeg(InertialSoutherlyDeg(inclinationDeg, latitudeDeg), latitudeDeg, inertialSpeed, radius);

        static double InertialSine(double inclinationDeg, double latitudeDeg)
        {
            double cphi = Math.Cos(latitudeDeg * Math.PI / 180.0);
            if (Math.Abs(cphi) < 1e-10)
                throw new ArgumentOutOfRangeException(nameof(latitudeDeg), "azimuth is undefined at a pole");
            double s = Math.Cos(inclinationDeg * Math.PI / 180.0) / cphi;
            if (s > 1.0)
            {
                if (s <= 1.0 + 1e-12) return 1.0;
                throw new ArgumentOutOfRangeException(nameof(inclinationDeg), "inclination is unreachable from this latitude");
            }
            if (s < -1.0)
            {
                if (s >= -1.0 - 1e-12) return -1.0;
                throw new ArgumentOutOfRangeException(nameof(inclinationDeg), "inclination is unreachable from this latitude");
            }
            return s;
        }

        static double RotatingDeg(double inertialAzDeg, double latitudeDeg, double inertialSpeed, double radius)
        {
            if (inertialSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(inertialSpeed));
            if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            double beta = inertialAzDeg * Math.PI / 180.0;
            double vNorth = inertialSpeed * Math.Cos(beta);
            double vEast = inertialSpeed * Math.Sin(beta) - Earth.OMEGA_EARTH * radius * Math.Cos(latitudeDeg * Math.PI / 180.0);
            double deg = Math.Atan2(vEast, vNorth) * 180.0 / Math.PI;
            if (deg < 0) deg += 360.0;
            return deg;
        }
    }
}
