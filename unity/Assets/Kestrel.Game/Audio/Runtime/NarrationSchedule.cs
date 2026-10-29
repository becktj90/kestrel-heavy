using System;
using System.Collections.Generic;
using Kestrel.Core.Flight.Narration;

namespace Kestrel.Game.Audio
{
    /// <summary>What <see cref="NarrationSchedule.Offer"/> did with an event.</summary>
    public enum NarrationOutcome
    {
        /// <summary>Nothing plays: muted, no true take, or a color exchange that had no safe moment.</summary>
        Dropped,
        /// <summary>Start <see cref="NarrationOffer.Resource"/> now.</summary>
        Started,
        /// <summary>A call waits for the one that is playing.</summary>
        Queued,
    }

    public readonly struct NarrationOffer
    {
        public readonly NarrationOutcome Outcome;
        public readonly string Resource;
        /// <summary>True when the clip that was playing was a color exchange and must fade out over <see cref="NarrationSchedule.FadeSeconds"/>.</summary>
        public readonly bool FadeOutPrevious;
        public readonly bool IsColor;
        public readonly string Reason;

        public NarrationOffer(NarrationOutcome outcome, string resource, bool fade, bool color, string reason)
        {
            Outcome = outcome; Resource = resource; FadeOutPrevious = fade; IsColor = color; Reason = reason;
        }

        public bool Started => Outcome == NarrationOutcome.Started;
    }

    /// <summary>
    /// Picks the next take for an event id. One line plays at a time; a later line waits.
    /// Abort lines replace whatever is playing. flight.begin clears the cursors.
    /// Two priority classes. A Call (mission control and every existing event) plays or waits, and a call that
    /// arrives over a playing Color exchange cuts it with a 120 ms fade. A Color exchange never waits: it starts
    /// only when nothing is playing or pending and the last line ended at least 1.5 s ago, else it is dropped.
    /// </summary>
    public sealed class NarrationSchedule
    {
        public const double FadeSeconds = 0.12;
        public const double ColorGapSeconds = 1.5;

        readonly Dictionary<string, int> cursors = new Dictionary<string, int>();
        double lastEnd = double.NegativeInfinity;

        public bool Busy { get; private set; }
        public string Current { get; private set; }
        public string Pending { get; private set; }
        public bool CurrentIsColor { get; private set; }

        /// <summary>
        /// Legacy facts-blind path: walks every take of the event in file order, true or not. The live player does
        /// not use it (it uses <see cref="Offer"/>); it stays for the schedule's first tests.
        /// </summary>
        public string Push(NarrationCatalog catalog, string id, bool muted)
        {
            if (catalog == null || string.IsNullOrEmpty(id) || muted) return null;
            if (!catalog.TryLine(id, Peek(id), out var line)) return null;
            Bump(id);
            if (Busy && !id.StartsWith("abort"))
            {
                Pending = line.Resource;
                return null;
            }
            Pending = null;
            Current = line.Resource;
            CurrentIsColor = false;
            Busy = true;
            return Current;
        }

        /// <summary>
        /// Offer an event with the rocket's facts. Only takes that are true for the facts can play, most specific
        /// first, then round-robin per event and lit stage.
        /// </summary>
        /// <param name="litStage">The core stage this event lit (0 for none): it adds the lit.* facts and separates cursors.</param>
        /// <param name="now">Seconds on any steady clock (unscaled time); used for the gap after a line.</param>
        /// <param name="accept">Last check on a color take before it starts (its length against the next call). Null accepts.</param>
        public NarrationOffer Offer(NarrationCatalog catalog, string id, NarrationFacts facts, int litStage,
            CommentaryMode mode, double now, Func<NarrationLine, bool> accept = null)
        {
            if (catalog == null || string.IsNullOrEmpty(id)) return Dropped("no event");
            if (!Commentary.AllowsCalls(mode)) return Dropped("commentary off");
            var ctx = facts != null && litStage > 0 ? facts.WithLit(litStage) : facts;
            var lines = catalog.Eligible(id, ctx);
            if (lines.Count == 0) return Dropped("no true take");
            string key = id + "|" + litStage;
            int cursor = Peek(key);
            var line = lines[cursor % lines.Count];
            bool color = line.IsColor || ColorWindows.IsColorEvent(id);
            if (color)
            {
                if (!Commentary.AllowsColor(mode)) return Dropped("calls only");
                if (Busy || !string.IsNullOrEmpty(Pending)) return Dropped("busy");
                if (now - lastEnd < ColorGapSeconds) return Dropped("gap");
                if (accept != null && !accept(line)) return Dropped("no safe moment");
                Bump(key);
                Current = line.Resource;
                CurrentIsColor = true;
                Busy = true;
                return new NarrationOffer(NarrationOutcome.Started, Current, false, true, null);
            }
            Bump(key);
            if (Busy && !id.StartsWith("abort") && !CurrentIsColor)
            {
                Pending = line.Resource;
                return new NarrationOffer(NarrationOutcome.Queued, null, false, false, null);
            }
            // Idle, an abort, or a call over a color exchange: plays now. A color that was playing fades.
            bool fade = Busy && CurrentIsColor;
            Pending = null;
            Current = line.Resource;
            CurrentIsColor = false;
            Busy = true;
            return new NarrationOffer(NarrationOutcome.Started, Current, fade, false, null);
        }

        /// <summary>The line that was waiting, or null. Clears the busy flag when nothing is queued.</summary>
        public string Complete() => Complete(double.NegativeInfinity);

        /// <summary>As <see cref="Complete()"/>, noting when the line ended (for the 1.5 s gap before a color exchange).</summary>
        public string Complete(double now)
        {
            Busy = false;
            Current = null;
            CurrentIsColor = false;
            lastEnd = now;
            if (string.IsNullOrEmpty(Pending)) return null;
            Current = Pending;
            Pending = null;
            Busy = true;
            return Current;
        }

        public void Release()
        {
            Busy = false;
            Current = null;
            Pending = null;
            CurrentIsColor = false;
            lastEnd = double.NegativeInfinity;
        }

        public void Reset()
        {
            Release();
            cursors.Clear();
        }

        static NarrationOffer Dropped(string why) => new NarrationOffer(NarrationOutcome.Dropped, null, false, false, why);

        int Peek(string id) => cursors.TryGetValue(id, out var i) ? i : 0;

        void Bump(string id)
        {
            cursors.TryGetValue(id, out var i);
            cursors[id] = i + 1;
        }
    }
}
