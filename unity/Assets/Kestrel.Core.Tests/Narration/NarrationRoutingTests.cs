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
            var s = new CallSchedule { Meco = meco };
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
