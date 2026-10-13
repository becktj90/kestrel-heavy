// Hangar feedback: which haptic to play and what VoiceOver should say after a change. Pure decisions, so EditMode
// tests pin them; HangarNative plays them on iOS. Patterns come from data/designer-feel.json ("snap" for a step or a
// selection, "payoff" when the rocket becomes ready to fly). Nothing here fires on a timer or nags: feedback only
// answers something the player just did.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Hangar;

namespace Kestrel.Slice
{
    public enum Haptic { None = 0, Snap = 1, Payoff = 2, Warning = 3 }

    /// <summary>What the Hangar looked like after one change: enough to decide feedback without keeping the design.</summary>
    public struct FeelState
    {
        public bool launchEnabled;
        public int blocks, warnings;
        public string firstBlockKid;
        public double liftoffTwr, totalDv, requiredDv;
        public bool valid;
        /// <summary>Each block as "code@part", so an edit that swaps one block for another still counts as a new one.</summary>
        public HashSet<string> blockKeys;

        public static FeelState Of(HangarSession s)
        {
            var st = new FeelState { valid = s != null };
            if (s == null) return st;
            st.launchEnabled = s.LaunchEnabled;
            foreach (var a in s.Advisories)
            {
                if (a.severity == AdvisorySeverity.Block)
                {
                    if (st.blocks++ == 0) st.firstBlockKid = a.kid;
                    (st.blockKeys ??= new HashSet<string>()).Add(a.code + "@" + a.part);
                }
                else if (a.severity == AdvisorySeverity.Warn) st.warnings++;
            }
            var sheet = s.Sheet;
            if (sheet != null) { st.liftoffTwr = sheet.liftoffTwr; st.totalDv = sheet.totalDeltaV_mps; st.requiredDv = sheet.requiredDeltaV_mps; }
            return st;
        }
    }

    public static class HangarFeel
    {
        public const string HapticsPref = "kh.hangar.haptics";
        /// <summary>Repeated stepper ticks closer than this get no extra snap (a held button stays gentle).</summary>
        public const float SnapMinSeconds = 0.08f;
        /// <summary>Announcements wait this long after the last change so a held stepper speaks once, with the final value.</summary>
        public const float AnnounceAfterSeconds = 0.45f;

        /// <summary>
        /// Haptic for a change from <paramref name="before"/> to <paramref name="after"/>: payoff when the rocket becomes
        /// ready to fly, a warning when a new block appears, otherwise a light snap for the step itself.
        /// </summary>
        public static Haptic For(FeelState before, FeelState after, bool stepped)
        {
            if (!after.valid) return Haptic.None;
            if (before.valid && !before.launchEnabled && after.launchEnabled) return Haptic.Payoff;
            if (before.valid && HasNewBlock(before, after)) return Haptic.Warning;
            return stepped ? Haptic.Snap : Haptic.None;
        }

        /// <summary>True when <paramref name="after"/> has a block <paramref name="before"/> did not (more blocks, or a different one).</summary>
        public static bool HasNewBlock(FeelState before, FeelState after)
        {
            if (after.blocks > before.blocks) return true;
            if (after.blockKeys == null) return false;
            foreach (var k in after.blockKeys) if (before.blockKeys == null || !before.blockKeys.Contains(k)) return true;
            return false;
        }

        /// <summary>Taptic pattern for a haptic, from designer-feel.json (milliseconds of each pulse).</summary>
        public static int[] Pattern(Haptic h, IReadOnlyDictionary<string, int[]> feel)
        {
            string key = h == Haptic.Payoff ? "payoff" : h == Haptic.None ? null : "snap";
            return key != null && feel != null && feel.TryGetValue(key, out var p) ? p : Array.Empty<int>();
        }

        /// <summary>
        /// What VoiceOver says after an edit: the change, then the liftoff push and speed, then the Launch gate when it
        /// flipped. "Stage 1 engines, 9. Liftoff push 1.38. Speed 112% of the trip." / "Launch unavailable. Too heavy …".
        /// </summary>
        public static string Announce(EditStep step, FeelState before, FeelState after, bool undo = false, bool redo = false)
        {
            var parts = new List<string>();
            if (step != null)
            {
                if (undo) parts.Add("Undid: " + step.Spoken);
                else if (redo) parts.Add("Redid: " + step.Spoken);
                else parts.Add(step.to != null ? $"{step.what}, {step.to}" : step.what);
            }
            if (after.valid && after.liftoffTwr > 0)
            {
                parts.Add("Liftoff push " + after.liftoffTwr.ToString("0.00", CultureInfo.InvariantCulture));
                if (after.requiredDv > 0)
                    parts.Add("Speed " + Math.Round(100 * after.totalDv / after.requiredDv, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture) + "% of the trip");
            }
            if (before.valid && after.valid && before.launchEnabled != after.launchEnabled)
                parts.Add(after.launchEnabled ? "Ready to launch" : "Launch unavailable. " + (after.firstBlockKid ?? "Something needs a fix").TrimEnd('.'));
            return parts.Count == 0 ? "" : string.Join(". ", parts) + ".";
        }

        /// <summary>Parse data/designer-feel.json's "haptics" block. Unknown or missing keys simply have no pattern.</summary>
        public static Dictionary<string, int[]> ParsePatterns(string json)
        {
            var o = new Dictionary<string, int[]>();
            if (string.IsNullOrEmpty(json)) return o;
            var h = Kestrel.Core.Json.MiniJson.ParseObject(json).Obj("haptics");
            if (h == null) return o;
            foreach (var k in h.Order)
            {
                if (!(h[k] is List<object> arr)) continue;
                var ms = new List<int>();
                foreach (var x in arr) if (x is double d && d > 0) ms.Add((int)d);
                o[k] = ms.ToArray();
            }
            return o;
        }
    }
}
