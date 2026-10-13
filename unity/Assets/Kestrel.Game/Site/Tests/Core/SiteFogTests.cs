using System.IO;
using NUnit.Framework;

namespace Kestrel.Site.Tests
{
    public class SiteFogTests
    {
        [Test]
        public void WeightIsLinearBetweenStartAndEndAndOffWhenDisabled()
        {
            Assert.That(SiteFog.Weight(1000f, 5000f, 15000f, false), Is.EqualTo(0f));
            Assert.That(SiteFog.Weight(5000f, 5000f, 15000f, true), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(SiteFog.Weight(10000f, 5000f, 15000f, true), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(SiteFog.Weight(20000f, 5000f, 15000f, true), Is.EqualTo(1f));
            Assert.That(SiteFog.Weight(0f, 10f, 10f, true), Is.EqualTo(0f));
        }

        [Test]
        public void CoastShadersUseOneFogVariant()
        {
            foreach (var rel in new[]
            {
                "Kestrel.Game/Site/Runtime/Resources/Site/KH_Terrain.shader",
                "Kestrel.Game/Site/Runtime/Resources/Site/KH_Water.shader",
            })
            {
                string text = ReadAsset(rel);
                Assert.That(text, Does.Not.Contain("multi_compile_fog"), rel);
                Assert.That(text, Does.Contain("_FogRange"), rel);
                Assert.That(text, Does.Contain("SiteFog.Weight"), rel);
            }
        }

        static string ReadAsset(string underAssets)
        {
            string a = Path.Combine("Assets", underAssets);
            if (File.Exists(a)) return File.ReadAllText(a);
            string b = Path.Combine("unity", "Assets", underAssets);
            if (File.Exists(b)) return File.ReadAllText(b);
            Assert.Fail("missing " + underAssets);
            return "";
        }
    }
}
