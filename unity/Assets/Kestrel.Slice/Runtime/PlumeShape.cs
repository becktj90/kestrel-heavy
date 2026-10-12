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
            int cloud = Mathf.Max(MinCloud, Mathf.RoundToInt(budget * 0.40f));
            int body = Mathf.Max(0, budget - core - dia - cloud);
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

        // ---- Visual layout (P0-3 follow-up: "still a thin trail of orange blobs") -----------------------------
        // Root causes found in the #119 Unity capture:
        //  1. Scale: every size keyed off ExitDiameter = sqrt(stage-0 engines) * nozzle exit (~3 m) while the
        //     vehicle is ~70 m tall and the cluster+boosters ~12 m wide; at sea level Width clamps to 0.7 D, so
        //     the plume was narrower than the engine cluster.
        //  2. Visible size: SoftParticle.Falloff is smoothstep^2, half-alpha at r ~ 0.3, so a particle reads at
        //     ~1/3 of its nominal startSize. Combined with 1. that is a ~1 m dot on a 70 m rocket.
        //  3. Colour: core alpha applied three times (startColor CoreAlpha x gradient x sprite), body gradient
        //     orange from 35% of life, and the shared alpha-blended LDR material cannot exceed white: no core.
        //  4. Ground cloud: shape radius max(3, D), particles 2.5-4.5 D, deck radius hard-coded 15 m, soft
        //     sprite alpha + 0.55-0.75 gradient alpha: a faint smudge; on Low (iPad capture preset, budget 150)
        //     it had 60 tiny particles and emission only ramped on after Apply, so it barely appeared.
        /// <summary>Fraction of a particle's nominal size that reads as solid with the soft sprite (alpha >= 0.5).</summary>
        public const float SpriteSolidFraction = 0.6f;

        public readonly struct Layout
        {
            public readonly float VisualD, Length, Width, CoreSize, CoreLength, BodyMin, BodyMax, BodyGrow, BodyLife, CloudRadius, CloudSizeMin, CloudSizeMax, CloudHeight;
            public Layout(float vd, float l, float w, float cs, float cl, float bmin, float bmax, float grow, float life, float cr, float cmin, float cmax, float ch)
            { VisualD = vd; Length = l; Width = w; CoreSize = cs; CoreLength = cl; BodyMin = bmin; BodyMax = bmax; BodyGrow = grow; BodyLife = life; CloudRadius = cr; CloudSizeMin = cmin; CloudSizeMax = cmax; CloudHeight = ch; }
        }

        /// <param name="clusterD">cluster-equivalent exit diameter (m)</param>
        /// <param name="vehicleRadius">widest vehicle radius incl. boosters (m)</param>
        /// <param name="vehicleHeight">vehicle height (m)</param>
        /// <param name="deckRadius">pad deck radius (m)</param>
        public static Layout ComputeLayout(float throttle, float paRatio, float clusterD, float vehicleRadius, float vehicleHeight, float deckRadius)
        {
            float vd = Mathf.Max(clusterD, 1.5f * vehicleRadius, 0.1f * vehicleHeight);   // plume reads as wide as the engine section
            var s = Compute(throttle, paRatio, vd);
            float len = Mathf.Max(s.Length, 0.9f * vehicleHeight * (0.4f + 0.6f * Mathf.Clamp01(throttle)));
            float width = Mathf.Max(s.Width, vd);
            float core = vd * 1.3f / SpriteSolidFraction;                         // solid core ~1.3 x engine section
            float bmin = vd * 1.0f / SpriteSolidFraction, bmax = width * 1.2f / SpriteSolidFraction;
            float cloudR = 3f * Mathf.Max(deckRadius, 10f);                        // 2-4x deck radius target
            float cmax = Mathf.Max(0.9f * deckRadius, 0.35f * vehicleHeight) / SpriteSolidFraction;
            return new Layout(vd, len, width, core, len * 0.3f, bmin, bmax, 2.2f, 0.6f, cloudR, cmax * 0.55f, cmax, 0.25f * deckRadius);
        }

        /// <summary>Overlap factor along the body: (live body particles x mean solid diameter) / length. &gt;= 2
        /// means every point on the axis is covered by ~2 particles (continuous, no blobs).</summary>
        public static float BodyOverlap(Layout L, int bodyParticles)
        {
            float mean = 0.5f * (L.BodyMin + L.BodyMax) * 0.5f * (1f + L.BodyGrow) * SpriteSolidFraction;
            return bodyParticles * mean / Mathf.Max(1f, L.Length);
        }

        /// <summary>Fraction of the ground-cloud disc covered by solid particle area at full emission.</summary>
        public static float CloudCoverage(Layout L, int cloudParticles)
        {
            float r = 0.5f * (L.CloudSizeMin + L.CloudSizeMax) * SpriteSolidFraction * 0.5f * 1.6f; // mean grown radius
            return Mathf.Clamp01(cloudParticles * r * r / (L.CloudRadius * L.CloudRadius));
        }

        /// <summary>Minimum particles kept for the ground cloud even on Low, so it reads on every device.</summary>
        public const int MinCloud = 48;
    }
}
