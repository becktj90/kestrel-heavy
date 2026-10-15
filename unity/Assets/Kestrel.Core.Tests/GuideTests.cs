// Guide engine: the Thrum script, bus binding, priority queue, seen lines, and settings.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Guide;
using Kestrel.Core.Save;
using Kestrel.Core.Timeline;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class GuideTests
    {
        static readonly string[] Ids = {
            "first.1", "first.2", "first.3", "first.4", "first.5",
            "brief.kh1", "brief.kh3", "brief.kh6", "brief.kh8", "brief.kh9", "brief.contract", "brief.bonus", "brief.wind",
            "step.1", "step.2", "step.2.pass", "step.2.fail.twr", "step.2.fail.mix",
            "step.3", "step.3.fail.dv", "step.3.warn.maxq",
            "step.4", "step.4.fail.fit", "step.4.fail.tumble",
            "step.5", "step.5.skip", "step.5.fail.reserve", "step.6", "step.7",
            "pre.green", "pre.maxq", "pre.twr", "pre.stability", "pre.overg", "pre.cryo", "pre.sep", "pre.leg", "pre.odds", "pre.red.confirm",
            "asc.liftoff", "asc.pitch", "asc.maxq.warn", "asc.maxq.perfect", "asc.maxq.late", "asc.meco",
            "asc.stage.window", "asc.stage.good", "asc.stage.late", "asc.fairing",
            "asc.engineout", "asc.engineout.ok", "asc.engineout.bad", "asc.shear", "asc.fuel", "asc.seco", "asc.orbit",
            "asc.deploy", "asc.landing", "asc.touch.perfect", "asc.auto",
            "win.1", "win.margin", "win.budget", "win.unlock", "win.next", "win.final",
            "fail.structural-maxq", "fail.overg", "fail.tumble", "fail.engine-overpressure", "fail.bad-mixture",
            "fail.thermal", "fail.cryo-material", "fail.prop-depletion", "fail.separation-collision", "fail.leg-collapse",
            "fail.engine-out", "fail.miss", "fail.payload", "fail.landing.splash", "fail.landing.tip", "fail.retry", "fail.repeat",
            "medal.bronze", "medal.silver", "medal.gold", "medal.star1", "medal.star2", "medal.star3",
            "medal.perfect.timing", "medal.bonus", "medal.cosmetic",
        };

        static readonly string[] FailureKinds = {
            "structural-maxq", "overg", "tumble", "engine-overpressure", "bad-mixture", "thermal",
            "cryo-material", "prop-depletion", "separation-collision", "leg-collapse", "engine-out",
        };

        [Test]
        public void Script_LoadsEveryLineWithAKidLineAndANumber()
        {
            var script = GuideScript.Load(Golden.Root);
            Assert.AreEqual("Mabel Thrum Pike", script.Character.Name);
            Assert.AreEqual("Thrum", script.Character.Call);
            Assert.AreEqual("Dolores", script.Character.Wrench);
            Assert.AreEqual("Sprocket", script.Character.Kestrel);
            Assert.AreEqual(Ids.Length, script.Lines.Count);
            for (int i = 0; i < Ids.Length; i++)
            {
                var line = script.Lines[i];
                Assert.AreEqual(Ids[i], line.Id);
                Assert.AreSame(line, script.ById(line.Id));
                Assert.AreEqual("thrum", line.Speaker);
                Assert.Greater(GuidePriority.Rank(line.Priority), 0, line.Id);
                Assert.Greater(line.Cards.Count, 0, line.Id);
                for (int c = 0; c < line.Cards.Count; c++)
                {
                    var card = line.Cards[c];
                    Assert.IsFalse(string.IsNullOrEmpty(card.Kid), card.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(card.Eng), card.Id);
                    Assert.LessOrEqual(card.Kid.Length, GuideScript.MaxCardChars, card.Id);
                    Assert.LessOrEqual(card.Eng.Length, GuideScript.MaxCardChars, card.Id);
                    Assert.LessOrEqual(GuideScript.MaxWords(card.Kid), GuideScript.MaxWordsPerSentence, card.Id + " kid");
                    Assert.LessOrEqual(GuideScript.MaxWords(card.Eng), GuideScript.MaxWordsPerSentence, card.Id + " eng");
                    Assert.IsTrue(HasDigit(card.Eng), card.Id + " eng " + card.Eng);
                    Assert.IsFalse(card.Kid.ToLowerInvariant().Contains("you failed"), card.Id);
                    Assert.IsFalse(card.Kid.ToLowerInvariant().Contains("you lost"), card.Id);
                    Assert.IsFalse(card.Eng.ToLowerInvariant().Contains("you failed"), card.Id);
                }
            }
            Assert.IsNull(script.ById("missing"));
            Assert.IsTrue(script.ById("first.1").Cards[0].Kid.Contains("I am Thrum"));
            Assert.IsTrue(script.ById("first.2").Cards[0].Kid.Contains("Dolores"));
            Assert.IsTrue(script.ById("step.2").Cards[0].Eng.Contains("9.80665"));
            Assert.IsTrue(script.ById("step.2").Cards[0].Eng.Contains("1.30"));
            Assert.IsTrue(script.ById("brief.kh3").Cards[0].Eng.Contains("7.8"));
            Assert.IsTrue(script.ById("brief.kh3").Cards[0].Eng.Contains("1.5"));
            Assert.IsTrue(script.ById("brief.kh8").Cards[0].Eng.Contains("4.5"));
            Assert.IsTrue(script.ById("brief.kh8").Cards[0].Eng.Contains("0.98"));
            Assert.IsTrue(script.ById("brief.wind").Cards[0].Eng.Contains("12 m/s"));
            Assert.IsTrue(script.ById("step.4.fail.tumble").Cards[0].Kid.Contains("up"));
            Assert.IsTrue(script.ById("step.4.fail.tumble").Cards[0].Eng.Contains("0.5"));
            Assert.IsTrue(script.ById("step.4.fail.tumble").Cards[0].Eng.Contains("15"));
            Assert.IsTrue(script.ById("pre.red.confirm").Actions[0] == "Launch anyway");
            Assert.AreEqual(2, script.ById("asc.maxq.warn").Actions.Count);
        }

        [Test]
        public void Parse_RejectsBlameAMissingNumberAndALongCard()
        {
            Assert.Throws<FormatException>(() => GuideScript.Parse(Wrap(Card("you failed the hop.", "TWR is under 1.00."))));
            Assert.Throws<FormatException>(() => GuideScript.Parse(Wrap(Card("Try the hop again.", "No number in this engineer line."))));
            Assert.Throws<FormatException>(() => GuideScript.Parse(Wrap(Card(new string('a', 91) + ".", "TWR is 1.30."))));
            Assert.Throws<FormatException>(() => GuideScript.Parse(Wrap(Card("Oh! Wow! Too many.", "TWR is 1.30."))));
        }

        [Test]
        public void Binder_MapsBusIdsMissionEventsAndFailures()
        {
            var binder = Fresh();
            Assert.AreEqual("asc.liftoff", binder.OnBus("liftoff", null, 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("asc.maxq.warn", binder.OnBus("maxq", "warn", 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("asc.maxq.perfect", binder.OnBus("maxq", "perfect", 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("asc.stage.window", binder.OnBus("sep", null, 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("asc.stage.good", binder.OnBus("sep-clear", null, 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("fail.landing.splash", binder.OnBus("splash", null, 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("fail.landing.tip", binder.OnBus("tip", null, 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("fail.structural-maxq", binder.OnBus("abort-structural-maxq", null, 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("fail.miss", binder.OnBus("flight.end", "miss", 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("brief.kh1", binder.Offer("guide.brief", "m1", 0, null).LineId);
            binder.Queue.Clear();
            Assert.AreEqual("step.2.fail.twr", binder.Offer("guide.step", "2.fail.twr", 0, null).LineId);

            for (int i = 0; i < FailureKinds.Length; i++)
            {
                binder.Queue.Clear();
                var face = binder.OnFailure(FailureKinds[i], 0, null);
                Assert.AreEqual("fail." + FailureKinds[i], face.LineId, FailureKinds[i]);
            }

            binder.Queue.Clear();
            var ev = new MissionEvent { links = new EventLinks() };
            ev.links.narration.Add("meco");
            Assert.AreEqual("asc.meco", binder.OnMissionEvent(ev, null, 0, null).LineId);

            binder.Queue.Clear();
            Assert.IsNull(binder.OnBus("maxq", "nope", 0, null));
            Assert.IsNull(binder.OnMissionEvent(null, null, 0, null));
        }

        [Test]
        public void Queue_DecisionDropsChatterAndSafetyOutranksADecision()
        {
            var script = GuideScript.Load(Golden.Root);
            var queue = new GuideQueue { Ascent = true };
            queue.Settings.Speed = GuideSpeed.Instant;
            queue.Push(script.ById("asc.liftoff"), 0, null, null);
            queue.Push(script.ById("asc.pitch"), 0, null, null);
            Assert.AreEqual("asc.liftoff", queue.CurrentId);
            Assert.AreEqual(1, queue.Pending);
            queue.Push(script.ById("asc.maxq.warn"), 1, null, null);
            Assert.AreEqual("asc.maxq.warn", queue.CurrentId);
            Assert.AreEqual(0, queue.Pending);
            queue.Push(script.ById("asc.seco"), 1, null, null);
            Assert.AreEqual(0, queue.Pending, "open decision drops chatter");

            queue.Clear();
            queue.Push(script.ById("asc.maxq.warn"), 0, null, null);
            queue.Push(script.ById("asc.shear"), 1, null, null);
            Assert.AreEqual("asc.shear", queue.CurrentId);
            Assert.AreEqual(1, queue.Pending);
            queue.Skip(1);
            Assert.AreEqual("asc.maxq.warn", queue.CurrentId);
        }

        [Test]
        public void Queue_AscentAdvancesItselfAndADecisionWaitsForATap()
        {
            var binder = Fresh();
            binder.Queue.Ascent = true;
            binder.Queue.Settings.Speed = GuideSpeed.Instant;
            var face = binder.Offer("liftoff", null, 0, null);
            double due = GuideQueue.ReadingSeconds(face.Full);
            Assert.GreaterOrEqual(due, GuideQueue.AscentMinSeconds);
            Assert.AreEqual("asc.liftoff", binder.Tick(due - 0.05).LineId);
            Assert.IsNull(binder.Tick(due));

            binder = Fresh();
            binder.Queue.Ascent = true;
            binder.Queue.Settings.Speed = GuideSpeed.Instant;
            Assert.AreEqual("asc.maxq.warn", binder.Offer("maxq", "warn", 0, null).LineId);
            Assert.AreEqual("asc.maxq.warn", binder.Tick(100).LineId);
            Assert.AreEqual("asc.maxq.warn", binder.Tap(0.2).LineId);
            Assert.IsNull(binder.Tap(GuideQueue.AscentMinSeconds));
        }

        [Test]
        public void Queue_DesignerWaitsAndTheFirstTapFinishesTheLine()
        {
            var binder = Fresh();
            binder.Queue.Ascent = false;
            binder.Queue.Settings.Speed = GuideSpeed.Normal;
            var face = binder.Offer("guide.step", "7", 0, null);
            Assert.IsTrue(face.TextShown);
            Assert.AreEqual(0, face.Visible.Length);
            Assert.IsFalse(face.Complete);
            Assert.Greater(face.Full.Length, 20);
            Assert.AreEqual("step.7", binder.Tick(100).LineId);
            var done = binder.Tap(0.1);
            Assert.IsTrue(done.Complete);
            Assert.AreEqual(done.Full, done.Visible);
            Assert.IsNull(binder.Tap(0.2));

            var queue = new GuideQueue();
            queue.Settings.ReduceMotion = true;
            var whole = queue.Push(binder.Script.ById("step.7"), 0, null, null);
            Assert.IsTrue(whole.Complete);
            Assert.AreEqual(whole.Full, whole.Visible);
            Assert.AreEqual(0, queue.Settings.CharsPerSecond);
        }

        [Test]
        public void Queue_SkipDropsTheRestOfTheBeat()
        {
            var binder = Fresh();
            binder.Queue.Settings.Speed = GuideSpeed.Instant;
            binder.Offer("guide.first", "2", 0, null);
            binder.Offer("guide.first", "3", 0, null);
            Assert.AreEqual("first.2", binder.Queue.CurrentId);
            Assert.AreEqual(0, binder.Queue.Face(0).CardIndex);
            Assert.Greater(binder.Queue.Face(0).CardCount, 1);
            Assert.AreEqual("first.3", binder.Skip(0).LineId);
        }

        [Test]
        public void Queue_TypewriterFollowsTheSpeed()
        {
            var script = GuideScript.Load(Golden.Root);
            var queue = new GuideQueue();
            queue.Settings.Speed = GuideSpeed.Normal;
            queue.Push(script.ById("step.7"), 0, null, null);
            var face = queue.Face(0.5);
            Assert.AreEqual(Math.Min(face.Full.Length, 20), face.Visible.Length);
            Assert.AreEqual(GuideSettings.NormalCharsPerSecond, queue.Settings.CharsPerSecond);
            queue.Settings.Speed = GuideSpeed.Slow;
            Assert.AreEqual(GuideSettings.SlowCharsPerSecond, queue.Settings.CharsPerSecond);
            queue.Settings.Speed = GuideSpeed.Fast;
            Assert.AreEqual(GuideSettings.FastCharsPerSecond, queue.Settings.CharsPerSecond);
            queue.Settings.Speed = GuideSpeed.Instant;
            Assert.AreEqual(0, queue.Settings.CharsPerSecond);
            Assert.AreEqual(GuideSettings.MinPointSize, 15);
            Assert.AreEqual(2.5, GuideQueue.ReadingSeconds("short"));
        }

        [Test]
        public void Seen_ATipPlaysOnceAndASafetyLineRepeats()
        {
            var binder = Fresh();
            binder.Queue.Settings.Speed = GuideSpeed.Instant;
            Assert.AreEqual("first.1", binder.Offer("guide.first", "1", 0, null).LineId);
            Assert.IsTrue(binder.State.HasSeen("first.1"));
            binder.Skip(0);
            Assert.IsNull(binder.Offer("guide.first", "1", 1, null));

            binder.Offer("guide.pre", "maxq", 2, Tokens());
            binder.Skip(2);
            Assert.AreEqual("pre.maxq", binder.Offer("guide.pre", "maxq", 3, Tokens()).LineId);

            var script = GuideScript.Parse(Wrap(Card("Walls look thin today.", "q limit is 35 kPa.", "safety", "true")));
            var state = GuideState.Fresh(false, false);
            Assert.IsTrue(state.Allows(script.ById("t.1")));
            state.MarkSeen("t.1");
            Assert.IsTrue(state.Allows(script.ById("t.1")));
        }

        [Test]
        public void Settings_WarningsAndOffKeepDecisionsAndAlwaysKeepText()
        {
            var script = GuideScript.Load(Golden.Root);
            var warnings = GuideState.Fresh(false, false);
            warnings.Settings.Tips = GuideTips.Warnings;
            Assert.IsFalse(warnings.Allows(script.ById("asc.liftoff")));
            Assert.IsTrue(warnings.Allows(script.ById("fail.overg")));
            Assert.IsTrue(warnings.Allows(script.ById("pre.maxq")));
            Assert.IsTrue(warnings.Allows(script.ById("asc.maxq.warn")));

            var off = GuideState.Fresh(false, false);
            off.Settings.Tips = GuideTips.Off;
            Assert.IsFalse(off.Allows(script.ById("asc.liftoff")));
            Assert.IsFalse(off.Allows(script.ById("fail.overg")));
            Assert.IsTrue(off.Allows(script.ById("pre.maxq")));
            Assert.IsTrue(off.Allows(script.ById("pre.red.confirm")));
            Assert.IsTrue(off.Allows(script.ById("asc.maxq.warn")));

            var queue = new GuideQueue();
            queue.Settings.Speed = GuideSpeed.Instant;
            queue.Settings.VoiceOn = true;
            queue.Settings.VoiceAvailable = true;
            var talking = queue.Push(script.ById("asc.liftoff"), 0, null, "/clips/asc.liftoff.mp3");
            Assert.IsTrue(talking.TextShown);
            Assert.IsTrue(talking.VoiceOn);
            Assert.AreEqual("/clips/asc.liftoff.mp3", talking.VoiceClip);
            Assert.Greater(talking.Full.Length, 0);
            queue.Settings.VoiceOn = false;
            var quiet = queue.Face(0);
            Assert.IsTrue(quiet.TextShown);
            Assert.IsFalse(quiet.VoiceOn);
            Assert.IsNull(quiet.VoiceClip);
            Assert.AreEqual(talking.Full, quiet.Full);

            queue.Engineer = true;
            var both = queue.Face(0);
            Assert.IsTrue(both.Full.Contains(both.Kid));
            Assert.IsTrue(both.Full.Contains("2 s"));
            Assert.IsTrue(both.TextShown);
        }

        [Test]
        public void Settings_VoiceDefaultsOnOnlyWhenClipsExistAndTextStays()
        {
            Assert.IsTrue(GuideState.Fresh(true, false).Settings.VoiceOn);
            Assert.IsTrue(GuideState.Fresh(true, false).Settings.VoiceAvailable);
            Assert.IsFalse(GuideState.Fresh(false, false).Settings.VoiceOn);
            Assert.IsFalse(GuideState.Fresh(false, false).Settings.VoiceAvailable);
            Assert.IsTrue(GuideState.Parse("{\"firstRunDone\":false}", true).Settings.VoiceOn);
            Assert.IsFalse(GuideState.Parse("{\"voiceOn\":false}", true).Settings.VoiceOn);
            Assert.IsFalse(GuideVoice.ClipsPresent(Golden.Root));

            string root = TempDir();
            try
            {
                string dir = Path.Combine(root, "assets", "narration", "guide");
                Directory.CreateDirectory(dir);
                string clip = Path.Combine(dir, "asc.liftoff.mp3");
                File.WriteAllBytes(clip, new byte[] { 0 });
                Assert.IsTrue(GuideVoice.ClipsPresent(root));
                Assert.AreEqual(clip, GuideVoice.ClipFor(root, "asc.liftoff"));
                Assert.IsNull(GuideVoice.ClipFor(root, "asc.pitch"));

                var state = GuideState.Fresh(true, false);
                var binder = new GuideBinder(GuideScript.Load(Golden.Root), state, new GuideQueue(), root);
                binder.Queue.Settings.Speed = GuideSpeed.Instant;
                var face = binder.Offer("liftoff", null, 0, null);
                Assert.IsTrue(face.TextShown);
                Assert.AreEqual(clip, face.VoiceClip);
                Assert.Greater(face.Full.Length, 0);
                binder.Queue.Clear();
                var silent = binder.Offer("guide.asc", "pitch", 0, null);
                Assert.IsTrue(silent.TextShown);
                Assert.IsNull(silent.VoiceClip);
                Assert.IsTrue(silent.Full.Contains("pitch"));
            }
            finally { TryDelete(root); }
        }

        [Test]
        public void Settings_RoundTripAndACorruptFileKeepsABak()
        {
            string dir = TempDir();
            try
            {
                var state = GuideState.Fresh(true, false);
                state.FirstRunDone = true;
                state.MarkSeen("first.1");
                state.MarkSeen("pre.maxq");
                state.Settings.Tips = GuideTips.Warnings;
                state.Settings.Speed = GuideSpeed.Slow;
                state.Settings.Size = GuideSize.ExtraLarge;
                state.Settings.VoiceOn = false;
                state.Save(dir);

                var store = new NativeStore(dir, () => 0, () => 0.5);
                var env = store.Read(GuideState.SaveKey);
                Assert.AreEqual("guide", env.Str("kind"));
                Assert.AreEqual("warnings", env.Obj("data").Str("tipsOn"));

                var back = GuideState.Load(dir, true, false);
                Assert.IsTrue(back.FirstRunDone);
                Assert.IsTrue(back.HasSeen("first.1"));
                Assert.IsTrue(back.HasSeen("pre.maxq"));
                Assert.AreEqual(GuideTips.Warnings, back.Settings.Tips);
                Assert.AreEqual(GuideSpeed.Slow, back.Settings.Speed);
                Assert.AreEqual(GuideSize.ExtraLarge, back.Settings.Size);
                Assert.IsFalse(back.Settings.VoiceOn);
                Assert.IsTrue(back.Settings.VoiceAvailable);

                Assert.AreEqual(GuideTips.Off, GuideState.Parse("{\"tipsOn\":false}", false).Settings.Tips);
                Assert.AreEqual(GuideSpeed.Fast, GuideState.Parse("{\"subtitleSpeed\":70}", false).Settings.Speed);
                Assert.AreEqual(GuideSize.Large, GuideState.Parse("{\"subtitleSize\":\"large\"}", false).Settings.Size);

                string path = Path.Combine(dir, GuideState.SaveKey + ".json");
                File.WriteAllText(path, "{{{");
                var fallen = GuideState.Load(dir, false, true);
                Assert.IsTrue(fallen.FirstRunDone);
                Assert.IsFalse(fallen.Settings.VoiceOn);
                Assert.IsFalse(File.Exists(path));
                Assert.AreEqual("{{{", File.ReadAllText(path + ".bak"));
            }
            finally { TryDelete(dir); }
        }

        [Test]
        public void Settings_ExistingDesignsSkipAForcedWelcome()
        {
            Assert.IsFalse(GuideState.Fresh(false, false).FirstRunDone);
            Assert.IsTrue(GuideState.Fresh(false, true).FirstRunDone);
            string dir = TempDir();
            try
            {
                var fresh = GuideState.Load(dir, false, true);
                Assert.IsTrue(fresh.FirstRunDone);
                var tour = GuideState.Load(dir, false, false);
                Assert.IsFalse(tour.FirstRunDone);
            }
            finally { TryDelete(dir); }

            var binder = Fresh();
            binder.Queue.Settings.Speed = GuideSpeed.Instant;
            Assert.AreEqual("first.1", binder.Welcome(0).LineId);
            Assert.Greater(binder.Queue.Pending, 0);
            Assert.IsFalse(binder.State.HasSeen("first.5"));
            binder.SkipWelcome();
            Assert.IsTrue(binder.State.FirstRunDone);
            Assert.IsTrue(binder.State.HasSeen("first.5"));
            Assert.IsNull(binder.Queue.CurrentId);
            Assert.IsNull(binder.Welcome(1));
        }

        [Test]
        public void Tokens_FillLiveNumbersAndLeaveTheRealOnes()
        {
            var binder = Fresh();
            binder.Queue.Engineer = true;
            binder.Queue.Settings.Speed = GuideSpeed.Instant;
            var tokens = new Dictionary<string, string> { { "twr", "1.42" }, { "q", "48" }, { "qlim", "40" } };
            var face = binder.Offer("guide.step", "2.pass", 0, tokens);
            Assert.IsTrue(face.Kid.Contains("1.42"));
            Assert.IsTrue(face.Eng.Contains("1.42"));
            Assert.IsTrue(face.Eng.Contains("1.30"));
            Assert.IsTrue(face.Full.Contains("1.30"));
            Assert.IsTrue(face.TextShown);
            binder.Queue.Clear();
            var warn = binder.Offer("guide.pre", "maxq", 0, tokens);
            Assert.IsTrue(warn.Eng.Contains("48"));
            Assert.IsTrue(warn.Eng.Contains("40"));
            Assert.IsTrue(warn.Eng.Contains("1.4"));
            Assert.AreEqual("safety", warn.Priority);
        }

        static GuideBinder Fresh()
        {
            var state = GuideState.Fresh(false, false);
            return new GuideBinder(GuideScript.Load(Golden.Root), state, new GuideQueue(), Golden.Root);
        }

        static Dictionary<string, string> Tokens()
            => new Dictionary<string, string> { { "q", "48" }, { "qlim", "40" }, { "pct", "12" } };

        static bool HasDigit(string text)
        {
            for (int i = 0; i < text.Length; i++) if (text[i] >= '0' && text[i] <= '9') return true;
            return false;
        }

        static string Wrap(string line)
            => "{\"version\":1,\"speeds\":{\"slow\":25,\"normal\":40,\"fast\":70,\"instant\":0},"
            + "\"character\":{\"id\":\"mabel-thrum-pike\",\"name\":\"Mabel Thrum Pike\",\"call\":\"Thrum\",\"role\":\"Hangar Keeper\",\"wrench\":\"Dolores\",\"kestrel\":\"Sprocket\"},"
            + "\"lines\":[" + line + "]}";

        static string Card(string kid, string eng, string priority = "chatter", string once = "true")
            => "{\"id\":\"t.1\",\"trigger\":\"t\",\"when\":\"1\",\"speaker\":\"thrum\",\"priority\":\"" + priority + "\",\"once\":" + once
            + ",\"cards\":[{\"kid\":\"" + kid + "\",\"eng\":\"" + eng + "\"}]}";

        static string TempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-guide-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static void TryDelete(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            catch (IOException) { }
        }
    }
}
