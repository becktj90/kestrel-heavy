// What a flight event should feel like, and how the airframe should read on the HUD.
// Numbers stay honest: a hot meter is live dynamic pressure against the design's own max-Q,
// not a fake danger bar.
using System;

namespace Kestrel.Core.Feel
{
    public struct FlightCue
    {
        public double Shake, Flash, HapticMs, Rumble, Punch;
        public bool Hard;
    }

    public static class FlightCues
    {
        public static FlightCue For(string name, bool reduced)
        {
            double shake = 0, flash = 0, haptic = 0, rumble = 0, punch = 0;
            bool hard = false;
            switch (name)
            {
                case "liftoff": shake = 0.55; flash = 0.70; haptic = 28; rumble = 0.80; punch = 0.80; break;
                case "maxq": shake = 0.72; flash = 0.25; haptic = 18; rumble = 1.00; punch = 0.45; break;
                case "stage": shake = 0.40; flash = 0.35; haptic = 14; rumble = 0.50; punch = 0.60; break;
                case "meco": shake = 0.22; flash = 0.10; haptic = 10; rumble = 0.20; punch = 0.30; break;
                case "hit": shake = 0.85; flash = 1.00; haptic = 22; rumble = 0.40; punch = 0.50; hard = true; break;
                case "breakup": shake = 1.00; flash = 1.00; haptic = 40; rumble = 0.30; punch = 1.00; hard = true; break;
            }
            if (reduced)
            {
                shake = 0;
                flash *= 0.25;
                haptic = 0;
                punch *= 0.2;
                hard = false;
            }
            return new FlightCue { Shake = shake, Flash = flash, HapticMs = haptic, Rumble = rumble, Punch = punch, Hard = hard };
        }
    }

    public static class AirframeBand
    {
        /// <summary>nominal, warm, scorched, critical, or lost. The 0.34 line matches the arcade integrity warn.</summary>
        public static string Of(double integrity)
        {
            if (double.IsNaN(integrity) || integrity <= 0) return "lost";
            if (integrity < 0.34) return "critical";
            if (integrity < 0.62) return "scorched";
            if (integrity < 0.85) return "warm";
            return "nominal";
        }

        public static void Tint(string band, out double r, out double g, out double b)
        {
            switch (band)
            {
                case "lost": r = 0.35; g = 0.32; b = 0.30; return;
                case "critical": r = 1.00; g = 0.42; b = 0.28; return;
                case "scorched": r = 1.00; g = 0.72; b = 0.38; return;
                case "warm": r = 1.00; g = 0.92; b = 0.72; return;
                default: r = 1; g = 1; b = 1; return;
            }
        }

        /// <summary>1 is a healthy stack. Dynamic pressure only starts to spend it once q passes 72% of the limit.</summary>
        public static double Integrity(double qPa, double qLimPa, double health01)
        {
            double q = 1;
            if (qLimPa > 1 && !double.IsNaN(qPa))
            {
                q = 1 - Math.Max(0, qPa / qLimPa - 0.72) / 0.7;
                if (q < 0) q = 0;
                if (q > 1) q = 1;
            }
            double h = health01;
            if (double.IsNaN(h)) h = 1;
            if (h < 0) h = 0;
            if (h > 1) h = 1;
            return q * h;
        }
    }

    public struct RiskReadout
    {
        public double Integrity, Energy, QRisk, Margin;
        public string Band, Call, Line;
    }

    public static class HudRisk
    {
        /// <summary>Hot when live q is 85% of the design max-Q, the same mark the webcast heat warning uses.</summary>
        public const double Hot = 0.85;

        public static RiskReadout Evaluate(double health01, double throttle01, double fuel01, double qPa, double qLimPa, double dvMargin)
        {
            double qRisk = 0;
            if (qLimPa > 1 && !double.IsNaN(qPa)) qRisk = qPa / qLimPa;
            if (qRisk < 0 || double.IsNaN(qRisk)) qRisk = 0;
            if (qRisk > 1.5) qRisk = 1.5;
            double thr = Clamp01(throttle01), fuel = Clamp01(fuel01);
            double energy = thr * fuel;
            double integ = AirframeBand.Integrity(qPa, qLimPa, health01);
            string band = AirframeBand.Of(integ);
            string call, line;
            if (band == "critical" || band == "lost") { call = "AIRFRAME"; line = "AIRFRAME — load is high"; }
            else if (qRisk >= Hot) { call = "MAX-Q"; line = "MAX-Q — the air is heavy"; }
            else if (!double.IsNaN(dvMargin) && dvMargin < 0) { call = "SHORT"; line = "SHORT — light on speed for this trip"; }
            else if (energy < 0.12 && fuel < 0.2) { call = "PROP"; line = "PROP — tanks are thin"; }
            else { call = "NOMINAL"; line = ""; }
            return new RiskReadout
            {
                Integrity = integ, Energy = energy, QRisk = qRisk, Margin = dvMargin,
                Band = band, Call = call, Line = line,
            };
        }

        static double Clamp01(double v)
        {
            if (double.IsNaN(v) || v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }
    }
}
