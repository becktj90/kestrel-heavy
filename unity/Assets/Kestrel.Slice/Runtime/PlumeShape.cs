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
            int core = Mathf.Clamp(Mathf.RoundToInt(budget * 0.04f), 6, MaxCoreParticles);
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
            float byHeight = 1f - Smoothstep(t, 2.5f * t, heightAboveDeck);
            // #119 v3: the cloud was gone by T+2.7 s (vehicle clears the tower fast). Hold full emission for
            // CloudHoldSeconds after ignition regardless of height, then fade over CloudFadeSeconds; billows
            // live CloudLife s, so the cloud reads for ~6-10 s after liftoff.
            float byTime = 1f - Smoothstep(CloudHoldSeconds, CloudHoldSeconds + CloudFadeSeconds, sinceIgnition);
            return Mathf.Clamp01(thrust) * ramp * Mathf.Max(byHeight, byTime);
        }

        public const float CloudRampSeconds = 0.25f;   // was 0.75: nothing at T+0.4 (plus an ignition burst)
        public const float CloudIgnitionBurst = 0.35f;  // fraction of the cloud cap emitted on the ignition frame
        public const float CloudHoldSeconds = 5.5f, CloudFadeSeconds = 2f;
        public const float CloudLifeMin = 3f, CloudLifeMax = 4.5f;
        /// <summary>Cloud colour-over-life alpha keys (time, alpha): dense from birth (was 0.3 -> 0.95 -> 0.85).</summary>
        public static readonly Vector2[] CloudAlphaKeys = { new Vector2(0f, 0.85f), new Vector2(0.05f, 1f), new Vector2(0.7f, 0.95f), new Vector2(1f, 0f) };

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
        // #119 v4 capture, measured in pixels (iPhone 1290x2796, 119-v4): vehicle base 326 px, plume solid
        // 217 px just below the nozzles (0.67x) after a 30-45 px gap, cloud a 25-30 px full-width line at the deck.
        // Root causes (not formula tuning):
        //  A. SpriteSolidFraction was 0.6, but SoftParticle.Falloff (smoothstep^2) reaches alpha 0.5 at
        //     r = 0.36 of the half-size, so every "solid" width in this file was overestimated by 0.6/0.36 = 1.67x.
        //     The derived value is now pinned by a test against Falloff (PlumeLayoutTests.SpriteFractionsMatchFalloff).
        //  B. CloudHeight was -0.1 x deck radius (-1.5..-2 m) while PadGround.GroundY is -1 m: the cloud emitter sat
        //     UNDER the ground plane, so the opaque ground/deck depth-occluded the lower part of every billboard and
        //     only a thin sliver at deck level showed (the "haze line").
        //  C. The emitter offset forbade any sprite above the exit plane while sprites are born at 0.2 size and grow
        //     faster than they travel, which forces a soft gap of ~0.12 D under the nozzles (measured 1.5-2 m).
        //     Sprites may now wrap the bell exits by NozzleOverlapD (depth-tested against the bells).
        /// <summary>Fraction of a particle's nominal size that reads as solid with the soft sprite (alpha >= 0.5):
        /// SoftParticle.Falloff(r) = 0.5 at r = 0.36.</summary>
        public const float SpriteSolidFraction = 0.36f;
        /// <summary>How far (x cluster D) sprites may reach above the nozzle exit plane: they wrap the bell exits.</summary>
        public const float NozzleOverlapD = 0f;   // #129-v6 shots: a 0.15 D allowance + linear fade-in = cream fringe over the cores
        /// <summary>Alpha ramp (normalised life) after the fade-in point: alpha is exactly 0 until a sprite's
        /// visible top has passed below the nozzle plane, then reaches full over this span.</summary>
        public const float FadeRamp = 0.04f;
        /// <summary>Body alpha ramp as normalised life: 0.1 D of travel (a fixed 0.04 of a ~15 D body was a 0.6 D
        /// translucent band = the v7-sim gap under the nozzles).</summary>
        public static float BodyFadeRamp(Layout L) => Mathf.Clamp(0.02f * L.VisualD / Mathf.Max(1f, L.Length), 0.002f, FadeRamp);

        /// <summary>Dense body length cap (m): ~1.0 H at full throttle at sea level (70 m vehicle -> 60-70 m),
        /// shorter at low throttle, +60% in near-vacuum (VISUAL_SPEC 3.2 lengthening).</summary>
        public static float DenseLength(float vehicleHeight, float throttle, float paRatio) =>
            Mathf.Max(5f, vehicleHeight) * (0.6f + 0.4f * Mathf.Clamp01(throttle)) * (1f + 0.6f * Smoothstep(0.2f, 0f, Mathf.Max(0f, paRatio)));

        /// <summary>Gap (m) from the nozzle exit to where the body reads solid: emitter-equivalent offset minus the
        /// newborn solid half, plus half the alpha ramp distance.</summary>
        public static float NearNozzleGap(Layout L) => BodyStartGap(L) + 0.5f * BodyFadeRamp(L) * L.Length;

        /// <summary>Sprite overlap along the first `span` m below the nozzles: particles in that span (uniform in
        /// age along Length) x mean solid diameter there / span.</summary>
        public static float NearOverlap(Layout L, int bodyParticles, float span)
        {
            float n = bodyParticles * Mathf.Clamp01(span / Mathf.Max(1f, L.Length));
            float solid = 0.5f * (L.BodyMin + L.BodyMax) * BodySizeStart * PlumeSolidFraction;
            return n * solid / Mathf.Max(0.1f, span);
        }
        /// <summary>#129-v7 cloud launch speed (x CloudRadius). 0.35-0.8 (14-32 m/s) with the weak limit-velocity
        /// damping (probe-calibrated ~1/s) flung newborn billows 19 m out in 0.55 s: a thin, sparse ring.</summary>
        public const float CloudSpeedMin = 0.08f, CloudSpeedMax = 0.25f;
        /// <summary>#130 review: diamonds travelled 0.5 Length (~310 m/s) over 0.3 s, so the 0.08 fade-in put the
        /// first visible knot 7-9.5 m below the exit (and deck bounces flung them +5.8 m above it). Now they drift
        /// only DiamondTravelD x D over their life, i.e. a stationary pattern hanging off the exit, and fade in
        /// exactly when the 5% fringe (0.5 x 0.74 x 0.5 D = 0.185 D) clears the plane: 0.185 / 1.5 = 0.123.</summary>
        public const float DiamondTravelD = 1.5f, DiamondSizeD = 0.5f, DiamondLife = 0.3f;
        public const float DiamondFadeIn = 0.13f;
        /// <summary>Depth (m) below the exit of the top of the first visible diamond (its 5% fringe).</summary>
        public static float DiamondTopDepth(float d) => DiamondFadeIn * DiamondTravelD * d - 0.5f * PlumeFringeFraction * DiamondSizeD * d;
        /// <summary>Body alpha by the time deck-deflected particles exist (u >= 1 - lifetime loss): below ~0.05.</summary>
        public const float BodyTailAlpha = 0.04f, BodyTailAt = 0.7f;
        /// <summary>Fraction of a particle's nominal size inside which the soft sprite is visible (alpha >= ~1%).</summary>
        public const float SpriteVisibleFraction = 0.8f;
        // #129-v7 (cream fringe over the cores / gap under the nozzles): with the soft sprite the 1% fringe (0.8)
        // is 2.2x the solid radius (0.36), so either the fringe climbs above the nozzles or the solid plume starts
        // ~0.22 sprite sizes below them. Body/core/diamonds now use SoftParticle.TightTexture (flat to 0.55, soft
        // edge to 0.85): solid 0.70, ~5%-alpha fringe 0.82. The ground cloud keeps the soft sprite.
        /// <summary>TightFalloff(r) = 0.5 radius (fraction of nominal size) for the plume sprites.</summary>
        public const float PlumeSolidFraction = 0.71f;
        /// <summary>Radius past which a plume sprite is below ~5% of its peak alpha (TightFalloff 0.0556).</summary>
        public const float PlumeFringeFraction = 0.74f;
        public const float CoreHdrHigh = 1.5f, CoreHdrLow = 1.1f, CoreAlphaMax = 0.8f;   // #129: alpha-blended now (cannot exceed its colour), 0.45 vanished in ascent
        public const int MaxCoreParticles = 8;
        public const float CoreSizeOverLife = 0.8f;   // core shrinks CoreSizeStart -> 0.8 over life
        /// <summary>#119 v3 (gap below the nozzles): sprites are born small and grow, so the emitter can sit right
        /// at the exit plane. Offset = visible half-size at birth only, which is a few cm, not half a full billow.</summary>
        // #129 probe: the probe's body width (1.2x) averaged all particles over the ~200 m body, but only the first
        // ~40 m is in frame, and those sprites were still at 0.3-0.6 of full size: on screen ~half. Born at 0.9 now.
        public const float BodySizeStart = 0.9f, CoreSizeStart = 1f;
        /// <summary>Tolerance for "no sprite above the nozzle plane": float rounding (~3e-8) relative to height.</summary>
        public const float TopEdgeRelTolerance = 1e-4f;

        public struct Layout
        {
            public float VisualD, Length, Width, CoreSize, CoreLength, CoreEmitOffset, CoreSpawnRadius;
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
            // Short body (lead's call after #130): the spec length (~15 D, ~190 m on the heavy) spread the same
            // particles over far more plume than the frame shows, so the near-nozzle body was sparse (gap, blobs).
            // The dense body is capped to ~what is in frame, scaled with the vehicle and still following the
            // VISUAL_SPEC throttle and altitude-lengthening rules; altitude widening keeps using Width via the cone.
            L.Length = Mathf.Min(L.Length, DenseLength(vehicleHeight, tf, paRatio));
            // Visible width: ~1.2 D at sea level (VISUAL_SPEC 0.7 D column reads too thin), then the spec curve.
            L.Width = Mathf.Max(s.Width, 1.2f * d);

            // Core: small bright zone at the nozzle, ~0.9 D solid, ~12% of the plume (capped at 2.5 D).
            // v5: several smaller sprites spread over the cluster disc (CoreSpawnRadius) instead of one 2.5 D
            // sprite: a soft sprite's visible half (0.4 size) exceeds its solid half (0.18 size), so one big core
            // sprite forced a ~0.4 D dark gap under the nozzles.
            // #129: the core travelled CoreLength (~2 D, 160+ m/s) over 0.15 s and was born small, so it showed as
            // 1-2 blobs metres below the centre engines. Now: small constant-size sprites over the whole cluster
            // disc at the nozzle plane, drifting only CoreLength = 0.5 D over CoreLife (low speed, no gravity).
            L.CoreSize = 0.3f * d / PlumeSolidFraction;
            L.CoreSpawnRadius = 0.4f * d;
            L.CoreLength = 1.0f * d;   // #129-v6: at 0.5 D the fade-in point (0.67) was clamped to 0.6, so cores showed above the plane
            L.CoreEmitOffset = 0f;   // set below, once the sizes are known

            // Body: starts ~1 D solid, grows only a little; the cone supplies altitude expansion up to Width.
            L.BodySpawnRadius = 0.08f * d;
            L.BodyMax = 1.15f * d / PlumeSolidFraction;
            L.BodyMin = 0.95f * d / PlumeSolidFraction;
            L.BodyGrow = 1.15f;
            L.BodyLife = 0.6f;
            float solidEnd = L.BodyMax * PlumeSolidFraction * L.BodyGrow + 2f * L.BodySpawnRadius;
            L.BodyConeDeg = Mathf.Atan2(Mathf.Max(0f, L.Width - solidEnd) * 0.5f, L.Length) * Mathf.Rad2Deg;
            // Smallest emitter offset keeping every sprite's top at or below the exit plane over its whole life
            // (born small -> offset ~ birth half-size; only grows if a sprite would outrun its own travel).
            L.BodyEmitOffset = 0f; L.CoreEmitOffset = 0f;
            for (int i = 0; i <= TopEdgeSamples; i++)
            {
                float u = i / (float)TopEdgeSamples;
                L.BodyEmitOffset = Mathf.Max(L.BodyEmitOffset, 0.5f * PlumeFringeFraction * L.BodyMax * BodySize(L, u) - L.Length * u - NozzleOverlapD * d);
                L.CoreEmitOffset = Mathf.Max(L.CoreEmitOffset, 0.5f * PlumeFringeFraction * L.CoreSize * CoreSize(u) - L.CoreLength * u - NozzleOverlapD * d);
            }

            // Ground cloud: same billows, but spawned at the deck edge (outside a clear zone round the engines),
            // a little below the deck top, moving outward and down before rising.
            float deck = Mathf.Max(deckRadius, 5f);
            L.CloudRadius = 2f * Mathf.Max(deck, 10f);   // v5 sim: 3x deck spread the billows thin
            // Solid billow targets re-derived with the real SpriteSolidFraction (A): keeping 0.35 H solid made 68 m
            // sprites that walled off the frame in the v5 reference render.
            L.CloudSizeMax = Mathf.Max(0.55f * deck, 0.2f * vehicleHeight) / SpriteSolidFraction;
            L.CloudSizeMin = L.CloudSizeMax * 0.55f;
            L.CloudSizeStart = 0.45f;   // #129-v7: 0.3 (with fast spread) left a thin strip at T+0.6
            L.CloudClearRadius = Mathf.Max(1.5f * vehicleRadius, 1.5f * d);
            L.CloudSpawnRadius = Mathf.Max(1.5f * deck, L.CloudClearRadius + CloudHalfVisible(L, 0f));   // #129: ring pushed outward
            // Root cause B: centre ABOVE the deck top (pad origin, y = 0; ground is PadGround.GroundY = -1 m) by the solid
            // half of a newborn billow, so the ground/deck never occludes the billow body.
            L.CloudHeight = CloudLift(L);
            return L;
        }

        public const float CloudGrowEnd = 1.6f;
        /// <summary>Cloud emitter height above the deck top: solid half-size of a newborn billow.</summary>
        public static float CloudLift(Layout L) => CloudLiftM;
        /// <summary>#129: centre just above the deck top (ground is -1 m) so billows stay low in the first second.</summary>
        public const float CloudLiftM = 0.5f;
        /// <summary>Normalised body life over which alpha fades in when the emitter sits at the exit plane: the
        /// time a particle needs to travel BodyEmitOffset (so the visible silhouette equals an offset emitter).</summary>
        public static float BodyFadeIn(Layout L) => Mathf.Clamp01(L.BodyEmitOffset / Mathf.Max(1f, L.Length));
        /// <summary>Core sprite lifetime (s).</summary>
        public const float CoreLife = 0.3f;
        /// <summary>Hold the plume's particle budget for this long after ignition, even if the governor steps the
        /// quality preset down (#129: caps dropped 208->48 between T+0.48 and T+1.31).</summary>
        // (#129-v6: the private 15 s budget hold is gone; QualityGovernor holds the tier for 20 s after ignition.)
        /// <summary>Body deck collision: deflect sideways, low bounce, high lifetime loss (short, faded spray).</summary>
        /// #130 review: lifetimeLoss 0.75 killed/faded nearly every body particle while the 186 m body crossed the
        /// deck, so the body sat at 48/212 live with alpha 0.02-0.06 until T+2.5. Now deflected particles keep
        /// their life and alpha (count reaches the budget, opaque from ignition) and are instead SHRUNK by
        /// size-by-speed: dampen 0.85 leaves 15% speed, so a deflected sprite is ~BodyDeflectSize of its size and
        /// cannot climb over the nozzles / boosters (the v6 "orange band").
        public const float BodyDeckBounce = 0f, BodyDeckDampen = 0.85f, BodyDeckLifetimeLoss = 0f, BodyDeckRadiusScale = 0.1f;
        public const float BodyDeflectSize = 0.12f;
        /// <summary>Size multiplier vs speed fraction (speed / startSpeed): linear from BodyDeflectSize at 0 to 1 at 0.6+.</summary>
        public static float BodySizeBySpeed(float speedFraction) => Mathf.Lerp(BodyDeflectSize, 1f, Mathf.Clamp01(speedFraction / 0.6f));
        /// <summary>Core and diamonds die on deck contact (no bounce): they are small near-nozzle features, and
        /// their deck bounce put them +2.8 / +5.8 m above the nozzle exit at T+0.6 (#130 review).</summary>
        public const float NearDeckBounce = 0f, NearDeckLifetimeLoss = 1f;   // #129-v6: deflected spray climbed 150-200 px; no bounce, alpha <= BodyTailAlpha after the loss   // v6 sim: 0.5 loss made an orange band over the nozzles
        /// <summary>Visible half-size of the largest cloud billow at normalised life u (size curve start -> 1.6).</summary>
        public static float CloudHalfVisible(Layout L, float u) => 0.5f * SpriteVisibleFraction * L.CloudSizeMax * Mathf.Lerp(L.CloudSizeStart, CloudGrowEnd, Mathf.Clamp01(u));
        /// <summary>Closest a cloud billow's visible edge gets to the vehicle axis right after spawning.</summary>
        public static float CloudInnerEdge(Layout L) => L.CloudSpawnRadius - CloudHalfVisible(L, 0f);

        /// <summary>Visible plume width at the end of the body (spawn spread + cone + grown particle).</summary>
        public static float BodyVisibleWidth(Layout L) =>
            2f * L.BodySpawnRadius + 2f * L.Length * Mathf.Tan(L.BodyConeDeg * Mathf.Deg2Rad) + L.BodyMax * PlumeSolidFraction * L.BodyGrow;

        /// <summary>Highest point (m above the nozzle-exit plane; &lt;= 0 means below it) any visible body or core
        /// sprite reaches over its life. Particles move down the axis at Length/life (body) or CoreLength/0.15 s.</summary>
        public static float PlumeTopEdge(Layout L)
        {
            float top = float.MinValue;
            for (int i = 0; i <= TopEdgeSamples; i++)
            {
                float u = i / (float)TopEdgeSamples;
                float bodyTop = 0.5f * PlumeFringeFraction * L.BodyMax * BodySize(L, u) - L.BodyEmitOffset - L.Length * u;
                float coreTop = 0.5f * PlumeFringeFraction * L.CoreSize * CoreSize(u) - L.CoreEmitOffset - L.CoreLength * u;
                top = Mathf.Max(top, Mathf.Max(bodyTop, coreTop));
            }
            return top;
        }

        /// <summary>Body size-over-life multiplier: born at BodySizeStart, full size by 15% of life, then grows to BodyGrow.</summary>
        public static float BodySize(Layout L, float u) => u < BodyFullAt ? Mathf.Lerp(BodySizeStart, 1f, u / BodyFullAt) : Mathf.Lerp(1f, L.BodyGrow, (u - BodyFullAt) / (1f - BodyFullAt));
        /// <summary>Core grows slower than it travels (full size at CoreFullAt of life) so it can start at the exit.</summary>
        public static float CoreSize(float u) => u < CoreFullAt ? Mathf.Lerp(CoreSizeStart, 1f, u / CoreFullAt) : Mathf.Lerp(1f, CoreSizeOverLife, (u - CoreFullAt) / (1f - CoreFullAt));
        public const float CoreFullAt = 0.6f;
        /// <summary>Gap (m) between the nozzle exit and the first solid core sprite at birth.</summary>
        public static float CoreStartGap(Layout L) => L.CoreEmitOffset - 0.5f * PlumeSolidFraction * L.CoreSize * CoreSizeStart;
        public const float BodyFullAt = 0.15f;
        public const int TopEdgeSamples = 40;
        /// <summary>Gap (m) between the nozzle exit and the first solid body sprite at birth (should be ~0).</summary>
        public static float BodyStartGap(Layout L) => L.BodyEmitOffset - 0.5f * PlumeSolidFraction * L.BodyMax * BodySizeStart;

        /// <summary>Peak additive core contribution on the axis: overlapping core sprites x alpha x HDR.</summary>
        public static float CorePeak(Layout L, int coreParticles, bool hdr)
        {
            float overlap = Mathf.Max(1f, coreParticles * L.CoreSize * PlumeSolidFraction / Mathf.Max(0.1f, L.CoreLength + L.CoreSize * PlumeSolidFraction));
            return overlap * CoreAlphaMax * (hdr ? CoreHdrHigh : CoreHdrLow);
        }

        /// <summary>Overlap factor along the body: (live body particles x mean solid diameter) / length. &gt;= 2
        /// means every point on the axis is covered by ~2 particles (continuous, no blobs).</summary>
        public static float BodyOverlap(Layout L, int bodyParticles)
        {
            float mean = 0.5f * (L.BodyMin + L.BodyMax) * 0.5f * (1f + L.BodyGrow) * PlumeSolidFraction;
            return bodyParticles * mean / Mathf.Max(1f, L.Length);
        }

        /// <summary>Fraction of the ground-cloud disc covered by solid particle area at full emission.</summary>
        public static float CloudCoverage(Layout L, int cloudParticles)
        {
            float r = 0.5f * (L.CloudSizeMin + L.CloudSizeMax) * SpriteSolidFraction * 0.5f * 1.6f; // mean grown radius
            return Mathf.Clamp01(cloudParticles * r * r / (L.CloudRadius * L.CloudRadius));
        }

        /// <summary>Minimum particles kept for the ground cloud even on Low, so it reads on every device.</summary>
        public const int MinCloud = 96;
    }
}
