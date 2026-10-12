// In-game Codex. data/codex.json is the source of truth (kid text plus an engineer paragraph).
// Unity CI copies every data/*.json into Resources/KestrelData. Pass that text to Parse.
// This type does not touch a scene.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Codex
{
    public sealed class CodexEntry
    {
        public readonly string Id, Title, Kid, Engineer;
        public readonly IReadOnlyList<string> Sources;

        internal CodexEntry(string id, string title, string kid, string engineer, List<string> sources)
        {
            Id = id; Title = title; Kid = kid; Engineer = engineer; Sources = sources;
        }
    }

    public sealed class CodexBook
    {
        public const int KidMaxChars = 300;
        public readonly int Version;
        public readonly IReadOnlyList<CodexEntry> Entries;
        readonly Dictionary<string, CodexEntry> byId;

        CodexBook(int version, List<CodexEntry> entries, Dictionary<string, CodexEntry> byId)
        {
            Version = version; Entries = entries; this.byId = byId;
        }

        public CodexEntry ById(string id) => id != null && byId.TryGetValue(id, out var e) ? e : null;

        public static CodexBook Load(string repoRoot)
            => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "codex.json")));

        public static CodexBook Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            if (o.Num("version") != 1) throw new FormatException("codex version must be 1");
            var arr = o.Arr("entries") ?? throw new FormatException("codex.entries missing");
            var entries = new List<CodexEntry>();
            var byId = new Dictionary<string, CodexEntry>();
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is JsonObject e)) throw new FormatException("codex.entries[" + i + "] is not an object");
                string id = Need(e, "id", i), title = Need(e, "title", i), kid = Need(e, "kid", i), engineer = Need(e, "engineer", i);
                if (kid.Length > KidMaxChars) throw new FormatException(id + " kid text is " + kid.Length + " chars");
                var sources = Strings(e.Arr("sources"), id);
                if (sources.Count == 0) throw new FormatException(id + " sources empty");
                if (byId.ContainsKey(id)) throw new FormatException("duplicate codex id " + id);
                var entry = new CodexEntry(id, title, kid, engineer, sources);
                entries.Add(entry);
                byId.Add(id, entry);
            }
            return new CodexBook(1, entries, byId);
        }

        static string Need(JsonObject e, string key, int i)
        {
            string s = e.Str(key);
            if (string.IsNullOrEmpty(s)) throw new FormatException("codex.entries[" + i + "]." + key + " missing");
            return s;
        }

        static List<string> Strings(List<object> arr, string id)
        {
            if (arr == null) throw new FormatException(id + " sources missing");
            var o = new List<string>();
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is string s) || s.Length == 0) throw new FormatException(id + " sources[" + i + "] is not text");
                o.Add(s);
            }
            return o;
        }
    }
}
