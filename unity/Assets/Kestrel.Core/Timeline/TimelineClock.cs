// T- compression. Ordering and relative spacing stay; game time shrinks the count.
// KID total count is about 32 s (inside the 20–40 s band). Engineer keeps real seconds.
// T+ times are not compressed: those come from the sim.
using System;

namespace Kestrel.Core.Timeline
{
    public static class TimelineClock
    {
        public const double RealHoldEarly = -7200; // T-2 h
        public const double RealHoldLate = -240;   // T-4 min
        public const double RealPropGo = -2280;    // T-38:00
        public const double RealFuelLox = -2100;   // T-35:00
        public const double RealUpperLox = -960;   // T-16:00
        public const double RealEngineChill = -420;// T-7:00
        public const double RealTankPress = -60;   // T-1:00
        public const double RealGoLaunch = -45;    // T-0:45
        public const double RealIgnition = -3;     // T-0:03

        public static string NormalizeMode(string mode)
        {
            if (string.Equals(mode, "kid", StringComparison.OrdinalIgnoreCase)) return "kid";
            if (string.Equals(mode, "engineer", StringComparison.OrdinalIgnoreCase)) return "engineer";
            return "standard";
        }

        /// <summary>Map a real countdown second (negative, liftoff = 0) into game seconds.</summary>
        public static double GameSeconds(double realSeconds, string mode)
        {
            if (realSeconds >= 0) return realSeconds;
            Spans(mode, out double terminalSpan, out double prelude);
            if (realSeconds >= RealHoldLate) return realSeconds / 240.0 * terminalSpan;
            double span = RealHoldLate - RealHoldEarly; // 6960
            double u = (realSeconds - RealHoldEarly) / span;
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            return -terminalSpan - prelude * (1 - u);
        }

        /// <summary>Inverse of <see cref="GameSeconds"/>. The pad contract is in real seconds; the player clock is not.</summary>
        public static double RealSeconds(double gameSeconds, string mode)
        {
            if (gameSeconds >= 0) return gameSeconds;
            Spans(mode, out double terminalSpan, out double prelude);
            if (NormalizeMode(mode) == "engineer") return gameSeconds;
            if (gameSeconds >= -terminalSpan) return gameSeconds / terminalSpan * 240.0;
            double u = (gameSeconds + terminalSpan + prelude) / prelude;
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            return RealHoldEarly + u * (RealHoldLate - RealHoldEarly);
        }

        static void Spans(string mode, out double terminalSpan, out double prelude)
        {
            mode = NormalizeMode(mode);
            if (mode == "kid") { terminalSpan = 24; prelude = 8; }
            else if (mode == "engineer") { terminalSpan = 240; prelude = 6960; }
            else { terminalSpan = 90; prelude = 45; }
        }

        public static double Round(double v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);

        public static string Clock(double t)
        {
            if (Math.Abs(t) < 0.0005) return "T+0:00";
            bool neg = t < 0;
            double a = Math.Abs(t);
            int msTotal = (int)Math.Round(a * 1000, MidpointRounding.AwayFromZero);
            int sec = msTotal / 1000;
            int frac = msTotal % 1000;
            int h = sec / 3600;
            int m = (sec % 3600) / 60;
            int s = sec % 60;
            string body = h > 0 ? h + ":" + m.ToString("00") + ":" + s.ToString("00") : m + ":" + s.ToString("00");
            if (frac != 0)
            {
                string f = frac.ToString("000");
                while (f.Length > 0 && f[f.Length - 1] == '0') f = f.Substring(0, f.Length - 1);
                body += "." + f;
            }
            return (neg ? "T-" : "T+") + body;
        }
    }
}
