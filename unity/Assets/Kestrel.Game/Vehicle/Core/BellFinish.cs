// Fuel cue on the engine bells (Starter preview: kerosene vs methane must read at a glance). Original,
// procedural finishes, no textures: a sooty charcoal bell for kerosene (kerolox gas generators coat the nozzle),
// clean bright steel for methane, and a cool blue-steel tint for hydrogen. Other fuels (solids, hybrids,
// storables) keep the shared KH_Metal look. Engine-free so dotnet and EditMode both test it; BellBuilder turns
// a finish into one shared, instanced material per key, so bells of a fuel stay one batch (WebGL-safe).
namespace Kestrel.Vehicle
{
    public struct BellTint
    {
        public float R, G, B, Metallic, Smoothness;
        /// <summary>Rec. 709 relative luminance of the base colour (linear 0..1 inputs).</summary>
        public float Luminance => 0.2126f * R + 0.7152f * G + 0.0722f * B;
    }

    public static class BellFinish
    {
        public const string Sooty = "sooty", Clean = "clean", Hydrogen = "hydrogen";

        /// <summary>Finish key for a catalog propellant id. Empty: the shared metal (no tint).</summary>
        public static string For(string propellant)
        {
            switch (propellant)
            {
                case "kerolox": return Sooty;
                case "methalox": return Clean;
                case "hydrolox": return Hydrogen;
                default: return "";
            }
        }

        public static bool TryTint(string key, out BellTint t)
        {
            switch (key)
            {
                case Sooty: t = new BellTint { R = 0.13f, G = 0.12f, B = 0.115f, Metallic = 0.45f, Smoothness = 0.22f }; return true;
                case Clean: t = new BellTint { R = 0.70f, G = 0.72f, B = 0.75f, Metallic = 0.92f, Smoothness = 0.74f }; return true;
                case Hydrogen: t = new BellTint { R = 0.55f, G = 0.64f, B = 0.80f, Metallic = 0.88f, Smoothness = 0.66f }; return true;
                default: t = default; return false;
            }
        }
    }
}
