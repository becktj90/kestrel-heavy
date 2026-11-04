// One place that makes the particle / glow materials for explosions, shockwave rings, debris sparks and failure bursts.
//
// Why it exists: every one of those used to do Shader.Find("Universal Render Pipeline/Particles/Unlit"), then Sprites/Default, then
// return null, and the callers either left the renderer's material unset or assigned null. An unset renderer draws the error shader,
// which is flat MAGENTA (the huge disc Trevor saw on TestFlight). Shader.Find also succeeds in the editor for every imported shader
// while the player only keeps the variants a shipped asset asks for (see MaterialLibrary).
//
// Rules here:
//   1. Prefer a clone of the Resources template (Materials/KH_FxParticle), which pins the exact variant into the player.
//   2. Check every shader is Usable (non-null, supported, not an error shader) before using it.
//   3. If nothing usable exists return null, and Apply() then DISABLES the renderer: a missing effect, never magenta.
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Kit
{
    public static class FxMaterials
    {
        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        public const string TemplateResource = "Materials/KH_FxParticle";

        /// <summary>Touch the template so the player keeps the particle shader variant the explosion materials use.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void KeepDeviceVariants() { _ = Resources.Load<Material>(TemplateResource); }

        /// <summary>A shader that can draw: not null, supported on this GPU, and not one of Unity's error shaders.</summary>
        public static bool Usable(Shader sh)
        {
            if (sh == null) return false;
            string n = sh.name;
            if (!string.IsNullOrEmpty(n) &&
                (n.IndexOf("InternalError", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("FallbackError", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("Hidden/InternalErrorShader", System.StringComparison.OrdinalIgnoreCase) >= 0)) return false;
            return sh.isSupported;
        }

        /// <summary>A material that will not draw magenta: non-null and a Usable shader.</summary>
        public static bool IsSafe(Material m) => m != null && Usable(m.shader);

        /// <summary>True when the renderer has at least one material and every material is safe.</summary>
        public static bool IsSafe(Renderer r)
        {
            if (r == null) return false;
            var ms = r.sharedMaterials;
            if (ms == null || ms.Length == 0) return false;
            for (int i = 0; i < ms.Length; i++) if (!IsSafe(ms[i])) return false;
            return true;
        }

        /// <summary>Assign <paramref name="m"/>; with no safe material the renderer is switched off instead of drawing the error material.</summary>
        public static void Apply(Renderer r, Material m)
        {
            if (r == null) return;
            if (IsSafe(m)) { r.sharedMaterial = m; r.enabled = true; }
            else r.enabled = false;
        }

        /// <summary>Material for a soft particle texture. <paramref name="additive"/> adds light; otherwise it is alpha blended (smoke).</summary>
        public static Material Particle(Texture tex, string name, bool additive, bool doubleSided = false, int queueOffset = 0)
        {
            Material mat = null;
            var tmpl = Resources.Load<Material>(TemplateResource);
            if (tmpl != null && Usable(tmpl.shader)) mat = new Material(tmpl);
            if (mat == null)
            {
                var sh = Shader.Find(ParticleShader);
                if (!Usable(sh)) sh = Shader.Find("Sprites/Default");
                if (!Usable(sh)) sh = Shader.Find("Unlit/Transparent");
                if (!Usable(sh)) return null;
                mat = new Material(sh);
            }
            mat.name = name;
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_ZWrite", 0f);
            if (doubleSided) mat.SetFloat("_Cull", 0f);
            if (additive)
            {
                mat.SetFloat("_Blend", 2f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.One);
            }
            else
            {
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_SOFTPARTICLES_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent + queueOffset;
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            return mat;
        }
    }
}
