// One line for the home screen: flights cleared, bank, patches earned.
// The bank is the career Kc (kilocredits, data/campaign.json), not a distance. It is written the way
// CostModel.FormatKc writes Kc elsewhere ("8.88M Kc"), so a real career fits the phone stats card:
// "8,878,502 Kc" read like an odd unit and pushed the line onto a third row.
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public static class HomeRecord
    {
        public static string Line(int cleared, long bankMinor, long minorPerKc, int patches)
        {
            if (cleared < 0) cleared = 0;
            if (patches < 0) patches = 0;
            if (minorPerKc < 1) minorPerKc = 1;
            if (bankMinor < 0) bankMinor = 0;
            long kc = bankMinor / minorPerKc;
            string flights = cleared == 1 ? "1 flight cleared" : cleared.ToString(CultureInfo.InvariantCulture) + " flights cleared";
            string patch = patches == 1 ? "1 patch" : patches.ToString(CultureInfo.InvariantCulture) + " patches";
            return flights + " · " + Kc(kc) + " · " + patch;
        }

        /// <summary>"850 Kc", "34.1k Kc", "8.88M Kc", "1.25B Kc" (CostModel.FormatKc, plus billions).</summary>
        public static string Kc(long kc)
        {
            var ci = CultureInfo.InvariantCulture;
            double a = kc < 0 ? -(double)kc : kc;
            if (a >= 1e9) return (kc / 1e9).ToString("N2", ci) + "B Kc";
            if (a >= 1e6) return (kc / 1e6).ToString("N2", ci) + "M Kc";
            if (a >= 1e3) return (kc / 1e3).ToString("N1", ci) + "k Kc";
            return kc.ToString("N0", ci) + " Kc";
        }

        /// <summary>The same record for a narrow card: "2 cleared · 8.88M Kc · 2 patches".</summary>
        public static string Short(string line)
        {
            if (string.IsNullOrEmpty(line)) return line ?? "";
            return line.Replace(" flights cleared", " cleared").Replace(" flight cleared", " cleared");
        }
    }
}
