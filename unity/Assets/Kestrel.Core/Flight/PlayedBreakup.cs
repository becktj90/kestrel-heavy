// When the played flight should come apart. The headless golden stays on its own path:
// a design that does not engage FailureMonitor still flies the same steps there.
// Here, a stack that cannot lift, is crushed by the air, or is short of the trip
// breaks up so the live view can explode it.
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public static class PlayedBreakup
    {
        /// <summary>
        /// Stock Kestrel Heavy peaks near 73 kPa on a 35 kPa reference limit.
        /// 2.4 keeps that flight whole. A weaker safety factor or material crosses it.
        /// </summary>
        public const double QFactor = 2.4;
        /// <summary>Stock's thrust-over-mass reaches about 18 g at first-stage burnout. A harder push snaps the stack.</summary>
        public const double GCap = 20;
        public const double PadHoldS = 2;
        public const double PadAltM = 5;

        /// <summary>Arcade and older designs. Version 3 already has the strict monitor.</summary>
        public static bool Arcade(Design d) => d != null && !FailureMonitor.Engages(d);

        public struct Sample
        {
            public double T, AltM, QPa, AccelMps2, QLimPa, LiftoffTwr;
            public bool Thrusting;
        }

        /// <summary>Ideal Δv fraction, (total − required) / required. Negative means the design cannot make the trip.</summary>
        public static double Margin(double idealMps, double requiredMps)
            => requiredMps > 1 ? (idealMps - requiredMps) / requiredMps : 0;

        /// <summary>A load or a pad failure, or null while the flight is still sound.</summary>
        public static FailureEvent At(in Sample s)
        {
            if (s.LiftoffTwr > 0 && s.LiftoffTwr < 1 && s.Thrusting && s.T >= PadHoldS && s.AltM < PadAltM)
            {
                return Make(FailureKind.StructuralMaxQ, "margin", s.T, s.LiftoffTwr, 1,
                    "The rocket is too heavy to lift. It tears itself apart on the pad. Take off propellant or add engines.",
                    "liftoff TWR " + s.LiftoffTwr.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " < 1.");
            }
            double qBreak = s.QLimPa * QFactor;
            if (s.QLimPa > 0 && s.QPa > qBreak)
            {
                return Make(FailureKind.StructuralMaxQ, "q", s.T, s.QPa, qBreak,
                    "Your rocket bent in half at T+" + N0(s.T) + " s. The air pushed " + N0(s.QPa / 1000.0)
                        + " kPa, past what this structure can fly through (" + N0(qBreak / 1000.0)
                        + " kPa). Raise the safety factor or throttle back at max-Q.",
                    "structural-maxq: q " + s.QPa.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                        + " Pa > " + qBreak.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " Pa.");
            }
            double g = s.AccelMps2 / FailureModel.G0;
            if (g > GCap)
            {
                return Make(FailureKind.OverG, "overg", s.T, g, GCap,
                    "The push snapped the stack at T+" + N0(s.T) + " s. It hit " + N1(g) + " g, past " + N1(GCap)
                        + " g. Fewer engines, or more mass under them.",
                    "overg: " + g.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " g > " + GCap.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " g.");
            }
            return null;
        }

        /// <summary>The vehicle came back down before the flight was done.</summary>
        public static FailureEvent Impact(double t)
            => Make(FailureKind.StructuralMaxQ, "margin", t, 0, 0,
                "It fell back and broke apart at T+" + N0(t) + " s. It never had the push to stay in the air.",
                "impact at T+" + t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s.");

        /// <summary>Ideal Δv is short, so burnout is a breakup. A design with speed to spare returns null.</summary>
        public static FailureEvent Short(double t, double marginFraction)
        {
            if (!(marginFraction < 0)) return null;
            return Make(FailureKind.StructuralMaxQ, "margin", t, marginFraction, 0,
                "Not enough speed for this trip. The stack comes apart at burnout. Add propellant or lighten the cargo.",
                "dv margin " + marginFraction.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " < 0 at burnout.");
        }

        static FailureEvent Make(int kind, string cause, double t, double value, double limit, string kid, string engineer)
            => new FailureEvent
            {
                t = t,
                kind = FailureKind.Name(kind),
                cause = cause,
                stage = 1,
                engineIndex = -1,
                value = value,
                limit = limit,
                kid = kid,
                engineer = engineer,
                fixHint = kind == FailureKind.OverG ? "fewer engines" : "raise SF, more propellant, or a lighter payload",
            };

        static string N0(double x) => System.Math.Round(x).ToString(System.Globalization.CultureInfo.InvariantCulture);
        static string N1(double x) => x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
