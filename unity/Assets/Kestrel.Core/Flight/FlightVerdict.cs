// DECISIONS #77 (UX review #416 item 5): one verdict source. The designer and Starter badge, the count card's spare or
// short line, the in-flight speed verdict, the medal and the result screen all read whether the flight passes from here,
// so "Ready" never sits next to "89 m/s short", a doomed flight never shows a medal, and a pass never reads short.
// Before launch the source is the launch predictor's flight (ReferenceAscent, the live pad's director and checks) judged
// by the batch's own acceptance rule (LaunchPredictor.AcceptsMission); after the flight it is the flown result.
using System.Collections.Generic;

namespace Kestrel.Core.Flight
{
    public enum VerdictState { Checking, Pass, Fail }

    public sealed class FlightVerdict
    {
        public VerdictState State;
        /// <summary>The predictor's cause for a fail (StarterVerdict cause ids), or null.</summary>
        public string Cause;

        public bool Known => State != VerdictState.Checking;
        /// <summary>"Ready" is shown only when this is true: the predictor's flight passes the mission.</summary>
        public bool Passes => State == VerdictState.Pass;

        public static readonly FlightVerdict Checking = new FlightVerdict { State = VerdictState.Checking };
        public static FlightVerdict Pass() => new FlightVerdict { State = VerdictState.Pass };
        public static FlightVerdict Fail(string cause) => new FlightVerdict { State = VerdictState.Fail, Cause = cause };

        // ---- the remembered predictor verdict, by ReferenceAscent.CanonicalKey (the Starter job and the predictor write it) ----

        static readonly Dictionary<string, FlightVerdict> Remembered = new Dictionary<string, FlightVerdict>();
        static readonly object Gate = new object();

        public static void Remember(string key, FlightVerdict v)
        {
            if (key == null || v == null) return;
            lock (Gate) Remembered[key] = v;
        }

        /// <summary>The remembered verdict for a design key, else <see cref="Checking"/>.</summary>
        public static FlightVerdict Recall(string key)
        {
            if (key == null) return Checking;
            lock (Gate) return Remembered.TryGetValue(key, out var v) ? v : Checking;
        }

        // ---- the consumers ----

        /// <summary>The medal a flight keeps: none on a failed flight.</summary>
        public static AscentMedal Medal(AscentMedal timing, bool passed) => passed ? timing : AscentMedal.None;

        /// <summary>In flight the medal is not known yet (the flight may still fail), so the line never names one.</summary>
        public const string InFlightMedalLine = "Flying the plan.";

        public const string PredictPass = "Test flight reaches {0}", PredictFail = "Test flight misses {0}";

        /// <summary>The count card's detail line. No sheet Δv number: the predictor's flight says pass or miss. Null when
        /// the verdict is not known (the caller keeps its own line).</summary>
        public static string CountLine(FlightVerdict v, string trip)
        {
            if (v == null || !v.Known) return null;
            return string.Format(v.Passes ? PredictPass : PredictFail, string.IsNullOrEmpty(trip) ? "this trip" : trip);
        }
    }
}
