// Sensor fidelity. Exact in KID and Sandbox, including when the designer layer is Standard.
// Light noise is one mulberry32 draw per channel, mapped to [-1, 1). Mass and propellant stay exact.
using System;

namespace Kestrel.Core.Flight
{
    public static class Sensors
    {
        public const double AltitudeRel = 0.0015, SpeedRel = 0.002, AngleDeg = 0.15;

        public static string Fidelity(string difficulty, bool sandbox, string layer)
        {
            if (sandbox || difficulty == "KID") return "exact";
            if (layer == "standard") return "light-noise";
            return "exact";
        }

        public static double Mulberry32(ref uint state)
        {
            state = unchecked(state + 0x6D2B79F5);
            uint a = state;
            uint t = Imul(a ^ (a >> 15), 1u | a);
            t = unchecked(t + Imul(t ^ (t >> 7), 61u | t)) ^ t;
            t ^= t >> 14;
            return t / 4294967296.0;
        }

        static uint Imul(uint x, uint y) => unchecked((uint)((int)x * (int)y));

        public struct Reading
        {
            public string mode;
            public double altitude, speed, angleDeg, mass, propellant;
            public bool hasMass, hasPropellant;
        }

        public static Reading Sense(double altitude, double speed, double angleDeg, string difficulty, bool sandbox, string layer, uint seed, double? mass = null, double? propellant = null)
        {
            var r = new Reading { mode = Fidelity(difficulty, sandbox, layer), altitude = altitude, speed = speed, angleDeg = angleDeg };
            if (mass.HasValue) { r.hasMass = true; r.mass = mass.Value; }
            if (propellant.HasValue) { r.hasPropellant = true; r.propellant = propellant.Value; }
            if (r.mode != "light-noise") return r;
            uint state = seed;
            double nAlt = Mulberry32(ref state) * 2 - 1, nSpd = Mulberry32(ref state) * 2 - 1, nAng = Mulberry32(ref state) * 2 - 1;
            r.altitude = altitude * (1 + AltitudeRel * nAlt);
            r.speed = speed * (1 + SpeedRel * nSpd);
            r.angleDeg = angleDeg + AngleDeg * nAng;
            return r;
        }
    }
}
