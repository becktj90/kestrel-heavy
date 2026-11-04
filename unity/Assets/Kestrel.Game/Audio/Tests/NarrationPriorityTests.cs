// Two priority classes on one narration voice: calls (mission control) and color. Self-contained: the manifest is built here,
// so the same file runs in Unity EditMode and in the dotnet harness (Kestrel.Core.Tests links it).
using System.Collections.Generic;
using Kestrel.Core.Flight.Narration;
using NUnit.Framework;

namespace Kestrel.Game.Audio.Tests
{
    public class NarrationPriorityTests
    {
        const string Manifest = @"{ ""clips"": [
  { ""id"": ""mc/liftoff.1"", ""event"": ""liftoff"", ""file"": ""mc/liftoff.1.mp3"", ""channel"": ""mc"", ""durationSec"": 2.0, ""neutral"": true },
  { ""id"": ""mc/maxq.1"",    ""event"": ""maxq"",    ""file"": ""mc/maxq.1.mp3"",    ""channel"": ""mc"", ""durationSec"": 2.0, ""neutral"": true },
  { ""id"": ""mc/abort-fuel.1"", ""event"": ""abort-fuel"", ""file"": ""mc/abort-fuel.1.mp3"", ""channel"": ""mc"", ""durationSec"": 2.0, ""neutral"": true },
  { ""id"": ""mc/meco.1"",   ""event"": ""meco"",   ""file"": ""mc/meco.1.mp3"",   ""channel"": ""mc"", ""durationSec"": 2.0, ""requires"": { ""stages"": "">=2"" }, ""claims"": [""stages""] },
  { ""id"": ""mc/ses1.1"",   ""event"": ""ses1"",   ""file"": ""mc/ses1.1.mp3"",   ""channel"": ""mc"", ""durationSec"": 2.0, ""requires"": { ""lit.stage"": 2 }, ""claims"": [""lit.stage""] },
  { ""id"": ""mc/ses1.2"",   ""event"": ""ses1"",   ""file"": ""mc/ses1.2.mp3"",   ""channel"": ""mc"", ""durationSec"": 2.0, ""requires"": { ""lit.stage"": 3 }, ""claims"": [""lit.stage""] },
  { ""id"": ""host/color.ascent.1"", ""event"": ""color.ascent"", ""file"": ""host/color.ascent.1.mp3"", ""channel"": ""host"", ""durationSec"": 6.0, ""layer"": ""color"", ""interruptible"": true, ""neutral"": true },
  { ""id"": ""host/color.ascent.2"", ""event"": ""color.ascent"", ""file"": ""host/color.ascent.2.mp3"", ""channel"": ""host"", ""durationSec"": 6.0, ""layer"": ""color"", ""interruptible"": true, ""neutral"": true },
  { ""id"": ""host/color.coast.1"",  ""event"": ""color.coast"",  ""file"": ""host/color.coast.1.mp3"",  ""channel"": ""host"", ""durationSec"": 6.0, ""layer"": ""color"", ""interruptible"": true, ""requires"": { ""stages"": "">=3"" }, ""claims"": [""stages""] }
] }";

        static NarrationCatalog Cat() => NarrationCatalog.Parse(Manifest);
        static NarrationFacts Facts(int stages) => new NarrationFacts().With("stages", stages);

        static readonly NarrationCatalog Shared = Cat();

        static NarrationOffer Offer(NarrationSchedule s, string id, double now, NarrationFacts f = null, int lit = 0,
            CommentaryMode mode = CommentaryMode.Full, System.Func<NarrationLine, bool> accept = null)
            => s.Offer(Shared, id, f ?? Facts(2), lit, mode, now, accept);

        [Test]
        public void ACallOverAPlayingColorCutsItWithA120msFade()
        {
            var s = new NarrationSchedule();
            var color = Offer(s, "color.ascent", 10);
            Assert.AreEqual(NarrationOutcome.Started, color.Outcome);
            Assert.IsTrue(color.IsColor);
            Assert.IsTrue(s.CurrentIsColor);
            var call = Offer(s, "maxq", 11);
            Assert.AreEqual(NarrationOutcome.Started, call.Outcome, "a call does not wait behind color");
            Assert.IsTrue(call.FadeOutPrevious);
            Assert.IsFalse(call.IsColor);
            Assert.IsFalse(s.CurrentIsColor);
            Assert.AreEqual(0.12, NarrationSchedule.FadeSeconds, 1e-9);
            StringAssert.Contains("maxq", call.Resource);
            Assert.IsNull(s.Pending);
        }

        [Test]
        public void AnAbortCutsAColorToo_AndACallOverACallJustQueues()
        {
            var s = new NarrationSchedule();
            Offer(s, "color.ascent", 10);
            var abort = Offer(s, "abort-fuel", 11, lit: 1);
            Assert.AreEqual(NarrationOutcome.Started, abort.Outcome);
            Assert.IsTrue(abort.FadeOutPrevious);

            var t = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Started, Offer(t, "liftoff", 0).Outcome);
            var second = Offer(t, "maxq", 1);
            Assert.AreEqual(NarrationOutcome.Queued, second.Outcome);
            Assert.IsFalse(second.FadeOutPrevious, "nothing to cut: it waits");
            var cut = Offer(t, "abort-fuel", 2, lit: 1);
            Assert.AreEqual(NarrationOutcome.Started, cut.Outcome);
            Assert.IsFalse(cut.FadeOutPrevious, "a call is not a color: no fade");
            Assert.IsNull(t.Pending, "an abort drops the waiting line");
        }

        [Test]
        public void ColorIsDroppedWhileAnythingPlaysOrWaits()
        {
            var s = new NarrationSchedule();
            Offer(s, "liftoff", 0);
            var dropped = Offer(s, "color.ascent", 10);
            Assert.AreEqual(NarrationOutcome.Dropped, dropped.Outcome);
            Assert.AreEqual("busy", dropped.Reason);

            Offer(s, "maxq", 10.5);
            Assert.IsNotNull(s.Pending);
            var promoted = s.Complete(11);
            Assert.IsNotNull(promoted, "the waiting call starts");
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(s, "color.ascent", 12).Outcome);
        }

        [Test]
        public void ColorNeverTakesThePlaceOfAPendingCall()
        {
            var s = new NarrationSchedule();
            Offer(s, "liftoff", 0);
            Offer(s, "maxq", 1);
            string pending = s.Pending;
            Assert.IsNotNull(pending);
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(s, "color.ascent", 2).Outcome);
            Assert.AreEqual(pending, s.Pending);
            Assert.AreEqual(pending, s.Complete(3), "the call still plays when the first ends");
        }

        [Test]
        public void ColorWaitsOneAndAHalfSecondsAfterAnyLine()
        {
            var s = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Started, Offer(s, "color.ascent", 0).Outcome, "the first line has no gap to wait out");
            s.Complete(10);
            var gap = Offer(s, "color.ascent", 11.4);
            Assert.AreEqual(NarrationOutcome.Dropped, gap.Outcome);
            Assert.AreEqual("gap", gap.Reason);
            Assert.AreEqual(NarrationOutcome.Started, Offer(s, "color.ascent", 11.5).Outcome);
            Assert.AreEqual(1.5, NarrationSchedule.ColorGapSeconds, 1e-9);

            var after = new NarrationSchedule();
            Offer(after, "liftoff", 0);
            after.Complete(20);
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(after, "color.ascent", 20.5).Outcome, "the gap follows a call too");
            Assert.AreEqual(NarrationOutcome.Started, Offer(after, "color.ascent", 21.6).Outcome);
        }

        [Test]
        public void CallsOnlyKeepsCallsAndDropsColor_OffDropsBoth()
        {
            var s = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(s, "color.ascent", 0, mode: CommentaryMode.CallsOnly).Outcome);
            Assert.AreEqual(NarrationOutcome.Started, Offer(s, "liftoff", 0, mode: CommentaryMode.CallsOnly).Outcome);
            var off = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(off, "liftoff", 0, mode: CommentaryMode.Off).Outcome);
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(off, "abort-fuel", 0, lit: 1, mode: CommentaryMode.Off).Outcome);
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(off, "color.ascent", 0, mode: CommentaryMode.Off).Outcome);
            Assert.IsFalse(off.Busy);
        }

        [Test]
        public void ARejectedColorTakeIsDroppedAndDoesNotUseUpItsTurn()
        {
            var s = new NarrationSchedule();
            var no = Offer(s, "color.ascent", 0, accept: l => false);
            Assert.AreEqual(NarrationOutcome.Dropped, no.Outcome);
            Assert.IsFalse(s.Busy);
            var yes = Offer(s, "color.ascent", 1);
            Assert.AreEqual("Narration/host/color.ascent.1", yes.Resource, "the same take is tried again");
            s.Complete(10);
            var next = Offer(s, "color.ascent", 20);
            Assert.AreEqual("Narration/host/color.ascent.2", next.Resource, "then the next one");
        }

        [Test]
        public void ColorOnlyPlaysTakesThatAreTrueForTheRocket()
        {
            var s = new NarrationSchedule();
            var two = Offer(s, "color.coast", 0, Facts(2));
            Assert.AreEqual(NarrationOutcome.Dropped, two.Outcome, "that take is about a third stage");
            Assert.AreEqual("no true take", two.Reason);
            var three = Offer(s, "color.coast", 0, Facts(3));
            Assert.AreEqual(NarrationOutcome.Started, three.Outcome);
            var blind = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Dropped, blind.Offer(Shared, "color.coast", null, 0, CommentaryMode.Full, 0).Outcome, "no facts, no tagged take");
        }

        [Test]
        public void CallsNeedFactsToo_AndNeutralTakesAlwaysPlay()
        {
            var s = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Dropped, Offer(s, "meco", 0, Facts(1)).Outcome, "a one-stage rocket has no stage meco");
            Assert.AreEqual(NarrationOutcome.Started, Offer(s, "meco", 0, Facts(2)).Outcome);
            var blind = new NarrationSchedule();
            Assert.AreEqual(NarrationOutcome.Started, blind.Offer(Shared, "liftoff", null, 0, CommentaryMode.Full, 0).Outcome, "neutral plays with no facts");
            Assert.AreEqual(NarrationOutcome.Dropped, blind.Offer(Shared, "nope", Facts(2), 0, CommentaryMode.Full, 0).Outcome);
        }

        [Test]
        public void LitStageSelectsTheTake_AndEachStageHasItsOwnCursor()
        {
            var s = new NarrationSchedule();
            var two = Offer(s, "ses1", 0, Facts(3), 2);
            StringAssert.EndsWith("ses1.1", two.Resource);
            s.Complete(5);
            var three = Offer(s, "ses1", 6, Facts(3), 3);
            StringAssert.EndsWith("ses1.2", three.Resource);
            s.Complete(10);
            var stage4 = Offer(s, "ses1", 11, Facts(3), 4);
            Assert.AreEqual(NarrationOutcome.Dropped, stage4.Outcome, "a stage the rocket does not have is not lit");
        }

        [Test]
        public void ResetClearsTheCursorsAndTheGap()
        {
            var s = new NarrationSchedule();
            Offer(s, "color.ascent", 0);
            s.Complete(1);
            s.Reset();
            var again = Offer(s, "color.ascent", 1.1);
            Assert.AreEqual(NarrationOutcome.Started, again.Outcome, "flight.begin clears the gap");
            StringAssert.EndsWith("color.ascent.1", again.Resource);
        }

        [Test]
        public void CatalogKeepsTheLayerOfEachClip()
        {
            var cat = Cat();
            Assert.AreEqual(9, cat.ClipCount);
            int color = 0;
            foreach (var line in cat.All) if (line.IsColor) color++;
            Assert.AreEqual(3, color);
            Assert.IsTrue(cat.Eligible("color.ascent", Facts(2))[0].IsColor);
            Assert.AreEqual(2, cat.Eligible("color.ascent", null).Count);
            Assert.AreEqual(0, cat.Eligible("color.coast", Facts(2)).Count);
        }

        [Test]
        public void ACallThatDoesNotFitIsDropped_AndDoesNotStepOnThePlayingCall()
        {
            // Pre-launch triggers offer a call with a "must end before T-0" filter. No take fitting is a drop, not a queue.
            var cat = NarrationCatalog.Parse(@"{ ""clips"": [
  { ""id"": ""mc/pad.1"", ""event"": ""pad"", ""file"": ""mc/pad.1.mp3"", ""channel"": ""mc"", ""durationSec"": 9.0, ""neutral"": true },
  { ""id"": ""mc/pad.2"", ""event"": ""pad"", ""file"": ""mc/pad.2.mp3"", ""channel"": ""mc"", ""durationSec"": 4.0, ""neutral"": true },
  { ""id"": ""mc/ignition.1"", ""event"": ""ignition"", ""file"": ""mc/ignition.1.mp3"", ""channel"": ""mc"", ""durationSec"": 2.0, ""neutral"": true }
] }");
            var s = new NarrationSchedule();
            var none = s.Offer(cat, "pad", Facts(2), 0, CommentaryMode.Full, 0, line => line.Duration <= 0.1);
            Assert.AreEqual(NarrationOutcome.Dropped, none.Outcome);
            Assert.AreEqual("no room", none.Reason);
            Assert.IsFalse(s.Busy);
            var first = s.Offer(cat, "pad", Facts(2), 0, CommentaryMode.Full, 0, line => line.Duration <= 5);
            Assert.IsTrue(first.Started);
            StringAssert.Contains("pad.2", first.Resource, "the long take is skipped for the one that ends in time");
            var fresh = new NarrationSchedule();
            Assert.IsTrue(fresh.Offer(cat, "pad", Facts(2), 0, CommentaryMode.Full, 0, line => line.Duration <= 0.1).Outcome == NarrationOutcome.Dropped);
            var after = fresh.Offer(cat, "pad", Facts(2), 0, CommentaryMode.Full, 0);
            Assert.AreEqual(new NarrationSchedule().Offer(cat, "pad", Facts(2), 0, CommentaryMode.Full, 0).Resource, after.Resource,
                "a dropped offer does not move the cursor");
            var queued = s.Offer(cat, "ignition", Facts(2), 1, CommentaryMode.Full, 1);
            Assert.AreEqual(NarrationOutcome.Queued, queued.Outcome, "calls still wait for the playing call");
        }
    }
}
