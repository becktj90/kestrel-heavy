// Sim event -> manifest cue, the color windows, the commentary mode, and the finless tag on the debrief pool.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;
using NUnit.Framework;

namespace Kestrel.Core.Tests.Narration
{
    public class NarrationRoutingTests
    {
        static void Is(NarrationCue cue, string id, int lit) { Assert.AreEqual(id, cue.EventId); Assert.AreEqual(lit, cue.LitStage); }

        [Test]
        public void TwoStageRocket_IgnitionThenSes1WithStageTwoLit()
        {
            Is(NarrationRoute.Route("ignition", 1, false, 2), "ignition", 1);
            Is(NarrationRoute.Route("ignition", 2, false, 2), "ses1", 2);
            Is(NarrationRoute.Route("ignition", 3, false, 2), null, 0);
        }

        [Test]
        public void StrapOnRocket_CoreSustainIsSilent_AndTheLaterIgnitionsAreSes1()
        {
            // KH-10 and KH-19: four sim ignitions on a three-stage core with strap-ons.
            Is(NarrationRoute.Route("ignition", 1, true, 3), "ignition", 1);
            Assert.IsTrue(NarrationRoute.Route("ignition", 2, true, 3).Silent, "the core burning on after strap-on sep is not an ignition");
            Is(NarrationRoute.Route("ignition", 3, true, 3), "ses1", 2);
            Is(NarrationRoute.Route("ignition", 4, true, 3), "ses1", 3);
            Assert.IsTrue(NarrationRoute.Route("ignition", 5, true, 3).Silent, "no fifth stage");
        }

        [Test]
        public void Meco_IsStageOne_AndOtherEventsPassThroughWithNoLit()
        {
            Is(NarrationRoute.Route("meco", 0, false, 2), "meco", 1);
            Is(NarrationRoute.Route("meco", 1, true, 3), "meco", 1);
            Is(NarrationRoute.Route("liftoff", 0, false, 2), "liftoff", 0);
            Is(NarrationRoute.Route("maxq", 0, true, 3), "maxq", 0);
            Is(NarrationRoute.Route("fairing", 0, false, 2), "fairing", 0);
            Assert.IsTrue(NarrationRoute.Route("", 1, false, 2).Silent);
            Assert.IsTrue(NarrationRoute.Route(null, 1, false, 2).Silent);
        }

        [Test]
        public void KH10AndKH19_RouteToTheirThreeCores()
        {
            foreach (var id in new[] { "m10", "m19" })
            {
                var f = NarrationFacts.From(Kestrel.Core.Vehicle.Design.FromJson(Starter(id)), "pier-7");
                Assert.AreEqual(3, f.Stages, id);
                Assert.IsTrue(f.TryGet("strapons", out var sc) && (int)sc == 2, id + " has two strap-ons");
                var seen = new List<string>();
                for (int sim = 1; sim <= 5; sim++)
                {
                    var cue = NarrationRoute.Route("ignition", sim, true, f.Stages);
                    seen.Add(cue.Silent ? "-" : cue.EventId + cue.LitStage);
                }
                CollectionAssert.AreEqual(new[] { "ignition1", "-", "ses12", "ses13", "-" }, seen, id);
            }
        }

        [Test]
        public void DefaultLit_PutsTheFirstStageCuesOnStageOne_AndSecoOnTheTopStage()
        {
            Assert.AreEqual(1, NarrationRoute.DefaultLit("ignition", 3));
            Assert.AreEqual(1, NarrationRoute.DefaultLit("tankpress", 3));
            Assert.AreEqual(1, NarrationRoute.DefaultLit("meco", 3));
            Assert.AreEqual(3, NarrationRoute.DefaultLit("seco", 3));
            Assert.AreEqual(2, NarrationRoute.DefaultLit("seco", 2));
            Assert.AreEqual(0, NarrationRoute.DefaultLit("liftoff", 3));
            Assert.AreEqual(0, NarrationRoute.DefaultLit("seco", 0));
        }

        // ---- commentary mode --------------------------------------------------------------------------

        [Test]
        public void Commentary_PrefKeyAndModes()
        {
            Assert.AreEqual("kh.audio.commentary", Commentary.Pref);
            Assert.AreEqual(CommentaryMode.Full, Commentary.FromInt(0));
            Assert.AreEqual(CommentaryMode.CallsOnly, Commentary.FromInt(1));
            Assert.AreEqual(CommentaryMode.Off, Commentary.FromInt(2));
            Assert.AreEqual(CommentaryMode.Full, Commentary.FromInt(9), "an unknown value is the default");
            Assert.AreEqual(CommentaryMode.Full, Commentary.FromInt(-1));
        }

        [Test]
        public void Commentary_PrefIsMirroredWithTheOtherSettings()
        {
            CollectionAssert.Contains(Kestrel.Core.Save.SettingsMirror.Keys, Commentary.Pref);
            CollectionAssert.Contains(Kestrel.Core.Save.SettingsMirror.Keys, "kh.audio.narration.muted", "the old mute stays so a save can still migrate");
        }

        [Test]
        public void Commentary_LegacyMuteMigratesToOff_AndAnExplicitPrefWins()
        {
            Assert.AreEqual(CommentaryMode.Full, Commentary.Resolve(false, 0, false));
            Assert.AreEqual(CommentaryMode.Off, Commentary.Resolve(false, 0, true));
            Assert.AreEqual(CommentaryMode.Full, Commentary.Resolve(true, 0, true), "the player chose Full after the old mute");
            Assert.AreEqual(CommentaryMode.CallsOnly, Commentary.Resolve(true, 1, true));
            Assert.AreEqual(CommentaryMode.Off, Commentary.Resolve(true, 2, false));
        }

        [Test]
        public void Commentary_CyclesAndGatesTheLayers()
        {
            Assert.AreEqual(CommentaryMode.CallsOnly, Commentary.Next(CommentaryMode.Full));
            Assert.AreEqual(CommentaryMode.Off, Commentary.Next(CommentaryMode.CallsOnly));
            Assert.AreEqual(CommentaryMode.Full, Commentary.Next(CommentaryMode.Off));
            Assert.IsTrue(Commentary.AllowsCalls(CommentaryMode.Full));
            Assert.IsTrue(Commentary.AllowsCalls(CommentaryMode.CallsOnly));
            Assert.IsFalse(Commentary.AllowsCalls(CommentaryMode.Off));
            Assert.IsTrue(Commentary.AllowsColor(CommentaryMode.Full));
            Assert.IsFalse(Commentary.AllowsColor(CommentaryMode.CallsOnly));
            Assert.IsFalse(Commentary.AllowsColor(CommentaryMode.Off));
            Assert.AreEqual("Narration on", Commentary.Label(CommentaryMode.Full));
            Assert.AreEqual("Narration calls", Commentary.Label(CommentaryMode.CallsOnly));
            Assert.AreEqual("Narration off", Commentary.Label(CommentaryMode.Off));
        }

        // ---- color windows ----------------------------------------------------------------------------

        [Test]
        public void Windows_AreStrictAndUnknownIsNo()
        {
            Assert.IsTrue(ColorWindows.PadOk(true, 20.1));
            Assert.IsFalse(ColorWindows.PadOk(true, 20.0));
            Assert.IsFalse(ColorWindows.PadOk(false, 100));
            Assert.IsFalse(ColorWindows.PadOk(true, double.NaN));
            Assert.IsTrue(ColorWindows.AscentOk(true, 12.1));
            Assert.IsFalse(ColorWindows.AscentOk(true, 12.0));
            Assert.IsFalse(ColorWindows.AscentOk(false, 100));
            Assert.IsFalse(ColorWindows.AscentOk(true, double.NaN));
            Assert.IsTrue(ColorWindows.KarmanCrossed(99999, 100000));
            Assert.IsFalse(ColorWindows.KarmanCrossed(100000, 100500), "already above");
            Assert.IsFalse(ColorWindows.KarmanCrossed(50000, 60000));
            Assert.IsTrue(ColorWindows.CoastOk(15.1));
            Assert.IsFalse(ColorWindows.CoastOk(15.0));
            Assert.IsFalse(ColorWindows.CoastOk(double.NaN));
            Assert.IsTrue(ColorWindows.RecoveryFits(8, 8));
            Assert.IsFalse(ColorWindows.RecoveryFits(7.9, 8));
            Assert.IsFalse(ColorWindows.RecoveryFits(double.NaN, 8));
            Assert.IsTrue(ColorWindows.IsColorEvent("color.pad"));
            Assert.IsFalse(ColorWindows.IsColorEvent("pad"));
        }

        static List<string> Poll(ColorDirector d, double t, double alt)
        {
            var cues = new List<ColorCue>();
            d.Poll(t, alt, cues);
            var ids = new List<string>();
            foreach (var c in cues) ids.Add(c.EventId);
            return ids;
        }

        static CallSchedule Calls(double meco, params double[] calls)
        {
            var s = new CallSchedule { Meco = meco, HasMeco = !double.IsNaN(meco) };
            foreach (var c in calls) s.AddCall(c);
            return s;
        }

        [Test]
        public void Director_AscentOpensAfterMaxQ_AndClosesTwelveSecondsBeforeMeco()
        {
            var d = new ColorDirector(false);
            d.UseCalls(Calls(150, 10, 60, 150));
            CollectionAssert.DoesNotContain(Poll(d, 30, 5000), "color.ascent", "before max-Q");
            d.OnEvent("maxq");
            CollectionAssert.Contains(Poll(d, 40, 9000), "color.ascent");
            d.MarkDone("color.ascent");
            CollectionAssert.DoesNotContain(Poll(d, 41, 9500), "color.ascent", "once only");
            var late = new ColorDirector(false);
            late.UseCalls(Calls(150, 60, 150));
            late.OnEvent("maxq");
            CollectionAssert.DoesNotContain(Poll(late, 139, 40000), "color.ascent", "11 s to MECO is too late");
        }

        // ---- color.ascent on a rocket with no MECO (solid first stage) ---------------------------------

        static CallSchedule Solid(double burnout, double maxq, params double[] calls)
        {
            var s = new CallSchedule { Meco = burnout, HasMeco = false, MaxQ = maxq };
            foreach (var c in calls) s.AddCall(c);
            return s;
        }

        static ColorCue? Ascent(ColorDirector d, double t)
        {
            var cues = new List<ColorCue>();
            d.Poll(t, 9000, cues);
            foreach (var c in cues) if (c.EventId == ColorWindows.Ascent) return c;
            return null;
        }

        [Test]
        public void Director_NoMeco_AscentOpensTenSecondsAfterMaxQ_AndMustFitBeforeTheNextCall()
        {
            // Burnout 4 s after max-Q: the MECO rule (12 s before MECO) could never open; the time rule does.
            var calls = Solid(36, 32, 0, 36, 120);
            var d = new ColorDirector(false);
            d.UseCalls(calls);
            Assert.IsNull(Ascent(d, 31), "before max-Q");
            d.OnEvent("maxq", 31.5);
            Assert.IsNull(Ascent(d, 41.4), "9.9 s after max-Q");
            var cue = Ascent(d, 41.9);
            Assert.IsNotNull(cue, "the next poll, 10.4 s after max-Q and past the burnout");
            Assert.IsTrue(cue.Value.MustFit, "the line must end before the next call");
            Assert.AreEqual(120 - 41.9, cue.Value.SecToNextCall, 1e-9);
            Assert.IsFalse(ColorWindows.AscentNoMecoOk(9.999));
            Assert.IsTrue(ColorWindows.AscentNoMecoOk(10));
            Assert.IsTrue(ColorWindows.RecoveryFits(cue.Value.SecToNextCall, 9.5));
            d.MarkDone(ColorWindows.Ascent);
            Assert.IsNull(Ascent(d, 50), "once only");
        }

        [Test]
        public void Director_NoMeco_AClipThatRunsIntoTheNextCallIsRefused()
        {
            var d = new ColorDirector(false);
            d.UseCalls(Solid(36, 32, 36, 48));
            d.OnEvent("maxq", 32);
            var cue = Ascent(d, 42);
            Assert.IsNotNull(cue);
            Assert.IsTrue(cue.Value.MustFit);
            Assert.AreEqual(6, cue.Value.SecToNextCall, 1e-9);
            Assert.IsFalse(ColorWindows.RecoveryFits(cue.Value.SecToNextCall, 8), "an 8 s exchange would sit on the 48 s call");
            Assert.IsTrue(ColorWindows.RecoveryFits(cue.Value.SecToNextCall, 6));
        }

        [Test]
        public void Director_NoMeco_UsesTheNominalMaxQ_WhenTheEventHadNoTime_AndFailsClosedWithNeither()
        {
            var nominal = new ColorDirector(false);
            nominal.UseCalls(Solid(36, 30, 120));
            nominal.OnEvent("maxq");
            Assert.IsNull(Ascent(nominal, 39.9));
            Assert.IsNotNull(Ascent(nominal, 40.5), "nominal max-Q 30 s + 10 s");

            var polled = new ColorDirector(false);
            polled.UseCalls(Solid(36, double.NaN, 120));
            Ascent(polled, 33);
            polled.OnEvent("maxq");
            Assert.IsNull(Ascent(polled, 42.9));
            Assert.IsNotNull(Ascent(polled, 43.4), "the last polled time stands in for the event time");

            var blind = new ColorDirector(false);
            blind.UseCalls(Solid(36, double.NaN, 120));
            blind.OnEvent("maxq");
            Assert.IsNull(Ascent(blind, 200), "no max-Q time at all: no window");
        }

        [Test]
        public void Director_NoMeco_EntryClosesTheWindow_AndAMecoRocketKeepsTheMecoRule()
        {
            var calls = Solid(36, 30, 400);
            calls.Entry = 60;
            var d = new ColorDirector(false);
            d.UseCalls(calls);
            d.OnEvent("maxq", 30);
            Assert.IsNull(Ascent(d, 61), "after the entry burn");

            var liquid = new ColorDirector(false);
            liquid.UseCalls(Calls(150, 150));
            liquid.OnEvent("maxq", 60);
            var cue = Ascent(liquid, 61);
            Assert.IsNotNull(cue, "a MECO rocket opens right after max-Q");
            Assert.IsFalse(cue.Value.MustFit, "the MECO window already keeps 12 s clear");
            Assert.IsNull(Ascent(liquid, 139), "and still closes 12 s before MECO, whatever max-Q + 10 s says");
        }

        [Test]
        public void SolidFirstStages_HaveNoMeco_AndStillOpenColorAscent()
        {
            // No shipped template or preset flies a solid first stage today, so the golden solid sounding fixture
            // stands in, plus any shipped one that appears later.
            var cat = Golden.Catalog;
            var designs = new List<Kestrel.Core.Vehicle.Design> { Golden.Fixture(Golden.SolidSoundingFixture) };
            var dir = Path.Combine(Golden.Root, "web", "js", "data", "templates");
            var files = new List<string>(Directory.GetFiles(dir, "*.json"));
            files.Sort(System.StringComparer.Ordinal);
            foreach (var f in files) designs.Add(Kestrel.Core.Vehicle.Design.FromJson(Kestrel.Core.Json.MiniJson.ParseObject(File.ReadAllText(f))));
            designs.AddRange(cat.Presets);
            int solids = 0;
            foreach (var design in designs)
            {
                if (design.stages == null || design.stages.Count == 0 || design.stages[0].propellant != "solid-apcp") continue;
                var tl = Kestrel.Core.Timeline.TimelineGenerator.Generate(cat, design, Kestrel.Core.Timeline.Pad.For(cat, design),
                    new Kestrel.Core.Timeline.Mission { mode = "standard" }, 1);
                var calls = CallSchedule.From(tl);
                if (double.IsNaN(calls.MaxQ)) continue;
                solids++;
                Assert.IsFalse(calls.HasMeco, design.id + ": a solid burns out, it has no MECO");
                var d = new ColorDirector(false);
                d.UseCalls(calls);
                d.OnEvent("maxq", calls.MaxQ);
                bool opened = false;
                for (double t = calls.MaxQ; t < calls.MaxQ + 600 && !opened; t += 0.5)
                {
                    var cue = Ascent(d, t);
                    if (cue == null) continue;
                    Assert.GreaterOrEqual(t, calls.MaxQ + ColorWindows.AscentAfterMaxQSec - 1e-9, design.id);
                    Assert.IsTrue(cue.Value.MustFit, design.id);
                    opened = true;
                }
                Assert.IsTrue(opened, design.id + ": color.ascent is not dropped on a rocket with no MECO");
            }
            Assert.Greater(solids, 0, "the solid fixture has a max-Q");
        }

        [Test]
        public void Director_PollsEveryHalfSecond()
        {
            var d = new ColorDirector(false);
            d.UseCalls(Calls(150, 150));
            d.OnEvent("maxq");
            CollectionAssert.Contains(Poll(d, 40, 9000), "color.ascent");
            Assert.IsEmpty(Poll(d, 40.2, 9100), "inside the poll interval");
            CollectionAssert.Contains(Poll(d, 40.6, 9200), "color.ascent", "offered again until it is marked done");
        }

        [Test]
        public void Director_KarmanFiresOnTheCrossingAndLapsesAfterThreeSeconds()
        {
            var d = new ColorDirector(false);
            d.UseCalls(CallSchedule.Empty);
            Poll(d, 100, 99000);
            CollectionAssert.Contains(Poll(d, 101, 100100), "color.karman");
            var late = new ColorDirector(false);
            Poll(late, 100, 99000);
            Poll(late, 101, 100100);
            CollectionAssert.DoesNotContain(Poll(late, 104.6, 105000), "color.karman");
        }

        [Test]
        public void Director_CoastNeedsMecoAndFifteenQuietSeconds()
        {
            var d = new ColorDirector(false);
            d.UseCalls(Calls(150, 150, 200));
            CollectionAssert.DoesNotContain(Poll(d, 151, 90000), "color.coast", "no MECO yet");
            d.OnEvent("meco");
            CollectionAssert.Contains(Poll(d, 152, 90000), "color.coast");
            var busy = new ColorDirector(false);
            busy.UseCalls(Calls(150, 150, 160));
            busy.OnEvent("meco");
            CollectionAssert.DoesNotContain(Poll(busy, 152, 90000), "color.coast", "a call 8 s away");
        }

        [Test]
        public void Director_RecoveryNeedsAPickedLanding_AndRidesBetweenBoostbackAndEntry()
        {
            var calls = Calls(150, 150, 400);
            calls.Boostback = 170; calls.Entry = 300;
            var none = new ColorDirector(false);
            none.UseCalls(calls); none.OnEvent("meco");
            CollectionAssert.DoesNotContain(Poll(none, 200, 90000), "color.recovery", "no landing picked");
            var d = new ColorDirector(true);
            d.UseCalls(calls);
            CollectionAssert.DoesNotContain(Poll(d, 175, 90000), "color.recovery", "no MECO yet");
            d.OnEvent("meco");
            CollectionAssert.Contains(Poll(d, 176, 90000), "color.recovery", "the boostback has started");
            var early = new ColorDirector(true);
            early.UseCalls(calls); early.OnEvent("meco");
            CollectionAssert.DoesNotContain(Poll(early, 160, 90000), "color.recovery", "before the boostback");
            CollectionAssert.Contains(Poll(d, 200, 90000), "color.recovery");
            CollectionAssert.DoesNotContain(Poll(d, 301, 90000), "color.recovery", "after the entry burn");
            var barge = new CallSchedule { Meco = 150, Entry = 300 };
            var b = new ColorDirector(true);
            b.UseCalls(barge); b.OnEvent("meco");
            CollectionAssert.DoesNotContain(Poll(b, 160, 90000), "color.recovery", "a barge waits 20 s after MECO");
            CollectionAssert.Contains(Poll(b, 171, 90000), "color.recovery");
            var noEntry = new ColorDirector(true);
            noEntry.UseCalls(new CallSchedule { Meco = 150 }); noEntry.OnEvent("meco");
            CollectionAssert.DoesNotContain(Poll(noEntry, 200, 90000), "color.recovery", "no entry event, no window");
        }

        [Test]
        public void Director_PadNeedsAHoldWithTwentySecondsToSpare_AndResetClears()
        {
            var d = new ColorDirector(false);
            CollectionAssert.DoesNotContain(Poll(d, 1, 0), "color.pad", "the live countdown has no hold");
            d.NoteHold(true, 45);
            CollectionAssert.Contains(Poll(d, 2, 0), "color.pad");
            d.NoteHold(true, 10);
            CollectionAssert.DoesNotContain(Poll(d, 3, 0), "color.pad");
            d.NoteHold(true, 45);
            d.MarkDone("color.pad");
            CollectionAssert.DoesNotContain(Poll(d, 4, 0), "color.pad");
            d.Reset();
            d.NoteHold(true, 45);
            CollectionAssert.Contains(Poll(d, 5, 0), "color.pad");
        }

        [Test]
        public void CallSchedule_NextCallAndNoMoreCalls()
        {
            var s = Calls(100, 30, 10, 60);
            Assert.AreEqual(10, s.SecondsToNextCall(0), 1e-9);
            Assert.AreEqual(20, s.SecondsToNextCall(10), 1e-9, "a call at exactly now is behind us");
            Assert.AreEqual(double.PositiveInfinity, s.SecondsToNextCall(61));
            Assert.AreEqual(3, s.Count);
        }

        // ---- the debrief pool's finless tag -----------------------------------------------------------

        [Test]
        public void Pool_FinlessLineOnlyForAFinlessRocket()
        {
            var pool = NarrationPool.Load(Golden.Root);
            var finless = new List<string> { "single", "finless" };
            var finned = new List<string> { "single" };
            for (ulong seed = 0; seed < 40; seed++)
            {
                var a = pool.Pick("tumble", "stability", finless, seed);
                var b = pool.Pick("tumble", "stability", finned, seed);
                Assert.IsNotNull(a); Assert.IsNotNull(b);
                StringAssert.DoesNotContain("No fins", b.Kid, "a finned rocket has fins (seed " + seed + ")");
                StringAssert.Contains("No fins", a.Kid, "the finless line is the best match for a finless rocket");
            }
            Assert.AreEqual(1, pool.VariantCount("tumble", "stability", finless), "only the finless line ties for best");
            Assert.AreEqual(2, pool.VariantCount("tumble", "stability", finned), "the two neutral lines");
        }

        // ---- the debrief pool's engine-out cluster gate ------------------------------------------------

        static FlightFacts EngineOut(int enginesOnFailedStage, bool redundant = false, int engineCount = 9)
            => new FlightFacts
            {
                RedundantAvionics = redundant,
                EngineCount = engineCount,
                Failure = new FailureEvent { kind = "engine-out", cause = "engine-out", value = 1, limit = enginesOnFailedStage, t = 48, stage = 1 },
            };

        [Test]
        public void Pool_ClusterEngineOutNeedsThreeEnginesOnTheFailedStage()
        {
            var pool = NarrationPool.Load(Golden.Root);
            foreach (int n in new[] { 3, 4, 7, 9, 27 })
            {
                var facts = EngineOut(n);
                CollectionAssert.Contains(facts.DesignTags, FlightFacts.ClusterOutTag, n + " engines");
                Assert.AreEqual(n, facts.FailedStageEngines);
                for (ulong seed = 0; seed < 40; seed++)
                    Assert.AreEqual("narr.engine.out.2", pool.Pick("engine-out", "engine-out", facts.DesignTags, seed).Id,
                        "the cluster take claims the engine count, so it is the most specific true line (" + n + " engines, seed " + seed + ")");
                Assert.AreEqual(1, pool.VariantCount("engine-out", "engine-out", facts.DesignTags));
            }
        }

        [Test]
        public void Pool_FewerThanThreeEnginesFallBackToThePlainEngineOutTake()
        {
            var pool = NarrationPool.Load(Golden.Root);
            foreach (var (n, count) in new[] { (0, 1), (1, 1), (2, 2), (2, 9) })
            {
                var facts = EngineOut(n, false, count);
                CollectionAssert.DoesNotContain(facts.DesignTags, FlightFacts.ClusterOutTag, n + " engines");
                for (ulong seed = 0; seed < 40; seed++)
                {
                    var line = pool.Pick("engine-out", "engine-out", facts.DesignTags, seed);
                    Assert.IsNotNull(line, n + " engines still hear an engine-out line");
                    Assert.AreEqual("narr.engine.out.1", line.Id, n + " engines, seed " + seed);
                    StringAssert.DoesNotContain("cluster", line.Kid.ToLowerInvariant());
                }
            }
        }

        [Test]
        public void Pool_ClusterTagOnlyComesFromAnEngineOut()
        {
            var facts = new FlightFacts { EngineCount = 9, Failure = new FailureEvent { kind = "engine-overpressure", limit = 9 } };
            CollectionAssert.DoesNotContain(facts.DesignTags, FlightFacts.ClusterOutTag);
            Assert.AreEqual(0, facts.FailedStageEngines);
            Assert.AreEqual(0, new FlightFacts { EngineCount = 9 }.FailedStageEngines, "no failure, no count");
        }

        [Test]
        public void Pool_EveryEngineOutCaseKeepsATrueLine()
        {
            var pool = NarrationPool.Load(Golden.Root);
            foreach (bool redundant in new[] { false, true })
                foreach (bool finless in new[] { false, true })
                    for (int n = 0; n <= 12; n++)
                    {
                        var facts = EngineOut(n, redundant, n);
                        facts.Finless = finless;
                        Assert.GreaterOrEqual(pool.VariantCount("engine-out", "engine-out", facts.DesignTags), 1, "n " + n + " redundant " + redundant);
                        var line = pool.Pick("engine-out", "engine-out", facts.DesignTags, 7);
                        Assert.IsNotNull(line);
                        if (n < FlightFacts.ClusterOutMinEngines) Assert.AreNotEqual("narr.engine.out.2", line.Id, "n " + n);
                    }
        }

        [Test]
        public void Pool_StringOverloadStillWorks()
        {
            var pool = NarrationPool.Load(Golden.Root);
            Assert.IsNotNull(pool.Pick("tumble", "alpha", "single", 3));
            Assert.AreEqual(pool.VariantCount("tumble", "alpha", "single"),
                pool.VariantCount("tumble", "alpha", new List<string> { "single" }));
        }

        static Kestrel.Core.Json.JsonObject Starter(string id)
        {
            var file = Kestrel.Core.Json.MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            foreach (Kestrel.Core.Json.JsonObject m in file.Arr("missions"))
                if (m.Str("id") == id) return (Kestrel.Core.Json.JsonObject)m.Arr("designs")[0];
            throw new KeyNotFoundException(id);
        }
    }
}
