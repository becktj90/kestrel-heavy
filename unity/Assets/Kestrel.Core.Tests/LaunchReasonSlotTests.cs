// Fun review item 13: the Hangar reason line must not grow and shrink the sheet as a tweak crosses a limit.
using Kestrel.Core.Designer;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LaunchReasonSlotTests
    {
        [Test]
        public void ReservesTwoLinesBeforeAnyReasonShows()
        {
            var slot = new LaunchReasonSlot();
            Assert.AreEqual(2 * 14f * LaunchReasonSlot.LineH, slot.Reserve(14f), 0.001f);
            // engineer/119: a one-line reason (~23 px at 14 px) now fits in the room already there.
            Assert.That(slot.Reserve(14f), Is.GreaterThanOrEqualTo(23f));
        }

        [Test]
        public void HoldsTheTallestReasonSoTheSheetDoesNotShrinkBack()
        {
            var slot = new LaunchReasonSlot();
            Assert.IsFalse(slot.Saw(14f, 20f), "a short reason fits the base");
            Assert.IsTrue(slot.Saw(14f, 57f), "engineer/252: a three-line reason grows the slot once");
            Assert.AreEqual(57f, slot.Reserve(14f), 0.001f);
            Assert.IsFalse(slot.Saw(14f, 23f));
            Assert.AreEqual(57f, slot.Reserve(14f), 0.001f, "clearing the warning keeps the room");
        }

        [Test]
        public void AFontChangeStartsOver()
        {
            var slot = new LaunchReasonSlot();
            slot.Saw(14f, 80f);
            Assert.AreEqual(LaunchReasonSlot.Base(18f), slot.Reserve(18f), 0.001f);
        }

        [Test]
        public void IgnoresBadHeights()
        {
            var slot = new LaunchReasonSlot();
            Assert.IsFalse(slot.Saw(14f, float.NaN));
            Assert.IsFalse(slot.Saw(14f, -3f));
            Assert.AreEqual(LaunchReasonSlot.Base(14f), slot.Reserve(14f), 0.001f);
        }
    }
}
