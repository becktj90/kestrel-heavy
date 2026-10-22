// Engine-free: Unity EditMode (Kestrel.Vehicle.Tests) and dotnet (Tests/Dotnet~).
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class BellFinishTests
    {
        [Test]
        public void KeroseneMethaneAndHydrogen_EachGetTheirOwnFinish()
        {
            Assert.AreEqual(BellFinish.Sooty, BellFinish.For("kerolox"));
            Assert.AreEqual(BellFinish.Clean, BellFinish.For("methalox"));
            Assert.AreEqual(BellFinish.Hydrogen, BellFinish.For("hydrolox"));
            foreach (var other in new[] { "solid-apcp", "hypergolic", "", null })
            {
                Assert.AreEqual("", BellFinish.For(other), other + " keeps the shared metal");
                Assert.IsFalse(BellFinish.TryTint(BellFinish.For(other), out _));
            }
        }

        [Test]
        public void KeroseneAndMethane_ReadApartAtAGlance()
        {
            Assert.IsTrue(BellFinish.TryTint(BellFinish.Sooty, out var kero));
            Assert.IsTrue(BellFinish.TryTint(BellFinish.Clean, out var meth));
            Assert.IsTrue(BellFinish.TryTint(BellFinish.Hydrogen, out var hydro));
            Assert.Greater(meth.Luminance - kero.Luminance, 0.4f, "sooty kerosene bell vs bright methane steel");
            Assert.Greater(meth.Smoothness - kero.Smoothness, 0.3f, "soot is matte");
            Assert.Greater(hydro.B - hydro.R, 0.15f, "hydrogen reads cool");
            foreach (var t in new[] { kero, meth, hydro })
                foreach (var v in new[] { t.R, t.G, t.B, t.Metallic, t.Smoothness }) Assert.That(v, Is.InRange(0f, 1f));
        }
    }
}
