// Design schema migration. Port of migrateDesign() in web/js/vehicle/design.js.
// Only v0 → v1 exists. There is no design v2; bumping DESIGN_VERSION would invalidate every preset.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Save
{
    public static class DesignMigration
    {
        public const int DesignVersion = 1;

        public static JsonObject Migrate(JsonObject input)
        {
            var d = input == null ? new JsonObject() : JsonMut.CloneObject(input);
            int v = JsonMut.IsInteger(d.ContainsKey("version") ? d["version"] : null) ? (int)d.Num("version") : 0;
            if (v > DesignVersion) throw new InvalidOperationException($"design version {v} is newer than supported {DesignVersion}");
            while (v < DesignVersion)
            {
                if (v != 0) throw new InvalidOperationException($"no migration from design version {v}");
                d = Migrate0(d);
                v = (int)d.Num("version");
            }
            return d;
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
