using NUnit.Framework;

namespace Kestrel.Game.Audio.Tests
{
    public class VoicePoolTests
    {
        [Test]
        public void CapStealsOldestLowerPriorityAndRejectsAWeakerVoice()
        {
            var pool = new VoicePool(2);
            int stopped = 0;
            var a = pool.Acquire("ui", 1);
            a.Stop = () => stopped++;
            var b = pool.Acquire("hit", 1);
            var c = pool.Acquire("liftoff", 2);
            Assert.IsNotNull(c);
            Assert.AreEqual(1, stopped, "oldest low-priority stolen");
            Assert.AreEqual(1, pool.Stolen);
            Assert.AreSame(a, c, "stolen slot is reused");
            Assert.IsNull(pool.Acquire("ui", 0), "lower priority cannot steal");
            Assert.AreEqual(1, pool.Rejected);
            pool.Release(b);
            pool.Release(c);
            Assert.AreEqual(0, pool.Size);
            var e = pool.Acquire("x", 0);
            Assert.IsTrue(e == a || e == b || e == c, "slot objects reused");
        }

        [Test]
        public void RetriggerLockBlocksUntilTheWindowPasses()
        {
            var gate = new SfxGate();
            Assert.IsTrue(gate.Allow("ui", 0.28f, 1f));
            Assert.IsFalse(gate.Allow("ui", 0.28f, 1.1f));
            Assert.IsTrue(gate.Allow("hit", 0.4f, 1.1f));
            Assert.IsTrue(gate.Allow("ui", 0.28f, 1.30f));
            gate.Reset();
            Assert.IsTrue(gate.Allow("ui", 0.28f, 1.1f));
        }
    }
}
