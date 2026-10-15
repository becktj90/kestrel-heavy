// Post-flight callout pool (S7). data/narration.json is the source of truth.
// A line is chosen by kind, cause, and design, then by seed, so the same flight repeats
// and a different seed can say it another way. Tokens are filled by the caller from the flight.
// Text only. No clips, no network, no timers.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public sealed class NarrationCause
    {
        public readonly string Kind, Title, Family;

        internal NarrationCause(string kind, string title, string family)
        {
            Kind = kind; Title = title; Family = family;
        }
    }

    public sealed class NarrationLine
    {
        public readonly string Id, Kind, When, Design, Kid, Eng;

        internal NarrationLine(string id, string kind, string when, string design, string kid, string eng)
        {
            Id = id; Kind = kind; When = when ?? ""; Design = design ?? ""; Kid = kid; Eng = eng;
        }
    }

    public sealed class NarrationPool
    {
        public const int Version = 1;

        public static readonly NarrationPool Empty = new NarrationPool(new List<NarrationCause>(), new List<NarrationLine>());

        readonly List<NarrationCause> causes;
        readonly List<NarrationLine> lines;
        readonly Dictionary<string, NarrationCause> byKind;

        NarrationPool(List<NarrationCause> causes, List<NarrationLine> lines)
        {
            this.causes = causes;
            this.lines = lines;
            byKind = new Dictionary<string, NarrationCause>();
            for (int i = 0; i < causes.Count; i++) byKind[causes[i].Kind] = causes[i];
        }

        public IReadOnlyList<NarrationCause> Causes => causes;
        public IReadOnlyList<NarrationLine> Lines => lines;

        public static NarrationPool Load(string repoRoot)
            => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "narration.json")));

        public static NarrationPool Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            if ((int)o.Num("version") != Version) throw new FormatException("narration version must be 1");
            var causeArr = o.Arr("causes") ?? throw new FormatException("narration.causes missing");
            var lineArr = o.Arr("lines") ?? throw new FormatException("narration.lines missing");
            var causes = new List<NarrationCause>();
            var seenKind = new HashSet<string>();
            for (int i = 0; i < causeArr.Count; i++)
            {
                if (!(causeArr[i] is JsonObject c)) throw new FormatException("narration.causes[" + i + "] is not an object");
                string kind = Need(c, "kind");
                string title = Need(c, "title");
                string family = Need(c, "family");
                if (!seenKind.Add(kind)) throw new FormatException("duplicate narration kind " + kind);
                causes.Add(new NarrationCause(kind, title, family));
            }
            var lines = new List<NarrationLine>();
            var seenId = new HashSet<string>();
            for (int i = 0; i < lineArr.Count; i++)
            {
                if (!(lineArr[i] is JsonObject e)) throw new FormatException("narration.lines[" + i + "] is not an object");
                string id = Need(e, "id");
                string kind = Need(e, "kind");
                if (!seenId.Add(id)) throw new FormatException("duplicate narration id " + id);
                if (!seenKind.Contains(kind)) throw new FormatException(id + " kind is not in causes");
                string kid = Need(e, "kid");
                string eng = Need(e, "eng");
                lines.Add(new NarrationLine(id, kind, e.Str("when") ?? "", e.Str("design") ?? "", kid, eng));
            }
            if (lines.Count == 0) throw new FormatException("narration.lines empty");
            return new NarrationPool(causes, lines);
        }

        public NarrationCause Cause(string kind)
            => kind != null && byKind.TryGetValue(kind, out var cause) ? cause : null;

        public string Title(string kind)
        {
            var cause = Cause(kind);
            return cause != null ? cause.Title : "Flight ended";
        }

        public string Family(string kind)
        {
            var cause = Cause(kind);
            return cause != null ? cause.Family : "";
        }

        /// <summary>
        /// The most specific lines for this kind, cause, and design tag, then one of them from the seed.
        /// The same seed returns the same id. A line whose when or design does not match is skipped.
        /// </summary>
        public NarrationLine Pick(string kind, string cause, string design, ulong seed)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            int best = -1;
            var matches = new List<NarrationLine>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Kind != kind) continue;
                int score = Score(line, cause, design);
                if (score < 0) continue;
                if (score > best)
                {
                    best = score;
                    matches.Clear();
                }
                if (score == best) matches.Add(line);
            }
            if (matches.Count == 0) return null;
            int index = (int)(Mix(seed) % (ulong)matches.Count);
            return matches[index];
        }

        public int VariantCount(string kind, string cause, string design)
        {
            int best = -1;
            int count = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Kind != kind) continue;
                int score = Score(line, cause, design);
                if (score < 0) continue;
                if (score > best) { best = score; count = 1; }
                else if (score == best) count++;
            }
            return count;
        }

        static int Score(NarrationLine line, string cause, string design)
        {
            int score = 0;
            bool whenAny = line.When.Length == 0 || line.When == "any";
            bool designAny = line.Design.Length == 0 || line.Design == "any";
            if (!whenAny)
            {
                if (line.When != (cause ?? "")) return -1;
                score += 2;
            }
            if (!designAny)
            {
                if (line.Design != (design ?? "")) return -1;
                score += 1;
            }
            return score;
        }

        static ulong Mix(ulong seed)
        {
            ulong z = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return z;
        }

        static string Need(JsonObject o, string key)
        {
            string s = o.Str(key);
            if (string.IsNullOrEmpty(s)) throw new FormatException("narration." + key + " missing");
            return s;
        }
    }
}
