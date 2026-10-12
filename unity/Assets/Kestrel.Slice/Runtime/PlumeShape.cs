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
        /// <summary>Body colour at the nozzle: yellow, so only the small additive core reads white.</summary>
        public static readonly Color BodyHead = Hex(0xffd27a);
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
        /// is the biggest consumer at liftoff and is idle once the vehicle clears the tower. The additive core is
        /// deliberately tiny (few particles) so it cannot stack to flat white.</summary>
        public static PlumeBudget Split(int budget, bool diamonds)
        {
            budget = Mathf.Max(40, budget);
            int core = Mathf.Clamp(Mathf.RoundToInt(budget * 0.04f), 4, MaxCoreParticles);
            int dia = diamonds ? Mathf.RoundToInt(budget * 0.05f) : 0;
            int cloud = Mathf.Max(MinCloud, Mathf.RoundToInt(budget * 0.40f));
            int body = Mathf.Max(0, budget - core - dia - cloud);
            return new PlumeBudget(core, body, dia, cloud);
        }

        public static int BudgetForLevel(int level) => Budgets[Mathf.Clamp(level, 0, Budgets.Length - 1)];

        /// <summary>Ground-cloud emission 0..1 from the nozzle's height above the deck: full while the jet hits
        /// the deck (until the vehicle clears the tower top), fading to 0 by 2.5x tower height.</summary>
        public static float GroundCloud(float thrust, float heightAboveDeck, float towerHeight, float sinceIgnition = float.MaxValue)
        {
            if (thrust <= 0.01f) return 0f;
            float t = Mathf.Max(10f, towerHeight);
            float ramp = Smoothstep(0f, CloudRampSeconds, sinceIgnition);   // builds up over ~0.75 s after ignition
            return Mathf.Clamp01(thrust) * ramp * (1f - Smoothstep(t, 2.5f * t, heightAboveDeck));
        }

        public const float CloudRampSeconds = 0.75f;

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
        // #119 v2 capture ("flat white blob wider than the vehicle over the lower third of the boosters"):
        //  - VisualD was max(cluster, 1.5 x vehicle radius, 0.1 x height) and the core 1.3 x that: far too wide.
        //  - Particles were emitted at the nozzle plane, so half of each billboard sat above it, over the boosters.
        //  - The additive core (HDR x4, 15% of budget, alpha 1) stacked to saturated white.
        // Now: everything keys off the engine cluster diameter, emitters sit below the nozzle plane by the visible
        // half-size of their largest particle, and the core is a few small, low-alpha, low-HDR particles.
        /// <summary>Fraction of a particle's nominal size that reads as solid with the soft sprite (alpha >= 0.5).</summary>
        public const float SpriteSolidFraction = 0.6f;
        /// <summary>Fraction of a particle's nominal size inside which the soft sprite is visible (alpha >= ~1%).</summary>
        public const float SpriteVisibleFraction = 0.8f;
        public const float CoreHdrHigh = 1.5f, CoreHdrLow = 1.1f, CoreAlphaMax = 0.45f;
        public const int MaxCoreParticles = 8;
        public const float CoreSizeOverLife = 0.8f;   // core shrinks 1 -> 0.8 over life

        public struct Layout
        {
            public float VisualD, Length, Width, CoreSize, CoreLength, CoreEmitOffset;
            public float BodyMin, BodyMax, BodyGrow, BodyLife, BodySpawnRadius, BodyConeDeg, BodyEmitOffset;
            public float CloudRadius, CloudSizeMin, CloudSizeMax, CloudSizeStart, CloudHeight, CloudSpawnRadius, CloudClearRadius;
        }

        /// <param name="clusterD">cluster-equivalent exit diameter (m)</param>
        /// <param name="vehicleRadius">widest vehicle radius incl. boosters (m)</param>
        /// <param name="vehicleHeight">vehicle height (m)</param>
        /// <param name="deckRadius">pad deck radius (m)</param>
        public static Layout ComputeLayout(float throttle, float paRatio, float clusterD, float vehicleRadius, float vehicleHeight, float deckRadius)
        {
            float d = Mathf.Max(0.5f, clusterD);
            float tf = Mathf.Clamp01(throttle);
            var s = Compute(tf, paRatio, d);
            var L = new Layout { VisualD = d };
            L.Length = Mathf.Max(s.Length, 0.6f * vehicleHeight * (0.4f + 0.6f * tf));
            // Visible width: ~1.2 D at sea level (VISUAL_SPEC 0.7 D column reads too thin), then the spec curve.
            L.Width = Mathf.Max(s.Width, 1.2f * d);

            // Core: small bright zone at the nozzle, ~0.9 D solid, ~12% of the plume (capped at 2.5 D).
            L.CoreSize = 0.9f * d / SpriteSolidFraction;
            L.CoreLength = Mathf.Min(0.12f * L.Length, 2.5f * d);
            L.CoreEmitOffset = 0.5f * SpriteVisibleFraction * L.CoreSize;

            // Body: starts ~1 D solid, grows only a little; the cone supplies altitude expansion up to Width.
            L.BodySpawnRadius = 0.08f * d;
            L.BodyMax = 1.0f * d / SpriteSolidFraction;
            L.BodyMin = 0.8f * d / SpriteSolidFraction;
            L.BodyGrow = 1.15f;
            L.BodyLife = 0.6f;
            float solidEnd = L.BodyMax * SpriteSolidFraction * L.BodyGrow + 2f * L.BodySpawnRadius;
            L.BodyConeDeg = Mathf.Atan2(Mathf.Max(0f, L.Width - solidEnd) * 0.5f, L.Length) * Mathf.Rad2Deg;
            L.BodyEmitOffset = 0.5f * SpriteVisibleFraction * L.BodyMax;

            // Ground cloud: same billows, but spawned at the deck edge (outside a clear zone round the engines),
            // a little below the deck top, moving outward and down before rising.
            float deck = Mathf.Max(deckRadius, 5f);
            L.CloudRadius = 3f * Mathf.Max(deck, 10f);
            L.CloudSizeMax = Mathf.Max(0.9f * deck, 0.35f * vehicleHeight) / SpriteSolidFraction;
            L.CloudSizeMin = L.CloudSizeMax * 0.55f;
            L.CloudSizeStart = 0.5f;
            L.CloudClearRadius = Mathf.Max(1.5f * vehicleRadius, 1.5f * d);
            L.CloudSpawnRadius = Mathf.Max(deck, L.CloudClearRadius + CloudHalfVisible(L, 0f));
            L.CloudHeight = -0.1f * deck;
            return L;
        }

        public const float CloudGrowEnd = 1.6f;
        /// <summary>Visible half-size of the largest cloud billow at normalised life u (size curve start -> 1.6).</summary>
        public static float CloudHalfVisible(Layout L, float u) => 0.5f * SpriteVisibleFraction * L.CloudSizeMax * Mathf.Lerp(L.CloudSizeStart, CloudGrowEnd, Mathf.Clamp01(u));
        /// <summary>Closest a cloud billow's visible edge gets to the vehicle axis right after spawning.</summary>
        public static float CloudInnerEdge(Layout L) => L.CloudSpawnRadius - CloudHalfVisible(L, 0f);

        /// <summary>Visible plume width at the end of the body (spawn spread + cone + grown particle).</summary>
        public static float BodyVisibleWidth(Layout L) =>
            2f * L.BodySpawnRadius + 2f * L.Length * Mathf.Tan(L.BodyConeDeg * Mathf.Deg2Rad) + L.BodyMax * SpriteSolidFraction * L.BodyGrow;

        /// <summary>Highest point (m above the nozzle-exit plane; &lt;= 0 means below it) any visible body or core
        /// sprite reaches over its life. Particles move down the axis at Length/life (body) or CoreLength/0.15 s.</summary>
        public static float PlumeTopEdge(Layout L)
        {
            float top = float.MinValue;
            for (int i = 0; i <= 20; i++)
            {
                float u = i / 20f;
                float bodyTop = 0.5f * SpriteVisibleFraction * L.BodyMax * Mathf.Lerp(1f, L.BodyGrow, u) - L.BodyEmitOffset - L.Length * u;
                float coreTop = 0.5f * SpriteVisibleFraction * L.CoreSize * Mathf.Lerp(1f, CoreSizeOverLife, u) - L.CoreEmitOffset - L.CoreLength * u;
                top = Mathf.Max(top, Mathf.Max(bodyTop, coreTop));
            }
            return top;
        }

        /// <summary>Peak additive core contribution on the axis: overlapping core sprites x alpha x HDR.</summary>
        public static float CorePeak(Layout L, int coreParticles, bool hdr)
        {
            float overlap = Mathf.Max(1f, coreParticles * L.CoreSize * SpriteSolidFraction / Mathf.Max(0.1f, L.CoreLength + L.CoreSize * SpriteSolidFraction));
            return overlap * CoreAlphaMax * (hdr ? CoreHdrHigh : CoreHdrLow);
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
