// G9: the subtitle box reads the real guide script, and every number is a token.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Guide;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class GuideHookTests
    {
        string root;
        string json;

        [SetUp]
        public void SetUp()
        {
            root = Repo();
            json = File.ReadAllText(Path.Combine(root, "data", "guide.json"));
            GuideSession.Bind(null);
        }

        [Test]
        public void HomeOffersTheWelcomeUntilItIsSeen()
        {
            var engine = Engine(false);
            GuideHooks.Home(engine);
            Assert.AreEqual("first.1", engine.Pending[0].Id);
            Assert.AreEqual("first.1.c1", engine.Pending[0].Cards[0].Id);
            Assert.AreEqual("first.2.c1", engine.Pending[1].Cards[0].Id);
            Assert.AreEqual("first.2.c2", engine.Pending[1].Cards[1].Id);
            Assert.AreEqual(5, engine.Pending.Count);
            engine.Complete("first.1");
            engine.Complete("first.2");
            engine.Complete("first.3");
            engine.Complete("first.4");
            engine.Complete("first.5");
            Assert.IsTrue(engine.State.FirstRunDone);
            GuideHooks.Home(engine);
            Assert.AreEqual(0, engine.Pending.Count);
        }

        [Test]
        public void WelcomeCardsDoNotReturnAfterTheyAreSaved()
        {
            var engine = Engine(false);
            GuideHooks.Home(engine);
            Assert.IsTrue(Has(engine, "first.1"));
            engine.Complete("first.1");
            string dir = Path.Combine(Path.GetTempPath(), "thrum-seen-" + Guid.NewGuid().ToString("N"));
            try
            {
                engine.State.Save(dir);
                var loaded = new LiveGuideEngine(json, GuideState.Load(dir, false, false), root);
                GuideHooks.Home(loaded);
                Assert.IsFalse(Has(loaded, "first.1"));
                Assert.IsTrue(Has(loaded, "first.2"));
                loaded.Complete("first.2");
                loaded.Complete("first.3");
                loaded.Complete("first.4");
                loaded.Complete("first.5");
                Assert.IsTrue(loaded.State.FirstRunDone);
                loaded.State.Save(dir);
                var done = new LiveGuideEngine(json, GuideState.Load(dir, false, false), root);
                GuideHooks.Home(done);
                Assert.AreEqual(0, done.Pending.Count);
                Assert.IsTrue(done.State.FirstRunDone);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void MissionSelectAndBriefingUseTheScriptedMission()
        {
            var engine = Engine(true);
            GuideHooks.MissionSelect(engine, "m1", false);
            Assert.AreEqual("brief.kh1", Only(engine).Id);
            engine.Complete("brief.kh1");

            engine = Engine(true);
            GuideHooks.MissionSelect(engine, "m2", true);
            Assert.AreEqual("brief.contract", Only(engine).Id);

            var card = new MissionBriefing { WindKt = 14 };
            card.Wind = new SeededWind(new[] { 0d }, new[] { 3d }, new[] { 0d }, 7, 270, 0, "W");
            var ctx = new MissionContext { MissionId = "m3", Mode = MissionMode.Campaign };
            ctx.BonusGoals.Add("soft ride");
            engine = Engine(true);
            GuideHooks.Briefing(engine, ctx, card);
            Assert.IsTrue(Has(engine, "brief.kh3"));
            Assert.IsTrue(Has(engine, "brief.bonus"));
            var wind = Find(engine, "brief.wind");
            var pages = GuidePages.Of(wind, false);
            StringAssert.Contains("14", pages[0]);
            StringAssert.Contains("W", pages[0]);
            StringAssert.DoesNotContain("{", pages[0]);
        }

        [Test]
        public void DesignerStepsCarryRealNumbersAndFollowDepth()
        {
            var engine = Engine(true);
            var pass = ReportWith(2, "pass", StepGate.Pass, 1.42, 1.3);
            pass.Readout = new QuickReadout { liftoffTWR = 1.42 };
            GuideHooks.Designer(engine, 2, pass, false);
            var beat = Find(engine, "step.2.pass");
            var kid = GuidePages.Of(beat, false);
            var both = GuidePages.Of(beat, true);
            Assert.AreEqual(1, kid.Length);
            StringAssert.Contains("1.42", kid[0]);
            StringAssert.DoesNotContain("1.30", kid[0]);
            StringAssert.Contains("1.30", both[1]);
            StringAssert.Contains("1.42", both[1]);
            Assert.IsFalse(engine.Settings.Engineer);

            engine = Engine(true);
            var low = ReportWith(2, "twr-low", StepGate.Red, 0.8, 1);
            GuideHooks.Designer(engine, 2, low, true);
            Assert.IsTrue(engine.Settings.Engineer);
            Assert.IsTrue(Has(engine, "step.2.fail.twr"));
            Assert.IsFalse(Has(engine, "step.2.pass"));

            engine = Engine(true);
            var mix = ReportWith(2, "bad-mixture", StepGate.Amber, 8.5, 6);
            GuideHooks.Designer(engine, 2, mix, false);
            var mixBeat = Find(engine, "step.2.fail.mix");
            StringAssert.Contains("8.5", GuidePages.Of(mixBeat, false)[0]);

            engine = Engine(true);
            var dry = ReportWith(3, "dv-margin", StepGate.Red, -0.12, 8000);
            dry.Readout = new QuickReadout { margin = -0.12, required = 8000 };
            GuideHooks.Designer(engine, 3, dry, false);
            StringAssert.Contains("960", GuidePages.Of(Find(engine, "step.3.fail.dv"), false)[0]);

            engine = Engine(true);
            GuideHooks.Designer(engine, 3, ReportWith(3, "maxq-high", StepGate.Amber, 0, 0), false);
            Assert.IsTrue(Has(engine, "step.3.warn.maxq"));

            engine = Engine(true);
            GuideHooks.Designer(engine, 4, ReportWith(4, "payload-doesnt-fit", StepGate.Red, 0, 0), false);
            Assert.IsTrue(Has(engine, "step.4.fail.fit"));

            engine = Engine(true);
            GuideHooks.Designer(engine, 4, ReportWith(4, "tumble", StepGate.Red, 0, 0), false);
            Assert.IsTrue(Has(engine, "step.4.fail.tumble"));

            engine = Engine(true);
            GuideHooks.Designer(engine, 5, ReportWith(5, "recovery-skip", StepGate.Skip, 0, 0), false);
            Assert.IsTrue(Has(engine, "step.5.skip"));

            engine = Engine(true);
            GuideHooks.Designer(engine, 5, ReportWith(5, "landing-reserve", StepGate.Red, 40, 100), false);
            StringAssert.Contains("60", GuidePages.Of(Find(engine, "step.5.fail.reserve"), false)[0]);
        }

        [Test]
        public void APassLineWithoutANumberIsNotQueued()
        {
            var engine = Engine(true);
            Assert.IsFalse(engine.Offer("guide.step", "2.pass", null, "2", false));
            Assert.AreEqual(0, engine.Pending.Count);
        }

        [Test]
        public void PrelaunchNamesTheCauseAndTheRedLight()
        {
            var engine = Engine(true);
            var report = new StepperReport
            {
                HasRedRisk = true,
                Readout = new QuickReadout { maxQ_kPa = 48, qLim_kPa = 35 },
                RedRisks = new List<FlightRisk>
                {
                    new FlightRisk { kind = "structural-maxq", level = "red", odds = -1 },
                    new FlightRisk { kind = "engine-out", level = "red", odds = 0.18 },
                },
            };
            report.Checks[7] = new StepCheck { Code = "structural-maxq", Gate = StepGate.Amber };
            GuideHooks.Designer(engine, 7, report, false);
            Assert.IsTrue(Has(engine, "step.7"));
            Assert.IsTrue(Has(engine, "pre.red.confirm"));
            var maxq = Find(engine, "pre.maxq");
            var page = GuidePages.Of(maxq, true);
            StringAssert.Contains("48", string.Join(" ", page));
            StringAssert.Contains("35", string.Join(" ", page));
            StringAssert.Contains("18", GuidePages.Of(Find(engine, "pre.odds"), false)[0]);

            engine = Engine(true);
            GuideHooks.Designer(engine, 7, new StepperReport(), false);
            Assert.IsTrue(Has(engine, "pre.green"));
        }

        [Test]
        public void AscentCallsAndTheBusUseTheSameScript()
        {
            var engine = Engine(true);
            GuideHooks.Ascent(engine, new[]
            {
                new GuideCallNote { Call = "MaxQ", Open = true, QKpa = 21, QLimKpa = 35 },
                new GuideCallNote { Call = "EngineOut", Open = false, Grade = "Good", EnginesLeft = 4 },
            }, true, false);
            Assert.IsTrue(Has(engine, "asc.auto"));
            Assert.IsTrue(Has(engine, "asc.maxq.warn"));
            Assert.IsTrue(Find(engine, "asc.maxq.warn").Decision);
            StringAssert.Contains("4", GuidePages.Of(Find(engine, "asc.engineout.ok"), false)[0]);

            GuideHooks.FlightBus(engine, "liftoff", null);
            GuideHooks.FlightBus(engine, "meco", null);
            GuideHooks.FlightBus(engine, "orbit", new Dictionary<string, string> { ["peri"] = "198", ["apo"] = "214" });
            Assert.IsTrue(Has(engine, "asc.liftoff"));
            Assert.IsTrue(Has(engine, "asc.meco"));
            StringAssert.Contains("198", GuidePages.Of(Find(engine, "asc.orbit"), true)[1]);
            StringAssert.Contains("214", GuidePages.Of(Find(engine, "asc.orbit"), true)[1]);
        }

        [Test]
        public void ReportHooksQuoteTheFlightAndDoNotScold()
        {
            var engine = Engine(true);
            engine.ShowReport(new GuideReport
            {
                Stamp = 7,
                Passed = true,
                MissionId = "m1",
                MissionIndex = 1,
                Stars = 2,
                Medal = "silver",
                SparePct = 6,
                UnderBudgetKc = 12.5,
                Part = "fins",
            });
            Assert.IsTrue(Has(engine, "win.1"));
            StringAssert.Contains("6", GuidePages.Of(Find(engine, "win.margin"), false)[0]);
            StringAssert.Contains("12.5", GuidePages.Of(Find(engine, "win.budget"), false)[0]);
            StringAssert.Contains("fins", GuidePages.Of(Find(engine, "win.unlock"), false)[0]);
            StringAssert.Contains("2", GuidePages.Of(Find(engine, "win.next"), false)[0]);
            Assert.IsTrue(Has(engine, "medal.star2"));
            Assert.IsTrue(Has(engine, "medal.silver"));

            engine = Engine(true);
            engine.ShowReport(new GuideReport
            {
                Stamp = 8,
                Passed = false,
                MissionId = "m4",
                Kind = "structural-maxq",
                QKpa = 52,
                QLimKpa = 35,
                MissionIndex = 4,
            });
            var bent = GuidePages.Of(Find(engine, "fail.structural-maxq"), false);
            StringAssert.Contains("52", bent[0]);
            StringAssert.Contains("35", bent[1]);
            Assert.IsTrue(Has(engine, "fail.retry"));
            for (int i = 0; i < engine.Pending.Count; i++)
            {
                var pages = GuidePages.Of(engine.Pending[i], true);
                for (int p = 0; p < pages.Length; p++)
                {
                    StringAssert.DoesNotContain("you failed", pages[p].ToLowerInvariant());
                    StringAssert.DoesNotContain("{", pages[p]);
                }
            }
        }

        [Test]
        public void ReportCopiesTheDebriefNumbers()
        {
            var engine = Engine(true);
            var debrief = new FlightDebrief
            {
                Passed = true,
                Kind = "pass",
                Stars = 2,
                MissionId = "m1",
                EconomySettled = true,
                CostKc = 37.5,
                Seed = 11,
            };
            debrief.Medals.Add("silver");
            debrief.Medals.Add("timing-gold");
            debrief.Unlocks.Add("fins");
            debrief.Cosmetics.Add("decal_thrums_toolbox");
            double need = MissionScore.RequiredDeltaV("apogee", 80, 0, 0);
            var mission = new MissionContext
            {
                MissionId = "m1",
                BudgetKc = 50,
                Target = new MissionTarget { Kind = "apogee", ApoKm = 80 },
            };
            var result = new SimResult
            {
                idealDeltaV = need * 1.06,
                orbit = new OrbitElements { apoAlt = 80000 },
            };
            var facts = GuideHooks.FromFlight(debrief, result, mission, null);
            Assert.AreEqual(6, System.Math.Round(facts.SparePct));
            Assert.AreEqual(12.5, facts.UnderBudgetKc);
            engine.ShowReport(facts);
            StringAssert.Contains("6", GuidePages.Of(Find(engine, "win.margin"), false)[0]);
            StringAssert.Contains("12.5", GuidePages.Of(Find(engine, "win.budget"), false)[0]);
            StringAssert.Contains("fins", GuidePages.Of(Find(engine, "win.unlock"), false)[0]);
            StringAssert.Contains("Thrum's Toolbox", GuidePages.Of(Find(engine, "medal.cosmetic"), false)[0]);
            Assert.IsTrue(Has(engine, "medal.silver"));
            Assert.IsTrue(Has(engine, "medal.gold"));
            Assert.IsTrue(Has(engine, "medal.perfect.timing"));
            Assert.IsTrue(Has(engine, "medal.star2"));
            StringAssert.Contains("2", GuidePages.Of(Find(engine, "win.next"), false)[0]);

            engine = Engine(true);
            var lost = new FlightDebrief { Passed = false, Kind = "structural-maxq", MissionId = "m4", Seed = 3 };
            var broken = new SimResult
            {
                failure = new FailureEvent { kind = "structural-maxq", cause = "q", value = 52000, limit = 35000 },
            };
            engine.ShowReport(GuideHooks.FromFlight(lost, broken, new MissionContext { MissionId = "m4" }, null));
            var bent = GuidePages.Of(Find(engine, "fail.structural-maxq"), false);
            StringAssert.Contains("52", bent[0]);
            StringAssert.Contains("35", bent[1]);
            Assert.IsTrue(Has(engine, "fail.retry"));
        }

        [Test]
        public void VoiceStaysOffUntilAGuideClipExists()
        {
            var silent = Engine(false);
            Assert.IsFalse(silent.ClipVoice);
            Assert.IsTrue(silent.Settings.SubtitlesOn);
            Assert.IsFalse(silent.Settings.ReadAloud);
            silent.Offer("guide.step", "6", null, "6", false);
            Assert.IsTrue(string.IsNullOrEmpty(Find(silent, "step.6").VoiceClip));

            string dir = Path.Combine(Path.GetTempPath(), "thrum-clips-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "assets", "narration", "guide"));
            string clip = Path.Combine(dir, "assets", "narration", "guide", "step.6.mp3");
            File.WriteAllBytes(clip, new byte[] { 1, 2, 3 });
            try
            {
                var state = GuideState.Fresh(true, true);
                var voiced = new LiveGuideEngine(json, state, dir);
                Assert.IsTrue(voiced.ClipVoice);
                voiced.Offer("guide.step", "6", null, "6", false);
                Assert.AreEqual(clip, Find(voiced, "step.6").VoiceClip);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void SkipTipsDropsChatterAndKeepsAFailure()
        {
            var engine = Engine(true);
            engine.Offer("guide.step", "1", null, "1", false);
            engine.Offer("guide.step", "2.fail.twr", null, "2", false);
            engine.SkipTipsForStep("1");
            Assert.IsFalse(Has(engine, "step.1"));
            Assert.IsTrue(Has(engine, "step.2.fail.twr"));
        }

        static StepperReport ReportWith(int step, string code, StepGate gate, double value, double limit)
        {
            var report = new StepperReport();
            report.Checks[step] = new StepCheck { Code = code == "pass" ? null : code, Gate = gate, Value = value, Limit = limit };
            return report;
        }

        LiveGuideEngine Engine(bool returning)
        {
            var state = GuideState.Fresh(false, returning);
            return new LiveGuideEngine(json, state, root);
        }

        static GuideBeat Only(LiveGuideEngine engine)
        {
            Assert.AreEqual(1, engine.Pending.Count);
            return engine.Pending[0];
        }

        static bool Has(LiveGuideEngine engine, string id)
        {
            var pending = engine.Pending;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i] != null && pending[i].Id == id) return true;
            return false;
        }

        static GuideBeat Find(LiveGuideEngine engine, string id)
        {
            var pending = engine.Pending;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Id == id) return pending[i];
            Assert.Fail("missing " + id);
            return null;
        }

        static string Repo()
        {
            string[] starts = { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            for (int i = 0; i < starts.Length; i++)
            {
                try { return Catalog.FindRepoRoot(starts[i]); }
                catch (DirectoryNotFoundException) { }
            }
            throw new DirectoryNotFoundException("data/guide.json");
        }
    }
}
