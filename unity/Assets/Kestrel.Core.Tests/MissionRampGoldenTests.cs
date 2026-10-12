using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionRampGoldenTests
    {
        [Test]
        public void Ramp_Stars_AndDrift_MatchJs()
        {
            MissionRamp ramp = null;
            int n = 0;
            foreach (var c in Golden.Cases("mission-ramp"))
            {
                string fn = c.Str("fn");
                if (fn == "fallback")
                {
                    var o = c.Obj("out");
                    ramp = MissionRamp.Load(Golden.Root, o.Num("landingFuel"), o.Num("swellAmp"));
                    Assert.AreEqual(o.Num("driftWind"), MissionRamp.DriftWindFloor);
                    Assert.AreEqual(o.Num("fuelMax"), MissionRamp.FuelMax);
                    n++;
                    continue;
                }
                Assert.IsNotNull(ramp);
                if (fn == "mission")
                {
                    Assert.IsTrue(ramp.TryGet(c.Str("mission"), out var m), c.Str("mission"));
                    var o = c.Obj("out");
                    Assert.AreEqual(o.Num("wind"), m.Wind);
                    Assert.AreEqual(o.Num("landingFuel"), m.LandingFuel);
                    Assert.AreEqual(o.Num("swellAmp"), m.SwellAmp);
                }
                else if (fn == "unlock")
                {
                    Assert.AreEqual(c.Bool("out"), ramp.UnlocksNext(c.Str("reason")), c.Str("reason"));
                }
                else if (fn == "stars")
                {
                    var t = c.Obj("touch");
                    int got = ramp.Stars(t.Str("reason"), t.Str("kind"), t.Num("vy"), t.Num("dx"), t.Num("landingVy"), t.Num("landingTol"), t.Bool("tipped"), t.Bool("recovered"));
                    Assert.AreEqual((int)c.Num("out"), got, c.Str("name"));
                }
                else if (fn == "wind")
                {
                    Golden.Near("mission-ramp", ramp.WindForce(c.Num("difficultyWind"), c.Str("mission")), c["out"], 1e-9, 1e-9, c.Str("mission"));
                }
                else if (fn == "haven")
                {
                    var d = ramp.Drift(c.Num("swellT"), c.Num("difficultyWind"), c.Str("mission"));
                    var o = c.Obj("out");
                    Golden.Near("mission-ramp", d.WindForce, o["wind"], 1e-9, 1e-9, "wind");
                    Golden.Near("mission-ramp", d.DriftWind, o["driftWind"], 1e-9, 1e-9, "drift");
                    Golden.Near("mission-ramp", d.Swell, o["swell"], 1e-9, 1e-9, "swell");
                    Golden.Near("mission-ramp", d.Gust, o["gust"], 1e-9, 1e-9, "gust");
                    Golden.Near("mission-ramp", d.ForceX, o["forceX"], 1e-12, 1e-12, "force");
                    Assert.AreEqual(o.Num("fuel"), ramp.HavenFuel(c.Num("sessionFuel"), c.Str("mission")));
                }
                else Assert.Fail(fn);
                n++;
            }
            Assert.That(n, Is.GreaterThan(20));
        }
    }
}
