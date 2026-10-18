// Selection fade on the mission board. Cubic-out, one short slide.
// Reduce Motion holds the final frame so the card does not travel.
using System;

namespace Kestrel.Core.Flight
{
    public static class MissionBoardMotion
    {
        public const float Seconds = 0.28f;
        public const float SlidePt = 16f;

        public static float Blend(float t, bool reduceMotion)
        {
            if (reduceMotion) return 1f;
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float Slide(float t, bool reduceMotion) => (1f - Blend(t, reduceMotion)) * SlidePt;
    }
}
