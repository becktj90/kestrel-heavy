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
            double[] lats = { 0, 28.5, 45, 90, -45 };
            double[] hs = { 0, 400000 };
            foreach (double latDeg in lats)
            foreach (double h in hs)
            {
                double lat = latDeg * Math.PI / 180.0;
                Frames.GeodeticToEcef(lat, 0.4, h, out double x, out double y, out double z);
                Frames.EcefToGeodetic(x, y, z, out double lat2, out double lon2, out double h2);
                Frames.GeodeticToEcef(lat2, lon2, h2, out double x2, out double y2, out double z2);
                Assert.AreEqual(x, x2, RoundTripM, "x " + latDeg + " " + h);
                Assert.AreEqual(y, y2, RoundTripM, "y " + latDeg + " " + h);
                Assert.AreEqual(z, z2, RoundTripM, "z " + latDeg + " " + h);
                Assert.AreEqual(h, h2, RoundTripM, "h " + latDeg);
            }
        }

        [Test]
        public void EraUsesAstroJulianDate()
        {
            double jd = Frames.JdUt1FromUtc(2000, 1, 1, 12);
            Assert.AreEqual(Time.JulianDate(2000, 1, 1, 12), jd);
            Assert.AreEqual(Time.J2000, jd);
            double era = Frames.EraRad(jd);
            double turns = 0.7790572732640;
            double expect = (turns - Math.Floor(turns)) * 2 * Math.PI;
            Assert.AreEqual(expect, era, 1e-12);
            int[] years = { 2000, 2020, 2024, 2050 };
            int[] months = { 1, 6 };
            foreach (int y in years)
            foreach (int m in months)
            {
                double jd2 = Frames.JdUt1FromUtc(y, m, 1, 0);
                double du = jd2 - Time.J2000;
                double t = 0.7790572732640 + 1.00273781191135448 * du;
                t -= Math.Floor(t);
                Assert.AreEqual(t * 2 * Math.PI, Frames.EraRad(jd2), 1e-9);
            }
        }

        [Test]
        public void EciRoundTripsWithTheRotationRate()
        {
            double jd = Frames.JdUt1FromUtc(2026, 1, 1, 0);
            Frames.EciToEcef(jd, 7000000, -1000000, 500000, 100, 7500, -20,
                out double xe, out double ye, out double ze, out double vxe, out double vye, out double vze);
            Frames.EcefToEci(jd, xe, ye, ze, vxe, vye, vze,
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
            Assert.AreEqual(0, Commands.Run(new[] { "propagate" }, out string prop));
            StringAssert.Contains("not-implemented", prop);
            Assert.AreEqual(0, Commands.Run(new[] { "predict" }, out string pred));
            StringAssert.Contains("not-implemented", pred);
            Assert.AreEqual(2, Commands.Run(new string[0], out _));
        }
    }
}
