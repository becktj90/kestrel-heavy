using System.IO;
using NUnit.Framework;

namespace Kestrel.Game.Audio.Tests
{
    public class NarrationScheduleTests
    {
        const string Root = "Assets/Kestrel.Game/Audio/Runtime/Resources/Narration";

        static NarrationCatalog Load() => NarrationCatalog.Parse(File.ReadAllText(Root + "/manifest.json"));

        [Test]
        public void ManifestKeysClipsByEventId()
        {
            var cat = Load();
            Assert.AreEqual(167, cat.ClipCount);
            Assert.AreEqual(44, cat.EventCount);
            Assert.AreEqual(8, cat.Count("liftoff"));
            Assert.AreEqual(0, cat.Count("theme"));
            Assert.IsTrue(cat.TryLine("liftoff", 0, out var first));
            Assert.IsTrue(cat.TryLine("liftoff", 1, out var second));
            Assert.IsTrue(cat.TryLine("liftoff", 8, out var wrapped));
            Assert.AreNotEqual(first.Resource, second.Resource);
            Assert.AreEqual(first.Resource, wrapped.Resource);
            Assert.IsTrue(first.Resource.StartsWith("Narration/"));
            Assert.IsFalse(cat.TryLine("ui", 0, out _));
            foreach (var line in cat.All)
            {
                Assert.IsTrue(File.Exists(Root + "/" + line.Relative), line.Relative);
                Assert.Greater(line.Duration, 0.2f, line.Relative);
            }
        }

        [Test]
        public void ScheduleCyclesQueuesAndLetsAbortCutIn()
        {
            var cat = Load();
            var schedule = new NarrationSchedule();
            Assert.IsNull(schedule.Push(cat, "nope", false));
            Assert.IsNull(schedule.Push(cat, "liftoff", true), "muted plays nothing");
            var first = schedule.Push(cat, "liftoff", false);
            Assert.IsNull(schedule.Push(cat, "meco", false));
            Assert.AreEqual(first, schedule.Current);
            Assert.IsNotNull(schedule.Pending);
            var cut = schedule.Push(cat, "abort-fuel", false);
            Assert.IsNotNull(cut);
            Assert.IsTrue(cut.Contains("abort-fuel"));
            Assert.IsNull(schedule.Pending, "abort drops the waiting line");
            Assert.IsNull(schedule.Complete());
            schedule.Reset();
            Assert.AreEqual(first, schedule.Push(cat, "liftoff", false));
        }

        [Test]
        public void WaitingLinePlaysWhenTheCurrentOneEnds()
        {
            var cat = Load();
            var schedule = new NarrationSchedule();
            var first = schedule.Push(cat, "pad", false);
            Assert.IsNull(schedule.Push(cat, "terminal", false));
            var next = schedule.Complete();
            Assert.AreNotEqual(first, next);
            Assert.IsTrue(next.Contains("terminal"));
            Assert.IsNull(schedule.Complete());
            Assert.IsFalse(schedule.Busy);
        }
    }
}
