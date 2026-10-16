// Fix it closes a real Δv shortfall in one undo step, and says why when it cannot.
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DvMarginFixTests
    {
        Catalog catalog;
        HangarData hangar;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
        }

        [Test]
        public void HopperOnFirstHopResolvesAndHasSpeedToSpare()
        {
            var design = HangarFixtures.Template("hopper");
            JsonMut.Set(design.Obj("payload"), "orbit", "kh-m1");
            JsonMut.Set(design.Obj("payload"), "mass", 50d);
            var shape = Validate.Shape(catalog, design);
            Assert.IsFalse(shape.errors.Exists(e => e.Contains("payload.orbit")), string.Join("; ", shape.errors));
            var readout = Readout.Quick(catalog, design);
            Assert.Less(readout.required, 2000, "a 10 km hop is not LEO");
            Assert.GreaterOrEqual(readout.margin, 0);
            var tanks = StepperModel.Evaluate(catalog, design)[DesignerStep.Tanks];
            Assert.AreNotEqual(StepGate.Red, tanks.Gate);
        }

        [Test]
        public void FixItClosesLeoShortfallInOneUndoStep()
        {
            var design = HangarFixtures.Template("hopper");
            JsonMut.Set(design.Obj("payload"), "orbit", "leo400");
            JsonMut.Set(design.Obj("payload"), "mass", 50d);
            JsonMut.Set(design, "budgetKc", 90000000d);
            var beforeRead = Readout.Quick(catalog, design);
            Assert.Less(beforeRead.margin, 0);
            var session = new HangarSession(catalog, hangar, design);
            string before = HangarSession.Snapshot(session.Design);
            double payload = session.Design.Obj("payload").Num("mass");
            var result = session.FixDeltaV();
            Assert.IsTrue(result.Solved, result.Reason);
            Assert.IsTrue(result.Changed);
            Assert.AreEqual(1, session.history.UndoCount);
            var after = Readout.Quick(catalog, session.Design);
            Assert.GreaterOrEqual(after.margin, 0, result.Reason);
            Assert.Greater(after.liftoffTWR, 1);
            Assert.AreEqual(payload, session.Design.Obj("payload").Num("mass"));
            Assert.AreEqual("leo400", session.Design.Obj("payload").Str("orbit"));
            var shape = Validate.Shape(catalog, session.Design);
            Assert.IsTrue(shape.ok, string.Join("; ", shape.errors));
            session.Undo();
            Assert.AreEqual(before, HangarSession.Snapshot(session.Design));
            Assert.Less(Readout.Quick(catalog, session.Design).margin, 0);
        }

        [Test]
        public void ImpossibleTripNamesTheLimitAndDoesNotChangeTheRocket()
        {
            var mission = new CampaignMission
            {
                Id = "too-high",
                Name = "Too High",
                TargetKind = "apogee",
                ApoKm = 100000,
                BudgetKc = 1000,
                MassKg = 50,
            };
            HomeAutoBuild.EnsureOrbit(catalog, mission);
            var design = HangarFixtures.Template("hopper");
            JsonMut.Set(design.Obj("payload"), "orbit", HomeAutoBuild.OrbitKey(mission));
            JsonMut.Set(design.Obj("payload"), "mass", 50d);
            JsonMut.Set(design, "budgetKc", 1000d);
            var session = new HangarSession(catalog, hangar, design);
            string before = HangarSession.Snapshot(session.Design);
            var result = session.FixDeltaV();
            Assert.IsFalse(result.Solved);
            Assert.IsFalse(result.Changed);
            Assert.AreEqual(0, session.history.UndoCount);
            Assert.AreEqual(before, HangarSession.Snapshot(session.Design));
            StringAssert.Contains("Lighten the cargo", result.Reason);
            StringAssert.Contains("budget", result.Reason);
            StringAssert.Contains("different mission", result.Reason);
            StringAssert.Contains("1000 Kc", result.Reason);
        }

        [Test]
        public void UnknownOrbitIsNamedInsteadOfPretendingItIsLeo()
        {
            var design = HangarFixtures.Template("hopper");
            JsonMut.Set(design.Obj("payload"), "orbit", "kh-missing");
            double prop = ((JsonObject)design.Arr("stages")[0]).Num("propMass");
            var result = DvMarginFix.Solve(catalog, design);
            Assert.IsFalse(result.Solved);
            StringAssert.Contains("kh-missing", result.Reason);
            StringAssert.Contains("not a known orbit", result.Reason);
            StringAssert.Contains("Lighten the cargo", result.Reason);
            StringAssert.Contains("different mission", result.Reason);
            Assert.AreEqual(prop, ((JsonObject)result.Design.Arr("stages")[0]).Num("propMass"));
        }

        [Test]
        public void EveryMissionAndContractOrbitIsInTheCatalog()
        {
            var campaign = Campaign.Load(Golden.Root);
            var pool = ContractGen.Load(Golden.Root);
            var raw = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "payloads.json")));
            var orbits = raw.Obj("orbits");
            Assert.IsNotNull(orbits);
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                Assert.IsFalse(string.IsNullOrEmpty(mission.OrbitId), mission.Id);
                Assert.IsTrue(orbits.ContainsKey(mission.OrbitId), mission.Id);
                Assert.IsTrue(catalog.Orbits.ContainsKey(mission.OrbitId), mission.OrbitId);
                double stack = Stack.RequiredDeltaV(catalog, mission.OrbitId, HomeAutoBuild.LatitudeDeg).required;
                double asked = mission.RequiredDeltaV(HomeAutoBuild.LatitudeDeg);
                Assert.That(System.Math.Abs(stack - asked), Is.LessThan(50), mission.Id + " stack " + stack + " asked " + asked);
            }
            for (int i = 0; i < pool.Orbits.Count; i++)
            {
                var orbit = pool.Orbits[i];
                Assert.IsTrue(orbits.ContainsKey(orbit.Id), orbit.Id);
                Assert.IsTrue(catalog.Orbits.ContainsKey(orbit.Id), orbit.Id);
            }
        }
    }
}
