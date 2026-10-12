// Pure plume math from docs/VISUAL_SPEC.md §3 (pressure regime, length/width curves, colours) plus the
// per-quality particle split and the liftoff ground-cloud envelope. No scene access: EditMode-testable.
using UnityEngine;

namespace Kestrel.Slice
{
    public readonly struct PlumeShapeOut
    {
        public readonly float Length, Width, CoreAlpha, X; public readonly int Diamonds;
        public PlumeShapeOut(float l, float w, float a, float x, int d) { Length = l; Width = w; CoreAlpha = a; X = x; Diamonds = d; }
    }

    public readonly struct PlumeBudget
    {
        public readonly int Core, Body, Diamonds, Cloud;
        public PlumeBudget(int c, int b, int d, int g) { Core = c; Body = b; Diamonds = d; Cloud = g; }
        public int Total => Core + Body + Diamonds + Cloud;
    }

    public static class PlumeShape
    {
        public const float P0 = 101325f;
        /// <summary>VISUAL_SPEC §3.1 design-pressure proxy for gg-kerolox (r = 0.905).</summary>
        public const float DefaultPd = 0.57f;
        /// <summary>data/quality.json particleBudget for Low/Medium/High/Ultra, indexed by quality level.</summary>
        public static readonly int[] Budgets = { 150, 400, 800, 1500 };

        // VISUAL_SPEC §3.4 kerolox (linear hex)
        public static readonly Color Core = Hex(0xfff4d8), Mid = Hex(0xffb040), Tail = Hex(0xff6a1a);
        public static readonly Color Smoke = new Color(0.86f, 0.85f, 0.83f), Steam = new Color(0.97f, 0.97f, 0.98f);

        static Color Hex(int h) => new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f);

        public static float Smoothstep(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3 - 2 * t);
        }

        /// <param name="throttle">s, normalised throttle 0..1</param>
        /// <param name="paRatio">ambient pressure / P0</param>
        /// <param name="d">exit diameter of the (cluster) plume, m</param>
        public static PlumeShapeOut Compute(float throttle, float paRatio, float d, float pd = DefaultPd)
        {
            float s = Mathf.Clamp01(throttle);
            float pa = Mathf.Max(0f, paRatio);
            float x = pd / Mathf.Max(pa, 1e-4f);
            float length = d * (6 + 10 * s) * (1 + 0.6f * Smoothstep(0.2f, 0f, pa));
            float width = d * Mathf.Clamp(Mathf.Sqrt(x), 0.7f, 6f);
            float coreAlpha = 0.95f * (0.55f + 0.45f * s) * (1 - 0.6f * Smoothstep(0.3f, 0.0005f, pa));
            int diamonds = x < 1.25f && pa > 0.05f ? Mathf.RoundToInt(3 + 4 * s) : 0;
            return new PlumeShapeOut(length, width, coreAlpha, x, diamonds);
        }

        /// <summary>Split one preset's particle budget. Low ("basic" plume) gets no diamonds; the ground cloud
        /// is the biggest consumer at liftoff and is idle once the vehicle clears the tower.</summary>
        public static PlumeBudget Split(int budget, bool diamonds)
        {
            budget = Mathf.Max(40, budget);
            int core = Mathf.RoundToInt(budget * 0.15f);
            int dia = diamonds ? Mathf.RoundToInt(budget * 0.05f) : 0;
            int cloud = Mathf.RoundToInt(budget * 0.40f);
            int body = budget - core - dia - cloud;
            return new PlumeBudget(core, body, dia, cloud);
        }

        public static int BudgetForLevel(int level) => Budgets[Mathf.Clamp(level, 0, Budgets.Length - 1)];

        /// <summary>Ground-cloud emission 0..1 from the nozzle's height above the deck: full while the jet hits
        /// the deck (until the vehicle clears the tower top), fading to 0 by 2.5x tower height.</summary>
        public static float GroundCloud(float thrust, float heightAboveDeck, float towerHeight)
        {
            if (thrust <= 0.01f) return 0f;
            float t = Mathf.Max(10f, towerHeight);
            return Mathf.Clamp01(thrust) * (1f - Smoothstep(t, 2.5f * t, heightAboveDeck));
        }
    }
}
