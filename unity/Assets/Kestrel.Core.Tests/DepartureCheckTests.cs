// #313 Physics A: ellipse and C3 targets climb to a parking orbit at the aim perigee, and pass only if the Δv actually
// left there covers the impulsive departure burn plus the finite-burn loss.

using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DepartureCheckTests
    {
        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        static Kestrel.Core.Json.JsonObject Winner(string id)
        {
            foreach (Kestrel.Core.Json.JsonObject row in Golden.File("campaign-winners").Arr("missions"))
                if (row.Str("id") == id) return (Kestrel.Core.Json.JsonObject)row.Arr("designs")[0];
            Assert.Fail("no winner " + id);
            return null;
        }

        [TestCase("m7")]
        [TestCase("m10")]
        [TestCase("m9")]
        [TestCase("m17")]
        public void EllipseAndC3_CutOffAtTheParkingOrbit_AtTheAimPerigee(string id)
        {
            var m = campaign.ById(id);
            Assert.IsTrue(m.DepartsFromParking, id);
            Assert.AreEqual(200e3, m.GuidanceAltM, 1e-9, id + " aims at its perigee");
            double parking = -Gravity.MU_EARTH / (2 * (Gravity.R_EARTH + 200e3));
            Assert.AreEqual(parking, MissionFlightSetup.TargetEnergy(m), 1e-6, id + ": a = R + aim perigee");
            Assert.AreEqual(parking, SimOptions.EnergyFor(200e3, 200e3), 1e-6);
            var design = Kestrel.Core.Vehicle.Design.FromJson(Winner(id));
            var setup = MissionFlightSetup.Build(catalog, m, design, MissionFlightSetup.BatchDt);
            Assert.AreEqual(parking, setup.Options.cutoffEnergy, 1e-6, id + " sim cutoff energy");
            Assert.IsTrue(setup.Options.energyCutoff);
        }

        [Test]
        public void ACircularTarget_StillCutsOffAtItsOwnEnergy()
        {
            var m = campaign.ById("m8");
            Assert.IsFalse(m.DepartsFromParking);
            Assert.AreEqual(SimOptions.EnergyFor(400e3, 400e3), MissionFlightSetup.TargetEnergy(m), 1e-6);
        }

        [Test]
        public void RequiredDepartureDv_MatchesTheSanityValues()
        {
            // 200 km circular to GTO (200 x 35,786 km): about 2.46 km/s. TLI (C3 -2 km2/s2) from 200 km: about 3.1 km/s.
            double gto = MissionScore.DepartureImpulsiveMps("ellipse", 35786, double.NaN, 200e3);
            double tli = MissionScore.DepartureImpulsiveMps("c3", double.NaN, -2, 200e3);
            Assert.AreEqual(2455, gto, 15, "GTO");
            Assert.AreEqual(3133, tli, 15, "TLI");
            Assert.That(tli, Is.InRange(3050, 3200));
            Assert.That(gto, Is.InRange(2430, 2480));
            Assert.AreEqual(0, MissionScore.DepartureImpulsiveMps("ellipse", 200, double.NaN, 200e3), 1e-6, "no burn to the parking orbit itself");
            Assert.IsTrue(double.IsNaN(MissionScore.DepartureImpulsiveMps("circular", 400, double.NaN, 400e3)), "a circular target has no departure burn");
        }

        [Test]
        public void FiniteBurnLoss_IsOnePointFivePercent_AtTwoOfPointFiveAndUp_AndGrowsBelow()
        {
            Assert.AreEqual(0.015, MissionScore.DepartureLoss(0.5), 1e-12);
            Assert.AreEqual(0.015, MissionScore.DepartureLoss(2.4), 1e-12);
            Assert.AreEqual(0.06, MissionScore.DepartureLoss(0.25), 1e-12, "(0.5/0.25)^2 x 1.5%");
            Assert.AreEqual(0.10, MissionScore.DepartureLoss(0.1), 1e-12, "capped at 10%");
            Assert.AreEqual(0.10, MissionScore.DepartureLoss(double.NaN), 1e-12, "no thrust left: the cap");
            double gto = MissionScore.DepartureImpulsiveMps("ellipse", 35786, double.NaN, 200e3);
            Assert.AreEqual(gto * 1.015, MissionScore.DepartureRequiredMps("ellipse", 35786, double.NaN, 200e3, 2.0), 1e-9);
        }

        [TestCase("m7")]
        [TestCase("m17")]
        public void TheDepartureCheck_PassesAtTheBurn_AndFailsOneMetrePerSecondShort(string id)
        {
            var m = campaign.ById(id);
            double need = MissionScore.DepartureRequiredMps(m.TargetKind, m.ApoKm, m.C3Km2PerS2, m.GuidanceAltM, 1.2);
            var enough = new GoalCheck { DepartureDv = need, DepartureTW = 1.2, C3Km2PerS2 = -60 };
            var shortBy1 = new GoalCheck { DepartureDv = need - 1, DepartureTW = 1.2, C3Km2PerS2 = -60 };
            // A clean 200 x 200 km parking orbit: the perigee band holds, the elements and C3 paths do not.
            Assert.IsTrue(FlightDebrief.DepartureMet(m, enough), id);
            Assert.IsFalse(FlightDebrief.DepartureMet(m, shortBy1), id);
            Assert.IsTrue(FlightDebrief.GoalMet(m, true, 200e3, 200e3, 3, "good", enough), id);
            Assert.IsFalse(FlightDebrief.GoalMet(m, true, 200e3, 200e3, 3, "good", shortBy1), id + " one m/s short");
            Assert.IsFalse(FlightDebrief.GoalMet(m, true, 200e3, 150e3, 3, "good", enough), id + " parking perigee out of band");
            Assert.IsFalse(FlightDebrief.DepartureMet(m, new GoalCheck()), "no departure budget known");
        }

        [Test]
        public void TheIdealStackDv_NoLongerPassesADeparture()
        {
            // Main's check compared the pad's ideal stack Δv with the mission ask. A departure now needs the Δv left in orbit.
            var m = campaign.ById("m7");
            var extra = new GoalCheck { IdealDv = 20000, C3Km2PerS2 = -60 };
            Assert.IsFalse(FlightDebrief.GoalMet(m, true, 200e3, 200e3, 3, "good", extra));
        }
    }
}
