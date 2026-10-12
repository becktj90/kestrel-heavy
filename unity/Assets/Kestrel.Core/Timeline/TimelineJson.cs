// Canonical JSON for timelines. Same input must be byte-identical (docs/MISSION_EVENTS.md §3).
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kestrel.Core.Timeline
{
    public static class TimelineJson
    {
        public static string Write(Timeline t)
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            Str(sb, 1, "schema", "MissionEvent");
            Num(sb, 1, "schemaVersion", 1);
            Str(sb, 1, "vehicleId", t.vehicleId);
            Str(sb, 1, "padId", t.padId);
            Str(sb, 1, "missionId", t.missionId);
            Str(sb, 1, "mode", t.mode);
            Str(sb, 1, "recovery", t.recovery);
            UInt(sb, 1, "seed", t.seed);
            Dbl(sb, 1, "latitude", t.latitude);
            Strings(sb, 1, "hardware", t.hardware);
            Engines(sb, 1, t.engines);
            Events(sb, 1, "events", t.events);
            Events(sb, 1, "branches", t.branches);
            Absences(sb, 1, t.absent);
            Checks(sb, 1, t.checks);
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>Bus payload for a status change: event.status {id, status, actual, progress}.</summary>
        public static string StatusEvent(MissionEvent e)
        {
            var sb = new StringBuilder(128);
            sb.Append("{\"type\":\"event.status\",\"id\":");
            Quote(sb, e.id);
            sb.Append(",\"status\":");
            Quote(sb, e.status?.state ?? "pending");
            sb.Append(",\"actual\":");
            if (e.actual.HasValue) sb.Append(F(e.actual.Value)); else sb.Append("null");
            sb.Append(",\"progress\":");
            sb.Append(F(e.status?.progress ?? 0));
            sb.Append("}");
            return sb.ToString();
        }

        static void Events(StringBuilder sb, int indent, string key, List<MissionEvent> list)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(": [\n");
            for (int i = 0; i < list.Count; i++)
            {
                Event(sb, indent + 1, list[i]);
                if (i + 1 < list.Count) sb.Append(',');
                sb.Append('\n');
            }
            Indent(sb, indent); sb.Append("],\n");
        }

        static void Event(StringBuilder sb, int indent, MissionEvent e)
        {
            Indent(sb, indent); sb.Append("{\n");
            Str(sb, indent + 1, "id", e.id);
            Str(sb, indent + 1, "name", e.name);
            Str(sb, indent + 1, "shortLabel", e.shortLabel);
            Str(sb, indent + 1, "phase", e.phase);
            Trigger(sb, indent + 1, e.trigger);
            Strings(sb, indent + 1, "preconditions", e.preconditions);
            Strings(sb, indent + 1, "blocks", e.blocks);
            Bool(sb, indent + 1, "holdable", e.holdable);
            Str(sb, indent + 1, "criticality", e.criticality);
            Rules(sb, indent + 1, e.abortRules);
            Dbl(sb, indent + 1, "nominal", e.nominal);
            DblNull(sb, indent + 1, "actual", e.actual);
            Dbl(sb, indent + 1, "tolerance", e.tolerance);
            Dbl(sb, indent + 1, "variance", e.variance);
            Status(sb, indent + 1, e.status);
            Links(sb, indent + 1, e.links);
            Str(sb, indent + 1, "visibility", e.visibility);
            Economy(sb, indent + 1, e.economy);
            StringsObj(sb, indent + 1, e.strings);
            Indent(sb, indent); sb.Append('}');
        }

        static void Trigger(StringBuilder sb, int indent, Trigger t)
        {
            Indent(sb, indent); sb.Append("\"trigger\": {");
            if (t == null) { sb.Append("},\n"); return; }
            sb.Append("\"kind\":"); Quote(sb, t.kind);
            if (t.kind == "tRel") { sb.Append(",\"t\":"); sb.Append(F(t.t)); sb.Append(",\"from\":"); Quote(sb, t.from); }
            else if (t.kind == "simCondition") { sb.Append(",\"expr\":"); Quote(sb, t.expr); }
            else if (t.kind == "after") { sb.Append(",\"eventId\":"); Quote(sb, t.eventId); sb.Append(",\"delay\":"); sb.Append(F(t.delay)); }
            else if (t.kind == "action") { sb.Append(",\"actor\":"); Quote(sb, t.actor); sb.Append(",\"id\":"); Quote(sb, t.actionId); }
            sb.Append("},\n");
        }

        static void Rules(StringBuilder sb, int indent, List<AbortRule> rules)
        {
            Indent(sb, indent); sb.Append("\"abortRules\": [");
            if (rules != null && rules.Count > 0)
            {
                sb.Append('\n');
                for (int i = 0; i < rules.Count; i++)
                {
                    Indent(sb, indent + 1); sb.Append("{\"condition\":"); Quote(sb, rules[i].condition);
                    sb.Append(",\"branchId\":"); Quote(sb, rules[i].branchId); sb.Append('}');
                    if (i + 1 < rules.Count) sb.Append(',');
                    sb.Append('\n');
                }
                Indent(sb, indent);
            }
            sb.Append("],\n");
        }

        static void Status(StringBuilder sb, int indent, EventStatus s)
        {
            Indent(sb, indent); sb.Append("\"status\": {\"state\":");
            Quote(sb, s?.state ?? "pending");
            sb.Append(",\"progress\":");
            sb.Append(F(s?.progress ?? 0));
            sb.Append("},\n");
        }

        static void Links(StringBuilder sb, int indent, EventLinks links)
        {
            Indent(sb, indent); sb.Append("\"links\": {\n");
            Strings(sb, indent + 1, "narration", links?.narration);
            Strings(sb, indent + 1, "sfx", links?.sfx);
            Strings(sb, indent + 1, "music", links?.music);
            Strings(sb, indent + 1, "telemetry", links?.telemetry);
            Strings(sb, indent + 1, "pad", links?.pad);
            StringsLast(sb, indent + 1, "codex", links?.codex);
            Indent(sb, indent); sb.Append("},\n");
        }

        static void Economy(StringBuilder sb, int indent, EconomyHooks e)
        {
            Indent(sb, indent); sb.Append("\"economy\": {\"costOnDone\":");
            sb.Append((e?.costOnDone ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"refundOnScrub\":");
            sb.Append((e?.refundOnScrub ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"currency\":");
            Quote(sb, e?.currency ?? "Kc");
            sb.Append("},\n");
        }

        static void StringsObj(StringBuilder sb, int indent, EventStrings s)
        {
            Indent(sb, indent); sb.Append("\"strings\": {\n");
            Str(sb, indent + 1, "name", s?.name);
            Str(sb, indent + 1, "short", s?.shortLabel);
            Str(sb, indent + 1, "explainWhy", s?.explainWhy);
            StrLast(sb, indent + 1, "anomaly", s?.anomaly);
            Indent(sb, indent); sb.Append("}\n");
        }

        static void Engines(StringBuilder sb, int indent, List<StageEngineFact> list)
        {
            Indent(sb, indent); sb.Append("\"engines\": [\n");
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    Indent(sb, indent + 1);
                    sb.Append("{\"stageKey\":"); Quote(sb, e.stageKey);
                    sb.Append(",\"engineId\":"); Quote(sb, e.engineId);
                    sb.Append(",\"restart\":"); sb.Append(e.restart ? "true" : "false");
                    sb.Append(",\"restarts\":"); sb.Append(e.restarts.ToString(CultureInfo.InvariantCulture));
                    sb.Append('}');
                    if (i + 1 < list.Count) sb.Append(',');
                    sb.Append('\n');
                }
            }
            Indent(sb, indent); sb.Append("],\n");
        }

        static void Absences(StringBuilder sb, int indent, List<Absence> list)
        {
            Indent(sb, indent); sb.Append("\"absent\": [\n");
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Indent(sb, indent + 1); sb.Append("{\"id\":"); Quote(sb, list[i].id);
                    sb.Append(",\"explainWhy\":"); Quote(sb, list[i].explainWhy); sb.Append('}');
                    if (i + 1 < list.Count) sb.Append(',');
                    sb.Append('\n');
                }
            }
            Indent(sb, indent); sb.Append("],\n");
        }

        static void Checks(StringBuilder sb, int indent, CheckReport c)
        {
            Indent(sb, indent); sb.Append("\"checks\": {\"ok\":");
            sb.Append(c != null && c.ok ? "true" : "false");
            sb.Append(",\"errors\":[");
            var errs = c?.errors;
            if (errs != null && errs.Count > 0)
            {
                for (int i = 0; i < errs.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Quote(sb, errs[i]);
                }
            }
            sb.Append("]}\n");
        }

        static void Strings(StringBuilder sb, int indent, string key, List<string> values)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(": [");
            WriteStringList(sb, values);
            sb.Append("],\n");
        }

        static void StringsLast(StringBuilder sb, int indent, string key, List<string> values)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(": [");
            WriteStringList(sb, values);
            sb.Append("]\n");
        }

        static void WriteStringList(StringBuilder sb, List<string> values)
        {
            if (values == null) return;
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Quote(sb, values[i]);
            }
        }

        static void Str(StringBuilder sb, int indent, string key, string value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':'); Quote(sb, value); sb.Append(",\n");
        }

        static void StrLast(StringBuilder sb, int indent, string key, string value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':'); Quote(sb, value); sb.Append('\n');
        }

        static void Dbl(StringBuilder sb, int indent, string key, double value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':'); sb.Append(F(value)); sb.Append(",\n");
        }

        static void DblNull(StringBuilder sb, int indent, string key, double? value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':');
            if (value.HasValue) sb.Append(F(value.Value)); else sb.Append("null");
            sb.Append(",\n");
        }

        static void Num(StringBuilder sb, int indent, string key, int value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':'); sb.Append(value.ToString(CultureInfo.InvariantCulture)); sb.Append(",\n");
        }

        static void UInt(StringBuilder sb, int indent, string key, uint value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':'); sb.Append(value.ToString(CultureInfo.InvariantCulture)); sb.Append(",\n");
        }

        static void Bool(StringBuilder sb, int indent, string key, bool value)
        {
            Indent(sb, indent); Quote(sb, key); sb.Append(':'); sb.Append(value ? "true" : "false"); sb.Append(",\n");
        }

        public static string F(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "null";
            double r = System.Math.Round(v, 3, System.MidpointRounding.AwayFromZero);
            if (r == 0) return "0";
            return r.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static void Indent(StringBuilder sb, int n) { sb.Append(' ', n * 2); }

        static void Quote(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (c == '"' || c == '\\') { sb.Append('\\'); sb.Append(c); }
                    else if (c == '\n') sb.Append("\\n");
                    else if (c == '\r') sb.Append("\\r");
                    else if (c == '\t') sb.Append("\\t");
                    else sb.Append(c);
                }
            }
            sb.Append('"');
        }
    }
}
