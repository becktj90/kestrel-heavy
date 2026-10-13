// Asset-backed vehicle materials. Instanced draws (bells, bay parts) must use a Resources material whose
// shader already has INSTANCING_ON, the same fix as KH_Body: Shader.Find succeeds in the editor and the
// player then strips the variant, so the mesh never appears on device.
using UnityEngine;

namespace Kestrel.Vehicle
{
    public static class VehicleLook
    {
        public const string BodyResource = "Materials/KH_Body";
        public const string MetalResource = "Materials/KH_Metal";

        /// <summary>Painted body. Callers that tint per design clone this; the shader stays the asset's.</summary>
        public static Material Body() => Pin(Resources.Load<Material>(BodyResource));

        /// <summary>Bare metal for instanced bells and bay hardware. Shared asset, not a per-object clone.</summary>
        public static Material Metal()
        {
            var m = Pin(Resources.Load<Material>(MetalResource));
            return m != null ? m : Body();
        }

        public static Material Pin(Material m)
        {
            if (m != null) m.enableInstancing = true;
            return m;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void KeepDeviceVariants()
        {
            Pin(Resources.Load<Material>(BodyResource));
            Pin(Resources.Load<Material>(MetalResource));
        }
    }
}
