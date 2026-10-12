// One-tap resume from the result screen. Clocks and the 2 s budget live in data/retry.json.
// Calling this does not clear unlocks or personal bests. It is a no-op unless the flight is on SUMMARY.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public sealed class QuickRetry
    {
        public readonly string RequiresStatus;
        public readonly int Taps;
        public readonly bool Confirm;
        public readonly double ResultsToRelaunchSec;
        public readonly bool PreserveUnlocks;
        public readonly bool PreserveBests;
        readonly Dictionary<string, RetryPoint> points = new Dictionary<string, RetryPoint>();

        public static QuickRetry Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "retry.json"))));

        public static QuickRetry FromJson(JsonObject o)
        {
            var q = new QuickRetry(o.Str("requiresStatus"), (int)o.Num("taps"), o.Bool("confirm"), o.Num("resultsToRelaunchSec"));
            foreach (string name in new[] { "unlocks", "personalBests" })
            {
                bool found = false;
                foreach (object item in o.Arr("preserve")) if ((string)item == name) found = true;
                if (!found) throw new System.InvalidOperationException("retry must preserve " + name);
            }
            foreach (var kv in o.Obj("points"))
            {
                var p = (JsonObject)kv.Value;
                double? alt = p.ContainsKey("altitudeKm") ? p.Num("altitudeKm") : (double?)null;
                q.points[kv.Key] = new RetryPoint(kv.Key, p.Str("phase"), p.Num("tClock"), alt);
            }
            return q;
        }

        QuickRetry(string requiresStatus, int taps, bool confirm, double resultsToRelaunchSec)
        {
            RequiresStatus = requiresStatus;
            Taps = taps;
            Confirm = confirm;
            ResultsToRelaunchSec = resultsToRelaunchSec;
            PreserveUnlocks = true;
            PreserveBests = true;
        }

        /// <summary>Null when the flight is not on the result screen, or the point is unknown.</summary>
        public RetryPlan TryPlan(string point, string status)
        {
            if (status != RequiresStatus) return null;
            if (point == null || !points.TryGetValue(point, out var p)) return null;
            return new RetryPlan(p, Taps, Confirm, ResultsToRelaunchSec, PreserveUnlocks, PreserveBests);
        }
    }

    public sealed class RetryPoint
    {
        public readonly string Point, Phase;
        public readonly double TClock;
        public readonly double? AltitudeKm;
        public RetryPoint(string point, string phase, double tClock, double? altitudeKm)
        {
            Point = point; Phase = phase; TClock = tClock; AltitudeKm = altitudeKm;
        }
    }

    public sealed class RetryPlan
    {
        public readonly string Point, Phase;
        public readonly double TClock;
        public readonly double? AltitudeKm;
        public readonly int Taps;
        public readonly bool Confirm;
        public readonly double ResultsToRelaunchSec;
        public readonly bool PreserveUnlocks, PreserveBests;

        public RetryPlan(RetryPoint point, int taps, bool confirm, double resultsToRelaunchSec, bool preserveUnlocks, bool preserveBests)
        {
            Point = point.Point; Phase = point.Phase; TClock = point.TClock; AltitudeKm = point.AltitudeKm;
            Taps = taps; Confirm = confirm; ResultsToRelaunchSec = resultsToRelaunchSec;
            PreserveUnlocks = preserveUnlocks; PreserveBests = preserveBests;
        }
    }
}
