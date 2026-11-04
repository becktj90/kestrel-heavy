using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>One <c>requires</c> entry: the key and what it must equal (bool, number, string, comparison string, or any-of list).</summary>
    public readonly struct NarrationRequirement
    {
        public readonly string Key;
        public readonly object Want;
        public NarrationRequirement(string key, object want) { Key = key; Want = want; }
    }

    /// <summary>
    /// The fact tags of one manifest clip (docs/narration-facts.md). A clip is either <c>neutral</c> (true on
    /// every rocket) or carries <c>requires</c> plus <c>claims</c>. Anything else is untagged and never eligible.
    /// </summary>
    public sealed class NarrationTake
    {
        public const string CallLayer = "call", ColorLayer = "color";

        public string Id;
        public string Event;
        public string Channel;
        public string Layer = CallLayer;
        public bool Interruptible;
        public bool Neutral;
        /// <summary>False when the clip has neither valid form. Such a clip is never picked.</summary>
        public bool Tagged;
        public int Claims;
        public List<NarrationRequirement> Requires = new List<NarrationRequirement>();

        public bool IsColor => Layer == ColorLayer;

        public static NarrationTake FromJson(JsonObject o)
        {
            var t = new NarrationTake
            {
                Id = o.Str("id"),
                Event = o.Str("event"),
                Channel = o.Str("channel"),
                Layer = o.Str("layer") == ColorLayer ? ColorLayer : CallLayer,
                Interruptible = o.Bool("interruptible"),
            };
            if (t.Layer == ColorLayer) t.Interruptible = true;
            bool neutral = o.Bool("neutral");
            var req = o.Obj("requires");
            var claims = o.Arr("claims");
            bool gated = req != null || claims != null;
            if (neutral && !gated) { t.Neutral = true; t.Tagged = true; return t; }
            if (!neutral && req != null && req.Count > 0 && claims != null && claims.Count > 0)
            {
                foreach (var key in req.Order) t.Requires.Add(new NarrationRequirement(key, req[key]));
                t.Claims = claims.Count;
                t.Tagged = true;
            }
            return t;
        }
    }

    /// <summary>
    /// The one place the manifest <c>requires</c> semantics live in C#. Same rules as the Node lint
    /// (tests/narration-facts-lint.test.cjs): AND over keys, a missing fact is false, an unknown key is
    /// false, numeric keys take a comparison string, a list is any-of.
    /// </summary>
    public static class NarrationRequires
    {
        static readonly Regex KeyRe = new Regex(
            @"^(stages|stage[1-9]\.engines|stage[1-9]\.propellant|strapons|strapons\.engines|strapons\.propellant|propellant\.all|recovery|legs|fairing|fairing\.halves|fins|finless|site|lit\.stage|lit\.engines|lit\.propellant|lit\.last)$",
            RegexOptions.CultureInvariant);
        static readonly Regex CmpRe = new Regex(@"^(>=|<=|!=|>|<)(-?\d+)$", RegexOptions.CultureInvariant);

        public static bool KnownKey(string key) => key != null && KeyRe.IsMatch(key);

        public static bool Holds(object have, object want)
        {
            if (have == null || want == null) return false;
            if (want is List<object> any)
            {
                for (int i = 0; i < any.Count; i++) if (Holds(have, any[i])) return true;
                return false;
            }
            if (want is string s)
            {
                var m = CmpRe.Match(s);
                if (m.Success)
                {
                    if (!(have is int n)) return false;
                    int x = int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    switch (m.Groups[1].Value)
                    {
                        case ">=": return n >= x;
                        case "<=": return n <= x;
                        case "!=": return n != x;
                        case ">": return n > x;
                        default: return n < x;
                    }
                }
                return have is string hs && hs == s;
            }
            if (want is bool wb) return have is bool hb && hb == wb;
            if (want is double wd) return have is int hi && hi == wd;
            return false;
        }

        /// <summary>Is the take true for these facts. Null facts leave only neutral takes.</summary>
        public static bool Eligible(NarrationTake take, NarrationFacts facts)
        {
            if (take == null || !take.Tagged) return false;
            if (take.Neutral) return true;
            if (facts == null || take.Requires.Count == 0) return false;
            for (int i = 0; i < take.Requires.Count; i++)
            {
                var r = take.Requires[i];
                if (!KnownKey(r.Key)) return false;
                if (!facts.TryGet(r.Key, out var have) || !Holds(have, r.Want)) return false;
            }
            return true;
        }
    }
}
