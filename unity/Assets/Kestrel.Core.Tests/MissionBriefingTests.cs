// S4 briefing: KH-n copy, seeded pad/clock/wind, and calm-wind identity with the ascent.
using System;
using System.IO;
using System.Text.RegularExpressions;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionBriefingTests
    {
        static readonly string[] Forbidden =
        {
            "spacex", "falcon", "starlink", "nasa", "blue origin", "ula", "arianespace",
            "cape canaveral", "kennedy space", "boca chica", "starbase", "soyuz", "saturn v",
        };

        [Test]
        public void Mix_MatchesFailureModel()
        {
            ulong[] hashes = { 0, 1, 13, 0x4F2AUL, ulong.MaxValue };
            ulong[] seeds = { 0, 1, 13, 0x4F2AUL, 99 };
            foreach (var h in hashes)
                foreach (var s in seeds)
                    Assert.AreEqual(FailureModel.Fresh(h, s).rng, MissionBriefing.Mix(h, s), h + " " + s);
        }

        [Test]
        public void ReplayLabel_AndFlightNumber_MatchTheCard()
        {
            var replay = new ReplaySeed(1, 0x4F2AUL);
            Assert.AreEqual("4F2A", replay.Label);
            Assert.AreEqual(14, MissionBriefing.FlightNumberOf(13));
            double kt = WeatherBook.Load(Golden.Root).KtPerMs;
            Assert.AreEqual("9 kt W", MissionBriefing.FormatWind(9.0 / kt, "W", kt));

            var card = MissionBriefing.Load(Golden.Root, 4, 1, 13);
            Assert.AreEqual("KH-14", card.FlightCode);
            Assert.AreEqual("KH-4", card.ContractCode);
            Assert.AreEqual("4F2A", new ReplaySeed(1, 0x4F2A).Label);
            Assert.AreEqual("000D", card.Replay.Label);
        }

        [Test]
        public void WeatherEye_MatchesTheBriefingCard()
        {
            var card = MissionBriefing.Load(Golden.Root, 4, 7, 13);
            Assert.AreEqual("Tern-3", card.PayloadName);
            Assert.AreEqual("Weather sat", card.PayloadKind);
            Assert.AreEqual("Tern-3 Weather Eye", card.Title);
            Assert.AreEqual("Put a 2,500 kg fictional weather sat in a 500 km, 51.6° orbit.", card.Goal);
            Assert.AreEqual(2500, card.MassKg);
            Assert.AreEqual(500, card.ApoKm);
            Assert.AreEqual(51.6, card.IncDeg);
            Assert.AreEqual(2, card.Bonuses.Count);
            Assert.AreEqual("land-booster", card.Bonuses[0].Id);
            Assert.AreEqual("Land the booster", card.Bonuses[0].Label);
            Assert.AreEqual("peak-g", card.Bonuses[1].Id);
            Assert.AreEqual("Keep peak g under 4", card.Bonuses[1].Label);
            Assert.AreEqual(4, card.Bonuses[1].MaxG);
            StringAssert.Contains("Wind ", card.StatusLine);
            StringAssert.Contains("Pad: ", card.StatusLine);
            StringAssert.Contains("Seed ", card.StatusLine);
            StringAssert.Contains("m/s", card.EngineerLine);
            StringAssert.Contains("shear", card.EngineerLine);
        }

        [Test]
        public void EveryMission_HasAFictionalBriefing()
        {
            var campaign = Campaign.Load(Golden.Root);
            Assert.AreEqual(20, campaign.Missions.Count);
            string blob = File.ReadAllText(Path.Combine(Golden.Root, "data", "campaign.json"))
                + File.ReadAllText(Path.Combine(Golden.Root, "data", "sites.json"))
                + File.ReadAllText(Path.Combine(Golden.Root, "data", "weather.json"));
            string lower = blob.ToLowerInvariant();
            foreach (var bad in Forbidden)
                Assert.IsFalse(Regex.IsMatch(lower, @"\b" + Regex.Escape(bad) + @"\b"), bad);

            var kinds = new System.Collections.Generic.HashSet<string>();
            for (int i = 1; i <= 20; i++)
            {
                var card = MissionBriefing.Load(Golden.Root, i, 3, 13);
                Assert.AreEqual("KH-" + i, card.ContractCode);
                Assert.IsFalse(string.IsNullOrEmpty(card.PayloadName));
                Assert.IsFalse(string.IsNullOrEmpty(card.Goal));
                Assert.GreaterOrEqual(card.Goal.Length, 12);
                Assert.That(card.Bonuses.Count, Is.InRange(1, 2), card.ContractName);
                kinds.Add(card.PayloadKind);
                Assert.IsNotNull(card.Wind);
                Assert.IsNotNull(card.Site);
                Assert.IsFalse(string.IsNullOrEmpty(card.TimeLabel));
            }
            CollectionAssert.IsSubsetOf(new[] { "Weather sat", "Cubesat", "Comsat", "Lunar probe", "Crew capsule" }, kinds);
        }

        [Test]
        public void Replay_RestoresTheSameLaunch_AndDesignHashDoesNotReshuffleWeather()
        {
            var a = MissionBriefing.Load(Golden.Root, 4, 1, 13);
            var b = MissionBriefing.Load(Golden.Root, 4, 1, 13);
            var replay = MissionBriefing.FromReplay(Golden.Root, a.Replay, 4, false);
            var otherDesign = MissionBriefing.Load(Golden.Root, 4, 99, 13);
            var otherSeed = MissionBriefing.Load(Golden.Root, 4, 1, 14);

            AssertSameEnvironment(a, b);
            AssertSameEnvironment(a, replay);
            AssertSameEnvironment(a, otherDesign);
            Assert.AreNotEqual(a.Replay.FailureRng, otherDesign.Replay.FailureRng);
            Assert.AreEqual(a.FlightCode, otherDesign.FlightCode);
            Assert.AreNotEqual(a.FlightCode, otherSeed.FlightCode);
            bool moved = a.Wind.SurfaceMs != otherSeed.Wind.SurfaceMs
                || a.Site.Id != otherSeed.Site.Id
                || a.TimeId != otherSeed.TimeId;
            Assert.IsTrue(moved, "a new mission seed should change the launch");
        }

        [Test]
        public void SurfaceWind_FollowsTheFirstDraw()
        {
            var weather = WeatherBook.Load(Golden.Root);
            ulong stream = MissionBriefing.EnvironmentState(13);
            double u = MissionBriefing.Draw(ref stream);
            double expect = MissionBriefing.Rayleigh(u, weather.RayleighScaleMs);
            var card = MissionBriefing.Load(Golden.Root, 4, 1, 13);
            Assert.AreEqual(expect, card.Wind.SurfaceMs, 0);
            Assert.AreEqual(MissionBriefing.FormatWind(expect, card.Wind.Compass, weather.KtPerMs), card.WindLine);
            Assert.Greater(card.Wind.SpeedAt(9000), card.Wind.SpeedAt(0));
            Assert.AreEqual(0, card.Wind.EastAt(25000), 0, "wind is gone above the fade altitude");
            Assert.AreEqual(0, card.Wind.NorthAt(25000), 0);
            Assert.AreNotEqual(0, card.Wind.ShearAt(6000));
        }

        [Test]
        public void Sites_UseLatitudeForInclinationAndRotation()
        {
            var book = SiteBook.Load(Golden.Root);
            Assert.GreaterOrEqual(book.Sites.Count, 3);
            LaunchSite pier = null, coast = null, inland = null;
            foreach (var s in book.Sites)
            {
                Assert.AreEqual(Earth.MinInclination(s.LatitudeDeg), s.FreeInclinationDeg, 0);
                Assert.AreEqual(Earth.SiteRotationSpeed(s.LatitudeDeg), s.RotationBoostMs, 0);
                if (s.Id == "pier-7") pier = s;
                if (s.Id == "coastal-pad-b") coast = s;
                if (s.Id == "salt-flat") inland = s;
            }
            Assert.AreEqual("Pier 7", pier.Name);
            Assert.AreEqual(28.5, pier.LatitudeDeg);
            Assert.IsTrue(pier.Coastal && pier.Barge);
            Assert.AreEqual("Haven", pier.BargeName);
            Assert.AreEqual("Coastal Pad B", coast.Name);
            Assert.IsTrue(coast.Barge);
            Assert.IsFalse(inland.Coastal);
            Assert.IsFalse(inland.Barge);
            Assert.Greater(pier.RotationBoostMs, book.Sites[book.Sites.Count - 1].RotationBoostMs);

            var barge = MissionBriefing.Load(Golden.Root, 7, 1, 13);
            Assert.IsTrue(barge.Site.Barge, barge.Site.Name);
            var inlandOk = MissionBriefing.Load(Golden.Root, 1, 1, 13);
            Assert.IsNotNull(inlandOk.Site);
        }

        [Test]
        public void FreeBuild_KeepsTheEnvironment()
        {
            var card = MissionBriefing.Free(Golden.Root, 4, 13);
            Assert.IsTrue(card.FreeBuild);
            Assert.AreEqual("free", card.ContractId);
            Assert.AreEqual(0, card.Bonuses.Count);
            Assert.AreEqual("KH-14", card.FlightCode);
            Assert.IsNotNull(card.Wind);
            Assert.IsNotNull(card.Site);
            Assert.IsFalse(string.IsNullOrEmpty(card.TimeId));
            var again = MissionBriefing.FromReplay(Golden.Root, card.Replay, 0, true);
            AssertSameEnvironment(card, again);
        }

        [Test]
        public void CalmWind_MatchesAtmosphereAndZeroBending()
        {
            var calm = SeededWind.CalmAir;
            double[] alts = { 0, 1000, 11000, 20000, 50000 };
            double[] speeds = { 0, 10, 250, 1500 };
            foreach (double z in alts)
            {
                var atm = Atmosphere.At(z);
                foreach (double v in speeds)
                {
                    var s = calm.Sample(z, v, 0, 0, 1.7, 2.2, 14, 9);
                    Assert.AreEqual(atm.T, s.Atmosphere.T, 0);
                    Assert.AreEqual(atm.p, s.Atmosphere.p, 0);
                    Assert.AreEqual(atm.rho, s.Atmosphere.rho, 0);
                    Assert.AreEqual(atm.a, s.Atmosphere.a, 0);
                    Assert.AreEqual(0.5 * atm.rho * v * v, s.QPa, 0, "q @" + z);
                    Assert.AreEqual(0, s.AlphaRad, 0);
                    Assert.AreEqual(0, s.BendingNm, 0);
                    Assert.AreEqual(0, s.ShearPerSec, 0);
                }
            }
        }

        [Test]
        public void Crosswind_FeedsAlphaAndBending()
        {
            var wind = SeededWind.Uniform(0, 18);
            double z = 4000;
            var atm = Atmosphere.At(z);
            var calmQ = 0.5 * atm.rho * 220 * 220;
            var s = wind.Sample(z, 220, 0, 0, 2.4, 1.8, 16, 11);
            Assert.AreEqual(atm.rho, s.Atmosphere.rho, 0);
            Assert.Greater(s.AlphaRad, 0);
            Assert.AreNotEqual(calmQ, s.QPa);
            double bend = StructureModel.BendingMomentNm(new LoadState
            {
                qPa = s.QPa,
                areaM2 = 2.4,
                cnAlpha = 1.8,
                alphaRad = s.AlphaRad,
                xCpM = 16,
                xCgM = 11,
            });
            Assert.AreEqual(bend, s.BendingNm, 0);
            Assert.Greater(s.BendingNm, 0);
        }

        [Test]
        public void CalmWind_DoesNotChangeNominalAscent()
        {
            var stages = Headless.MediumLifter();
            var plain = new SimOptions { tMax = 90, dt = 0.05 };
            var calm = new SimOptions { tMax = 90, dt = 0.05, wind = SeededWind.CalmAir };
            var vacuumPlain = new SimOptions { tMax = 20, dt = 0.05, vacuum = true };
            var vacuumWind = new SimOptions { tMax = 20, dt = 0.05, vacuum = true, wind = SeededWind.Uniform(30, 12) };

            SameFlight(Headless.Simulate(stages, plain), Headless.Simulate(stages, calm));
            SameFlight(Ascent3Dof.SimulatePlanar(stages, plain), Ascent3Dof.SimulatePlanar(stages, calm));
            SameFlight(Headless.Simulate(stages, vacuumPlain), Headless.Simulate(stages, vacuumWind));
            SameFlight(Ascent3Dof.SimulatePlanar(stages, vacuumPlain), Ascent3Dof.SimulatePlanar(stages, vacuumWind));
        }

        [Test]
        public void EastAndNorthWind_ChangeDynamicPressure()
        {
            var stages = Headless.MediumLifter();
            var plain = Headless.Simulate(stages, new SimOptions { tMax = 70, dt = 0.05 });
            var east = Headless.Simulate(stages, new SimOptions { tMax = 70, dt = 0.05, wind = SeededWind.Uniform(22, 0) });
            var north = Headless.Simulate(stages, new SimOptions { tMax = 70, dt = 0.05, wind = SeededWind.Uniform(0, 22) });
            Assert.AreNotEqual(plain.maxQ, east.maxQ);
            Assert.AreNotEqual(plain.maxQ, north.maxQ);
            var planar = Ascent3Dof.SimulatePlanar(stages, new SimOptions { tMax = 70, dt = 0.05, wind = SeededWind.Uniform(22, 0) });
            Assert.AreEqual(east.maxQ, planar.maxQ, Math.Abs(east.maxQ) * 1e-9 + 1e-6);
            Assert.AreEqual(east.time, planar.time, Math.Abs(east.time) * 1e-9 + 1e-9);
        }

        [Test]
        public void AboveTheFade_RetargetIsANoOp()
        {
            var card = MissionBriefing.Load(Golden.Root, 4, 1, 13);
            double avx = 30, avy = -12, vrel = 88;
            double got = card.Wind.Retarget(25000, -1, 0, ref avx, ref avy, vrel);
            Assert.AreEqual(vrel, got, 0);
            Assert.AreEqual(30, avx, 0);
            Assert.AreEqual(-12, avy, 0);
        }

        [Test]
        public void Bind_SetsWindAndSiteLatitude()
        {
            var card = MissionBriefing.Load(Golden.Root, 4, 1, 13);
            var salt = SiteBook.Load(Golden.Root);
            LaunchSite flat = null;
            foreach (var s in salt.Sites) if (s.Id == "salt-flat") flat = s;
            card.Site = flat;
            card.Wind = SeededWind.CalmAir;
            var opts = new SimOptions();
            card.Bind(opts);
            Assert.AreSame(SeededWind.CalmAir, opts.wind);
            Assert.AreEqual(flat.LatitudeDeg, opts.latitude, 0);
            Assert.IsTrue(opts.wind.Calm);
        }

        static void AssertSameEnvironment(MissionBriefing a, MissionBriefing b)
        {
            Assert.AreEqual(a.FlightCode, b.FlightCode);
            Assert.AreEqual(a.Goal, b.Goal);
            Assert.AreEqual(a.Site.Id, b.Site.Id);
            Assert.AreEqual(a.TimeId, b.TimeId);
            Assert.AreEqual(a.DayOfYear, b.DayOfYear);
            Assert.AreEqual(a.WindLine, b.WindLine);
            Assert.AreEqual(a.Wind.SurfaceMs, b.Wind.SurfaceMs, 0);
            Assert.AreEqual(a.Wind.Compass, b.Wind.Compass);
            CollectionAssert.AreEqual(a.Wind.EastMs, b.Wind.EastMs);
            CollectionAssert.AreEqual(a.Wind.NorthMs, b.Wind.NorthMs);
        }

        static void SameFlight(SimResult a, SimResult b)
        {
            Assert.AreEqual(a.outcome, b.outcome);
            Assert.AreEqual(a.time, b.time, 0);
            Assert.AreEqual(a.maxQ, b.maxQ, 0);
            Assert.AreEqual(a.maxG, b.maxG, 0);
            Assert.AreEqual(a.lossDrag, b.lossDrag, 0);
            Assert.AreEqual(a.orbit.apoAlt, b.orbit.apoAlt, 0);
            Assert.AreEqual(a.orbit.periAlt, b.orbit.periAlt, 0);
            Assert.AreEqual(a.x, b.x, 0);
            Assert.AreEqual(a.y, b.y, 0);
            Assert.AreEqual(a.vx, b.vx, 0);
            Assert.AreEqual(a.vy, b.vy, 0);
        }
    }
}
