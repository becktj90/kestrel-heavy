using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // P0 fix 2: the plume drew through the pad deck (no deck collision, no soft-particle depth fade).
    public class PlumeDepthTests
    {
        [Test]
        public void DeckCollisionIsPlanesModeOnDeckTop()
        {
            var deck = new GameObject("DeckTop").transform;
            var go = new GameObject("ps"); var ps = go.AddComponent<ParticleSystem>();
            try
            {
                PlumeController.ConfigureDeckCollision(ps, deck);
                var c = ps.collision;
                Assert.That(c.enabled, Is.True);
                Assert.That(c.type, Is.EqualTo(ParticleSystemCollisionType.Planes));
                Assert.That(c.GetPlane(0), Is.SameAs(deck));
                Assert.That(c.bounce.constant, Is.LessThan(0.2f), "jet should spread along the deck, not bounce up");
                PlumeController.ConfigureDeckCollision(ps, null);
                Assert.That(ps.collision.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(deck.gameObject); }
        }

        [Test]
        public void PlumeMaterialDepthTestsAndTogglesSoftParticles()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) Assert.Ignore("URP not installed");
            var m = SoftParticle.PlumeMaterial(new Material(sh));
            Assert.That(m.renderQueue, Is.GreaterThanOrEqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent));
            Assert.That(m.GetInt("_ZWrite"), Is.EqualTo(0));
            if (m.HasProperty("_ZTest")) Assert.That(m.GetInt("_ZTest"), Is.EqualTo((int)UnityEngine.Rendering.CompareFunction.LessEqual));
            SoftParticle.ApplySoftParticles(m, true);
            Assert.That(m.IsKeywordEnabled("_SOFTPARTICLES_ON"), Is.True);
            SoftParticle.ApplySoftParticles(m, false);
            Assert.That(m.IsKeywordEnabled("_SOFTPARTICLES_ON"), Is.False);
        }
    }
}
