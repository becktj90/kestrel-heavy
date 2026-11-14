// Playthrough 2026-11-11 (Kestrel Arcade, Unity play-through of main 45b5b15a): the pass card on KH-7 and KH-15 read
// "Apogee 204 km. Goal 35786 km." and on KH-9, 10 and 17–20 "Goal 0 km.", and the narrator said "Pass at apogee 204 km.
// The goal was 35786 km." The sim flies a GTO or C3 mission to its parking orbit and passes it on the Δv left there for
// the transfer or departure burn (#313 Physics A, FlightDebrief.DepartureMet); that burn is not flown. The card and the
// narrator now say exactly that, with the sim's own numbers, for every departure mission's campaign winner.
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DepartureCardTests
    {
        static Catalog catalog;
        static Campaign campaign;
        static NarrationPool pool;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            pool = NarrationPool.Load(Golden.Root);
        }

        static Design Winner(string id)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == id) return Design.FromJson((JsonObject)row.Arr("designs")[0]);
            return null;
        }

        static IEnumerable<string> DepartureMissions()
        {
            var c = Campaign.Load(Golden.Root);
            for (int i = 1; i <= 20; i++)
            {
                var m = c.Mission(i);
                if (m != null && m.DepartsFromParking) yield return m.Id;
            }
        }

        [Test]
        public void TheDepartureMissions_AreTheOnesArcadeFlew()
        {
            CollectionAssert.AreEquivalent(new[] { "m7", "m9", "m10", "m15", "m17", "m18", "m19", "m20" }, new List<string>(DepartureMissions()));
        }

        [TestCaseSource(nameof(DepartureMissions))]
        public void DepartureWin_SaysParkedWithTheDvLeft_NotApogeeVersusGoal(string id)
        {
            var row = campaign.ById(id);
            var design = Winner(id);
            Assert.IsNotNull(design, id + " campaign winner");
            var flight = FlightBatch.Fly(catalog, row, design, 1);
            Assert.IsTrue(flight.passed, id + " winner passes (seed 1): " + flight.outcome + " " + flight.failure);

            var extra = FlightDebrief.FromFlight(flight);
            var facts = new FlightFacts { Passed = true, ApoKm = flight.apoKm, GoalKm = row.ApoKm, PeakG = flight.maxG, Seed = 1 };
            facts.FillDeparture(row, flight.apoKm * 1000, flight.periKm * 1000, extra);
            Assert.IsTrue(facts.Departs, id);
            Assert.IsTrue(facts.Parked, id + ": the winner parks; it does not fly the " + facts.BurnName + " burn");
            Assert.AreEqual(flight.departReqDv, facts.DepartureNeedMps, 1e-6, id + ": the card's burn is the batch's");
            Assert.GreaterOrEqual(facts.DepartureDvLeft + 1e-6, facts.DepartureNeedMps, id + ": a pass covers the burn");
            Assert.AreEqual(row.TargetKind == "c3" ? "departure" : "transfer", facts.BurnName, id);

            var card = FlightDebrief.Compose(null, MissionContext.Free(), facts, pool, 100000, null);
            Assert.IsTrue(card.Passed, id);
            FlightDebrief.PairText(flight.departDv, flight.departReqDv, out var left, out var need);
            string expect = "Parked " + FlightDebrief.ParkText(facts) + " with " + left + " m/s left. The " + facts.BurnName
                + " burn needs " + need + " m/s and isn't flown.";
            TestContext.WriteLine(id + ": " + card.Numbers + " | " + card.NarratorEng + " | " + card.NarratorKid);
            Assert.AreEqual(expect, card.Numbers, id);
            Assert.That(card.Numbers, Does.Not.Contain("Goal"), id);
            Assert.That(card.Numbers, Does.Not.Contain("Apogee"), id);
            Assert.That(card.Numbers, Does.Contain(((long)System.Math.Round(flight.apoKm)).ToString(CultureInfo.InvariantCulture) + " km"), id + ": the parking height is the sim's");
            Assert.Less(flight.apoKm, 1000, id + ": parked low, so 'parked' is true");
            Assert.AreNotEqual(left, need, id + ": the pair never reads equal");

            StringAssert.StartsWith("narr.pass.depart.", card.NarratorId, id);
            foreach (var said in new[] { card.NarratorEng, card.NarratorKid })
            {
                Assert.That(said, Does.Not.Contain("{"), id);
                Assert.That(said, Does.Not.Contain("apogee").IgnoreCase, id + ": " + said);
                Assert.That(said, Does.Not.Contain("goal").IgnoreCase, id + ": " + said);
                Assert.That(said, Does.Contain(left + " m/s"), id + ": " + said);
                Assert.IsFalse(PlayerText.Leak(said), said);
            }
        }

        [Test]
        public void EveryDepartureSeed_PicksADepartureLine_AndNoGenericPassLine()
        {
            var facts = new FlightFacts { Passed = true, ApoKm = 204, PeriKm = 199, GoalKm = 35786, Departs = true, TargetKind = "ellipse", DepartureDvLeft = 2480, DepartureNeedMps = 2451 };
            for (ulong seed = 1; seed <= 40; seed++)
            {
                facts.Seed = seed;
                var card = FlightDebrief.Compose(null, MissionContext.Free(), facts, pool, 100000, null);
                StringAssert.StartsWith("narr.pass.depart.", card.NarratorId, "seed " + seed);
                Assert.AreEqual("Parked in a 199 × 204 km orbit with 2480 m/s left. The transfer burn needs 2451 m/s and isn't flown.", card.Numbers);
            }
        }

        [Test]
        public void ADirectHit_AndAnOrdinaryOrbit_KeepTheApogeeLine()
        {
            var direct = new FlightFacts { Passed = true, ApoKm = 35790, PeriKm = 200, GoalKm = 35786, Departs = true, TargetKind = "ellipse", DepartureDvLeft = 10, DepartureNeedMps = 2451, ReachedDirectly = true };
            Assert.IsFalse(direct.Parked);
            Assert.AreEqual("Apogee 35790 km. Goal 35786 km.", FlightDebrief.NumberLine(null, direct, true));
            var leo = new FlightFacts { Passed = true, ApoKm = 201, GoalKm = 200 };
            Assert.IsFalse(leo.Parked);
            Assert.AreEqual("Apogee 201 km. Goal 200 km.", FlightDebrief.NumberLine(null, leo, true));
            var card = FlightDebrief.Compose(null, MissionContext.Free(), leo, pool, 100000, null);
            Assert.That(card.NarratorId, Does.Not.StartWith("narr.pass.depart."));
        }

        [Test]
        public void FillDeparture_ReadsTheRowAndTheFlight()
        {
            var row = campaign.ById("m9");
            var facts = new FlightFacts { ApoKm = 205 };
            facts.FillDeparture(row, 205000, 198000, new GoalCheck { DepartureDv = 3300, DepartureTW = 0.8, C3Km2PerS2 = -60 });
            Assert.IsTrue(facts.Parked);
            Assert.AreEqual(198, facts.PeriKm, 1e-9);
            Assert.AreEqual(MissionScore.DepartureRequiredMps("c3", row.ApoKm, row.C3Km2PerS2, row.GuidanceAltM, 0.8), facts.DepartureNeedMps, 1e-9);
            Assert.AreEqual("departure", facts.BurnName);
            var leo = new FlightFacts();
            leo.FillDeparture(campaign.ById("m3"), 200000, 200000, new GoalCheck { DepartureDv = 100 });
            Assert.IsFalse(leo.Departs);
            Assert.IsFalse(leo.Parked);
        }
    }
}
