// Hanging hose / cable shape (engine-free). y(x) = a cosh((x - x0)/a) + c through two endpoints with a given
// arc length. Solved for a by bisection; deterministic, no allocations besides the output.
using System;

namespace Kestrel.Kit
{
    public static class Catenary
    {
        /// <summary>Sample a catenary in the vertical plane through p0 and p1 (each as (horizontal, vertical)).
        /// length must exceed the chord; it is clamped to chord * 1.0001 otherwise. Returns n+1 points.</summary>
        public static void Sample2D(double h0, double v0, double h1, double v1, double length, int n, double[] outH, double[] outV)
        {
            if (n < 1) throw new ArgumentOutOfRangeException(nameof(n));
            double dh = h1 - h0, dv = v1 - v0, chord = Math.Sqrt(dh * dh + dv * dv);
            length = Math.Max(length, chord * 1.0001);
            if (Math.Abs(dh) < 1e-6)
            {   // vertical drop: hose folds straight down then up; approximate as a straight line
                for (int i = 0; i <= n; i++) { double t = (double)i / n; outH[i] = h0; outV[i] = v0 + dv * t; }
                return;
            }
            double sign = Math.Sign(dh); double w = Math.Abs(dh);
            // sqrt(L^2 - dv^2) = 2a sinh(w / 2a)  -> solve for a
            double target = Math.Sqrt(length * length - dv * dv);
            double lo = 1e-4, hi = 1e6;
            for (int it = 0; it < 200; it++)
            {
                double a = 0.5 * (lo + hi);
                double f = 2 * a * Math.Sinh(w / (2 * a));
                if (double.IsInfinity(f) || f > target) lo = a; else hi = a;
            }
            double A = 0.5 * (lo + hi);
            // x offset so the curve passes both endpoints (local x from 0..w)
            double x0 = w / 2 - A * Atanh(dv / length);
            double c = v0 - A * Math.Cosh((0 - x0) / A);
            for (int i = 0; i <= n; i++)
            {
                double x = w * i / n;
                outH[i] = h0 + sign * x;
                outV[i] = A * Math.Cosh((x - x0) / A) + c;
            }
            outV[0] = v0; outV[n] = v1;
        }

        /// <summary>Lowest vertical value of the sampled curve (sag check).</summary>
        public static double MinV(double[] v, int count) { double m = double.MaxValue; for (int i = 0; i < count; i++) m = Math.Min(m, v[i]); return m; }

        static double Atanh(double x) => 0.5 * Math.Log((1 + x) / (1 - x));
    }
}
