// Hangar catalogs: starter templates, the Look tab (palette, finish slots, decal slots, decals),
// launch budgets, and the finish/livery ids the Look tab offers. All of it is data/*.json:
// designer-templates.json, designer-look.json, designer-budget.json, finishes.json, liveries.json.
// Engine-free so EditMode and dotnet run the same tests. Unlocks are earned by play only
// (start, orbits, landings, mission); there is no price, clock or random input anywhere here.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Hangar
{
    public sealed class UnlockRule
    {
        public string type = "start", missionId;
        public int count;

        public static readonly UnlockRule Start = new UnlockRule();

        public static UnlockRule FromJson(JsonObject o)
        {
            if (o == null) return Start;
            return new UnlockRule { type = o.Str("type", "start"), count = (int)o.Num("count"), missionId = o.Str("missionId") };
        }

        /// <summary>Plain words for the card, shown before the item is earned (rules are public).</summary>
        public string Describe()
        {
            switch (type)
            {
                case "orbits": return count == 1 ? "Reach orbit once" : $"Reach orbit {count} times";
                case "landings": return count == 1 ? "Land a booster once" : $"Land a booster {count} times";
                case "mission": return $"Complete mission {missionId}";
                default: return "Yours from the start";
            }
        }
    }

    /// <summary>What the player has done. Counters only grow, so anything earned stays earned.</summary>
    public sealed class HangarProgress
    {
        public int orbits, landings;
        public readonly HashSet<string> missions = new HashSet<string>();

        public static readonly HangarProgress None = new HangarProgress();

        public bool Earned(UnlockRule r)
        {
            if (r == null) return true;
            switch (r.type)
            {
                case "start": return true;
                case "orbits": return orbits >= r.count;
                case "landings": return landings >= r.count;
                case "mission": return r.missionId != null && missions.Contains(r.missionId);
                default: return false;
            }
        }
    }

    public sealed class StarterTemplate
    {
        public string id, name, kidBlurb, engineerBlurb;
        public int version = 1, order;
        public JsonObject design;
    }

    public sealed class DecalSlot
    {
        public string id, part;
        public double maxSizeM;
        public bool auto;
        public readonly List<string> content = new List<string>();
    }

    public sealed class DecalDef { public string id, label, content; public UnlockRule earn = UnlockRule.Start; }

    public sealed class FinishDef
    {
        public string id, label, albedo, tint;
        public double roughness, metallic;
        public readonly List<string> partTypes = new List<string>();
        public UnlockRule earn = UnlockRule.Start;
    }

    public sealed class LiveryDef { public string id, label, primary, accent, pattern; public UnlockRule earn = UnlockRule.Start; }

    public sealed class Budgets
    {
        public double defaultKc = 40000, nearFraction = 0.9;
        public readonly Dictionary<string, double> byOrbit = new Dictionary<string, double>();

        /// <summary>The design's own budgetKc (a mission sets it), else the orbit row, else the default.</summary>
        public double For(JsonObject design)
        {
            if (design != null && design.ContainsKey("budgetKc") && design["budgetKc"] is double own && own > 0) return own;
            var orbit = design?.Obj("payload")?.Str("orbit");
            return orbit != null && byOrbit.TryGetValue(orbit, out var kc) ? kc : defaultKc;
        }
    }

    public sealed class HangarData
    {
        public readonly List<StarterTemplate> templates = new List<StarterTemplate>();
        public readonly List<string> palette = new List<string>();
        public readonly List<string> finishSlots = new List<string>();
        public readonly List<DecalSlot> decalSlots = new List<DecalSlot>();
        public readonly List<DecalDef> decals = new List<DecalDef>();
        public readonly List<FinishDef> finishes = new List<FinishDef>();
        public readonly List<LiveryDef> liveries = new List<LiveryDef>();
        public readonly List<double> decalScales = new List<double>();
        public readonly List<int> decalRotations = new List<int>();
        public int maxDecals = 6;
        public Budgets budgets = new Budgets();

        public StarterTemplate Template(string id) => templates.Find(t => t.id == id);
        public DecalSlot DecalSlot(string id) => decalSlots.Find(s => s.id == id);
        public DecalDef Decal(string id) => decals.Find(d => d.id == id);
        public FinishDef Finish(string id) => finishes.Find(f => f.id == id);
        public LiveryDef Livery(string id) => liveries.Find(l => l.id == id);

        /// <summary>Finishes allowed on a slot. Locked ones are included; check <see cref="HangarProgress.Earned"/> to grey them.</summary>
        public List<FinishDef> FinishesFor(string slot) => finishes.FindAll(f => f.partTypes.Contains(slot));

        /// <summary>Decals that may sit in a slot (content match). Auto slots take none.</summary>
        public List<DecalDef> DecalsFor(string slotId)
        {
            var s = DecalSlot(slotId);
            if (s == null || s.auto) return new List<DecalDef>();
            return decals.FindAll(d => s.content.Contains(d.content));
        }

        public static HangarData FromJson(string templatesJson, string lookJson, string budgetJson, string finishesJson, string liveriesJson)
        {
            var h = new HangarData();
            if (!string.IsNullOrEmpty(templatesJson))
            {
                foreach (JsonObject t in MiniJson.ParseObject(templatesJson).Arr("templates") ?? new List<object>())
                    h.templates.Add(new StarterTemplate
                    {
                        id = t.Str("id"), name = t.Str("name"), version = (int)t.Num("version", 1), order = (int)t.Num("order"),
                        kidBlurb = t.Str("kidBlurb", ""), engineerBlurb = t.Str("engineerBlurb", ""), design = t.Obj("design"),
                    });
                h.templates.Sort((a, b) => a.order.CompareTo(b.order));
            }
            if (!string.IsNullOrEmpty(lookJson))
            {
                var look = MiniJson.ParseObject(lookJson);
                foreach (var x in look.Arr("palette") ?? new List<object>()) if (x is string s) h.palette.Add(s);
                foreach (var x in look.Arr("finishSlots") ?? new List<object>()) if (x is string s) h.finishSlots.Add(s);
                var lim = look.Obj("decalLimits");
                if (lim != null)
                {
                    h.maxDecals = (int)lim.Num("maxPerVehicle", 6);
                    foreach (var x in lim.Arr("scales") ?? new List<object>()) if (x is double d) h.decalScales.Add(d);
                    foreach (var x in lim.Arr("rotations") ?? new List<object>()) if (x is double d) h.decalRotations.Add((int)d);
                }
                foreach (JsonObject s in look.Arr("decalSlots") ?? new List<object>())
                {
                    var slot = new DecalSlot { id = s.Str("id"), part = s.Str("part"), maxSizeM = s.Num("maxSizeM"), auto = s.Bool("auto") };
                    foreach (var x in s.Arr("content") ?? new List<object>()) if (x is string c) slot.content.Add(c);
                    h.decalSlots.Add(slot);
                }
                foreach (JsonObject d in look.Arr("decals") ?? new List<object>())
                    h.decals.Add(new DecalDef { id = d.Str("id"), label = d.Str("label"), content = d.Str("content"), earn = UnlockRule.FromJson(d.Obj("earnRule")) });
            }
            if (h.decalScales.Count == 0) h.decalScales.AddRange(new[] { 0.5, 1, 1.5 });
            if (h.decalRotations.Count == 0) h.decalRotations.AddRange(new[] { 0, 90 });
            if (!string.IsNullOrEmpty(budgetJson))
            {
                var b = MiniJson.ParseObject(budgetJson);
                h.budgets.defaultKc = b.Num("defaultKc", 40000);
                h.budgets.nearFraction = b.Num("nearFraction", 0.9);
                var by = b.Obj("byOrbit");
                if (by != null) foreach (var k in by.Order) if (by[k] is double kc) h.budgets.byOrbit[k] = kc;
            }
            if (!string.IsNullOrEmpty(finishesJson))
            {
                foreach (JsonObject f in MiniJson.ParseObject(finishesJson).Arr("finishes") ?? new List<object>())
                {
                    var fd = new FinishDef
                    {
                        id = f.Str("id"), label = f.Str("label"), albedo = f.Str("albedo"), tint = f.Str("tint"),
                        roughness = f.Num("roughness"), metallic = f.Num("metallic"), earn = UnlockRule.FromJson(f.Obj("earnRule")),
                    };
                    foreach (var x in f.Arr("partTypes") ?? new List<object>()) if (x is string s) fd.partTypes.Add(s);
                    h.finishes.Add(fd);
                }
            }
            if (!string.IsNullOrEmpty(liveriesJson))
            {
                foreach (JsonObject l in MiniJson.ParseObject(liveriesJson).Arr("liveries") ?? new List<object>())
                    h.liveries.Add(new LiveryDef
                    {
                        id = l.Str("id"), label = l.Str("label"), primary = l.Str("primary"), accent = l.Str("accent"),
                        pattern = l.Str("pattern"), earn = UnlockRule.FromJson(l.Obj("earnRule")),
                    });
            }
            return h;
        }

        public static readonly string[] Files = { "designer-templates", "designer-look", "designer-budget", "finishes", "liveries" };

        /// <summary>Load from a checkout (tests, CLI). The player loads the same files from Resources/KestrelData.</summary>
        public static HangarData LoadFromRepo(string repoRoot)
        {
            string R(string n) => File.ReadAllText(Path.Combine(repoRoot, "data", n + ".json"));
            return FromJson(R(Files[0]), R(Files[1]), R(Files[2]), R(Files[3]), R(Files[4]));
        }
    }
}
