// Designer layers. Simple is the default. Standard unlocks after the first orbit.
// Engineer is a Settings toggle and is available before that orbit.
using System.Collections.Generic;

namespace Kestrel.Core.Designer
{
    public static class Experience
    {
        public const string KidSchema = "Some of these numbers are outside what the shop can build. Try Make it fly, or turn on Engineer in Settings to see the details.";
        public const string Ready = "This design is ready to fly.";

        public static string[] Available(int orbitsCompleted, bool engineerEnabled)
        {
            var layers = new List<string> { "simple" };
            if (orbitsCompleted >= 1) layers.Add("standard");
            if (engineerEnabled) layers.Add("engineer");
            return layers.ToArray();
        }

        public static (string layer, string locked) Resolve(int orbitsCompleted, bool engineerEnabled, string layer)
        {
            int orbits = orbitsCompleted > 0 ? orbitsCompleted : 0;
            string want = string.IsNullOrEmpty(layer) ? "simple" : layer;
            string locked = null;
            if (want == "engineer" && !engineerEnabled)
            {
                locked = "engineer-toggle";
                want = orbits >= 1 ? "standard" : "simple";
            }
            if (want == "standard" && orbits < 1)
            {
                locked = "first-orbit";
                want = "simple";
            }
            return (want, locked);
        }

        public static Presented Present(EngineeringReport report, string layer)
        {
            var blocks = Clone(report.blocks);
            var warnings = Clone(report.warnings);
            var shown = blocks;
            if (layer != "engineer")
            {
                var nonSchema = blocks.FindAll(b => b.code != "schema");
                if (nonSchema.Count == 0 && blocks.Count > 0)
                    shown = new List<Issue> { new Issue { code = "schema", tab = "stages", msg = KidSchema } };
                else shown = nonSchema;
            }
            if (layer == "simple") warnings = new List<Issue>();
            string next = shown.Count > 0 ? shown[0].msg : warnings.Count > 0 ? warnings[0].msg : Ready;
            return new Presented
            {
                layer = layer, ok = report.ok, blocks = shown, warnings = warnings, next = next,
                showTune = layer == "engineer", showSchemaText = layer == "engineer", makeItFly = true,
            };
        }

        static List<Issue> Clone(List<Issue> src)
        {
            var o = new List<Issue>();
            if (src == null) return o;
            foreach (var x in src) o.Add(new Issue { code = x.code, tab = x.tab, msg = x.msg, index = x.index });
            return o;
        }
    }

    public sealed class Presented
    {
        public string layer, next;
        public bool ok, showTune, showSchemaText, makeItFly;
        public List<Issue> blocks, warnings;
    }
}
