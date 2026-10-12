// Helpers that reproduce JavaScript Math semantics where .NET differs, so ports match the
// JS reference bit-for-bit wherever the platform libm agrees.
using System;

namespace Kestrel.Core.Physics
{
    public static class JsMath
    {
        /// <summary>V8's Math.hypot (max-scaled, Kahan-compensated), 2 args.</summary>
        public static double Hypot(double a, double b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            if (double.IsInfinity(a) || double.IsInfinity(b)) return double.PositiveInfinity;
            if (double.IsNaN(a) || double.IsNaN(b)) return double.NaN;
            double max = a > b ? a : b;
            if (max == 0) return 0;
            double sum = 0, comp = 0;
            double n = a / max, summand = n * n - comp, pre = sum + summand; comp = (pre - sum) - summand; sum = pre;
            n = b / max; summand = n * n - comp; pre = sum + summand; comp = (pre - sum) - summand; sum = pre;
            return Math.Sqrt(sum) * max;
        }

        /// <summary>JS Math.round: round half toward +infinity.</summary>
        public static double Round(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return x;
            double r = Math.Floor(x);
            return x - r >= 0.5 ? r + 1 : r;
        }
    }
}
