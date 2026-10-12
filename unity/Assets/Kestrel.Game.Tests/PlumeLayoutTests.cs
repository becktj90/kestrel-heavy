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

        [TestCase(1f, 1f), TestCase(0.4f, 1f), TestCase(1f, 0.3f), TestCase(1f, 0.01f), TestCase(1f, 0f)]
        public void PlumeTopEdgeStaysBelowNozzlePlane(float throttle, float pa)
        {
            var L = PlumeShape.ComputeLayout(throttle, pa, ClusterD, R, H, Deck);
            float tol = PlumeShape.TopEdgeRelTolerance * H;   // float rounding (~3e-8) relative to vehicle height
            Assert.That(PlumeShape.PlumeTopEdge(L), Is.LessThanOrEqualTo(tol), "no visible plume sprite above the nozzle exit");
            Assert.That(L.BodyEmitOffset, Is.GreaterThan(0f)); Assert.That(L.CoreEmitOffset, Is.GreaterThan(0f));
        }

        // #119 v3: gap between the nozzles and the body.
        [TestCase(1f, 1f), TestCase(0.4f, 1f), TestCase(1f, 0.01f)]
        public void BodyStartsAtTheNozzleExit(float throttle, float pa)
        {
            foreach (var d in new[] { ClusterD, 12f })
            {
                var L = PlumeShape.ComputeLayout(throttle, pa, d, R, H, Deck);
                Assert.That(PlumeShape.BodyStartGap(L), Is.LessThanOrEqualTo(0.1f * d), $"d={d}: solid body begins within 0.1 D of the exit");
                Assert.That(PlumeShape.PlumeTopEdge(L), Is.LessThanOrEqualTo(PlumeShape.TopEdgeRelTolerance * H), $"d={d}");
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
            Assert.That(L.CoreSize * PlumeShape.SpriteSolidFraction, Is.LessThanOrEqualTo(1.0f * ClusterD), "core solid size <= cluster");
            Assert.That(L.CoreLength, Is.LessThanOrEqualTo(0.15f * L.Length), "core is a small zone at the nozzle");
            Assert.That(b.Core, Is.LessThanOrEqualTo(PlumeShape.MaxCoreParticles));
            Assert.That(PlumeShape.CoreHdrHigh, Is.InRange(1.2f, 1.8f));
            Assert.That(PlumeShape.CoreHdrLow, Is.InRange(1.0f, 1.2f));
            Assert.That(PlumeShape.CoreAlphaMax, Is.LessThanOrEqualTo(0.5f));
            Assert.That(PlumeShape.CorePeak(L, b.Core, level >= 2), Is.LessThanOrEqualTo(2.5f), $"level {level}: additive core must not stack to flat white");
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void GroundCloudReadableWithEngineClearZone(int level)
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            Assert.That(L.CloudRadius / Deck, Is.InRange(2f, 4f), "cloud spans 2-4x deck radius");
            Assert.That(L.CloudSizeMax * PlumeShape.SpriteSolidFraction / H, Is.GreaterThanOrEqualTo(0.3f), "billows read at phone size");
            Assert.That(b.Cloud, Is.GreaterThanOrEqualTo(PlumeShape.MinCloud));
            Assert.That(PlumeShape.CloudCoverage(L, b.Cloud), Is.GreaterThanOrEqualTo(0.9f), $"level {level}: cloud disc covered");
            Assert.That(L.CloudSpawnRadius, Is.GreaterThanOrEqualTo(Deck), "spawns at/outside the deck edge");
            Assert.That(L.CloudHeight, Is.LessThanOrEqualTo(0f), "spawns at or below the deck top (outward and down)");
            Assert.That(L.CloudClearRadius, Is.GreaterThanOrEqualTo(1.5f * R), "clear zone covers the vehicle base");
            Assert.That(PlumeShape.CloudInnerEdge(L), Is.GreaterThanOrEqualTo(L.CloudClearRadius - 1e-3f), "no billow inside the engine clear zone");
        }

        [Test]
        public void GroundCloudRampsUpAfterIgnition()
        {
            Assert.That(PlumeShape.GroundCloud(1f, 0f, 80f, 0f), Is.EqualTo(0f));
            Assert.That(PlumeShape.GroundCloud(1f, 0f, 80f, 0.3f), Is.InRange(0.05f, 0.9f));
            Assert.That(PlumeShape.GroundCloud(1f, 0f, 80f, 1f), Is.EqualTo(1f));
            Assert.That(PlumeShape.CloudRampSeconds, Is.InRange(0.5f, 1f));
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
