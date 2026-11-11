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
    }
}
