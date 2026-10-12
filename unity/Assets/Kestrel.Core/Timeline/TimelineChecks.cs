// Causality, hardware existence, and staging consistency (docs/MISSION_EVENTS.md §3).
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kestrel.Core.Timeline
{
    public static class TimelineChecks
    {
        static readonly Regex Ses = new Regex(@"^stg\.(ses|seco)\.([A-Za-z0-9]+)\.(\d+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static CheckReport Evaluate(Timeline t)
        {
            var errors = new List<string>();
            if (t == null) { errors.Add("timeline is null"); return Finish(errors); }
            var hardware = new HashSet<string>(t.hardware ?? new List<string>());
            var engines = new Dictionary<string, StageEngineFact>();
            if (t.engines != null) foreach (var e in t.engines) if (e?.stageKey != null && !engines.ContainsKey(e.stageKey)) engines[e.stageKey] = e;

            var index = new Dictionary<string, int>();
            var seen = new HashSet<string>();
            var events = t.events ?? new List<MissionEvent>();
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e == null) { errors.Add("null event at " + i); continue; }
                if (string.IsNullOrEmpty(e.id)) errors.Add("event " + i + " has no id");
                else if (!seen.Add(e.id)) errors.Add(e.id + ": duplicate id");
                else index[e.id] = i;
                if (i > 0 && events[i - 1] != null && e.nominal + 1e-9 < events[i - 1].nominal)
                    errors.Add(e.id + ": nominal " + e.nominal + " is before " + events[i - 1].id);
                CheckEvent(e, hardware, errors, false);
                CheckSes(e, engines, errors);
            }

            var branches = t.branches ?? new List<MissionEvent>();
            var branchIds = new HashSet<string>();
            foreach (var b in branches)
            {
                if (b == null) { errors.Add("null branch"); continue; }
                if (string.IsNullOrEmpty(b.id)) { errors.Add("branch has no id"); continue; }
                if (!branchIds.Add(b.id) || seen.Contains(b.id)) errors.Add(b.id + ": duplicate id");
                CheckEvent(b, hardware, errors, true);
                if (b.preconditions != null)
                    foreach (var pre in b.preconditions)
                        if (!index.ContainsKey(pre)) errors.Add(b.id + ": branch precondition " + pre + " is not a nominal event");
            }

            foreach (var e in events)
            {
                if (e?.preconditions == null) continue;
                if (!index.TryGetValue(e.id, out var at)) continue;
                foreach (var pre in e.preconditions)
                {
                    if (!index.TryGetValue(pre, out var pAt)) errors.Add(e.id + ": precondition " + pre + " is missing");
                    else if (pAt >= at) errors.Add(e.id + ": precondition " + pre + " does not precede it");
                }
                if (e.abortRules != null)
                    foreach (var rule in e.abortRules)
                        if (rule == null || !branchIds.Contains(rule.branchId))
                            errors.Add(e.id + ": abort rule points at missing branch " + (rule?.branchId ?? "(null)"));
            }

            if (HasCycle(events)) errors.Add("precondition graph has a cycle");

            if (t.absent != null)
            {
                var abs = new HashSet<string>();
                foreach (var a in t.absent)
                {
                    if (a == null || string.IsNullOrEmpty(a.id)) { errors.Add("absence missing id"); continue; }
                    if (!abs.Add(a.id)) errors.Add(a.id + ": duplicate absence");
                    if (seen.Contains(a.id) || branchIds.Contains(a.id)) errors.Add(a.id + ": listed as both present and absent");
                    if (string.IsNullOrEmpty(a.explainWhy)) errors.Add(a.id + ": absence has no explain-why");
                }
            }
            return Finish(errors);
        }

        static void CheckSes(MissionEvent e, Dictionary<string, StageEngineFact> engines, List<string> errors)
        {
            var m = Ses.Match(e.id ?? "");
            if (!m.Success) return;
            string key = m.Groups[2].Value;
            int n = int.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (!engines.TryGetValue(key, out var fact)) { errors.Add(e.id + ": no engine fact for stage " + key); return; }
            if (!fact.restart) errors.Add(e.id + ": SES/SECO requires a restartable engine (" + fact.engineId + " is not)");
            if (n >= 2 && fact.restarts < n - 1) errors.Add(e.id + ": needs " + (n - 1) + " restarts, " + fact.engineId + " has " + fact.restarts);
            if (n > fact.restarts + 1) errors.Add(e.id + ": burn " + n + " exceeds starts on " + fact.engineId);
        }

        static void CheckEvent(MissionEvent e, HashSet<string> hardware, List<string> errors, bool branch)
        {
            string id = e.id ?? ("#" + (branch ? "branch" : "event"));
            if (!Contains(TimelinePhase.All, e.phase)) errors.Add(id + ": bad phase");
            if (!Contains(TimelineCriticality.All, e.criticality)) errors.Add(id + ": bad criticality");
            if (!Contains(TimelineVisibility.All, e.visibility)) errors.Add(id + ": bad visibility");
            if (e.status == null || !Contains(TimelineStatus.All, e.status.state)) errors.Add(id + ": bad status");
            if (e.strings == null || string.IsNullOrEmpty(e.strings.explainWhy)) errors.Add(id + ": missing explain-why");
            if (e.strings != null && (e.strings.shortLabel == null || e.strings.shortLabel.Length > 12))
                errors.Add(id + ": short label must be 1..12 characters");
            if (e.links == null || e.links.pad == null || e.links.pad.Count == 0) errors.Add(id + ": references no hardware");
            else foreach (var h in e.links.pad) if (!hardware.Contains(h)) errors.Add(id + ": hardware " + h + " is not on this vehicle or pad");
            var tr = e.trigger;
            if (tr == null) errors.Add(id + ": missing trigger");
            else if (tr.kind == "tRel") { if (string.IsNullOrEmpty(tr.from)) errors.Add(id + ": tRel needs from"); }
            else if (tr.kind == "simCondition") { if (string.IsNullOrEmpty(tr.expr)) errors.Add(id + ": simCondition needs expr"); }
            else if (tr.kind == "after") { if (string.IsNullOrEmpty(tr.eventId)) errors.Add(id + ": after needs eventId"); }
            else if (tr.kind == "action")
            {
                if (tr.actor != "player" && tr.actor != "autopilot") errors.Add(id + ": action actor must be player or autopilot");
                if (string.IsNullOrEmpty(tr.actionId)) errors.Add(id + ": action needs id");
            }
            else errors.Add(id + ": bad trigger kind");
            if (double.IsNaN(e.nominal) || double.IsInfinity(e.nominal)) errors.Add(id + ": nominal is not finite");
        }

        static bool HasCycle(List<MissionEvent> events)
        {
            var ids = new HashSet<string>();
            foreach (var e in events) if (e?.id != null) ids.Add(e.id);
            var color = new Dictionary<string, int>();
            foreach (var e in events) if (e?.id != null && !color.ContainsKey(e.id) && Walk(e.id, events, ids, color)) return true;
            return false;
        }

        static bool Walk(string id, List<MissionEvent> events, HashSet<string> ids, Dictionary<string, int> color)
        {
            color[id] = 1;
            MissionEvent e = null;
            foreach (var x in events) if (x?.id == id) { e = x; break; }
            if (e?.preconditions != null)
            {
                foreach (var pre in e.preconditions)
                {
                    if (!ids.Contains(pre)) continue;
                    if (!color.TryGetValue(pre, out var c)) { if (Walk(pre, events, ids, color)) return true; }
                    else if (c == 1) return true;
                }
            }
            color[id] = 2;
            return false;
        }

        static bool Contains(string[] all, string value)
        {
            if (value == null) return false;
            for (int i = 0; i < all.Length; i++) if (all[i] == value) return true;
            return false;
        }

        static CheckReport Finish(List<string> errors) => new CheckReport { ok = errors.Count == 0, errors = errors };
    }
}
