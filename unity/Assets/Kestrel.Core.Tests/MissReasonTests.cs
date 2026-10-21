// F4: the report says why an intact, Ready starter missed, with its own numbers, and what to change.
// KH-3, 4, 5, 6, 8, 12, 14 and 16 stay unchanged as design puzzles: each real miss (FlightBatch, seed 1)
// is pinned to its exact text, and the advice is checked by flying it.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissReasonTests
    {
        Catalog catalog;
        Campaign campaign;
        HangarData hangar;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
        }

        CampaignMission Mission(string id)
        {
            foreach (var m in campaign.Missions) if (m.Id == id) return m;
            Assert.Fail("no mission " + id);
            return null;
        }

        Design Starter(CampaignMission m) => Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);

        MissExplanation Fly(CampaignMission m, Design d, out CampaignFlight flight)
        {
            flight = FlightBatch.Fly(catalog, m, d, 1);
            return MissReason.Explain(m, MissReason.FromFlight(flight, d.stages.Count));
        }

        [TestCase("m3", "apo-high",
            "Missed the 200 km circle: the orbit flew 192 km × 221 km, and the high point needs 190 km to 210 km. That is about 6 m/s too fast at the low point.",
            "Add fuel to stage 2, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m4", "apo-high",
            "Missed the 500 km circle: the orbit flew 502 km × 2,637 km, and the high point needs 475 km to 525 km. That is about 495 m/s too fast at the low point.",
            "Add fuel to stage 2, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m5", "apo-high",
            "Missed the 400 km circle: the orbit flew 393 km × 1,898 km, and the high point needs 380 km to 420 km. That is about 373 m/s too fast at the low point.",
            "Add fuel to stage 2, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m6", "apo-high",
            "Missed the 200 km circle: the orbit flew 193 km × 216 km, and the high point needs 190 km to 210 km. That is about 5 m/s too fast at the low point.",
            "Add fuel to stage 3, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m8", "apo-high",
            "Missed the 400 km circle: the orbit flew 393 km × 1,728 km, and the high point needs 380 km to 420 km. That is about 335 m/s too fast at the low point.",
            "Add fuel to stage 2, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m12", "apo-high",
            "Missed the 600 km circle: the orbit flew 592 km × 649 km, and the high point needs 570 km to 630 km. That is about 13 m/s too fast at the low point.",
            "Add fuel to stage 2, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m14", "apo-high",
            "Missed the 400 km circle: the orbit flew 391 km × 1,059 km, and the high point needs 380 km to 420 km. That is about 176 m/s too fast at the low point.",
            "Add fuel to stage 2, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m16", "reliability",
            "Reliability was 97.9%; this mission needs 98%.",
            "Raise the safety factor to 1.5 or more for the reliability bonus, or pick redundant avionics.")]
        public void StarterMissSaysWhyAndWhatToChange(string id, string code, string reason, string advice)
        {
            var m = Mission(id);
            var x = Fly(m, Starter(m), out var flight);
            Assert.IsNull(flight.failure, id + " flies intact");
            Assert.IsFalse(flight.passed, id + " misses");
            Assert.IsNotNull(x, id + " has a reason");
            Assert.AreEqual(code, x.Code);
            Assert.AreEqual(reason, x.Reason);
            Assert.AreEqual(advice, x.Advice);
            Assert.AreEqual(reason + " " + advice, x.Line);
        }

        [TestCase("m1")]
        [TestCase("m2")]
        [TestCase("m7")]
        [TestCase("m11")]
        [TestCase("m13")]
        [TestCase("m15")]
        public void PassingStarterHasNoReason(string id)
        {
            var m = Mission(id);
            var x = Fly(m, Starter(m), out var flight);
            Assert.IsTrue(flight.passed, id + " passes");
            Assert.IsNull(x);
        }

        /// <summary>A failure event explains itself: a batch row with a failure kind gets no miss reason.</summary>
        [Test]
        public void FailureEventExplainsItselfNotTheMissReason()
        {
            var m = Mission("m4");
            var d = Starter(m);
            var flight = FlightBatch.Fly(catalog, m, d, 1);
            flight.failure = "structural-maxq";
            var input = MissReason.FromFlight(flight, d.stages.Count);
            Assert.IsFalse(input.Intact);
            Assert.IsNull(MissReason.Explain(m, input));
        }

        /// <summary>The overshoot advice works: some +2% step up to +10% on the top stage brings the high point into the band.</summary>
        [TestCase("m3", 1.02)]
        [TestCase("m4", 1.04)]
        [TestCase("m5", 1.02)]
        [TestCase("m6", 1.02)]
        [TestCase("m8", 1.02)]
        [TestCase("m12", 1.10)]
        [TestCase("m14", 1.04)]
        public void MoreTopStageFuelBringsTheHighPointIn(string id, double first)
        {
            var m = Mission(id);
            double found = double.NaN;
            for (int step = 1; step <= 5 && double.IsNaN(found); step++)
            {
                double k = 1 + 0.02 * step;
                var d = Starter(m);
                d.stages[d.stages.Count - 1].propMass *= k;
                var x = Fly(m, d, out var flight);
                if (flight.failure == null && (x == null || (x.Code != "apo-high" && x.Code != "peri-low" && x.Code != "orbit-low")))
                    found = k;
            }
            Assert.AreEqual(first, found, 1e-9, id + ": first top-stage step that lands the circle");
        }

        [TestCase("m8")]
        [TestCase("m16")]
        public void SafetyFactorAdviceClearsTheReliabilityGate(string id)
        {
            var m = Mission(id);
            var d = Starter(m);
            d.safetyFactor = MissReason.SafetyBonusAt;
            var x = Fly(m, d, out var flight);
            Assert.IsTrue(flight.passed, id + " passes at safety factor " + MissReason.SafetyBonusAt + ": " + x?.Line);
        }

        [Test]
        public void SafetyBonusMatchesTheEconomy()
            => Assert.AreEqual(catalog.Econ.Obj("reliability").Num("highSafetyFactor"), MissReason.SafetyBonusAt, 1e-12);

        static CampaignMission Row(string kind, double apoKm, double periKm)
            => new CampaignMission { Id = "t", TargetKind = kind, ApoKm = apoKm, PeriKm = periKm };

        static MissInput Flown(double apoKm, double periKm, int stages = 2)
            => new MissInput { ApoM = apoKm * 1000, PeriM = periKm * 1000, MaxG = 3, Stages = stages };

        [Test]
        public void HopShortSaysHowHighItGot()
        {
            var x = MissReason.Explain(Row("apogee", 100, 0), Flown(36, -6000, 1));
            Assert.AreEqual("altitude-low", x.Code);
            Assert.AreEqual("Topped out at 36 km of 100 km (it needs 95 km to 105 km).", x.Reason);
            Assert.AreEqual("Add fuel to stage 1, or a bigger engine.", x.Advice);
        }

        [Test]
        public void CircleTooLowSaysTheSpeedShortfall()
        {
            var x = MissReason.Explain(Row("circular", 400, 400), Flown(250, 120));
            Assert.AreEqual("orbit-low", x.Code);
            StringAssert.StartsWith("Missed the 400 km circle by about ", x.Reason);
            StringAssert.Contains("the orbit flew 120 km × 250 km", x.Reason);
            Assert.AreEqual("Add fuel to stage 2, or another stage.", x.Advice);
        }

        [Test]
        public void SuborbitalCircleSaysWhereItToppedOut()
        {
            var x = MissReason.Explain(Row("circular", 200, 200), Flown(36, -6000));
            Assert.AreEqual("orbit-low", x.Code);
            StringAssert.Contains("it topped out at 36 km of 200 km", x.Reason);
            StringAssert.Contains("km/s", x.Reason);
        }

        [Test]
        public void LowPointShortSaysTheSpeedAtTheHighPoint()
        {
            var x = MissReason.Explain(Row("circular", 400, 400), Flown(405, 300));
            Assert.AreEqual("peri-low", x.Code);
            StringAssert.Contains("the low point needs 380 km to 420 km", x.Reason);
            StringAssert.Contains("m/s short at the high point", x.Reason);
        }

        [Test]
        public void NothingToExplainOnAPassAFailureOrNoMission()
        {
            Assert.IsNull(MissReason.Explain(Row("circular", 400, 400), Flown(402, 398)));
            Assert.IsNull(MissReason.Explain(Row("circular", 400, 400), new MissInput { Intact = false, ApoM = 1, PeriM = 1 }));
            Assert.IsNull(MissReason.Explain(Row("circular", 400, 400), new MissInput { FailureKind = "overg", ApoM = 1, PeriM = 1 }));
            Assert.IsNull(MissReason.Explain(null, Flown(1, 1)));
        }

        /// <summary>Same verdict as the score, for every board mission: a reason exactly when GoalMet says miss on an intact flight.</summary>
        [Test]
        public void ReasonExactlyWhenTheScoreSaysMiss()
        {
            foreach (var m in campaign.Missions)
            {
                var d = Starter(m);
                var x = Fly(m, d, out var flight);
                bool explainable = flight.failure == null && !flight.passed;
                Assert.AreEqual(explainable, x != null, m.Id);
            }
        }

        /// <summary>The report card text: Numbers carries the reason, Engineer the advice, and no pool line contradicts them.</summary>
        [Test]
        public void DebriefShowsTheReasonAndTheAdviceOnAMiss()
        {
            var m = Mission("m4");
            var d = Starter(m);
            var x = Fly(m, d, out var flight);
            var facts = new FlightFacts { Passed = false, ApoKm = flight.apoKm, GoalKm = m.ApoKm, Miss = x, Seed = 1 };
            var ctx = MissionContext.FromCampaign(campaign, "m4");
            var pool = NarrationPool.Load(Golden.Root);
            var card = FlightDebrief.Compose(null, ctx, facts, pool, 100000, null);
            Assert.AreEqual("miss", card.Kind);
            Assert.AreEqual(x.Reason, card.Numbers);
            Assert.AreEqual(x.Advice, card.Engineer);
            Assert.AreEqual("apo-high", card.MissCode);
            Assert.AreEqual(x.Advice, card.MissAdvice);
            Assert.AreEqual("", card.NarratorKid);
            Assert.AreEqual("", card.NarratorEng);
        }

        [Test]
        public void DebriefWithoutAMissReasonKeepsTheOldLines()
        {
            var facts = new FlightFacts { Passed = false, ApoKm = 22, GoalKm = 10, Seed = 1 };
            var ctx = MissionContext.FromCampaign(campaign, "m1");
            var card = FlightDebrief.Compose(null, ctx, facts, NarrationPool.Load(Golden.Root), 100000, null);
            Assert.AreEqual("Reached 22 km. The goal was 10 km.", card.Numbers);
            Assert.AreEqual("", card.MissCode);
            StringAssert.Contains("22", card.NarratorKid);
        }

        [Test]
        public void AFailureEventKeepsItsOwnLinesEvenWithAMissAttached()
        {
            var ev = new FailureEvent { kind = "overg", value = 7.2, limit = 6, kid = "Too hard a push.", engineer = "Lower the g limit." };
            var facts = new FlightFacts { Failure = ev, Miss = new MissExplanation { Code = "g", Reason = "r", Advice = "a" }, Seed = 1 };
            var card = FlightDebrief.Compose(null, MissionContext.FromCampaign(campaign, "m1"), facts, NarrationPool.Empty, 100000, null);
            Assert.AreEqual("overg", card.Kind);
            Assert.AreEqual("", card.MissCode);
            Assert.AreEqual("Lower the g limit.", card.Engineer);
        }
    }
}
