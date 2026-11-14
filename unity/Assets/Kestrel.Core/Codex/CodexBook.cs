// In-game Codex. data/codex.json is the source of truth (kid text plus an engineer paragraph).
// Real-world history lives only in an entry's optional History list; every fact names at least one
// ref id from the book's Refs (title plus https url). The content guards are in CodexTests.
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
        public readonly IReadOnlyList<CodexFact> History;

        internal CodexEntry(string id, string title, string kid, string engineer, List<string> sources, List<CodexFact> history)
        {
            Id = id; Title = title; Kid = kid; Engineer = engineer; Sources = sources; History = history;
        }
    }

    /// <summary>One real-world fact. Refs are ids into CodexBook.Refs.</summary>
    public sealed class CodexFact
    {
        public readonly string Text;
        public readonly IReadOnlyList<string> Refs;
        internal CodexFact(string text, List<string> refs) { Text = text; Refs = refs; }
    }

    /// <summary>One real-world source a history fact cites.</summary>
    public sealed class CodexRef
    {
        public readonly string Id, Title, Url;
        internal CodexRef(string id, string title, string url) { Id = id; Title = title; Url = url; }
    }

    public sealed class CodexBook
    {
        public const int KidMaxChars = 300;
        public readonly int Version;
        public readonly IReadOnlyList<CodexEntry> Entries;
        public readonly IReadOnlyDictionary<string, CodexRef> Refs;
        readonly Dictionary<string, CodexEntry> byId;

        CodexBook(int version, List<CodexEntry> entries, Dictionary<string, CodexEntry> byId, Dictionary<string, CodexRef> refs)
        {
            Version = version; Entries = entries; this.byId = byId; Refs = refs;
        }

        public CodexEntry ById(string id) => id != null && byId.TryGetValue(id, out var e) ? e : null;

        public static CodexBook Load(string repoRoot)
            => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "codex.json")));

        public static CodexBook Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            if (o.Num("version") != 1) throw new FormatException("codex version must be 1");
            var refs = new Dictionary<string, CodexRef>();
            var refsObj = o.Obj("refs");
            if (o.ContainsKey("refs") && refsObj == null) throw new FormatException("codex.refs is not an object");
            if (refsObj != null)
                foreach (var key in new List<string>(refsObj.Keys))
                {
                    if (!(refsObj[key] is JsonObject r)) throw new FormatException("codex.refs." + key + " is not an object");
                    string rt = r.Str("title"), ru = r.Str("url");
                    if (string.IsNullOrEmpty(rt)) throw new FormatException("codex.refs." + key + ".title missing");
                    if (ru == null || !ru.StartsWith("https://", StringComparison.Ordinal)) throw new FormatException("codex.refs." + key + ".url must be https");
                    refs.Add(key, new CodexRef(key, rt, ru));
                }
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
                var history = History(e, id, refs);
                var entry = new CodexEntry(id, title, kid, engineer, sources, history);
                entries.Add(entry);
                byId.Add(id, entry);
            }
            return new CodexBook(1, entries, byId, refs);
        }

        static List<CodexFact> History(JsonObject e, string id, Dictionary<string, CodexRef> refs)
        {
            var o = new List<CodexFact>();
            if (!e.ContainsKey("history")) return o;
            var arr = e.Arr("history") ?? throw new FormatException(id + " history is not a list");
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is JsonObject h)) throw new FormatException(id + " history[" + i + "] is not an object");
                string text = h.Str("text");
                if (string.IsNullOrEmpty(text)) throw new FormatException(id + " history[" + i + "].text missing");
                var ra = h.Arr("refs");
                if (ra == null || ra.Count == 0) throw new FormatException(id + " history[" + i + "] has no source ref");
                var ids = new List<string>();
                foreach (var x in ra)
                {
                    if (!(x is string r) || !refs.ContainsKey(r)) throw new FormatException(id + " history[" + i + "] cites an unknown ref");
                    ids.Add(r);
                }
                o.Add(new CodexFact(text, ids));
            }
            return o;
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
