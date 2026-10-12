using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    // P0-3 follow-up, #119 v2 capture: plume blown out to flat white, wider than the vehicle, covering the lower
    // third of the boosters, and the ground cloud hiding the engines at liftoff.
    public class PlumeLayoutTests
    {
        const float H = 70f, R = 6f, ClusterD = 3f, Deck = 15f;   // slice heavy: 70 m, boosters, 9-engine core

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void BodyNarrowAtSeaLevelAndContinuous(int level)
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            float w = PlumeShape.BodyVisibleWidth(L);
            Assert.That(w, Is.LessThanOrEqualTo(1.5f * ClusterD), "sea-level body width <= 1.5x engine cluster");
            Assert.That(w, Is.GreaterThanOrEqualTo(1.0f * ClusterD), "sea-level body at least the engine cluster");
            Assert.That(L.VisualD, Is.EqualTo(ClusterD).Within(1e-4f), "keyed to the engine cluster, not the vehicle radius/height");
            Assert.That(L.Length / H, Is.GreaterThanOrEqualTo(0.6f), "length floor 0.6x vehicle height at full throttle");
            Assert.That(PlumeShape.BodyOverlap(L, b.Body), Is.GreaterThanOrEqualTo(2f), $"level {level}: body must overlap (no blobs)");
        }

        // Short-body PR (lead's call): 208 body particles over the ~186 m spec length left a ~3 m translucent gap
        // under the nozzles and a blobby body. Dense body ~ what's in frame (60-80 m), continuous near the nozzles.
        [TestCase(0, 3f), TestCase(1, 3f), TestCase(2, 3f), TestCase(3, 3f), TestCase(0, 12.38f), TestCase(3, 12.38f), TestCase(0, 14f)]
        public void ShortDenseBodyIsContinuousNearNozzles(int level, float d)
        {
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            foreach (var tf in new[] { 1f, 0.9f, 0.4f })
            {
                var L = PlumeShape.ComputeLayout(tf, 1f, d, R, H, Deck);
                Assert.That(L.Length, Is.LessThanOrEqualTo(80f), $"tf {tf}: dense body <= 80 m at sea level");
                Assert.That(PlumeShape.NearOverlap(L, b.Body, 3f * d), Is.GreaterThanOrEqualTo(2f), $"level {level} tf {tf}: >=2x sprite overlap in first 3 D");
                Assert.That(PlumeShape.NearNozzleGap(L), Is.LessThanOrEqualTo(0.5f), $"tf {tf}: near-nozzle gap <= 0.5 m");
            }
            float full = PlumeShape.ComputeLayout(1f, 1f, d, R, H, Deck).Length, low = PlumeShape.ComputeLayout(0.4f, 1f, d, R, H, Deck).Length;
            Assert.That(full, Is.GreaterThan(low), "throttle still lengthens the dense body");
            Assert.That(PlumeShape.ComputeLayout(1f, 0.01f, d, R, H, Deck).Length, Is.GreaterThan(full), "near-vacuum still lengthens it");
        }

        [TestCase(1f, 1f), TestCase(0.4f, 1f), TestCase(1f, 0.3f), TestCase(1f, 0.01f), TestCase(1f, 0f)]
        public void PlumeTopEdgeStaysBelowNozzlePlane(float throttle, float pa)
        {
            var L = PlumeShape.ComputeLayout(throttle, pa, ClusterD, R, H, Deck);
            float tol = PlumeShape.TopEdgeRelTolerance * H;   // float rounding (~3e-8) relative to vehicle height
            Assert.That(PlumeShape.PlumeTopEdge(L), Is.LessThanOrEqualTo(PlumeShape.NozzleOverlapD * ClusterD + tol), "#129-v7: nothing visible above the nozzle plane (overlap allowance is 0)");
            Assert.That(PlumeShape.NozzleOverlapD, Is.EqualTo(0f));
            Assert.That(L.BodyEmitOffset, Is.GreaterThanOrEqualTo(0f)); Assert.That(L.CoreEmitOffset, Is.GreaterThanOrEqualTo(0f));
        }

        // #119 v3: gap between the nozzles and the body.
        [TestCase(1f, 1f), TestCase(0.4f, 1f), TestCase(1f, 0.01f)]
        public void BodyStartsAtTheNozzleExit(float throttle, float pa)
        {
            foreach (var d in new[] { ClusterD, 12f })
            {
                var L = PlumeShape.ComputeLayout(throttle, pa, d, R, H, Deck);
                // #129: the core covers the exit zone; the solid body must begin inside it (no dark gap between them).
                float coreEnd = L.CoreEmitOffset + L.CoreLength + 0.5f * PlumeShape.PlumeSolidFraction * L.CoreSize * PlumeShape.CoreSizeOverLife;
                Assert.That(PlumeShape.BodyStartGap(L), Is.LessThanOrEqualTo(coreEnd), $"d={d}: body starts inside the core zone");
                Assert.That(PlumeShape.BodySizeStart, Is.GreaterThanOrEqualTo(0.8f), "born near full size: in-frame sprites are young (#129 half-width)");
                float nearSolid = L.BodyMin * PlumeShape.PlumeSolidFraction * PlumeShape.BodySizeStart;
                Assert.That(nearSolid / d, Is.InRange(0.8f, 1.4f), $"d={d}: visible body near the nozzles ~1.0-1.4x cluster");
                Assert.That(PlumeShape.CoreStartGap(L), Is.LessThanOrEqualTo(0.05f * d), $"d={d}: core begins at the exit");
                Assert.That(PlumeShape.PlumeTopEdge(L), Is.LessThanOrEqualTo(PlumeShape.NozzleOverlapD * d + PlumeShape.TopEdgeRelTolerance * H), $"d={d}");
            }
        }

        [Test]
        public void GroundCloudHoldsSixToTenSecondsAfterLiftoff()
        {
            const float tower = 80f, high = 2000f;   // vehicle long past the tower
            Assert.That(PlumeShape.GroundCloud(1f, high, tower, 2.7f), Is.EqualTo(1f), "still full at T+2.7 (was gone)");
            Assert.That(PlumeShape.GroundCloud(1f, high, tower, 5f), Is.EqualTo(1f));
            Assert.That(PlumeShape.GroundCloud(1f, high, tower, 12f), Is.EqualTo(0f), "emission ends after the hold");
            float lastVisible = PlumeShape.CloudHoldSeconds + PlumeShape.CloudFadeSeconds + PlumeShape.CloudLifeMax;
            Assert.That(PlumeShape.CloudHoldSeconds + PlumeShape.CloudLifeMin, Is.GreaterThanOrEqualTo(6f));
            Assert.That(lastVisible, Is.InRange(8f, 12.5f));
            foreach (var k in PlumeShape.CloudAlphaKeys) if (k.x < 0.7f) Assert.That(k.y, Is.GreaterThanOrEqualTo(0.85f), "dense, not a haze");
        }

        [Test]
        public void CloudVelocityCurvesShareOneMode()
        {
            var go = new UnityEngine.GameObject("cloud-test");
            try
            {
                var ps = go.AddComponent<UnityEngine.ParticleSystem>();
                PlumeController.ConfigureCloudVelocity(ps, 45f);
                var v = ps.velocityOverLifetime;
                foreach (var c in new[] { v.y, v.z, v.orbitalX, v.orbitalY, v.orbitalZ, v.radial })
                    Assert.That(c.mode, Is.EqualTo(v.x.mode));
                Assert.That(ps.limitVelocityOverLifetime.separateAxes, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void CoreIsSmallAndCapped(int level)
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            Assert.That(L.CoreSize * PlumeShape.PlumeSolidFraction, Is.LessThanOrEqualTo(1.0f * ClusterD), "core solid size <= cluster");
            Assert.That(L.CoreLength, Is.LessThanOrEqualTo(0.15f * L.Length), "core is a small zone at the nozzle");
            Assert.That(b.Core, Is.LessThanOrEqualTo(PlumeShape.MaxCoreParticles));
            Assert.That(PlumeShape.CoreHdrHigh, Is.InRange(1.2f, 1.8f));
            Assert.That(PlumeShape.CoreHdrLow, Is.InRange(1.0f, 1.2f));
            Assert.That(PlumeShape.CoreAlphaMax, Is.LessThanOrEqualTo(0.85f), "alpha-blended core (#129)");
            Assert.That(PlumeShape.CorePeak(L, b.Core, level >= 2), Is.LessThanOrEqualTo(6f), $"level {level}: core overlap bounded (alpha-blended: saturates at its colour, not white)");
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void GroundCloudReadableWithEngineClearZone(int level)
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            Assert.That(L.CloudRadius / Deck, Is.InRange(2f, 4f), "cloud spans 2-4x deck radius");
            Assert.That(L.CloudSizeMax * PlumeShape.SpriteSolidFraction / H, Is.InRange(0.18f, 0.3f), "billows read at phone size without walling off the frame (v5 sim)");
            Assert.That(b.Cloud, Is.GreaterThanOrEqualTo(PlumeShape.MinCloud));
            Assert.That(PlumeShape.CloudCoverage(L, b.Cloud), Is.GreaterThanOrEqualTo(0.9f), $"level {level}: cloud disc covered");
            Assert.That(L.CloudSpawnRadius, Is.GreaterThanOrEqualTo(Deck), "spawns at/outside the deck edge");
            // v5 root cause B: the emitter was at -0.1 x deck (-1.5 m) under PadGround.GroundY (-1 m), so the ground
            // depth-occluded the billows and only a thin line showed. Centre must sit above the deck top by the solid
            // half of a newborn billow.
            Assert.That(L.CloudHeight, Is.GreaterThan(0f), "emitter above the deck top");
            Assert.That(L.CloudHeight, Is.GreaterThan(Kestrel.Slice.PadGround.GroundY), "emitter above the ground plane");
            // #129: low and outward so newborn billows don't cover the nozzles in the first second.
            Assert.That(L.CloudHeight, Is.LessThanOrEqualTo(1f), "centre near the deck top");
            Assert.That(L.CloudHeight - Kestrel.Slice.PadGround.GroundY, Is.GreaterThanOrEqualTo(0.5f * PlumeShape.SpriteSolidFraction * L.CloudSizeMax * L.CloudSizeStart * 0.4f), "most of a newborn billow above the ground");
            Assert.That(L.CloudSpawnRadius, Is.GreaterThanOrEqualTo(1.5f * Deck), "spawn ring pushed outward");
            Assert.That(L.CloudClearRadius, Is.GreaterThanOrEqualTo(1.5f * R), "clear zone covers the vehicle base");
            Assert.That(PlumeShape.CloudInnerEdge(L), Is.GreaterThanOrEqualTo(L.CloudClearRadius - 1e-3f), "no billow inside the engine clear zone");
        }

        [Test]
        public void GroundCloudRampsUpAfterIgnition()
        {
            Assert.That(PlumeShape.GroundCloud(1f, 0f, 80f, 0f), Is.EqualTo(0f));
            Assert.That(PlumeShape.GroundCloud(1f, 0f, 80f, 0.1f), Is.InRange(0.05f, 0.9f));
            Assert.That(PlumeShape.GroundCloud(1f, 0f, 80f, 0.4f), Is.EqualTo(1f), "full by T+0.4 (was ~0.3: the haze line)");
            Assert.That(PlumeShape.CloudRampSeconds, Is.InRange(0.1f, 0.4f));
            Assert.That(PlumeShape.CloudIgnitionBurst, Is.InRange(0.2f, 0.6f));
        }

        // v5 root cause A: SpriteSolidFraction (0.6) did not match the sprite. Pin both fractions to Falloff.
        [Test]
        public void SpriteFractionsMatchFalloff()
        {
            Assert.That(SoftParticle.Falloff(PlumeShape.SpriteSolidFraction), Is.EqualTo(0.5f).Within(0.02f), "alpha 0.5 at the solid radius");
            Assert.That(SoftParticle.Falloff(PlumeShape.SpriteVisibleFraction), Is.InRange(0.002f, 0.02f), "~1% alpha at the visible radius");
        }

        // v5 root cause D (sprites under the deck drew over it) + #129 (killing them left 0/208 live on the pad):
        // deflect with low bounce and high lifetime loss; emitter at the exit plane so particles start above the deck.
        [Test]
        public void BodyDeflectsAtTheDeck()
        {
            var root = new UnityEngine.GameObject("plume-test"); var deck = new UnityEngine.GameObject("deck");
            try
            {
                var pc = PlumeController.Create(root.transform, UnityEngine.Vector3.zero, 14f, null);
                pc.SetDeck(deck.transform);
                Assert.That(pc.Body.collision.enabled, Is.True);
                var c = pc.Body.collision;
                Assert.That(c.lifetimeLoss.constant, Is.InRange(0.5f, 0.9f), "short-lived, faded after contact, not killed");
                Assert.That(c.bounce.constant, Is.LessThanOrEqualTo(0.1f), "low bounce: slides sideways");
                pc.Apply(0.9, PlumeShape.P0, 0);
                Assert.That(pc.Body.transform.localPosition.magnitude, Is.LessThan(1e-4f), "body emitter at the nozzle plane");
                Assert.That(pc.Emitters[0].transform.localPosition.magnitude, Is.LessThan(1e-4f), "core emitter at the nozzle plane");
                Assert.That(pc.Emitters[0].main.gravityModifier.constant, Is.EqualTo(0f));
                Assert.That(pc.Emitters[0].main.simulationSpace, Is.EqualTo(UnityEngine.ParticleSystemSimulationSpace.Local));
                Assert.That(pc.Emitters[0].main.startSpeed.constant * PlumeShape.CoreLife, Is.LessThanOrEqualTo(1.1f * 14f), "core drifts <= ~1 D: stays attached");
                Assert.That(pc.Emitters[2].transform.localPosition.magnitude, Is.LessThan(1e-4f), "#129-v7: diamonds at the nozzle plane (was -2.25 m)");
                Assert.That(c.bounce.constant, Is.EqualTo(0f), "#129-v7: deflected spray must not climb");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(deck); }
        }

        [Test]
        public void BodyFadeInMatchesOffsetSilhouette()
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, 14f, 7f, H, 20f);
            Assert.That(PlumeShape.BodyFadeIn(L) * L.Length, Is.EqualTo(L.BodyEmitOffset).Within(1e-3f));
            Assert.That(PlumeShape.BodyFadeIn(L), Is.LessThan(0.35f));
        }

        // #129: QualityGovernor stepped Medium -> Low mid-liftoff and the plume rebuilt with Low caps.
        [Test]
        public void PlumeFollowsGovernorHold() => Assert.That(Kestrel.Game.Quality.QualityGovernor.LiftoffHoldSeconds, Is.GreaterThanOrEqualTo(20.0));

        // #129-v7: the linear 0 -> peak fade-in showed alpha while sprites still reached above the nozzle plane.
        [Test]
        public void FadeKeysAreZeroUntilFadeIn()
        {
            var k = PlumeController.FadeKeys(0.2f, 0.9f, (0.6f, 0.4f));
            var g = new UnityEngine.Gradient(); g.SetKeys(new[] { new UnityEngine.GradientColorKey(UnityEngine.Color.white, 0) }, k);
            Assert.That(g.Evaluate(0.1f).a, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(g.Evaluate(0.2f).a, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(g.Evaluate(0.25f).a, Is.EqualTo(0.9f).Within(0.05f));
            var L = PlumeShape.ComputeLayout(1f, 1f, 14f, 7f, H, 20f);
            Assert.That(PlumeShape.BodyFadeRamp(L) * L.Length, Is.LessThanOrEqualTo(0.11f * 14f), "body ramp ~0.1 D of travel");
            Assert.That(PlumeShape.BodyTailAlpha, Is.LessThanOrEqualTo(0.05f));
            Assert.That(1f - PlumeShape.BodyDeckLifetimeLoss, Is.LessThanOrEqualTo(1f - PlumeShape.BodyTailAt), "deflected particles land in the invisible tail");
        }

        [Test]
        public void TightSpriteFractionsMatch()
        {
            Assert.That(SoftParticle.TightFalloff(PlumeShape.PlumeSolidFraction), Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(SoftParticle.TightFalloff(PlumeShape.PlumeFringeFraction), Is.LessThanOrEqualTo(0.06f));
            Assert.That(SoftParticle.TightFalloff(0.75f), Is.EqualTo(0f).Within(1e-5f));
        }

        // #129-v7 probe/sim: 14-32 m/s launch with ~1/s damping flung billows 19 m out in 0.55 s (thin strip).
        [Test]
        public void CloudLaunchIsGentle() => Assert.That(PlumeShape.CloudSpeedMax, Is.LessThanOrEqualTo(0.3f));

        // #129: _SOFTPARTICLES_ON was still on the core/body; core was additive (adds the blue sky: "blue core").
        [Test]
        public void SoftParticlesOffAndCoreAlphaBlended()
        {
            var sh = UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) Assert.Ignore("URP particle shader not available");
            var t = new UnityEngine.Material(sh);
            try
            {
                foreach (var m in new[] { SoftParticle.PlumeMaterial(t), SoftParticle.CoreMaterial(t, true), SoftParticle.CloudMaterial(t) })
                {
                    Assert.That(m.IsKeywordEnabled("_SOFTPARTICLES_ON"), Is.False, m.name);
                    Assert.That(m.GetFloat("_SoftParticlesEnabled"), Is.EqualTo(0f), m.name);
                    Assert.That(m.GetInt("_DstBlend"), Is.EqualTo((int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha), m.name + " alpha-blended");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(t); }
        }

        [Test]
        public void TransparentKeywordsSetFromCode()
        {
            var sh = UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) Assert.Ignore("URP particle shader not available");
            var m = new UnityEngine.Material(sh);
            try
            {
                SoftParticle.EnsureTransparent(m, additive: false);
                Assert.That(m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True);
                Assert.That(m.GetTag("RenderType", false), Is.EqualTo("Transparent"));
                Assert.That(m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(m); }
        }

        [Test]
        public void StillWidensWithAltitude()
        {
            float prev = 0f;
            foreach (float pa in new[] { 1f, 0.5f, 0.2f, 0.05f, 0.01f, 0f })
            {
                float w = PlumeShape.BodyVisibleWidth(PlumeShape.ComputeLayout(1f, pa, ClusterD, R, H, Deck));
                Assert.That(w, Is.GreaterThanOrEqualTo(prev - 1e-3f), $"pa {pa}");
                prev = w;
            }
            Assert.That(prev, Is.GreaterThan(1.5f * PlumeShape.BodyVisibleWidth(PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck))));
        }
    }
}
