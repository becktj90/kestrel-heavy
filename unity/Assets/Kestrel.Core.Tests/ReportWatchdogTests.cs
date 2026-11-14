// P0 Trevor 2026-11-09 (TF 218/219, #482 incomplete): the report must open even
// when Conclude never runs, the ascent was lost, or an open landing burn is still
// holding RecoveryLog.Active. These cases fail before ReportWatchdog is armed
// (Active stays true past the deadline); they pass once TryRelease fires.
//
// Follow-up: FailureHost used to pass latch.Reported / reported as "card up" and
// skipped TryRelease entirely on the failure-playback branch, so a cardPending
// wait on RecoveryLog.Active never Released (mission-done softlock again).
using System.IO;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ReportWatchdogTests
    {
        [TearDown]
        public void Drop()
        {
            RecoveryLog.Clear();
            ReportWatchdog.BlockRecoveryPresent = false;
        }

        [Test]
        public void DoesNotReleaseWhileTheAscentIsStillFlying()
        {
            RecoveryLog.Engage();
            var dog = new ReportWatchdog();
            Assert.IsFalse(dog.ShouldRelease(false, true, false, ReportWatchdog.ReleaseAfterSeconds + 1));
            Assert.IsTrue(RecoveryLog.Active);
        }

        [Test]
        public void DoesNotReleaseBeforeTheDeadline()
        {
            RecoveryLog.Engage();
            var dog = new ReportWatchdog();
            Assert.IsFalse(dog.TryRelease(true, true, false, 0));
            Assert.IsFalse(dog.TryRelease(true, true, false, ReportWatchdog.ReleaseAfterSeconds - 0.01));
            Assert.IsTrue(RecoveryLog.Active, "still inside the window — Conclude can finish");
        }

        [Test]
        public void ReleasesAStuckRecoveryAfterTheAscentEnds()
        {
            RecoveryLog.Engage();
            var dog = new ReportWatchdog();
            dog.TryRelease(true, true, false, 0);
            Assert.IsTrue(dog.TryRelease(true, true, false, ReportWatchdog.ReleaseAfterSeconds));
            Assert.IsFalse(RecoveryLog.Active, "watchdog clears the report gate");
        }

        [Test]
        public void ReproducesTheFreezeWhenTheWatchdogDoesNotRun()
        {
            // Before the failsafe: Active stays up after insertion (Conclude skipped on a
            // lost ascent, or Present never called) and FailureHost never opens the card.
            RecoveryLog.Engage();
            for (double t = 0; t <= ReportWatchdog.ReleaseAfterSeconds + 20; t += 1)
                Assert.IsTrue(RecoveryLog.Active, "without TryRelease the card stays blocked at t=" + t);
        }

        [Test]
        public void ALostAscentOrOpenLandingBurnCannotHoldTheCardForever()
        {
            // Conclude(ascentLost: true) and an open planned landing burn both
            // return without Release. The watchdog still frees the report.
            RecoveryLog.Engage();
            var dog = new ReportWatchdog();
            Assert.IsFalse(dog.TryRelease(true, true, false, 0), "arms on the first over-frame");
            Assert.IsTrue(dog.TryRelease(true, true, false, ReportWatchdog.ReleaseAfterSeconds));
            Assert.IsFalse(RecoveryLog.Active);
        }

        [Test]
        public void StaysQuietOnceTheCardHasOpened()
        {
            RecoveryLog.Engage();
            var dog = new ReportWatchdog();
            Assert.IsFalse(dog.ShouldRelease(true, true, true, ReportWatchdog.ReleaseAfterSeconds + 5));
            Assert.IsTrue(RecoveryLog.Active, "a shown card is not the watchdog's to dismiss");
        }

        [Test]
        public void LatchReportedWithoutACardStillReleases()
        {
            // The hole after #488: FailureHost passed reported || latch.Reported as cardUp,
            // so a Begin() that left cardPending waiting on RecoveryLog.Active never Released.
            RecoveryLog.Engage();
            var dog = new ReportWatchdog();
            Assert.IsFalse(dog.TryRelease(true, true, false, 0), "arms — card is not up");
            Assert.IsTrue(dog.TryRelease(true, true, false, ReportWatchdog.ReleaseAfterSeconds));
            Assert.IsFalse(RecoveryLog.Active, "no on-screen card → watchdog still frees the gate");
        }

        [Test]
        public void FailureHostAndSliceAppArmTheWatchdog()
        {
            string root = Golden.Root;
            string host = File.ReadAllText(Path.Combine(root, "unity", "Assets", "Kestrel.Game", "Failure", "FailureHost.cs"));
            StringAssert.Contains("reportWatchdog.TryRelease(ascentOver, RecoveryLog.Active, cardUp, Time.unscaledTime)", host);
            StringAssert.Contains("bool cardUp = CardIsUp()", host);
            StringAssert.Contains("RetryShowCard()", host);
            // Failure playback retries ShowCard even after UndoOpen cleared the latch (Codex P2).
            StringAssert.Contains("else if (!cardUp && !RecoveryLog.Active)", host);
            // Watchdog must run even while the failure show plays (cardPending / RecoveryLog wait).
            int tryReleaseAt = host.IndexOf("reportWatchdog.TryRelease(ascentOver");
            int playedBranch = host.IndexOf("if (played)");
            Assert.Greater(tryReleaseAt, 0);
            Assert.Greater(playedBranch, tryReleaseAt, "TryRelease runs before the played early-return");
            string app = File.ReadAllText(Path.Combine(root, "unity", "Assets", "Kestrel.Slice", "Runtime", "SliceApp.cs"));
            StringAssert.Contains("TickReportFailsafe()", app);
            StringAssert.Contains("PresentRecoveryIfAscentOver()", app);
            string driver = File.ReadAllText(Path.Combine(root, "unity", "Assets", "Kestrel.Slice", "Runtime", "FlightDriver.cs"));
            StringAssert.Contains("public void EnsureReportReady(double unscaledTime)", driver);
        }
    }
}
