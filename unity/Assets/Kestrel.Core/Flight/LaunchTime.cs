// Launch lighting for one flight. Deterministic and platform-independent:
// FNV-1a 64 of the mission id (UTF-8 bytes) and the seed, mapped through data/time-of-day.json.
// No System.Random and no string.GetHashCode (those differ by runtime and by process).
// web/js/flight/launch-time.js is the BigInt port; data/golden/launch-time.json binds the two.
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
        public const ulong FnvOffset = 14695981039346656037UL;
        public const ulong FnvPrime = 1099511628211UL;
        /// <summary>Resources path of the bundled table in a player build (CI copies data/*.json there).</summary>
        public const string ResourcePath = "KestrelData/time-of-day";

        static readonly object Gate = new object();
        static LaunchTimeTable _table;

        /// <summary>
        /// Install the weight table PickTimeOfDay uses. A player build calls this at boot with the
        /// bundled <c>Resources/KestrelData/time-of-day</c> text, because a device has no repo checkout.
        /// Pass null to drop it; the next pick then loads <c>data/time-of-day.json</c> from the checkout.
        /// </summary>
        public static void UseTable(LaunchTimeTable table)
        {
            lock (Gate) _table = table;
        }

        /// <summary>Parse <c>time-of-day.json</c> text and install it. Throws FormatException on a bad table.</summary>
        public static LaunchTimeTable UseJson(string json)
        {
            var table = LaunchTimeTable.Parse(json);
            UseTable(table);
            return table;
        }

        /// <summary>True when a table is installed or was already loaded.</summary>
        public static bool HasTable => _table != null;

        /// <summary>
        /// Plain FNV-1a 64 over the UTF-8 bytes of <paramref name="text"/> (null is ""). A lone
        /// surrogate encodes as U+FFFD (EF BF BD), the same as Encoding.UTF8 and the JS TextEncoder.
        /// Published vectors: "" cbf29ce484222325, "a" af63dc4c8601ec8c, "foobar" 85944171f73967e8.
        /// </summary>
        public static ulong Fnv1a64(string text) => MixUtf8(FnvOffset, text ?? "");

        /// <summary>
        /// FNV-1a 64 over the UTF-8 bytes of the mission id, then the seed as eight little-endian bytes.
        /// Explicit UTF-8, not UTF-16 code units, so any runtime or language that hashes bytes agrees.
        /// ASCII ids are one byte per character either way, so shipped KH-n picks are unchanged.
        /// </summary>
        public static ulong Hash(string missionId, ulong seed)
        {
            ulong h = MixUtf8(FnvOffset, missionId ?? "");
            for (int i = 0; i < 8; i++)
            {
                h ^= (seed >> (8 * i)) & 0xFFUL;
                h *= FnvPrime;
            }
            return h;
        }

        // UTF-8 encode inline so the hash allocates nothing.
        static ulong MixUtf8(ulong h, string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                int cp = s[i];
                if (cp >= 0xD800 && cp <= 0xDBFF && i + 1 < s.Length && s[i + 1] >= 0xDC00 && s[i + 1] <= 0xDFFF)
                {
                    cp = 0x10000 + ((cp - 0xD800) << 10) + (s[i + 1] - 0xDC00);
                    i++;
                }
                else if (cp >= 0xD800 && cp <= 0xDFFF)
                {
                    cp = 0xFFFD;
                }

                if (cp < 0x80)
                {
                    h = Mix(h, cp);
                }
                else if (cp < 0x800)
                {
                    h = Mix(h, 0xC0 | (cp >> 6));
                    h = Mix(h, 0x80 | (cp & 0x3F));
                }
                else if (cp < 0x10000)
                {
                    h = Mix(h, 0xE0 | (cp >> 12));
                    h = Mix(h, 0x80 | ((cp >> 6) & 0x3F));
                    h = Mix(h, 0x80 | (cp & 0x3F));
                }
                else
                {
                    h = Mix(h, 0xF0 | (cp >> 18));
                    h = Mix(h, 0x80 | ((cp >> 12) & 0x3F));
                    h = Mix(h, 0x80 | ((cp >> 6) & 0x3F));
                    h = Mix(h, 0x80 | (cp & 0x3F));
                }
            }
            return h;
        }

        static ulong Mix(ulong h, int b)
        {
            h ^= (ulong)(b & 0xFF);
            return h * FnvPrime;
        }

        /// <summary>
        /// Time of day for this flight. Same id and seed always return the same value.
        /// A pinned mission ignores the seed. An id with no row uses the default weights.
        /// Uses the table from <see cref="UseTable"/> when one is installed (player builds);
        /// otherwise loads <c>data/time-of-day.json</c> from the repo checkout (editor, dotnet tests).
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
