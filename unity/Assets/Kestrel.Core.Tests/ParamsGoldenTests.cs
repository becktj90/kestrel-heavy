using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Params;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ParamsGoldenTests
    {
        [Test]
        public void Registry_Sensors_Weather_MatchJs()
        {
            var reg = Registry.Load(Golden.Root);
            Assert.AreEqual("portrait", reg.Orientation);
            Assert.AreEqual("themed-fill", reg.BigScreen);
            Assert.AreEqual(120, reg.RefreshHz);
            Assert.AreEqual("Auto", reg.Quality);
            Assert.IsFalse(reg.BatterySaver);
            Assert.IsTrue(reg.SafeFlash);
            Assert.AreEqual("follow-os", reg.ReducedMotion);
            Assert.AreEqual("moderate", reg.FailurePenalty);
            Assert.AreEqual("simple", reg.DefaultLayer);
            Assert.AreEqual("first-orbit", reg.StandardUnlock);
            Assert.AreEqual("settings-toggle", reg.EngineerToggle);
            Assert.IsFalse(reg.KidHold);
            Assert.IsFalse(reg.RelaxedHold);
            Assert.AreEqual(Weather.DefaultWindLimitMs, reg.WindLimitMs);
            Assert.AreEqual(28.5, reg.LaunchLatitude, 1e-12);
            Assert.AreEqual(Sensors.AltitudeRel, reg.Raw.Obj("sensors").Num("altitudeRel"));
            Assert.AreEqual(Sensors.SpeedRel, reg.Raw.Obj("sensors").Num("speedRel"));
            Assert.AreEqual(Sensors.AngleDeg, reg.Raw.Obj("sensors").Num("angleDeg"));

            foreach (JsonObject c in Golden.Cases("params"))
            {
                if (c.Str("kind") == "registry") JsonSame.Equal(reg.Raw, c["out"], "registry");
                if (c.Str("kind") == "fidelity")
                    foreach (JsonObject row in c.Arr("cases"))
                        Assert.AreEqual(row.Str("mode"), Sensors.Fidelity(row.Str("difficulty"), row.Bool("sandbox"), row.Str("layer")));
                if (c.Str("kind") == "sense")
                {
                    var truth = c.Obj("truth");
                    var exact = Sensors.Sense(truth.Num("altitude"), truth.Num("speed"), truth.Num("angleDeg"), "KID", false, "standard", 1, truth.Num("mass"), truth.Num("propellant"));
                    SameReading(exact, c.Obj("exact"));
                    foreach (JsonObject row in c.Arr("cases"))
                    {
                        var got = Sensors.Sense(truth.Num("altitude"), truth.Num("speed"), truth.Num("angleDeg"), "CADET", false, "standard", (uint)row.Num("seed"), truth.Num("mass"), truth.Num("propellant"));
                        SameReading(got, row.Obj("out"));
                    }
                }
                if (c.Str("kind") == "weather")
                    foreach (JsonObject row in c.Arr("cases"))
                    {
                        var got = Weather.Hold(row.Str("difficulty"), row.Bool("relaxed"), row.Num("windMs"));
                        var exp = row.Obj("out");
                        Assert.AreEqual(exp.Bool("hold"), got.hold, row.Str("difficulty"));
                        Assert.AreEqual(exp.Bool("allowed"), got.allowed, row.Str("difficulty"));
                    }
            }
        }

        [Test]
        public void SampleDensity_MatchesSequentialAndGolden()
        {
            var alts = new System.Collections.Generic.List<double>();
            foreach (JsonObject c in Golden.Cases("atmosphere")) alts.Add(c.Obj("in").Num("z"));
            while (alts.Count < 32) alts.Add(alts[alts.Count - 1] + 1000);
            var z = alts.ToArray();
            var rho = new double[z.Length];
            Atmosphere.SampleDensity(z, rho);
            for (int i = 0; i < z.Length; i++)
                Assert.AreEqual(Atmosphere.At(z[i]).rho, rho[i], "sample " + i);
            int n = 0;
            foreach (JsonObject c in Golden.Cases("atmosphere"))
            {
                Golden.Near("atmosphere", rho[n], c.Obj("out")["rho"], 1e-9, 1e-9, "batch rho " + n);
                n++;
            }
        }

        static void SameReading(Sensors.Reading got, JsonObject exp)
        {
            Assert.AreEqual(exp.Str("mode"), got.mode);
            Assert.AreEqual(exp.Num("altitude"), got.altitude);
            Assert.AreEqual(exp.Num("speed"), got.speed);
            Assert.AreEqual(exp.Num("angleDeg"), got.angleDeg);
            Assert.AreEqual(exp.Num("mass"), got.mass);
            Assert.AreEqual(exp.Num("propellant"), got.propellant);
        }
    }
}
