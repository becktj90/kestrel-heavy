// The seed the live pad flies. A design may name one. Otherwise the session seed is the flight,
// which is how a Daily challenge reaches the sim instead of the design's default of 1.
using System;
using System.Globalization;
using Kestrel.Core.Economy;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public static class FlightSeed
    {
        /// <summary>JSON numbers stay exact only through <see cref="CareerStore.SafeInteger"/>.</summary>
        public static ulong Fit(ulong seed)
        {
            const ulong max = (ulong)CareerStore.SafeInteger;
            if (seed == 0) return 1;
            if (seed <= max) return seed;
            seed &= max;
            return seed == 0 ? 1UL : seed;
        }

        /// <summary>True when the document names a seed. A missing field is not a seed of 1.</summary>
        public static bool NamesSeed(JsonObject json)
        {
            if (json == null) return false;
            if (!string.IsNullOrEmpty(json.Str("seed"))) return true;
            if (!string.IsNullOrEmpty(json.Obj("mission")?.Str("seed"))) return true;
            if (!string.IsNullOrEmpty(json.Str("missionSeed"))) return true;
            return json.TryGetValue("missionSeed", out var raw) && raw is double d
                && d > 0 && d == Math.Truncate(d) && d < ulong.MaxValue;
        }

        /// <summary>The design's seed, or 1 when it names none. Same result the pad used to read inline.</summary>
        public static ulong Read(JsonObject json)
        {
            if (json == null) return 1;
            string s = json.Str("seed");
            if (string.IsNullOrEmpty(s)) s = json.Obj("mission")?.Str("seed");
            if (string.IsNullOrEmpty(s)) s = json.Str("missionSeed");
            if (!string.IsNullOrEmpty(s))
            {
                bool hex = false;
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')) { hex = true; break; }
                }
                if (!hex && ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong dec))
                    return dec == 0 ? 1UL : dec;
                if (hex && ulong.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong h))
                    return h == 0 ? 1UL : h;
            }
            if (json.TryGetValue("missionSeed", out var raw) && raw is double d && d > 0 && d == Math.Truncate(d) && d < ulong.MaxValue)
                return (ulong)d;
            return 1;
        }

        /// <summary>
        /// Seed for this ascent. An explicit design seed wins. Otherwise the session seed
        /// (today's Daily, when the board armed one) is what the briefing already disclosed.
        /// </summary>
        public static ulong ForAscent(JsonObject json, ulong? sessionSeed)
        {
            if (NamesSeed(json)) return Read(json);
            if (sessionSeed.HasValue && sessionSeed.Value != 0) return sessionSeed.Value;
            return 1;
        }
    }
}
