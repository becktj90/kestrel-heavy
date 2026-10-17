// Procedural payload shapes. Boxes only, so the card painter and the URP mesh share one list.
// Colors are the hangar palette: navy, cream, orange. No photos and no paid meshes.
using System.Collections.Generic;

namespace Kestrel.Core.Flight
{
    public struct PayloadPart
    {
        public float X, Y, Z, W, H, D;
        public byte R, G, B;

        public PayloadPart(float x, float y, float z, float w, float h, float d, byte r, byte g, byte b)
        {
            X = x; Y = y; Z = z; W = w; H = h; D = d; R = r; G = g; B = b;
        }
    }

    public static class PayloadForms
    {
        public const byte NavyR = 30, NavyG = 46, NavyB = 72;
        public const byte CreamR = 236, CreamG = 226, CreamB = 204;
        public const byte OrangeR = 214, OrangeG = 122, OrangeB = 48;
        public const byte SteelR = 168, SteelG = 178, SteelB = 190;
        public const byte GlassR = 126, GlassG = 186, GlassB = 206;
        public const byte GoldR = 196, GoldG = 154, GoldB = 72;
        public const byte InkR = 16, InkG = 20, InkB = 28;

        static readonly string[] Known =
        {
            "box", "cubesat", "bus", "weather", "comms", "telescope",
            "probe", "cargo", "crew", "dispenser", "relay", "lander",
        };

        public static IReadOnlyList<string> Ids => Known;

        public static bool KnownForm(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < Known.Length; i++)
                if (Known[i] == id) return true;
            return false;
        }

        public static PayloadPart[] Of(string id)
        {
            switch (id)
            {
                case "cubesat": return Cubesat();
                case "bus": return Bus();
                case "weather": return Weather();
                case "comms": return Comms();
                case "telescope": return Telescope();
                case "probe": return Probe();
                case "cargo": return Cargo();
                case "crew": return Crew();
                case "dispenser": return Dispenser();
                case "relay": return Relay();
                case "lander": return Lander();
                default: return Box();
            }
        }

        static PayloadPart P(float x, float y, float z, float w, float h, float d, byte r, byte g, byte b)
            => new PayloadPart(x, y, z, w, h, d, r, g, b);

        static PayloadPart[] Box() => new[]
        {
            P(0, 0.15f, 0, 0.92f, 0.62f, 0.72f, CreamR, CreamG, CreamB),
            P(0, 0.28f, 0, 0.94f, 0.12f, 0.74f, OrangeR, OrangeG, OrangeB),
            P(0, 0.58f, 0, 0.08f, 0.42f, 0.08f, SteelR, SteelG, SteelB),
            P(0, 0.82f, 0, 0.22f, 0.06f, 0.06f, OrangeR, OrangeG, OrangeB),
        };

        static PayloadPart[] Cubesat() => new[]
        {
            P(0, 0.2f, 0, 0.42f, 0.42f, 0.42f, GoldR, GoldG, GoldB),
            P(-0.46f, 0.2f, 0, 0.5f, 0.36f, 0.08f, NavyR, NavyG, NavyB),
            P(0.46f, 0.2f, 0, 0.5f, 0.36f, 0.08f, NavyR, NavyG, NavyB),
            P(0, 0.52f, 0, 0.05f, 0.36f, 0.05f, SteelR, SteelG, SteelB),
            P(0, 0.72f, 0, 0.16f, 0.04f, 0.04f, OrangeR, OrangeG, OrangeB),
        };

        static PayloadPart[] Bus() => new[]
        {
            P(0, 0.16f, 0, 0.7f, 0.46f, 0.7f, NavyR, NavyG, NavyB),
            P(0, 0.42f, 0, 0.5f, 0.08f, 0.5f, CreamR, CreamG, CreamB),
            P(-0.85f, 0.16f, 0, 0.9f, 0.08f, 0.42f, GlassR, GlassG, GlassB),
            P(0.85f, 0.16f, 0, 0.9f, 0.08f, 0.42f, GlassR, GlassG, GlassB),
            P(0, 0.48f, 0.1f, 0.06f, 0.28f, 0.06f, SteelR, SteelG, SteelB),
        };

        static PayloadPart[] Weather() => new[]
        {
            P(0, 0.12f, 0, 0.62f, 0.4f, 0.62f, NavyR, NavyG, NavyB),
            P(0, 0.4f, 0, 0.36f, 0.28f, 0.36f, CreamR, CreamG, CreamB),
            P(0, 0.62f, 0, 0.5f, 0.1f, 0.5f, OrangeR, OrangeG, OrangeB),
            P(-0.78f, 0.12f, 0, 0.85f, 0.06f, 0.38f, GlassR, GlassG, GlassB),
            P(0.78f, 0.12f, 0, 0.85f, 0.06f, 0.38f, GlassR, GlassG, GlassB),
            P(0, 0.28f, 0.48f, 0.28f, 0.28f, 0.08f, GoldR, GoldG, GoldB),
        };

        static PayloadPart[] Comms() => new[]
        {
            P(0, 0.1f, -0.05f, 0.55f, 0.4f, 0.55f, NavyR, NavyG, NavyB),
            P(-1.05f, 0.1f, -0.05f, 1.15f, 0.05f, 0.36f, GlassR, GlassG, GlassB),
            P(1.05f, 0.1f, -0.05f, 1.15f, 0.05f, 0.36f, GlassR, GlassG, GlassB),
            P(0, 0.22f, 0.55f, 0.62f, 0.62f, 0.08f, GoldR, GoldG, GoldB),
            P(0, 0.22f, 0.62f, 0.16f, 0.16f, 0.06f, OrangeR, OrangeG, OrangeB),
        };

        static PayloadPart[] Telescope() => new[]
        {
            P(0, 0.16f, 0, 0.38f, 0.38f, 1.35f, CreamR, CreamG, CreamB),
            P(0, 0.16f, 0.78f, 0.62f, 0.62f, 0.16f, NavyR, NavyG, NavyB),
            P(0, 0.16f, -0.72f, 0.28f, 0.28f, 0.2f, SteelR, SteelG, SteelB),
            P(-0.55f, 0.16f, -0.15f, 0.55f, 0.05f, 0.28f, GlassR, GlassG, GlassB),
            P(0.55f, 0.16f, -0.15f, 0.55f, 0.05f, 0.28f, GlassR, GlassG, GlassB),
            P(0, 0.4f, 0.1f, 0.16f, 0.08f, 0.3f, OrangeR, OrangeG, OrangeB),
        };

        static PayloadPart[] Probe() => new[]
        {
            P(0, 0.14f, -0.1f, 0.5f, 0.36f, 0.5f, NavyR, NavyG, NavyB),
            P(0, 0.2f, 0.48f, 0.7f, 0.7f, 0.08f, GoldR, GoldG, GoldB),
            P(0, 0.2f, 0.55f, 0.18f, 0.18f, 0.06f, OrangeR, OrangeG, OrangeB),
            P(0.42f, 0.14f, -0.35f, 0.55f, 0.08f, 0.08f, SteelR, SteelG, SteelB),
            P(0.72f, 0.14f, -0.35f, 0.16f, 0.16f, 0.16f, CreamR, CreamG, CreamB),
            P(-0.42f, 0.3f, -0.1f, 0.28f, 0.08f, 0.22f, GlassR, GlassG, GlassB),
        };

        static PayloadPart[] Cargo() => new[]
        {
            P(0, -0.15f, 0, 0.7f, 0.55f, 0.7f, SteelR, SteelG, SteelB),
            P(0, 0.28f, 0, 0.78f, 0.32f, 0.78f, CreamR, CreamG, CreamB),
            P(0, 0.55f, 0, 0.55f, 0.24f, 0.55f, CreamR, CreamG, CreamB),
            P(0, 0.76f, 0, 0.28f, 0.16f, 0.28f, OrangeR, OrangeG, OrangeB),
            P(0, 0.05f, 0.4f, 0.22f, 0.16f, 0.04f, NavyR, NavyG, NavyB),
        };

        static PayloadPart[] Crew() => new[]
        {
            P(0, -0.05f, 0, 0.78f, 0.42f, 0.78f, CreamR, CreamG, CreamB),
            P(0, 0.32f, 0, 0.7f, 0.36f, 0.7f, CreamR, CreamG, CreamB),
            P(0, 0.58f, 0, 0.4f, 0.2f, 0.4f, NavyR, NavyG, NavyB),
            P(0, 0.82f, 0, 0.08f, 0.34f, 0.08f, SteelR, SteelG, SteelB),
            P(0, 1.02f, 0, 0.22f, 0.08f, 0.22f, OrangeR, OrangeG, OrangeB),
            P(0, 0.38f, 0.36f, 0.28f, 0.1f, 0.04f, GlassR, GlassG, GlassB),
        };

        static PayloadPart[] Dispenser() => new[]
        {
            P(0, 0.08f, 0, 0.46f, 0.28f, 1.45f, NavyR, NavyG, NavyB),
            P(0, 0.28f, -0.48f, 0.28f, 0.22f, 0.28f, GoldR, GoldG, GoldB),
            P(0, 0.28f, -0.16f, 0.28f, 0.22f, 0.28f, GoldR, GoldG, GoldB),
            P(0, 0.28f, 0.16f, 0.28f, 0.22f, 0.28f, GoldR, GoldG, GoldB),
            P(0, 0.28f, 0.48f, 0.28f, 0.22f, 0.28f, GoldR, GoldG, GoldB),
            P(-0.32f, 0.08f, 0, 0.08f, 0.08f, 1.4f, OrangeR, OrangeG, OrangeB),
            P(0.32f, 0.08f, 0, 0.08f, 0.08f, 1.4f, OrangeR, OrangeG, OrangeB),
        };

        static PayloadPart[] Relay() => new[]
        {
            P(0, 0.05f, -0.15f, 0.48f, 0.36f, 0.48f, NavyR, NavyG, NavyB),
            P(-1.15f, 0.05f, -0.15f, 1.3f, 0.05f, 0.32f, GlassR, GlassG, GlassB),
            P(1.15f, 0.05f, -0.15f, 1.3f, 0.05f, 0.32f, GlassR, GlassG, GlassB),
            P(0, 0.18f, 0.62f, 1.05f, 1.05f, 0.08f, GoldR, GoldG, GoldB),
            P(0, 0.18f, 0.7f, 0.22f, 0.22f, 0.06f, OrangeR, OrangeG, OrangeB),
            P(0, 0.32f, -0.15f, 0.16f, 0.12f, 0.16f, CreamR, CreamG, CreamB),
        };

        static PayloadPart[] Lander() => new[]
        {
            P(0, 0.22f, 0, 0.7f, 0.16f, 0.7f, CreamR, CreamG, CreamB),
            P(0, 0.42f, 0, 0.36f, 0.28f, 0.36f, NavyR, NavyG, NavyB),
            P(0, 0.08f, 0, 0.22f, 0.16f, 0.22f, SteelR, SteelG, SteelB),
            P(-0.48f, 0.02f, -0.48f, 0.08f, 0.42f, 0.08f, OrangeR, OrangeG, OrangeB),
            P(0.48f, 0.02f, -0.48f, 0.08f, 0.42f, 0.08f, OrangeR, OrangeG, OrangeB),
            P(-0.48f, 0.02f, 0.48f, 0.08f, 0.42f, 0.08f, OrangeR, OrangeG, OrangeB),
            P(0.48f, 0.02f, 0.48f, 0.08f, 0.42f, 0.08f, OrangeR, OrangeG, OrangeB),
            P(0, 0.62f, 0, 0.28f, 0.08f, 0.28f, GoldR, GoldG, GoldB),
        };
    }
}
