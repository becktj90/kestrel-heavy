// Compact Kc for the report card (#416 item 3): "2.0M", "4.8K", "10". Whole Kc under a thousand,
// one decimal with K or M above. Rounds half away from zero in decimal, so 999,950 is "1.0M", never "1000.0K".
using System;
using System.Globalization;

namespace Kestrel.Core.Economy
{
    public static class KcFormat
    {
        /// <summary>"2.0M", "4.8K", "999", "-4.8K", "0". NaN and infinities read "0".</summary>
        public static string Compact(double kc)
        {
            if (double.IsNaN(kc) || double.IsInfinity(kc)) return "0";
            decimal a = (decimal)Math.Min(Math.Abs(kc), 7.9e27);
            string sign = kc < 0 ? "-" : "";
            decimal whole = Math.Round(a, 0, MidpointRounding.AwayFromZero);
            if (whole == 0m) return "0";
            if (whole < 1000m) return sign + whole.ToString("0", CultureInfo.InvariantCulture);
            decimal k = Math.Round(a / 1000m, 1, MidpointRounding.AwayFromZero);
            if (k < 1000m) return sign + k.ToString("0.0", CultureInfo.InvariantCulture) + "K";
            decimal m = Math.Round(a / 1000000m, 1, MidpointRounding.AwayFromZero);
            return sign + m.ToString("0.0", CultureInfo.InvariantCulture) + "M";
        }

        /// <summary>Compact with a leading "+" on gains: "+2.0M", "-4.8K", "0".</summary>
        public static string Signed(double kc)
        {
            string s = Compact(kc);
            return kc > 0 && s != "0" ? "+" + s : s;
        }
    }
}
