// EditMode (and dotnet) tests for the altitude → sky colour math.
// The skybox only displays this grade; portrait size never enters it.
using System;
using System.IO;
using Kestrel.Core.Physics;
using Kestrel.Core.Sky;
using Kestrel.Core.Weather;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SkyColorTests
    {
        const double SunHigh = 55 * Math.PI / 180.0;

        [TearDown]
        public void RestoreSun() => SkySun.Publish(42 * Math.PI / 180.0);

        static SkyGrade Noon(double altM, SkyWeather? weather = null, string status = null)
            => SkyColor.At(altM, SunHigh, SkyArt.Neutral, weather ?? SkyWeather.Clear, status);

        [Test]
        public void ColumnFollowsUssa76Pressure()
        {
            Assert.AreEqual(SkyAtmosphereMode.Ussa76, SkyColor.Mode);
            Assert.AreEqual(1, SkyColor.Column(0), 1e-9);
            double p0 = Atmosphere.At(0).p;
            foreach (var z in new[] { 0.0, 5000, 11000, 20000, 50000, 100000, 200000 })
                Assert.AreEqual(Math.Min(1, Atmosphere.At(z).p / p0), SkyColor.Column(z), 1e-12, "z=" + z);
            Assert.Greater(SkyColor.Column(0), SkyColor.Column(10000));
            Assert.Greater(SkyColor.Column(10000), SkyColor.Column(30000));
            Assert.Greater(SkyColor.Column(30000), SkyColor.Column(80000));
            Assert.Less(SkyColor.Column(100000), 0.01);
        }

        [Test]
        public void SeaLevelNoonZenithIsBlueAndHorizonIsBrighter()
        {
            var g = Noon(0);
            Assert.Greater(g.Zenith.B, g.Zenith.G);
            Assert.Greater(g.Zenith.G, g.Zenith.R);
            Assert.Greater(g.Zenith.Luminance, 0.2);
            Assert.Less(g.Zenith.Luminance, 0.85);
            Assert.Greater(g.Horizon.Luminance, g.Zenith.Luminance);
            Assert.Less(g.Horizon.Saturation, g.Zenith.Saturation);
            Assert.Greater(g.SunDisc, 0.9);
            Assert.Greater(g.SunGlow, 0.4);
            Assert.AreEqual(SkyAtmosphereMode.Ussa76, g.Mode);
        }

        [Test]
        public void ZenithDarkensMonotonicallyTowardBlack()
        {
            double[] alts = { 0, 2000, 8000, 15000, 25000, 40000, 70000, 100000, 200000 };
            double prev = double.PositiveInfinity;
            bool dropped = false;
            foreach (var z in alts)
            {
                double y = Noon(z).Zenith.Luminance;
                Assert.LessOrEqual(y, prev + 1e-9, "zenith should not brighten at " + z + " m");
                if (y < prev - 1e-4) dropped = true;
                prev = y;
            }
            Assert.IsTrue(dropped);
            var space = Noon(100000);
            Assert.Less(space.Zenith.Luminance, 0.02);
            Assert.Less(space.Horizon.Luminance, 0.02);
            Assert.Greater(space.StarAlpha, 0.85);
            Assert.Less(Noon(0).StarAlpha, 0.08);
            Assert.Greater(Noon(80000).StarAlpha, Noon(20000).StarAlpha);
        }

        [Test]
        public void HorizonStaysLitLongerThanTheZenith()
        {
            var g = Noon(20000);
            Assert.Greater(g.Horizon.Luminance, g.Zenith.Luminance * 1.4);
            Assert.Greater(g.Zenith.B, g.Zenith.R);
        }

        [Test]
        public void ForcedSpaceStatusBlacksTheSkyAtThePad()
        {
            foreach (var status in new[] { "SEP", "HAVEN", "JACKLYN" })
            {
                var g = Noon(0, null, status);
                Assert.AreEqual(1, g.SpaceT, 1e-12, status);
                Assert.Less(g.Zenith.Luminance, 1e-6, status);
                Assert.Greater(g.StarAlpha, 0.9, status);
                Assert.AreEqual(0, g.CloudAlpha, 1e-9, status);
            }
        }

        [Test]
        public void NightIsDarkAndStarryOnTheGround()
        {
            var art = SkyArt.Neutral;
            art.StarFloor = 0.92;
            var g = SkyColor.At(0, -20 * Math.PI / 180.0, art, SkyWeather.Clear);
            Assert.Less(g.Zenith.Luminance, Noon(0).Zenith.Luminance * 0.25);
            Assert.Greater(g.StarAlpha, 0.5);
            Assert.AreEqual(0, g.SunDisc, 1e-6);
            Assert.Less(g.SunGlow, 0.05);
        }

        [Test]
        public void LowSunWarmsTheLimb()
        {
            var dawn = new SkyArt
            {
                Horizon = Rgb.FromHex("#f6c888"),
                Near = Rgb.FromHex("#c45a48"),
                Space = Rgb.FromHex("#05010a"),
                StarFloor = 0.22,
            };
            var g = SkyColor.At(0, 3 * Math.PI / 180.0, dawn, SkyWeather.Clear);
            Assert.Greater(g.Horizon.R, g.Horizon.B);
            Assert.Greater(g.Limb.R, g.Limb.B);
            Assert.Greater(g.Sun.R, g.Sun.B);
        }

        [Test]
        public void WeatherHooksChangeCloudsHazeAndStars()
        {
            var clear = SkyWeather.Clear;
            clear.CloudFraction = 0.1;
            clear.Precip = false;
            clear.CeilingM = 4000;
            var thick = clear;
            thick.CloudFraction = 0.9;
            var rain = thick;
            rain.Precip = true;
            var nightArt = SkyArt.Neutral;
            nightArt.StarFloor = 0.9;
            var a = SkyColor.At(0, -15 * Math.PI / 180.0, nightArt, clear);
            var b = SkyColor.At(0, -15 * Math.PI / 180.0, nightArt, thick);
            var c = SkyColor.At(0, -15 * Math.PI / 180.0, nightArt, rain);
            Assert.Greater(b.CloudAlpha, a.CloudAlpha);
            Assert.Less(b.StarAlpha, a.StarAlpha);
            Assert.Greater(c.Haze, b.Haze);
            Assert.Less(b.SunGlow, SkyColor.At(0, SunHigh, SkyArt.Neutral, clear).SunGlow);

            var low = SkyColor.At(0, SunHigh, SkyArt.Neutral, clear);
            var above = SkyColor.At(8000, SunHigh, SkyArt.Neutral, clear);
            Assert.Greater(low.CloudAlpha, 0.05);
            Assert.Less(above.CloudAlpha, 0.02);

            var day = new WeatherDay { cloudFraction = 0.4, ceilingM = 1500, precip = true, thunderDay = true, directionDeg = 200, windMs = 9 };
            var w = SkyWeather.From(day);
            Assert.AreEqual(0.52, w.CloudFraction, 1e-12);
            Assert.IsTrue(w.Precip);
            Assert.IsTrue(w.Thunder);
            Assert.AreEqual(9, w.WindMs, 1e-12);
        }

        [Test]
        public void ViewMatchesZenithAndIsDeviceIndependent()
        {
            var g = Noon(0);
            var zenith = SkyColor.View(g, 1);
            Assert.AreEqual(g.Zenith.R, zenith.R, 0.02);
            Assert.AreEqual(g.Zenith.B, zenith.B, 0.02);
            double mu = Math.Sin(25 * Math.PI / 180.0);
            var iphone = SkyColor.View(g, mu);
            var ipad = SkyColor.View(g, mu);
            Assert.AreEqual(iphone.R, ipad.R);
            Assert.AreEqual(iphone.G, ipad.G);
            Assert.AreEqual(iphone.B, ipad.B);
            Assert.Greater(g.Horizon.Luminance, SkyColor.View(g, 0.35).Luminance);
            Assert.IsTrue(SkyColor.IsPortrait(393, 852));
            Assert.IsTrue(SkyColor.IsPortrait(820, 1180));
            Assert.IsTrue(SkyColor.IsPortrait(1032, 1376));
            Assert.IsFalse(SkyColor.IsPortrait(1180, 820));
            var below = SkyColor.View(g, -0.4);
            Assert.Less(below.Luminance, g.Horizon.Luminance);
        }

        [Test]
        public void FogMatchesTheHorizonAndThinsWithAltitude()
        {
            var pad = Noon(0);
            var high = Noon(90000);
            Assert.AreEqual(pad.Horizon.R, pad.Fog.R);
            Assert.AreEqual(pad.Horizon.G, pad.Fog.G);
            Assert.AreEqual(pad.Horizon.B, pad.Fog.B);
            Assert.Greater(high.FogStartM, pad.FogStartM);
            Assert.Greater(high.FogEndM, pad.FogEndM);
        }

        [Test]
        public void SunElevationAccessorMatchesPadNightRamp()
        {
            // PadLighting.NightFactor(deg) = clamp((6 - deg) / 12, 0, 1). Same ramp, degrees.
            Assert.AreEqual(0f, SkySun.NightFactor(6f), 1e-6f);
            Assert.AreEqual(0f, SkySun.NightFactor(42f), 1e-6f);
            Assert.AreEqual(0.5f, SkySun.NightFactor(0f), 1e-6f);
            Assert.AreEqual(1f, SkySun.NightFactor(-6f), 1e-6f);
            Assert.AreEqual(1f, SkySun.NightFactor(-40f), 1e-6f);
            SkySun.Publish(15 * Math.PI / 180.0);
            Assert.AreEqual(15f, SkySun.ElevationDeg, 1e-4f);
            Assert.AreEqual(0f, SkySun.NightFactor(), 1e-5f);
            SkySun.Publish(-12 * Math.PI / 180.0);
            Assert.AreEqual(1f, SkySun.NightFactor(), 1e-5f);
        }

        [Test]
        public void DefaultClockIsDaylightAtHaven()
        {
            double elev = SkyBodies.SunElevationRad(SkyClock.LatitudeDeg, SkyClock.LongitudeDeg,
                SkyClock.Year, SkyClock.Month, SkyClock.Day, SkyClock.HoursUtc);
            double deg = elev * 180 / Math.PI;
            Assert.Greater(deg, 6);
            Assert.Less(deg, 90);
            var sun = SkyBodies.SunUnity(SkyClock.LatitudeDeg, SkyClock.LongitudeDeg,
                SkyClock.Year, SkyClock.Month, SkyClock.Day, SkyClock.HoursUtc);
            Assert.AreEqual(1, sun.Length, 1e-9);
            Assert.Greater(sun.Y, 0);
        }

        [Test]
        public void CelestialPoleSitsAtTheLatitude()
        {
            Frames.JdUt1Parts(2026, 6, 21, 16, out var jd1, out var jd2);
            var pole = SkyBodies.EquatorialToUnity(0, 90, SkyClock.LatitudeDeg, SkyClock.LongitudeDeg, jd1, jd2);
            double elev = Math.Asin(Math.Max(-1, Math.Min(1, pole.Y))) * 180 / Math.PI;
            Assert.AreEqual(SkyClock.LatitudeDeg, elev, 1e-6);
        }

        [Test]
        public void StarCatalogBudgetAndEquirect()
        {
            const string fixture = "1,0,0,-1.46,0.00\n2,180,45,0.5,0.6\n3,10,-10,4.2,1.4\n";
            var stars = SkyBodies.LoadStars(fixture, "low");
            Assert.AreEqual(3, stars.Count);
            Assert.AreEqual(1, stars[0].Id);
            Assert.AreEqual(1, stars[0].Brightness, 1e-9);
            Assert.Less(stars[1].Brightness, stars[0].Brightness);
            Assert.Less(stars[2].Brightness, stars[1].Brightness);
            var blue = SkyBodies.ColorFromBv(-0.2);
            var orange = SkyBodies.ColorFromBv(1.5);
            Assert.Greater(blue.B, blue.R);
            Assert.Greater(orange.R, orange.B);

            SkyBodies.EquirectUv(0, 0, out var u, out var v);
            Assert.AreEqual(0, u, 1e-12);
            Assert.AreEqual(0.5, v, 1e-12);
            SkyBodies.EquirectTexel(180, 90, 360, 181, out var x, out var y);
            Assert.AreEqual(180, x);
            Assert.AreEqual(180, y);

            string path = Path.Combine(Golden.Root, "data", "astro", "bsc5p-compact.csv");
            Assert.IsTrue(File.Exists(path), path);
            var catalog = SkyBodies.LoadStars(File.ReadAllText(path), "low");
            Assert.AreEqual(2000, catalog.Count);
            Assert.AreEqual(2491, catalog[0].Id, "Sirius is the brightest BSC star we ship");
            var high = SkyBodies.LoadStars(File.ReadAllText(path), "high");
            Assert.AreEqual(9000, high.Count);
            Assert.Less(high[100].Brightness, high[0].Brightness);
        }
    }
}
