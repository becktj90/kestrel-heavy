using System.Linq;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class TimeOfDayTests
    {
        static readonly LaunchTimeOfDay[] All = { LaunchTimeOfDay.Day, LaunchTimeOfDay.Sunset, LaunchTimeOfDay.Night };

        static bool In01(Rgb3 c) => c.R >= 0 && c.R <= 1 && c.G >= 0 && c.G <= 1 && c.B >= 0 && c.B <= 1;
        static float Lum(Rgb3 c) => 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;

        [Test]
        public void PresetValuesAreSaneForEverySeed()
        {
            foreach (var t in All)
                for (int seed = 0; seed < 200; seed++)
                {
                    var p = TimeOfDayPresets.For(t, seed);
                    Assert.That(p.Tod, Is.EqualTo(t));
                    Assert.That(p.SunElevationDeg, Is.InRange(-90f, 90f)); Assert.That(p.SunAzimuthDeg, Is.InRange(0f, 360f));
                    Assert.That(p.SunIntensity, Is.InRange(0f, 3f)); Assert.That(p.ShadowStrength, Is.InRange(0f, 1f));
                    Assert.That(p.AmbientIntensity, Is.InRange(0f, 2f)); Assert.That(p.StarDensity, Is.InRange(0f, 1f));
                    Assert.That(p.FogDensity, Is.InRange(0.1f, 3f)); Assert.That(p.Exposure, Is.InRange(0.5f, 2f));
                    Assert.That(p.SunTintMix, Is.InRange(0f, 1f)); Assert.That(p.FogTintMix, Is.InRange(0f, 1f));
                    foreach (var c in new[] { p.SunColor, p.AmbientSky, p.AmbientEquator, p.AmbientGround, p.FogColor, p.SkyZenith, p.SkyHorizon, p.HazeColor }) Assert.That(In01(c), Is.True, $"{t}/{seed}");
                }
        }

        [Test]
        public void ThePresetsTellDayFromSunsetFromNight()
        {
            var d = TimeOfDayPresets.Base(LaunchTimeOfDay.Day); var s = TimeOfDayPresets.Base(LaunchTimeOfDay.Sunset); var n = TimeOfDayPresets.Base(LaunchTimeOfDay.Night);
            Assert.That(d.SunElevationDeg, Is.GreaterThan(s.SunElevationDeg)); Assert.That(s.SunElevationDeg, Is.GreaterThan(n.SunElevationDeg));
            Assert.That(d.SunIntensity, Is.GreaterThan(s.SunIntensity)); Assert.That(s.SunIntensity, Is.GreaterThan(n.SunIntensity));
            Assert.That(d.StarDensity, Is.LessThan(s.StarDensity)); Assert.That(s.StarDensity, Is.LessThan(n.StarDensity));
            Assert.That(Lum(d.AmbientSky), Is.GreaterThan(Lum(n.AmbientSky)));
            Assert.That(s.SunColor.R, Is.GreaterThan(s.SunColor.B * 2f), "sunset sun is warm");
            Assert.That(s.HazeColor.R, Is.GreaterThan(s.HazeColor.B), "sunset haze is warm");
            Assert.That(n.NightFactor, Is.EqualTo(1f)); Assert.That(d.NightFactor, Is.EqualTo(0f));
            Assert.That(s.NightFactor, Is.InRange(0.2f, 0.7f));
            Assert.That(n.ShadowStrength, Is.EqualTo(0f));
        }

        [Test]
        public void VarietyStaysInsideEachPresetsBand()
        {
            for (int seed = 0; seed < 300; seed++)
            {
                Assert.That(TimeOfDayPresets.For(LaunchTimeOfDay.Day, seed).NightFactor, Is.EqualTo(0f));
                Assert.That(TimeOfDayPresets.For(LaunchTimeOfDay.Sunset, seed).SunElevationDeg, Is.InRange(0.4f, 5.6f));
                Assert.That(TimeOfDayPresets.For(LaunchTimeOfDay.Night, seed).NightFactor, Is.EqualTo(1f));
            }
            Assert.That(Enumerable.Range(0, 20).Select(s => TimeOfDayPresets.For(LaunchTimeOfDay.Sunset, s).SunElevationDeg).Distinct().Count(), Is.GreaterThan(10));
        }

        [Test]
        public void NightFactorIsMonotonicAndBounded()
        {
            float prev = TimeOfDayPresets.NightFactor(90f);
            for (float e = 89f; e >= -90f; e -= 0.5f)
            {
                float n = TimeOfDayPresets.NightFactor(e);
                Assert.That(n, Is.GreaterThanOrEqualTo(prev), $"elev {e}"); Assert.That(n, Is.InRange(0f, 1f)); prev = n;
            }
            Assert.That(TimeOfDayPresets.NightFactor(6f), Is.EqualTo(0f)); Assert.That(TimeOfDayPresets.NightFactor(-6f), Is.EqualTo(1f));
            Assert.That(TimeOfDayPresets.NightFactor(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void PresetForAPickedValueIsDeterministicPerSeed()
        {
            foreach (var t in All)
                foreach (ulong seed in new ulong[] { 0, 1, 42, 999983, ulong.MaxValue })
                {
                    var a = TimeOfDayPresets.For(t, TimeOfDayPresets.VarietySeed(seed)); var b = TimeOfDayPresets.For(t, TimeOfDayPresets.VarietySeed(seed));
                    Assert.That(a.Tod, Is.EqualTo(t)); Assert.That(a.SunElevationDeg, Is.EqualTo(b.SunElevationDeg)); Assert.That(a.StarDensity, Is.EqualTo(b.StarDensity));
                }
            Assert.That(TimeOfDayPresets.VarietySeed(7UL), Is.EqualTo(TimeOfDayPresets.VarietySeed(7UL)));
            Assert.That(TimeOfDayPresets.VarietySeed(7UL), Is.Not.EqualTo(TimeOfDayPresets.VarietySeed(8UL)));
        }

        [Test]
        public void CommandLineParsing()
        {
            Assert.That(TimeOfDayPresets.TryParseArgs(new[] { "app", "-khTimeOfDay=night" }, out var t, out _), Is.True); Assert.That(t, Is.EqualTo(LaunchTimeOfDay.Night));
            Assert.That(TimeOfDayPresets.TryParseArgs(new[] { "-khtimeofday", "SUNSET", "-khTodSeed=9" }, out t, out var seed), Is.True);
            Assert.That(t, Is.EqualTo(LaunchTimeOfDay.Sunset)); Assert.That(seed, Is.EqualTo(9));
            Assert.That(TimeOfDayPresets.TryParseArgs(new[] { "-khTimeOfDay=day" }, out t, out _), Is.True); Assert.That(t, Is.EqualTo(LaunchTimeOfDay.Day));
            Assert.That(TimeOfDayPresets.TryParseArgs(new[] { "-khTimeOfDay=midnightish" }, out _, out _), Is.False);
            Assert.That(TimeOfDayPresets.TryParseArgs(new[] { "-other" }, out _, out _), Is.False);
            Assert.That(TimeOfDayPresets.TryParseArgs(null, out _, out _), Is.False);
        }
    }
}
