// Exploded view and part picking for the Hangar, as pure math so EditMode covers it.
// DESIGNER_SPEC §5: stages, boosters and fairing separate along the axis by 0.15 × the vehicle height per joint,
// eased over 400 ms, instant with Reduce Motion. Each part rises by the gaps of every joint below it, so the
// stack spreads evenly and nothing overlaps; boosters slide outward instead of up.
using System.Collections.Generic;
using Kestrel.Core.Hangar;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class ExplodedView
    {
        public const float GapFraction = 0.15f;
        public const float Seconds = 0.4f;

        /// <summary>Joint order bottom to top: stage 1, interstage 1–2, stage 2, …, fairing contents (payload), fairing.</summary>
        public static int Level(PartRef p)
        {
            switch (p.kind)
            {
                case PartKind.Stage: return 2 * p.index;
                case PartKind.Interstage: return 2 * p.index - 1;
                case PartKind.Payload: return 1000;
                case PartKind.Fairing: return 1001;
                default: return 0;
            }
        }

        /// <summary>
        /// Offset of a part at explode amount <paramref name="t"/> (0 assembled, 1 fully open). Parts on the axis rise
        /// one gap per level; the fairing rises one extra gap above the payload so the cargo shows. Boosters move out
        /// radially by one gap along <paramref name="boosterOutward"/>.
        /// </summary>
        public static Vector3 Offset(PartRef p, int stageCount, float vehicleHeight, float t, Vector3 boosterOutward)
        {
            t = Mathf.Clamp01(float.IsNaN(t) ? 0f : t);
            float gap = GapFraction * DesignFraming.SafeHeight(vehicleHeight);
            if (p.kind == PartKind.Boosters) return boosterOutward.normalized * gap * t;
            int level = Level(p);
            float steps;
            if (p.kind == PartKind.Payload) steps = Mathf.Max(0, 2 * stageCount - 2) + 1;
            else if (p.kind == PartKind.Fairing) steps = Mathf.Max(0, 2 * stageCount - 2) + 2;
            else steps = level;
            return Vector3.up * gap * 0.5f * steps * t;
        }

        /// <summary>Eased explode amount for a timer (SmoothStep over <see cref="Seconds"/>).</summary>
        public static float Ease(float elapsed) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Seconds));

        /// <summary>Height the exploded stack needs, so framing can include it.</summary>
        public static float ExplodedHeight(float vehicleHeight, int stageCount)
            => DesignFraming.SafeHeight(vehicleHeight) * (1f + GapFraction * 0.5f * (Mathf.Max(0, 2 * stageCount - 2) + 2));
    }

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
