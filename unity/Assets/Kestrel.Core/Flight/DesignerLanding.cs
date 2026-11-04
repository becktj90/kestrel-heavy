// The guard that every route back to the pad (Fix it, Back to designer) really lands in the designer.
// UX fun review 2026-11-03 #1 / QA #417: Fix it after a break-up left an empty sky with no buttons. The stale-result
// re-report itself is fixed by ReportLatch (#419); this is the fallback for anything else that strands the player.
namespace Kestrel.Core.Flight
{
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
