using System;
using System.Collections.Generic;
using Kestrel.Core.Timeline;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>
    /// Where the two-voice "color" exchanges may be fired (docs/narration-webcast.md section 3). A color clip
    /// is 6 to 10 s long and is never allowed to sit on top of a call the player has to hear. An unknown
    /// time is NaN, and every comparison with NaN is false, so unknown means no.
    /// </summary>
    public static class ColorWindows
    {
        public const double PadHoldMinSec = 20;
        public const double AscentMecoMinSec = 12;
        public const double CoastQuietSec = 15;
        public const double KarmanAltM = 100000;
        /// <summary>How long after the 100 km crossing color.karman may still start (the line is about the crossing).</summary>
        public const double KarmanGraceSec = 3;

        public const string Pad = "color.pad", Ascent = "color.ascent", Karman = "color.karman", Coast = "color.coast", Recovery = "color.recovery";

        public static bool IsColorEvent(string id) => id != null && id.StartsWith("color.", StringComparison.Ordinal);

        /// <summary>color.pad: only during a hold, with more than 20 s left before T-minus 60 s.</summary>
        public static bool PadOk(bool inHold, double secToTMinus60) => inHold && secToTMinus60 > PadHoldMinSec;

        /// <summary>color.ascent: only after max-Q and with more than 12 s to MECO.</summary>
        public static bool AscentOk(bool pastMaxQ, double secToMeco) => pastMaxQ && secToMeco > AscentMecoMinSec;

        /// <summary>color.karman: the flight crossed 100 km on this step.</summary>
        public static bool KarmanCrossed(double prevAltM, double altM) => prevAltM < KarmanAltM && altM >= KarmanAltM;

        /// <summary>color.coast: only when no call is expected in the next 15 s.</summary>
        public static bool CoastOk(double secToNextCall) => secToNextCall > CoastQuietSec;

        /// <summary>color.recovery: drop it when the next call is under the clip length away.</summary>
        public static bool RecoveryFits(double secToNextCall, double clipSec) => secToNextCall >= clipSec;
    }

    /// <summary>
    /// When the calls the player must hear are expected, from the generated timeline (nominal seconds from
    /// liftoff). Anything with a narration link counts as a call except aborts; being too careful here only
    /// means a color exchange is skipped.
    /// </summary>
    public sealed class CallSchedule
    {
        public static readonly CallSchedule Empty = new CallSchedule();

        readonly List<double> calls = new List<double>();
        public double Meco = double.NaN;
        public double Boostback = double.NaN;
        public double Entry = double.NaN;

        public int Count => calls.Count;

        public void AddCall(double t) { calls.Add(t); calls.Sort(); }

        /// <summary>Seconds from <paramref name="t"/> to the next call strictly after it. +Infinity when none is left.</summary>
        public double SecondsToNextCall(double t)
        {
            for (int i = 0; i < calls.Count; i++)
                if (calls[i] > t) return calls[i] - t;
            return double.PositiveInfinity;
        }

        public static CallSchedule From(Kestrel.Core.Timeline.Timeline tl)
        {
            var s = new CallSchedule();
            if (tl == null || tl.events == null) return s;
            for (int i = 0; i < tl.events.Count; i++)
            {
                var e = tl.events[i];
                if (e == null) continue;
                if (e.id == "asc.meco") s.Meco = e.nominal;
                else if (e.id == "asc.burnout" && double.IsNaN(s.Meco)) s.Meco = e.nominal;
                else if (e.id == "rec.boostback") s.Boostback = e.nominal;
                else if (e.id == "rec.entry") s.Entry = e.nominal;
                var ids = e.links != null ? e.links.narration : null;
                if (ids == null) continue;
                for (int k = 0; k < ids.Count; k++)
                {
                    string id = ids[k];
                    if (string.IsNullOrEmpty(id) || id.StartsWith("abort", StringComparison.Ordinal) || ColorWindows.IsColorEvent(id)) continue;
                    s.calls.Add(e.nominal);
                    break;
                }
            }
            s.calls.Sort();
            return s;
        }
    }

    /// <summary>A color event that is due and the time to the next call, for the clip-length check.</summary>
    public readonly struct ColorCue
    {
        public readonly string EventId;
        public readonly double SecToNextCall;
        public ColorCue(string eventId, double secToNextCall) { EventId = eventId; SecToNextCall = secToNextCall; }
    }

    /// <summary>
    /// Arms each color event once its safe window opens and offers it on every poll until the player confirms
    /// it played or the window is gone. Pure and clocked by the flight (sim seconds), so it is tested without a scene.
    /// color.pad is armed by <see cref="NoteHold"/> only: the live countdown has no hold today.
    /// </summary>
    public sealed class ColorDirector
    {
        public const double PollSec = 0.5;

        readonly HashSet<string> done = new HashSet<string>();
        readonly bool hasRecovery;
        CallSchedule calls = CallSchedule.Empty;
        bool pastMaxQ, mecoSeen;
        double prevAlt = double.NaN, karmanAt = double.NaN, nextPoll = double.NegativeInfinity;
        bool holdOpen; double secToT60 = double.NaN;

        /// <param name="recoveryPicked">The design chose a landing (barge or rtls) and has a booster to bring back.</param>
        public ColorDirector(bool recoveryPicked) { hasRecovery = recoveryPicked; }

        public void UseCalls(CallSchedule schedule) { calls = schedule ?? CallSchedule.Empty; }

        public void Reset()
        {
            done.Clear();
            pastMaxQ = mecoSeen = false;
            prevAlt = karmanAt = double.NaN;
            nextPoll = double.NegativeInfinity;
            holdOpen = false; secToT60 = double.NaN;
        }

        public void OnEvent(string id)
        {
            if (id == "maxq") pastMaxQ = true;
            else if (id == "meco") mecoSeen = true;
        }

        public void NoteHold(bool inHold, double secondsToTMinus60) { holdOpen = inHold; secToT60 = secondsToTMinus60; }

        public void MarkDone(string eventId) { if (eventId != null) done.Add(eventId); }

        /// <summary>Color events that may start now. The caller offers them in order; the first that plays ends the poll.</summary>
        public void Poll(double t, double altM, List<ColorCue> into)
        {
            into.Clear();
            if (!double.IsNaN(prevAlt) && ColorWindows.KarmanCrossed(prevAlt, altM)) karmanAt = t;
            prevAlt = altM;
            if (t < nextPoll) return;
            nextPoll = t + PollSec;
            double toNext = calls.SecondsToNextCall(t);
            if (!done.Contains(ColorWindows.Pad) && ColorWindows.PadOk(holdOpen, secToT60))
                into.Add(new ColorCue(ColorWindows.Pad, toNext));
            if (!done.Contains(ColorWindows.Ascent) && ColorWindows.AscentOk(pastMaxQ, calls.Meco - t))
                into.Add(new ColorCue(ColorWindows.Ascent, toNext));
            if (!done.Contains(ColorWindows.Karman) && !double.IsNaN(karmanAt) && t - karmanAt <= ColorWindows.KarmanGraceSec)
                into.Add(new ColorCue(ColorWindows.Karman, toNext));
            if (!done.Contains(ColorWindows.Coast) && mecoSeen && ColorWindows.CoastOk(toNext))
                into.Add(new ColorCue(ColorWindows.Coast, toNext));
            if (hasRecovery && !done.Contains(ColorWindows.Recovery) && mecoSeen && RecoveryAnchor(t))
                into.Add(new ColorCue(ColorWindows.Recovery, toNext));
        }

        // The booster coast back: from the boostback burn (return to launch site) or 20 s after MECO (droneship
        // landing, which has an entry burn but no boostback), up to the entry burn. Nothing without an entry event.
        bool RecoveryAnchor(double t)
        {
            if (double.IsNaN(calls.Entry)) return false;
            double from = !double.IsNaN(calls.Boostback) ? calls.Boostback : calls.Meco + 20;
            return !double.IsNaN(from) && t >= from && t < calls.Entry;
        }
    }
}
