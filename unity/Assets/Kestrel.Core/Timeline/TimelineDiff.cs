// Designer-preview diff (docs/MISSION_EVENTS.md §4). Pure: two designs, or the timelines
// of a design before and after an edit, become added, removed, and shifted events.
// Problem flags explain a causality break, missing hardware, a lost restart, or a failed prediction.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Timeline
{
    public static class TimelineDiff
    {
        public const string Added = "added";
        public const string Removed = "removed";
        public const string Shifted = "shifted";
        public const string Causality = "causality";
        public const string MissingHardware = "missing-hardware";
        public const string LostRestart = "lost-restart";
        public const string FailedPrediction = "failed-prediction";

        static readonly Regex Ses = new Regex(@"^stg\.(ses|seco)\.([A-Za-z0-9]+)\.(\d+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static DiffReport Compare(Catalog catalog, Design before, Design after, uint seed = 1)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            var mission = new Mission { mode = "standard" };
            var beforeTimeline = TimelineGenerator.Generate(catalog, before, Pad.For(catalog, before), mission, seed);
            var afterTimeline = TimelineGenerator.Generate(catalog, after, Pad.For(catalog, after), mission, seed);
            var beforePrediction = LaunchPredictor.Predict(catalog, before, "standard", seed);
            var afterPrediction = LaunchPredictor.Predict(catalog, after, "standard", seed);
            return Compare(beforeTimeline, afterTimeline, beforePrediction, afterPrediction, seed);
        }

        public static DiffReport Compare(Timeline before, Timeline after, LaunchPrediction beforePrediction, LaunchPrediction afterPrediction, uint seed = 1)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            var report = new DiffReport
            {
                beforeVehicleId = before.vehicleId ?? "",
                afterVehicleId = after.vehicleId ?? "",
                seed = seed,
            };
            DiffEvents(before, after, report);
            FlagCausality(after, report);
            FlagMissingHardware(before, after, report);
            FlagLostRestart(before, after, report);
            FlagPrediction(beforePrediction, afterPrediction, report);
            Sort(report);
            return report;
        }

        static void DiffEvents(Timeline before, Timeline after, DiffReport report)
        {
            var beforeEvents = Index(before.events);
            var afterEvents = Index(after.events);
            foreach (var pair in afterEvents)
            {
                if (!beforeEvents.ContainsKey(pair.Key))
                {
                    var e = pair.Value;
                    report.added.Add(new DiffChange
                    {
                        kind = Added,
                        id = e.id,
                        label = "+ " + Short(e),
                        nominal = e.nominal,
                        afterNominal = e.nominal,
                        explainWhy = Why(e),
                    });
                }
            }
            foreach (var pair in beforeEvents)
            {
                if (afterEvents.ContainsKey(pair.Key)) continue;
                var e = pair.Value;
                var absence = FindAbsence(after, e.id);
                report.removed.Add(new DiffChange
                {
                    kind = Removed,
                    id = e.id,
                    label = "\u2212 " + Short(e),
                    nominal = e.nominal,
                    beforeNominal = e.nominal,
                    explainWhy = absence != null && !string.IsNullOrEmpty(absence.explainWhy)
                        ? absence.explainWhy
                        : "The edited vehicle no longer produces this event.",
                });
            }
            foreach (var pair in beforeEvents)
            {
                if (!afterEvents.TryGetValue(pair.Key, out var next)) continue;
                var prev = pair.Value;
                double delta = next.nominal - prev.nominal;
                if (Math.Abs(delta) <= 0.0005) continue;
                report.shifted.Add(new DiffChange
                {
                    kind = Shifted,
                    id = prev.id,
                    label = Short(next),
                    nominal = prev.nominal,
                    beforeNominal = prev.nominal,
                    afterNominal = next.nominal,
                    deltaSeconds = delta,
                    explainWhy = Short(next) + " shifts " + Signed(delta) + " s. " + Why(next),
                });
            }
        }

        static void FlagCausality(Timeline after, DiffReport report)
        {
            var checks = TimelineChecks.Evaluate(after);
            if (checks.errors == null) return;
            foreach (var err in checks.errors)
            {
                if (!IsCausality(err)) continue;
                report.problems.Add(new DiffProblem
                {
                    kind = Causality,
                    id = ErrorId(err),
                    explainWhy = OneLine(err),
                });
            }
        }

        static void FlagMissingHardware(Timeline before, Timeline after, DiffReport report)
        {
            var checks = TimelineChecks.Evaluate(after);
            if (checks.errors != null)
            {
                foreach (var err in checks.errors)
                {
                    if (err == null || err.IndexOf("hardware", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    report.problems.Add(new DiffProblem
                    {
                        kind = MissingHardware,
                        id = ErrorId(err),
                        explainWhy = OneLine(err),
                    });
                }
            }

            var afterHw = new HashSet<string>(after.hardware ?? new List<string>());
            var removed = new Dictionary<string, MissionEvent>();
            foreach (var e in before.events ?? new List<MissionEvent>())
            {
                if (e?.id == null || removed.ContainsKey(e.id)) continue;
                bool still = false;
                if (after.events != null)
                    foreach (var n in after.events) if (n?.id == e.id) { still = true; break; }
                if (!still) removed[e.id] = e;
            }

            var dropped = new List<string>();
            if (before.hardware != null)
                foreach (var h in before.hardware)
                    if (!string.IsNullOrEmpty(h) && !afterHw.Contains(h) && !dropped.Contains(h) && KeepHardware(h))
                        dropped.Add(h);
            foreach (var h in dropped)
            {
                var refs = new List<string>();
                foreach (var pair in removed)
                {
                    var links = pair.Value.links?.pad;
                    if (links == null) continue;
                    if (links.Contains(h)) refs.Add(pair.Key);
                }
                if (refs.Count == 0) continue;
                refs.Sort(StringComparer.Ordinal);
                var why = new StringBuilder();
                why.Append("The edit drops ").Append(h).Append(", referenced by ").Append(string.Join(", ", refs)).Append('.');
                foreach (var id in refs)
                {
                    var absence = FindAbsence(after, id);
                    if (absence != null && !string.IsNullOrEmpty(absence.explainWhy))
                        why.Append(' ').Append(absence.explainWhy);
                }
                report.problems.Add(new DiffProblem { kind = MissingHardware, id = h, explainWhy = OneLine(why.ToString()) });
            }

            var afterStages = new HashSet<string>();
            if (after.engines != null)
                foreach (var e in after.engines) if (e?.stageKey != null) afterStages.Add(e.stageKey);
            if (before.engines != null)
            {
                foreach (var e in before.engines)
                {
                    if (e?.stageKey == null || afterStages.Contains(e.stageKey)) continue;
                    report.problems.Add(new DiffProblem
                    {
                        kind = MissingHardware,
                        id = "stage:" + e.stageKey,
                        explainWhy = "The edit removes " + e.stageKey + " (" + e.engineId + "). Events that belonged to that stage are gone.",
                    });
                }
            }
        }

        static void FlagLostRestart(Timeline before, Timeline after, DiffReport report)
        {
            if (before.engines == null) return;
            foreach (var prev in before.engines)
            {
                if (prev?.stageKey == null) continue;
                StageEngineFact next = null;
                if (after.engines != null)
                    foreach (var e in after.engines) if (e?.stageKey == prev.stageKey) { next = e; break; }
                if (next == null) continue;
                bool lost = (prev.restart && !next.restart) || next.restarts < prev.restarts;
                if (!lost) continue;
                var burns = new List<string>();
                if (before.events != null)
                {
                    foreach (var e in before.events)
                    {
                        if (e?.id == null) continue;
                        var m = Ses.Match(e.id);
                        if (!m.Success || m.Groups[2].Value != prev.stageKey) continue;
                        bool still = false;
                        if (after.events != null)
                            foreach (var n in after.events) if (n?.id == e.id) { still = true; break; }
                        if (!still) burns.Add(e.id);
                    }
                }
                burns.Sort(StringComparer.Ordinal);
                var why = new StringBuilder();
                why.Append(prev.stageKey).Append(" lost restart: ").Append(prev.engineId)
                    .Append(" had restart ").Append(prev.restart ? "true" : "false")
                    .Append(" and ").Append(prev.restarts.ToString(CultureInfo.InvariantCulture))
                    .Append(" restarts; ").Append(next.engineId)
                    .Append(" has restart ").Append(next.restart ? "true" : "false")
                    .Append(" and ").Append(next.restarts.ToString(CultureInfo.InvariantCulture))
                    .Append(" restarts.");
                if (burns.Count > 0) why.Append(" Removed ").Append(string.Join(", ", burns)).Append('.');
                report.problems.Add(new DiffProblem { kind = LostRestart, id = prev.stageKey, explainWhy = OneLine(why.ToString()) });
            }
        }

        static void FlagPrediction(LaunchPrediction beforePrediction, LaunchPrediction afterPrediction, DiffReport report)
        {
            if (afterPrediction == null || afterPrediction.Success) return;
            string why = afterPrediction.Text ?? "";
            if (beforePrediction != null && beforePrediction.Success)
                why = "The edit loses the target. " + why;
            // The failure that ended the prediction flight, by type and time, so a pad-level apogee reads as the
            // breakup it is (scout prop cut: overg at T+0.000 s), not as a flight that never left.
            if (!string.IsNullOrEmpty(afterPrediction.Failure))
                why += " First failure: " + afterPrediction.Failure;
            report.problems.Add(new DiffProblem
            {
                kind = FailedPrediction,
                id = string.IsNullOrEmpty(afterPrediction.VehicleId) ? report.afterVehicleId : afterPrediction.VehicleId,
                explainWhy = OneLine(why),
            });
        }

        static void Sort(DiffReport report)
        {
            report.added.Sort(ByNominalThenId);
            report.removed.Sort(ByNominalThenId);
            report.shifted.Sort(ByNominalThenId);
            report.problems.Sort((a, b) =>
            {
                int k = Rank(a.kind).CompareTo(Rank(b.kind));
                if (k != 0) return k;
                int i = string.CompareOrdinal(a.id, b.id);
                if (i != 0) return i;
                return string.CompareOrdinal(a.explainWhy, b.explainWhy);
            });
        }

        static int ByNominalThenId(DiffChange a, DiffChange b)
        {
            int n = a.nominal.CompareTo(b.nominal);
            if (n != 0) return n;
            return string.CompareOrdinal(a.id, b.id);
        }

        static int Rank(string kind)
        {
            if (kind == Causality) return 0;
            if (kind == MissingHardware) return 1;
            if (kind == LostRestart) return 2;
            if (kind == FailedPrediction) return 3;
            return 4;
        }

        static bool IsCausality(string err)
        {
            if (string.IsNullOrEmpty(err)) return false;
            if (err.IndexOf("precondition", StringComparison.Ordinal) >= 0) return true;
            if (err.IndexOf("cycle", StringComparison.Ordinal) >= 0) return true;
            return err.IndexOf("nominal", StringComparison.Ordinal) >= 0 && err.IndexOf("before", StringComparison.Ordinal) >= 0;
        }

        static bool KeepHardware(string h)
        {
            if (h.StartsWith("engine:", StringComparison.Ordinal)) return false;
            if (h.StartsWith("propellant:", StringComparison.Ordinal)) return false;
            if (h.StartsWith("stage:", StringComparison.Ordinal)) return false;
            return true;
        }

        static string ErrorId(string err)
        {
            int c = err.IndexOf(':');
            if (c <= 0) return "preconditions";
            return err.Substring(0, c);
        }

        static Dictionary<string, MissionEvent> Index(List<MissionEvent> events)
        {
            var map = new Dictionary<string, MissionEvent>();
            if (events == null) return map;
            foreach (var e in events)
            {
                if (e?.id == null || map.ContainsKey(e.id)) continue;
                map[e.id] = e;
            }
            return map;
        }

        static Absence FindAbsence(Timeline t, string id)
        {
            if (t?.absent == null || id == null) return null;
            foreach (var a in t.absent) if (a?.id == id) return a;
            return null;
        }

        static string Short(MissionEvent e)
        {
            string s = e?.strings?.shortLabel;
            if (string.IsNullOrEmpty(s)) s = e?.id ?? "";
            return s;
        }

        static string Why(MissionEvent e) => OneLine(e?.strings?.explainWhy ?? "");

        static string Signed(double delta)
        {
            string body = TimelineJson.F(delta);
            if (delta > 0 && !body.StartsWith("+", StringComparison.Ordinal)) return "+" + body;
            return body;
        }

        static string OneLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }

    public sealed class DiffChange
    {
        public string kind;
        public string id;
        public string label;
        public double nominal;
        public double? beforeNominal;
        public double? afterNominal;
        public double? deltaSeconds;
        public string explainWhy;
    }

    public sealed class DiffProblem
    {
        public string kind;
        public string id;
        public string explainWhy;
    }

    public sealed class DiffReport
    {
        public string beforeVehicleId;
        public string afterVehicleId;
        public uint seed;
        public List<DiffChange> added = new List<DiffChange>();
        public List<DiffChange> removed = new List<DiffChange>();
        public List<DiffChange> shifted = new List<DiffChange>();
        public List<DiffProblem> problems = new List<DiffProblem>();

        public string Canonical()
        {
            var sb = new StringBuilder();
            sb.Append("before=").Append(beforeVehicleId).Append('\n');
            sb.Append("after=").Append(afterVehicleId).Append('\n');
            sb.Append("seed=").Append(seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            Section(sb, "added", added, false);
            Section(sb, "removed", removed, false);
            Section(sb, "shifted", shifted, true);
            sb.Append("problems:\n");
            foreach (var p in problems)
                sb.Append(p.kind).Append(" | ").Append(p.id).Append(" | ").Append(p.explainWhy).Append('\n');
            return sb.ToString();
        }

        static void Section(StringBuilder sb, string name, List<DiffChange> rows, bool shifted)
        {
            sb.Append(name).Append(":\n");
            foreach (var c in rows)
            {
                sb.Append(c.id).Append(" | ").Append(c.label).Append(" | ");
                if (shifted)
                {
                    sb.Append("before=").Append(TimelineJson.F(c.beforeNominal ?? 0));
                    sb.Append(" | after=").Append(TimelineJson.F(c.afterNominal ?? 0));
                    sb.Append(" | delta=").Append(c.deltaSeconds.HasValue && c.deltaSeconds.Value > 0 ? "+" : "");
                    sb.Append(TimelineJson.F(c.deltaSeconds ?? 0));
                }
                else sb.Append("t=").Append(TimelineJson.F(c.nominal));
                sb.Append(" | ").Append(c.explainWhy).Append('\n');
            }
        }
    }
}
