// Fun review #416 item 8: the web build drew grey desktop scrollbars (a 16-30 px lane with arrows)
// on every list, and on the KH-3 mission card they sat over the title and brief. On the web the
// scroller is hidden (wheel, drag and touch still scroll) and a short fade at the bottom edge says
// "there is more" instead. iOS keeps its own thin overlay indicator, so this is web-only.
using System;

namespace Kestrel.Core.Feel
{
    public static class WebScroll
    {
        /// <summary>Height of the bottom fade, in panel points.</summary>
        public const float FadePt = 28f;

        /// <summary>Bands the fade is drawn with (no gradient style in UI Toolkit).</summary>
        public const int FadeBands = 10;

        /// <summary>Strongest fade alpha at the very bottom edge.</summary>
        public const float FadeMaxAlpha = 0.9f;

        /// <summary>The lane a list keeps for its scroller: none on the web, where the scroller is hidden.</summary>
        public static float Lane(bool web, float nativeLane) => web ? 0f : nativeLane;

        /// <summary>The fade shows only while there is more content below the viewport.</summary>
        public static bool FadeShows(float contentH, float viewportH, float offsetY)
        {
            if (float.IsNaN(contentH) || float.IsNaN(viewportH) || viewportH <= 0f) return false;
            float rest = contentH - viewportH - Math.Max(0f, offsetY);
            return rest > 2f;
        }

        /// <summary>Alpha of band <paramref name="i"/> (0 = top, transparent end).</summary>
        public static float BandAlpha(int i)
        {
            if (i < 0) i = 0;
            if (i >= FadeBands) i = FadeBands - 1;
            float t = (i + 1f) / FadeBands;
            return FadeMaxAlpha * t * t;
        }
    }
}
