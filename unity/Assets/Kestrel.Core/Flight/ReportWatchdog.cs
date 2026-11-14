// Failsafe so the post-flight report card always opens after the ascent ends.
// #482 budgeted Conclude and kept the climax moving while paused, but the card
// could still never appear: RecoveryShow.Present (the only Conclude caller) is
// skipped when LateUpdate returns early (HUD hidden, some pause paths), and
// Conclude itself refuses a lost ascent and an open player landing burn, so
// RecoveryLog.Active can hold FailureHost forever. A loss also folds the dock
// (FlightHudLayout.DockShown), which matches Trevor's TestFlight 218/219
// screenshot (2026-11-09): lone stage, sky, no HUD, no card or buttons.
//
// #488 armed the watchdog, but FailureHost still had two holes that left the
// same sky / lone-stage softlock: (1) the failure-playback branch returned
// before TryRelease, and (2) reportedAlready counted latch.Reported / reported
// while cardPending still waited on RecoveryLog.Active, so the host's own
// watchdog never Released. Only an on-screen card counts as reported.
namespace Kestrel.Core.Flight
{
    public sealed class ReportWatchdog
    {
        /// <summary>
        /// Unscaled seconds recovery may still hold the card after the ascent is over.
        /// Long enough for a budgeted Conclude to finish; short enough that a stuck
        /// booster, a lost ascent, or a Present that never runs cannot freeze the flight.
        /// </summary>
        public const float ReleaseAfterSeconds = 8f;

        /// <summary>
        /// PlayMode only: skip RecoveryShow.Present so a stuck <see cref="RecoveryLog.Active"/>
        /// can be reproduced without Conclude clearing it. Nothing in play sets this.
        /// </summary>
        public static bool BlockRecoveryPresent;

        double armedAt = double.NaN;

        public void Reset() => armedAt = double.NaN;

        /// <summary>
        /// True when recovery has held the report long enough after the ascent ended.
        /// <paramref name="cardUp"/> must mean the results card is actually on screen —
        /// not that a latch fired or a failure show began — or a cardPending wait on
        /// RecoveryLog.Active never Releases.
        /// </summary>
        public bool ShouldRelease(bool ascentOver, bool recovering, bool cardUp, double unscaledTime)
        {
            if (cardUp || !ascentOver || !recovering)
            {
                armedAt = double.NaN;
                return false;
            }
            if (double.IsNaN(armedAt)) armedAt = unscaledTime;
            return unscaledTime - armedAt >= ReleaseAfterSeconds;
        }

        /// <summary>Release a stuck log and reset the timer. True when it fired this call.</summary>
        public bool TryRelease(bool ascentOver, bool recovering, bool cardUp, double unscaledTime)
        {
            if (!ShouldRelease(ascentOver, recovering, cardUp, unscaledTime)) return false;
            if (RecoveryLog.Active) RecoveryLog.Release();
            Reset();
            return true;
        }
    }
}
