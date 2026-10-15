// Persisted stepperProgress on the design JSON. Absent on old saves: step 1, All parts, nothing visited.
// Hangar snapshots the JsonObject, so this key survives save and migration. Design.ToJson drops it;
// the next load then takes the same safe default.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer.Stepper
{
    public sealed class StepperProgress
    {
        public const string Field = "stepperProgress";

        public DesignerStep Current = DesignerStep.Mission;
        public readonly HashSet<DesignerStep> Visited = new HashSet<DesignerStep>();
        /// <summary>Steps whose check changed because an earlier step was edited. Cleared when the player opens the step.</summary>
        public readonly HashSet<DesignerStep> Affected = new HashSet<DesignerStep>();
        public StepperView View = StepperView.AllParts;
        /// <summary>
        /// Null means "use the depth default": ON in Simple, OFF in Engineer.
        /// A stored true or false wins in either depth.
        /// </summary>
        public bool? AutoAssist;
        /// <summary>True only when the design has no stepperProgress field.</summary>
        public bool Legacy { get; private set; }

        public bool AutoAssistOn(DesignerDepth depth) => AutoAssist ?? depth == DesignerDepth.Simple;

        public static StepperProgress FreshGuided()
        {
            var p = new StepperProgress();
            p.Current = DesignerStep.Mission;
            p.Visited.Add(DesignerStep.Mission);
            p.View = StepperView.Guided;
            p.Legacy = false;
            return p;
        }

        /// <summary>Missing, wrong-typed, or out-of-range data becomes step 1. It never throws.</summary>
        public static StepperProgress Read(JsonObject design)
        {
            var o = design?.Obj(Field);
            if (o == null) return LegacyDefault();
            var p = new StepperProgress { Legacy = false, View = StepperView.Guided };
            if (!Integer(o, "current", out int current) || !StepperCatalog.TryStep(current, out var step))
                step = DesignerStep.Mission;
            p.Current = step;
            var visited = o.Arr("visited");
            if (visited != null)
            {
                for (int i = 0; i < visited.Count; i++)
                {
                    if (visited[i] is double n && n == Math.Truncate(n) && StepperCatalog.TryStep((int)n, out var s))
                        p.Visited.Add(s);
                }
            }
            string view = o.Str("view");
            if (view == "all-parts") p.View = StepperView.AllParts;
            else if (view == "guided") p.View = StepperView.Guided;
            if (o.ContainsKey("autoAssist") && o["autoAssist"] is bool assist) p.AutoAssist = assist;
            return p;
        }

        /// <summary>Writes current, visited, view, and autoAssist when set. Other design keys stay.</summary>
        public void Write(JsonObject design)
        {
            if (design == null) throw new ArgumentNullException(nameof(design));
            Visited.Add(Current);
            var o = new JsonObject();
            o.Add("current", (double)(int)Current);
            var visited = new List<object>();
            for (int n = 1; n <= StepperCatalog.Count; n++)
            {
                var step = (DesignerStep)n;
                if (Visited.Contains(step)) visited.Add((double)n);
            }
            o.Add("visited", visited);
            o.Add("view", View == StepperView.AllParts ? "all-parts" : "guided");
            if (AutoAssist.HasValue) o.Add("autoAssist", AutoAssist.Value);
            JsonMut.Set(design, Field, o);
            Legacy = false;
        }

        public void EnterGuided()
        {
            View = StepperView.Guided;
            Legacy = false;
            Visited.Add(Current);
        }

        public void ShowAllParts() => View = StepperView.AllParts;

        static StepperProgress LegacyDefault()
            => new StepperProgress
            {
                Current = DesignerStep.Mission,
                View = StepperView.AllParts,
                Legacy = true,
            };

        static bool Integer(JsonObject o, string key, out int n)
        {
            n = 0;
            if (o == null || !o.TryGetValue(key, out var raw) || !(raw is double d)) return false;
            if (double.IsNaN(d) || double.IsInfinity(d) || d != Math.Truncate(d)) return false;
            if (d < int.MinValue || d > int.MaxValue) return false;
            n = (int)d;
            return true;
        }
    }
}
