using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SteerGoldenTests
    {
        [Test]
        public void ExpoCurve_MatchesJs()
        {
            var tune = SteerTune.Load(Golden.Root);
            Assert.AreEqual(1.6, tune.Expo, 0);
            Assert.AreEqual(110, tune.FullPx, 0);
            Assert.AreEqual(72, tune.FullPxCompact, 0);
            int n = 0;
            foreach (var c in Golden.Cases("steer"))
            {
                string fn = c.Str("fn");
                double got;
                if (fn == "fullPx") got = tune.FullTravel(c.Num("viewportWidth"));
                else if (fn == "clamp") got = tune.ClampSensitivity(c.Num("sensitivity"));
                else if (fn == "curve") got = tune.Curve(c.Num("dx"), c.Num("sensitivity"), c.Num("viewportWidth"));
                else if (fn == "peek") got = tune.Peek(c.Num("dx"), c.Bool("armed"), c.Num("sensitivity"), c.Num("viewportWidth"));
                else { Assert.Fail(fn); return; }
                Golden.Near("steer", got, c["out"], 1e-9, 1e-9, fn);
                n++;
            }
            Assert.That(n, Is.GreaterThan(100));
        }
    }
}
