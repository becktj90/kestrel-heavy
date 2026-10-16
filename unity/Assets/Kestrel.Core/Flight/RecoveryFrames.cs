// Tangent-plane frame at the pad. x is east, y is up from the pad plane.
// Geometric altitude is the distance from Earth's centre, so a long downrange
// does not fly through the curve.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class RecoveryFrames
    {
        public static void FromPad(double downrangeM, double altM, double vEast, double vUp,
            out double x, out double y, out double vx, out double vy)
        {
            double theta = downrangeM / Gravity.R_EARTH;
            double r = Gravity.R_EARTH + (altM > -Gravity.R_EARTH * 0.5 ? altM : 0);
            double s = Math.Sin(theta), c = Math.Cos(theta);
            x = r * s;
            y = r * c - Gravity.R_EARTH;
            vx = vEast * c + vUp * s;
            vy = -vEast * s + vUp * c;
        }

        public static void Axes(double x, double y, out double r, out double alt, out double arc,
            out double eastX, out double eastY, out double upX, out double upY)
        {
            double cx = x, cy = Gravity.R_EARTH + y;
            r = Math.Sqrt(cx * cx + cy * cy);
            if (r < 1) r = 1;
            alt = r - Gravity.R_EARTH;
            arc = Math.Atan2(cx, cy) * Gravity.R_EARTH;
            upX = cx / r;
            upY = cy / r;
            eastX = cy / r;
            eastY = -cx / r;
        }

        public static void Split(double x, double y, double vx, double vy, out double east, out double radial)
        {
            Axes(x, y, out _, out _, out _, out double ex, out double ey, out double ux, out double uy);
            east = vx * ex + vy * ey;
            radial = vx * ux + vy * uy;
        }

        /// <summary>Vacuum time from altitude to the surface under constant g. The positive root.</summary>
        public static double FallTime(double alt, double vUp, double g)
        {
            if (!(g > 0.1) || double.IsNaN(g)) g = Atmosphere.G0;
            if (alt < 0 || double.IsNaN(alt)) alt = 0;
            if (double.IsNaN(vUp)) vUp = 0;
            double disc = vUp * vUp + 2 * g * alt;
            if (disc < 0) disc = 0;
            return (vUp + Math.Sqrt(disc)) / g;
        }

        public static double Wrap(double rad)
        {
            if (double.IsNaN(rad)) return 0;
            while (rad > Math.PI) rad -= 2 * Math.PI;
            while (rad < -Math.PI) rad += 2 * Math.PI;
            return rad;
        }
    }
}
