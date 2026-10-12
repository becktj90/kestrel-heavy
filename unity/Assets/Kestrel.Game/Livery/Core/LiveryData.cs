// Finishes + liveries data (data/finishes.json, data/liveries.json; schema agreed with UX in PR #142).
// Earned by play only: the loader REJECTS any price/cost/currency/purchase/expiry/time-limit/random/streak field,
// and the only unlock types are start, orbits, landings, mission. Engine-free (dotnet + Unity EditMode).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Kestrel.Livery
{
    public enum EarnType { Start, Orbits, Landings, Mission }

    public sealed class EarnRule
    {
        public EarnType Type; public int Count; public string MissionId;
        public static readonly EarnRule StartRule = new EarnRule { Type = EarnType.Start };
        /// <summary>Plain-language rule key for the card, shown before it's earned (#142: rules are public).</summary>
        public string DescriptionKey => Type switch
        {
            EarnType.Start => "earn.start",
            EarnType.Orbits => "earn.orbits",        // "Reach orbit {count} times"
            EarnType.Landings => "earn.landings",    // "Land a booster {count} times"
            _ => "earn.mission"                      // "Complete mission {missionId}"
        };
    }

    public sealed class Finish
    {
        public string Id, Label, Albedo, Tint; public string[] PartTypes; public double Roughness, Metallic; public EarnRule Earn;
    }

    public sealed class Livery
    {
        public string Id, Label, Primary, Accent, Pattern, CodexText; public EarnRule Earn;
    }

    /// <summary>Player progress the unlock checks read. Gameplay Dev owns the counters (save data); Art only reads.</summary>
    public sealed class PlayProgress
    {
        public int Orbits, Landings; public HashSet<string> MissionsCompleted = new HashSet<string>();
    }

    public static class Unlocks
    {
        /// <summary>Pure function of play progress. No clock, no randomness, no purchase state: once earned, earned forever
        /// (counters only grow).</summary>
        public static bool IsEarned(EarnRule r, PlayProgress p) => r.Type switch
        {
            EarnType.Start => true,
            EarnType.Orbits => p.Orbits >= r.Count,
            EarnType.Landings => p.Landings >= r.Count,
            EarnType.Mission => p.MissionsCompleted.Contains(r.MissionId),
            _ => false
        };
    }

    public static class LiveryCatalog
    {
        public static readonly string[] PartTypes = { "tank", "engine", "booster", "interstage", "fairing", "legs", "fins", "gridfins", "rcs" };
        public static readonly string[] Patterns = { "solid", "belt_stripes", "split_vertical", "checker_roll", "chevron_nose", "gradient_aft", "two_tone_stage" };
        public const int FinishCount20 = 12, LiveryCount20 = 16;

        static readonly Regex SnakeId = new Regex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$");
        static readonly Regex Hex = new Regex("^#[0-9A-Fa-f]{6}$");
        static readonly Regex KeyRx = new Regex("^[a-z][a-z0-9_]*(\\.[a-z0-9_]+)+$");
        static readonly Regex MissionRx = new Regex("^KH-[0-9]+$");

        /// <summary>Monetisation, scarcity and chance fields that must never appear (any depth, case-insensitive substring).</summary>
        public static readonly string[] ForbiddenKeyParts = { "price", "cost", "currency", "coin", "gem", "purchase", "iap", "sku", "store", "buy", "sale", "expir", "until", "deadline", "season", "limited", "random", "chance", "odds", "lootbox", "gacha", "streak", "ad_", "rewarded" };

        static readonly string[] FinishKeys = { "id", "label", "partTypes", "albedo", "roughness", "metallic", "tint", "earnRule" };
        static readonly string[] LiveryKeys = { "id", "label", "primary", "accent", "pattern", "earnRule", "codexText" };

        public static List<Finish> LoadFinishes(string json, List<string> errors)
        {
            var list = new List<Finish>();
            var root = Root(json, "finishes_v1", "finishes", errors, out var items); if (root == null) return list;
            var ids = new HashSet<string>();
            foreach (var o in items)
            {
                if (!(o is Dictionary<string, object> d)) { errors.Add("finish entry is not an object"); continue; }
                string where = $"finish {Get(d, "id") ?? "?"}";
                Keys(d, FinishKeys, where, errors);
                var f = new Finish
                {
                    Id = Id(d, where, ids, errors), Label = Key(d, "label", where, errors),
                    Albedo = Colour(d, "albedo", where, errors, required: true), Tint = Colour(d, "tint", where, errors, required: false),
                    Roughness = Unit(d, "roughness", where, errors), Metallic = Unit(d, "metallic", where, errors),
                    Earn = d.ContainsKey("earnRule") ? Rule(d["earnRule"], where, errors) : EarnRule.StartRule
                };
                var pts = new List<string>();
                if (d.TryGetValue("partTypes", out var pv) && pv is List<object> pl && pl.Count > 0)
                    foreach (var x in pl)
                        if (x is string ps && Array.IndexOf(PartTypes, ps) >= 0 && !pts.Contains(ps)) pts.Add(ps);
                        else errors.Add($"{where}: bad or duplicate partType '{x}'");
                else errors.Add($"{where}: partTypes must be a non-empty array");
                f.PartTypes = pts.ToArray();
                list.Add(f);
            }
            return list;
        }

        public static List<Livery> LoadLiveries(string json, List<string> errors)
        {
            var list = new List<Livery>();
            var root = Root(json, "liveries_v1", "liveries", errors, out var items); if (root == null) return list;
            var ids = new HashSet<string>();
            foreach (var o in items)
            {
                if (!(o is Dictionary<string, object> d)) { errors.Add("livery entry is not an object"); continue; }
                string where = $"livery {Get(d, "id") ?? "?"}";
                Keys(d, LiveryKeys, where, errors);
                var l = new Livery
                {
                    Id = Id(d, where, ids, errors), Label = Key(d, "label", where, errors), CodexText = Key(d, "codexText", where, errors),
                    Primary = Colour(d, "primary", where, errors, true), Accent = Colour(d, "accent", where, errors, true),
                    Pattern = Get(d, "pattern"),
                    Earn = d.ContainsKey("earnRule") ? Rule(d["earnRule"], where, errors) : null
                };
                if (l.Earn == null) errors.Add($"{where}: earnRule is required");
                if (Array.IndexOf(Patterns, l.Pattern) < 0) errors.Add($"{where}: unknown pattern '{l.Pattern}'");
                if (l.Primary != null && l.Primary.Equals(l.Accent, StringComparison.OrdinalIgnoreCase)) errors.Add($"{where}: primary and accent must differ");
                list.Add(l);
            }
            return list;
        }

        // ---- helpers ----
        static Dictionary<string, object> Root(string json, string schema, string arrayKey, List<string> errors, out List<object> items)
        {
            items = null;
            object parsed;
            try { parsed = MiniJson.Parse(json); } catch (FormatException e) { errors.Add("json: " + e.Message); return null; }
            if (!(parsed is Dictionary<string, object> root)) { errors.Add("root must be an object"); return null; }
            Forbidden(root, "", errors);
            Keys(root, new[] { "schema", arrayKey }, "root", errors);
            if (Get(root, "schema") != schema) errors.Add($"schema must be '{schema}'");
            if (!root.TryGetValue(arrayKey, out var a) || !(a is List<object> l)) { errors.Add($"'{arrayKey}' array missing"); return null; }
            items = l; return root;
        }

        static void Forbidden(object o, string path, List<string> errors)
        {
            if (o is Dictionary<string, object> d)
                foreach (var kv in d)
                {
                    var k = kv.Key.ToLowerInvariant();
                    foreach (var f in ForbiddenKeyParts) if (k.Contains(f)) { errors.Add($"forbidden field '{path}{kv.Key}' (earned-only: no price, expiry, random or streak)"); break; }
                    Forbidden(kv.Value, path + kv.Key + ".", errors);
                }
            else if (o is List<object> l) foreach (var x in l) Forbidden(x, path, errors);
        }

        static void Keys(Dictionary<string, object> d, string[] allowed, string where, List<string> errors)
        { foreach (var k in d.Keys) if (Array.IndexOf(allowed, k) < 0) errors.Add($"{where}: unknown field '{k}'"); }

        static string Get(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) ? v as string : null;

        static string Id(Dictionary<string, object> d, string where, HashSet<string> ids, List<string> errors)
        {
            var id = Get(d, "id");
            if (id == null || !SnakeId.IsMatch(id)) errors.Add($"{where}: id must be snake_case");
            else if (!ids.Add(id)) errors.Add($"{where}: duplicate id");
            return id;
        }

        static string Key(Dictionary<string, object> d, string k, string where, List<string> errors)
        { var v = Get(d, k); if (v == null || !KeyRx.IsMatch(v)) errors.Add($"{where}: {k} must be a localisation key like 'a.b'"); return v; }

        static string Colour(Dictionary<string, object> d, string k, string where, List<string> errors, bool required)
        {
            if (!d.ContainsKey(k)) { if (required) errors.Add($"{where}: {k} required"); return null; }
            var v = Get(d, k); if (v == null || !Hex.IsMatch(v)) errors.Add($"{where}: {k} must be #RRGGBB"); return v;
        }

        static double Unit(Dictionary<string, object> d, string k, string where, List<string> errors)
        {
            if (d.TryGetValue(k, out var v) && v is double x && x >= 0 && x <= 1) return x;
            errors.Add($"{where}: {k} must be a number in [0, 1]"); return 0;
        }

        static EarnRule Rule(object o, string where, List<string> errors)
        {
            if (!(o is Dictionary<string, object> d)) { errors.Add($"{where}: earnRule must be an object"); return null; }
            var t = Get(d, "type"); var r = new EarnRule();
            switch (t)
            {
                case "start": r.Type = EarnType.Start; Keys(d, new[] { "type" }, where + " earnRule", errors); break;
                case "orbits": case "landings":
                    r.Type = t == "orbits" ? EarnType.Orbits : EarnType.Landings;
                    Keys(d, new[] { "type", "count" }, where + " earnRule", errors);
                    if (d.TryGetValue("count", out var c) && c is double n && n >= 1 && n <= 1000 && Math.Abs(n - Math.Round(n)) < 1e-9) r.Count = (int)n;
                    else errors.Add($"{where}: earnRule.count must be an integer 1..1000");
                    break;
                case "mission":
                    r.Type = EarnType.Mission; Keys(d, new[] { "type", "missionId" }, where + " earnRule", errors);
                    r.MissionId = Get(d, "missionId");
                    if (r.MissionId == null || !MissionRx.IsMatch(r.MissionId)) errors.Add($"{where}: earnRule.missionId must look like KH-<n>");
                    break;
                default: errors.Add($"{where}: earnRule.type must be start|orbits|landings|mission"); return null;
            }
            return r;
        }
    }
}
