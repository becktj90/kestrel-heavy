using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    // Regression: plume particles were untextured square quads (blocky, stair-stepped edge).
    public class PlumeSoftEdgeTests
    {
        [Test]
        public void FalloffIsOpaqueAtCentreZeroAtEdgeAndMonotonic()
        {
            Assert.That(SoftParticle.Falloff(0f), Is.EqualTo(1f).Within(1e-5));
            Assert.That(SoftParticle.Falloff(1f), Is.EqualTo(0f));
            Assert.That(SoftParticle.Falloff(1.42f), Is.EqualTo(0f), "quad corners must be fully transparent");
            float prev = 1f;
            for (int i = 1; i <= 100; i++) { float a = SoftParticle.Falloff(i / 100f); Assert.That(a, Is.LessThanOrEqualTo(prev)); prev = a; }
        }

        [Test]
        public void FalloffHasNoHardStep()
        {
            // Max alpha change between adjacent texels of the 64 px sprite stays small: no visible edge.
            float step = 2f / (SoftParticle.Size - 1);
            for (float r = 0; r < 1.2f; r += step)
                Assert.That(Mathf.Abs(SoftParticle.Falloff(r) - SoftParticle.Falloff(r + step)), Is.LessThan(0.08f));
        }

        [Test]
        public void TextureIsFilteredClampedAndMipmapped()
        {
            var t = SoftParticle.Texture;
            Assert.That(t.filterMode, Is.Not.EqualTo(FilterMode.Point));
            Assert.That(t.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(t.mipmapCount, Is.GreaterThan(1));
        }
    }
}
