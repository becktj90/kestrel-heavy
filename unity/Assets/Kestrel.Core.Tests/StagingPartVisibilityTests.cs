// Which painted Hangar parts the flight picture hides at each separation (StagingPartVisibility), engine-free.
// Since #288 the painted parts are the visible rocket; FlightStageShow hides each one its moving piece replaces,
// on the same Released()/FairingAt() test, so nothing is drawn twice and nothing pops back until a new flight.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StagingPartVisibilityTests
    {
        /// <summary>Two strap-ons, then a three-stage core (KH-10 / KH-19 shape).</summary>
        static List<StagingBody> HeavyPlan() => FlightStagingPlan.Bodies(new List<SerialStage>
        {
            new SerialStage { role = "strapons+core", label = "Core + 2 strap-ons" },
            new SerialStage { role = "core", index = 0, label = "Stage 1" },
            new SerialStage { role = "core", index = 1, label = "Stage 2" },
            new SerialStage { role = "core", index = 2, label = "Stage 3" },
        }, 2);

        [Test]
        public void AssembledStackHidesNothingAndKeepsThePayloadInside()
        {
            var v = new StagingPartVisibility(HeavyPlan(), 3, true);
            Assert.IsFalse(v.Update(_ => false, false));
            Assert.AreEqual(0, v.Hidden.Count);
            Assert.IsFalse(v.PayloadShown);
            Assert.IsFalse(v.FairingSeparated);
        }

        [Test]
        public void FairingSeparationHidesThePaintedFairingAndRevealsThePayload()
        {
            var v = new StagingPartVisibility(HeavyPlan(), 3, true);
            Assert.IsTrue(v.Update(_ => false, true), "the first separated frame is a change");
            Assert.IsTrue(v.IsHidden(PartRef.Fairing));
            Assert.IsTrue(v.PayloadShown);
            Assert.IsTrue(v.FairingSeparated);
            Assert.IsFalse(v.IsHidden(PartRef.Stage(0)));
            Assert.IsFalse(v.Update(_ => false, true), "later frames change nothing");
        }

        [Test]
        public void ARocketWithoutAFairingNeverRevealsThePayload()
        {
            var v = new StagingPartVisibility(HeavyPlan(), 3, false);
            Assert.IsFalse(v.Update(_ => false, true));
            Assert.IsFalse(v.IsHidden(PartRef.Fairing));
            Assert.IsFalse(v.PayloadShown);
        }

        [Test]
        public void EachReleaseHidesItsPaintedBodyAndTheInterstageRidingOnIt()
        {
            var plan = HeavyPlan();
            var v = new StagingPartVisibility(plan, 3, true);
            int boosterSerial = plan.Find(b => b.Role == "booster").Serial;
            int s0 = plan.Find(b => b.Role == "stage" && b.Index == 0).Serial;
            int s1 = plan.Find(b => b.Role == "stage" && b.Index == 1).Serial;
            var gone = new HashSet<int>();

            gone.Add(boosterSerial);
            Assert.IsTrue(v.Update(gone.Contains, false));
            CollectionAssert.AreEquivalent(new[] { PartRef.Boosters }, v.Hidden);

            gone.Add(s0);
            Assert.IsTrue(v.Update(gone.Contains, false));
            CollectionAssert.AreEquivalent(new[] { PartRef.Boosters, PartRef.Stage(0), PartRef.Interstage(1) }, v.Hidden);

            Assert.IsTrue(v.Update(gone.Contains, true));
            gone.Add(s1);
            Assert.IsTrue(v.Update(gone.Contains, true));
            CollectionAssert.AreEquivalent(new[]
            {
                PartRef.Boosters, PartRef.Stage(0), PartRef.Interstage(1), PartRef.Fairing, PartRef.Stage(1), PartRef.Interstage(2),
            }, v.Hidden);
            Assert.IsFalse(v.IsHidden(PartRef.Stage(2)), "the last stage stays on the stack");
            Assert.IsFalse(v.IsHidden(PartRef.Payload));
        }

        [Test]
        public void NothingPopsBackUntilReset()
        {
            var plan = HeavyPlan();
            var v = new StagingPartVisibility(plan, 3, true);
            int s0 = plan.Find(b => b.Role == "stage" && b.Index == 0).Serial;
            v.Update(serial => serial == s0, true);
            Assert.IsFalse(v.Update(_ => false, false), "a later frame that reads nothing released changes nothing");
            Assert.IsTrue(v.IsHidden(PartRef.Stage(0)));
            Assert.IsTrue(v.IsHidden(PartRef.Fairing));
            Assert.IsTrue(v.PayloadShown);

            v.Reset();
            Assert.AreEqual(0, v.Hidden.Count);
            Assert.IsFalse(v.PayloadShown);
            Assert.IsFalse(v.FairingSeparated);
            Assert.IsTrue(v.Update(serial => serial == s0, true), "the next flight separates again");
        }

        [Test]
        public void HidesOnTheSameTickTheStagingPictureReleasesThePiece()
        {
            var plan = FlightStagingPlan.Bodies(new List<SerialStage>
            {
                new SerialStage { role = "core", index = 0, label = "Stage 1" },
                new SerialStage { role = "core", index = 1, label = "Stage 2" },
            }, 0);
            var events = new List<SimEvent>
            {
                new SimEvent { t = 80, type = "burnout", stage = 1, alt = 42000, v = 1860 },
                new SimEvent { t = 80, type = "separation", stage = 1, alt = 42000, v = 1860 },
                new SimEvent { t = 80, type = "ignition", stage = 2, alt = 42000, v = 1860 },
                new SimEvent { t = 181.182, type = "fairing", alt = 110000, v = 3000 },
            };
            var staging = new FlightStaging();
            staging.Reset(true, 110000, 0, false);
            var v = new StagingPartVisibility(plan, 2, true);
            double sepAt = 80 + FlightStaging.SepDelaySec;
            for (double t = 0; t <= 200; t += 1.0 / 120)
            {
                staging.Advance(events, t, 1000 + t * 600, 2000, 10000, 1);
                v.Update(staging, t);
                Assert.AreEqual(staging.Released(0, t), v.IsHidden(PartRef.Stage(0)), "stage 1 at T+" + t);
                Assert.AreEqual(staging.Released(0, t), v.IsHidden(PartRef.Interstage(1)), "interstage at T+" + t);
                Assert.AreEqual(staging.FairingAt(t).Separated, v.IsHidden(PartRef.Fairing), "fairing at T+" + t);
                Assert.AreEqual(staging.FairingAt(t).Separated, v.PayloadShown, "payload at T+" + t);
                Assert.IsFalse(v.IsHidden(PartRef.Stage(1)));
            }
            Assert.IsTrue(staging.Released(0, sepAt + 1e-9));
            Assert.IsTrue(v.IsHidden(PartRef.Fairing));
        }

        [Test]
        public void CampaignWinners_HideEachReleasedBodyAndTheFairingOnTheirOwnFlights()
        {
            var root = Golden.Root;
            var catalog = Catalog.LoadFromRepo(root);
            var campaign = Campaign.Load(root);
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            int flown = 0, fairings = 0, released = 0;
            foreach (JsonObject row in file.Arr("missions"))
            {
                string id = row.Str("id");
                var mission = campaign.ById(id);
                Assert.IsNotNull(mission, id);
                var design = Design.FromJson((JsonObject)row.Arr("designs")[0]);
                var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.BatchDt, null, MissionFlightSetup.BatchDt);
                var sim = new AscentSim(setup.Stages, setup.Options);
                while (!sim.Done) sim.Tick();
                var result = sim.Finish();
                Assert.IsNotNull(result.samples, id);

                bool hasFairing = design.fairingLength > 0.2 && design.fairingDiameter > 0.1;
                double jettison = design.fairingJettisonAltitudeKm > 0 ? design.fairingJettisonAltitudeKm * 1000 : 110000;
                var plan = FlightStagingPlan.Bodies(Stack.SerialStages(catalog, design).stages, design.StrapOnCount);
                var v = new StagingPartVisibility(plan, design.stages.Count, hasFairing);
                var staging = new FlightStaging();
                staging.Reset(hasFairing, jettison, design.StrapOnCount, false);
                var hiddenBefore = new HashSet<PartRef>();
                double fairingT = -1, fairingAlt = 0, apogee = 0;
                foreach (var s in result.samples)
                {
                    staging.Advance(result.events, s.t, s.alt, s.v, s.q, 1);
                    v.Update(staging, s.t);
                    if (s.alt > apogee) apogee = s.alt;
                    foreach (var p in hiddenBefore) Assert.IsTrue(v.IsHidden(p), id + " " + p + " popped back at T+" + s.t);
                    foreach (var b in plan)
                    {
                        if (b.Role != "stage") continue;
                        Assert.AreEqual(staging.Released(b.Serial, s.t), v.IsHidden(PartRef.Stage(b.Index)), id + " stage " + (b.Index + 1) + " at T+" + s.t);
                    }
                    if (plan.Exists(b => b.Role == "booster"))
                    {
                        int bs = plan.Find(b => b.Role == "booster").Serial;
                        Assert.AreEqual(staging.Released(bs, s.t), v.IsHidden(PartRef.Boosters), id + " boosters at T+" + s.t);
                    }
                    bool sep = staging.FairingAt(s.t).Separated;
                    Assert.AreEqual(sep && hasFairing, v.IsHidden(PartRef.Fairing), id + " fairing at T+" + s.t);
                    Assert.AreEqual(sep && hasFairing, v.PayloadShown, id + " payload at T+" + s.t);
                    if (sep && fairingT < 0) { fairingT = s.t; fairingAlt = s.alt; }
                    hiddenBefore.UnionWith(v.Hidden);
                }
                flown++;
                foreach (var b in plan) if (b.Releases && staging.Released(b.Serial, result.time)) released++;
                if (hasFairing && apogee > jettison + 1000 && result.outcome != "failure")
                {
                    Assert.That(fairingT, Is.GreaterThan(0), id + " climbed past " + jettison / 1000 + " km but the painted fairing never left");
                    Assert.That(fairingAlt, Is.GreaterThanOrEqualTo(jettison - 2000), id + " fairing went early");
                    fairings++;
                    TestContext.WriteLine($"{id}: fairing hidden T+{fairingT:0.0} s at {fairingAlt / 1000:0.0} km");
                }
            }
            Assert.AreEqual(20, flown);
            Assert.That(fairings, Is.GreaterThanOrEqualTo(18), "KH-3..KH-20 all fly past the jettison altitude");
            Assert.That(released, Is.GreaterThan(0));
        }
    }
}
