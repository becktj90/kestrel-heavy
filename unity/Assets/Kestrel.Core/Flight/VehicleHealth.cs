// Compact vehicle health for the flight HUD. Five parts, each green, amber, or red,
// from the live sim: heat, dynamic pressure, fuel, g-load, and engine state.
// No Unity types. The same sample always returns the same tones and sentences.
using System;

namespace Kestrel.Core.Flight
{
    public enum VehicleHealthTone
    {
        Green = 0,
        Amber = 1,
        Red = 2,
    }

    public struct VehicleHealthInput
    {
        public double QPa;
        public double QLimitPa;
        public double Fuel01;
        public double GLoad;
        public double GLimit;
        public double Thrust01;
        public bool ExpectThrust;
        public bool EngineOut;
        public bool Lost;
        public string LossKind;
        public bool HasFairing;
        public bool FairingOff;
        public bool PayloadHot;
    }

    public sealed class VehicleHealthPart
    {
        public string Id;
        public string Mark;
        public string Name;
        public VehicleHealthTone Tone;
        public string Detail;
    }

    public static class VehicleHealth
    {
        public const int Count = 5;
        public const double HeatAmber = 0.85;
        public const double FuelAmber = 0.22;
        public const double FuelRed = 0.05;
        public const double GAmber = 0.75;

        public static VehicleHealthPart[] Read(VehicleHealthInput s)
        {
            string loss = s.LossKind ?? "";
            double heat = s.QLimitPa > 1 ? s.QPa / s.QLimitPa : 0;
            if (heat < 0 || double.IsNaN(heat) || double.IsInfinity(heat)) heat = 0;
            return new[]
            {
                Engines(s, loss),
                Tanks(s, loss),
                Structure(s, loss, heat),
                Guidance(s, loss),
                Fairing(s, loss, heat),
            };
        }

        static VehicleHealthPart Engines(VehicleHealthInput s, string loss)
        {
            bool failed = s.EngineOut || Loss(loss, "engine");
            if (failed)
                return Part("engines", "Eng", "Engines", VehicleHealthTone.Red, "An engine is out.");
            if (s.ExpectThrust && s.Thrust01 < 0.35 && !s.Lost)
                return Part("engines", "Eng", "Engines", VehicleHealthTone.Amber, "Engines are quiet.");
            if (s.Thrust01 >= 0.35)
                return Part("engines", "Eng", "Engines", VehicleHealthTone.Green, "Engines are lit.");
            return Part("engines", "Eng", "Engines", VehicleHealthTone.Green, "Engines are ready.");
        }

        static VehicleHealthPart Tanks(VehicleHealthInput s, string loss)
        {
            if (Loss(loss, "prop"))
                return Part("tanks", "Tnk", "Tanks", VehicleHealthTone.Red, "The tanks have run dry.");
            if (double.IsNaN(s.Fuel01))
                return Part("tanks", "Tnk", "Tanks", VehicleHealthTone.Green, "Tanks are coasting.");
            if (s.Fuel01 < FuelRed)
                return Part("tanks", "Tnk", "Tanks", VehicleHealthTone.Red, "Tanks are nearly empty.");
            if (s.Fuel01 < FuelAmber)
                return Part("tanks", "Tnk", "Tanks", VehicleHealthTone.Amber, "Tanks are getting low.");
            return Part("tanks", "Tnk", "Tanks", VehicleHealthTone.Green, "Tanks are above a fifth.");
        }

        static VehicleHealthPart Structure(VehicleHealthInput s, string loss, double heat)
        {
            if (Loss(loss, "structural") || heat >= 1)
                return Part("structure", "Hull", "Structure", VehicleHealthTone.Red, "The stack is past its Q limit.");
            if (heat >= HeatAmber)
                return Part("structure", "Hull", "Structure", VehicleHealthTone.Amber, "Heating is near the Q limit.");
            return Part("structure", "Hull", "Structure", VehicleHealthTone.Green, "Structure is inside the Q limit.");
        }

        static VehicleHealthPart Guidance(VehicleHealthInput s, string loss)
        {
            bool over = s.GLimit > 0.1 && s.GLoad > s.GLimit;
            if (Loss(loss, "overg") || Loss(loss, "tumble") || over)
                return Part("guidance", "Guide", "Guidance", VehicleHealthTone.Red, "g-load is past the limit.");
            if (s.GLimit > 0.1 && s.GLoad > s.GLimit * GAmber)
                return Part("guidance", "Guide", "Guidance", VehicleHealthTone.Amber, "g-load is near the limit.");
            return Part("guidance", "Guide", "Guidance", VehicleHealthTone.Green, "Guidance is inside the g limit.");
        }

        static VehicleHealthPart Fairing(VehicleHealthInput s, string loss, double heat)
        {
            if (s.PayloadHot || Loss(loss, "thermal"))
                return Part("fairing", "Fair", "Fairing", VehicleHealthTone.Red, "Payload heat is past the limit.");
            if (!s.HasFairing)
                return Part("fairing", "Fair", "Fairing", VehicleHealthTone.Green, "This rocket has no fairing.");
            if (s.FairingOff)
                return Part("fairing", "Fair", "Fairing", VehicleHealthTone.Green, "Fairing is off.");
            if (heat >= HeatAmber)
                return Part("fairing", "Fair", "Fairing", VehicleHealthTone.Amber, "The fairing is taking heat.");
            return Part("fairing", "Fair", "Fairing", VehicleHealthTone.Green, "Fairing is on and cool.");
        }

        static bool Loss(string loss, string token) =>
            loss.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        static VehicleHealthPart Part(string id, string mark, string name, VehicleHealthTone tone, string detail) =>
            new VehicleHealthPart { Id = id, Mark = mark, Name = name, Tone = tone, Detail = detail };
    }
}
