// Why-this-matters and check strings. S3 owns data/designer-stepper.json and may land it in parallel.
// This reader accepts that file when it is present and falls back to short placeholders keyed by id.
//
// Ids:
//   step.{mission|engines|tanks|aero|recovery|look|review}.why.kid
//   step.{...}.why.engineer
//   step.{...}.goal
//   check.{code}.kid
//   check.{code}.engineer
//
// Accepted shapes (any mix):
//   { "strings": { "step.engines.why.kid": "..." } }
//   { "strings": { "step.engines.why.kid": { "en": "..." } } }
//   { "steps": { "engines": { "goal": "...", "why": { "kid": "...", "engineer": "..." } } } }
//   { "steps": [ { "id": "engines", "whyKid": "...", "whyEngineer": "..." } ] }
//   { "checks": { "twr-low": { "kid": "...", "engineer": "..." } } }
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Json;
using UnityEngine;

namespace Kestrel.Slice.DesignerV3.Stepper
{
    public sealed class StepperCopy
    {
        public const string FileName = "designer-stepper.json";
        public const string ResourcePath = "KestrelData/designer-stepper";

        readonly Dictionary<string, string> strings = new Dictionary<string, string>();

        static StepperCopy current;

        public static StepperCopy Current => current ??= Load();

        public static void Reset() => current = null;

        public static string Fallback(string id)
        {
            switch (id)
            {
                case "step.mission.why.kid": return "Pick the cargo and where it is going. That choice is the job.";
                case "step.mission.why.engineer": return "Payload mass and target orbit set the Δv the rocket has to reach.";
                case "step.engines.why.kid": return "Fuel and engines make the push. Too little push and the rocket stays on the pad.";
                case "step.engines.why.engineer": return "Liftoff TWR is thrust divided by weight. A mixture offset past ±6% makes the burn uneven.";
                case "step.tanks.why.kid": return "More fuel goes farther, and it weighs more. Extra stages leave empty tanks behind.";
                case "step.tanks.why.engineer": return "Δv comes from the rocket equation. A higher safety factor makes the walls thicker.";
                case "step.aero.why.kid": return "The cargo has to fit in the nose, and the rocket has to stay pointy-end up.";
                case "step.aero.why.engineer": return "Fairing fit is a hard gate. Stability stays positive when the balance point is above the fins.";
                case "step.recovery.why.kid": return "Let it fall, or save some fuel and add legs so it can come home.";
                case "step.recovery.why.engineer": return "A landing mission needs a reserve that covers the return burn.";
                case "step.look.why.kid": return "Paint and a name. This does not change the flight.";
                case "step.look.why.engineer": return "Livery does not change mass, thrust, or Δv.";
                case "step.review.why.kid": return "Read each risk, then launch. A red row is worth a look first.";
                case "step.review.why.engineer": return "Red rows use the same launch rules as the all-parts check.";
                default: return null;
            }
        }

        public static string StepKey(DesignerStep step)
        {
            switch (step)
            {
                case DesignerStep.Mission: return "mission";
                case DesignerStep.Engines: return "engines";
                case DesignerStep.Tanks: return "tanks";
                case DesignerStep.Aero: return "aero";
                case DesignerStep.Recovery: return "recovery";
                case DesignerStep.Look: return "look";
                default: return "review";
            }
        }

        public static string WhyKidId(DesignerStep step) => "step." + StepKey(step) + ".why.kid";
        public static string WhyEngineerId(DesignerStep step) => "step." + StepKey(step) + ".why.engineer";
        public static string GoalId(DesignerStep step) => "step." + StepKey(step) + ".goal";
        public static string CheckKidId(string code) => "check." + (code ?? "") + ".kid";
        public static string CheckEngineerId(string code) => "check." + (code ?? "") + ".engineer";

        /// <summary>Text from the data file only. Null when the id was not in the file.</summary>
        public string FileText(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return strings.TryGetValue(id, out var text) && !string.IsNullOrEmpty(text) ? text : null;
        }

        /// <summary>File text when present, otherwise the placeholder. Unknown ids come back empty.</summary>
        public string Resolve(string id)
        {
            var file = FileText(id);
            if (!string.IsNullOrEmpty(file)) return file;
            return Fallback(id) ?? "";
        }

        public static StepperCopy Parse(string json)
        {
            var copy = new StepperCopy();
            if (string.IsNullOrWhiteSpace(json)) return copy;
            try { copy.Read(MiniJson.ParseObject(json)); }
            catch (Exception) { return new StepperCopy(); }
            return copy;
        }

        public static StepperCopy Load()
        {
            string json = ReadResource() ?? ReadRepoFile();
            return Parse(json);
        }

        static string ReadResource()
        {
            try
            {
                var asset = Resources.Load<TextAsset>(ResourcePath);
                if (asset != null && !string.IsNullOrWhiteSpace(asset.text)) return asset.text;
            }
            catch (Exception) { }
            return null;
        }

        static string ReadRepoFile()
        {
            try
            {
                string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
                string path = Path.Combine(root, "data", FileName);
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            catch (Exception) { }
            return null;
        }

        void Read(JsonObject root)
        {
            if (root == null) return;
            var flat = root.Obj("strings") ?? root.Obj("copy") ?? root.Obj("ids");
            if (flat != null)
            {
                foreach (var kv in flat)
                    Put(kv.Key, TextOf(kv.Value));
            }
            ReadSteps(root.Obj("steps"));
            ReadStepArray(root.Arr("steps"));
            ReadChecks(root.Obj("checks"));
        }

        void ReadSteps(JsonObject steps)
        {
            if (steps == null) return;
            foreach (var kv in steps)
            {
                if (!(kv.Value is JsonObject o)) continue;
                string key = NormalizeStep(kv.Key);
                if (key == null) continue;
                Put("step." + key + ".goal", First(o, "goal", "goalKid"));
                var why = o.Obj("why");
                if (why != null)
                {
                    Put("step." + key + ".why.kid", First(why, "kid", "simple", "text"));
                    Put("step." + key + ".why.engineer", First(why, "engineer", "eng"));
                }
                Put("step." + key + ".why.kid", First(o, "whyKid", "kid"));
                Put("step." + key + ".why.engineer", First(o, "whyEngineer", "engineer"));
            }
        }

        void ReadStepArray(List<object> steps)
        {
            if (steps == null) return;
            for (int i = 0; i < steps.Count; i++)
            {
                if (!(steps[i] is JsonObject o)) continue;
                string key = NormalizeStep(o.Str("id") ?? o.Str("step") ?? o.Str("key"));
                if (key == null && o.ContainsKey("number")) key = NormalizeStep(((int)o.Num("number")).ToString());
                if (key == null) continue;
                Put("step." + key + ".goal", First(o, "goal"));
                var why = o.Obj("why");
                if (why != null)
                {
                    Put("step." + key + ".why.kid", First(why, "kid", "simple", "text"));
                    Put("step." + key + ".why.engineer", First(why, "engineer", "eng"));
                }
                Put("step." + key + ".why.kid", First(o, "whyKid", "kid"));
                Put("step." + key + ".why.engineer", First(o, "whyEngineer", "engineer"));
            }
        }

        void ReadChecks(JsonObject checks)
        {
            if (checks == null) return;
            foreach (var kv in checks)
            {
                if (kv.Value is string s)
                {
                    Put("check." + kv.Key + ".kid", s);
                    continue;
                }
                if (!(kv.Value is JsonObject o)) continue;
                Put("check." + kv.Key + ".kid", First(o, "kid", "simple", "text"));
                Put("check." + kv.Key + ".engineer", First(o, "engineer", "eng"));
            }
        }

        static string NormalizeStep(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "1":
                case "mission":
                case "payload": return "mission";
                case "2":
                case "engines":
                case "engine":
                case "propellant": return "engines";
                case "3":
                case "tanks":
                case "tank":
                case "stages": return "tanks";
                case "4":
                case "aero":
                case "fairing": return "aero";
                case "5":
                case "recovery":
                case "landing": return "recovery";
                case "6":
                case "look": return "look";
                case "7":
                case "review":
                case "launch": return "review";
                default: return null;
            }
        }

        void Put(string id, string text)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(text)) return;
            strings[id] = text.Trim();
        }

        static string First(JsonObject o, params string[] keys)
        {
            if (o == null) return null;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!o.ContainsKey(keys[i])) continue;
                string text = TextOf(o[keys[i]]);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            return null;
        }

        static string TextOf(object value)
        {
            if (value is string s) return s;
            if (value is JsonObject o)
                return First(o, "en", "text", "kid", "value", "engineer");
            return null;
        }
    }
}
