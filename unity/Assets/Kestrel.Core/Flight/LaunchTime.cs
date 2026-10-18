// Launch lighting for one flight. Deterministic and platform-independent:
// FNV-1a 64 of the mission id and the seed, mapped through data/time-of-day.json.
// No System.Random and no string.GetHashCode (those differ by runtime and by process).
// Named LaunchTimeOfDay because Flight.TimeOfDay is the weather-clock row on the briefing.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    /// <summary>
    /// Day, sunset, or night for one launch. Values match the Kit lighting presets
    /// (Day 0, Sunset 1, Night 2). Graphics owns the sun and the fog; this is only the pick.
    /// </summary>
    public enum LaunchTimeOfDay
    {
        Day = 0,
        Sunset = 1,
        Night = 2,
    }

    /// <summary>Weight table from <c>data/time-of-day.json</c>. A fixed row ignores the seed.</summary>
    public sealed class LaunchTimeTable
    {
        readonly struct Weights
        {
            public readonly int Day, Sunset, Night;
            public Weights(int day, int sunset, int night) { Day = day; Sunset = sunset; Night = night; }
            public int Total => Day + Sunset + Night;
        }

        readonly Weights _default;
        readonly Dictionary<string, LaunchTimeOfDay> _fixed;
        readonly Dictionary<string, Weights> _weights;

        LaunchTimeTable(Weights fallback, Dictionary<string, LaunchTimeOfDay> pinned, Dictionary<string, Weights> weights)
        {
            _default = fallback;
            _fixed = pinned;
            _weights = weights;
        }

        public static LaunchTimeTable Load(string repoRoot)
        {
            if (string.IsNullOrEmpty(repoRoot)) throw new ArgumentException("repo root is required", nameof(repoRoot));
            return Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "time-of-day.json")));
        }

        public static LaunchTimeTable Parse(string json)
        {
            var o = MiniJson.ParseObject(json ?? throw new ArgumentNullException(nameof(json)));
            if (o.ContainsKey("version") && (int)o.Num("version") != 1)
                throw new FormatException("time-of-day version must be 1");
            var defRow = o.Obj("default") ?? throw new FormatException("time-of-day default is missing");
            var fallback = ReadWeights(defRow, "default");
            if (fallback.Total <= 0) throw new FormatException("time-of-day default weights must sum to more than 0");

            var pinned = new Dictionary<string, LaunchTimeOfDay>();
            var weights = new Dictionary<string, Weights>();
            var missions = o.Obj("missions");
            if (missions != null)
            {
                foreach (var id in missions.Order)
                {
                    if (!(missions[id] is JsonObject row))
                        throw new FormatException("time-of-day mission " + id + " is not an object");
                    if (row.ContainsKey("fixed"))
                    {
                        if (!TryParse(row.Str("fixed"), out var pin))
                            throw new FormatException("time-of-day " + id + " fixed must be Day, Sunset, or Night");
                        pinned[id] = pin;
                    }
                    var w = ReadWeights(row, id);
                    if (w.Total > 0) weights[id] = w;
                }
            }
            return new LaunchTimeTable(fallback, pinned, weights);
        }

        /// <summary>True when this id has its own row (a pin, weights, or both).</summary>
        public bool Contains(string missionId)
        {
            string key = Key(missionId);
            return _fixed.ContainsKey(key) || _weights.ContainsKey(key);
        }

        public bool TryFixed(string missionId, out LaunchTimeOfDay value)
            => _fixed.TryGetValue(Key(missionId), out value);

        /// <summary>
        /// Chances used for the draw. A pinned mission reports 100 on that value and 0 on the others.
        /// A weighted row reports its table integers. An unknown id reports the default.
        /// </summary>
        public void Chances(string missionId, out int day, out int sunset, out int night)
        {
            if (_fixed.TryGetValue(Key(missionId), out var pin))
            {
                day = pin == LaunchTimeOfDay.Day ? 100 : 0;
                sunset = pin == LaunchTimeOfDay.Sunset ? 100 : 0;
                night = pin == LaunchTimeOfDay.Night ? 100 : 0;
                return;
            }
            var w = WeightsOf(missionId);
            day = w.Day;
            sunset = w.Sunset;
            night = w.Night;
        }

        public LaunchTimeOfDay Pick(string missionId, ulong seed)
        {
            if (_fixed.TryGetValue(Key(missionId), out var pin)) return pin;
            var w = WeightsOf(missionId);
            ulong slot = LaunchTime.Hash(missionId, seed) % (ulong)w.Total;
            if (slot < (ulong)w.Day) return LaunchTimeOfDay.Day;
            if (slot < (ulong)(w.Day + w.Sunset)) return LaunchTimeOfDay.Sunset;
            return LaunchTimeOfDay.Night;
        }

        Weights WeightsOf(string missionId)
        {
            if (_weights.TryGetValue(Key(missionId), out var w) && w.Total > 0) return w;
            return _default;
        }

        static string Key(string missionId) => missionId ?? "";

        static Weights ReadWeights(JsonObject row, string where)
            => new Weights(Weight(row, "day", where), Weight(row, "sunset", where), Weight(row, "night", where));

        static int Weight(JsonObject row, string key, string where)
        {
            if (!row.TryGetValue(key, out var raw) || raw == null) return 0;
            if (!(raw is double v) || double.IsNaN(v) || double.IsInfinity(v) || v < 0 || v > 1000000 || v != Math.Floor(v))
                throw new FormatException(where + "." + key + " must be a whole number from 0 to 1000000");
            return (int)v;
        }

        public static bool TryParse(string text, out LaunchTimeOfDay value)
        {
            value = LaunchTimeOfDay.Day;
            if (string.Equals(text, "Day", StringComparison.OrdinalIgnoreCase)) { value = LaunchTimeOfDay.Day; return true; }
            if (string.Equals(text, "Sunset", StringComparison.OrdinalIgnoreCase)) { value = LaunchTimeOfDay.Sunset; return true; }
            if (string.Equals(text, "Night", StringComparison.OrdinalIgnoreCase)) { value = LaunchTimeOfDay.Night; return true; }
            return false;
        }
    }

    public static class LaunchTime
    {
        static readonly object Gate = new object();
        static LaunchTimeTable _table;

        /// <summary>
        /// FNV-1a 64. The mission id is mixed as UTF-16 code units (ASCII ids are one byte each),
        /// then the seed as eight little-endian bytes. Same bits on every runtime.
        /// </summary>
        public static ulong Hash(string missionId, ulong seed)
        {
            ulong h = 14695981039346656037UL;
            string s = missionId ?? "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            for (int i = 0; i < 8; i++)
            {
                h ^= (seed >> (8 * i)) & 0xFFUL;
                h *= 1099511628211UL;
            }
            return h;
        }

        /// <summary>
        /// Time of day for this flight. Same id and seed always return the same value.
        /// A pinned mission ignores the seed. An id with no row uses the default weights.
        /// </summary>
        public static LaunchTimeOfDay PickTimeOfDay(string missionId, ulong seed)
        {
            var table = _table;
            if (table == null)
            {
                lock (Gate)
                {
                    if (_table == null) _table = LaunchTimeTable.Load(CheckoutRoot());
                    table = _table;
                }
            }
            return table.Pick(missionId, seed);
        }

        static string CheckoutRoot()
        {
            var env = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            if (!string.IsNullOrEmpty(env))
            {
                if (Directory.Exists(Path.Combine(env, "data", "golden"))) return env;
                throw new DirectoryNotFoundException("KESTREL_REPO_ROOT is not a Kestrel checkout");
            }
            try { return Catalog.FindRepoRoot(Directory.GetCurrentDirectory()); }
            catch (DirectoryNotFoundException) { }
            return Catalog.FindRepoRoot(AppContext.BaseDirectory);
        }
    }
}
