// B-10 launch gate (LANDSCAPE_WEB_BRIEF §3, audit B-10/B-05). Launch is ready only when the design
// is not blocked for this mission: no validation blocks, no deterministic red prediction from the
// cached ascent, the payload fits and the speed reaches the target. A stale ascent cache is
// "Checking…", never ready. Experience.Present still drops warnings in Simple; this gate is what
// decides the Ready line and the Launch button, so a red predicted failure can no longer read as ready.
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer
{
    public enum LaunchGateState { Ready, Blocked, Checking }

    public sealed class LaunchGateResult
    {
        public LaunchGateState state;
        /// <summary>One plain sentence. Empty when ready.</summary>
        public string reason = "";
        /// <summary>Designer tab id the fix lives on ("tanks", "payload", ...). Null when ready or checking.</summary>
        public string tab;
        public string code;
        public bool Ready => state == LaunchGateState.Ready;
        /// <summary>The line the designer shows: the reason, "Checking…", or <see cref="Experience.Ready"/>.</summary>
        public string Line => state == LaunchGateState.Ready ? Experience.Ready : state == LaunchGateState.Checking ? LaunchGate.Checking : reason;
    }

    public static class LaunchGate
    {
        public const string Checking = "Checking…";
        public const string Fallback = "Something on this rocket needs a fix first.";
        public const string NoFit = "The cargo does not fit inside the nose cone.";
        public const string TooSlow = "Not enough speed for this orbit. Add fuel or a bigger engine.";
        public const string WillFail = "The test flight says this rocket fails.";

        public static LaunchGateResult Evaluate(EngineeringReport report, QuickReadout readout)
        {
            if (report == null || !report.ok)
                return Blocked(First(report), "schema", "stages");
            if (report.blocks != null && report.blocks.Count > 0)
            {
                var b = report.blocks[0];
                return Blocked(Plain(b.kid, b.msg), b.code, b.tab);
            }
            var r = readout ?? report.readout;
            if (r == null) return new LaunchGateResult { state = LaunchGateState.Checking };
            if (r.fairingFits == false) return Blocked(NoFit, "payload-doesnt-fit", "payload");
            if (r.margin < 0) return Blocked(TooSlow, "dv-margin", "tanks");
            var red = DeterministicRed(r.risks);
            if (red != null) return Blocked(Plain(red.kid, red.engineer, WillFail), red.kind, red.tab);
            if (r.predictedFailure != null)
                return Blocked(Plain(r.predictedFailure.kid, r.predictedFailure.fixHint, WillFail), r.predictedFailure.kind, null);
            if (r.maxQStale) return new LaunchGateResult { state = LaunchGateState.Checking };
            return new LaunchGateResult { state = LaunchGateState.Ready };
        }

        /// <summary>A red row that is not one of the seeded fair events (odds &lt; 0) happens on every flight.</summary>
        public static FlightRisk DeterministicRed(IList<FlightRisk> risks)
        {
            if (risks == null) return null;
            for (int i = 0; i < risks.Count; i++)
            {
                var risk = risks[i];
                if (risk != null && risk.level == "red" && risk.odds < 0) return risk;
            }
            return null;
        }

        static string First(EngineeringReport report)
        {
            if (report != null && report.blocks != null && report.blocks.Count > 0)
                return Plain(report.blocks[0].kid, report.blocks[0].msg);
            return Fallback;
        }

        static string Plain(string kid, string msg, string fallback = Fallback)
        {
            if (!string.IsNullOrWhiteSpace(kid)) return kid.Trim();
            if (!string.IsNullOrWhiteSpace(msg)) return msg.Trim();
            return fallback;
        }

        static LaunchGateResult Blocked(string reason, string code, string tab) =>
            new LaunchGateResult { state = LaunchGateState.Blocked, reason = reason ?? Fallback, code = code, tab = tab };
    }
}
