// Linear fog weight shared by the coast shaders (KH_Terrain, KH_Water) and tests.
// The shaders do not use multi_compile_fog: that permutation is stripped from player builds
// because no scene has fog enabled at build time, and SkyRig only turns fog on at runtime.
// A stripped FOG_LINEAR variant leaves the coast undrawn on device.

namespace Kestrel.Site
{
    public static class SiteFog
    {
        /// <summary>0 at <paramref name="start"/> metres, 1 at <paramref name="end"/>. 0 when fog is off.</summary>
        public static float Weight(float distance, float start, float end, bool enabled)
        {
            if (!enabled) return 0f;
            float span = end - start;
            if (span < 0.001f) span = 0.001f;
            float t = (distance - start) / span;
            if (t < 0f) return 0f;
            if (t > 1f) return 1f;
            return t;
        }
    }
}
