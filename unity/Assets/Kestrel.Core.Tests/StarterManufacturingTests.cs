// Starter manufacturing grades: Shop is cheaper and holds together worse; Flight costs more and holds better.
// Standard leaves the campaign winner byte-identical (DECISIONS #87).
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterManufacturingTests
    {
        [Test]
        public void Standard_LeavesTheWinnerUnchanged()
        {
            var r = StarterFixture.Rocket("m3");
            Assert.AreEqual(StarterBuild.Standard, r.BuildGrade);
            var stamped = StarterBases.For("m3");
            HomeAutoBuild.ApplyMission(StarterFixture.Catalog, stamped, r.Mission);
            Assert.AreEqual(HangarSession.Snapshot(stamped), HangarSession.Snapshot(r.BuildDesign()));
            Assert.IsNull(r.BuildDesign().Str("manufacturing"));
        }

        [Test]
        public void Shop_IsCheaperAndLessReliable_ThanFlight()
        {
            var r = StarterFixture.Rocket("m5");
            var shop = r.BuildDesign(r.Choice.WithBuild(StarterBuild.Shop));
            var flight = r.BuildDesign(r.Choice.WithBuild(StarterBuild.Flight));
            Assert.AreEqual("shop", shop.Str("manufacturing"));
            Assert.AreEqual("flight", flight.Str("manufacturing"));
            Assert.AreEqual("basic", shop.Obj("avionics").Str("tier"));
            Assert.AreEqual("redundant", flight.Obj("avionics").Str("tier"));
            Assert.AreEqual(StarterManufacturing.ShopSafety, shop.Num("safetyFactor"), 1e-9);
            Assert.GreaterOrEqual(flight.Num("safetyFactor"), StarterManufacturing.FlightSafety);

            double shopCost = StarterManufacturing.CostKc(r.Catalog, shop);
            double flightCost = StarterManufacturing.CostKc(r.Catalog, flight);
            double shopHold = StarterManufacturing.HoldTogether(r.Catalog, shop);
            double flightHold = StarterManufacturing.HoldTogether(r.Catalog, flight);
            Assert.Less(shopCost, flightCost, "Shop must cost less than Flight");
            Assert.Less(shopHold, flightHold, "Shop must hold together worse than Flight");
            Assert.That(StarterManufacturing.SpendLine(r.Catalog, shop, r.Mission.BudgetKc), Does.Contain("Holds"));
            Assert.That(StarterManufacturing.SpendLine(r.Catalog, shop, r.Mission.BudgetKc), Does.Contain("Kc"));
        }

        [Test]
        public void Flight_DoesNotLowerWinnerSafety()
        {
            // m5 winner is already 1.7 with titanium + redundant; Safe must keep or raise that floor.
            var r = StarterFixture.Rocket("m5");
            double baseSf = r.BuildDesign().Num("safetyFactor");
            Assert.Greater(baseSf, StarterManufacturing.FlightSafety);
            var flight = r.BuildDesign(r.Choice.WithBuild(StarterBuild.Flight));
            Assert.AreEqual(baseSf, flight.Num("safetyFactor"), 1e-9);
        }

        [Test]
        public void Shop_HoldTogether_IncludesEnginePenalty()
        {
            var r = StarterFixture.Rocket("m5");
            var shop = r.BuildDesign(r.Choice.WithBuild(StarterBuild.Shop));
            double withScale = StarterManufacturing.HoldTogether(r.Catalog, shop);
            shop["manufacturing"] = "standard";
            double withoutScale = StarterManufacturing.HoldTogether(r.Catalog, shop);
            Assert.Less(withScale, withoutScale, "Shop Holds % must reflect the 0.92 engine scale");
        }

        [Test]
        public void EngineReliabilityScale_OnlyShopCutsCorners()
        {
            Assert.AreEqual(0.92, StarterManufacturing.EngineReliabilityScale("shop"), 1e-9);
            Assert.AreEqual(1.0, StarterManufacturing.EngineReliabilityScale("standard"), 1e-9);
            Assert.AreEqual(1.0, StarterManufacturing.EngineReliabilityScale("flight"), 1e-9);
            Assert.AreEqual(1.0, StarterManufacturing.EngineReliabilityScale(null), 1e-9);
        }

        [Test]
        public void SaveRoundTripsBuildGrade()
        {
            var r = StarterFixture.Rocket("m1");
            r.BuildGrade = StarterBuild.Flight;
            var saved = r.ToSave();
            Assert.AreEqual(3, (int)saved.Num("v"));
            Assert.AreEqual("flight", saved.Str("build"));
            var again = StarterFixture.Rocket("m1");
            Assert.IsTrue(again.ApplySave(saved));
            Assert.AreEqual(StarterBuild.Flight, again.BuildGrade);
        }
    }
}
