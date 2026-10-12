// JS Number.toString(36) for save ids: 'd' + Date.now().toString(36) + Math.random().toString(36).slice(2, 8).
// Terminating fractions are not padded: (0.5).toString(36).slice(2, 8) is "i", not "i00000".
using System;
using System.Text;

namespace Kestrel.Core.Save
{
    public static class JsRadix
    {
        const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        public static string Int36(long n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (n == 0) return "0";
            var sb = new StringBuilder();
            while (n > 0) { sb.Insert(0, Digits[(int)(n % 36)]); n /= 36; }
            return sb.ToString();
        }

        public static string Frac36(double x)
        {
            var sb = new StringBuilder();
            double f = x;
            for (int i = 0; i < 24; i++)
            {
                if (f == 0) break;
                f *= 36;
                int d = (int)Math.Floor(f + 1e-12);
                if (d < 0) d = 0;
                if (d > 35) d = 35;
                sb.Append(Digits[d]);
                f -= d;
                if (Math.Abs(f) < 1e-12) break;
            }
            return sb.ToString();
        }

        public static string NewId(long nowMs, double random)
        {
            string frac = Frac36(random);
            if (frac.Length > 6) frac = frac.Substring(0, 6);
            return "d" + Int36(nowMs) + frac;
        }
    }
}
