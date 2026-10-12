// Shared Hangar fixtures: the data/designer-*.json catalogs and fresh copies of presets and templates.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Tests
{
    public static class HangarFixtures
    {
        static HangarData data;
        public static HangarData Data => data ??= HangarData.LoadFromRepo(Golden.Root);

        /// <summary>A migrated, normalised copy of a starter template's design (safe to mutate).</summary>
        public static JsonObject Template(string id)
        {
            var t = Data.Template(id);
            var d = DesignMigration.Migrate(t.design);
            HangarEditor.Normalize(d);
            return d;
        }

        /// <summary>Raw preset JSON, migrated to the current schema.</summary>
        public static IEnumerable<JsonObject> Presets()
        {
            var doc = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json")));
            foreach (JsonObject p in doc.Arr("presets")) yield return DesignMigration.Migrate(JsonMut.CloneObject(p));
        }

        public static IEnumerable<JsonObject> AllDesigns()
        {
            foreach (var p in Presets()) yield return p;
            foreach (var t in Data.templates) yield return Template(t.id);
        }
    }
}
