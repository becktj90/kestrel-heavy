// Shared fairing silhouette (engine-free, so dotnet tests and the Unity mesh builder use the same numbers).
//
// Physics' spec (docs/VEHICLE_ENGINEERING_RULES.md "Fairing joint and flare", DECISIONS #71): the fairing's JOINT diameter equals the top
// stage's diameter. fairing.diameter stays the BARREL diameter (the widest part when the fairing flares; a narrower fairing tapers in).
// A CONICAL shoulder joins the joint to the barrel. Its length is min(dR / tan 15 deg, 0.5 x cylinder length), carved out of the fairing's
// own cylinder, so the fairing's total length, the stack height and the CG do not change. The shoulder is part of the fairing mesh, so it
// jettisons with the halves. Like Falcon 9 (5.2 m on 3.66 m) or Atlas V (5.4 m on 3.81 m).
//
// Profile, bottom to top, radius R at height Y above the joint:
//   (0,0) -> (rJoint,0)          base disc, same radius as the stage top
//   (rBarrel, Lsh)               the cone (absent when the diameters match)
//   (rBarrel, cylinder)          barrel
//   nose (ogive / conic / von Karman) to the tip (0, length)
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public static class FairingShoulder
    {
        public readonly struct Station
        {
            public readonly float R, Y;
            public Station(float r, float y) { R = r; Y = y; }
        }

        /// <summary>Default shoulder half-angle: attached flow at 10 deg and lowest afterbody drag near 14 deg (Tran 2019), so 15 deg.</summary>
        public const float DefaultHalfAngleDeg = 15f;
        /// <summary>The shoulder never takes more than this share of the cylinder, so a short fairing keeps a barrel.</summary>
        public const float MaxCylinderShare = 0.5f;
        /// <summary>Radius difference below which the fairing is simply flush with the stage.</summary>
        public const float FlushTolerance = 1e-3f;
        const int NoseSteps = 10;

        /// <summary>Nose share of the fairing length, by aero.nose.</summary>
        public static float NoseFraction(string nose) => nose == "blunt" ? 0.25f : nose == "conic" ? 0.4f : nose == "von-karman" ? 0.5f : 0.45f;

        /// <summary>Nose radius at t (0 = shoulder, 1 = tip). Von Karman is the LD-Haack series (C = 0).</summary>
        public static float NoseRadius(string nose, float rf, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            if (nose == "conic") return rf * (1 - t);
            if (nose == "von-karman")
            {
                double x = 1.0 - 2.0 * (1.0 - t);
                double th = Math.Acos(x < -1.0 ? -1.0 : x > 1.0 ? 1.0 : x);
                return rf * (float)Math.Sqrt(Math.Max(0.0, (th - Math.Sin(2.0 * th) * 0.5) / Math.PI));
            }
            return rf * (float)Math.Sqrt(Math.Max(0.0, 1.0 - (double)t * t));   // tangent ogive approximation
        }

        /// <summary>Shoulder length L_sh = min(dR / tan 15 deg, 0.5 x cylinder length), for a nose taking <paramref name="noseFraction"/> of the length. 0 when the diameters match.</summary>
        public static float Height(float rJoint, float rFairing, float length, float noseFraction)
        {
            float dr = Math.Abs(rFairing - rJoint);
            if (!(dr > FlushTolerance) || !(length > 0f)) return 0f;
            float cylinder = length * (1f - noseFraction);
            float tan15 = (float)Math.Tan(DefaultHalfAngleDeg * Math.PI / 180.0);
            return Math.Min(dr / tan15, cylinder * MaxCylinderShare);
        }

        public static float Height(float rJoint, float rFairing, float length, string nose) => Height(rJoint, rFairing, length, NoseFraction(nose));

        /// <summary>Half-angle of the shoulder cone in degrees (15 unless the 0.5-cylinder limit bites, then steeper). 0 when flush.</summary>
        public static float HalfAngleDeg(float rJoint, float rFairing, float length, float noseFraction)
        {
            float hs = Height(rJoint, rFairing, length, noseFraction);
            return hs > 0f ? (float)(Math.Atan2(Math.Abs(rFairing - rJoint), hs) * 180.0 / Math.PI) : 0f;
        }

        /// <summary>Radius on the shoulder cone at height h above the joint (straight line). Past the shoulder it is the barrel radius.</summary>
        public static float ShoulderRadius(float rJoint, float rFairing, float shoulderHeight, float h)
        {
            if (!(shoulderHeight > 0f)) return h <= 0f ? rJoint : rFairing;
            if (h <= 0f) return rJoint;
            if (h >= shoulderHeight) return rFairing;
            return rJoint + (rFairing - rJoint) * (h / shoulderHeight);
        }

        /// <summary>Body radius at height h above the joint (the base of the fairing), 0 above the tip.</summary>
        public static float RadiusAt(float rJoint, float rFairing, float length, string nose, float h)
        {
            if (h <= 0f) return rJoint;
            if (h >= length) return 0f;
            float noseFrac = NoseFraction(nose);
            float hs = Height(rJoint, rFairing, length, noseFrac);
            float barrel = length * (1f - noseFrac);
            if (h <= barrel) return ShoulderRadius(rJoint, rFairing, hs, h);
            return NoseRadius(nose, rFairing, (h - barrel) / (length - barrel));
        }

        /// <summary>The lathe profile, Y measured from the joint. Add the joint height to Y to place it on the vehicle.</summary>
        public static List<Station> Profile(float rJoint, float rFairing, float length, string nose)
        {
            float noseFrac = NoseFraction(nose);
            float barrel = length * (1f - noseFrac), noseLen = length * noseFrac;
            float hs = Height(rJoint, rFairing, length, nose);
            var p = new List<Station> { new Station(0f, 0f), new Station(rJoint, 0f) };
            if (hs > 0f) p.Add(new Station(rFairing, hs));
            p.Add(new Station(rFairing, barrel));
            for (int k = 1; k <= NoseSteps; k++)
            {
                float t = (float)k / NoseSteps;
                p.Add(new Station(k == NoseSteps ? 0f : NoseRadius(nose, rFairing, t), barrel + noseLen * t));
            }
            return p;
        }
    }
}
