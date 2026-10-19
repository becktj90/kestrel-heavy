// Fuel family drives the flame and, when a stage is loaded with a different propellant than
// the engine was tabulated for, the thrust and Isp the ascent sim burns. Tank mass already
// follows bulk density (DesignMath.SizeTank, Mass.StructuralFractionMass). A matched pair
// (kerolox engine, kerolox load) is left on the catalog bell so the goldens do not move.
using Kestrel.Core.Data;

namespace Kestrel.Core.Flight
{
    public enum FuelFamily
    {
        Kerolox = 0,
        Methalox = 1,
        Hydrolox = 2,
        Solid = 3,
        Hypergol = 4,
        Hybrid = 5,
    }

    /// <summary>Linear RGB 0..1 plus the shape multipliers on the kerolox plume curves.</summary>
    public struct PropellantFlame
    {
        public FuelFamily Family;
        public string Id;
        public double CoreR, CoreG, CoreB;
        public double MidR, MidG, MidB;
        public double TailR, TailG, TailB;
        public double HeadR, HeadG, HeadB;
        public double Soot;
        public double LengthScale, WidthScale, AlphaScale;
    }

    public struct PropellantBell
    {
        public double ThrustSL, ThrustVac, IspSL, IspVac;
        public bool Scaled;
    }

    public static class PropellantFlight
    {
        public static FuelFamily FamilyOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return FuelFamily.Kerolox;
            if (id == "solid" || id == "solid-apcp" || id.IndexOf("solid", System.StringComparison.Ordinal) >= 0) return FuelFamily.Solid;
            if (id == "hydrolox" || id.IndexOf("hydrolox", System.StringComparison.Ordinal) >= 0 || id.IndexOf("lh2", System.StringComparison.Ordinal) >= 0) return FuelFamily.Hydrolox;
            if (id == "methalox" || id.IndexOf("methalox", System.StringComparison.Ordinal) >= 0 || id.IndexOf("ch4", System.StringComparison.Ordinal) >= 0) return FuelFamily.Methalox;
            if (id == "hybrid") return FuelFamily.Hybrid;
            if (id == "mmh-nto" || id == "udmh-nto" || id.IndexOf("nto", System.StringComparison.Ordinal) >= 0) return FuelFamily.Hypergol;
            return FuelFamily.Kerolox;
        }

        /// <summary>VISUAL_SPEC §3.4. Kerolox matches the plume that already ships.</summary>
        public static PropellantFlame Flame(string id)
        {
            var family = FamilyOf(id);
            var f = new PropellantFlame
            {
                Family = family,
                Id = string.IsNullOrEmpty(id) ? "kerolox" : id,
                LengthScale = 1,
                WidthScale = 1,
                AlphaScale = 1,
            };
            switch (family)
            {
                case FuelFamily.Methalox:
                    Paint(ref f, 0xe8f0ff, 0xa0c8ff, 0xff8a50, 0xd6e4ff);
                    f.Soot = 0.05; f.LengthScale = 1.08; f.WidthScale = 0.90; f.AlphaScale = 0.85;
                    break;
                case FuelFamily.Hydrolox:
                    Paint(ref f, 0xf0f8ff, 0xc8d8ff, 0x80a0ff, 0xe4eeff);
                    f.Soot = 0; f.LengthScale = 1.22; f.WidthScale = 1.28; f.AlphaScale = 0.40;
                    break;
                case FuelFamily.Solid:
                    Paint(ref f, 0xffffff, 0xfff0c0, 0xe0d8c8, 0xfff6e0);
                    f.Soot = 0.60; f.LengthScale = 0.72; f.WidthScale = 1.45; f.AlphaScale = 0.95;
                    break;
                case FuelFamily.Hypergol:
                    Paint(ref f, 0xfff0d0, 0xffb070, 0xc86040, 0xffd0a0);
                    f.Soot = 0.10; f.LengthScale = 0.95; f.WidthScale = 0.92; f.AlphaScale = 0.9;
                    break;
                case FuelFamily.Hybrid:
                    Paint(ref f, 0xfff0d0, 0xffa040, 0xd06030, 0xffc878);
                    f.Soot = 0.40; f.LengthScale = 0.88; f.WidthScale = 1.15; f.AlphaScale = 0.9;
                    break;
                default:
                    Paint(ref f, 0xfff4d8, 0xffb040, 0xff6a1a, 0xffd27a);
                    f.Soot = 0.35; f.LengthScale = 1; f.WidthScale = 1; f.AlphaScale = 1;
                    break;
            }
            return f;
        }

        /// <summary>
        /// Same mass flow, Isp (and therefore thrust) follows the loaded propellant.
        /// The catalog bell is the answer when the load matches the engine's propellant.
        /// </summary>
        public static PropellantBell Scale(Propellant loaded, Propellant native, double thrustSL, double thrustVac, double ispSL, double ispVac)
        {
            var bell = new PropellantBell { ThrustSL = thrustSL, ThrustVac = thrustVac, IspSL = ispSL, IspVac = ispVac, Scaled = false };
            if (loaded == null || native == null) return bell;
            if (string.Equals(loaded.id, native.id, System.StringComparison.Ordinal)) return bell;
            if (!(native.ispVac > 1) || !(loaded.ispVac > 1) || !(ispVac > 1)) return bell;
            double k = loaded.ispVac / native.ispVac;
            if (double.IsNaN(k) || double.IsInfinity(k) || k <= 0) return bell;
            bell.IspVac = ispVac * k;
            bell.IspSL = ispSL > 0 ? ispSL * k : 0;
            bell.ThrustVac = thrustVac * k;
            bell.ThrustSL = thrustSL > 0 ? thrustSL * k : 0;
            bell.Scaled = true;
            return bell;
        }

        /// <summary>Structural mass grows as the tank grows. Lower bulk density means a longer barrel.</summary>
        public static double StructureScale(Propellant prop)
        {
            if (prop == null || !(prop.bulkDensity > 1)) return 1;
            if (prop.storability == "solid") return 1;
            double s = System.Math.Sqrt(1030.0 / prop.bulkDensity);
            if (double.IsNaN(s) || s <= 0) return 1;
            return s;
        }

        static void Paint(ref PropellantFlame f, int core, int mid, int tail, int head)
        {
            Split(core, out f.CoreR, out f.CoreG, out f.CoreB);
            Split(mid, out f.MidR, out f.MidG, out f.MidB);
            Split(tail, out f.TailR, out f.TailG, out f.TailB);
            Split(head, out f.HeadR, out f.HeadG, out f.HeadB);
        }

        static void Split(int hex, out double r, out double g, out double b)
        {
            r = ((hex >> 16) & 255) / 255.0;
            g = ((hex >> 8) & 255) / 255.0;
            b = (hex & 255) / 255.0;
        }
    }
}
