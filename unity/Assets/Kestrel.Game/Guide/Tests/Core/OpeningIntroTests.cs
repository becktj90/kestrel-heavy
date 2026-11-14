using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Guide;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class OpeningIntroTests
    {
        string root;
        string json;

        [SetUp]
        public void SetUp()
        {
            root = Repo();
            json = File.ReadAllText(Path.Combine(root, "data", "guide.json"));
        }

        [Test]
        public void Copy_IsGroundedInKh1AndTheTwentyMissionBoard()
        {
            var script = GuideScript.Load(root);
            StringAssert.Contains("I am Thrum", script.ById("first.1").Cards[0].Kid);
            StringAssert.Contains("KH-1", OpeningIntroSequence.CtaKid(script));
            StringAssert.Contains("LAUNCH", OpeningIntroSequence.CtaKid(script));
            StringAssert.Contains("Your first rocket is ready", OpeningIntroSequence.CloseKid(script));
            StringAssert.Contains("Twenty flights", OpeningIntroSequence.HostKid());
            StringAssert.Contains("far hold", OpeningIntroSequence.HostKid());
            StringAssert.Contains("budget", OpeningIntroSequence.CohostKid());
            StringAssert.Contains("First hop", OpeningIntroSequence.SendoffKid());
            var engine = Engine(false);
            GuideHooks.Home(engine);
            // Host and co-host beats are several cards. HostKid/CohostKid are the first card only.
            StringAssert.Contains("First hop", engine.Pending[1].Cards[1].Kid);
            StringAssert.Contains("booster", engine.Pending[2].Cards[1].Kid);
            Assert.AreEqual(6, OpeningIntroSequence.BeatIds.Length);
        }

        [Test]
        public void WelcomeQueuesSixBeatsOnceEach()
        {
            var engine = Engine(false);
            GuideHooks.Home(engine);
            Assert.AreEqual(6, engine.Pending.Count);
            Assert.AreEqual("first.1", engine.Pending[0].Id);
            Assert.AreEqual(GuideSpeaker.Thrum, engine.Pending[0].Speaker);
            Assert.AreEqual("first.host", engine.Pending[1].Id);
            Assert.AreEqual(GuideSpeaker.Host, engine.Pending[1].Speaker);
            Assert.AreEqual("first.cohost", engine.Pending[2].Id);
            Assert.AreEqual(GuideSpeaker.Cohost, engine.Pending[2].Speaker);
            Assert.AreEqual("first.close", engine.Pending[3].Id);
            Assert.AreEqual(GuideSpeaker.Thrum, engine.Pending[3].Speaker);
            Assert.AreEqual("first.sendoff", engine.Pending[4].Id);
            Assert.AreEqual(GuideSpeaker.Host, engine.Pending[4].Speaker);
            Assert.AreEqual("first.cta", engine.Pending[5].Id);
            Assert.IsTrue(engine.Pending[5].Decision);
        }

        [Test]
        public void SkipWelcomeMarksEveryBeatAndSetsFirstRunDone()
        {
            var engine = Engine(false);
            GuideHooks.Home(engine);
            engine.SkipWelcome();
            Assert.IsTrue(engine.State.FirstRunDone);
            for (int i = 0; i < OpeningIntroSequence.BeatIds.Length; i++)
                Assert.IsTrue(engine.State.HasSeen(OpeningIntroSequence.BeatIds[i]));
            GuideHooks.Home(engine);
            Assert.AreEqual(0, engine.Pending.Count);
        }

        [Test]
        public void CompletingEachBeatFinishesTheWelcomeOnce()
        {
            var engine = Engine(false);
            GuideHooks.Home(engine);
            foreach (var id in OpeningIntroSequence.BeatIds) engine.Complete(id);
            Assert.IsTrue(engine.State.FirstRunDone);
        }

        static LiveGuideEngine Engine(bool returning)
        {
            var state = GuideState.Fresh(false, returning);
            return new LiveGuideEngine(File.ReadAllText(Path.Combine(Repo(), "data", "guide.json")), state, Repo());
        }

        static string Repo()
        {
            string[] starts = { Directory.GetCurrentDirectory(), System.AppContext.BaseDirectory };
            for (int i = 0; i < starts.Length; i++)
            {
                try { return Catalog.FindRepoRoot(starts[i]); }
                catch (DirectoryNotFoundException) { }
            }
            throw new DirectoryNotFoundException("data/guide.json");
        }
    }
}