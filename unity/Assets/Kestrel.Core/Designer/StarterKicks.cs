// DECISIONS #85 (Trevor via Claude, 11-07): the Starter screen is forgiving. Every Starter setting with enough delta-v for
// its mission must reach the mission band on the live pad, so the Starter flight picks its pitch kick per setting instead of
// flying the base rocket's one kick for all 30 (or 90) settings. The upper stage already cuts off on the mission's target
// energy for every setting (#313); the kick sets the stage-1 loft, which decides the flight-path angle at that cutoff (a
// kick too small for a heavier rocket climbs steeply and falls back; too large dives under the band).
//
// The kicks are a precomputed table, data/starter-kicks.json, written by tools/starter-kicks: for each mission and each
// non-default setting, the kick at the middle of the widest run of kicks whose designer practice flight (ReferenceAscent:
// the seed-1 briefing wind and pad, BatchDt) passes the mission and whose live pad flight (LiveDt, 1/60 s frames) passes
// too. Physics is unchanged: the kick is an ordinary design value (guidance.pitchKickDeg, inside designer-limits) that
// StarterRocket.Build writes into the rocket, so the predictor, the timeline, the live pad and the report all fly the same
// JSON. A setting with no winning kick keeps the base kick and reads its cause before launch (Short, Breaks up, ...).
// The default setting is never in the table: it flies the campaign winner byte for byte.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    public sealed class StarterKicks
    {
        public const string FileName = "starter-kicks.json";
        /// <summary>Resources path of the shipped copy in a player build (CI copies data/*.json there).</summary>
        public const string ResourcePath = "KestrelData/starter-kicks";

        readonly Dictionary<string, Dictionary<string, double>> kicks = new Dictionary<string, Dictionary<string, double>>();
        readonly HashSet<string> guided = new HashSet<string>();
        readonly Dictionary<string, double> baseKicks = new Dictionary<string, double>();

        /// <summary>The table a player build installs at boot (StarterBasesBoot). Null: <see cref="Current"/> reads the checkout.</summary>
        public static StarterKicks Active;

        /// <summary>tools/starter-kicks sets this while it searches, so every setting flies the base rocket's own kick.</summary>
        public static bool Disabled;

        static readonly object Gate = new object();
        static StarterKicks checkout;
        static bool checkoutRead;

        /// <summary>The table in use: <see cref="Active"/>, else data/starter-kicks.json from the checkout (read once), else null.</summary>
        public static StarterKicks Current
        {
            get
            {
                if (Disabled) return null;
                var a = Active;
                if (a != null) return a;
                lock (Gate)
                {
                    if (!checkoutRead)
                    {
                        checkoutRead = true;
                        try
                        {
                            string root = Catalog.RepoRootOrNull();
                            string path = root != null ? Path.Combine(root, "data", FileName) : null;
                            if (path != null && File.Exists(path)) checkout = Parse(File.ReadAllText(path));
                        }
                        catch (Exception) { checkout = null; }
                    }
                    return checkout;
                }
            }
        }

        /// <summary>Drop the cached checkout copy (the generator rewrites the file; tests).</summary>
        public static void Forget()
        {
            lock (Gate) { checkout = null; checkoutRead = false; }
        }

        /// <summary>{"missions":{"m3":{"baseKickDeg":8,"kicks":{"kerosene-2-f0-b0":9.25,...}}}}</summary>
        public static StarterKicks Parse(string json)
        {
            var t = new StarterKicks();
            if (string.IsNullOrWhiteSpace(json)) return t;
            var o = MiniJson.ParseObject(json);
            if (!(o.Obj("missions") is JsonObject missions)) return t;
            foreach (var id in missions.Order)
            {
                if (!(missions.Obj(id) is JsonObject row)) continue;
                if (row.ContainsKey("baseKickDeg")) t.baseKicks[id] = row.Num("baseKickDeg");
                var map = new Dictionary<string, double>();
                if (row.Obj("kicks") is JsonObject ks)
                    foreach (var key in ks.Order)
                    {
                        if (!ks.TryGetValue(key, out var v)) continue;
                        if (v is double d && !double.IsNaN(d)) map[key] = d;
                        else if (v is JsonObject e && e.ContainsKey("kickDeg"))
                        {
                            map[key] = e.Num("kickDeg");
                            if (e.Bool("guidanceComputer")) t.guided.Add(id + "/" + key);
                        }
                    }
                t.kicks[id] = map;
            }
            return t;
        }

        /// <summary>The kick the table picked for a non-default setting of a mission. False: fly the base rocket's kick.</summary>
        public bool TryKick(string missionId, string settingKey, out double kickDeg)
        {
            kickDeg = double.NaN;
            return missionId != null && settingKey != null && kicks.TryGetValue(missionId, out var map) && map.TryGetValue(settingKey, out kickDeg);
        }

        /// <summary>True when the table fits the upper-stage guidance computer (#341, its real mass and price) to this setting.</summary>
        public bool GuidanceComputer(string missionId, string settingKey) => guided.Contains(missionId + "/" + settingKey);

        /// <summary>The base rocket's kick the table was generated against (NaN when not recorded).</summary>
        public double BaseKick(string missionId) => missionId != null && baseKicks.TryGetValue(missionId, out var k) ? k : double.NaN;

        /// <summary>Number of settings with a picked kick on a mission.</summary>
        public int Count(string missionId) => missionId != null && kicks.TryGetValue(missionId, out var map) ? map.Count : 0;

        /// <summary>Missions in the table.</summary>
        public IEnumerable<string> Missions => kicks.Keys;

        /// <summary>Write <paramref name="kickDeg"/> into a design JSON's guidance.pitchKickDeg (creating guidance if needed).</summary>
        public static void Apply(JsonObject design, double kickDeg)
        {
            if (design == null || double.IsNaN(kickDeg)) return;
            var g = design.Obj("guidance");
            if (g == null) { g = new JsonObject(); JsonMut.Set(design, "guidance", g); }
            JsonMut.Set(g, "pitchKickDeg", kickDeg);
        }

        /// <summary>Fit the #341 upper-stage guidance computer (avionics.guidanceComputer) to a design JSON.</summary>
        public static void FitGuidanceComputer(JsonObject design)
        {
            if (design == null) return;
            var av = design.Obj("avionics");
            if (av == null) { av = new JsonObject(); JsonMut.Set(av, "tier", "standard"); JsonMut.Set(design, "avionics", av); }
            JsonMut.Set(av, "guidanceComputer", true);
        }

        /// <summary>True when a design JSON carries the guidance computer.</summary>
        public static bool HasGuidanceComputer(JsonObject design) => design?.Obj("avionics")?.Bool("guidanceComputer") ?? false;

        /// <summary>The kick a design JSON flies (guidance.pitchKickDeg), or NaN when it sets none.</summary>
        public static double KickOf(JsonObject design)
        {
            var g = design?.Obj("guidance");
            return g != null && g.ContainsKey("pitchKickDeg") ? g.Num("pitchKickDeg") : double.NaN;
        }
    }
}
