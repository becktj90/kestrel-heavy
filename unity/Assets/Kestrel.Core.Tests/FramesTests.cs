// P1: WGS-84 geodetic/ECEF and ERA. Bands are "Frames (P1)" in docs/PHYSICS.md.
using System;
using Kestrel.Core.Astro;
using Kestrel.Core.Bodies;
using Kestrel.Core.Cli;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FramesTests
    {
        const double RoundTripM = 1e-6;
        const double Vel = 1e-9;

        [Test]
        public void EarthRecordMatchesTheGravityConstants()
        {
            var earth = BodyCatalog.Require(BodyCatalog.LoadFromRepo(Golden.Root), "earth");
            Assert.AreEqual(Gravity.MU_EARTH, earth.mu);
            Assert.AreEqual(Gravity.R_EARTH, earth.radius);
            Assert.AreEqual(Earth.OMEGA_EARTH, earth.omega);
            Assert.AreEqual(Frames.InverseFlattening, earth.inverseFlattening);
            Assert.AreEqual(1.08262668e-3, earth.j2);
            Assert.AreEqual(Geopotential.J3, earth.j3);
            Assert.AreEqual(Geopotential.J4, earth.j4);
            Assert.IsTrue(earth.gravity);
            Assert.AreEqual(1, BodyCatalog.LoadFromRepo(Golden.Root).Count);
        }

        [Test]
        public void EquatorAndPoleMatchTheEllipsoid()
        {
            Frames.GeodeticToEcef(0, 0, 0, out double x, out double y, out double z);
            Assert.AreEqual(Gravity.R_EARTH, x, 1e-6);
            Assert.AreEqual(0, y, 1e-6);
            Assert.AreEqual(0, z, 1e-6);
            double b = Gravity.R_EARTH * (1 - Frames.Flattening);
            Frames.GeodeticToEcef(Math.PI / 2, 0, 0, out x, out y, out z);
            Assert.AreEqual(0, x, 1e-4);
            Assert.AreEqual(0, y, 1e-4);
            Assert.AreEqual(b, z, 1e-4);
        }

        [Test]
        public void GeodeticRoundTrips()
        {
            double[] lats = { 0, 28.5, 45, 90, -90, -45 };
            double[] hs = { -1000, 0, 400000, 36000000 };
            foreach (double latDeg in lats)
            foreach (double h in hs)
                RoundTrip(latDeg, h);
        }

        [Test]
        public void Geodetic_PolesEquatorNegativeAltitudeAndGeo()
        {
            RoundTrip(0, 0);
            RoundTrip(0, -1000);
            RoundTrip(0, 36000000);
            RoundTrip(90, 0);
            RoundTrip(90, -1000);
            RoundTrip(90, 36000000);
            RoundTrip(-90, 0);
            RoundTrip(-90, -1000);
            RoundTrip(-90, 36000000);
        }

        static void RoundTrip(double latDeg, double h)
        {
            double lat = latDeg * Math.PI / 180.0;
            double lon = Math.Abs(latDeg) == 90 ? 0 : 0.4;
            Frames.GeodeticToEcef(lat, lon, h, out double x, out double y, out double z);
            Frames.EcefToGeodetic(x, y, z, out double lat2, out double lon2, out double h2);
            Frames.GeodeticToEcef(lat2, lon2, h2, out double x2, out double y2, out double z2);
            Assert.AreEqual(x, x2, RoundTripM, "x " + latDeg + " " + h);
            Assert.AreEqual(y, y2, RoundTripM, "y " + latDeg + " " + h);
            Assert.AreEqual(z, z2, RoundTripM, "z " + latDeg + " " + h);
            Assert.AreEqual(h, h2, RoundTripM, "h " + latDeg + " " + h);
            if (Math.Abs(latDeg) == 90) Assert.AreEqual(lat, lat2, 1e-12, "lat " + latDeg);
        }

        [Test]
        public void EraTwoPartHoldsOneNanoRadian()
        {
            Frames.JdUt1Parts(2000, 1, 1, 12, out double j1, out double j2);
            Assert.AreEqual(Time.J2000, j1 + j2, 0);
            double noon = (0.7790572732640 - Math.Floor(0.7790572732640)) * 2 * Math.PI;
            Assert.AreEqual(noon, Frames.EraRad(j1, j2), 1e-12);

            var epochs = new[]
            {
                new[] { 2000.0, 1, 1, 15.5 },
                new[] { 2020.0, 1, 1, 0.000001 },
                new[] { 2024.0, 6, 15, 18.123456789 },
                new[] { 2050.0, 6, 1, 7.891234567 },
                new[] { 1990.0, 11, 3, 23.999999 },
                new[] { 2100.0, 1, 1, 12.345678901 },
                new[] { 2100.0, 12, 31, 23.999999 },
                new[] { 1900.0, 3, 1, 6.5 },
            };
            foreach (var e in epochs)
            {
                int y = (int)e[0], m = (int)e[1], d = (int)e[2];
                double hours = e[3];
                string label = y + "-" + m + "-" + d + " " + hours.ToString("G17");
                Frames.JdUt1Parts(y, m, d, hours, out double jd1, out double jd2);
                Assert.AreEqual(EraSplitReference(y, m, d, hours), Frames.EraRad(jd1, jd2), 1e-9, label);
            }
            // Packing the fraction into the Julian date first misses eraClosed on these hours.
            Assert.Greater(Math.Abs(EraPacked(2024, 6, 15, 18.123456789) - EraSplitReference(2024, 6, 15, 18.123456789)), 1e-9);
            Assert.Greater(Math.Abs(EraPacked(2050, 6, 1, 7.891234567) - EraSplitReference(2050, 6, 1, 7.891234567)), 1e-9);
            Assert.Greater(Math.Abs(EraPacked(1990, 11, 3, 23.999999) - EraSplitReference(1990, 11, 3, 23.999999)), 1e-9);
        }

        /// <summary>Days since J2000 with the day fraction multiplied on its own. Independent of <see cref="Frames.EraRad"/>.</summary>
        static double EraSplitReference(int y, int m, int d, double hours)
        {
            double dayCount = Time.JulianDate(y, m, d, 0) - Time.J2000;
            double dayFrac = hours / 24.0;
            const double c = 1.00273781191135448;
            double turns = 0.7790572732640 + c * dayCount + c * dayFrac;
            turns -= Math.Floor(turns);
            return turns * 2.0 * Math.PI;
        }

        static double EraPacked(int y, int m, int d, double hours)
        {
            double du = Time.JulianDate(y, m, d, hours) - Time.J2000;
            double turns = 0.7790572732640 + 1.00273781191135448 * du;
            turns -= Math.Floor(turns);
            return turns * 2.0 * Math.PI;
        }

        [Test]
        public void EciRoundTripsWithTheRotationRate()
        {
            Frames.JdUt1Parts(2026, 1, 1, 0, out double jd1, out double jd2);
            Frames.EciToEcef(jd1, jd2, 7000000, -1000000, 500000, 100, 7500, -20,
                out double xe, out double ye, out double ze, out double vxe, out double vye, out double vze);
            Frames.EcefToEci(jd1, jd2, xe, ye, ze, vxe, vye, vze,
                out double xi, out double yi, out double zi, out double vxi, out double vyi, out double vzi);
            Assert.AreEqual(7000000, xi, RoundTripM);
            Assert.AreEqual(-1000000, yi, RoundTripM);
            Assert.AreEqual(500000, zi, RoundTripM);
            Assert.AreEqual(100, vxi, Vel);
            Assert.AreEqual(7500, vyi, Vel);
            Assert.AreEqual(-20, vzi, Vel);
        }

        [Test]
        public void CliAtmoEraAndTheNamedLaterCommands()
        {
            Assert.AreEqual(0, Commands.Run(new[] { "atmo", "0" }, out string atmo));
            StringAssert.Contains("\"rho\":", atmo);
            Assert.AreEqual(0, Commands.Run(new[] { "era", "2000", "1", "1", "12" }, out string era));
            StringAssert.Contains("\"eraRad\":", era);
            Assert.AreEqual(0, Commands.Run(new[] { "mc" }, out string mc));
            StringAssert.Contains("not-implemented", mc);
            Assert.AreEqual(2, Commands.Run(new[] { "propagate" }, out string propBad));
            Assert.AreEqual(0, Commands.Run(new[]
            {
                "propagate", "kepler",
                "7000000", "0", "0", "0", "7546", "0", "60"
            }, out string prop));
            StringAssert.Contains("\"mode\":\"kepler\"", prop);
            StringAssert.Contains("\"rx\":", prop);
            Assert.AreEqual(0, Commands.Run(new[] { "predict" }, out string pred));
            StringAssert.Contains("not-implemented", pred);
            Assert.AreEqual(2, Commands.Run(new string[0], out _));
        }
    }
}
