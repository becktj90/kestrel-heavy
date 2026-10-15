// S5 event odds. Nine standard engines at rated chamber pressure are a 4% burn.
// Gentle turns the draws off. Deterministic failures stay on. The predict list carries the odds.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class EventModelTests
    {
        [Test]
        public void NineEngines_Standard_AtRated_AreFourPercent()
        {
            double p = EventModel.EngineAnomaly(9, "standard", 97, 97);
            Assert.AreEqual(0.04, p, 1e-12, "1-(1-p)^9 = 0.04 at the reference burn");
            Assert.AreEqual(1, EventModel.ChamberFactor(0), 1e-12);
            Assert.AreEqual(1, EventModel.AvionicsFactor("standard"), 0);
            var stages = new[] { new EventStage { engineCount = 9, pc = 97, pcRated = 97 } };
            var vehicle = new EventVehicle { avionicsTier = "standard", safetyFactor = 1.4, canStage = false };
            var odds = EventModel.Odds(stages, vehicle);
            Assert.AreEqual(0.04, odds.engineAnomaly, 1e-12);
            var schedule = EventModel.Roll(1, 1, stages, vehicle, false);
            var row = Row(RiskShellDisclose(schedule, false), "engine-out");
            StringAssert.Contains("~4%/burn", row.engineer);
            StringAssert.Contains("9 engines", row.engineer);
            StringAssert.Contains("standard avionics", row.engineer);
            StringAssert.DoesNotContain("pc margin", row.engineer);
            Assert.AreEqual("amber", row.level);
            Assert.AreEqual(0.04, row.odds, 1e-12);

            var red = new System.Collections.Generic.List<FlightRisk>
            {
                new FlightRisk { kind = "engine-out", level = "red", kid = "An engine quit.", engineer = "engine-out (engine-out): value 1.000 limit 9.000 at T+0.00 s." },
            };
            EventModel.Disclose(red, schedule, false);
            Assert.AreEqual("An engine quit.", red[0].kid);
            Assert.AreEqual("red", red[0].level);
            Assert.AreEqual(0.04, red[0].odds, 1e-12);
            StringAssert.Contains("~4%/burn", red[0].oddsLine);
        }

        [Test]
        public void DesignChoices_MoveTheThreeOdds_InTheFairDirection()
        {
            double hot = EventModel.EngineAnomaly(9, "standard", 97 * 1.10, 97);
            double rated = EventModel.EngineAnomaly(9, "standard", 97, 97);
            double cool = EventModel.EngineAnomaly(9, "standard", 97 * 0.85, 97);
            Assert.Greater(hot, rated);
            Assert.Greater(rated, cool);
            Assert.Greater(EventModel.EngineAnomaly(9, "basic", 97, 97), rated);
            Assert.Less(EventModel.EngineAnomaly(9, "redundant", 97, 97), rated);
            Assert.Greater(EventModel.EngineAnomaly(12, "standard", 97, 97), rated);
            Assert.Less(EventModel.EngineAnomaly(3, "standard", 97, 97), rated);
            Assert.AreEqual(0, EventModel.EngineAnomaly(0, "standard", 97, 97), 0);

            Assert.AreEqual(0.7, EventModel.ChamberFactor(0.15), 1e-12);
            Assert.AreEqual(4, EventModel.ChamberFactor(-0.15), 1e-12);
            Assert.AreEqual(4, EventModel.ChamberFactor(-0.40), 1e-12, "past the 1.15 cap the deterministic overpressure owns it");

            double nominal = EventModel.StagingJitter("standard", 1.4, true);
            Assert.AreEqual(0.04, nominal, 1e-12);
            Assert.Greater(EventModel.StagingJitter("standard", 1.1, true), nominal);
            Assert.Less(EventModel.StagingJitter("standard", 1.8, true), nominal);
            Assert.Greater(EventModel.StagingJitter("basic", 1.4, true), nominal);
            Assert.Less(EventModel.StagingJitter("redundant", 1.4, true), nominal);
            Assert.AreEqual(0, EventModel.StagingJitter("basic", 1.1, false), 0);

            double bare = EventModel.RecoveryGust("standard", 1.4, 0, true);
            double saved = EventModel.RecoveryGust("standard", 1.4, 0.10, true);
            double full = EventModel.RecoveryGust("standard", 1.4, 0.20, true);
            Assert.Greater(bare, saved);
            Assert.Greater(saved, full);
            Assert.Greater(EventModel.RecoveryGust("standard", 1.1, 0.10, true), saved);
            Assert.Less(EventModel.RecoveryGust("redundant", 1.4, 0.10, true), saved);
            Assert.Greater(EventModel.RecoveryGust("basic", 1.4, 0.10, true), saved);
            Assert.AreEqual(0, EventModel.RecoveryGust("basic", 1.0, 0, false), 0);
        }

        [Test]
        public void SameSeed_Replays_AndGentleConsumesTheStreamWithoutFiring()
        {
            Assert.AreEqual(0x7f4978c768aa6bf5UL, EventModel.Seed(1, 1));
            ulong rng = EventModel.Seed(1, 1);
            Assert.AreEqual(0.1101340621731308, EventModel.NextUniform(ref rng), 1e-12);
            ulong other = EventModel.Seed(1, 2);
            Assert.AreNotEqual(EventModel.Seed(1, 1), other);

            var stages = new[]
            {
                new EventStage { engineCount = 9, pc = 300, pcRated = 300 },
                new EventStage { engineCount = 1, pc = 300, pcRated = 300 },
            };
            var vehicle = new EventVehicle
            {
                avionicsTier = "standard", safetyFactor = 1.4, reserveFraction = 0.10,
                landing = true, canStage = true,
            };
            var a = EventModel.Roll(0xA11CE, 42, stages, vehicle, false);
            var b = EventModel.Roll(0xA11CE, 42, stages, vehicle, false);
            AssertSame(a, b);
            var quiet = EventModel.Roll(0xA11CE, 42, stages, vehicle, true);
            Assert.IsFalse(quiet.EngineAnomaly(0));
            Assert.IsFalse(quiet.EngineAnomaly(1));
            Assert.AreEqual(1, quiet.SepScale(1), 0);
            Assert.AreEqual(0, quiet.GustMps, 0);
            Assert.Greater(quiet.odds.engineAnomaly, 0, "the design odds stay visible as latent");
            Assert.AreEqual(a.odds.engineAnomaly, quiet.odds.engineAnomaly, 0);
            Assert.AreEqual(a.odds.stagingJitter, quiet.odds.stagingJitter, 0);
            Assert.AreEqual(a.odds.recoveryGust, quiet.odds.recoveryGust, 0);
        }

        [Test]
        public void Gentle_DisablesRandomEvents_AndDeterministicFailuresStay()
        {
            var cryo = new FailureSample
            {
                cryoFail = true, gentle = true, stage = 1,
                margin = double.PositiveInfinity, car = double.PositiveInfinity,
            };
            var scratch = FailureModel.Fresh(1, 1);
            var hit = FailureModel.Evaluate(cryo, ref scratch);
            Assert.IsTrue(hit.on);
            Assert.AreEqual(FailureKind.CryoMaterial, hit.kind);

            var roll = new FailureSample
            {
                gentle = true, rollEngines = true, engineCount = 4, reliability = 1e-4, stage = 1,
                margin = double.PositiveInfinity, car = double.PositiveInfinity, stability = 1,
            };
            scratch = FailureModel.Fresh(9, 9);
            hit = FailureModel.Evaluate(roll, ref scratch);
            Assert.IsFalse(hit.on, "gentle does not roll engine-out");

            roll.gentle = false;
            scratch = FailureModel.Fresh(9, 9);
            hit = FailureModel.Evaluate(roll, ref scratch);
            Assert.IsTrue(hit.on);
            Assert.AreEqual(FailureKind.EngineOut, hit.kind);

            var over = new FailureSample
            {
                gentle = true, qPa = 50000, qLimPa = 30000, stage = 1,
                margin = double.PositiveInfinity, car = double.PositiveInfinity,
            };
            scratch = FailureModel.Fresh(1, 1);
            hit = FailureModel.Evaluate(over, ref scratch);
            Assert.AreEqual(FailureKind.StructuralMaxQ, hit.kind);
        }

        [Test]
        public void Predict_ShowsOdds_OnTheRiskRows_WithoutTouchingOtherKinds()
        {
            var cat = Golden.Catalog;
            var d = cat.Preset("scout").Clone();
            d.version = 3;
            d.tier = "campaign";
            d.avionicsTier = "standard";
            d.safetyFactor = 1.4;
            d.stages[0].engine = "gg-kerolox";
            d.stages[0].engineCount = 9;
            d.stages[0].engineParams = null;
            var upper = d.stages[0].Clone();
            upper.engineCount = 1;
            upper.sepMode = "springs";
            upper.sepImpulseSet = true;
            upper.sepImpulse = 500;
            d.stages.Add(upper);
            d.legs = true;
            d.legsKind = "light";
            d.recoveryMode = "rtls";
            d.landingReserveSet = true;
            d.landingReserveFraction = 0.10;

            var mon = FailureMonitor.Create(cat, d, 200000, 1, 1);
            Assert.IsFalse(mon.Gentle);
            var risks = mon.BuildRisks();
            Assert.AreEqual(11, risks.Count);
            var engine = Find(risks, "engine-out");
            Assert.AreEqual(0.04, engine.odds, 1e-9);
            StringAssert.Contains("engine-out ~4%/burn (9 engines, standard avionics)", engine.engineer);
            Assert.AreEqual("amber", engine.level);
            Assert.IsFalse(string.IsNullOrEmpty(engine.kid));

            var sep = Find(risks, "separation-collision");
            Assert.AreEqual(0.04, sep.odds, 1e-12);
            StringAssert.Contains("~4%/sep", sep.engineer);
            Assert.AreEqual("amber", sep.level);

            var legs = Find(risks, "leg-collapse");
            Assert.Greater(legs.odds, 0.02);
            StringAssert.Contains("/landing", legs.engineer);
            StringAssert.Contains("reserve 10%", legs.engineer);
            Assert.AreEqual("amber", legs.level);

            var structure = Find(risks, "structural-maxq");
            Assert.AreEqual(-1, structure.odds, 0);
            Assert.IsNull(structure.oddsLine);

            d.tier = "kid";
            var quiet = FailureMonitor.Create(cat, d, 200000, 1, 1);
            var quietRisks = quiet.BuildRisks();
            var quietEngine = Find(quietRisks, "engine-out");
            Assert.AreEqual(0, quietEngine.odds, 0);
            StringAssert.Contains("gentle: random events off", quietEngine.engineer);
            Assert.AreNotEqual("amber", quietEngine.level);
            Assert.AreEqual(0, quiet.Events.GustMps, 0);
            Assert.AreEqual(1, quiet.Events.SepScale(1), 0);
            Assert.IsFalse(quiet.Events.EngineAnomaly(0));
        }

        [Test]
        public void StagingJitter_ScalesSeparationSpeed_AndCanCollide()
        {
            var cat = Golden.Catalog;
            bool sawShort = false, sawNominal = false;
            for (ulong seed = 1; seed <= 200 && !(sawShort && sawNominal); seed++)
            {
                var mon = Staged(cat, "campaign", seed);
                double scale = mon.Events.SepScale(1);
                const double mass = 1000;
                // 255 N·s on two 1 t masses is 0.51 m/s. v_min at q = 0 is 0.50 m/s.
                // Every short push (scale ≤ 0.95) falls under v_min. A nominal push does not.
                double bare = RecoveryModel.SeparationRelSpeed(255, mass, mass);
                double scaled = RecoveryModel.SeparationRelSpeed(255 * scale, mass, mass);
                Assert.AreEqual(bare * scale, scaled, 1e-9, "v_rel stays linear in impulse");
                Assert.AreEqual(0.51, bare, 1e-9);
                double vMin = RecoveryModel.MinRelSpeed(0);
                Assert.AreEqual(0.5, vMin, 1e-9);
                bool expect = scaled < vMin;
                bool hit = mon.OnSeparation(12, 40000, 0, 0, mass, mass);
                Assert.AreEqual(expect, hit, "seed " + seed + " scale " + scale);
                if (hit)
                {
                    Assert.AreEqual("separation-collision", mon.Event.kind);
                    sawShort = true;
                }
                else sawNominal = true;
            }
            Assert.IsTrue(sawShort, "a short push falls under v_min");
            Assert.IsTrue(sawNominal, "a nominal push stays clear");

            var gentle = Staged(cat, "kid", 1);
            Assert.IsFalse(gentle.OnSeparation(12, 40000, 0, 0, 1000, 1000));
            Assert.IsNull(gentle.Event);
        }

        [Test]
        public void RecoveryGust_AddsSink_AndLegsStillUseTheSameLimit()
        {
            var cat = Golden.Catalog;
            bool sawFold = false, sawHold = false;
            for (ulong seed = 1; seed <= 80 && !(sawFold && sawHold); seed++)
            {
                var mon = Lander(cat, "campaign", seed);
                double gust = mon.Events.GustMps;
                double sink = 1.0 + gust;
                bool expect = sink > RecoveryModel.VLeg("light");
                bool hit = mon.OnImpact(400, 1.0);
                Assert.AreEqual(expect, hit, "seed " + seed + " gust " + gust);
                if (hit)
                {
                    Assert.AreEqual("leg-collapse", mon.Event.kind);
                    Assert.Greater(mon.Event.value, 1.0);
                    sawFold = true;
                }
                else sawHold = true;
            }
            Assert.IsTrue(sawFold);
            Assert.IsTrue(sawHold);

            var gentle = Lander(cat, "1", 1);
            Assert.AreEqual(0, gentle.Events.GustMps, 0);
            Assert.IsFalse(gentle.OnImpact(400, 1.0), "1 m/s is under the 2 m/s light-leg limit when the gust is off");
            Assert.IsTrue(gentle.OnImpact(401, 2.5), "deterministic leg collapse still fires in gentle");
            Assert.AreEqual("leg-collapse", gentle.Event.kind);
        }

        static void AssertSame(EventSchedule a, EventSchedule b)
        {
            Assert.AreEqual(a.odds.engineAnomaly, b.odds.engineAnomaly, 0);
            Assert.AreEqual(a.odds.stagingJitter, b.odds.stagingJitter, 0);
            Assert.AreEqual(a.odds.recoveryGust, b.odds.recoveryGust, 0);
            Assert.AreEqual(a.EngineAnomaly(0), b.EngineAnomaly(0));
            Assert.AreEqual(a.EngineAnomaly(1), b.EngineAnomaly(1));
            Assert.AreEqual(a.EngineIndex(0), b.EngineIndex(0));
            Assert.AreEqual(a.EngineIndex(1), b.EngineIndex(1));
            Assert.AreEqual(a.SepScale(1), b.SepScale(1), 0);
            Assert.AreEqual(a.GustMps, b.GustMps, 0);
        }

        static System.Collections.Generic.List<FlightRisk> RiskShell()
        {
            return new System.Collections.Generic.List<FlightRisk>
            {
                new FlightRisk { kind = "structural-maxq", level = "green", margin = 0.4, engineer = "structural-maxq margin 0.40" },
                new FlightRisk { kind = "engine-out", level = "green", margin = 0.5, engineer = "engine-out margin 0.50" },
                new FlightRisk { kind = "separation-collision", level = "green", margin = 1, engineer = "separation-collision margin 1.00" },
                new FlightRisk { kind = "leg-collapse", level = "green", margin = 1, engineer = "leg-collapse margin 1.00" },
            };
        }

        static System.Collections.Generic.List<FlightRisk> RiskShellDisclose(EventSchedule schedule, bool gentle)
        {
            var list = RiskShell();
            EventModel.Disclose(list, schedule, gentle);
            return list;
        }

        static FlightRisk Row(System.Collections.Generic.List<FlightRisk> risks, string kind)
        {
            var row = Find(risks, kind);
            Assert.IsNotNull(row, kind);
            return row;
        }

        static FlightRisk Find(System.Collections.Generic.List<FlightRisk> risks, string kind)
        {
            for (int i = 0; i < risks.Count; i++)
                if (risks[i].kind == kind) return risks[i];
            return null;
        }

        static FailureMonitor Staged(Catalog cat, string tier, ulong seed)
        {
            var d = cat.Preset("scout").Clone();
            d.version = 3;
            d.tier = tier;
            d.avionicsTier = "basic";
            d.safetyFactor = 1.4;
            var upper = d.stages[1];
            upper.sepMode = "springs";
            upper.sepImpulseSet = true;
            upper.sepImpulse = 255;
            return FailureMonitor.Create(cat, d, 200000, 7, seed);
        }

        static FailureMonitor Lander(Catalog cat, string tier, ulong seed)
        {
            var d = cat.Preset("scout").Clone();
            d.version = 3;
            d.tier = tier;
            d.avionicsTier = "standard";
            d.safetyFactor = 1.4;
            d.legs = true;
            d.legsKind = "light";
            d.recoveryMode = "rtls";
            d.landingReserveSet = true;
            d.landingReserveFraction = 0;
            return FailureMonitor.Create(cat, d, 200000, 11, seed);
        }
    }
}
