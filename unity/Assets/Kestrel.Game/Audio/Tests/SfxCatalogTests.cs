using NUnit.Framework;

namespace Kestrel.Game.Audio.Tests
{
    public class SfxCatalogTests
    {
        [Test]
        public void WebKeysResolveAndThemeIsNotACue()
        {
            Assert.IsTrue(SfxCatalog.TryResolve("liftoff", out var liftoff));
            Assert.AreEqual("liftoff", liftoff.File);
            Assert.AreEqual(0.72f, liftoff.Volume);
            Assert.IsTrue(liftoff.Critical);
            Assert.IsFalse(liftoff.Air);

            Assert.IsTrue(SfxCatalog.TryResolve("separation", out var sep));
            Assert.AreEqual("whoosh", sep.File);
            Assert.IsTrue(sep.Air);

            Assert.IsTrue(SfxCatalog.TryResolve("ignition", out var ignite));
            Assert.AreEqual("quindar", ignite.File);
            Assert.AreEqual("ignite", ignite.Name);

            Assert.IsTrue(SfxCatalog.TryResolve("UI", out var ui));
            Assert.AreEqual("ui", ui.Name);

            Assert.IsFalse(SfxCatalog.TryResolve("theme", out _));
            Assert.IsFalse(SfxCatalog.TryResolve("flight.begin", out _));
            Assert.IsFalse(SfxCatalog.TryResolve("", out _));
        }

        [Test]
        public void LoopBedsAreResourcesAndNotOneShots()
        {
            CollectionAssert.Contains(SfxCatalog.Files, "roar-loop");
            CollectionAssert.Contains(SfxCatalog.Files, "burn-loop");
            CollectionAssert.DoesNotContain(SfxCatalog.Files, "theme");
            Assert.IsFalse(SfxCatalog.TryResolve("roar-loop", out _));
        }
    }
}
