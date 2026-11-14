// P0 (QA #417, kid 040-041, engineer 104-109): the first tap on the results card was swallowed.
// "Back to designer" rebuilt the rocket, the new FailureHost saw the finished result still on the
// flight driver, and re-opened the card. The latch opens a card exactly once per result.
using System.IO;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ReportLatchTests
    {
        [Test]
        public void AFinishedPassOpensItsCardOnce()
        {
            var latch = new ReportLatch();
            latch.Arm(null);
            var result = new object();
            Assert.IsFalse(latch.TryOpen(result, false, true, false), "still flying");
            Assert.IsFalse(latch.TryOpen(result, false, false, true), "recovery still running");
            Assert.IsTrue(latch.TryOpen(result, false, false, false));
            Assert.IsFalse(latch.TryOpen(result, false, false, false), "no second card for the same flight");
        }

        [Test]
        public void AFailureOpensAtOnce()
        {
            var latch = new ReportLatch();
            latch.Arm(null);
            Assert.IsTrue(latch.TryOpen(new object(), true, true, false));
        }

        [Test]
        public void ARebuiltRocketDoesNotReopenTheCardTheTapJustClosed()
        {
            var result = new object();
            var shown = new ReportLatch();
            shown.Arm(null);
            Assert.IsTrue(shown.TryOpen(result, false, false, false));
            // Back to designer: BuildRocket attaches a fresh host bound to the same driver.
            var rebuilt = new ReportLatch();
            rebuilt.Arm(result);
            for (int frame = 0; frame < 3; frame++)
                Assert.IsFalse(rebuilt.TryOpen(result, false, false, false), "frame " + frame);
            // The next launch replaces the result, and that one reports.
            var next = new object();
            Assert.IsTrue(rebuilt.TryOpen(next, false, false, false));
        }

        [Test]
        public void ARetryResetKeepsTheOldResultClosed()
        {
            var result = new object();
            var latch = new ReportLatch();
            latch.Arm(null);
            Assert.IsTrue(latch.TryOpen(result, true, false, false));
            latch.Arm(result);
            Assert.IsFalse(latch.Reported);
            Assert.IsFalse(latch.TryOpen(result, true, false, false));
        }

        [Test]
        public void UndoOpen_LetsTheSameResultRetry_WhenShowCardNeverLanded()
        {
            var result = new object();
            var latch = new ReportLatch();
            latch.Arm(null);
            Assert.IsTrue(latch.TryOpen(result, false, false, false));
            Assert.IsTrue(latch.Reported);
            latch.UndoOpen();
            Assert.IsFalse(latch.Reported);
            Assert.IsTrue(latch.TryOpen(result, false, false, false), "same result can open after a failed ShowCard");
            latch.UndoOpen();
            latch.UndoOpen(); // no-op when already clear
            Assert.IsFalse(latch.Reported);
        }

        [Test]
        public void FailureHostRoutesEveryReportThroughTheLatch()
        {
            string host = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Failure", "FailureHost.cs"));
            StringAssert.Contains("latch.Arm(flight != null ? flight.Result : null);", host);
            StringAssert.Contains("latch.TryOpen(result, failed, flight.Running, RecoveryLog.Active)", host);
            StringAssert.Contains("latch.UndoOpen()", host);
            StringAssert.Contains("ShowCardOrUndo()", host);
            Assert.AreEqual(2, System.Text.RegularExpressions.Regex.Matches(host, @"latch\.Arm\(").Count, "Bind and ResetFlight both arm");
        }
    }
}
