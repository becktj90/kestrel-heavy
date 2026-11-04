// When the post-flight card (or a failure replay) may open, and the guard that every route back to the pad
// (Fix it, Back to designer) really lands in the designer.
//
// Fix it rebuilt the rocket on the pad and attached a new FailureHost while the FlightDriver still held the last
// result. The fresh host had nothing to compare against, so it reported that result again: a miss reopened the card
// (and charged the hardware again on every tap), and a break-up replayed on the pad with the designer hidden
// (the WebGL "Fix it -> empty sky for 35 s" soft-lock, UX fun review 2026-11-03 #1).
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class ReportGate
    {
        /// <summary>The result a freshly attached host must ignore: whatever a stopped driver still holds.
        /// A running driver has no result yet (Launch clears it), so nothing is ignored.</summary>
        public static SimResult Stale(SimResult onDriver, bool running) => running ? null : onDriver;

        /// <summary>True when a host that has <paramref name="reported"/> (or not) and last closed
        /// <paramref name="closed"/> must open the card or the failure replay for <paramref name="result"/>.</summary>
        public static bool Opens(SimResult result, bool reported, SimResult closed)
            => result != null && !reported && !ReferenceEquals(result, closed);
    }

    /// <summary>
    /// Armed by a route back to the pad. If after <see cref="GraceSeconds"/> neither the designer nor a new flight is
    /// on screen (or the report is still up), the caller forces the designer. Any throw in the route lands there too.
    /// </summary>
    public sealed class DesignerLanding
    {
        /// <summary>UX review: "if no screen root is visible 1 s after a route change, return to the designer".</summary>
        public const float GraceSeconds = 1f;

        float armedAt = -1f;

        public bool Armed => armedAt >= 0f;

        public void Arm(float now) => armedAt = now < 0f ? 0f : now;

        public void Clear() => armedAt = -1f;

        /// <summary>True once the route has landed: the designer shows with no report over it, or a new flight began.</summary>
        public static bool Landed(bool designerShown, bool reportShown, bool flightShown)
            => flightShown || (designerShown && !reportShown);

        /// <summary>One frame. Judged once, at the end of the grace period, so a card that comes back a frame after
        /// the route (the old bug) is still caught. True means "force the designer now". Either way it disarms then.
        /// A new flight disarms at once (the player launched again).</summary>
        public bool Check(float now, bool designerShown, bool reportShown, bool flightShown)
        {
            if (armedAt < 0f) return false;
            if (flightShown) { armedAt = -1f; return false; }
            if (now - armedAt < GraceSeconds) return false;
            armedAt = -1f;
            return !Landed(designerShown, reportShown, flightShown);
        }
    }
}
