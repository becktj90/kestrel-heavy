// R2/S1: sun and Moon vs JPL Horizons geocentric apparent RA/Dec (data/astro/horizons-sun-moon.json), <= 0.1 deg.
using System;
using System.IO;
using Kestrel.Core.Astro;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AstroGoldenTests
    {
        static JsonObject Load() => MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "astro", "horizons-sun-moon.json")));

        [Test]
        public void SunAndMoonMatchHorizonsWithinTolerance()
        {
            var g = Load(); double tol = Golden.Num(((JsonObject)g["tolerance"])["angleDeg"]);
            double worstSun = 0, worstMoon = 0; int n = 0;
            foreach (JsonObject c in g.Arr("cases"))
            {
                double jd = Golden.Num(c["jdTT"]);
                var s = Ephemeris.Sun(jd); var m = Ephemeris.Moon(jd);
                double es = Ephemeris.AngularSeparationDeg(s.RaDeg, s.DecDeg, Golden.Num(c["sunRaDeg"]), Golden.Num(c["sunDecDeg"]));
                double em = Ephemeris.AngularSeparationDeg(m.RaDeg, m.DecDeg, Golden.Num(c["moonRaDeg"]), Golden.Num(c["moonDecDeg"]));
                worstSun = Math.Max(worstSun, es); worstMoon = Math.Max(worstMoon, em); n++;
                Assert.LessOrEqual(es, tol, "sun @ JD " + jd); Assert.LessOrEqual(em, tol, "moon @ JD " + jd);
                Assert.AreEqual(Golden.Num(c["sunDistAu"]), s.DistanceKm / Ephemeris.AU_KM, 1e-4, "sun dist @ JD " + jd);
                Assert.AreEqual(Golden.Num(c["moonDistAu"]) * Ephemeris.AU_KM, m.DistanceKm, 50, "moon dist @ JD " + jd);
            }
            Assert.AreEqual((int)Golden.Num(g["count"]), n);
            TestContext.WriteLine($"astro: {n} epochs, worst sun {worstSun * 3600:F1}\" moon {worstMoon * 3600:F1}\"");
        }

        [Test]
        public void Deterministic() { var a = Ephemeris.Moon(2460000.25); var b = Ephemeris.Moon(2460000.25); Assert.AreEqual(a.RaDeg, b.RaDeg); Assert.AreEqual(a.DecDeg, b.DecDeg); }

        [Test]
        public void JulianDateAndDeltaT()
        {
            Assert.AreEqual(2451545.0, Time.JulianDate(2000, 1, 1, 12), 1e-9);
            Assert.AreEqual(2436116.31, Time.JulianDate(1957, 10, 4, 0.81 * 24), 1e-6); // Meeus ex. 7.a
            Assert.That(Time.DeltaT(2026), Is.InRange(60.0, 80.0));
        }

        [Test]
        public void MoonPhaseKnownEvents()
        {
            // Full Moon 2024-04-23 23:49 UTC, new Moon 2024-04-08 18:21 UTC (total solar eclipse).
            Assert.Greater(Ephemeris.MoonIlluminatedFraction(Time.UtcToTT(Time.JulianDate(2024, 4, 23, 23 + 49 / 60.0))), 0.995);
            Assert.Less(Ephemeris.MoonIlluminatedFraction(Time.UtcToTT(Time.JulianDate(2024, 4, 8, 18 + 21 / 60.0))), 0.005);
        }

        [Test]
        public void StarCatalogParsesSortsAndCaps()
        {
            var csv = "# id,ra,dec,vmag,bv\n3,10.0,5.0,4.2\n1,101.287,-16.716,-1.46,0.00\n2,279.234,38.784,0.03,0.00\n";
            var all = StarCatalog.Parse(csv);
            Assert.AreEqual(3, all.Count); Assert.AreEqual(1, all[0].Id); Assert.AreEqual(0.6, all[2].BV);
            Assert.AreEqual(2, StarCatalog.Parse(csv, maxMag: 1).Count);
            Assert.AreEqual(1, StarCatalog.Parse(csv, maxCount: 1).Count);
            Assert.Throws<FormatException>(() => StarCatalog.Parse("1,400,0,1"));
            Assert.AreEqual(2000, StarCatalog.BudgetForTier("low"));
        }
    }
}
