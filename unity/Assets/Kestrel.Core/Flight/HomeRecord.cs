// One line for the home screen: flights cleared, bank, patches earned.
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
            return flights + " · " + kc.ToString("N0", CultureInfo.InvariantCulture) + " Kc · " + patch;
        }
    }
}
