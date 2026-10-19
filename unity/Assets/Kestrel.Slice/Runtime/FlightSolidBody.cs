// Flight used to hide the painted parts and draw only the combined lathe. On device that
// lathe can fail to read, which leaves the strut lattice looking like a wireframe.
// These calls force the body material opaque so the skin stays solid in Hangar and in flight.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class FlightSolidBody
    {
        public static void Apply(Material m, Color c)
        {
            if (m == null) return;
            c.a = 1f;
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
            m.SetOverrideTag("RenderType", "Opaque");
            m.renderQueue = 2000;
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHAMODULATE_ON");
            m.EnableKeyword("_SURFACE_TYPE_OPAQUE");
        }
    }
}
