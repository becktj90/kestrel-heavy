// Apply LCC verdicts to existing MissionEvent statuses (no new schema — DECISIONS #30).
// Hold → TimelineStatus.Held; Scrub → TimelineStatus.Failed on the holdable poll.
// KID/Relaxed: visual only — events are not mutated.
using System;
using Tev = Kestrel.Core.Timeline;

namespace Kestrel.Core.Weather
{
    public static class WeatherCommit
    {
        public const string DefaultPollEventId = "term.poll";

        /// <summary>
        /// Drive a holdable MissionEvent from an LCC result. Returns false if the event was left unchanged
        /// (Go, visual-only difficulty, or missing/non-holdable event).
        /// </summary>
        public static bool Apply(LccResult lcc, Tev.MissionEvent holdableEvent)
        {
            if (holdableEvent == null) throw new ArgumentNullException(nameof(holdableEvent));
            if (!lcc.commitAllowed || lcc.verdict == LccVerdict.Go) return false;
            if (!holdableEvent.holdable) return false;

            if (holdableEvent.status == null)
                holdableEvent.status = new Tev.EventStatus();

            if (lcc.verdict == LccVerdict.Scrub)
            {
                holdableEvent.status.state = Tev.TimelineStatus.Failed;
                holdableEvent.status.progress = 0;
            }
            else
            {
                holdableEvent.status.state = Tev.TimelineStatus.Held;
                holdableEvent.status.progress = 0;
            }
            return true;
        }

        /// <summary>Find <paramref name="eventId"/> on a timeline and apply LCC. No-op if missing.</summary>
        public static bool ApplyToTimeline(LccResult lcc, Tev.Timeline timeline, string eventId = DefaultPollEventId)
        {
            if (timeline?.events == null) return false;
            for (int i = 0; i < timeline.events.Count; i++)
            {
                var e = timeline.events[i];
                if (e != null && e.id == eventId)
                    return Apply(lcc, e);
            }
            return false;
        }
    }
}
