// Design schema migration. v0 → v1 fills defaults (the JS contract). v1 → v2 adds
// cluster.pattern = auto, interstage.shape = straight on upper stages, and nozzle = default.
// A missing interstageMass becomes 0 so C# and JS keep the same mass. A stored value, including 0, stays.
// v2 → v3 is an identity on every number except the version integer. v3 fields stay absent,
// which is the v2 behaviour. The default stop is still v2 so the existing goldens stay exact.
// Pass toVersion 3 to take the identity step.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Save
{
    public static class DesignMigration
    {
        public const int DesignVersion = 2;
        /// <summary>Newest schema this migration can write. Opt-in: <see cref="Migrate"/> stops at <see cref="DesignVersion"/> unless told otherwise.</summary>
        public const int LatestVersion = 3;

        /// <param name="toVersion">Stop after this version. The JS golden still ends at 1. Omit to reach <see cref="DesignVersion"/> (v2). Pass 3 for the v2→v3 identity.</param>
        public static JsonObject Migrate(JsonObject input, int toVersion = -1)
        {
            int target = toVersion < 0 ? DesignVersion : toVersion;
            if (target > LatestVersion) throw new InvalidOperationException($"design version {target} is newer than supported {LatestVersion}");
            var d = input == null ? new JsonObject() : JsonMut.CloneObject(input);
            int v = JsonMut.IsInteger(d.ContainsKey("version") ? d["version"] : null) ? (int)d.Num("version") : 0;
            if (v > LatestVersion) throw new InvalidOperationException($"design version {v} is newer than supported {LatestVersion}");
            while (v < target)
            {
                if (v == 0) d = Migrate0(d);
                else if (v == 1) d = Migrate1(d);
                else if (v == 2) d = Migrate2(d);
                else throw new InvalidOperationException($"no migration from design version {v}");
                v = (int)d.Num("version");
            }
            return d;
        }

        /// <summary>v2 → v3. Sets version to 3 and leaves every other key, including every number, untouched.</summary>
        static JsonObject Migrate2(JsonObject d)
        {
            JsonMut.Set(d, "version", 3d);
            return d;
        }

        static JsonObject Migrate1(JsonObject d)
        {
            var stages = d.Arr("stages");
            if (stages != null)
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    if (stages[i] is JsonObject s) FillPart(s, i > 0);
                }
            }
            var so = d.Obj("strapOns");
            if (so != null) FillPart(so, false);
            JsonMut.Set(d, "version", 2d);
            return d;
        }

        static void FillPart(JsonObject s, bool upper)
        {
            if (s.Obj("cluster") == null)
                JsonMut.Set(s, "cluster", Obj(("pattern", "auto")));
            else if (s.Obj("cluster").Str("pattern") == null)
                JsonMut.Set(s.Obj("cluster"), "pattern", "auto");
            if (upper)
            {
                if (s.Obj("interstage") == null)
                    JsonMut.Set(s, "interstage", Obj(("shape", "straight")));
                else if (s.Obj("interstage").Str("shape") == null)
                    JsonMut.Set(s.Obj("interstage"), "shape", "straight");
            }
            if (!s.ContainsKey("nozzle") || s["nozzle"] == null)
                JsonMut.Set(s, "nozzle", "default");
            if (!s.ContainsKey("interstageMass") || s["interstageMass"] == null)
                JsonMut.Set(s, "interstageMass", 0d);
        }

        static JsonObject Migrate0(JsonObject d)
        {
            var def = DefaultDesign();
            foreach (var k in d.Order) JsonMut.Set(def, k, JsonMut.Clone(d[k]));
            JsonMut.Set(def, "version", 1d);
            return def;
        }

        public static JsonObject DefaultDesign()
        {
            var d = new JsonObject();
            d.Add("version", 1d);
            d.Add("name", "Untitled");
            d.Add("stages", new List<object> { DefaultStage("Stage 1") });
            d.Add("strapOns", Obj(
                ("count", 0d), ("kind", "solid"), ("propellant", "solid-apcp"), ("engine", "srm-small"),
                ("engineCount", 1d), ("propMass", 0d), ("diameter", 1.6), ("material", "steel"), ("crossfeed", false)));
            d.Add("payload", Obj(("type", "leo-sat"), ("mass", 1500d), ("orbit", "leo400")));
            d.Add("fairing", Obj(("diameter", 2d), ("length", 5d), ("material", "composite"), ("jettisonAltitudeKm", 110d)));
            d.Add("aero", Obj(("nose", "ogive"), ("fins", false)));
            d.Add("landing", Obj(("legs", false), ("gridFins", false), ("rcs", false)));
            d.Add("avionics", Obj(("tier", "standard")));
            d.Add("safetyFactor", 1.4);
            d.Add("livery", Obj(("primary", "#f2f2f2"), ("accent", "#1a3c6e"), ("name", "")));
            return d;
        }

        static JsonObject DefaultStage(string name) => Obj(
            ("name", name), ("propellant", "kerolox"), ("engine", "gg-kerolox"), ("engineCount", 1d),
            ("propMass", 20000d), ("diameter", 2d), ("material", "al-li"), ("interstageMass", 0d),
            ("separationMass", 50d), ("landingPropMass", 0d));

        static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }
    }
}
