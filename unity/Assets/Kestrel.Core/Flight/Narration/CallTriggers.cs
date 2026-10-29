using System;
using System.Collections.Generic;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>A call trigger that is due: the manifest event, the core stage it is about (0 for none), and the longest take that may start.</summary>
    public readonly struct TriggerCue
    {
        public readonly string EventId;
        public readonly int LitStage;
        /// <summary>Longest take, in seconds, that may start now. +Infinity: no limit. Before liftoff: the line must end before T-0.</summary>
        public readonly double MaxSec;
        public readonly bool PreLaunch;
        public TriggerCue(string eventId, int litStage, double maxSec, bool preLaunch)
        {
            EventId = eventId; LitStage = litStage; MaxSec = maxSec; PreLaunch = preLaunch;
        }
    }

    /// <summary>
    /// The calls no sim event names: pad, tank press, SECO, Karman line and the booster entry burn. Each is due only
    /// when its event really happens on this flight, for this design (the generated timeline says the design flies it,
    /// see <see cref="CallSchedule"/>, and the live count or flight says it happened now), and each fires once.
    /// A due call never steps on another call: it is held while a call is waiting, never cuts one that is playing
    /// (calls queue, as every call does), and before liftoff it starts only into silence and only with a take that
    /// ends before T-0. A call that cannot go out while it is still about now is dropped. Pure, so it is tested
    /// without a scene; clocks are the caller's (count seconds, flight seconds, and a steady clock for freshness).
    /// </summary>
    public sealed class CallTriggers
    {
        public const string Pad = "pad", TankPress = "tankpress", Seco = "seco", Karman = "karman", Entry = "entry";
        /// <summary>A call later than this after its event is no longer about now and is dropped.</summary>
        public const double FreshSec = 4;
        /// <summary>A take before liftoff must end this long before T-0 (flight.begin stops the voice).</summary>
        public const double LiftoffMarginSec = 0.5;

        sealed class Due
        {
            public string Id;
            public int Lit;
            public double At;
            public bool PreLaunch;
        }

        readonly CallSchedule calls;
        readonly int stages;
        readonly bool strapOns;
        readonly List<Due> due = new List<Due>();
        readonly HashSet<string> seen = new HashSet<string>();
        bool counting, launched, topCutoff;
        double countT = double.NaN, prevAlt = double.NaN;
        int beatsSeen;

        /// <param name="schedule">The calls of this design's generated timeline (what it really flies).</param>
        /// <param name="coreStages">Core stage count of the flown design.</param>
        /// <param name="strapOnGroup">The sim stack starts with the strap-on group (its stage numbers are one ahead of the core's).</param>
        public CallTriggers(CallSchedule schedule, int coreStages, bool strapOnGroup)
        {
            calls = schedule ?? CallSchedule.Empty;
            stages = coreStages;
            strapOns = strapOnGroup;
        }

        public void Reset()
        {
            due.Clear();
            seen.Clear();
            counting = launched = topCutoff = false;
            countT = prevAlt = double.NaN;
            beatsSeen = 0;
        }

        /// <summary>Ids due now, in order. For tests and the debug overlay.</summary>
        public IEnumerable<string> DueIds { get { foreach (var d in due) yield return d.Id; } }

        // ---- the count (before liftoff) ----

        /// <summary>
        /// The terminal count is on screen at <paramref name="countSeconds"/> (negative). The first call makes "pad"
        /// due (the count is armed and running), and every beat that has fired so far is read for its tank press.
        /// </summary>
        public void CountTick(double countSeconds, IReadOnlyList<string> firedBeatIds, double now)
        {
            if (launched || double.IsNaN(countSeconds) || countSeconds >= 0) return;
            countT = countSeconds;
            if (!counting)
            {
                counting = true;
                Add(Pad, 0, now, true);
            }
            if (firedBeatIds == null) return;
            for (; beatsSeen < firedBeatIds.Count; beatsSeen++)
            {
                string id = firedBeatIds[beatsSeen];
                // count.press.s<n>: the timeline presses core stage n here (strap-on presses, count.press.so, are not a core stage's call).
                if (id == null || !id.StartsWith("count.press.s", StringComparison.Ordinal)) continue;
                if (!int.TryParse(id.Substring("count.press.s".Length), out int stage)) continue;
                if (calls.PressStages.Contains(stage)) Add(TankPress, stage, now, true);
            }
        }

        /// <summary>The vehicle left the pad (or the count was skipped). A pre-launch call that did not start is gone.</summary>
        public void Liftoff()
        {
            launched = true;
            counting = false;
            due.RemoveAll(d => d.PreLaunch);
        }

        // ---- the flight ----

        /// <summary>
        /// A sim event. SECO is due on insertion ("orbit") after the top core stage's guided cutoff, and only when the
        /// design's top stage really ends its burn in a SECO (a restartable engine). A burnout (ran dry) is not a SECO.
        /// </summary>
        public void OnSimEvent(string type, int simStage, double now)
        {
            if (string.IsNullOrEmpty(type)) return;
            if (type == "liftoff" || type == "ignition") { if (!launched) Liftoff(); return; }
            if (type == "cutoff")
            {
                int core = strapOns ? simStage - 1 : simStage;
                topCutoff = stages >= 2 && core == stages && calls.SecoStages.Contains(core);
            }
            else if (type == "orbit")
            {
                if (topCutoff) Add(Seco, stages, now, false);
            }
            else if (type == "failure" || type == "impact") topCutoff = false;
        }

        /// <summary>The flight's altitude. The Karman call is due on the step that crosses 100 km, on a design whose timeline crosses it.</summary>
        public void OnAltitude(double altM, double now)
        {
            if (!double.IsNaN(prevAlt) && calls.HasKarman && ColorWindows.KarmanCrossed(prevAlt, altM)) Add(Karman, 0, now, false);
            prevAlt = altM;
        }

        /// <summary>
        /// The booster's live phase marks on the flight clock (<see cref="RecoveryLog.FlightMarks"/>) and the rail's
        /// "now" (<see cref="RecoveryLog.RailClock"/>). Entry is due when an entry-burn mark appears while it is still
        /// current. A mark first seen stale (the booster was run to the end at once when the ascent finished) is not
        /// happening now and never plays.
        /// </summary>
        public void OnRecovery(RecoveryMark[] flightMarks, double railNow, double now)
        {
            if (!calls.HasEntry || flightMarks == null || seen.Contains(Entry)) return;
            for (int i = 0; i < flightMarks.Length; i++)
            {
                if (flightMarks[i].Phase != RecoveryPhase.Entry) continue;
                double age = railNow - flightMarks[i].T;
                if (double.IsNaN(age) || age < -1e-6) return;
                if (age <= FreshSec) Add(Entry, 0, now, false);
                else seen.Add(Entry);
                return;
            }
        }

        // ---- offering ----

        /// <summary>
        /// The next call that may be offered now, or false. <paramref name="busy"/>: a line is playing.
        /// <paramref name="callPending"/>: a call is waiting for it (the schedule holds one). Expired calls are dropped here.
        /// </summary>
        public bool Next(double now, bool busy, bool callPending, out TriggerCue cue)
        {
            cue = default;
            due.RemoveAll(d => !d.PreLaunch && now - d.At > FreshSec);
            if (counting && !double.IsNaN(countT))
                due.RemoveAll(d => d.PreLaunch && -countT - LiftoffMarginSec <= 0);
            if (due.Count == 0 || callPending) return false;
            var first = due[0];
            if (first.PreLaunch)
            {
                if (busy || !counting || double.IsNaN(countT)) return false;
                cue = new TriggerCue(first.Id, first.Lit, -countT - LiftoffMarginSec, true);
                return true;
            }
            cue = new TriggerCue(first.Id, first.Lit, double.PositiveInfinity, false);
            return true;
        }

        /// <summary>The cue was offered (started, queued, or had no true take): it does not come back.</summary>
        public void Offered(in TriggerCue cue)
        {
            for (int i = 0; i < due.Count; i++)
            {
                if (due[i].Id != cue.EventId || due[i].Lit != cue.LitStage) continue;
                due.RemoveAt(i);
                return;
            }
        }

        void Add(string id, int lit, double now, bool preLaunch)
        {
            string key = id + "|" + lit;
            if (!seen.Add(key)) return;
            if (id == Entry) seen.Add(Entry);
            due.Add(new Due { Id = id, Lit = lit, At = now, PreLaunch = preLaunch });
        }
    }
}
