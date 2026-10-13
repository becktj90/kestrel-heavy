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
            // dotnet test often runs with cwd under Dotnet~/bin; walk up for the repo Assets tree.
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    foreach (var rel in new[]
                    {
                        Path.Combine("Assets", underAssets),
                        Path.Combine("unity", "Assets", underAssets),
                    })
                    {
                        string candidate = Path.Combine(dir.FullName, rel);
                        if (File.Exists(candidate)) return File.ReadAllText(candidate);
                    }
                    dir = dir.Parent;
                }
            }
            Assert.Fail("missing " + underAssets);
            return "";
        }
    }
}
