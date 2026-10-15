// Reynolds steering used by the breakup: shards separate, and a guide can arrive on a point
// or stay off a corridor wall. Accelerations are in the caller's units per second.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Feel
{
    public static class Steering
    {
        public static void Seek(double x, double y, double vx, double vy, double tx, double ty, double maxSpeed, out double ax, out double ay)
        {
            double dx = tx - x, dy = ty - y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-6) { ax = -vx; ay = -vy; return; }
            double sp = maxSpeed < 0 ? 0 : maxSpeed;
            ax = dx / d * sp - vx;
            ay = dy / d * sp - vy;
        }

        public static void Arrive(double x, double y, double vx, double vy, double tx, double ty, double maxSpeed, double slowRadius, out double ax, out double ay)
        {
            double dx = tx - x, dy = ty - y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-6) { ax = -vx; ay = -vy; return; }
            double sp = maxSpeed < 0 ? 0 : maxSpeed;
            if (slowRadius > 1e-6 && d < slowRadius) sp *= d / slowRadius;
            ax = dx / d * sp - vx;
            ay = dy / d * sp - vy;
        }

        /// <summary>Push away from neighbours inside a few radii. <paramref name="self"/> is skipped.</summary>
        public static void Separate(double x, double y, double radius, Shard[] shards, List<int> neighbors, int self, out double ax, out double ay)
        {
            ax = 0; ay = 0;
            if (shards == null || neighbors == null) return;
            int n = 0;
            double reach = Math.Max(0.1, radius) * 4;
            for (int k = 0; k < neighbors.Count; k++)
            {
                int j = neighbors[k];
                if (j == self || j < 0 || j >= shards.Length || !shards[j].Alive) continue;
                double dx = x - shards[j].X, dy = y - shards[j].Y;
                double d2 = dx * dx + dy * dy;
                double lim = reach + shards[j].Radius;
                if (d2 > lim * lim || d2 < 1e-8) continue;
                double d = Math.Sqrt(d2);
                ax += dx / d / d;
                ay += dy / d / d;
                n++;
            }
            if (n > 0) { ax /= n; ay /= n; }
            double mag = Math.Sqrt(ax * ax + ay * ay);
            if (mag > 40) { ax *= 40 / mag; ay *= 40 / mag; }
        }

        /// <summary>Lateral push that grows as x nears either wall at ±halfWidth.</summary>
        public static double AvoidWalls(double x, double halfWidth, double margin)
        {
            if (margin <= 0 || halfWidth <= 0) return 0;
            double ax = 0;
            double left = x + halfWidth;
            double right = halfWidth - x;
            if (left < margin) ax += (margin - left) / margin;
            if (right < margin) ax -= (margin - right) / margin;
            return ax;
        }

        public static void Blend(double ax, double ay, double bx, double by, double wa, double wb, out double ox, out double oy)
        {
            double w = wa + wb;
            if (w < 1e-8 || double.IsNaN(w)) { ox = 0; oy = 0; return; }
            ox = (ax * wa + bx * wb) / w;
            oy = (ay * wa + by * wb) / w;
        }
    }
}
