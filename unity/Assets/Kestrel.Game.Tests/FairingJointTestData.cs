// The real designs the fairing joint tests run over: every designer template, board starter, campaign winner and preset.
// Uses only Design and the data files, so the same tests compile against the old geometry (the before captures).
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    public static class FairingJointTestData
    {
        public static string Root => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());

        static void Add(List<KeyValuePair<string, Design>> rows, string name, object design)
        {
            if (design is JsonObject d && d.Arr("stages") != null && d.Arr("stages").Count > 0 && d.Obj("fairing") != null)
                rows.Add(new KeyValuePair<string, Design>(name, Design.FromJson(d)));
        }

        public static List<KeyValuePair<string, Design>> All()
        {
            string root = Root;
            var rows = new List<KeyValuePair<string, Design>>();
            foreach (JsonObject t in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "designer-templates.json"))).Arr("templates"))
                Add(rows, "template:" + t.Str("id"), t.Obj("design"));
            foreach (JsonObject m in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "board-starters.json"))).Arr("missions"))
            {
                var ds = m.Arr("designs"); if (ds == null) continue;
                for (int i = 0; i < ds.Count; i++) Add(rows, "board:" + m.Str("id") + "#" + i, ds[i]);
            }
            foreach (JsonObject m in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json"))).Arr("missions"))
            {
                var ds = m.Arr("designs"); if (ds == null) continue;
                for (int i = 0; i < ds.Count; i++) Add(rows, "winner:" + m.Str("id") + "#" + i, ds[i]);
            }
            foreach (var p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "presets.json"))).Arr("presets"))
                if (p is JsonObject po) Add(rows, "preset:" + po.Str("id"), po);
            return rows;
        }

        public static Design Winner(string id)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "golden", "campaign-winners.json")));
            foreach (JsonObject row in file.Arr("missions"))
                if (row.Str("id") == id) return Design.FromJson((JsonObject)row.Arr("designs")[0]);
            Assert.Fail("no campaign winner " + id);
            return null;
        }

        public static Design Preset(string id)
        {
            foreach (var p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "presets.json"))).Arr("presets"))
                if (p is JsonObject po && po.Str("id") == id) return Design.FromJson(po);
            Assert.Fail("no preset " + id);
            return null;
        }
    }
}
