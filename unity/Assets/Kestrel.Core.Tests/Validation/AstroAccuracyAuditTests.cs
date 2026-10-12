// Validation (audit #83): Kestrel.Core.Astro.Ephemeris vs NASA/JPL Horizons.
//
// Reference: data/astro/horizons-sun-moon.json (50 seeded epochs, 2000-2100), already logged in LICENSES.md.
// Frame:      geocentric (Horizons site 500@399), apparent RA/Dec of date (QUANTITIES=2): true equator and
//             equinox of date, light-time, aberration and nutation applied. Ephemeris.cs outputs the same frame.
// Time scale: epochs are Julian Dates in TT (Horizons TIME_TYPE=TT); Ephemeris takes jdTT directly, so no
//             Delta-T or UTC/leap-second error enters this comparison. (Delta-T matters only for wall-clock input.)
//
// Measured at e326f0d: worst Sun 29.8", worst Moon 9.5". AstroGoldenTests keeps the coarse 0.1 deg gameplay gate.
// This file adds tighter ceilings taken from the *published accuracy of the algorithms*, not from the measured
// result plus slack. A change that makes either body worse than its published model accuracy fails here.
//
//  Sun  ceiling 36"  = 0.01 deg, Meeus "Astronomical Algorithms" ch. 25 stated accuracy of the low-precision
//                      solar theory. Measured 29.8" (83% of the bound).
//  Moon ceiling 11"  = Meeus ch. 47 main-term series: ~10" in longitude and ~4" in latitude,
//                      sqrt(10^2 + 4^2) = 10.8", rounded up to 11". Measured 9.5" (86% of the bound).
//  Moon distance 50 km: unchanged from AstroGoldenTests (ch. 47 distance series is good to tens of km).
//  Sun distance 1e-4 AU: unchanged (~15,000 km; ch. 25 radius-vector accuracy).
using System;
using System.IO;
using Kestrel.Core.Astro;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests.Validation
{
    public class AstroAccuracyAuditTests
    {
        const double SunCeilingArcsec = 36.0;  // Meeus ch. 25 published accuracy (0.01 deg)
        const double MoonCeilingArcsec = 11.0; // Meeus ch. 47 published accuracy (10" lon, 4" lat)

        static JsonObject Load() => MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "astro", "horizons-sun-moon.json")));

        [Test]
        public void ReferenceFileIsGeocentricApparentOfDateInTT()
        {
            var g = Load();
            string frame = (string)g["frame"];
            StringAssert.Contains("500@399", frame);
            StringAssert.Contains("apparent", frame);
            StringAssert.Contains("TT", frame);
            Assert.AreEqual(50, (int)Golden.Num(g["count"]));
        }

        [Test]
        public void SunAndMoonWithinPublishedModelAccuracy()
        {
            var g = Load();
            double worstSun = 0, worstMoon = 0, ssSun = 0, ssMoon = 0, minJd = double.MaxValue, maxJd = 0; int n = 0;
            foreach (JsonObject c in g.Arr("cases"))
            {
                double jd = Golden.Num(c["jdTT"]);
                var s = Ephemeris.Sun(jd); var m = Ephemeris.Moon(jd);
                double es = 3600 * Ephemeris.AngularSeparationDeg(s.RaDeg, s.DecDeg, Golden.Num(c["sunRaDeg"]), Golden.Num(c["sunDecDeg"]));
                double em = 3600 * Ephemeris.AngularSeparationDeg(m.RaDeg, m.DecDeg, Golden.Num(c["moonRaDeg"]), Golden.Num(c["moonDecDeg"]));
                Assert.LessOrEqual(es, SunCeilingArcsec, "sun @ JD(TT) " + jd);
                Assert.LessOrEqual(em, MoonCeilingArcsec, "moon @ JD(TT) " + jd);
                worstSun = Math.Max(worstSun, es); worstMoon = Math.Max(worstMoon, em);
                ssSun += es * es; ssMoon += em * em; n++;
                minJd = Math.Min(minJd, jd); maxJd = Math.Max(maxJd, jd);
            }
            // Coverage: the audit claim is "2000-2100"; make sure the vectors actually span it.
            Assert.Less(minJd, Time.JulianDate(2005, 1, 1, 0));
            Assert.Greater(maxJd, Time.JulianDate(2095, 1, 1, 0));
            TestContext.WriteLine($"audit #83: n={n} sun worst {worstSun:F1}\" rms {Math.Sqrt(ssSun / n):F1}\" (ceiling {SunCeilingArcsec}\"), moon worst {worstMoon:F1}\" rms {Math.Sqrt(ssMoon / n):F1}\" (ceiling {MoonCeilingArcsec}\")");
        }
    }
}
