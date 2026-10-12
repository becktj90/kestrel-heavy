// Tap-to-select for the Hangar 3D view, as pure math so EditMode covers it: each part's axis is projected to the
// screen and the tap picks the part whose capsule (axis ± radius + a fingertip of slop) contains it.
using System.Collections.Generic;
using Kestrel.Core.Hangar;
using UnityEngine;

namespace Kestrel.Slice
{
    /// <summary>A part as seen on screen: its axis from bottom to top centre, and its radius, all in pixels.</summary>
    public struct ScreenPart
    {
        public PartRef part;
        public Vector2 bottom, top;
        public float radiusPx;
        /// <summary>Nearer parts win ties (smaller is nearer).</summary>
        public float depth;
    }

    public static class PartPicker
    {
        /// <summary>Extra reach around thin parts, in pixels (about a fingertip).</summary>
        public const float SlopPx = 24f;

        /// <summary>The part whose projected axis capsule contains the tap, preferring the closest axis then the nearest depth.</summary>
        public static bool Pick(IReadOnlyList<ScreenPart> parts, Vector2 tap, out PartRef picked, float slopPx = SlopPx)
        {
            picked = default;
            float best = float.MaxValue, bestDepth = float.MaxValue;
            bool found = false;
            if (parts == null) return false;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                float d = DistanceToSegment(tap, p.bottom, p.top);
                if (float.IsNaN(d) || d > p.radiusPx + slopPx) continue;
                float score = d / Mathf.Max(1f, p.radiusPx);
                if (score < best - 1e-4f || (Mathf.Abs(score - best) <= 1e-4f && p.depth < bestDepth))
                {
                    best = score; bestDepth = p.depth; picked = p.part; found = true;
                }
            }
            return found;
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float len2 = ab.x * ab.x + ab.y * ab.y;
            if (len2 < 1e-9f) return Vector2.Distance(p, a);
            float u = Mathf.Clamp01(((p.x - a.x) * ab.x + (p.y - a.y) * ab.y) / len2);
            return Vector2.Distance(p, new Vector2(a.x + ab.x * u, a.y + ab.y * u));
        }
    }
}
