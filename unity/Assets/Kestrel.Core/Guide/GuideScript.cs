// Guide script for Mabel "Thrum" Pike. data/guide.json is the source of truth.
// Unity CI copies every data/*.json into Resources/KestrelData. Pass that text to Parse.
// Lines bind to narration bus ids and MissionEvent.links.narration. No second event type.
// This type does not touch a scene.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Guide
{
    public static class GuidePriority
    {
        public const string Safety = "safety";
        public const string Decision = "decision";
        public const string Failure = "failure";
        public const string Chatter = "chatter";

        public static int Rank(string priority)
        {
            if (priority == Safety) return 4;
            if (priority == Decision) return 3;
            if (priority == Failure) return 2;
            if (priority == Chatter) return 1;
            return 0;
        }
    }

    public sealed class GuideCard
    {
        public readonly string Id, Kid, Eng;

        internal GuideCard(string id, string kid, string eng)
        {
            Id = id; Kid = kid; Eng = eng;
        }
    }

    /// <summary>One beat. Cards play in order. Kid text is the whole experience; eng adds the number.</summary>
    public sealed class GuideLine
    {
        public readonly string Id, Trigger, When, Speaker, Priority;
        public readonly bool Once;
        public readonly IReadOnlyList<GuideCard> Cards;
        public readonly IReadOnlyList<string> Actions;

        internal GuideLine(string id, string trigger, string when, string speaker, string priority, bool once, List<GuideCard> cards, List<string> actions)
        {
            Id = id; Trigger = trigger; When = when ?? ""; Speaker = speaker; Priority = priority; Once = once;
            Cards = cards; Actions = actions;
        }

        public bool Safety => Priority == GuidePriority.Safety;
    }

    public sealed class GuideCharacter
    {
        public readonly string Id, Name, Call, Role, Wrench, Kestrel;

        internal GuideCharacter(string id, string name, string call, string role, string wrench, string kestrel)
        {
            Id = id; Name = name; Call = call; Role = role; Wrench = wrench; Kestrel = kestrel;
        }
    }

    public sealed class GuideScript
    {
        public const int Version = 1;
        public const int MaxCardChars = 90;
        public const int MaxWordsPerSentence = 14;
        public const int MaxExclamations = 1;
        public const string SpeakerId = "thrum";

        public readonly GuideCharacter Character;
        public readonly IReadOnlyList<GuideLine> Lines;
        readonly Dictionary<string, GuideLine> byId;

        GuideScript(GuideCharacter character, List<GuideLine> lines, Dictionary<string, GuideLine> byId)
        {
            Character = character; Lines = lines; this.byId = byId;
        }

        public GuideLine ById(string id) => id != null && byId.TryGetValue(id, out var line) ? line : null;

        public static GuideScript Load(string repoRoot)
            => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "guide.json")));

        public static GuideScript Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            if ((int)o.Num("version") != Version) throw new FormatException("guide version must be 1");
            var speeds = o.Obj("speeds") ?? throw new FormatException("guide.speeds missing");
            if (speeds.Num("slow") != GuideSettings.SlowCharsPerSecond
                || speeds.Num("normal") != GuideSettings.NormalCharsPerSecond
                || speeds.Num("fast") != GuideSettings.FastCharsPerSecond
                || speeds.Num("instant") != 0)
                throw new FormatException("guide speeds drifted");
            var who = o.Obj("character") ?? throw new FormatException("guide.character missing");
            var character = new GuideCharacter(
                Need(who, "id"), Need(who, "name"), Need(who, "call"),
                Need(who, "role"), Need(who, "wrench"), Need(who, "kestrel"));
            if (character.Name != "Mabel Thrum Pike") throw new FormatException("guide character must be Mabel Thrum Pike");
            var arr = o.Arr("lines") ?? throw new FormatException("guide.lines missing");
            var lines = new List<GuideLine>();
            var byId = new Dictionary<string, GuideLine>();
            var seenTrigger = new HashSet<string>();
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is JsonObject e)) throw new FormatException("guide.lines[" + i + "] is not an object");
                string id = Need(e, "id");
                string trigger = Need(e, "trigger");
                string when = e.Str("when") ?? "";
                string speaker = Need(e, "speaker");
                string priority = Need(e, "priority");
                if (speaker != SpeakerId) throw new FormatException(id + " speaker must be thrum");
                if (GuidePriority.Rank(priority) == 0) throw new FormatException(id + " priority is unknown");
                if (byId.ContainsKey(id)) throw new FormatException("duplicate guide id " + id);
                string key = trigger + "\n" + when;
                if (!seenTrigger.Add(key)) throw new FormatException("duplicate guide trigger " + trigger + " when " + when);
                var cards = Cards(e.Arr("cards"), id);
                var actions = Actions(e.Arr("actions"), id);
                bool once = e.Bool("once");
                var line = new GuideLine(id, trigger, when, speaker, priority, once, cards, actions);
                lines.Add(line);
                byId.Add(id, line);
            }
            if (lines.Count == 0) throw new FormatException("guide.lines empty");
            return new GuideScript(character, lines, byId);
        }

        /// <summary>
        /// Bus type, abort-kind, or a line id. A specific <paramref name="when"/> never falls through to another beat.
        /// </summary>
        public GuideLine Match(string trigger, string when)
        {
            if (string.IsNullOrEmpty(trigger)) return null;
            string bus = trigger;
            string qualifier = when ?? "";
            if (bus.StartsWith("abort-", StringComparison.Ordinal))
            {
                if (qualifier.Length == 0) qualifier = bus.Substring("abort-".Length);
                bus = "abort";
            }
            GuideLine byIdent = null;
            GuideLine exact = null;
            GuideLine open = null;
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (line.Id == trigger) byIdent = line;
                if (line.Trigger != bus && line.Id != bus) continue;
                if (line.When == qualifier) exact = line;
                else if (line.When.Length == 0) open = line;
            }
            if (exact != null) return exact;
            if (qualifier.Length == 0) return open ?? byIdent;
            return byIdent != null && byIdent.Id == trigger ? byIdent : null;
        }

        static List<GuideCard> Cards(List<object> arr, string id)
        {
            if (arr == null || arr.Count == 0) throw new FormatException(id + " cards missing");
            var cards = new List<GuideCard>();
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is JsonObject c)) throw new FormatException(id + " cards[" + i + "] is not an object");
                string cid = c.Str("id");
                if (string.IsNullOrEmpty(cid)) cid = id + ".c" + (i + 1);
                string kid = c.Str("kid");
                string eng = c.Str("eng");
                if (string.IsNullOrEmpty(eng)) eng = c.Str("engineer");
                if (string.IsNullOrEmpty(kid)) throw new FormatException(cid + " kid missing");
                if (string.IsNullOrEmpty(eng)) throw new FormatException(cid + " eng missing");
                CheckVoice(cid, "kid", kid, false);
                CheckVoice(cid, "eng", eng, true);
                cards.Add(new GuideCard(cid, kid, eng));
            }
            return cards;
        }

        static List<string> Actions(List<object> arr, string id)
        {
            var actions = new List<string>();
            if (arr == null) return actions;
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is string s) || s.Length == 0) throw new FormatException(id + " actions[" + i + "] is not text");
                CheckVoice(id + " action", "label", s, false);
                actions.Add(s);
            }
            return actions;
        }

        static void CheckVoice(string id, string field, string text, bool needNumber)
        {
            if (text.Length > MaxCardChars) throw new FormatException(id + " " + field + " is " + text.Length + " chars");
            if (Exclamations(text) > MaxExclamations) throw new FormatException(id + " " + field + " has more than one !");
            int words = MaxWords(text);
            if (words > MaxWordsPerSentence) throw new FormatException(id + " " + field + " has a " + words + "-word sentence");
            if (needNumber && !HasDigit(text)) throw new FormatException(id + " eng needs a real number");
            if (Shouts(text)) throw new FormatException(id + " " + field + " shouts");
            if (Banned(text)) throw new FormatException(id + " " + field + " uses a banned phrase");
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 32 || c > 126) throw new FormatException(id + " " + field + " is not plain text");
            }
        }

        static int Exclamations(string text)
        {
            int n = 0;
            for (int i = 0; i < text.Length; i++) if (text[i] == '!') n++;
            return n;
        }

        static bool HasDigit(string text)
        {
            for (int i = 0; i < text.Length; i++) if (text[i] >= '0' && text[i] <= '9') return true;
            return false;
        }

        public static int MaxWords(string text)
        {
            int max = 0, words = 0;
            bool inWord = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (SentenceEnd(text, i))
                {
                    if (inWord) { words++; inWord = false; }
                    if (words > max) max = words;
                    words = 0;
                }
                else if (c == ' ' || c == '\n' || c == '\t')
                {
                    if (inWord) { words++; inWord = false; }
                }
                else inWord = true;
            }
            if (inWord) words++;
            if (words > max) max = words;
            return max;
        }

        static bool SentenceEnd(string s, int i)
        {
            char c = s[i];
            if (c == '!' || c == '?') return true;
            if (c != '.') return false;
            if (i > 0 && i + 1 < s.Length && s[i - 1] >= '0' && s[i - 1] <= '9' && s[i + 1] >= '0' && s[i + 1] <= '9') return false;
            return true;
        }

        static bool Shouts(string text)
        {
            int i = 0;
            while (i < text.Length)
            {
                while (i < text.Length && !Letter(text[i])) i++;
                int start = i;
                while (i < text.Length && (Letter(text[i]) || text[i] == '-' || (text[i] >= '0' && text[i] <= '9'))) i++;
                if (i - start < 4) continue;
                bool all = true;
                int letters = 0;
                for (int k = start; k < i; k++)
                {
                    if (!Letter(text[k])) continue;
                    letters++;
                    if (text[k] < 'A' || text[k] > 'Z') all = false;
                }
                if (all && letters >= 4)
                {
                    string word = text.Substring(start, i - start);
                    if (word != "SKIP" && word != "MECO" && word != "SECO" && word != "RTLS") return true;
                }
            }
            return false;
        }

        static bool Letter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        static bool Banned(string text)
        {
            string lower = text.ToLowerInvariant();
            string[] phrases = {
                "you failed", "you fail", "you lost", "you lose", "you missed", "game over", "your fault",
                "blue origin", "new glenn", "jacklyn", "spacex", "nasa", "boeing", "lockheed",
                "ariane", "soyuz", "falcon", "starship", "saturn", "apollo", "rp-1", "kerbal", "elon", "musk", "bezos"
            };
            for (int i = 0; i < phrases.Length; i++)
                if (lower.IndexOf(phrases[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        static string Need(JsonObject e, string key)
        {
            string s = e.Str(key);
            if (string.IsNullOrEmpty(s)) throw new FormatException("guide." + key + " missing");
            return s;
        }
    }

    public static class GuideText
    {
        public static string Fill(string text, IReadOnlyDictionary<string, string> tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null || tokens.Count == 0) return text ?? "";
            var sb = new System.Text.StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    int j = text.IndexOf('}', i + 1);
                    if (j > i + 1)
                    {
                        string key = text.Substring(i + 1, j - i - 1);
                        if (tokens.TryGetValue(key, out var value) && value != null)
                        {
                            sb.Append(value);
                            i = j;
                            continue;
                        }
                    }
                }
                sb.Append(text[i]);
            }
            return sb.ToString();
        }
    }
}
