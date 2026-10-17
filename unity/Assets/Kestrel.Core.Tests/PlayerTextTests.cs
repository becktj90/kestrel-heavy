// Player-visible text never carries a raw id or a dotted or snake_case key.
using System.Collections.Generic;
using Kestrel.Core;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PlayerTextTests
    {
        [Test]
        public void RawIdsAndKeysAreLeaks_PlainNumbersAreNot()
        {
            Assert.IsTrue(PlayerText.Leak("tl.asc.maxq.short T+0:42"));
            Assert.IsTrue(PlayerText.Leak("tl.throttle.short"));
            Assert.IsTrue(PlayerText.Leak("tl.ign.release.short"));
            Assert.IsTrue(PlayerText.Leak("tl.term.liftoff.short"));
            Assert.IsTrue(PlayerText.Leak("kh-m12"));
            Assert.IsTrue(PlayerText.Leak("payload.orbit"));
            Assert.IsTrue(PlayerText.Leak("engineParams.pc"));
            Assert.IsTrue(PlayerText.Leak("g_lim"));
            Assert.IsTrue(PlayerText.Leak("q_lim"));
            Assert.IsTrue(PlayerText.Leak("v_leg"));
            Assert.IsTrue(PlayerText.Leak("overg: 8.00 g > limit 8.00 g."));
            Assert.IsTrue(PlayerText.Leak("structural-maxq: q 1 Pa > q_lim 1 Pa"));
            Assert.IsFalse(PlayerText.Leak("8.0 g"));
            Assert.IsFalse(PlayerText.Leak("T+0:42"));
            Assert.IsFalse(PlayerText.Leak("51.6 kPa / 35.0"));
            Assert.IsFalse(PlayerText.Leak("On track"));
            Assert.IsFalse(PlayerText.Leak("Have 8900 m/s. Need 7600 m/s."));
        }

        [Test]
        public void ForPlayerDropsTheRawId()
        {
            Assert.AreEqual("", PlayerText.ForPlayer("tl.asc.maxq.short"));
            Assert.That(PlayerText.ForPlayer("peak 8.0 g against g_lim 8.0 g"), Does.Not.Contain("g_lim"));
            Assert.That(PlayerText.ForPlayer("overg: 8.00 g > limit 8.00 g."), Does.Not.Contain("overg:"));
            Assert.IsFalse(PlayerText.Leak(PlayerText.ForPlayer("kh-m3 payload.orbit engineParams.pc")));
        }

        [Test]
        public void NarrationFailureAndCoachCopyStayClean()
        {
            var pool = NarrationPool.Load(Golden.Root);
            var ev = new FailureEvent { t = 124, kind = "overg", value = 8.04, limit = 8, cause = "overg" };
            var facts = new FlightFacts { ApoKm = 10, GoalKm = 200 };
            var debrief = new FlightDebrief { Stars = 0 };
            foreach (var line in pool.Lines)
            {
                Assert.IsFalse(PlayerText.Leak(line.Kid), line.Id + " kid");
                Assert.IsFalse(PlayerText.Leak(line.Eng), line.Id + " eng");
                string kid = FlightDebrief.Fill(line.Kid, ev, facts, debrief);
                string eng = FlightDebrief.Fill(line.Eng, ev, facts, debrief);
                Assert.IsFalse(PlayerText.Leak(kid), line.Id);
                Assert.IsFalse(PlayerText.Leak(eng), line.Id + " " + eng);
                Assert.That(kid, Does.Not.Contain("{"));
                Assert.That(eng, Does.Not.Contain("{"));
            }

            for (int kind = 1; kind <= 11; kind++)
            for (int cause = 0; cause <= 3; cause++)
            {
                var hit = new FailureHit { on = true, kind = kind, cause = cause, value = 8.04, limit = 8, engine = -1, stage = 0 };
                var explained = FailureModel.Explain(hit, 124, 52000);
                Assert.IsFalse(PlayerText.Leak(explained.kid), explained.kind + " kid");
                Assert.IsFalse(PlayerText.Leak(explained.engineer), explained.kind + " " + explained.engineer);
            }

            var heavy = PlayedBreakup.At(new PlayedBreakup.Sample
            {
                T = 40, AltM = 12000, QPa = 90000, AccelMps2 = 30, QLimPa = 35000, LiftoffTwr = 1.4, Thrusting = true,
            });
            Assert.IsNotNull(heavy);
            Assert.IsFalse(PlayerText.Leak(heavy.engineer), heavy.engineer);
            var snap = PlayedBreakup.At(new PlayedBreakup.Sample
            {
                T = 80, AltM = 40000, QPa = 1000, AccelMps2 = FailureModel.G0 * 22, QLimPa = 35000, LiftoffTwr = 1.4, Thrusting = true,
            });
            Assert.IsNotNull(snap);
            Assert.IsFalse(PlayerText.Leak(snap.kid), snap.kid);
            Assert.IsFalse(PlayerText.Leak(snap.engineer), snap.engineer);

            var over = new FailureEvent { kind = "overg", cause = "overg", t = 124, value = 8.04, limit = 8 };
            string numbers = FlightDebrief.NumberLine(over, null, false);
            Assert.That(numbers, Does.Contain("g"));
            Assert.IsFalse(PlayerText.Leak(numbers), numbers);
            var card = FlightDebrief.Compose(null, MissionContext.Free(), new FlightFacts { Failure = over, Passed = false, PeakG = 8.04, GLimit = 8 }, NarrationPool.Empty, 100000, null);
            Assert.AreEqual("No stars this flight.", card.StarsLine);
            Assert.IsFalse(PlayerText.Leak(card.Numbers));
            Assert.IsFalse(PlayerText.Leak(card.StarsLine));
            Assert.IsFalse(PlayerText.Leak(card.MedalsLine));

            foreach (var step in DesignerCoach.Flow)
                Assert.IsFalse(PlayerText.Leak(DesignerCoach.Goal(step)), step);
            Assert.IsFalse(PlayerText.Leak(DesignerCoach.WhyEngines(1.94, 311, 845000)));
            Assert.IsFalse(PlayerText.Leak(DesignerCoach.WhyMaxQ(51.6, 35)));
            Assert.IsFalse(PlayerText.Leak(DesignerCoach.WhyGLimit(8)));
            Assert.IsFalse(PlayerText.Leak(DesignerCoach.BudgetLine(8900, 9400)));

            foreach (var verdict in new[] { "On the count", "Heating hard", "Speed is there", "Climbing", "Short on speed", "On track", "Vehicle lost" })
                Assert.IsFalse(PlayerText.Leak(verdict), verdict);
        }
    }

    public class DesignerSheetLayoutTests
    {
        static readonly string[] Stress = { "409%", "1.94", "51.6 kPa / 35.0", "$563.40M" };
        const string Delta = "+0 m/s Δv · +0.00 TWR";
        const string Banner = "The air pushed 52 kPa, but your tanks could only take 36 kPa. Try a higher safety factor or a lower max-Q throttle.";

        [Test]
        public void StatsDeltaAndBannerDoNotOverlapOnSeProMaxOrIpad()
        {
            foreach (var d in FlightHudLayout.PortraitDevices())
            {
                var open = DesignerSheetLayout.Place(d, false, Stress, Delta, Banner);
                string where = d.Name + " " + d.W + "x" + d.H;
                Assert.IsFalse(open.ReportOpen, where);
                Assert.IsTrue(open.Title.Live && open.Stats.Live && open.Delta.Live && open.Budget.Live && open.Banner.Live, where);
                Assert.IsFalse(open.Card.Overlaps(open.Title), where + " title");
                AssertNoOverlap(open.Title, open.Stats, where);
                AssertNoOverlap(open.Title, open.Delta, where);
                AssertNoOverlap(open.Title, open.Budget, where);
                AssertNoOverlap(open.Title, open.Banner, where);
                AssertNoOverlap(open.Stats, open.Delta, where);
                AssertNoOverlap(open.Stats, open.Budget, where);
                AssertNoOverlap(open.Stats, open.Banner, where);
                AssertNoOverlap(open.Delta, open.Budget, where);
                AssertNoOverlap(open.Delta, open.Banner, where);
                AssertNoOverlap(open.Budget, open.Banner, where);
                Assert.That(open.Title.Bottom, Is.LessThanOrEqualTo(open.Stats.Y + 0.4f), where);
                Assert.That(open.Stats.Bottom, Is.LessThanOrEqualTo(open.Delta.Y + 0.4f), where);
                Assert.That(open.Delta.Bottom, Is.LessThanOrEqualTo(open.Budget.Y + 0.4f), where);
                Assert.That(open.Budget.Bottom, Is.LessThanOrEqualTo(open.Banner.Y + 0.4f), where);
                Assert.That(open.Banner.Bottom, Is.LessThanOrEqualTo(d.H - d.SafeBottom + 0.6f), where);
                for (int i = 0; i < 4; i++)
                {
                    float inner = open.Cells[i].W - DesignerSheetLayout.CellPadPt * 2f;
                    Assert.That(FlightHudLayout.TextWidth(open.CellText[i], open.CellFont[i]), Is.LessThanOrEqualTo(inner + 0.6f), where + " " + open.CellText[i]);
                    Assert.That(open.CellFont[i], Is.InRange(DesignerSheetLayout.ValueMinPt, DesignerSheetLayout.ValueStartPt), where);
                    if (i > 0) Assert.IsFalse(open.Cells[i - 1].Overlaps(open.Cells[i]), where + " cell " + i);
                }
                int bannerLines = DesignerSheetLayout.LineCount(Banner, open.Banner.W - DesignerSheetLayout.CellPadPt * 2f, DesignerSheetLayout.BannerFontPt);
                Assert.That(open.Banner.H, Is.GreaterThanOrEqualTo(bannerLines * DesignerSheetLayout.LinePt - 0.1f), where);
                Assert.That(open.Banner.H, Is.GreaterThanOrEqualTo(DesignerSheetLayout.BannerMinPt), where);

                var covered = DesignerSheetLayout.Place(d, true, Stress, Delta, Banner);
                Assert.IsTrue(covered.ReportOpen, where);
                Assert.IsFalse(covered.Title.Live, where + " the sheet hides while the report is up");
                Assert.IsFalse(covered.Stats.Live, where);
                Assert.IsTrue(covered.Card.Live, where);
            }
        }

        static void AssertNoOverlap(HudBox a, HudBox b, string where)
        {
            if (a.Live && b.Live) Assert.IsFalse(a.Overlaps(b), where);
        }
    }

    public class DesignerRepairTests
    {
        [Test]
        public void OverGLowersTheGuidanceLimit()
        {
            var d = new JsonObject();
            var g = new JsonObject();
            JsonMut.Set(g, "gLimit", 8d);
            JsonMut.Set(d, "guidance", g);
            var edit = DesignerRepair.Apply(null, d, "overg");
            Assert.IsTrue(edit.changed);
            Assert.AreEqual(7.5, g.Num("gLimit"), 1e-6);
            Assert.AreEqual(Edit.None.changed, DesignerRepair.Apply(null, d, "overg").changed);
        }

        [Test]
        public void MaxQRaisesTheSafetyFactor_TumbleAddsFins()
        {
            var d = new JsonObject();
            JsonMut.Set(d, "safetyFactor", 1.4d);
            var edit = DesignerRepair.Apply(null, d, "structural-maxq");
            Assert.IsTrue(edit.changed);
            Assert.Greater(d.Num("safetyFactor"), 1.4);
            Assert.LessOrEqual(d.Num("safetyFactor"), 3);
            var fins = DesignerRepair.Apply(null, d, "tumble");
            Assert.IsTrue(fins.changed);
            Assert.IsTrue(d.Obj("aero").Bool("fins"));
        }

        [Test]
        public void CoachBudgetNamesNeedAndHave()
        {
            Assert.That(DesignerCoach.BudgetLine(8000, 9000), Does.Contain("1,000 m/s short"));
            Assert.That(DesignerCoach.BudgetLine(9000, 8000), Does.Contain("to spare"));
            Assert.That(DesignerCoach.WhyEngines(1.2, 320, 1000), Does.Contain("TWR is 1.20"));
            Assert.That(DesignerCoach.WhyEngines(1.2, 320, 1000), Does.Contain("Isp"));
            Assert.That(DesignerCoach.WhyMaxQ(51.6, 35), Does.Contain("45 kPa"));
            Assert.That(DesignerCoach.WhyGLimit(8), Does.Contain("8.0 g"));
            Assert.AreEqual("orbits", DesignerCoach.CodexId("orbit"));
            Assert.AreEqual("delta-v", DesignerCoach.CodexId("tanks"));
            Assert.AreEqual("staging", DesignerCoach.CodexId("stages"));
            Assert.IsTrue(DesignerRepair.NeedsDeltaV("dv-margin"));
            Assert.IsTrue(DesignerRepair.NeedsDeltaV("prop-depletion"));
        }
    }
}
