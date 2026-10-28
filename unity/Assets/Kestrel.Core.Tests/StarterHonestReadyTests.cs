// Honest Ready (docs/DESIGNER_TIERS_SPEC.md delivery step 1, audit B-10). The Starter badge never reads Ready or Tight
// when the launch predictor's flight (ReferenceAscent.Flight: the live pad's PlayedChecks and director) breaks up, the
// mission's landing fuel is short, or the cargo does not fit. Copy comes from data/starter-readiness.json.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterHonestReadyTests
    {
        static readonly HashSet<string> RandomKinds = new HashSet<string> { "bad-mixture", "engine-out" };

        static bool ReadyOrTight(string badge) => badge == StarterScreen.ReadyBadge || badge == StarterScreen.TightBadge;

        /// <summary>Independent of StarterVerdict: did the predictor's own flight fail for certain, or the landing come up short?</summary>
        static string Truth(StarterRocket r)
        {
            var dj = r.BuildDesign();
            var d = Design.FromJson(dj);
            var sim = ReferenceAscent.Flight(StarterFixture.Catalog, d).Run();
            var f = sim.failure;
            if (f != null && !RandomKinds.Contains(f.kind) && !(f.kind == "engine-overpressure" && f.cause == "hazard"))
                return f.kind + "/" + f.cause;
            var m = r.Mission;
            if (f == null && m.LandingRequired)
            {
                if (FlightBatch.PredictLandingGrade(StarterFixture.Catalog, d, sim, m.LandingMode, false) == "short") return "landing";
                if (m.LandBoosters && FlightBatch.PredictLandingGrade(StarterFixture.Catalog, d, sim, m.LandingMode, true) == "short") return "booster-landing";
            }
            if (Readout.Quick(StarterFixture.Catalog, dj).fairingFits == false) return "fit";
            return null;
        }

        static IEnumerable<StarterChoice> Settings(StarterRocket r)
        {
            foreach (var e in StarterRocket.EngineOrder)
                foreach (var s in StarterRocket.StageValues)
                    for (int f = 0; f < StarterRocket.FuelNotches; f++)
                        foreach (var b in StarterRocket.BoosterValues)
                            if (r.IsStagesAllowed(s) && r.IsBoostersAllowed(b)) yield return new StarterChoice(e, s, f, b);
        }

        static void Set(StarterRocket r, StarterChoice c) { r.Engine = c.Engine; r.Stages = c.Stages; r.FuelNotch = c.FuelNotch; r.Boosters = c.Boosters; }

        static void AssertHonest(StarterRocket r, string where)
        {
            bool launches = StarterScreen.Readiness(r, out var badge, out var rest);
            Assert.AreEqual(!r.Check().Blocked, launches, where + ": Launch follows the hard block only");
            if (r.Check().Blocked) return;
            string truth = Truth(r);
            var v = r.Verdict();
            Assert.AreEqual(truth == null, v.Survives, where + ": verdict " + v.Cause + " vs predictor " + truth);
            if (truth != null)
            {
                Assert.IsFalse(ReadyOrTight(badge), where + ": " + truth + " but the badge reads " + badge + " " + rest);
                Assert.IsNotEmpty(rest, where);
            }
        }

        /// <summary>Settings the sweep found reading Ready before this change while the predictor broke them up or left
        /// the landing short (one per cause). Unity-sized; the dotnet sweep covers every setting.</summary>
        static readonly (string mission, string key, string cause)[] Known =
        {
            ("m3", "kerosene-2-f4-b0", StarterVerdict.MaxQ),
            ("m3", "kerosene-3-f1-b0", StarterVerdict.Tumble),
            ("m3", "methane-3-f0-b0", StarterVerdict.OverG),
            ("m5", "kerosene-2-f1-b2", StarterVerdict.Short),
            ("m13", "methane-2-f4-b0", StarterVerdict.Landing),
        };

        [Test]
        public void KnownReadyButFailing_Settings_NowNameTheirCause()
        {
            foreach (var (mission, key, cause) in Known)
            {
                var r = StarterLayoutTests.Rocket(mission, key);
                Assert.IsTrue(StarterScreen.Readiness(r, out var badge, out var rest), mission + " " + key + ": still launches");
                Assert.AreEqual(cause, r.Verdict().Cause, mission + " " + key);
                Assert.IsFalse(ReadyOrTight(badge), mission + " " + key + ": " + badge);
                var copy = StarterReadinessCopy.Current;
                if (!(cause == StarterVerdict.Short && r.Check().Margin < 0))
                {
                    Assert.AreEqual(copy.Badge(cause), badge, mission + " " + key);
                    StringAssert.StartsWith(copy.Causes[cause].Kid, rest, mission + " " + key);
                }
                Assert.IsTrue(r.CanLaunch, "bad but buildable rockets still launch (F1)");
                AssertHonest(r, mission + " " + key);
            }
            var maxq = StarterLayoutTests.Rocket("m3", "kerosene-2-f4-b0");
            StarterScreen.Readiness(maxq, out _, out var line);
            Assert.AreEqual("It would shake apart in the thick air. Try fewer stages or less fuel.", line);
            StringAssert.StartsWith("Max-Q", StarterScreen.EngineerReason(maxq));
        }

        [Test]
        public void EveryMissionDefault_IsHonest_AndTheWinnersReadReadyOrTight()
        {
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = StarterFixture.Rocket(m.Id);
                AssertHonest(r, m.Id + " default");
                StarterScreen.Readiness(r, out var badge, out var rest);
                Assert.IsTrue(r.Verdict().Survives, m.Id + ": the campaign winner survives the predictor (" + r.Verdict().Cause + ")");
                Assert.IsTrue(ReadyOrTight(badge), m.Id + ": the winner reads " + badge + " " + rest);
            }
        }

#if KH_DOTNET_TESTS
        /// <summary>Every Starter setting on every mission (engine fuel × stages × fuel notch × boosters): about 720 flights.</summary>
        [Test]
        public void EverySetting_OnEveryMission_IsNeverReadyWhenThePredictorFails()
        {
            int n = 0, failing = 0;
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                var r = StarterFixture.Rocket(m.Id);
                foreach (var c in Settings(r))
                {
                    Set(r, c);
                    Assert.AreEqual(c, r.Choice);
                    AssertHonest(r, m.Id + " " + c.Key);
                    n++;
                    if (!r.Check().Blocked && !r.Verdict().Survives) failing++;
                }
            }
            TestContext.WriteLine(n + " settings, " + failing + " fail in the predictor");
            Assert.Greater(n, 600);
            Assert.Greater(failing, 0);
        }
#endif

        [Test]
        public void EveryCause_HasBadgeKidAndEngineerCopy_ThatFitsTheScreen()
        {
            var copy = StarterReadinessCopy.Load(Golden.Root);
            Assert.IsNotEmpty(copy.Checking);
            foreach (var cause in StarterVerdict.Causes)
            {
                Assert.IsTrue(copy.Causes.ContainsKey(cause), "data/starter-readiness.json has " + cause);
                var c = copy.Causes[cause];
                StringAssert.StartsWith("✗ ", c.Badge, cause);
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.Kid), cause + " kid");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.Engineer), cause + " engineer");
                Assert.IsFalse(c.Kid.Contains("!"), cause);
                foreach (var f in c.Fixes) Assert.IsTrue(copy.Fixes.ContainsKey(f), cause + " fix " + f);
                Assert.IsFalse(ReadyOrTight(c.Badge), cause);

                // The longest line the screen can make for this cause fits a 335x667 phone (StarterLayout's own checks).
                foreach (var line in new[] { copy.Kid(cause, _ => true), copy.Kid(cause, _ => false) })
                {
                    var r = StarterFixture.Rocket("m12");
                    var input = new StarterLayoutInput
                    {
                        Width = 335, Height = 667, BoostersVisible = r.BoostersVisible,
                        Title = StarterScreen.Title(r.Mission), Subtitle = StarterScreen.Subtitle(r.Mission),
                        Hint = StarterScreen.Hint(r, StarterKnob.None), Badge = c.Badge, Readiness = line,
                    };
                    var frame = StarterLayout.Place(input);
                    TestContext.WriteLine(cause + " " + frame.ReadinessLines + " lines @" + frame.ReadinessPt + ": " + line);
                    Assert.That(StarterLayout.Problems(frame, input), Is.Empty, cause + ": " + line);
                }
            }
            foreach (var cause in StarterVerdict.Causes)
            {
                var c = copy.Causes[cause];
                foreach (var line in new[] { copy.Kid(cause, _ => true), copy.Kid(cause, _ => false) })
                {
                    var r = StarterFixture.Rocket("m12");
                    var input = new StarterLayoutInput
                    {
                        Width = 335, Height = 667, BoostersVisible = r.BoostersVisible,
                        Title = StarterScreen.Title(r.Mission), Subtitle = StarterScreen.Subtitle(r.Mission),
                        Hint = StarterScreen.Hint(r, StarterKnob.None), Badge = c.Badge, Readiness = line,
                    };
                    var frame = StarterLayout.Place(input);
                    Assert.LessOrEqual(frame.ReadinessLines, StarterLayout.ReadinessMaxLines, cause + ": " + line);
                }
            }
            foreach (var k in copy.Causes.Keys) Assert.IsTrue(((IList<string>)StarterVerdict.Causes).Contains(k), "unknown cause " + k);
        }

        [Test]
        public void CauseOf_MapsEveryFailureKind_AndIgnoresSeededEvents()
        {
            FailureEvent F(string kind, string cause, string engineer = "") => new FailureEvent { kind = kind, cause = cause, engineer = engineer };
            Assert.AreEqual(StarterVerdict.MaxQ, StarterVerdict.CauseOf(F("structural-maxq", "q")));
            Assert.AreEqual(StarterVerdict.Overload, StarterVerdict.CauseOf(F("structural-maxq", "margin", "Load margin -0.100 at T+3.00 s.")));
            Assert.AreEqual(StarterVerdict.Overload, StarterVerdict.CauseOf(F("structural-maxq", "hot-staging")));
            Assert.AreEqual(StarterVerdict.Short, StarterVerdict.CauseOf(PlayedBreakup.Short(40, -0.1)));
            Assert.AreEqual(StarterVerdict.Short, StarterVerdict.CauseOf(PlayedBreakup.Impact(40)));
            Assert.AreEqual(StarterVerdict.Pad, StarterVerdict.CauseOf(PlayedBreakup.At(new PlayedBreakup.Sample { T = 3, AltM = 0, LiftoffTwr = 0.9, Thrusting = true })));
            Assert.AreEqual(StarterVerdict.MaxQ, StarterVerdict.CauseOf(PlayedBreakup.At(new PlayedBreakup.Sample { T = 50, AltM = 9000, QPa = 100000, QLimPa = 30000, Thrusting = true })));
            Assert.AreEqual(StarterVerdict.OverG, StarterVerdict.CauseOf(F("overg", "overg")));
            Assert.AreEqual(StarterVerdict.Tumble, StarterVerdict.CauseOf(F("tumble", "alpha")));
            Assert.AreEqual(StarterVerdict.BaseHeat, StarterVerdict.CauseOf(F("thermal", "base-heating")));
            Assert.AreEqual(StarterVerdict.Heat, StarterVerdict.CauseOf(F("thermal", "nose")));
            Assert.AreEqual(StarterVerdict.Cryo, StarterVerdict.CauseOf(F("cryo-material", "cryo-material")));
            Assert.AreEqual(StarterVerdict.Short, StarterVerdict.CauseOf(F("prop-depletion", "prop-depletion")));
            Assert.AreEqual(StarterVerdict.Separation, StarterVerdict.CauseOf(F("separation-collision", "separation-collision")));
            Assert.AreEqual(StarterVerdict.Legs, StarterVerdict.CauseOf(F("leg-collapse", "leg-collapse")));
            Assert.AreEqual(StarterVerdict.Overpressure, StarterVerdict.CauseOf(F("engine-overpressure", "pc-cap")));
            Assert.IsNull(StarterVerdict.CauseOf(F("engine-overpressure", "hazard")), "seeded hazard");
            Assert.IsNull(StarterVerdict.CauseOf(F("bad-mixture", "bad-mixture")), "seeded");
            Assert.IsNull(StarterVerdict.CauseOf(F("engine-out", "engine-out")), "seeded");
            Assert.IsNull(StarterVerdict.CauseOf(null));
            Assert.AreEqual(StarterVerdict.Fit, StarterVerdict.Judge(StarterFixture.Catalog, null, null, null, false).Cause, "a cargo that does not fit");
        }

        [Test]
        public void CryoOnComposite_IsNamed_ByTheSameFlightAsThePad()
        {
            // B-06: the flight fails any cryogenic propellant on a cryoOK=false tank at T+0. Starter cannot pick a
            // material, so this builds the KH-5 winner on composite tanks directly.
            var cat = StarterFixture.Catalog;
            Assert.IsFalse(cat.MaterialById["composite"].cryoOK, "materials.json");
            var r = StarterFixture.Rocket("m5");
            var d = r.BuildDesign();
            foreach (JsonObject s in d.Arr("stages")) JsonMut.Set(s, "material", "composite");
            var design = Design.FromJson(d);
            var sim = ReferenceAscent.Flight(cat, design).Run();
            var v = StarterVerdict.Judge(cat, design, r.Mission, sim, true);
            Assert.AreEqual(StarterVerdict.Cryo, v.Cause, sim.failure?.kind);
            StringAssert.StartsWith("The cold fuel would crack its tanks.", StarterReadinessCopy.Current.Kid(v.Cause, r.CanFix));
        }

        [Test]
        public void Judge_NamesTheLandingCause_ForAnIntactFlight_WhoseReserveCannotBuyTheReturn()
        {
            // The Landing branch has an exemplar here whether or not any shipped Starter setting reaches it: an intact
            // predictor flight of the KH-13 winner whose landing reserve is a token 1 t cannot buy the return (the
            // reserve's delta-v is under the barge need, FlightBatch.GradeLanding), so the verdict names Landing and
            // not a flown failure. The copy for that cause is the shipped one.
            var cat = StarterFixture.Catalog;
            var r = StarterFixture.Rocket("m13");
            Assert.IsTrue(r.Mission.LandingRequired, "KH-13 grades a landing");
            var d = r.BuildDesign();
            JsonMut.Set((JsonObject)d.Arr("stages")[0], "landingPropMass", 1000);
            var design = Design.FromJson(d);
            var sim = ReferenceAscent.Flight(cat, design).Run();
            Assert.IsNull(sim.failure, "the flight stays intact: " + sim.failure?.kind + "/" + sim.failure?.cause);
            var v = StarterVerdict.Judge(cat, design, r.Mission, sim, true);
            Assert.AreEqual("short", v.LandingGrade);
            Assert.AreEqual(StarterVerdict.Landing, v.Cause);
            Assert.IsFalse(v.Survives);
            Assert.AreEqual("\u2717 Can't land.", StarterReadinessCopy.Current.Badge(v.Cause));

            // The same flight with its real reserve is not a Landing verdict.
            var real = Design.FromJson(r.BuildDesign());
            var ok = StarterVerdict.Judge(cat, real, r.Mission, ReferenceAscent.Flight(cat, real).Run(), true);
            Assert.AreNotEqual(StarterVerdict.Landing, ok.Cause);
            Assert.IsNull(ok.Cause, "the shipped KH-13 starter reads clean: " + ok.Cause);
        }

        [Test]
        public void TheScreen_ChecksWithoutBlocking_ThenWarmsEveryOneTapNeighbour()
        {
            var r = StarterLayoutTests.Rocket("m3", "kerosene-2-f4-b0");
            Assert.IsFalse(r.TryVerdict(out _), "nothing flown yet");
            Assert.IsTrue(StarterScreen.ReadinessNow(r, out var badge, out var rest));
            Assert.AreEqual("", badge, "no Ready while checking");
            Assert.AreEqual(StarterReadinessCopy.Current.Checking, rest);

            int frames = 0;
            bool done = false;
            while (!done && frames < 100000) { done = r.StepPrediction(ReferenceAscent.FrameBudgetMs); frames++; }
            Assert.IsTrue(done, "the current setting finished");
            Assert.IsTrue(r.TryVerdict(out var v));
            Assert.AreEqual(StarterVerdict.MaxQ, v.Cause);
            StarterScreen.ReadinessNow(r, out badge, out rest);
            Assert.AreEqual(StarterReadinessCopy.Current.Badge(StarterVerdict.MaxQ), badge);

            // Idle frames warm every setting one tap away, so the next tap reads from the cache.
            for (int i = 0; i < 100000; i++) r.StepPrediction(ReferenceAscent.FrameBudgetMs);
            var here = r.Choice;
            foreach (var n in new List<StarterChoice>(r.Neighbours()))
            {
                Set(r, here);
                Set(r, n);
                if (!r.Choice.Equals(n)) continue; // not offered on this mission
                Assert.IsTrue(r.Check().Blocked || r.TryVerdict(out _), n.Key + " was warmed");
            }

            // A tap away mid-flight drops the old job and flies the new setting first.
            var other = StarterLayoutTests.Rocket("m4");
            other.StepPrediction(0);
            other.FuelNotch = 0;
            while (!other.StepPrediction(ReferenceAscent.FrameBudgetMs)) { }
            Assert.IsTrue(other.TryVerdict(out _), "the tapped setting");
        }

        [Test]
        public void Recompute_StaysCheap_CachedTapsAddNothing_AndOneFlightStaysUnderTheReferenceCeiling()
        {
            // A tap rebuilds the rocket and re-reads Validate + Readout (StarterRocket.Check, as before) and looks the
            // verdict up. The flight itself runs off the tap, 2 ms a frame (ReferenceAscent.FrameBudgetMs).
            var r = StarterFixture.Rocket("m5");
            var settings = new List<StarterChoice>(Settings(r));
            var flights = new List<double>();
            foreach (var c in settings.GetRange(0, Math.Min(12, settings.Count)))
            {
                Set(r, c);
                if (r.Check().Blocked) continue;
                var sw = Stopwatch.StartNew();
                r.Verdict();
                flights.Add(sw.Elapsed.TotalMilliseconds);
            }
            var taps = new List<double>();
            for (int k = 0; k < 3; k++)
                foreach (var c in settings.GetRange(0, Math.Min(12, settings.Count)))
                {
                    Set(r, Default(r));
                    var sw = Stopwatch.StartNew();
                    Set(r, c);
                    StarterScreen.ReadinessNow(r, out _, out _);
                    taps.Add(sw.Elapsed.TotalMilliseconds);
                }
            flights.Sort(); taps.Sort();
            double flightP50 = flights[flights.Count / 2], tapP95 = taps[(int)(taps.Count * 0.95)];
            TestContext.WriteLine("predictor flight p50 " + flightP50.ToString("0.0") + " ms, max " + flights[flights.Count - 1].ToString("0.0") + " ms; cached tap p95 " + tapP95.ToString("0.00") + " ms");
            // ReadoutV2Tests.Recompute_MeetsLatencyBudget: 80 ms is the Debug ceiling for one reference ascent.
            Assert.Less(flightP50, 80, "one predictor flight");
            Assert.Less(tapP95, 50, "a cached tap (build + Validate + Readout, no flight)");
        }

        static StarterChoice Default(StarterRocket r) => r.Default;
    }
}
