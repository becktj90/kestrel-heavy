// While the flight camera follows the booster (RecoveryShow.SteerCamera: boostback, entry, landing), the HUD's
// telemetry strip reads the booster, not the upper stage. Before: the #381 landing capture read S3's ascent numbers
// (T+491, 158.9 km, 4.53 km/s), a red Fair chip and BACK TO DESIGN while the booster hung 40 m over the Haven deck.
// Booster focus: ALT, UP/DOWN (vertical speed), FUEL (landing propellant left) and TO DECK (or TO PAD for a
// return-to-launch-site landing). The upper stage's health tabs, its stage chip and BACK TO DESIGN are hidden
// (visibility only, so nothing in the layout moves) and come back when the camera returns or the flight ends.
// Every number is read from the recovery sample BoosterLanding already steps; nothing here is new physics.
// Static text and colours, no blink or pulse (photosensitivity / Reduce Motion safe). No Unity types; AscentHud converts.
using System;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public enum FlightHudFocus { Ascent = 0, Booster = 1 }

    /// <summary>Colour slot of a booster cell, the same palette as the ascent strip (ink, amber, green).</summary>
    public enum BoosterHudTone { Ink = 0, Amber = 1, Green = 2 }

    public struct BoosterHudCell
    {
        /// <summary>Short key over the value (ALT, DOWN, FUEL, TO DECK).</summary>
        public string Key;
        /// <summary>The value as drawn ("40 m", "12 m/s", "35%").</summary>
        public string Value;
        /// <summary>The cell read out in words, for the tooltip (the project's VoiceOver pattern).</summary>
        public string Spoken;
        public BoosterHudTone Tone;
    }

    public struct BoosterHudReadout
    {
        public BoosterHudCell Alt, Vertical, Fuel, Target;

        public BoosterHudCell this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return Alt;
                    case 1: return Vertical;
                    case 2: return Fuel;
                    case 3: return Target;
                    default: throw new ArgumentOutOfRangeException(nameof(i));
                }
            }
        }

        public const int Count = 4;
    }

    public static class BoosterHud
    {
        public const string AltKey = "ALT";
        public const string DownKey = "DOWN";
        public const string UpKey = "UP";
        public const string FuelKey = "FUEL";
        public const string DeckKey = "TO DECK";
        public const string PadKey = "TO PAD";
        /// <summary>Fuel at or under this share of the landing reserve draws amber (static, never blinks).</summary>
        public const double LowFuel01 = 0.15;

        /// <summary>The HUD reads the booster exactly while the camera is steered onto it.</summary>
        public static FlightHudFocus Focus(bool cameraOnBooster) => cameraOnBooster ? FlightHudFocus.Booster : FlightHudFocus.Ascent;

        /// <summary>The upper stage's own chrome (health tabs, stage chip, BACK TO DESIGN) shows only in ascent focus.</summary>
        public static bool ShowsAscentChrome(FlightHudFocus focus) => focus == FlightHudFocus.Ascent;

        /// <summary>The landing target's key: the Haven deck at sea, the pad for a return-to-launch-site landing.</summary>
        public static string TargetKey(string mode) => mode == "rtls" ? PadKey : DeckKey;

        /// <summary>Share of the landing reserve still aboard, 0..1 (0 when the craft carried no reserve).</summary>
        public static double Fuel01(double propKg, double reserveKg)
        {
            if (!(reserveKg > 0) || double.IsNaN(propKg)) return 0;
            double f = propKg / reserveKg;
            return f < 0 ? 0 : f > 1 ? 1 : f;
        }

        public static BoosterHudReadout Read(in RecoverySample s, in RecoveryCraft craft)
        {
            var inv = CultureInfo.InvariantCulture;
            double alt = Math.Max(0, s.Alt);
            // Radial is the signed vertical speed (+ up); the sample's Sink is its falling half only.
            double vUp = s.Radial;
            bool rising = vUp > 0.5;
            double vAbs = Math.Abs(vUp);
            double fuel = Fuel01(s.Prop, craft.ReserveKg);
            int fuelPct = (int)Math.Round(fuel * 100.0, MidpointRounding.AwayFromZero);
            double miss = Math.Abs(s.MissM);
            bool pad = craft.Mode == "rtls";
            string place = pad ? "pad" : "deck";
            return new BoosterHudReadout
            {
                Alt = new BoosterHudCell
                {
                    Key = AltKey,
                    Value = Distance(alt, inv),
                    Spoken = "Booster height " + SpokenDistance(alt, inv) + ".",
                    Tone = BoosterHudTone.Ink,
                },
                Vertical = new BoosterHudCell
                {
                    Key = rising ? UpKey : DownKey,
                    Value = Speed(vAbs, inv),
                    Spoken = "Booster " + (rising ? "going up " : "coming down ") + SpokenSpeed(vAbs, inv) + ".",
                    Tone = BoosterHudTone.Ink,
                },
                Fuel = new BoosterHudCell
                {
                    Key = FuelKey,
                    Value = fuelPct.ToString(inv) + "%",
                    Spoken = "Landing fuel " + fuelPct.ToString(inv) + " percent left.",
                    Tone = fuel <= LowFuel01 ? BoosterHudTone.Amber : BoosterHudTone.Green,
                },
                Target = new BoosterHudCell
                {
                    Key = TargetKey(craft.Mode),
                    Value = Distance(miss, inv),
                    Spoken = (pad ? "Landing pad " : "Landing deck ") + SpokenDistance(miss, inv) + " away.",
                    Tone = BoosterHudTone.Amber,
                },
            };
        }

        /// <summary>Metres under 1 km ("40 m"), then kilometres to one place ("12.3 km"), as the ascent ALT reads.</summary>
        public static string Distance(double m, IFormatProvider inv)
            => m < 999.5 ? m.ToString("0", inv) + " m" : (m / 1000.0).ToString("0.0", inv) + " km";

        /// <summary>m/s under 1 km/s, then km/s to two places, the ascent SPEED format.</summary>
        public static string Speed(double mps, IFormatProvider inv)
            => mps < 999.5 ? mps.ToString("0", inv) + " m/s" : (mps / 1000.0).ToString("0.00", inv) + " km/s";

        static string SpokenDistance(double m, IFormatProvider inv)
            => m < 999.5 ? m.ToString("0", inv) + " metres" : (m / 1000.0).ToString("0.0", inv) + " kilometres";

        static string SpokenSpeed(double mps, IFormatProvider inv) => mps.ToString("0", inv) + " metres a second";
    }
}
