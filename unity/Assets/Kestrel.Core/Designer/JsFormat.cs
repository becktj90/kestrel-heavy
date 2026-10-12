// Number.toFixed as JavaScript specifies it: exact integer rounding of the IEEE value,
// half away from zero. A decimal cast does not match V8 (1.005.toFixed(2) is "1.00").
using System;
using System.Globalization;
using System.Numerics;

namespace Kestrel.Core.Designer
{
    public static class JsFormat
    {
        public static string ToFixed(double value, int digits)
        {
            if (digits < 0 || digits > 100) throw new ArgumentOutOfRangeException(nameof(digits));
            if (double.IsNaN(value)) return "NaN";
            if (double.IsPositiveInfinity(value)) return "Infinity";
            if (double.IsNegativeInfinity(value)) return "-Infinity";
            bool neg = value < 0;
            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(value) & 0x7FFFFFFFFFFFFFFFUL;
            ulong sig = bits & 0xFFFFFFFFFFFFFUL;
            int biased = (int)((bits >> 52) & 0x7FF);
            int pow2;
            if (biased == 0) pow2 = -1074;
            else { sig |= 1UL << 52; pow2 = biased - 1075; }
            BigInteger scaled = new BigInteger(sig) * BigInteger.Pow(10, digits);
            BigInteger ip;
            if (pow2 >= 0) ip = scaled << pow2;
            else
            {
                BigInteger div = BigInteger.One << (-pow2);
                BigInteger q = BigInteger.DivRem(scaled, div, out var rem);
                if (rem * 2 >= div) q += 1;
                ip = q;
            }
            string text = ip.ToString(CultureInfo.InvariantCulture);
            if (text.Length <= digits) text = text.PadLeft(digits + 1, '0');
            if (digits == 0) return (neg ? "-" : "") + text;
            int cut = text.Length - digits;
            return (neg ? "-" : "") + text.Substring(0, cut) + "." + text.Substring(cut);
        }

        /// <summary>JS Number(toFixed(digits)), used when a rounded decimal is stored back on the design.</summary>
        public static double ToFixedNumber(double value, int digits)
            => double.Parse(ToFixed(value, digits), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
