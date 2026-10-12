using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LandingGradeGoldenTests
    {
        [Test]
        public void Grades_MatchJsMargins()
        {
            var tune = LandingTune.Load(Golden.Root);
            int n = 0;
            foreach (var c in Golden.Cases("landing-grade"))
            {
                if (c.Str("fn") == "padRat")
                {
                    var o = c.Obj("out");
                    Assert.AreEqual(o.Num("padRatLandingVy"), tune.PadRatLandingVy);
                    Assert.AreEqual(o.Num("difficulty"), tune.PadRatLandingVy);
                    n++;
                    continue;
                }
                var g = tune.Grade(c.Str("kind"), c.Num("vy"), c.Num("dx"), c.Num("landingVy"), c.Num("landingTol"), c.Bool("tipped"));
                var expect = c.Obj("out");
                Golden.Near("landing-grade", g.Margins.VyMargin, expect["vyMargin"], 1e-9, 1e-9, c.Str("name") + " vy");
                Golden.Near("landing-grade", g.Margins.DxMargin, expect["dxMargin"], 1e-9, 1e-9, c.Str("name") + " dx");
                Golden.Near("landing-grade", g.Margins.Margin, expect["margin"], 1e-9, 1e-9, c.Str("name") + " margin");
                Assert.AreEqual(expect["grade"] as string, g.Name, c.Str("name"));
                n++;
            }
            Assert.That(n, Is.GreaterThanOrEqualTo(12));
        }
    }
}
