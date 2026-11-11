// Playthrough 2026-11-11 (Kestrel Arcade, main 45b5b15a), the minor fixes:
// - "Air pushed 25 kPa. The walls held 25 kPa." from 25.06 vs 25.00 kPa: close values print the first precision that differs.
// - "Next mission" on a miss with the next mission locked lands on the Missions list, so it says "Missions".
// - KH-3 / KH-14's margin bonus is margin >= 0, so it says "Don't come up short on speed", not "with speed to spare".
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PlaythroughFixTests
    {
        [TearDown]
        public void Clear() => GameSession.Clear();

        static FailureEvent Breakup(double qPa, double limitPa, out FlightDebrief debrief)
        {
            var hit = new FailureHit { on = true, kind = FailureKind.StructuralMaxQ, cause = 0, value = qPa, limit = limitPa, engine = -1, stage = 1 };
            var ev = FailureModel.Explain(hit, 4.84, qPa);
            debrief = FlightDebrief.Compose(null, MissionContext.Free(), new FlightFacts { Failure = ev, Passed = false, Seed = 1 }, NarrationPool.Empty, 100000, null);
            return ev;
        }

        [TestCase(25.0566, 25.0, "25.1", "25.0")]
        [TestCase(25.004, 25.0, "25.004", "25.000")]
        [TestCase(25.04, 25.0, "25.04", "25.00")]
        [TestCase(41.0, 33.0, "41", "33")]
        [TestCase(25.6, 25.4, "26", "25")]
        [TestCase(25.0, 25.0, "25", "25")]
        public void PairText_ShowsTheFirstPrecisionWhereTheValuesDiffer(double a, double b, string aText, string bText)
        {
            FlightDebrief.PairText(a, b, out var x, out var y);
            Assert.AreEqual(aText, x);
            Assert.AreEqual(bText, y);
        }

        [Test]
        public void PairText_ClosePairsNeverReadEqual_WhenTheyDifferByAPascalOrMore()
        {
            for (double limit = 10000; limit <= 90000; limit += 777)
                foreach (double over in new[] { 1.0, 4.0, 49.0, 51.0, 449.0, 499.0, 501.0 })
                {
                    FlightDebrief.KpaPair(limit + over, limit, out var q, out var lim);
                    Assert.AreNotEqual(q, lim, (limit + over) + " Pa vs " + limit + " Pa");
                }
        }

        [Test]
        public void BreakupCard_25056Pa_Against25000Pa_DiffersVisibly()
        {
            var ev = Breakup(25056.6, 25000, out var debrief);
            Assert.AreEqual("Air pushed 25.1 kPa. The walls held 25.0 kPa.", debrief.Numbers);
            Assert.AreEqual("Air pushed 25.1 kPa. The walls held 25.0 kPa.", FlightDebrief.NumberLine(ev, null, false));
            StringAssert.Contains("The air pushed 25.1 kPa, but your tanks could only take 25.0 kPa.", ev.kid);
            Assert.AreEqual("The stack folded at T+5 s. 25.1 kPa of air, and the walls stop at 25.0 kPa.",
                FlightDebrief.Fill("The stack folded at T+{t} s. {q} kPa of air, and the walls stop at {qlim} kPa.", ev, null, debrief));
            Assert.AreEqual("Air load 25056.6 Pa, limit 25000.0 Pa.",
                FlightDebrief.Fill("Air load {qPa} Pa, limit {qlimPa} Pa.", ev, null, debrief), "the Pa tokens are untouched");
        }

        [Test]
        public void BreakupCard_FarApart_KeepsWholeNumbers()
        {
            var ev = Breakup(41000, 33000, out var debrief);
            Assert.AreEqual("Air pushed 41 kPa. The walls held 33 kPa.", debrief.Numbers);
            StringAssert.Contains("The air pushed 41 kPa, but your tanks could only take 33 kPa.", ev.kid);
        }

        static Campaign Load() => Campaign.Load(Golden.Root);

        [Test]
        public void NextLabel_MissWithTheNextMissionLocked_SaysMissions()
        {
            var c = Load();
            var kh1 = MissionContext.FromCampaign(c, "m1");
            Assert.IsTrue(ReportRoutes.NextLandsOnList(c, kh1, 0, passed: false), "KH-1 miss, nothing cleared: KH-2 is locked");
            Assert.AreEqual(ReportRoutes.NextLockedLabel, ReportRoutes.NextLabelFor(c, kh1, 0, false));
            Assert.AreEqual("Missions", ReportRoutes.NextLockedLabel);
            // The route agrees: SceneFlow.ReportNext opens the next designer only when index <= cleared + 1.
            Assert.Greater(c.Mission(2).Index, 0 + 1);
        }

        [Test]
        public void NextLabel_PassOrAnOpenNextMission_StaysNextMission()
        {
            var c = Load();
            var kh1 = MissionContext.FromCampaign(c, "m1");
            Assert.AreEqual(ReportRoutes.NextLabel, ReportRoutes.NextLabelFor(c, kh1, 0, passed: true), "a pass unlocks KH-2");
            Assert.AreEqual(ReportRoutes.NextLabel, ReportRoutes.NextLabelFor(c, kh1, 1, passed: false), "KH-2 already open after a replayed miss");
            Assert.AreEqual(ReportRoutes.NextLabel, ReportRoutes.NextLabelFor(c, MissionContext.FromCampaign(c, "m5"), 7, false));
        }

        [Test]
        public void NextLabel_NoNextMission_OrNotCampaign_SaysMissions_NoSession_StaysNextMission()
        {
            var c = Load();
            var last = c.Missions[c.Missions.Count - 1];
            Assert.IsTrue(ReportRoutes.NextLandsOnList(c, MissionContext.FromCampaign(c, last.Id), 99, true), last.Id + " has no next mission");
            Assert.IsTrue(ReportRoutes.NextLandsOnList(c, MissionContext.Free(), 0, true), "a free build's Next is the list");
            Assert.IsFalse(ReportRoutes.NextLandsOnList(c, null, 0, false), "no session: unknown, label unchanged");
            Assert.IsFalse(ReportRoutes.NextLandsOnList(null, MissionContext.FromCampaign(c, "m1"), 0, false), "no data: unknown");
        }

        [Test]
        public void NextLabel_MatchesWhereTheRouteGoes_ForEveryMissionAndClearCount()
        {
            var c = Load();
            foreach (var m in c.Missions)
                for (int cleared = 0; cleared <= c.Missions.Count; cleared++)
                    foreach (bool passed in new[] { false, true })
                    {
                        GameSession.Carry(MissionContext.FromCampaign(c, m.Id));
                        bool toList = ReportRoutes.NextLandsOnList(c, GameSession.Current, cleared, passed);
                        var r = GameSession.Apply(RouteId.ReportNext, c);
                        // GameSession routes to the next designer; SceneFlow.ReportNext then falls back to the list when locked.
                        int effCleared = passed ? System.Math.Max(cleared, m.Index) : cleared;
                        bool opens = r.Screen == SessionScreens.Design && r.Session != null && c.ById(r.Session.MissionId).Index <= effCleared + 1;
                        Assert.AreEqual(!opens, toList, m.Id + " cleared " + cleared + " passed " + passed);
                    }
        }

        [Test]
        public void MarginBonus_SaysWhatItChecks()
        {
            var raw = MiniJson.ParseObject(System.IO.File.ReadAllText(System.IO.Path.Combine(Golden.Root, "data", "campaign.json")));
            int seen = 0;
            foreach (var o in raw.Arr("missions"))
            {
                var m = (JsonObject)o;
                var b = m.Obj("briefing");
                var bonuses = b?.Arr("bonuses");
                if (bonuses == null) continue;
                foreach (var x in bonuses)
                {
                    var bonus = (JsonObject)x;
                    if (bonus.Str("id") != "margin") continue;
                    double min = double.NaN;
                    foreach (var r in m.Arr("requirements"))
                        if (((JsonObject)r).Str("id") == "margin") min = ((JsonObject)r).Num("min");
                    Assert.AreEqual(0.0, min, m.Str("id") + " margin requirement");
                    Assert.AreEqual("Don't come up short on speed", bonus.Str("label"), m.Str("id") + ": margin >= 0 is not 'speed to spare'");
                    seen++;
                }
                StringAssert.DoesNotContain("speed to spare", b?.Str("control") ?? "", m.Str("id") + " control line");
            }
            Assert.GreaterOrEqual(seen, 2, "KH-3 and KH-14 carry the margin bonus");
        }

        // ---- full play-through (scope 2): every value/limit pair, at the pair's own precision ----

        [TestCase(8.04, 8.0, 1, "8.04", "8.00")]     // "8.00 g past 8.0 g" (KH-3): one precision for both
        [TestCase(8.004, 8.0, 1, "8.004", "8.000")]
        [TestCase(8.6, 8.0, 1, "8.6", "8.0")]
        [TestCase(75.3, 75.0, 0, "75.3", "75.0")]     // "75 kPa vs 75 kPa"
        [TestCase(1.0, 6.0, 0, "1", "6")]
        [TestCase(0.5, 0.5, 2, "0.50", "0.50")]
        public void PairText_FromAnyPrecision_PrintsBothAlike_AndAddsPlacesOnlyWhileEqual(double a, double b, int decimals, string aText, string bText)
        {
            FlightDebrief.PairText(a, b, decimals, out var x, out var y);
            Assert.AreEqual(aText, x);
            Assert.AreEqual(bText, y);
            int Places(string t) => t.Contains(".") ? t.Length - t.IndexOf('.') - 1 : 0;
            Assert.AreEqual(Places(x), Places(y), "one precision for the pair");
        }

        [Test]
        public void OverG_8004_Against8_ReadsDifferent_OnTheCard_KidLine_AndNarration()
        {
            var hit = new FailureHit { on = true, kind = FailureKind.OverG, cause = 0, value = 8.004, limit = 8.0, engine = -1, stage = 0 };
            var ev = FailureModel.Explain(hit, 116, 0);
            Assert.AreEqual("The push reached 8.004 g, past the 8.000 g limit.", FlightDebrief.NumberLine(ev, null, false));
            StringAssert.Contains("felt 8.004 g", ev.kid);
            StringAssert.Contains("rated 8.000 g", ev.kid);
            var near = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.OverG, value = 8.04, limit = 8.0, engine = -1 }, 116, 0);
            Assert.AreEqual("The push reached 8.04 g, past the 8.00 g limit.", FlightDebrief.NumberLine(near, null, false));
            Assert.AreEqual("8.04 g against 8.00 g", FlightDebrief.Fill("{g} g against {glim} g", near, null, null));
            Assert.AreEqual("8.04 vs 8.00", FlightDebrief.Fill("{value} vs {limit}", near, null, null));
        }

        [Test]
        public void EveryPairedNumberLine_NeverReadsEqual_WhenTheValuesDiffer()
        {
            var kinds = new[] { "engine-overpressure", "bad-mixture", "separation-collision", "overg", "thermal", "leg-collapse", "recovery-fuel", "recovery-fast", "recovery-tilt", "recovery-miss" };
            foreach (var kind in kinds)
            foreach (var (v, l) in new[] { (75.3, 75.0), (8.004, 8.0), (1.04, 1.0), (300.2, 300.0) })
            {
                var ev = new FailureEvent { kind = kind, cause = kind == "thermal" ? "skin" : kind, value = v, limit = l, t = 10 };
                string line = FlightDebrief.NumberLine(ev, null, false);
                var nums = System.Text.RegularExpressions.Regex.Matches(line, @"\d+(\.\d+)?");
                var shown = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in nums) shown.Add(m.Value);
                Assert.GreaterOrEqual(shown.Count, 2, line);
                Assert.AreNotEqual(shown[shown.Count - 2], shown[shown.Count - 1], kind + ": " + line);
            }
            var tumble = new FailureEvent { kind = "tumble", cause = "alpha", value = 12.3, limit = 12, t = 10 };
            Assert.AreEqual("12.3 degrees sideways. The limit is 12.0 degrees.", FlightDebrief.NumberLine(tumble, null, false));
        }

        [Test]
        public void EngineOut_EngineerLine_SaysHowManyQuit_AndTheRideThroughRule()
        {
            var hit = new FailureHit { on = true, kind = FailureKind.EngineOut, cause = 0, value = 1, limit = 6, engine = 2, stage = 0 };
            var ev = FailureModel.Explain(hit, 0, 0);
            Assert.AreEqual("1 of 6 engines quit at T+0.0 s. A stage rides through one out only with 7 or more engines, redundant avionics, and a thrust-to-weight above 1.05 on the rest.", ev.engineer);
            Assert.That(ev.engineer, Does.Not.Contain("Reading"));
            Assert.That(ev.engineer, Does.Not.Contain("limit"));
            Assert.AreEqual("1 engine quit out of 6.", FlightDebrief.NumberLine(ev, null, false));
            var pool = NarrationPool.Load(Golden.Root);
            foreach (var line in pool.Lines)
            {
                if (line.Kind != "engine-out") continue;
                string eng = FlightDebrief.Fill(line.Eng, ev, null, null), kid = FlightDebrief.Fill(line.Kid, ev, null, null);
                Assert.That(eng, Does.Not.Contain("value").And.Not.Contain("limit").And.Not.Contain("1.00"), line.Id + ": " + eng);
                Assert.That(eng, Does.Contain("1 of 6"), line.Id + ": " + eng);
                Assert.That(kid, Does.Not.Contain("{"), line.Id);
            }
        }

        [Test]
        public void LostBoosterTip_OnTheStarterScreen_NamesNoRecoveryTab()
        {
            foreach (var (fuel, legs) in new[] { (0.0, false), (0.0, true), (5000.0, false), (5000.0, true) })
            {
                var craft = new RecoveryCraft { DryKg = 20000, ReserveKg = fuel, Legs = legs, Mode = "none" };
                BoosterLanding.Why(craft, true, out var starter, out _);
                Assert.That(starter, Does.Not.Contain("Recovery"), starter);
                StringAssert.EndsWith(BoosterLanding.StarterLandingFix, starter);
                BoosterLanding.Why(craft, false, out var full, out _);
                StringAssert.Contains("in Recovery", full, "the full designer keeps its Recovery section");
            }
            foreach (var label in new[] { Kestrel.Core.Designer.StarterScreen.EngineLabel, Kestrel.Core.Designer.StarterScreen.StagesLabel, Kestrel.Core.Designer.StarterScreen.FuelLabel, Kestrel.Core.Designer.StarterScreen.BoostersLabel })
                Assert.AreNotEqual("Recovery", label, "the Starter screen has no Recovery control");
            var saved = Kestrel.Core.Features.FeatureFlags.Active;
            try
            {
                Kestrel.Core.Features.FeatureFlags.Active = new Kestrel.Core.Features.FeatureFlags();
                Assert.IsTrue(Kestrel.Core.Designer.StarterScreen.UseStarter(Kestrel.Core.Features.FeatureFlags.Active), "default flags are the Starter screen");
                BoosterLanding.Why(new RecoveryCraft { DryKg = 20000 }, out var shipped, out _);
                Assert.That(shipped, Does.Not.Contain("Recovery"), "the shipping default is the Starter screen");
            }
            finally { Kestrel.Core.Features.FeatureFlags.Active = saved; }
        }

        [Test]
        public void ContinueCaption_NamesWhatItOpens()
        {
            // The play-through: the newest save, no draft, no last mission -> it opened "QA Saved Orbiter" as a free build.
            Assert.AreEqual("QA Saved Orbiter \u00B7 free build", Kestrel.Core.Save.ResumeOffer.Caption(false, null, "QA Saved Orbiter"));
            Assert.AreEqual("QA Saved Orbiter \u00B7 KH-3", Kestrel.Core.Save.ResumeOffer.Caption(false, "KH-3 \u00B7 Tiny Orbit", "QA Saved Orbiter"));
            Assert.AreEqual("KH-3 \u00B7 Tiny Orbit", Kestrel.Core.Save.ResumeOffer.Caption(true, "KH-3 \u00B7 Tiny Orbit", "KH-3 draft"), "a mission's draft resumes that mission");
            Assert.AreEqual("Your last rocket \u00B7 free build", Kestrel.Core.Save.ResumeOffer.Caption(true, null, " "));
            Assert.AreEqual("Continue. Opens QA Saved Orbiter, free build.", Kestrel.Core.Save.ResumeOffer.Spoken("QA Saved Orbiter \u00B7 free build"));
            Assert.AreEqual(Kestrel.Core.Save.ResumeOffer.ContinueSpoken, Kestrel.Core.Save.ResumeOffer.Spoken(null));
        }
    }
}
