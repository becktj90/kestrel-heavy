using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Timeline;
using Kestrel.Game.Audio;
using NUnit.Framework;

namespace Kestrel.Core.Tests.Narration
{
    /// <summary>Pad, tank-press, SECO, Karman and entry calls: due only when the event really happens for the flown design.</summary>
    public class CallTriggersTests
    {
        static CallSchedule Truth(int[] press = null, int[] seco = null, bool karman = false, bool entry = false)
        {
            var s = new CallSchedule();
            if (press != null) s.PressStages.AddRange(press);
            if (seco != null) s.SecoStages.AddRange(seco);
            s.HasKarman = karman;
            s.HasEntry = entry;
            return s;
        }

        static List<string> Drain(CallTriggers t, double now, bool busy = false, bool pending = false)
        {
            var got = new List<string>();
            for (int i = 0; i < 20 && t.Next(now, busy, pending, out var cue); i++)
            {
                got.Add(cue.LitStage > 0 ? cue.EventId + "." + cue.LitStage : cue.EventId);
                t.Offered(cue);
            }
            return got;
        }

        static RecoveryMark Mark(RecoveryPhase p, double t) => new RecoveryMark { Phase = p, T = t };

        // ---- the count ----

        [Test]
        public void Pad_IsDueWhenTheCountOpens_IntoSilence_WithATakeThatEndsBeforeTMinusZero()
        {
            var t = new CallTriggers(Truth(), 2, false);
            Assert.IsFalse(t.Next(0, false, false, out _), "nothing before the count");
            t.CountTick(-12, new string[0], 0);
            Assert.IsFalse(t.Next(0, true, false, out _), "a line is playing: a pre-launch call waits for silence");
            Assert.IsTrue(t.Next(0, false, false, out var cue));
            Assert.AreEqual(CallTriggers.Pad, cue.EventId);
            Assert.IsTrue(cue.PreLaunch);
            Assert.AreEqual(12 - CallTriggers.LiftoffMarginSec, cue.MaxSec, 1e-9);
            t.CountTick(-4, new string[0], 8);
            Assert.IsTrue(t.Next(8, false, false, out cue));
            Assert.AreEqual(4 - CallTriggers.LiftoffMarginSec, cue.MaxSec, 1e-9, "the room left shrinks with the count");
            t.Offered(cue);
            t.CountTick(-3, new string[0], 9);
            Assert.IsEmpty(Drain(t, 9), "pad fires once");
        }

        [Test]
        public void TankPress_OnlyForTheStagesTheTimelinePresses_InCountOrder_Once()
        {
            var t = new CallTriggers(Truth(press: new[] { 1, 2 }), 3, true);
            var beats = new List<string> { "term.poll", "count.press.so", "count.press.s1" };
            t.CountTick(-12, beats, 0);
            CollectionAssert.AreEqual(new[] { "pad", "tankpress.1" }, Drain(t, 0), "strap-on presses are not a core stage's call");
            beats.Add("count.press.s2");
            beats.Add("count.press.s3");
            t.CountTick(-10, beats, 2);
            t.CountTick(-9, beats, 3);
            CollectionAssert.AreEqual(new[] { "tankpress.2" }, Drain(t, 3), "s3 is not pressed by this design (solid or hypergolic), and nothing repeats");
        }

        [Test]
        public void PreLaunchCalls_DropWhenNoTakeCanEndBeforeTMinusZero_AndAtLiftoff()
        {
            var t = new CallTriggers(Truth(press: new[] { 1 }), 2, false);
            t.CountTick(-12, new[] { "count.press.s1" }, 0);
            t.CountTick(-0.4, new[] { "count.press.s1" }, 11.6);
            Assert.IsEmpty(Drain(t, 11.6), "under the margin: no room before T-0");

            var u = new CallTriggers(Truth(press: new[] { 1 }), 2, false);
            u.CountTick(-12, new[] { "count.press.s1" }, 0);
            u.Liftoff();
            Assert.IsEmpty(Drain(u, 0), "the count's calls do not play in flight");
            u.CountTick(-5, new[] { "count.press.s1" }, 1);
            Assert.IsEmpty(Drain(u, 1), "and the count does not reopen after liftoff");
        }

        // ---- the flight ----

        [Test]
        public void Seco_OnlyOnInsertionAfterTheTopStagesGuidedCutoff_OnADesignThatFliesASeco()
        {
            var t = new CallTriggers(Truth(seco: new[] { 2 }), 2, false);
            t.OnSimEvent("cutoff", 2, 0);
            Assert.IsEmpty(Drain(t, 0), "the cutoff alone is not insertion");
            t.OnSimEvent("orbit", 0, 0.1);
            CollectionAssert.AreEqual(new[] { "seco.2" }, Drain(t, 0.1));

            var dry = new CallTriggers(Truth(seco: new[] { 2 }), 2, false);
            dry.OnSimEvent("burnout", 2, 0);
            dry.OnSimEvent("orbit", 0, 0);
            Assert.IsEmpty(Drain(dry, 0), "running dry is a burnout, not a SECO");

            var noRestart = new CallTriggers(Truth(), 2, false);
            noRestart.OnSimEvent("cutoff", 2, 0);
            noRestart.OnSimEvent("orbit", 0, 0);
            Assert.IsEmpty(Drain(noRestart, 0), "an engine that cannot restart ends in a cutoff, not a SECO");

            var middle = new CallTriggers(Truth(seco: new[] { 2, 3 }), 3, false);
            middle.OnSimEvent("cutoff", 2, 0);
            middle.OnSimEvent("orbit", 0, 0);
            Assert.IsEmpty(Drain(middle, 0), "only the top stage's SECO is insertion");

            var single = new CallTriggers(Truth(seco: new[] { 1 }), 1, false);
            single.OnSimEvent("cutoff", 1, 0);
            single.OnSimEvent("orbit", 0, 0);
            Assert.IsEmpty(Drain(single, 0), "one stage has no second-stage cutoff");

            var straps = new CallTriggers(Truth(seco: new[] { 2 }), 2, true);
            straps.OnSimEvent("cutoff", 3, 0);
            straps.OnSimEvent("orbit", 0, 0);
            CollectionAssert.AreEqual(new[] { "seco.2" }, Drain(straps, 0), "sim stack entry 3 is core stage 2 behind the strap-on group");

            var lost = new CallTriggers(Truth(seco: new[] { 2 }), 2, false);
            lost.OnSimEvent("cutoff", 2, 0);
            lost.OnSimEvent("failure", 2, 0);
            lost.OnSimEvent("orbit", 0, 0);
            Assert.IsEmpty(Drain(lost, 0));
        }

        [Test]
        public void Karman_OnTheStepThatCrosses100Km_Once_AndOnlyWhenTheTimelineCrossesIt()
        {
            var t = new CallTriggers(Truth(karman: true), 2, false);
            t.OnAltitude(99000, 0);
            Assert.IsEmpty(Drain(t, 0));
            t.OnAltitude(100500, 1);
            CollectionAssert.AreEqual(new[] { "karman" }, Drain(t, 1));
            t.OnAltitude(99000, 2);
            t.OnAltitude(101000, 3);
            Assert.IsEmpty(Drain(t, 3), "once");

            var low = new CallTriggers(Truth(), 2, false);
            low.OnAltitude(99000, 0);
            low.OnAltitude(100500, 1);
            Assert.IsEmpty(Drain(low, 1));
        }

        [Test]
        public void Entry_WhenTheBoostersEntryBurnMarkIsCurrent_NeverAStaleOne()
        {
            var marks = new[] { Mark(RecoveryPhase.Boostback, 150), Mark(RecoveryPhase.Entry, 330) };
            var t = new CallTriggers(Truth(entry: true), 2, false, true);
            t.OnRecovery(new[] { marks[0] }, 200, 0);
            Assert.IsEmpty(Drain(t, 0), "boostback is not the entry burn");
            t.OnRecovery(marks, 329, 1);
            Assert.IsEmpty(Drain(t, 1), "the mark is ahead of the rail clock");
            t.OnRecovery(marks, 331, 2);
            CollectionAssert.AreEqual(new[] { "entry" }, Drain(t, 2));
            t.OnRecovery(marks, 332, 3);
            Assert.IsEmpty(Drain(t, 3), "once");

            var late = new CallTriggers(Truth(entry: true), 2, false, true);
            late.OnRecovery(marks, 330 + CallTriggers.FreshSec + 1, 0);
            late.OnRecovery(marks, 331, 1);
            Assert.IsEmpty(Drain(late, 1), "first seen stale (the booster was run to the end at once): it never plays");

            var none = new CallTriggers(Truth(), 2, false, true);
            none.OnRecovery(marks, 331, 0);
            Assert.IsEmpty(Drain(none, 0), "no entry burn in this design's timeline");

            var unpicked = new CallTriggers(Truth(entry: true), 2, false, false);
            unpicked.OnRecovery(marks, 331, 0);
            Assert.IsEmpty(Drain(unpicked, 0), "landing hardware but no landing picked: the booster is not coming home");
        }

        [Test]
        public void FlightCalls_NeverGoOverAWaitingCall_QueueBehindAPlayingOne_AndExpire()
        {
            var t = new CallTriggers(Truth(karman: true), 2, false);
            t.OnAltitude(99000, 0);
            t.OnAltitude(100100, 10);
            Assert.IsFalse(t.Next(10, true, true, out _), "a call is waiting: never replace it");
            Assert.IsFalse(t.Next(11, false, true, out _));
            Assert.IsTrue(t.Next(12, true, false, out var cue), "a call is playing and none waits: queue behind it (calls never cut calls)");
            Assert.IsFalse(cue.PreLaunch);
            Assert.IsTrue(double.IsPositiveInfinity(cue.MaxSec));
            Assert.IsFalse(t.Next(10 + CallTriggers.FreshSec + 0.01, false, false, out _), "past its moment it is dropped, not said late");
        }

        [Test]
        public void Schedule_ACallThatDoesNotFitIsDropped_AndDoesNotStepOnThePlayingCall()
        {
            var cat = NarrationCatalog.Parse(File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Audio", "Runtime", "Resources", "Narration", "manifest.json")));
            var s = new NarrationSchedule();
            var none = s.Offer(cat, "pad", null, 0, CommentaryMode.Full, 0, line => line.Duration <= 0.1);
            Assert.AreEqual(NarrationOutcome.Dropped, none.Outcome);
            Assert.AreEqual("no room", none.Reason);
            Assert.IsFalse(s.Busy);
            var first = s.Offer(cat, "pad", null, 0, CommentaryMode.Full, 0, line => line.Duration <= 11.5);
            Assert.IsTrue(first.Started);
            var again = new NarrationSchedule();
            Assert.AreEqual(first.Resource, again.Offer(cat, "pad", null, 0, CommentaryMode.Full, 0).Resource, "a dropped offer does not move the cursor");
            var queued = s.Offer(cat, "ignition", null, 1, CommentaryMode.Full, 1);
            Assert.AreEqual(NarrationOutcome.Queued, queued.Outcome, "calls still wait for the playing call");
        }

        // ---- the truth comes from the timeline ----

        static List<Kestrel.Core.Vehicle.Design> Designs()
        {
            var cat = Golden.Catalog;
            var designs = new List<Kestrel.Core.Vehicle.Design> { Golden.Fixture(Golden.SolidSoundingFixture) };
            var dir = Path.Combine(Golden.Root, "web", "js", "data", "templates");
            var files = new List<string>(Directory.GetFiles(dir, "*.json"));
            files.Sort(System.StringComparer.Ordinal);
            foreach (var f in files) designs.Add(Kestrel.Core.Vehicle.Design.FromJson(Kestrel.Core.Json.MiniJson.ParseObject(File.ReadAllText(f))));
            designs.AddRange(cat.Presets);
            // No shipped design picks a landing (the player does, in the designer), so the reusable template is flown
            // with each pick as well.
            foreach (var mode in new[] { "barge", "rtls" })
            {
                var picked = Kestrel.Core.Vehicle.Design.FromJson(Kestrel.Core.Json.MiniJson.ParseObject(File.ReadAllText(Path.Combine(dir, "medium-kerolox-reusable.json"))));
                picked.recoveryMode = mode;
                picked.id += "+" + mode;
                designs.Add(picked);
            }
            return designs;
        }

        static Timeline.Timeline Generate(Kestrel.Core.Vehicle.Design d, string mode = "standard")
        {
            var cat = Golden.Catalog;
            return TimelineGenerator.Generate(cat, d, Pad.For(cat, d), new Mission { mode = mode }, 1);
        }

        static bool Has(Timeline.Timeline tl, string id)
        {
            foreach (var e in tl.events) if (e != null && e.id == id) return true;
            return false;
        }

        [Test]
        public void TimelineTruth_MatchesWhatEachShippedDesignFlies()
        {
            int press = 0, seco = 0, karman = 0, entry = 0;
            foreach (var d in Designs())
            {
                var tl = Generate(d);
                var s = CallSchedule.From(tl);
                for (int i = 0; i < d.stages.Count; i++)
                {
                    bool pressed = s.PressStages.Contains(i + 1);
                    Assert.AreEqual(Has(tl, "count.press.s" + (i + 1)), pressed, d.id + " s" + (i + 1));
                    if (d.stages[i].propellant == "solid-apcp") Assert.IsFalse(pressed, d.id + ": a solid stage has no tank press");
                    if (pressed) press++;
                }
                foreach (int n in s.SecoStages)
                {
                    Assert.IsTrue(n >= 2 && n <= d.stages.Count, d.id + " seco s" + n);
                    Assert.IsTrue(Has(tl, "stg.ses.s" + n + ".1"), d.id + ": a SECO only on a restartable upper stage");
                    if (n == d.stages.Count) seco++;
                }
                Assert.AreEqual(Has(tl, "asc.karman"), s.HasKarman, d.id);
                Assert.AreEqual(Has(tl, "rec.entry"), s.HasEntry, d.id);
                if (s.HasEntry)
                {
                    Assert.IsFalse(double.IsNaN(s.Entry), d.id);
                    if (NarrationFacts.RecoveryOf(d) != "none") entry++;
                }
                if (s.HasKarman) karman++;
            }
            Assert.Greater(press, 0);
            Assert.Greater(seco, 0, "some shipped design inserts with a top-stage SECO");
            Assert.Greater(karman, 0);
            Assert.Greater(entry, 0, "some shipped design flies a booster entry burn");
        }

        [Test]
        public void TankPressBeats_ReachTheTriggers_InEveryCountMode()
        {
            foreach (var d in Designs())
            foreach (var mode in new[] { "kid", "standard", "engineer" })
            {
                var tl = Generate(d, mode);
                var s = CallSchedule.From(tl);
                var seq = LaunchSequence.Open(tl);
                var handoff = new LiftoffHandoff();
                handoff.Enter(seq);
                var t = new CallTriggers(s, d.stages.Count, d.StrapOnCount > 0);
                var ids = new List<string>();
                var heard = new List<string>();
                double now = 0;
                while (handoff.Counting && !seq.Liftoff && now < 120)
                {
                    ids.Clear();
                    foreach (var b in seq.Fired) ids.Add(b.Id);
                    t.CountTick(seq.Time, ids, now);
                    // A line takes 3 s here; a pre-launch call only starts into silence.
                    while (t.Next(now, false, false, out var cue)) { heard.Add(cue.EventId + "." + cue.LitStage); t.Offered(cue); Assert.Greater(cue.MaxSec, 0); }
                    seq.Step(0.1);
                    now += 0.1;
                }
                Assert.IsTrue(heard.Contains("pad.0"), d.id + " " + mode);
                foreach (int n in s.PressStages) Assert.IsTrue(heard.Contains("tankpress." + n), d.id + " " + mode + ": pressed s" + n + " reaches the trigger");
                foreach (var h in heard)
                    if (h.StartsWith("tankpress.")) Assert.IsTrue(s.PressStages.Contains(int.Parse(h.Substring(10))), d.id + " " + h);
            }
        }

        [Test]
        public void EveryTriggerEvent_HasOnlyTrueTakes_AndPreLaunchTakesFitTheOpenCount()
        {
            foreach (var path in Manifests())
            {
                var cat = NarrationCatalog.Parse(File.ReadAllText(path));
                foreach (var d in Designs())
                {
                    var s = CallSchedule.From(Generate(d));
                    var facts = NarrationFacts.From(d, null);
                    foreach (var line in cat.Eligible(CallTriggers.Pad, facts))
                        Assert.LessOrEqual(line.Duration, -LiftoffHandoff.OpenAt - CallTriggers.LiftoffMarginSec, path + " " + line.Resource);
                    foreach (int n in s.PressStages)
                        foreach (var line in cat.Eligible(CallTriggers.TankPress, facts.WithLit(n)))
                            Assert.LessOrEqual(line.Duration, -LiftoffHandoff.OpenAt - CallTriggers.LiftoffMarginSec, path + " " + line.Resource);
                    if (!s.HasEntry) continue;
                    // Entry fires only with a picked landing; then a true take exists. Without one nothing is true (and nothing fires).
                    int entryTakes = cat.Eligible(CallTriggers.Entry, facts).Count;
                    if (NarrationFacts.RecoveryOf(d) != "none") Assert.Greater(entryTakes, 0, path + " " + d.id + ": an entry burn with no true take");
                    else Assert.AreEqual(0, entryTakes, path + " " + d.id + ": no landing picked, no entry take is true");
                }
            }
        }

        static IEnumerable<string> Manifests()
        {
            yield return Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Audio", "Runtime", "Resources", "Narration", "manifest.json");
            string alt = System.Environment.GetEnvironmentVariable("KESTREL_NARRATION_MANIFEST");
            if (!string.IsNullOrEmpty(alt) && File.Exists(alt)) yield return alt;
        }
    }
}
