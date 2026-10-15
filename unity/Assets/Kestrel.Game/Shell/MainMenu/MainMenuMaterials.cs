// Materials cloned from the Resources library so the player build keeps the URP variants
// (KH_Body opaque lit, KH_LitEmission, KH_Plume additive, KH_Glass transparent lit).
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class MainMenuMaterials
    {
        public const string BodyResource = "Materials/KH_Body";
        public const string EmissionResource = "Materials/KH_LitEmission";
        public const string GlowResource = "Materials/KH_Plume";
        public const string GlassResource = "Materials/KH_Glass";
        public const string TmpResource = "UI/KH_MenuTmp";
        public const string FontResource = "Materials/KH_Font";

        public static readonly Color Cyan = new Color(0.25f, 0.95f, 1f, 1f);
        public static readonly Color LaunchRed = new Color(0.72f, 0.04f, 0.09f, 1f);
        public static readonly Color Cream = new Color(0.96f, 0.94f, 0.88f, 1f);
        public static readonly Color GlassTint = new Color(0.62f, 0.82f, 0.92f, 0.32f);
        public static readonly Color Bay = new Color(0.04f, 0.05f, 0.07f, 1f);

        public static Material Body(Color color, float metallic, float smoothness, Color emission)
        {
            var m = Clone(Resources.Load<Material>(BodyResource));
            if (m == null) return null;
            Tint(m, color, metallic, smoothness, emission);
            return m;
        }

        public static Material Lamp(Color emission)
        {
            var m = Clone(Resources.Load<Material>(EmissionResource)) ?? Body(Color.white, 0f, 0.4f, emission);
            if (m == null) return null;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.SetColor("_BaseColor", Color.white);
            return m;
        }

        public static Material Additive(Color color)
        {
            var m = Clone(Resources.Load<Material>(GlowResource));
            if (m == null) m = Body(color, 0f, 0.2f, color);
            if (m == null) return null;
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color);
            return m;
        }

        public static Material Glass(Color tint, float glow)
        {
            var m = Clone(Resources.Load<Material>(GlassResource)) ?? Clone(Resources.Load<Material>(BodyResource));
            if (m == null) return null;
            m.SetColor("_BaseColor", tint);
            m.SetColor("_Color", tint);
            m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_Metallic", 0.05f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Cyan * glow);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.renderQueue = 3000;
            return m;
        }

        /// <summary>Unlit font cutout. Cloned so each label can tint without fighting the others.
        /// The Resources material keeps Kestrel/Font in the player; Shader.Find alone would not.</summary>
        public static Material Font()
        {
            var src = Resources.Load<Material>(FontResource);
            Material m = null;
            if (src != null && Usable(src.shader)) m = new Material(src) { name = "KH_Font_Menu" };
            if (m == null)
            {
                var sh = Shader.Find("Kestrel/Font");
                if (!Usable(sh)) return null;
                m = new Material(sh) { name = "KH_Font_Menu" };
            }
            m.renderQueue = 3100;
            m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            return m;
        }

        public static Color InkFor(MenuTextRole role, bool live)
        {
            switch (role)
            {
                case MenuTextRole.Launch: return Color.white;
                case MenuTextRole.Hud: return new Color(0.93f, 0.98f, 1f, 1f);
                case MenuTextRole.Subtitle: return new Color(0.78f, 0.86f, 0.92f, 1f);
                case MenuTextRole.Dial: return Cyan;
                case MenuTextRole.Glass:
                    return live ? Cream : new Color(0.82f, 0.86f, 0.90f, 0.72f);
                default: return Cream;
            }
        }

        public static Material TmpShaderSource()
        {
            var pinned = Resources.Load<Material>(TmpResource);
            if (pinned != null && Usable(pinned.shader)) return pinned;
            return null;
        }

        public static Shader TmpShader()
        {
            var pinned = TmpShaderSource();
            if (pinned != null) return pinned.shader;
            var sh = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (!Usable(sh)) sh = Shader.Find("TextMeshPro/Distance Field");
            return Usable(sh) ? sh : null;
        }

        public static Material Clone(Material src)
        {
            if (src == null || !Usable(src.shader)) return null;
            var m = new Material(src) { name = src.name + "_Menu" };
            return m;
        }

        public static void Tint(Material m, Color color, float metallic, float smoothness, Color emission)
        {
            if (m == null) return;
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
        }

        public static bool Usable(Shader sh)
        {
            if (sh == null) return false;
            string n = sh.name;
            if (string.IsNullOrEmpty(n)) return true;
            return n.IndexOf("InternalError", System.StringComparison.OrdinalIgnoreCase) < 0
                && n.IndexOf("FallbackError", System.StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
