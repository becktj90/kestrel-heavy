// Test Flight (DECISIONS #100): the classifier and its copy, real headless flights of an absurd and a good rocket,
// the Sandbox route it extends, and the no-write rule (career save, wallet, stars).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TestFlightTests
    {
        [SetUp, TearDown]
        public void ClearSession()
        {
            GameSession.Clear();
            FlightDebrief.ClearLatest();
        }

        static JsonObject Template(string id)
            => MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "web", "js", "data", "templates", id + ".json")));

        static JsonObject Stage(JsonObject d, int i) => (JsonObject)d.Arr("stages")[i];

        // ---------------------------------------------------------------- headless, real sim numbers

        [Test]
        public void GoodRocket_MakesOrbit_WithRealNumbers()
        {
            var facts = TestFlight.FlyHeadless(Golden.Catalog, Template("hydrogen-upper"), 1);
            var card = TestFlight.Card(facts);
            Assert.AreEqual(TestOutcome.Orbit, card.Outcome, card.Title + " / " + card.Engineer);
            Assert.AreEqual(TestFlightCopy.OrbitTitle, card.Title);
            Assert.AreEqual(TestFlightCopy.OrbitKid, card.Kid);
            Assert.AreEqual("", card.Hint, "orbit has no Try: line");
            StringAssert.IsMatch(@"^Orbit [\d,]+ × [\d,]+ km · [\d,]+ m/s left$", card.Engineer);
            Assert.Greater(facts.PeriAltM, TestFlight.OrbitPerigeeFloorM);
            Assert.AreEqual("", card.FixKind);
        }

        [Test]
        public void AbsurdRocket_TooHeavy_NeverLeavesThePad()
        {
            var d = Template("hydrogen-upper");
            JsonMut.Set(d.Obj("payload"), "mass", 2000000.0);
            var facts = TestFlight.FlyHeadless(Golden.Catalog, d, 1);
            var card = TestFlight.Card(facts);
            Assert.IsFalse(facts.LiftedOff);
            Assert.AreEqual(TestOutcome.NoLiftoff, card.Outcome);
            Assert.AreEqual(TestFlightCopy.LiftTitle, card.Title);
            StringAssert.IsMatch(@"^Thrust-to-weight 0\.\d\d at launch · needs over 1\.0$", card.Engineer);
            Assert.Less(facts.LiftoffTwr, 1.0);
            Assert.AreEqual("Try: " + TestFlightCopy.LiftHint, card.HintLine);
        }

        [Test]
        public void TinyFirstStage_BreaksUpAtMaxQ_LimitNeverReadsEqual()
        {
            var d = Template("hydrogen-upper");
            JsonMut.Set(Stage(d, 0), "propMass", 20000.0);
            var card = TestFlight.Card(TestFlight.FlyHeadless(Golden.Catalog, d, 1));
            Assert.AreEqual(TestOutcome.MaxQBreakup, card.Outcome, card.Title + " / " + card.Engineer);
            var m = System.Text.RegularExpressions.Regex.Match(card.Engineer, @"^([\d.,]+) kPa at [\d,]+ km · limit ([\d.,]+) kPa$");
            Assert.IsTrue(m.Success, card.Engineer);
            Assert.AreNotEqual(m.Groups[1].Value, m.Groups[2].Value, "a load printed equal to its limit");
        }

        [Test]
        public void OneEngine_SpinsOut()
        {
            var d = Template("hydrogen-upper");
            JsonMut.Set(Stage(d, 0), "engineCount", 1.0);
            var card = TestFlight.Card(TestFlight.FlyHeadless(Golden.Catalog, d, 1));
            Assert.AreEqual(TestOutcome.LostControl, card.Outcome, card.Title + " / " + card.Engineer);
            StringAssert.IsMatch(@"^[\d,]+° off course at [\d,]+ km$", card.Engineer);
            Assert.AreEqual("tumble", card.FixKind);
        }

        [Test]
        public void SoundingRocket_FallsBackDown_SpeedOfNeeded()
        {
            var card = TestFlight.Card(TestFlight.FlyHeadless(Golden.Catalog, Template("sounding-hybrid"), 1));
            Assert.AreEqual(TestOutcome.FellShort, card.Outcome, card.Title + " / " + card.Engineer);
            var m = System.Text.RegularExpressions.Regex.Match(card.Engineer, @"^Peak [\d,]+ km · ([\d,]+) of ([\d,]+) m/s$");
            Assert.IsTrue(m.Success, card.Engineer);
            Assert.Less(double.Parse(m.Groups[1].Value.Replace(",", "")), double.Parse(m.Groups[2].Value.Replace(",", "")));
        }

        [Test]
        public void HeadlessFlight_IsDeterministic()
        {
            var a = TestFlight.Card(TestFlight.FlyHeadless(Golden.Catalog, Template("hydrogen-upper"), 7));
            var b = TestFlight.Card(TestFlight.FlyHeadless(Golden.Catalog, Template("hydrogen-upper"), 7));
            Assert.AreEqual(a.Spoken, b.Spoken);
        }

        // ---------------------------------------------------------------- classifier, synthesized facts

        [Test]
        public void OutOfFuel_InFlight_ShowsShortfall()
        {
            var facts = new TestFlightFacts
            {
                LiftedOff = true, Outcome = "failure",
                Failure = new FailureEvent { kind = "prop-depletion", cause = "prop-depletion", t = 140 },
                FailAltM = 82000, FailSpeedMps = 3100, ApoAltM = 160000, PeriAltM = -6000000,
            };
            var card = TestFlight.Card(facts);
            Assert.AreEqual(TestOutcome.OutOfFuel, card.Outcome);
            Assert.AreEqual(TestFlightCopy.FuelTitle, card.Title);
            StringAssert.IsMatch(@"^Empty at 82 km, 3,100 m/s · [\d,]+ m/s short$", card.Engineer);
            Assert.AreEqual("prop-depletion", card.FixKind);
        }

        [Test]
        public void UnknownFailure_KeepsTheSimsOwnWords_NoGuessedHint()
        {
            var ev = new FailureEvent { kind = "thermal", cause = "base-heating", t = 4, kid = "The engines cooked the rocket's base." };
            var card = TestFlight.Card(new TestFlightFacts { LiftedOff = true, Outcome = "failure", Failure = ev });
            Assert.AreEqual(TestOutcome.Failure, card.Outcome);
            Assert.AreEqual(FlightDebrief.TitleOf("thermal"), card.Title);
            Assert.AreEqual(ev.kid, card.Kid);
            Assert.AreEqual("", card.Hint);
        }

        [Test]
        public void Spoken_IsTitleKidEngineerHint_InOrder()
        {
            var card = new TestFlightCard { Title = "Spun out", Kid = "It tipped over.", Engineer = "129° off course at 3 km", Hint = "More fins." };
            Assert.AreEqual("Spun out. It tipped over. 129° off course at 3 km. Try: More fins.", card.Spoken);
        }

        [Test]
        public void CopyMatchesTheUxSpec()
        {
            Assert.AreEqual("Test Flight", TestFlightCopy.HomeLabel);
            Assert.AreEqual("Build anything. See if it flies.", TestFlightCopy.HomeSub);
            Assert.AreEqual("TEST FLIGHT", TestFlightCopy.Eyebrow);
            Assert.AreEqual("No Kc spent, no stars. Just science.", TestFlightCopy.Rules);
            Assert.AreEqual("Tweak & Retry", TestFlightCopy.Retry);
            Assert.AreEqual("New Design", TestFlightCopy.NewDesign);
            foreach (var title in new[] { TestFlightCopy.OrbitTitle, TestFlightCopy.FuelTitle, TestFlightCopy.LiftTitle, TestFlightCopy.MaxQTitle, TestFlightCopy.SpinTitle, TestFlightCopy.ShortTitle })
                Assert.LessOrEqual(title.Length, 20, title);
        }

        // ---------------------------------------------------------------- routes: the Sandbox path, extended

        [Test]
        public void Sandbox_IsTestFlight_StillAFreeBuild()
        {
            var r = GameSession.Apply(RouteId.HomeHangar);
            Assert.AreEqual(SessionScreens.Design, r.Screen);
            Assert.IsTrue(GameSession.IsFreeBuild, "the Sandbox route keeps MissionMode.Free");
            Assert.IsTrue(GameSession.IsTestFlight);
            Assert.IsNull(GameSession.Current.TestReturn);
            Assert.AreEqual(SessionScreens.Home, GameSession.Apply(RouteId.DesignerBack).Screen);
            Assert.IsNull(GameSession.Current);
        }

        [Test]
        public void MissionTestIt_RetryAndNewDesign_HandTheMissionBack()
        {
            var campaign = Campaign.Load(Golden.Root);
            var mission = MissionContext.FromCampaign(campaign, "m1");
            foreach (var route in new[] { RouteId.TestRetry, RouteId.TestNewDesign, RouteId.DesignerBack })
            {
                GameSession.Carry(MissionContext.TestOf(mission));
                Assert.IsTrue(GameSession.IsTestFlight);
                Assert.IsFalse(GameSession.IsFreeBuild, "a mission's test keeps the mission's target");
                Assert.AreEqual(mission.MissionId, GameSession.Current.MissionId);
                GameSession.Apply(route, campaign);
                Assert.AreSame(mission, GameSession.Current, route.ToString());
                Assert.IsFalse(mission.TestFlight, "TestOf never flags the real mission");
            }
        }

        [Test]
        public void FreeTest_RetryStaysATest()
        {
            GameSession.Apply(RouteId.HomeHangar);
            var test = GameSession.Current;
            Assert.AreEqual(SessionScreens.Design, GameSession.Apply(RouteId.TestRetry).Screen);
            Assert.AreSame(test, GameSession.Current);
            Assert.AreEqual(SessionScreens.Design, GameSession.Apply(RouteId.TestNewDesign).Screen);
            Assert.IsTrue(GameSession.IsTestFlight);
        }

        // ---------------------------------------------------------------- nothing is written

        [Test]
        public void TestOfMission_Debrief_WritesNothing_NoStarsNoKc()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-testflight-" + Guid.NewGuid().ToString("N"));
            try
            {
                var campaign = Campaign.Load(Golden.Root);
                var native = new NativeStore(dir, () => 1000, () => 0.25);
                var career = CareerStore.Load(native, campaign, 100000);
                career.Save();
                var before = Snapshot(dir);
                var mission = MissionContext.FromCampaign(campaign, "m1");
                var test = MissionContext.TestOf(mission);
                var facts = new FlightFacts
                {
                    Passed = true, EconomyKnown = true, CostMinor = 500000, ApoKm = 120, GoalKm = 100, Seed = test.Seed,
                    Test = TestFlight.FlyHeadless(Golden.Catalog, Template("hydrogen-upper"), 1),
                };
                var card = FlightDebrief.Commit(career, test, facts, NarrationPool.Load(Golden.Root), 100000, HangarData.LoadFromRepo(Golden.Root));
                Assert.IsNotNull(card.TestCard);
                Assert.AreEqual(TestOutcome.Orbit, card.TestCard.Outcome);
                Assert.AreEqual(0, card.Stars);
                Assert.AreEqual(TestFlightCopy.Rules, card.KcLine);
                CollectionAssert.AreEquivalent(before, Snapshot(dir), "a test flight wrote to the save folder");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void TestDesigns_SaveToTheirOwnRecord_NotTheDraft()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-testflight-" + Guid.NewGuid().ToString("N"));
            try
            {
                var native = new NativeStore(dir, () => 1000, () => 0.25);
                var library = new DesignLibrary(native);
                var doc = Template("hydrogen-upper");
                library.SaveTestFlight(doc, "Heron test");
                Assert.IsNotNull(library.LoadTestFlight());
                Assert.IsNull(library.LoadDraft(out _, out _), "the campaign draft is untouched");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        static List<string> Snapshot(string dir)
        {
            if (!Directory.Exists(dir)) return new List<string>();
            return Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => Path.GetFileName(p) + ":" + File.ReadAllText(p))
                .ToList();
        }
    }
}
